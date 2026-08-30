using LightInDark.Configuration;
using LightInDark.Core;
using LightInDark.Game;
using LightInDark.Roles;
using Light.UI.Ability;
using System;

namespace Light.Roles.Crewmates;

/// <summary>
/// Caller（Role 定义侧）：只负责定义角色（静态信息、配置项、分配参数）与事件监听；
/// 实际技能效果全部交给 <see cref="CallerButton"/>（Button 侧）。
/// 配置项用 [RoleOption] 声明，注册时自动扫描并写回此静态属性。
/// </summary>
public class Caller : DefinedRole
{
    public const string Code = "Caller";

    public Caller() : base(LightInDark.Color.Yellow, RoleCategory.Crewmate) { }

    public override string CodeName => Code;
    public override string IntroBlurbKey => "Caller.intro";
    public override string SkillDescriptionKey => "Caller.skill";

    /// <summary>分配参数：每局必出 1 名 Caller（MaxCount/Chance 可由 .cfg 覆盖）。</summary>
    public override AllocationParameters Allocation => new() { MaxCount = 1, GuaranteedCount = 1, Chance = 100 };

    /// <summary>会议技能冷却（秒）。[RoleOption] 自动注册进配置并写回此属性。</summary>
    [RoleOption("Cooldown", 20f, 0f, 120f, "技能冷却")]
    public static float Cooldown { get; set; } = 20f;

    public override RuntimeRole CreateInstance(Player player, int[] arguments)
        => new CallerRuntime(this, player, arguments);
}

/// <summary>Caller 运行时：只负责事件监听与按钮挂载，实际效果在 CallerButton。</summary>
public class CallerRuntime : RuntimeRole
{
    public CallerRuntime(DefinedRole definition, Player player, int[] arguments)
        : base(definition, player, arguments) { }

    protected override void OnActivated()
    {
        try
        {
            if (AmOwner) CallerButton.Create(this);
        }
        catch (Exception ex)
        {
            LightLogger.LogError("[CallerRuntime.OnActivated]", ex);
        }
    }
}
