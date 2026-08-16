using HarmonyLib;
using LightInDark;
using LightInDark.Core;
using System;

namespace Light.Patches;

public static class PlayerColorHelper
{
    public static UnityEngine.Color GetPlayerColor(byte playerId)
    {
        try
        {
            foreach (var pc in PlayerControl.AllPlayerControls)
                if (pc.PlayerId == playerId) return GetPlayerColor(pc);
        }
        catch (Exception ex) { LightLogger.LogError($"[PlayerColorHelper.GetPlayerColor] playerId={playerId}", ex); }
        return UnityEngine.Color.white;
    }

    public static UnityEngine.Color GetPlayerColor(PlayerControl pc)
    {
        try
        {
            if (pc == null || pc.Data == null) return UnityEngine.Color.white;
            var colorId = pc.Data.DefaultOutfit.ColorId;
            if (colorId >= 0 && colorId < Palette.PlayerColors.Length)
            {
                var uc = Palette.PlayerColors[colorId];
                LightLogger.Log($"[PlayerColorHelper] playerId={pc.PlayerId} colorId={colorId} color=({uc.r:F2},{uc.g:F2},{uc.b:F2})");
                return uc;
            }
        }
        catch (Exception ex) { LightLogger.LogError("[PlayerColorHelper.GetPlayerColor(PlayerControl)]", ex); }
        return UnityEngine.Color.white;
    }

    public static UnityEngine.Color GetPlayerColor(NetworkedPlayerInfo info)
    {
        try
        {
            if (info == null) return UnityEngine.Color.white;
            var colorId = info.DefaultOutfit.ColorId;
            if (colorId >= 0 && colorId < Palette.PlayerColors.Length)
            {
                var uc = Palette.PlayerColors[colorId];
                LightLogger.Log($"[PlayerColorHelper] info colorId={colorId} color=({uc.r:F2},{uc.g:F2},{uc.b:F2})");
                return uc;
            }
        }
        catch (Exception ex) { LightLogger.LogError("[PlayerColorHelper.GetPlayerColor(NetworkedPlayerInfo)]", ex); }
        return UnityEngine.Color.white;
    }
}

/// <summary>
/// 聊天颜色解析器：气泡和背景共用同一套逻辑。
/// ChatFollowPlayerColor=true  → 玩家颜色
/// ChatFollowPlayerColor=false → CustomChatColorOverride → ModMainColor → 抛异常
/// </summary>
public static class ChatColorResolver
{
    public static UnityEngine.Color ResolveColor(NetworkedPlayerInfo? info = null, float alpha = 0.8f)
    {
        UnityEngine.Color color;
        try
        {
            if (LightPlugin.ColorData.ChatFollowPlayerColor)
            {
                if (info != null)
                    color = PlayerColorHelper.GetPlayerColor(info);
                else
                    color = PlayerColorHelper.GetPlayerColor(PlayerControl.LocalPlayer);
            }
            else
            {
                var custom = LightPlugin.ColorData.CustomChatFiledColor;
                if (custom.HasValue)
                {
                    color = custom.Value.ToUnityColor();
                }
                else
                {
                    var main = LightPlugin.ColorData.modMainColor;
                    if (IsColorValid(main))
                    {
                        color = main.ToUnityColor();
                        throw new InvalidColorTypeException(
                            $"CustomChatColorOverride '{LightPlugin.ColorData.CustomChatColorOverride}' 无效，已回退到 ModMainColorARGB。");
                    }
                    else
                    {
                        throw new InvalidColorTypeException(
                            "CustomChatColorOverride 和 ModMainColorARGB 均无效，无法设置聊天颜色。");
                    }
                }
            }
        }
        catch (InvalidColorTypeException ex)
        {
            LightLogger.LogError("[ChatColorResolver] 颜色配置异常", ex);
            color = LightPlugin.ColorData.modMainColor.ToUnityColor();
        }

        color.a = alpha;
        return color;
    }

    private static bool IsColorValid(LightInDark.Color c)
    {
        return c.A > 0f && (c.R > 0f || c.G > 0f || c.B > 0f);
    }
}

