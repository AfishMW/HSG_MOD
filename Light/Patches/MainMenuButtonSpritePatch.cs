using System;
using HarmonyLib;
using Light.Utilities;
using LightInDark.Core;
using UnityEngine;

namespace Light.Patches;

/// <summary>
/// 更换主界面左侧按钮样式：把六张自定义按钮图裁剪掉四周透明留白后替换原版贴图，
/// 并修正点击碰撞盒，保持按钮条位置与原始宽高比。
/// </summary>
[HarmonyPatch(typeof(MainMenuManager))]
public static class MainMenuButtonSpritePatch
{
    private const byte AlphaThreshold = 8;

    /// <summary>文件名与 MainMenuManager 字段选择器一一对应。</summary>
    private static readonly (string File, Func<MainMenuManager, PassiveButton> Pick)[] Buttons =
    [
        ("Buttons/MainMenu/1787250791214.png", m => m.playButton),
        ("Buttons/MainMenu/1787250799180.png", m => m.inventoryButton),
        ("Buttons/MainMenu/1787250815120.png", m => m.shopButton),
        ("Buttons/MainMenu/1787250837638.png", m => m.newsButton),
        ("Buttons/MainMenu/1787250844759.png", m => m.myAccountButton),
        ("Buttons/MainMenu/1787250853178.png", m => m.settingsButton),
        ("Buttons/MainMenu/1787149768081.png", m => m.creditsButton),
        ("Buttons/MainMenu/1787149768081.png", m => m.quitButton),
    ];

    [HarmonyPatch(nameof(MainMenuManager.Start))]
    [HarmonyPostfix]
    public static void StartPostfix(MainMenuManager __instance)
    {
        try
        {
            foreach (var (file, pick) in Buttons)
            {
                try
                {
                    var btn = pick(__instance);
                    if (btn == null)
                    {
                        LightLogger.LogWarning($"[MainMenuButtonSprite] 按钮为空，跳过 {file}");
                        continue;
                    }
                    ReplaceButton(btn, file);
                }
                catch (Exception ex)
                {
                    LightLogger.LogError($"[MainMenuButtonSprite] 替换失败 {file}", ex);
                }
            }
        }
        catch (Exception ex)
        {
            LightLogger.LogError("[MainMenuButtonSprite.StartPostfix]", ex);
        }
    }

    private static void ReplaceButton(PassiveButton btn, string relativePath)
    {
        var texture = ResourceHelper.LoadTexture(relativePath);
        if (texture == null)
        {
            LightLogger.LogWarning($"[MainMenuButtonSprite] 加载纹理失败 {relativePath}");
            return;
        }

        if (!FindOpaqueBounds(texture, AlphaThreshold, out int minX, out int minY, out int width, out int height))
        {
            LightLogger.LogWarning($"[MainMenuButtonSprite] 未检测到非透明区域，使用整图 {relativePath}");
            minX = 0;
            minY = 0;
            width = texture.width;
            height = texture.height;
        }

        var activeSr = btn.activeSprites != null ? btn.activeSprites.GetComponent<SpriteRenderer>() : null;
        if (activeSr == null)
        {
            LightLogger.LogWarning($"[MainMenuButtonSprite] 未找到 activeSprites 渲染器 {relativePath}");
            return;
        }
        var inactiveSr = btn.inactiveSprites != null ? btn.inactiveSprites.GetComponent<SpriteRenderer>() : null;

        var oldSpr = activeSr.sprite;
        float basePPU = oldSpr != null ? oldSpr.pixelsPerUnit : 100f;

        // 沿用原按钮锚点语义：用原 sprite 归一化 pivot 作为新裁剪 sprite 的 pivot，
        // 使按钮条中心对准原按钮中心，避免整体偏移（原锚点默认在中心 0.5）
        var pivot = oldSpr != null && oldSpr.rect.width > 0f && oldSpr.rect.height > 0f
            ? new Vector2(oldSpr.pivot.x / oldSpr.rect.width, oldSpr.pivot.y / oldSpr.rect.height)
            : new Vector2(0.5f, 0.5f);

        // 以原按钮宽度换算 PPU，让裁剪后的按钮条与原按钮同宽（保持图片宽高比）
        float ppu = basePPU;
        if (oldSpr != null && oldSpr.bounds.size.x > 0f)
            ppu = width / oldSpr.bounds.size.x;

        var sprite = Sprite.Create(texture, new Rect(minX, minY, width, height), pivot, ppu);

        activeSr.sprite = sprite;
        if (inactiveSr != null) inactiveSr.sprite = sprite;

        LightLogger.Log($"[MainMenuButtonSprite] {relativePath} 裁剪 {minX},{minY} {width}x{height} PPU={ppu}");

        FixCollider(btn, activeSr);
        HideDecorations(btn);
        SetButtonTextColor(btn);
    }

