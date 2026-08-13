using System;
using System.Collections.Generic;
using LightInDark.Configuration;
using LightInDark.Core;

namespace LightInDark.Roles
{
    /// <summary>
    /// 角色注册中心。在插件主类 Load() 中调用 RoleRegistry.Register() 注册角色。
    /// 防止运行时反射扫描的性能消耗。
    /// </summary>
    public static class RoleRegistry
    {
        private static readonly Dictionary<string, DefinedRole> _roles = new();
        private static readonly Dictionary<Type, DefinedRole> _rolesByType = new();
        private static readonly Dictionary<int, DefinedRole> _rolesById = new();
        private static int _nextId;

        /// <summary>已注册的所有角色定义</summary>
        public static IReadOnlyCollection<DefinedRole> AllRoles => _roles.Values;

        /// <summary>
        /// 注册角色。在 LIDPlugin.Load() 中调用。
        /// </summary>
        public static T Register<T>() where T : DefinedRole, new()
        {
            try
            {
                return Register(new T());
            }
            catch (Exception ex)
            {
                LightLogger.LogError("RoleRegistry.Register", ex);
                return default;
            }
        }

        /// <summary>
        /// 注册角色实例。
        /// </summary>
        public static T Register<T>(T role) where T : DefinedRole
        {
            try
            {
                if (role == null) throw new ArgumentNullException(nameof(role));
                role.Id = _nextId++;
                _roles[role.Name] = role;
                _rolesByType[typeof(T)] = role;
                _rolesById[role.Id] = role;
                return role;
            }
            catch (Exception ex)
            {
                LightLogger.LogError("RoleRegistry.Register<T>", ex);
                return default;
            }
        }

        /// <summary>按名称获取角色定义</summary>
        public static DefinedRole GetByName(string name)
        {
            try
            {
                return _roles.TryGetValue(name, out var role) ? role : null;
            }
            catch (Exception ex)
            {
                LightLogger.LogError("RoleRegistry.GetByName", ex);
                return null;
            }
        }

        /// <summary>按类型获取角色定义</summary>
        public static T Get<T>() where T : DefinedRole
        {
            try
            {
                return _rolesByType.TryGetValue(typeof(T), out var role) ? role as T : null;
            }
            catch (Exception ex)
            {
                LightLogger.LogError("RoleRegistry.Get", ex);
                return default;
            }
        }

        /// <summary>按注册序号获取角色定义</summary>
        public static DefinedRole GetById(int id)
        {
            try
            {
                return _rolesById.TryGetValue(id, out var role) ? role : null;
            }
            catch (Exception ex)
            {
                LightLogger.LogError("RoleRegistry.GetById", ex);
                return null;
            }
        }

        /// <summary>角色是否已注册</summary>
        public static bool IsRegistered(string name)
        {
            try
            {
                return _roles.ContainsKey(name);
            }
            catch (Exception ex)
            {
                LightLogger.LogError("RoleRegistry.IsRegistered", ex);
                return default;
            }
        }

        /// <summary>清空所有注册（游戏结束时调用）</summary>
        public static void Clear()
        {
            try
            {
                _roles.Clear();
                _rolesByType.Clear();
                _rolesById.Clear();
                _nextId = 0;
            }
            catch (Exception ex)
            {
                LightLogger.LogError("RoleRegistry.Clear", ex);
            }
        }
    }

    /// <summary>
    /// 角色类型检查扩展。使用方式：player.Is&lt;SheriffRuntime&gt;()
    /// 或 player.HasRole&lt;SheriffRuntime&gt;()
    /// </summary>
    public static class RoleTypeChecker
    {
        /// <summary>检查玩家是否拥有指定类型的角色</summary>
        public static bool HasRole<T>(this Game.Player player) where T : RuntimeRole
        {
            try
            {
                return player.Role is T;
            }
            catch (Exception ex)
            {
                LightLogger.LogError("RoleTypeChecker.HasRole", ex);
                return default;
            }
        }

        /// <summary>获取玩家的指定类型角色实例（如果存在）</summary>
        public static T GetRole<T>(this Game.Player player) where T : RuntimeRole
        {
            try
            {
                return player.Role as T;
            }
            catch (Exception ex)
            {
                LightLogger.LogError("RoleTypeChecker.GetRole", ex);
                return default;
            }
        }

        /// <summary>检查玩家是否为指定角色定义</summary>
        public static bool Is<TDef, TRuntime>(this Game.Player player)
            where TDef : DefinedRole
            where TRuntime : RuntimeRole
        {
            try
            {
                return player.Role is TRuntime && player.Role?.Definition is TDef;
            }
            catch (Exception ex)
            {
                LightLogger.LogError("RoleTypeChecker.Is", ex);
                return default;
            }
        }

        /// <summary>检查玩家是否为指定类别</summary>
        public static bool IsCategory(this Game.Player player, RoleCategory category)
        {
            try
            {
                return player.Role?.Definition?.Category == category;
            }
            catch (Exception ex)
            {
                LightLogger.LogError("RoleTypeChecker.IsCategory", ex);
                return default;
            }
        }

        /// <summary>检查玩家是否为船员</summary>
        public static bool IsCrewmate(this Game.Player player)
        {
            try
            {
                return player.IsCategory(RoleCategory.Crewmate);
            }
            catch (Exception ex)
            {
                LightLogger.LogError("RoleTypeChecker.IsCrewmate", ex);
                return default;
            }
        }

        /// <summary>检查玩家是否为内鬼</summary>
        public static bool IsImpostor(this Game.Player player)
        {
            try
            {
                return player.IsCategory(RoleCategory.Impostor);
            }
            catch (Exception ex)
            {
                LightLogger.LogError("RoleTypeChecker.IsImpostor", ex);
                return default;
            }
        }

        /// <summary>检查玩家是否为中立</summary>
        public static bool IsNeutral(this Game.Player player)
        {
            try
            {
                return player.IsCategory(RoleCategory.Neutral);
            }
            catch (Exception ex)
            {
                LightLogger.LogError("RoleTypeChecker.IsNeutral", ex);
                return default;
            }
        }

        /// <summary>检查玩家是否存活且有角色</summary>
        public static bool IsAliveWithRole(this Game.Player player)
        {
            try
            {
                return !player.IsDead && player.HasRole;
            }
            catch (Exception ex)
            {
                LightLogger.LogError("RoleTypeChecker.IsAliveWithRole", ex);
                return default;
            }
        }
    }
}
