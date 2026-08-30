namespace LightInDark.Events
{
    /// <summary>
    /// 场景切换事件（监听 UNITY 的 activeSceneChanged，切换后触发）。
    /// 内部包含切换前后两个场景的名字，可通过 <see cref="PreviousSceneName"/> / <see cref="NextSceneName"/> 拿到。
    /// 进入主菜单可用 <see cref="EnteredMainMenu"/> 快速判断。
    /// </summary>
    public class SceneChangedEvent : IEvent
    {
        /// <summary>切换前的场景名（前一帧活动场景）。</summary>
        public string PreviousSceneName { get; init; } = "";

        /// <summary>切换后的场景名（新活动场景）。</summary>
        public string NextSceneName { get; init; } = "";

        /// <summary>是否进入了主菜单（MainMenu）场景。</summary>
        public bool EnteredMainMenu => NextSceneName == "MainMenu";
    }
}
