using System;
using System.Collections.Generic;
using LightInDark.Configuration;

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

        /// <summary>已注册的所有角色定义</summary>
        public static IReadOnlyCollection<DefinedRole> AllRoles => _roles.Values;

        /// <summary>
        /// 注册角色。在 LIDPlugin.Load() 中调用。
        /// </summary>
        public static T Register<T>() where T : DefinedRole, new()
        {
            var role = new T();
            _roles[role.Name] = role;
            _rolesByType[typeof(T)] = role;
            return role;
        }

        /// <summary>
        /// 注册角色实例。
        /// </summary>
        public static T Register<T>(T role) where T : DefinedRole
        {
            if (role == null) throw new ArgumentNullException(nameof(role));
            _roles[role.Name] = role;
            _rolesByType[typeof(T)] = role;
            return role;
        }

        /// <summary>按名称获取角色定义</summary>
        public static DefinedRole GetByName(string name)
            => _roles.TryGetValue(name, out var role) ? role : null;

        /// <summary>按类型获取角色定义</summary>
        public static T Get<T>() where T : DefinedRole
            => _rolesByType.TryGetValue(typeof(T), out var role) ? role as T : null;

        /// <summary>角色是否已注册</summary>
        public static bool IsRegistered(string name) => _roles.ContainsKey(name);

        /// <summary>清空所有注册（游戏结束时调用）</summary>
        public static void Clear()
        {
            _roles.Clear();
            _rolesByType.Clear();
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
            => player.Role is T;

        /// <summary>获取玩家的指定类型角色实例（如果存在）</summary>
        public static T GetRole<T>(this Game.Player player) where T : RuntimeRole
            => player.Role as T;

        /// <summary>检查玩家是否为指定角色定义</summary>
        public static bool Is<TDef, TRuntime>(this Game.Player player)
            where TDef : DefinedRole
            where TRuntime : RuntimeRole
            => player.Role is TRuntime && player.Role?.Definition is TDef;

        /// <summary>检查玩家是否为指定类别</summary>
        public static bool IsCategory(this Game.Player player, RoleCategory category)
            => player.Role?.Definition?.Category == category;

        /// <summary>检查玩家是否为船员</summary>
        public static bool IsCrewmate(this Game.Player player)
            => player.IsCategory(RoleCategory.Crewmate);

        /// <summary>检查玩家是否为内鬼</summary>
        public static bool IsImpostor(this Game.Player player)
            => player.IsCategory(RoleCategory.Impostor);

        /// <summary>检查玩家是否为中立</summary>
        public static bool IsNeutral(this Game.Player player)
            => player.IsCategory(RoleCategory.Neutral);

        /// <summary>检查玩家是否存活且有角色</summary>
        public static bool IsAliveWithRole(this Game.Player player)
            => !player.IsDead && player.HasRole;
    }
}
