using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Light.Patches;

[HarmonyPatch(typeof(HatManager),nameof(HatManager.Initialize))]
public static class CosmicPatch
{
    [HarmonyPostfix]
    public static void HatManagerInit_Postfix(HatManager __instance)
    {
        if (!LightPlugin.LightSettingsData.UnlockAllCosmic) return;
        foreach (var v in __instance.allVisors) v.Free = true;
        foreach (var v in __instance.allStarBundles) v.price = 0;
        foreach (var v in __instance.allHats) v.Free = true;
        foreach (var v in __instance.allPets) v.Free = true;
        foreach (var v in __instance.allSkins) v.Free = true;
        foreach (var v in __instance.allNamePlates) v.Free = true;
        foreach (var v in __instance.allFeaturedBundles) v.Free = true;
        foreach (var v in __instance.allFeaturedCubes) v.Free = true;
        foreach (var v in __instance.allFeaturedItems) v.Free = true;
        foreach (var v in __instance.allBundles) v.Free = true;
    }
}
[HarmonyPatch(typeof(PlayerPurchasesData), nameof(PlayerPurchasesData.GetPurchase))]
public static class PlayerPurchasesData_GetPurchase
{

    public static bool Prefix(PlayerPurchasesData __instance, string itemKey, string bundleKey, ref bool __result)
    {
        if (!LightPlugin.LightSettingsData.UnlockAllCosmic) return true;
        __result = true;
        return false;
    }
}
[HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.FixedUpdate))]
public static class PlayerControl_FixedUpdate
{
    public static void Postfix(PlayerControl __instance)
    {
        if (!LightPlugin.LightSettingsData.DontShowCosmic) return;
        __instance.SetHat("", 0);
        __instance.SetSkin("", 0);
        __instance.SetVisor("", 0);
        __instance.SetNamePlate("");
        __instance.SetPet("", 0);
    }
}