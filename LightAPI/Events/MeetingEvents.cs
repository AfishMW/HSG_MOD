namespace LightInDark.Events
{
    // =====================================================================
    //  会议事件
    // =====================================================================

    /// <summary>尝试召开会议时触发（可阻止）。</summary>
    public class MeetingTryStartEvent : BaseCancelableEvent
    {
        public PlayerControl Reporter { get; init; }
        public NetworkedPlayerInfo ReportedBody { get; init; }
        public bool IsEmergencyMeeting { get; init; }
    }

    /// <summary>会议预开始时触发（报告/紧急召集的瞬间）。</summary>
    public class MeetingPreStartEvent : BasePlayerEvent
    {
        public PlayerControl Reported { get; init; }
        public PlayerControl Reporter => Player;
        public MeetingPreStartEvent() { }
        public MeetingPreStartEvent(PlayerControl reporter, PlayerControl reported = null) : base(reporter) { Reported = reported; }
    }

    /// <summary>报告尸体时触发。</summary>
    public class ReportDeadBodyEvent : MeetingPreStartEvent
    {
        public ReportDeadBodyEvent() { }
        public ReportDeadBodyEvent(PlayerControl reporter, PlayerControl reported = null) : base(reporter, reported) { }
    }

    /// <summary>紧急会议召开时触发。</summary>
    public class CalledEmergencyMeetingEvent : MeetingPreStartEvent
    {
        public CalledEmergencyMeetingEvent() { }
        public CalledEmergencyMeetingEvent(PlayerControl reporter) : base(reporter) { }
    }

    /// <summary>检查能否按紧急按钮时触发（可阻止）。</summary>
    public class CheckCanPushEmergencyButtonEvent : BaseCancelableEvent
    {
        public bool CanPushButton { get; private set; } = true;
        public string CannotPushReason { get; private set; }
        public void DenyButton(string reason = null)
        {
            CanPushButton = false;
            CannotPushReason ??= reason;
        }
    }

    /// <summary>会议开始时触发。</summary>
    public class MeetingStartEvent : IEvent
    {
        public PlayerControl Reporter { get; init; }
        public NetworkedPlayerInfo ReportedBody { get; init; }
        public bool IsEmergencyMeeting { get; init; }
        public bool CanVote { get; set; } = true;
    }

    /// <summary>讨论阶段开始时触发。</summary>
    public class MeetingDiscussionStartEvent : IEvent { }

    /// <summary>投票阶段开始时触发。</summary>
    public class MeetingVotingStartEvent : IEvent { }

    /// <summary>玩家投票时触发。可修改投票权重。</summary>
    public class PlayerVoteEvent : BasePlayerEvent
    {
        public byte VotedForPlayerId { get; set; }
        public int VoteWeight { get; set; } = 1;
        public PlayerVoteEvent() { }
        public PlayerVoteEvent(PlayerControl player, byte votedFor) : base(player) { VotedForPlayerId = votedFor; }
    }

    /// <summary>本地玩家投票提交时触发。</summary>
    public class PlayerVoteCastEvent : BasePlayerEvent
    {
        public int Vote { get; init; } = 1;
        public PlayerControl VoteFor { get; init; }
        public PlayerControl Voter => Player;
        public PlayerVoteCastEvent() { }
        public PlayerVoteCastEvent(PlayerControl voter, PlayerControl voteFor = null, int vote = 1) : base(voter) { VoteFor = voteFor; Vote = vote; }
    }

    /// <summary>本地玩家已投票时触发。</summary>
    public class PlayerVotedEvent : BasePlayerEvent
    {
        public System.Collections.Generic.IReadOnlyList<PlayerControl> Voters { get; init; }
        public PlayerVotedEvent() { }
        public PlayerVotedEvent(PlayerControl player, System.Collections.Generic.IReadOnlyList<PlayerControl> voters) : base(player) { Voters = voters; }
    }

    /// <summary>尝试结束投票时触发（可修改谁被放逐）。</summary>
    public class MeetingTryEndVotingEvent : BaseCancelableEvent
    {
        public byte ExiledPlayerId { get; set; }
        public bool IsTie { get; set; }
    }

    /// <summary>投票阶段结束时触发。</summary>
    public class MeetingVoteEndEvent : IEvent
    {
        public MeetingHud.VoterState[] VoteStates { get; init; }
    }

    /// <summary>投票公开时触发。</summary>
    public class MeetingVoteDisclosedEvent : IEvent
    {
        public MeetingHud.VoterState[] VoteStates { get; init; }
    }

    /// <summary>会议即将结束前触发（可阻止）。</summary>
    public class MeetingTryEndEvent : BaseCancelableEvent { }

    /// <summary>会议预结束时触发（放逐已执行但协程未完成）。</summary>
    public class MeetingPreEndEvent : IEvent { }

    /// <summary>会议结束时触发。</summary>
    public class MeetingEndEvent : IEvent
    {
        public byte ExiledPlayerId { get; init; }
        public bool WasTie { get; init; }
    }

    /// <summary>放逐场景预开始时触发。</summary>
    public class ExileScenePreStartEvent : IEvent
    {
        public System.Collections.Generic.IReadOnlyList<PlayerControl> Exiled { get; init; }
    }

    /// <summary>放逐场景开始时触发。</summary>
    public class ExileSceneStartEvent : IEvent
    {
        public System.Collections.Generic.IReadOnlyList<PlayerControl> Exiled { get; init; }
    }

    /// <summary>修正放逐文本时触发。可追加放逐画面显示的文本。</summary>
    public class FixExileTextEvent : IEvent
    {
        public System.Collections.Generic.IReadOnlyList<PlayerControl> Exiled { get; init; }
        private readonly System.Collections.Generic.List<string> _texts = new();
        public void AddText(string text) => _texts.Add(text);
        public System.Collections.Generic.IReadOnlyList<string> GetTexts() => _texts;
    }

    // =====================================================================
    //  放逐事件
    // =====================================================================

    /// <summary>放逐即将执行时触发（可修改谁被放逐）。</summary>
    public class PlayerTryExileEvent : BaseCancelableEvent
    {
        public byte ExilePlayerId { get; set; }
        public bool IsTie { get; set; }
    }

    /// <summary>放逐动画执行中触发（只读）。</summary>
    public class PlayerExileEvent : IEvent
    {
        public PlayerControl Exiled { get; init; }
        public Game.PlayerState State { get; init; } = Game.PlayerState.Exile;
        public PlayerExileEvent() { }
        public PlayerExileEvent(PlayerControl exiled, Game.PlayerState state = Game.PlayerState.Exile) { Exiled = exiled; State = state; }
    }
}
