using LightInDark.Ability;
using LightInDark.Configuration;
using LightInDark.Game;
using LightInDark.Roles;
using LightInDark.RPCs;
using Light.UI.Ability;
using UnityEngine;
using System;

namespace Light.Roles.Crewmates;

/// <summary>
/// Caller：可在任意时刻强制召开紧急会议。
/// 所有文案走语言键（Caller.name / Caller.describe / Caller.intro / Caller.skill）。
/// </summary>
public class Caller : DefinedRole
{
    public const string Code = "Caller";

    public Caller() : base(LightInDark.Color.Yellow, RoleCategory.Crewmate) { }

    public override string CodeName => Code;
    public override string IntroBlurbKey => "Caller.intro";
    public override string SkillDescriptionKey => "Caller.skill";

    /// <summary>分配参数：每局必出 1 名 Caller（MaxCount/Chance 可由 .cfg 覆盖）</summary>
    public override AllocationParameters Allocation => new() { MaxCount = 1, GuaranteedCount = 1, Chance = 100};

    /// <summary>自定义配置项示例：会议技能冷却（秒），通过 RoleConfig 形参绑定。</summary>
    public static float Cooldown => RoleConfig.GetFloat(Code, "Cooldown", 20f);

    public override RuntimeRole CreateInstance(Player player, int[] arguments) => new CallerRuntime(this, player, arguments);
}

public class CallerRuntime : RuntimeRole
{
    public CallerRuntime(DefinedRole definition, Player player, int[] arguments) : base(definition, player, arguments) { }

    protected override void OnActivated()
    {
        try
        {
            if (!AmOwner) return;
            var config = new AbilityButtonConfig
            {
                LabelKey = "Button.Caller.label",
                Hotkey = KeyCode.E,
                Cooldown = Caller.Cooldown,
                CanUse = () => MyPlayer.IsDead == false && MyPlayer.Control != null && !MyPlayer.Control.inVent,
                CanShow = () => true,
                IsKillButton = false,
                AlwaysShow = false,
            };
            AbilityButtonManager.Register(this, MyPlayer, config, () =>
            {
                if (!AmOwner) return;
                if (MyPlayer?.Control == null) return;
                RpcDefinitions.RpcStartMeeting();
                LightInDark.Core.LightLogger.Log("[Ability]Caller ability used.");
            });
        }
        catch (Exception ex)
        {
            LightInDark.Core.LightLogger.LogError("[Caller.OnActivated]", ex);
        }
    }
}
