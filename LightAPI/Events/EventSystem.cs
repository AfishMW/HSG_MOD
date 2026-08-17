using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using LightInDark.Core;

namespace LightInDark.Events
{
    public static class EventSystem
    {
        private class ListenerEntry
        {
            public object Instance;
            public MethodInfo Method;
            public int Priority;
            public bool OnlyHost;
            public bool OnlyMyPlayer;
            public bool Local;
        }

        private static readonly Dictionary<Type, List<ListenerEntry>> _listeners = new();
        private static readonly HashSet<object> _registeredInstances = new();

        public static void ScanAndRegisterAll()
        {
            try
            {
                _listeners.Clear();
                _playerAccessorCache.Clear();
                _dispatchCache.Clear();
                var assemblies = new[] { Assembly.GetExecutingAssembly(), AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(a => a.GetName().Name == "Light") }
                    .Where(a => a != null).Distinct();

                foreach (var assembly in assemblies)
                foreach (var type in assembly.GetTypes())
                {
                    if (type.IsAbstract || type.IsInterface || !type.IsClass)
                        continue;

                    foreach (var method in type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static))
                    {
                        // 仅接受单个 IEvent 参数的监听方法
                        var parameters = method.GetParameters();
                        if (parameters.Length != 1)
                            continue;

                        var eventType = parameters[0].ParameterType;
                        if (!typeof(IEvent).IsAssignableFrom(eventType))
                            continue;

                        var priority = method.GetCustomAttribute<EventPriorityAttribute>()?.Priority ?? 0;
                        var onlyHost = method.GetCustomAttribute<OnlyHostAttribute>() != null;
                        var onlyMyPlayer = method.GetCustomAttribute<OnlyMyPlayerAttribute>() != null;
                        var local = method.GetCustomAttribute<LocalAttribute>() != null;

                        if (!_listeners.ContainsKey(eventType))
                            _listeners[eventType] = new List<ListenerEntry>();

                        _listeners[eventType].Add(new ListenerEntry
                        {
                            Instance = null,
                            Method = method,
                            Priority = priority,
                            OnlyHost = onlyHost,
                            OnlyMyPlayer = onlyMyPlayer,
                            Local = local
                        });
                    }
                }

                SortAllListeners();
                LightLogger.Log($"EventSystem scan complete. {_listeners.Sum(kv => kv.Value.Count)} listeners cached.");
            }
            catch (Exception ex)
            {
                LightLogger.LogError("EventSystem.ScanAndRegisterAll", ex);
            }
        }

