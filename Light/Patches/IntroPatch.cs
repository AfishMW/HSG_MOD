using System;
using System.Collections;
using BepInEx.Unity.IL2CPP.Utils.Collections;
using HarmonyLib;
using LightInDark.Core;
using UnityEngine;
using LightGameManager = LightInDark.Game.GameManager;

namespace Light.Patches;

/// <summary>开局播报：将职业说明替换为模组开场白</summary>
[HarmonyPatch(typeof(IntroCutscene), nameof(IntroCutscene.CoBegin))]
public static class IntroPatch
{
    public static void Postfix(IntroCutscene __instance)
    {
        __instance.StartCoroutine(CoOverrideBlurb(__instance).WrapToIl2Cpp());
    }

    private static IEnumerator CoOverrideBlurb(IntroCutscene __instance)
    {
        // 等待角色揭示画面出现（YouAreText 激活），带超时保护防止死等
        float wait = 0f;
        while (__instance != null
            && (__instance.YouAreText == null || !__instance.YouAreText.gameObject.activeSelf)
            && wait < 15f)
        {
            wait += Time.deltaTime;
            yield return null;
        }

        try
        {
            if (__instance == null || __instance.RoleBlurbText == null) yield break;

            var role = LightGameManager.Instance?.LocalPlayer?.Role?.Definition;
            if (role == null || string.IsNullOrEmpty(role.IntroBlurb)) yield break;

            var blurb = __instance.RoleBlurbText;
            blurb.text = role.IntroBlurb;
            blurb.color = LightInDark.ColorHelper.ToUnityColor(role.Color);
            blurb.gameObject.SetActive(true);
        }
        catch (Exception ex)
        {
            LightLogger.LogWarning($"[IntroPatch] 开场白播报异常: {ex.Message}");
        }
    }
}
