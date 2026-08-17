using AmongUs.GameOptions;
using LightInDark;
using LightInDark.Configuration;
using LightInDark.Game;
using LightInDark.Roles;
using LightInDark.Core;

namespace Light.Roles.Vanilla;

/// <summary>默认内鬼职业：未分配自定义职业时的默认占位（不参与自定义分配）。</summary>
public class VanillaImpostor : DefinedRole
{
    public static readonly VanillaImpostor Instance = new();
    public VanillaImpostor() : base(LightInDark.Color.Red, RoleCategory.Impostor) { }

    public override string CodeName => "VanillaImpostor";
    public override string IntroBlurbKey => "Role.VanillaImpostor.intro";
    public override string SkillDescriptionKey => "Role.VanillaImpostor.skill";

    public override RuntimeRole CreateInstance(Player player, int[] arguments) => new VanillaImpostorRuntime(this, player, arguments);
}

public class VanillaImpostorRuntime : RuntimeRole
{
    public VanillaImpostorRuntime(DefinedRole definition, Player player, int[] arguments) : base(definition, player, arguments) { }
    protected override void OnActivated()
    {
        try
        {
            if (MyPlayer.Control != null)
                RoleManager.Instance.SetRole(MyPlayer.Control, RoleTypes.Impostor);
        }
        catch (Exception ex)
        {
            LightLogger.LogError("[VanillaRoles.OnActivated]", ex);
        }
    }
}

/// <summary>默认船员职业：未分配自定义职业时的默认占位（不参与自定义分配）。</summary>
public class VanillaCrewmate : DefinedRole
{
    public static readonly VanillaCrewmate Instance = new();
    public VanillaCrewmate() : base(LightInDark.Color.Green, RoleCategory.Crewmate) { }

    public override string CodeName => "VanillaCrewmate";
    public override string IntroBlurbKey => "Role.VanillaCrewmate.intro";
    public override string SkillDescriptionKey => "Role.VanillaCrewmate.skill";

    public override RuntimeRole CreateInstance(Player player, int[] arguments) => new VanillaCrewmateRuntime(this, player, arguments);
}

public class VanillaCrewmateRuntime : RuntimeRole
{
    public VanillaCrewmateRuntime(DefinedRole definition, Player player, int[] arguments) : base(definition, player, arguments) { }
    protected override void OnActivated()
    {
        try
        {
            if (MyPlayer.Control != null)
                RoleManager.Instance.SetRole(MyPlayer.Control, RoleTypes.Crewmate);
        }
        catch (Exception ex)
        {
            LightLogger.LogError("[VanillaRoles.OnActivated]", ex);
        }
    }
}
