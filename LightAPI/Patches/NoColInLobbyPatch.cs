using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using LightInDark.Utilities;
using UnityEngine;

namespace LightInDark.Patches;

[HarmonyPatch(typeof(PlayerControl))]
public static class NoColInLobbyPatch
{
    [HarmonyPostfix]
    [HarmonyPatch(nameof(PlayerControl.FixedUpdate))]
    public static void Postfix(PlayerControl __instance)
    {
        if(!__instance.AmOwner) return; ;
        if (!AmongUsEdited.IsInLobby() || !AmongUsEdited.IsCustomServer()) return;
        bool pressShift = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
        if(__instance.Collider.enabled == pressShift)
        {
            __instance.Collider.enabled = !pressShift;
        }
    }
}