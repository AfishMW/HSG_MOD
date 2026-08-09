using HarmonyLib;
using UnityEngine;

namespace Light.Patches;

[HarmonyPatch(typeof(LobbyBehaviour), nameof(LobbyBehaviour.Update))]
public static class DisableLobbyEnginePowerPatch
{
    public static void Postfix(LobbyBehaviour __instance)
    {
        if (Input.GetKeyDown(KeyCode.LeftControl))
        {
            var LeftEngine = __instance.transform.GetChild(2);
            LeftEngine.gameObject.SetActive(!LeftEngine.gameObject.activeSelf);
        }
        if(Input.GetKeyDown(KeyCode.RightControl))
        {
            var RightEngine = __instance.transform.GetChild(1);
            RightEngine.gameObject.SetActive(!RightEngine.gameObject.activeSelf);
        }
    }
}