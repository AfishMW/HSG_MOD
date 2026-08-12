using LightInDark.Roles;

namespace LightInDark.Events
{
    // =====================================================================
    //  角色事件
    // =====================================================================

    /// <summary>自定义角色被设置到玩家时触发。</summary>
    public class RoleAssignedEvent : BasePlayerEvent
    {
        public DefinedRole Role { get; init; }
        public int[] Arguments { get; init; } = System.Array.Empty<int>();
        public RoleAssignedEvent() { }
        public RoleAssignedEvent(PlayerControl player, DefinedRole role, int[] arguments = null) : base(player) { Role = role; Arguments = arguments ?? System.Array.Empty<int>(); }
    }

    /// <summary>换职前触发（可阻止）。</summary>
    public class PlayerTryToChangeRoleEvent : BaseCancelablePlayerEvent
    {
        public RuntimeRole OldRole { get; init; }
        public DefinedRole NewRole { get; init; }
        public PlayerTryToChangeRoleEvent() { }
        public PlayerTryToChangeRoleEvent(PlayerControl player, RuntimeRole oldRole, DefinedRole newRole) : base(player) { OldRole = oldRole; NewRole = newRole; }
    }

    /// <summary>分配确定前触发（供分配机修正分配表）。</summary>
    public class PreFixAssignmentEvent : IEvent
    {
        public IRoleTable Table { get; }
        public PreFixAssignmentEvent(IRoleTable table) { Table = table; }
    }

    /// <summary>角色已设置到玩家时触发。</summary>
    public class PlayerRoleSetEvent : BasePlayerEvent
    {
        public RuntimeRole Role { get; init; }
        public PlayerRoleSetEvent() { }
        public PlayerRoleSetEvent(PlayerControl player, RuntimeRole role) : base(player) { Role = role; }
    }

    /// <summary>角色交换时触发。</summary>
    public class PlayerRoleSwapEvent : BasePlayerEvent
    {
        public enum SwapType { Swap, Duplicate }

        public PlayerControl Source { get; init; }
        public PlayerControl Destination => Player;
        public DefinedRole Role { get; init; }
        public SwapType Type { get; init; }
        public PlayerRoleSwapEvent() { }
        public PlayerRoleSwapEvent(PlayerControl source, PlayerControl destination, DefinedRole role, SwapType type) : base(destination)
        { Source = source; Role = role; Type = type; }
    }

    /// <summary>检查玩家是否胜利时触发（仅房主）。</summary>
    public class PlayerCheckWinEvent : BasePlayerEvent
    {
        public string GameEnd { get; init; } = "";
        public bool IsWin { get; set; }
        public void SetWinIf(bool win) => IsWin |= win;
        public PlayerCheckWinEvent() { }
        public PlayerCheckWinEvent(PlayerControl player, string gameEnd = "") : base(player) { GameEnd = gameEnd; }
    }

    /// <summary>检查额外胜利时触发。</summary>
    public class PlayerCheckExtraWinEvent : BasePlayerEvent
    {
        public string GameEnd { get; init; } = "";
        public bool IsExtraWin { get; set; }
        public void SetWinIf(bool win) => IsExtraWin |= win;
        public PlayerCheckExtraWinEvent() { }
        public PlayerCheckExtraWinEvent(PlayerControl player, string gameEnd = "") : base(player) { GameEnd = gameEnd; }
    }

    /// <summary>阻止玩家胜利时触发。</summary>
    public class PlayerBlockWinEvent : BasePlayerEvent
    {
        public string GameEnd { get; init; } = "";
        public bool IsWin { get; init; }
        public bool IsBlocked { get; set; }
        public void SetBlockedIf(bool blocked) => IsBlocked |= blocked;
        public PlayerBlockWinEvent() { }
        public PlayerBlockWinEvent(PlayerControl player, bool isWin, string gameEnd = "") : base(player) { IsWin = isWin; GameEnd = gameEnd; }
    }

    /// <summary>修饰器被添加到玩家时触发。</summary>
    public class PlayerModifierSetEvent : BasePlayerEvent
    {
        public string ModifierName { get; init; } = "";
        public PlayerModifierSetEvent() { }
        public PlayerModifierSetEvent(PlayerControl player, string modifierName) : base(player) { ModifierName = modifierName; }
    }

    /// <summary>修饰器从玩家移除时触发。</summary>
    public class PlayerModifierRemoveEvent : BasePlayerEvent
    {
        public string ModifierName { get; init; } = "";
        public PlayerModifierRemoveEvent() { }
        public PlayerModifierRemoveEvent(PlayerControl player, string modifierName) : base(player) { ModifierName = modifierName; }
    }
}
