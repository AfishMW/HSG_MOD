using UnityEngine;

namespace LightInDark.Events
{
    // =====================================================================
    //  游戏流程事件
    // =====================================================================

    /// <summary>游戏开始时触发。</summary>
    public class GameStartEvent : IEvent
    {
        /// <summary>参与本局的玩家数量。</summary>
        public int PlayerCount { get; init; }
    }

    /// <summary>游戏结束时触发。</summary>
    public class GameEndEvent : IEvent
    {
        /// <summary>船员是否获胜。</summary>
        public bool CrewmatesWin { get; init; }
        /// <summary>内鬼是否获胜。</summary>
        public bool ImpostorsWin { get; init; }
        /// <summary>获胜原因描述。</summary>
        public string WinReason { get; init; } = "";
    }

    /// <summary>游戏即将结束（可阻止）。</summary>
    public class GameTryEndEvent : BaseCancelableEvent
    {
        public bool CrewmatesWin { get; set; }
        public bool ImpostorsWin { get; set; }
        public string Reason { get; set; } = "";
    }

    /// <summary>游戏每帧更新时触发。</summary>
    public class GameUpdateEvent : IEvent
    {
        public float DeltaTime { get; init; }
    }

    /// <summary>HUD 每帧更新时触发。用于修改玩家轮廓等视觉元素。</summary>
    public class GameHudUpdateEvent : IEvent
    {
        public float DeltaTime { get; init; }
    }

    /// <summary>LateUpdate 时机触发。</summary>
    public class GameLateUpdateEvent : IEvent
    {
        public float DeltaTime { get; init; }
    }

    /// <summary>紧急按钮被破坏时触发。</summary>
    public class EmergencyButtonBrokenEvent : IEvent { }

    /// <summary>破坏系统激活时触发。</summary>
    public class SabotageStartEvent : IEvent
    {
        public SystemTypes SystemType { get; init; }
        public SabotageStartEvent() { }
        public SabotageStartEvent(SystemTypes type) { SystemType = type; }
    }

    /// <summary>破坏系统修复时触发。</summary>
    public class SabotageEndEvent : IEvent
    {
        public SystemTypes SystemType { get; init; }
        public SabotageEndEvent() { }
        public SabotageEndEvent(SystemTypes type) { SystemType = type; }
    }

    /// <summary>HUD 激活状态变更时触发。</summary>
    public class HudActiveChangeEvent : IEvent
    {
        public bool IsActive { get; init; }
    }
}
