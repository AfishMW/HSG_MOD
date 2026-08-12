using UnityEngine;

namespace LightInDark.Events
{
    // =====================================================================
    //  玩家生死事件
    // =====================================================================

    /// <summary>玩家自杀时触发。</summary>
    public class PlayerSuicideEvent : BasePlayerEvent
    {
        public string Reason { get; init; } = "suicide";
        public bool NeedLog { get; init; } = true;
        public Game.PlayerState State { get; init; } = Game.PlayerState.Suicide;
        public PlayerSuicideEvent() { }
        public PlayerSuicideEvent(PlayerControl player, string reason = "suicide", Game.PlayerState state = Game.PlayerState.Suicide) : base(player) { Reason = reason; State = state; }
    }

    /// <summary>玩家击杀玩家时触发。</summary>
    public class PlayerMurderEvent : BasePlayerEvent
    {
        public PlayerControl Killer => Player;
        public PlayerControl Victim { get; init; }
        public Game.PlayerState State { get; init; } = Game.PlayerState.BeKilled;
        public PlayerMurderEvent() { }
        public PlayerMurderEvent(PlayerControl killer, PlayerControl victim, Game.PlayerState state = Game.PlayerState.BeKilled) : base(killer) { Victim = victim; State = state; }
    }

    /// <summary>尝试击杀时触发（可阻止）。</summary>
    public class PlayerTryMurderEvent : BaseCancelablePlayerEvent
    {
        public PlayerControl Killer => Player;
        public PlayerControl Victim { get; init; }
        public PlayerTryMurderEvent() { }
        public PlayerTryMurderEvent(PlayerControl killer, PlayerControl victim) : base(killer) { Victim = victim; }
    }

    /// <summary>玩家死亡时触发。</summary>
    public class PlayerDeathEvent : BasePlayerEvent
    {
        public PlayerControl Killer { get; init; }
        public DeathReason Reason { get; init; }
        public Game.PlayerState State { get; init; } = Game.PlayerState.Dead;
        public PlayerDeathEvent() { }
        public PlayerDeathEvent(PlayerControl player, DeathReason reason, PlayerControl killer = null, Game.PlayerState state = Game.PlayerState.Dead) : base(player) { Reason = reason; Killer = killer; State = state; }
    }

    /// <summary>玩家复活时触发。</summary>
    public class PlayerReviveEvent : BasePlayerEvent
    {
        public PlayerControl Healer { get; init; }
        public PlayerReviveEvent() { }
        public PlayerReviveEvent(PlayerControl player, PlayerControl healer = null) : base(player) { Healer = healer; }
    }

    /// <summary>玩家断开连接时触发。</summary>
    public class PlayerDisconnectEvent : BasePlayerEvent
    {
        public PlayerDisconnectEvent() { }
        public PlayerDisconnectEvent(PlayerControl player) : base(player) { }
    }

    /// <summary>检查能否击杀目标时触发（仅本地）。</summary>
    public class PlayerCheckCanKillEvent : BasePlayerEvent
    {
        public PlayerControl Target { get; init; }
        private bool _cannotKillBasically;
        private bool _canKillForcedly;
        private bool _cannotKillForcedly;
        public bool CanKill => !_cannotKillForcedly && (_canKillForcedly || !_cannotKillBasically);
        public void SetAsCannotKillBasically() => _cannotKillBasically = true;
        public void SetAsCanKillForcedly() => _canKillForcedly = true;
        public void SetAsCannotKillForcedly() => _cannotKillForcedly = true;
        public PlayerCheckCanKillEvent() { }
        public PlayerCheckCanKillEvent(PlayerControl player, PlayerControl target) : base(player) { Target = target; }
    }

    /// <summary>尝试原版击杀时触发（可阻止）。</summary>
    public class PlayerTryVanillaKillEvent : BaseCancelablePlayerEvent
    {
        public PlayerControl Target { get; init; }
        public bool ResetCooldown { get; private set; } = true;
        public void Cancel(bool resetCooldown = false)
        {
            IsCanceled = true;
            ResetCooldown = resetCooldown;
        }
        public PlayerTryVanillaKillEvent() { }
        public PlayerTryVanillaKillEvent(PlayerControl killer, PlayerControl target) : base(killer) { Target = target; }
    }

