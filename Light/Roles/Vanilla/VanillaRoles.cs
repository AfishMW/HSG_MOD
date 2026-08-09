using AmongUs.GameOptions;
using LightInDark;
using LightInDark.Configuration;
using LightInDark.Game;
using LightInDark.Roles;
using Color = LightInDark.Color;

namespace Light.Roles.Vanilla;

/// <summary>默认内鬼职业：未分配自定义职业时的默认占位</summary>
public class VanillaImpostor : DefinedRole
{
    public static readonly VanillaImpostor Instance = new();
    public VanillaImpostor() : base("内鬼", Color.Red, RoleCategory.Impostor, "基础内鬼职业，未分配自定义职业时的默认占位") { }

    /// <summary>开场白：分配职业播报时显示在职业名底部</summary>
    public override string IntroBlurb => "暗中潜伏，伺机而动。";

    /// <summary>技能介绍：帮助详情立绘下方，多行用 \n 分隔</summary>
    public override string SkillDescription => "击杀船员、破坏设施、钻管道。\n无模组专属技能。";

    public override RuntimeRole CreateInstance(Player player, int[] arguments) => new VanillaImpostorRuntime(this, player, arguments);
}

public class VanillaImpostorRuntime : RuntimeRole
{
    public VanillaImpostorRuntime(DefinedRole definition, Player player, int[] arguments) : base(definition, player, arguments) { }
    protected override void OnActivated()
    {
        if (MyPlayer.Control != null)
            RoleManager.Instance.SetRole(MyPlayer.Control, RoleTypes.Impostor);
    }
}

/// <summary>默认船员职业：未分配自定义职业时的默认占位</summary>
public class VanillaCrewmate : DefinedRole
{
    public static readonly VanillaCrewmate Instance = new();
    public VanillaCrewmate() : base("船员", Color.Green, RoleCategory.Crewmate, "基础船员职业，未分配自定义职业时的默认占位") { }

    /// <summary>开场白：分配职业播报时显示在职业名底部</summary>
    public override string IntroBlurb => "完成任务，揪出内鬼。";

    /// <summary>技能介绍：帮助详情立绘下方，多行用 \n 分隔</summary>
    public override string SkillDescription => "完成所有任务，发现并投票放逐内鬼。\n无模组专属技能。";

    public override RuntimeRole CreateInstance(Player player, int[] arguments) => new VanillaCrewmateRuntime(this, player, arguments);
}

public class VanillaCrewmateRuntime : RuntimeRole
{
    public VanillaCrewmateRuntime(DefinedRole definition, Player player, int[] arguments) : base(definition, player, arguments) { }
    protected override void OnActivated()
    {
        if (MyPlayer.Control != null)
            RoleManager.Instance.SetRole(MyPlayer.Control, RoleTypes.Crewmate);
    }
}
