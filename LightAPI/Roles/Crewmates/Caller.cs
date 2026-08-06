using LightInDark.Abilities;
using LightInDark.Core;
using LightInDark.Game;
using LightInDark.RPCs;
using LightInDark.UI;
using UnityEngine;

namespace LightInDark.Roles.Crewmates;

public class Caller : DefinedRole
{
    public Caller() : base("Caller", Color.Yellow, Configuration.RoleCategory.Crewmate, "可以在任意时刻强制召开紧急会议。")
    {
    }

    public override RuntimeRole CreateInstance(Player player) => new CallerRuntime(this, player);
}

public class CallerRuntime : RuntimeRole
{
    public CallerRuntime(DefinedRole definition, Player player) : base(definition, player) { }

    protected override void OnActivated()
    {
        if (!AmOwner) return;
        AddAbility(new CallerAbility(this));
    }
}

public class CallerAbility : AbstractPlayerAbility
{
    private LightInDark.UI.AbilityButton? _button;

    public CallerAbility(RuntimeRole role) : base(role)
    {
        // 参考 MiraAPI：通过 AbilityButtonManager.Register 注册
        // 按钮将在 HudManager.Start 时自动创建
        var config = new AbilityButtonConfig
        {
            Label = "开会",
            Icon = null, // 将在创建时设置
            Hotkey = KeyCode.E,
            Cooldown = 20f,
            CanUse = () => MyPlayer.IsDead == false && MyPlayer.Control != null && !MyPlayer.Control.inVent,
            CanShow = () => true,
            IsKillButton = false,
            AlwaysShow = false,
        };

        AbilityButtonManager.Register(Role, MyPlayer, config, OnClick,
            onCreated: button =>
            {
                _button = button;
                // 设置图标（在 HudManager 就绪后）
                if (HudManager.Instance?.KillButton?.graphic?.sprite != null)
                {
                    var killSprite = HudManager.Instance.KillButton.graphic.sprite;
                    var newConfig = new AbilityButtonConfig
                    {
                        Label = "开会",
                        Icon = killSprite,
                        Hotkey = KeyCode.E,
                        Cooldown = 20f,
                        CanUse = () => MyPlayer.IsDead == false && MyPlayer.Control != null && !MyPlayer.Control.inVent,
                        CanShow = () => true,
                        IsKillButton = false,
                        AlwaysShow = false,
                    };
                    button.UpdateConfig(newConfig);
                }
            });
    }

    private void OnClick()
    {
        if (!AmOwner) return;
        if (MyPlayer?.Control == null) return;
        RpcDefinitions.RpcStartMeeting();
        LightLogger.Log("[Ability]Caller ability used.");
    }

    public override void Release()
    {
        _button?.Release();
        base.Release();
    }
}