[HarmonyPatch(typeof(ChatBubble), nameof(ChatBubble.SetText))]
public static class ChatBubbleColorPatch
{
    [HarmonyPrefix]
    public static void SetText_Prefix(ChatBubble __instance)
    {
        try
        {
            if (LightPlugin.ColorData == null || LightPlugin.ColorData.IsVanillaMode) return;
            if (__instance == null || __instance.Background == null) return;

            var bubbleColor = ChatColorResolver.ResolveColor(__instance.playerInfo, alpha: 0.8f);
            __instance.Background.color = bubbleColor;
        }
        catch (Exception ex)
        {
            LightLogger.LogError("[ChatBubbleColorPatch.SetText_Prefix]", ex);
        }
    }
}

[HarmonyPatch(typeof(ChatController), nameof(ChatController.Update))]
public static class ChatControllerColorPatch
{
    public static void Postfix(ChatController __instance)
    {
        try
        {
            if (__instance == null) return;
            if (LightPlugin.ColorData == null || LightPlugin.ColorData.IsVanillaMode) return;

            // 聊天面板背景 — 同一套颜色逻辑，alpha 0.6
            var panelColor = ChatColorResolver.ResolveColor(alpha: 0.6f);

            var chatScreen = __instance.chatScreen;
            if (chatScreen != null)
            {
                var bg = chatScreen.transform.FindChild("ChatScreenContainer")?.FindChild("Background");
                if (bg != null)
                {
                    var sr = bg.GetComponent<SpriteRenderer>();
                    if (sr != null) sr.color = panelColor;
                }
            }

            // 输入框背景 — 同一套颜色逻辑，alpha 0.6
            var inputColor = ChatColorResolver.ResolveColor(alpha: 0.6f);
            if (__instance.freeChatField?.background != null)
                __instance.freeChatField.background.color = inputColor;
            if (__instance.quickChatField?.background != null)
                __instance.quickChatField.background.color = inputColor;
        }
        catch { }
    }
}

[HarmonyPatch(typeof(ButtonRolloverHandler))]
internal class ButtonRolloverHandlerPatch
{
    private static readonly UnityEngine.Color PureGreen = new(0f, 1f, 0f, 1f);

    private static bool IsGreen(UnityEngine.Color c)
    {
        return c == PureGreen
            || (Mathf.Approximately(c.r, 0f)
                && Mathf.Approximately(c.g, 1f)
                && c.b is > 0.16f and < 0.17f
                && Mathf.Approximately(c.a, 1f));
    }

    [HarmonyPatch(nameof(ButtonRolloverHandler.DoMouseOver))]
    [HarmonyPrefix]
    public static void DoMouseOver_Prefix(ButtonRolloverHandler __instance)
    {
        if (LightPlugin.ColorData == null || LightPlugin.ColorData.IsVanillaMode) return;
        if (IsGreen(__instance.OverColor))
            __instance.OverColor = LightPlugin.ColorData.modMainColor.ToUnityColor();
    }

    [HarmonyPatch(nameof(ButtonRolloverHandler.DoMouseOut))]
    [HarmonyPrefix]
    public static void DoMouseOut_Prefix(ButtonRolloverHandler __instance)
    {
        if (LightPlugin.ColorData == null || LightPlugin.ColorData.IsVanillaMode) return;
        if (IsGreen(__instance.OutColor))
            __instance.OutColor = LightPlugin.ColorData.modMainColor.ToUnityColor();
    }

    [HarmonyPatch(nameof(ButtonRolloverHandler.ChangeOutColor))]
    [HarmonyPrefix]
    public static void ChangeOutColor_Prefix(ref UnityEngine.Color color)
    {
        if (LightPlugin.ColorData == null || LightPlugin.ColorData.IsVanillaMode) return;
        if (IsGreen(color))
            color = LightPlugin.ColorData.modMainColor.ToUnityColor();
    }
}

[HarmonyPatch(typeof(Palette))]
internal class PalettePatch
{
    [HarmonyPatch(nameof(Palette.AcceptedGreen), MethodType.Getter)]
    [HarmonyPrefix]
    public static bool GetAcceptedGreen_Prefix(ref UnityEngine.Color __result)
    {
        if (LightPlugin.ColorData == null || LightPlugin.ColorData.IsVanillaMode) return true;
        var c = LightPlugin.ColorData.modMainColor.ToUnityColor();
        __result = new Color32((byte)(c.r * 255), (byte)(c.g * 255), (byte)(c.b * 255), (byte)(c.a * 255));
        return false;
    }
}
