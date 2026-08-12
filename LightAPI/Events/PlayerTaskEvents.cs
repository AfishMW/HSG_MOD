namespace LightInDark.Events
{
    // =====================================================================
    //  任务事件
    // =====================================================================

    /// <summary>玩家完成任务时触发。</summary>
    public class PlayerTaskCompleteEvent : BasePlayerEvent
    {
        public int CompletedTasks { get; init; }
        public int TotalTasks { get; init; }
        public PlayerTaskCompleteEvent() { }
        public PlayerTaskCompleteEvent(PlayerControl player, int completed, int total) : base(player) { CompletedTasks = completed; TotalTasks = total; }
    }

    /// <summary>本地玩家完成任务时触发。</summary>
    public class PlayerTaskCompleteLocalEvent : BasePlayerEvent
    {
        public PlayerTaskCompleteLocalEvent() { }
        public PlayerTaskCompleteLocalEvent(PlayerControl player) : base(player) { }
    }

    /// <summary>所有任务完成时触发（可阻止游戏结束）。</summary>
    public class AllTasksCompleteEvent : BaseCancelableEvent
    {
        public PlayerControl Player { get; init; }
    }

    /// <summary>任务进度更新时触发。</summary>
    public class PlayerTaskUpdateEvent : BasePlayerEvent
    {
        public PlayerTaskUpdateEvent() { }
        public PlayerTaskUpdateEvent(PlayerControl player) : base(player) { }
    }

    /// <summary>玩家获得任务时触发。</summary>
    public class PlayerGetTaskEvent : BasePlayerEvent
    {
        public NormalPlayerTask Task { get; init; }
        public PlayerGetTaskEvent() { }
        public PlayerGetTaskEvent(PlayerControl player, NormalPlayerTask task) : base(player) { Task = task; }
    }

    /// <summary>任务被移除时触发。</summary>
    public class PlayerTaskRemoveEvent : BasePlayerEvent
    {
        public PlayerTask Task { get; init; }
        public PlayerTaskRemoveEvent() { }
        public PlayerTaskRemoveEvent(PlayerControl player, PlayerTask task) : base(player) { Task = task; }
    }
}
