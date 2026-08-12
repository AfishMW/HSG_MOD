using UnityEngine;

namespace LightInDark.Events
{
    // =====================================================================
    //  玩家视觉 / 外观事件
    // =====================================================================

    /// <summary>玩家透明度更新时触发。</summary>
    public class PlayerAlphaUpdateEvent : BasePlayerEvent
    {
        public float Alpha { get; set; }
        public float AlphaIgnoresWall { get; set; }
        public PlayerAlphaUpdateEvent() { }
        public PlayerAlphaUpdateEvent(PlayerControl player, float alpha, float alphaIgnoresWall = 0f) : base(player)
        { Alpha = alpha; AlphaIgnoresWall = alphaIgnoresWall; }
    }

    /// <summary>
    /// 玩家可见性更新时触发（每帧）。
    /// 灵界视角下即使设为不可见也能半透明看到。
    /// </summary>
    public class PlayerUpdateVisibilityEvent : BasePlayerEvent
    {
        public VisibilityLevel Visibility { get; set; }
        public VisibilityLevel LastVisibility { get; init; }

        public enum VisibilityLevel
        {
            Visible = 0,
            SemiTransparent = 1,
            Invisible = 2
        }

        public void SetVisible() => Visibility = VisibilityLevel.Visible;
        public void SetSemitransparent() => Visibility = VisibilityLevel.SemiTransparent;
        public void SetInvisible() => Visibility = VisibilityLevel.Invisible;

        public PlayerUpdateVisibilityEvent() { }
        public PlayerUpdateVisibilityEvent(PlayerControl player, VisibilityLevel visibility, VisibilityLevel lastVisibility) : base(player)
        { Visibility = visibility; LastVisibility = lastVisibility; }
    }

    /// <summary>玩家名字装饰时触发。可修改显示名字和颜色。</summary>
    public class PlayerDecorateNameEvent : BasePlayerEvent
    {
        public string Name { get; set; } = "";
        public Color? NameColor { get; set; }
        public bool CanSeeAllInfo { get; init; }
        public PlayerDecorateNameEvent() { }
        public PlayerDecorateNameEvent(PlayerControl player, string name, bool canSeeAllInfo = false) : base(player)
        { Name = name; CanSeeAllInfo = canSeeAllInfo; }
    }

    /// <summary>检查是否播放脚步声时触发。</summary>
    public class PlayerCheckPlayFootSoundEvent : BasePlayerEvent
    {
        public bool PlayFootSound { get; set; } = true;
        public PlayerCheckPlayFootSoundEvent() { }
        public PlayerCheckPlayFootSoundEvent(PlayerControl player) : base(player) { }
    }

    /// <summary>管道可用性更新时触发（仅本地）。</summary>
    public class PlayerUpdateVentStateEvent : BasePlayerEvent
    {
        public bool CanUseVent { get; set; }
        public bool CannotUseVentTemporary { get; set; }
        public bool ShouldShowVentButton => CanUseVent;
        public bool CanUseVentButton => CanUseVent && !CannotUseVentTemporary;
        public PlayerUpdateVentStateEvent() { }
        public PlayerUpdateVentStateEvent(PlayerControl player) : base(player) { CanUseVent = true; CannotUseVentTemporary = false; }
    }
}
