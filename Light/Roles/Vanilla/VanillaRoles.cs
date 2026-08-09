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
