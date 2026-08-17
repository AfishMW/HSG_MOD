namespace LightInDark.Events
{
    // =====================================================================
    //  修饰器（Modifier）事件
    // =====================================================================

    /// <summary>修饰器被添加到玩家时触发。</summary>
    public class ModifierAddedEvent : BasePlayerEvent
    {
        public Modifiers.Modifier Modifier { get; init; }
        public ModifierAddedEvent() { }
        public ModifierAddedEvent(PlayerControl player, Modifiers.Modifier modifier) : base(player) { Modifier = modifier; }
    }

    /// <summary>修饰器从玩家移除时触发。</summary>
    public class ModifierRemovedEvent : BasePlayerEvent
    {
        public Modifiers.Modifier Modifier { get; init; }
        public ModifierRemovedEvent() { }
        public ModifierRemovedEvent(PlayerControl player, Modifiers.Modifier modifier) : base(player) { Modifier = modifier; }
    }
}
