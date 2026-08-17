namespace LightInDark.Events
{
    // =====================================================================
    //  大厅（Lobby）事件
    //
    //  大厅阶段（房主在大厅界面操作开始/取消/跳过）触发的细分事件。
    //  同类事件在 <see cref="EventTriggers"/> 中统一触发入口。
    // =====================================================================

    /// <summary>
    /// 房主点击“开始”并进入倒计时时触发。
    /// （原版 GameStartManager 进入 Countdown 状态，倒计时正式开始。）
    /// </summary>
    public class LobbyCountdownStartEvent : IEvent
    {
        /// <summary>当前大厅的有效玩家数。</summary>
        public int PlayerCount { get; init; }
        /// <summary>倒计时时长（秒）。</summary>
        public int CountdownDuration { get; init; }
    }

    /// <summary>
    /// 房主点击“开始游戏”时触发（开始倒计时之前）。可取消以阻止开局。
    /// 区别于 <see cref="LobbyCountdownStartEvent"/>：本事件在倒计时真正开始前、仍可撤回。
    /// </summary>
    public class LobbyStartGameEvent : BaseCancelableEvent
    {
        /// <summary>当前大厅的有效玩家数。</summary>
        public int PlayerCount { get; init; }
    }

    /// <summary>
    /// 房主通过模组的“跳过”按钮跳过倒计时、立即开局时触发。
    /// </summary>
    public class LobbySkipCountdownEvent : IEvent
    {
        /// <summary>当前大厅的有效玩家数。</summary>
        public int PlayerCount { get; init; }
    }

    /// <summary>
    /// 房主取消开局/倒计时时触发。
    /// </summary>
    public class LobbyCancelStartEvent : IEvent
    {
        /// <summary>取消时的倒计时剩余秒数（0 表示未在倒计时）。</summary>
        public int RemainingSeconds { get; init; }
    }
}
