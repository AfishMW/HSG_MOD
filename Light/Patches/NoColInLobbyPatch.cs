using LightInDark.Utilities;

namespace Light.Patches;

[HarmonyPatch(typeof(PlayerControl))]
public static class NoColInLobbyPatch
{
    [HarmonyPostfix]
    [HarmonyPatch(nameof(PlayerControl.FixedUpdate))]
    public static void Postfix(PlayerControl __instance)
    {
            if(!__instance.AmOwner) return; 
            if (!LightUtils.IsInLobby() || !LightUtils.IsCustomServer()) return;
            bool pressShift = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
            if(__instance.Collider.enabled == pressShift)
            {
                __instance.Collider.enabled = !pressShift;
            }
    }
}