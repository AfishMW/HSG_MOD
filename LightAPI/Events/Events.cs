using LightInDark.Roles;
using UnityEngine;

namespace LightInDark.Events
{
    // =====================================================================
    // 事件基础类型
    //
    // - Event: 标记接口（已有 IEvent）
    // - AbstractPlayerEvent: 持有 PlayerControl，支持 [OnlyMyPlayer] 等过滤
    // - 可取消事件: public bool IsCanceled { get; set; }（合作式取消，非抢占式）
    // - 内部构造函数：事件由框架构造，不由消费者创建
    // =====================================================================

    /// <summary>
    /// 玩家事件基类。
    /// 持有 PlayerControl，支持 [OnlyMyPlayer] / [OnlyLocalPlayer] 等过滤。
    /// </summary>
    public abstract class BasePlayerEvent : IEvent
    {
        public PlayerControl Player { get; init; }
        protected BasePlayerEvent(PlayerControl player) => Player = player;
        protected BasePlayerEvent() { }
    }

    /// <summary>
    /// 可取消事件基类。
    /// IsCanceled 为合作式：监听者设置后，后续监听者可读取。
    /// 调度器不会自动中断，由调用方检查 IsCanceled。
    /// </summary>
    public abstract class BaseCancelableEvent : IEvent
    {
        public bool IsCanceled { get; set; }
    }

    /// <summary>
    /// 可取消的玩家事件。
    /// </summary>
    public abstract class BaseCancelablePlayerEvent : BasePlayerEvent
    {
        public bool IsCanceled { get; set; }
        protected BaseCancelablePlayerEvent(PlayerControl player) : base(player) { }
        protected BaseCancelablePlayerEvent() { }
    }

    // =====================================================================
    // 游戏流程事件
    // =====================================================================

    /// <summary>游戏开始</summary>
    public class GameStartEvent : IEvent
    {
        public int PlayerCount { get; init; }
    }

    /// <summary>游戏结束</summary>
    public class GameEndEvent : IEvent
    {
        public bool CrewmatesWin { get; init; }
        public bool ImpostorsWin { get; init; }
        public string WinReason { get; init; } = "";
    }

    /// <summary>游戏即将结束（可阻止）</summary>
    public class GameTryEndEvent : BaseCancelableEvent
    {
        public bool CrewmatesWin { get; set; }
        public bool ImpostorsWin { get; set; }
        public string Reason { get; set; } = "";
    }

    /// <summary>自定义职业被设置到玩家</summary>
    public class RoleAssignedEvent : BasePlayerEvent
    {
        public DefinedRole Role { get; init; }
        public int[] Arguments { get; init; } = System.Array.Empty<int>();
        public RoleAssignedEvent() { }
        public RoleAssignedEvent(PlayerControl player, DefinedRole role, int[] arguments = null) : base(player) { Role = role; Arguments = arguments ?? System.Array.Empty<int>(); }
    }

    /// <summary>换职前（可阻止）</summary>
    public class PlayerTryToChangeRoleEvent : BaseCancelablePlayerEvent
    {
        public RuntimeRole OldRole { get; init; }
        public DefinedRole NewRole { get; init; }
        public PlayerTryToChangeRoleEvent() { }
        public PlayerTryToChangeRoleEvent(PlayerControl player, RuntimeRole oldRole, DefinedRole newRole) : base(player) { OldRole = oldRole; NewRole = newRole; }
    }

    /// <summary>分配确定前（供分配机修正分配表）</summary>
    public class PreFixAssignmentEvent : IEvent
    {
        public IRoleTable Table { get; }
        public PreFixAssignmentEvent(IRoleTable table) { Table = table; }
    }

    /// <summary>玩家断开连接</summary>
    public class PlayerDisconnectEvent : BasePlayerEvent
    {
        public PlayerDisconnectEvent() { }
        public PlayerDisconnectEvent(PlayerControl player) : base(player) { }
    }

    // =====================================================================
    // 玩家生死事件
    // =====================================================================

    /// <summary>玩家自杀</summary>
    public class PlayerSuicideEvent : BasePlayerEvent
    {
        public string Reason { get; init; } = "suicide";
        public bool NeedLog { get; init; } = true;
        public PlayerSuicideEvent() { }
        public PlayerSuicideEvent(PlayerControl player, string reason = "suicide") : base(player) { Reason = reason; }
    }

    /// <summary>玩家击杀</summary>
    public class PlayerMurderEvent : BasePlayerEvent
    {
        public PlayerControl Killer => Player;
        public PlayerControl Victim { get; init; }
        public PlayerMurderEvent() { }
        public PlayerMurderEvent(PlayerControl killer, PlayerControl victim) : base(killer) { Victim = victim; }
    }

    /// <summary>尝试击杀（可阻止）</summary>
    public class PlayerTryMurderEvent : BaseCancelablePlayerEvent
    {
        public PlayerControl Killer => Player;
        public PlayerControl Victim { get; init; }
        public PlayerTryMurderEvent(PlayerControl killer, PlayerControl victim) : base(killer) { Victim = victim; }
    }

    /// <summary>玩家死亡</summary>
    public class PlayerDeathEvent : BasePlayerEvent
    {
        public PlayerControl Killer { get; init; }
        public DeathReason Reason { get; init; }
        public PlayerDeathEvent() { }
        public PlayerDeathEvent(PlayerControl player, DeathReason reason, PlayerControl killer = null) : base(player) { Reason = reason; Killer = killer; }
    }

