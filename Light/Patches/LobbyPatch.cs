using HarmonyLib;
using LightInDark.Core;
using Light.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Light.Patches;

[HarmonyPatch(typeof(LobbyBehaviour), nameof(LobbyBehaviour.Start))]
public static class AddLobbyDecorations
{
    public static void Postfix(LobbyBehaviour __instance)
    {
        try
        {
            var sprite = ResourceHelper.LoadSpriteFromResource("Light.Resources.Lobby.LightInDark.png");

            GameObject decor = new GameObject("MyLobbyDecor");
            decor.transform.SetParent(__instance.transform);
            decor.transform.localPosition = new Vector3(0f, 3.5f, 0f);
            decor.transform.localScale = new Vector3(0.4f, 0.4f, 0.5f);

            var renderer = decor.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
        }
        catch (System.Exception ex)
        {
            LightLogger.LogWarning("[Light] AddLobbyDecorations.Postfix NRE: " + ex.Message + "\n" + ex.StackTrace);
        }
    }
}