    /// <summary>玩家被保护免受击杀时触发。</summary>
    public class PlayerGuardEvent : BasePlayerEvent
    {
        public PlayerControl Murderer { get; init; }
        public PlayerGuardEvent() { }
        public PlayerGuardEvent(PlayerControl player, PlayerControl killer) : base(player) { Murderer = killer; }
    }

    // =====================================================================
    //  玩家移动 / 交互
    // =====================================================================

    /// <summary>玩家移动时触发（每帧，高频）。</summary>
    public class PlayerMoveEvent : BasePlayerEvent
    {
        public Vector2 Position { get; init; }
        public PlayerMoveEvent() { }
        public PlayerMoveEvent(PlayerControl player, Vector2 pos) : base(player) { Position = pos; }
    }

    /// <summary>玩家爬梯子时触发。</summary>
    public class PlayerClimbLadderEvent : BasePlayerEvent
    {
        public bool IsClimbingUp { get; init; }
        public Vector2 From { get; init; }
        public Vector2 To { get; init; }
        public PlayerClimbLadderEvent() { }
        public PlayerClimbLadderEvent(PlayerControl player, bool isClimbingUp, Vector2 from, Vector2 to) : base(player)
        { IsClimbingUp = isClimbingUp; From = from; To = to; }
    }

    /// <summary>玩家使用移动平台时触发。</summary>
    public class PlayerUseMovingPlatformEvent : BasePlayerEvent
    {
        public Vector2 From { get; init; }
        public Vector2 To { get; init; }
        public PlayerUseMovingPlatformEvent() { }
        public PlayerUseMovingPlatformEvent(PlayerControl player, Vector2 from, Vector2 to) : base(player) { From = from; To = to; }
    }

    /// <summary>玩家使用滑索时触发。</summary>
    public class PlayerUseZiplineEvent : BasePlayerEvent
    {
        public bool GoesToTop { get; set; }
        public Vector2 From { get; init; }
        public Vector2 To { get; init; }
        public PlayerUseZiplineEvent() { }
        public PlayerUseZiplineEvent(PlayerControl player, bool goesToTop, Vector2 from, Vector2 to) : base(player)
        { GoesToTop = goesToTop; From = from; To = to; }
    }

    /// <summary>玩家通过控制台开始小游戏时触发。</summary>
    public class PlayerBeginMinigameByConsoleEvent : BasePlayerEvent
    {
        public Console Console { get; init; }
        public PlayerBeginMinigameByConsoleEvent() { }
        public PlayerBeginMinigameByConsoleEvent(PlayerControl player, Console console) : base(player) { Console = console; }
    }

    /// <summary>玩家通过门控制台开始小游戏时触发。</summary>
    public class PlayerBeginMinigameByDoorEvent : BasePlayerEvent
    {
        public DoorConsole Door { get; init; }
        public PlayerBeginMinigameByDoorEvent() { }
        public PlayerBeginMinigameByDoorEvent(PlayerControl player, DoorConsole door) : base(player) { Door = door; }
    }

    // =====================================================================
    //  聊天 / 踢出 / 击杀冷却
    // =====================================================================

    /// <summary>聊天消息时触发。</summary>
    public class ChatMessageEvent : BasePlayerEvent
    {
        public string Message { get; init; } = "";
        public ChatMessageEvent() { }
        public ChatMessageEvent(PlayerControl player, string message) : base(player) { Message = message; }
    }

    /// <summary>玩家被踢出时触发。</summary>
    public class PlayerKickEvent : BasePlayerEvent
    {
        public string Reason { get; init; } = "";
        public PlayerControl Kicker { get; init; }
        public PlayerKickEvent() { }
        public PlayerKickEvent(PlayerControl player, PlayerControl kicker, string reason = "") : base(player) { Kicker = kicker; Reason = reason; }
    }

    /// <summary>重置击杀冷却时触发（仅本地）。</summary>
    public class ResetKillCooldownEvent : BasePlayerEvent
    {
        private float? _cooldown;
        public bool UseDefaultCooldown => !_cooldown.HasValue;
        public float? FixedCooldown => _cooldown;
        public void SetFixedCooldown(float cooldown) => _cooldown = cooldown;
        public ResetKillCooldownEvent() { }
        public ResetKillCooldownEvent(PlayerControl player) : base(player) { _cooldown = null; }
    }
}