    /// <summary>检测非透明像素的最小/最大包围盒。</summary>
    private static bool FindOpaqueBounds(Texture2D tex, byte alphaThreshold, out int minX, out int minY, out int w, out int h)
    {
        minX = 0;
        minY = 0;
        w = tex.width;
        h = tex.height;

        var pixels = tex.GetPixels32();
        int width = tex.width;
        int height = tex.height;
        int xMin = width, yMin = height, xMax = -1, yMax = -1;

        for (int y = 0; y < height; y++)
        {
            int row = y * width;
            for (int x = 0; x < width; x++)
            {
                if (pixels[row + x].a > alphaThreshold)
                {
                    if (x < xMin) xMin = x;
                    if (x > xMax) xMax = x;
                    if (y < yMin) yMin = y;
                    if (y > yMax) yMax = y;
                }
            }
        }

        if (xMax < 0) return false;
        minX = xMin;
        minY = yMin;
        w = xMax - xMin + 1;
        h = yMax - yMin + 1;
        return true;
    }

    /// <summary>按新贴图的世界包围盒修正点击碰撞盒，避免透明区误触或热区错位。</summary>
    private static void FixCollider(PassiveButton btn, SpriteRenderer activeSr)
    {
        try
        {
            var col = btn.activeSprites != null ? btn.activeSprites.GetComponent<BoxCollider2D>() : null;
            if (col == null) return;

            var bounds = activeSr.bounds;
            var scale = col.transform.lossyScale;
            col.size = new Vector2(
                bounds.size.x / Mathf.Abs(scale.x),
                bounds.size.y / Mathf.Abs(scale.y));

            var centerLocal = col.transform.InverseTransformPoint(bounds.center);
            col.offset = new Vector2(centerLocal.x, centerLocal.y);
        }
        catch (Exception ex)
        {
            LightLogger.LogError("[MainMenuButtonSprite.FixCollider]", ex);
        }
    }

    /// <summary>隐藏辉光方块(Shine)，删除所有 sprite 容器下的左侧图标(Icon)，保留按钮文字。</summary>
    private static void HideDecorations(PassiveButton btn)
    {
        try
        {
            var containers = new[]
            {
                btn.activeSprites, btn.inactiveSprites, btn.disabledSprites,
                btn.selectedSprites, btn.selectedInactiveSprites,
            };
            foreach (var container in containers)
            {
                if (container == null) continue;
                HideChild(container, "Shine");
                var icon = container.transform.FindChild("Icon");
                if (icon != null) UnityEngine.Object.Destroy(icon.gameObject);
            }
        }
        catch (Exception ex)
        {
            LightLogger.LogError("[MainMenuButtonSprite.HideDecorations]", ex);
        }
    }

    /// <summary>按钮文字设为辉光白，与按钮边缘辉光呼应。</summary>
    private static void SetButtonTextColor(PassiveButton btn)
    {
        try
        {
            // 深蓝紫渐变底配暖白辉光文字
            var glowWhite = new Color(1f, 0.95f, 0.85f, 1f);
            btn.activeTextColor = glowWhite;
            btn.inactiveTextColor = glowWhite;
            btn.selectedTextColor = glowWhite;
            btn.disabledTextColor = glowWhite;
            if (btn.buttonText != null)
            {
                btn.buttonText.gameObject.SetActive(true);
                btn.buttonText.color = glowWhite;
            }
        }
        catch (Exception ex)
        {
            LightLogger.LogError("[MainMenuButtonSprite.SetButtonTextColor]", ex);
        }
    }

    private static void HideChild(GameObject parent, string childName)
    {
        if (parent == null) return;
        parent.transform.FindChild(childName)?.gameObject.SetActive(false);
    }
}
