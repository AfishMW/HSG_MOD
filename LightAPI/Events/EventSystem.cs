using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using LightInDark.Core;

namespace LightInDark.Events
{
    public interface IEvent { }

    [AttributeUsage(AttributeTargets.Method)]
    public class EventPriority : Attribute
    {
        public int Priority;
        public EventPriority(int priority = 0) => Priority = priority;
    }

    [AttributeUsage(AttributeTargets.Method)]
    public class OnlyMyPlayer : Attribute { }

    [AttributeUsage(AttributeTargets.Method)]
    public class OnlyHost : Attribute { }

    [AttributeUsage(AttributeTargets.Method)]
    public class Local : Attribute { }

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
            _listeners.Clear();
            var assembly = Assembly.GetExecutingAssembly();

            foreach (var type in assembly.GetTypes())
            {
                if (type.IsAbstract || type.IsInterface || !type.IsClass)
                    continue;

                foreach (var method in type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
                {
                    var parameters = method.GetParameters();
                    if (parameters.Length != 1)
                        continue;

                    var eventType = parameters[0].ParameterType;
                    if (!typeof(IEvent).IsAssignableFrom(eventType))
                        continue;

                    var priority = method.GetCustomAttribute<EventPriority>()?.Priority ?? 0;
                    var onlyHost = method.GetCustomAttribute<OnlyHost>() != null;
                    var onlyMyPlayer = method.GetCustomAttribute<OnlyMyPlayer>() != null;
                    var local = method.GetCustomAttribute<Local>() != null;

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

            LightLogger.Log($"EventSystem scan complete. {_listeners.Sum(kv => kv.Value.Count)} listeners cached.");
        }

        public static void RegisterInstance(object instance)
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
        }

        public static void UnregisterInstance(object instance)
        {
            if (instance == null)
                return;

            _registeredInstances.Remove(instance);

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

        public static T RunEvent<T>(T ev) where T : IEvent
        {
            var type = typeof(T);
            if (!_listeners.TryGetValue(type, out var candidates))
                return ev;

            var sorted = candidates
                .Where(c => c.Instance != null)
                .OrderByDescending(c => c.Priority)
                .ToList();

            foreach (var entry in sorted)
            {
                if (entry.OnlyHost && !AmongUsClient.Instance.AmHost)
                    continue;

                if (entry.Local && !AmongUsClient.Instance.AmClient)
                    continue;

                if (entry.OnlyMyPlayer)
                {
                    var localPlayer = PlayerControl.LocalPlayer;
                    if (localPlayer == null)
                        continue;

                    var playerMember = type.GetProperty("Player") ?? (MemberInfo)type.GetField("Player");
                    if (playerMember != null)
                    {
                        PlayerControl eventPlayer = null;
                        if (playerMember is PropertyInfo prop)
                            eventPlayer = prop.GetValue(ev) as PlayerControl;
                        else if (playerMember is FieldInfo field)
                            eventPlayer = field.GetValue(ev) as PlayerControl;

                        if (eventPlayer != localPlayer)
                            continue;
                    }
                    else
                    {
                        LightLogger.LogWarning($"Event {type.Name} has no 'Player' field, OnlyMyPlayer ignored.");
                    }
                }

                try
                {
                    entry.Method.Invoke(entry.Instance, new object[] { ev });
                }
                catch (Exception ex)
                {
                    LightLogger.LogError($"Event execution error in {entry.Method.Name}: {ex}");
                }
            }

            return ev;
        }
    }
}