using AmongUs.GameOptions;
using HarmonyLib;
using LightInDark.RPCs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LightInDark.Role.Crewmate;
public class GetEmer : RoleBehaviour
{
    public const RoleTypes RoleId = (RoleTypes)100;
    public override void Initialize(PlayerControl player)
    {
        base.Initialize(player);
        Role = RoleId;
        TeamType = RoleTeamTypes.Crewmate;
        StringName = (StringNames)2163;
    }
    public override void OnMeetingStart()
    {
        base.OnMeetingStart();
        RPC.Suicide(PlayerControl.LocalPlayer,true,"GetEmer死亡，日志");
    }
}
[HarmonyPatch(typeof(RoleBehaviour), nameof(RoleBehaviour.NameColor), MethodType.Getter)]
public static class RoleBehaviour_NameColor_Patch
{
    public static void Postfix(RoleBehaviour __instance, ref Color __result)
    {
        if (__instance is GetEmer)
            __result = Color.Yellow;
    }
}

[HarmonyPatch(typeof(RoleBehaviour), nameof(RoleBehaviour.TeamColor), MethodType.Getter)]
public static class RoleBehaviour_TeamColor_Patch
{
    public static void Postfix(RoleBehaviour __instance, ref Color __result)
    {
        if (__instance is GetEmer)
            __result = Color.Yellow;
    }
}

[HarmonyPatch(typeof(RoleBehaviour), nameof(RoleBehaviour.NiceName), MethodType.Getter)]
public static class RoleBehaviour_NiceName_Patch
{
    public static void Postfix(RoleBehaviour __instance, ref string __result)
    {
        if (__instance is GetEmer)
            __result = "执钮";
    }
}

[HarmonyPatch(typeof(RoleBehaviour), nameof(RoleBehaviour.IsImpostor), MethodType.Getter)]
public static class RoleBehaviour_IsImpostor_Patch
{
    public static void Postfix(RoleBehaviour __instance, ref bool __result)
    {
        if (__instance is GetEmer)
            __result = false;
    }
}