        public static void RegisterInstance(object instance)
        {
            try
            {
                if (instance == null || _registeredInstances.Contains(instance))
                    return;

                var type = instance.GetType();
                foreach (var method in type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
                {
                    var parameters = method.GetParameters();
                    if (parameters.Length != 1)
                        continue;

                    var eventType = parameters[0].ParameterType;
                    if (!typeof(IEvent).IsAssignableFrom(eventType))
                        continue;

                    if (_listeners.TryGetValue(eventType, out var list))
                    {
                        for (int i = 0; i < list.Count; i++)
                        {
                            if (list[i].Method.DeclaringType == type && list[i].Instance == null)
                            {
                                list[i] = new ListenerEntry
                                {
                                    Instance = instance,
                                    Method = list[i].Method,
                                    Priority = list[i].Priority,
                                    OnlyHost = list[i].OnlyHost,
                                    OnlyMyPlayer = list[i].OnlyMyPlayer,
                                    Local = list[i].Local
                                };
                            }
                        }
                    }
                }

                _registeredInstances.Add(instance);
                SortAllListeners();
            }
            catch (Exception ex)
            {
                LightLogger.LogError("EventSystem.RegisterInstance", ex);
            }
        }

        public static void UnregisterInstance(object instance)
        {
            try
            {
                if (instance == null)
                    return;

                _registeredInstances.Remove(instance);
                SortAllListeners();

                foreach (var kv in _listeners)
                {
                    for (int i = kv.Value.Count - 1; i >= 0; i--)
                    {
                        if (kv.Value[i].Instance == instance)
                        {
                            kv.Value[i] = new ListenerEntry
                            {
                                Instance = null,
                                Method = kv.Value[i].Method,
                                Priority = kv.Value[i].Priority,
                                OnlyHost = kv.Value[i].OnlyHost,
                                OnlyMyPlayer = kv.Value[i].OnlyMyPlayer,
                                Local = kv.Value[i].Local
                            };
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LightLogger.LogError("EventSystem.UnregisterInstance", ex);
            }
        }

        // 事件类型 -> Player 字段访问器缓存（只为需要 OnlyMyPlayer 的事件创建）
        private static readonly Dictionary<Type, Func<IEvent, object>> _playerAccessorCache = new();
        // 事件类型 -> 已合并排序的调度计划（Scan/Register/Unregister 后重建，运行期只读）
        private static readonly Dictionary<Type, List<ListenerEntry>> _dispatchCache = new();

        public static T RunEvent<T>(T ev) where T : IEvent
        {
            try
            {
                var type = typeof(T);

                // 按优先级降序对“自身 + 所有 IEvent 基类”的监听器统一调度。
                // 支持事件继承：如 ReportDeadBodyEvent/CalledEmergencyMeetingEvent : MeetingPreStartEvent。
                DispatchEvent(type, ev);
                return ev;
            }
            catch (Exception ex)
            {
                LightLogger.LogError("EventSystem.RunEvent", ex);
                return default(T);
            }
        }

        /// <summary>把事件分发给该类型及其所有 IEvent 基类的监听器。</summary>
        private static void DispatchEvent(Type eventType, IEvent ev)
        {
            if (!_dispatchCache.TryGetValue(eventType, out var combined) || combined == null || combined.Count == 0)
                return;

            bool host = AmongUsClient.Instance?.AmHost ?? false;
            bool client = AmongUsClient.Instance?.AmClient ?? false;
            PlayerControl localPlayer = PlayerControl.LocalPlayer;

            for (int i = 0; i < combined.Count; i++)
            {
                var entry = combined[i];

                // 静态监听可直接调用；实例监听需绑定实例，未绑定则跳过
                if (!entry.Method.IsStatic && entry.Instance == null)
                    continue;

                // 过滤属性
                if (entry.OnlyHost && !host) continue;
                if (entry.Local && !client) continue;
                if (entry.OnlyMyPlayer)
                {
                    if (localPlayer == null) continue;
                    var getter = GetPlayerAccessor(eventType);
                    PlayerControl eventPlayer = null;
                    if (getter != null)
                    {
                        try { eventPlayer = getter(ev) as PlayerControl; }
                        catch { eventPlayer = null; }
                    }
                    if (eventPlayer != localPlayer) continue;
                }

                try
                {
                    // 实例方法用绑定实例，静态方法用 null 接收器
                    entry.Method.Invoke(entry.Instance, new[] { ev });
                }
                catch (Exception ex)
                {
                    LightLogger.LogError($"Event execution error in {entry.Method.Name}: {ex}");
                }
            }
        }

        private static Func<IEvent, object> GetPlayerAccessor(Type type)
        {
            if (_playerAccessorCache.TryGetValue(type, out var accessor))
                return accessor;

            var prop = type.GetProperty("Player");
            Func<IEvent, object> result = null;
            if (prop != null && typeof(PlayerControl).IsAssignableFrom(prop.PropertyType))
            {
                var getMethod = prop.GetGetMethod();
                if (getMethod != null)
                {
                    result = ev => { try { return getMethod.Invoke(ev, null); } catch { return null; } };
                }
            }
            _playerAccessorCache[type] = result;
            return result;
        }

        /// <summary>按优先级降序对某事件的监听器列表排序。</summary>
        private static void SortListeners(Type type)
        {
            if (!_listeners.TryGetValue(type, out var list)) return;
            list.Sort((a, b) => b.Priority.CompareTo(a.Priority));
        }

        /// <summary>对所有已注册事件：按优先级排序，并重建“包含基类监听器的调度计划”。</summary>
        private static void SortAllListeners()
        {
            foreach (var key in _listeners.Keys)
                SortListeners(key);
            RebuildDispatchCache();
        }

        /// <summary>
        /// 预生成每个“具体事件类”的调度计划：其“自身 + 所有 IEvent 基类”的合并监听列表
        /// （按优先级降序）。运行期 RunEvent 直接查表，避免每次分派做反射扫描与排序。
        /// 覆盖所有可触发的事件类型（含派生子类，如 ReportDeadBodyEvent : MeetingPreStartEvent）。
        /// </summary>
        private static void RebuildDispatchCache()
        {
            _dispatchCache.Clear();

            // 收集本程序集（API）里所有实现 IEvent 的具体类（含子类，用于派生事件命中基类监听器）
            var allEventTypes = new List<Type>();
            foreach (var asm in new[] { Assembly.GetExecutingAssembly() })
            {
                Type[] types;
                try { types = asm.GetTypes(); }
                catch { continue; }
                foreach (var t in types)
                {
                    if (t.IsAbstract || t.IsInterface || !t.IsClass) continue;
                    if (typeof(IEvent).IsAssignableFrom(t))
                        allEventTypes.Add(t);
                }
            }

            foreach (var concreteType in allEventTypes)
            {
                List<ListenerEntry> combined = null;
                Type cur = concreteType;
                while (cur != null && typeof(IEvent).IsAssignableFrom(cur))
                {
                    if (_listeners.TryGetValue(cur, out var list))
                    {
                        if (combined == null) combined = new List<ListenerEntry>();
                        combined.AddRange(list);
                    }
                    cur = cur.BaseType;
                }
                if (combined != null)
                {
                    combined.Sort((a, b) => b.Priority.CompareTo(a.Priority));
                    _dispatchCache[concreteType] = combined;
                }
            }
        }
    }
}