    /// <summary>玩家复活</summary>
    public class PlayerReviveEvent : BasePlayerEvent
    {
        public PlayerReviveEvent() { }
        public PlayerReviveEvent(PlayerControl player) : base(player) { }
    }

    // =====================================================================
    // 任务事件
    // =====================================================================

    /// <summary>玩家完成任务</summary>
    public class PlayerTaskCompleteEvent : BasePlayerEvent
    {
        public int CompletedTasks { get; init; }
        public int TotalTasks { get; init; }
        public PlayerTaskCompleteEvent() { }
        public PlayerTaskCompleteEvent(PlayerControl player, int completed, int total) : base(player) { CompletedTasks = completed; TotalTasks = total; }
    }

    /// <summary>所有任务完成（可阻止游戏结束）</summary>
    public class AllTasksCompleteEvent : BaseCancelableEvent
    {
        public PlayerControl Player { get; init; }
    }

    // =====================================================================
    // 会议事件
    // =====================================================================

    /// <summary>尝试召开会议（可阻止）</summary>
    public class MeetingTryStartEvent : BaseCancelableEvent
    {
        public PlayerControl Reporter { get; init; }
        public NetworkedPlayerInfo ReportedBody { get; init; }
        public bool IsEmergencyMeeting { get; init; }
    }

    /// <summary>会议开始</summary>
    public class MeetingStartEvent : IEvent
    {
        public PlayerControl Reporter { get; init; }
        public NetworkedPlayerInfo ReportedBody { get; init; }
        public bool IsEmergencyMeeting { get; init; }
    }

    /// <summary>讨论阶段开始</summary>
    public class MeetingDiscussionStartEvent : IEvent { }

    /// <summary>投票阶段开始</summary>
    public class MeetingVotingStartEvent : IEvent { }

    /// <summary>玩家投票（可修改投票权重）</summary>
    public class PlayerVoteEvent : BasePlayerEvent
    {
        public byte VotedForPlayerId { get; set; }
        public int VoteWeight { get; set; } = 1;
        public PlayerVoteEvent() { }
        public PlayerVoteEvent(PlayerControl player, byte votedFor) : base(player) { VotedForPlayerId = votedFor; }
    }

    /// <summary>尝试结束投票（可修改谁被放逐）</summary>
    public class MeetingTryEndVotingEvent : BaseCancelableEvent
    {
        public byte ExiledPlayerId { get; set; }
        public bool IsTie { get; set; }
    }

    /// <summary>会议即将结束（可阻止）</summary>
    public class MeetingTryEndEvent : BaseCancelableEvent { }

    /// <summary>会议结束</summary>
    public class MeetingEndEvent : IEvent
    {
        public byte ExiledPlayerId { get; init; }
        public bool WasTie { get; init; }
    }

    // =====================================================================
    // 放逐事件
    // =====================================================================

    /// <summary>放逐即将执行（可修改谁被放逐）</summary>
    public class PlayerTryExileEvent : BaseCancelableEvent
    {
        public byte ExilePlayerId { get; set; }
        public bool IsTie { get; set; }
    }

    /// <summary>放逐动画执行中（只读）</summary>
    public class PlayerExileEvent : IEvent
    {
        public PlayerControl Exiled { get; init; }
        public PlayerExileEvent() { }
        public PlayerExileEvent(PlayerControl exiled) { Exiled = exiled; }
    }

    // =====================================================================
    // 系统/交互事件
    // =====================================================================

    /// <summary>紧急按钮被破坏</summary>
    public class EmergencyButtonBrokenEvent : IEvent { }

    /// <summary>HUD 激活状态变更</summary>
    public class HudActiveChangeEvent : IEvent
    {
        public bool IsActive { get; init; }
    }

    /// <summary>破坏系统激活</summary>
    public class SabotageStartEvent : IEvent
    {
        public SystemTypes SystemType { get; init; }
        public SabotageStartEvent(SystemTypes type) { SystemType = type; }
    }

    /// <summary>破坏系统修复</summary>
    public class SabotageEndEvent : IEvent
    {
        public SystemTypes SystemType { get; init; }
        public SabotageEndEvent(SystemTypes type) { SystemType = type; }
    }

    /// <summary>管道被使用</summary>
    public class VentUsedEvent : IEvent
    {
        public int VentId { get; init; }
        public PlayerControl Player { get; init; }
    }

    /// <summary>玩家被踢出</summary>
    public class PlayerKickEvent : BasePlayerEvent
    {
        public string Reason { get; init; } = "";
        public PlayerControl Kicker { get; init; }
        public PlayerKickEvent(PlayerControl player, PlayerControl kicker, string reason = "") : base(player) { Kicker = kicker; Reason = reason; }
    }

    /// <summary>聊天消息</summary>
    public class ChatMessageEvent : BasePlayerEvent
    {
        public string Message { get; init; } = "";
        public ChatMessageEvent() { }
        public ChatMessageEvent(PlayerControl player, string message) : base(player) { Message = message; }
    }

    /// <summary>玩家移动（每帧，高频）</summary>
    public class PlayerMoveEvent : BasePlayerEvent
    {
        public Vector2 Position { get; init; }
        public PlayerMoveEvent(PlayerControl player, Vector2 pos) : base(player) { Position = pos; }
    }
}
