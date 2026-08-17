using UnityEngine;

namespace LightInDark.Events
{
    // =====================================================================
    //  深度生命周期事件（Nebula 风格：深度拦截原版，注入自定义逻辑）
    //
    //  这些事件在游戏最底层的原版生命周期方法上触发：
    //    - CoShowIntro / CoStartGameHost / GameManager.StartGame
    //    - RoleManager.SelectRoles / ShipStatus.Begin
    //  监听者可通过 [OnlyHost] / [OnlyMyPlayer] / [Local] / [EventPriority] 过滤。
    // =====================================================================

    /// <summary>主机收到开局指令、即将加载船只（AmongUsClient.CoStartGameHost 开始时）。</summary>
    public class GameLoadingStartEvent : IEvent
    {
        /// <summary>本局地图 Id。</summary>
        public int MapId { get; init; }
    }

    /// <summary>开场动画（IntroCutscene）即将开始（HudManager.CoShowIntro 将实例化 IntroCutscene）。</summary>
    public class IntroBeginEvent : IEvent { }

    /// <summary>开场动画结束、原版 GameManager.StartGame() 即将调用（CoShowIntro 末尾）。</summary>
    public class IntroEndEvent : IEvent { }

    /// <summary>原版角色分配（RoleManager.SelectRoles）即将执行。</summary>
    public class RoleSelectionBeginEvent : IEvent
    {
        /// <summary>参与本局且存活的玩家数量。</summary>
        public int PlayerCount { get; init; }
    }

    /// <summary>原版船只任务分配（ShipStatus.Begin）即将执行。</summary>
    public class ShipBeginEvent : IEvent { }

    /// <summary>船只加载完成、所有客户端就绪、原版 SelectRoles 前一刻。</summary>
    public class PlayersSpawnedEvent : IEvent { }

    /// <summary>一局正式结束（GameManager.EndGame）时触发，可在结局结算前注入自定义收尾。</summary>
    public class GamePreEndEvent : IEvent
    {
        public bool CrewmatesWin { get; init; }
        public bool ImpostorsWin { get; init; }
        public string Reason { get; init; } = "";
    }
}
