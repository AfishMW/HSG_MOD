using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using LightInDark.Core;
using LightInDark.Utilities;
using UnityEngine;

namespace Light.Patches;

[HarmonyPatch(typeof(PlayerControl))]
public static class NoColInLobbyPatch
{
    [HarmonyPostfix]
    [HarmonyPatch(nameof(PlayerControl.FixedUpdate))]
    public static void Postfix(PlayerControl __instance)
    {
        try
        {
            if(!__instance.AmOwner) return; ;
            if (!AmongUsEdited.IsInLobby() || !AmongUsEdited.IsCustomServer()) return;
            bool pressShift = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
            if(__instance.Collider.enabled == pressShift)
            {
                __instance.Collider.enabled = !pressShift;
            }
        }
        catch (System.Exception ex)
        {
            LightLogger.LogWarning("[Light] NoColInLobbyPatch.Postfix NRE: " + ex.Message + "\n" + ex.StackTrace);
        }
    }
}