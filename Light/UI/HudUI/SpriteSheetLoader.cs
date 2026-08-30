using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using LightInDark.Core;
using System;

namespace Light.UI.HudUI;


/// <summary>
/// 精灵图加载器，支持从嵌入资源加载并分割精灵图。
/// </summary>
public static class SpriteSheetLoader
{
    private static readonly Dictionary<string, Sprite[]> _cache = new();

    /// <summary>
    /// 从嵌入资源加载完整 Sprite（不分割）
    /// </summary>
    public static Sprite Load(string resourcePath, float pixelsPerUnit = 100f)
    {
        try
        {
            var tex = LoadTexture(resourcePath);
            if (tex == null) return null!;
            return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height),
                new Vector2(0.5f, 0.5f), pixelsPerUnit);
        }
        catch (Exception ex)
        {
            LightLogger.LogError("[SpriteSheetLoader.Load]", ex); return default;
        }
    }

    /// <summary>
    /// 从嵌入资源加载并分割精灵图
    /// </summary>
    /// <param name="resourcePath">资源路径</param>
    /// <param name="pixelsPerUnit">每单位像素数</param>
    /// <param name="cols">列数</param>
    /// <param name="rows">行数</param>
    /// <param name="borderLeft">九宫格左边框（像素）</param>
    /// <param name="borderRight">九宫格右边框（像素）</param>
    /// <param name="borderTop">九宫格上边框（像素）</param>
    /// <param name="borderBottom">九宫格下边框（像素）</param>
    public static Sprite[] LoadDivided(string resourcePath, float pixelsPerUnit,
        int cols, int rows,
        int borderLeft = 0, int borderRight = 0, int borderTop = 0, int borderBottom = 0)
    {
        try
        {
            var cacheKey = $"{resourcePath}_{cols}x{rows}_{borderLeft}_{borderRight}_{borderTop}_{borderBottom}";
            if (_cache.TryGetValue(cacheKey, out var cached))
                return cached;

            var tex = LoadTexture(resourcePath);
            if (tex == null) return null!;

            var sprites = new Sprite[cols * rows];
            int cellW = tex.width / cols;
            int cellH = tex.height / rows;

            for (int row = 0; row < rows; row++)
            {
                for (int col = 0; col < cols; col++)
                {
                    // Unity Texture 坐标：左下为原点，精灵图从上到下排列
                    int x = col * cellW;
                    int y = (rows - 1 - row) * cellH;

                    var rect = new Rect(x, y, cellW, cellH);

                    // 九宫格边框
                    Vector4 border = new(
                        borderLeft, borderBottom,
                        borderRight, borderTop);

                    // 需要确保 border 不超过 cell 尺寸
                    border.x = Mathf.Min(border.x, cellW * 0.5f);
                    border.z = Mathf.Min(border.z, cellW * 0.5f);
                    border.y = Mathf.Min(border.y, cellH * 0.5f);
                    border.w = Mathf.Min(border.w, cellH * 0.5f);

                    var sprite = Sprite.Create(tex, rect,
                        new Vector2(0.5f, 0.5f), pixelsPerUnit,
                        0, SpriteMeshType.FullRect, border);

                    sprites[row * cols + col] = sprite;
                }
            }

            _cache[cacheKey] = sprites;
            return sprites;
        }
        catch (Exception ex)
        {
            LightLogger.LogError("[SpriteSheetLoader.LoadDivided]", ex); return default;
        }
    }

    /// <summary>
    /// 加载单个九宫格 Sprite（带边框）
    /// </summary>
    public static Sprite LoadSliced(string resourcePath, float pixelsPerUnit,
        int borderLeft, int borderRight, int borderTop, int borderBottom)
    {
        try
        {
            var tex = LoadTexture(resourcePath);
            if (tex == null) return null!;

            var rect = new Rect(0, 0, tex.width, tex.height);
            Vector4 border = new(borderLeft, borderBottom, borderRight, borderTop);

            return Sprite.Create(tex, rect,
                new Vector2(0.5f, 0.5f), pixelsPerUnit,
                0, SpriteMeshType.FullRect, border);
        }
        catch (Exception ex)
        {
            LightLogger.LogError("[SpriteSheetLoader.LoadSliced]", ex); return default;
        }
    }

    private static Texture2D LoadTexture(string resourcePath)
    {
        try
        {
            var assembly = Assembly.GetExecutingAssembly();
            using var stream = assembly.GetManifestResourceStream(resourcePath);
            if (stream == null)
            {
                LightInDark.Core.LightLogger.LogWarning($"[SpriteSheetLoader] 资源未找到: {resourcePath}");
                return null!;
            }

            byte[] bytes = new byte[stream.Length];
            stream.Read(bytes, 0, bytes.Length);

            var tex = new Texture2D(2, 2, TextureFormat.ARGB32, false);
            if (!ImageConversion.LoadImage(tex, bytes, false))
            {
                LightInDark.Core.LightLogger.LogWarning($"[SpriteSheetLoader] 图片加载失败: {resourcePath}");
                return null!;
            }
            tex.wrapMode = TextureWrapMode.Clamp;
            return tex;
        }
        catch (Exception ex)
        {
            LightLogger.LogError("[SpriteSheetLoader.LoadTexture]", ex); return default;
        }
    }
}

/// <summary>
/// HudUI 精灵图资源缓存
/// </summary>
public static class HudUIAssets
{
    // GUI/Button.png — 3列×2行，12px 边框
    // [0]=normal [1]=hover [2]=unused
    // [3]=selected [4]=selected+hover [5]=unused
    private static Sprite[]? _buttonSprites;
    public static Sprite[] ButtonSprites
    {
        get
        {
            try
            {
                if (_buttonSprites != null) return _buttonSprites;
                _buttonSprites = SpriteSheetLoader.LoadDivided(
                    "Light.Resources.GUI.SettingGUI.Button.png", 150f, 3, 2, 12, 12, 12, 12);
                return _buttonSprites!;
            }
            catch (Exception ex)
            {
                LightLogger.LogError("[SpriteSheetLoader.get]", ex); return default;
            }
        }
    }

    public static Sprite ButtonNormal => ButtonSprites[0];
    public static Sprite ButtonHover => ButtonSprites[1];
    public static Sprite ButtonSelected => ButtonSprites[3];
    public static Sprite ButtonSelectedHover => ButtonSprites[4];

    // GUI/Checkmark.png — 2列×1行
    private static Sprite[]? _checkmarkSprites;
    public static Sprite[] CheckmarkSprites
    {
        get
        {
            try
            {
                if (_checkmarkSprites != null) return _checkmarkSprites;
                _checkmarkSprites = SpriteSheetLoader.LoadDivided(
                    "Light.Resources.GUI.SettingGUI.Checkmark.png", 150f, 2, 1, 0, 0, 0, 0);
                return _checkmarkSprites!;
            }
            catch (Exception ex)
            {
                LightLogger.LogError("[SpriteSheetLoader.get]", ex); return default;
            }
        }
    }
    public static Sprite CheckmarkUnselected => CheckmarkSprites[0];
    public static Sprite CheckmarkSelected => CheckmarkSprites[1];

    // GUI/CloseButton.png — 2列×1行
    private static Sprite[]? _closeButtonSprites;
    public static Sprite[] CloseButtonSprites
    {
        get
        {
            try
            {
                if (_closeButtonSprites != null) return _closeButtonSprites;
                _closeButtonSprites = SpriteSheetLoader.LoadDivided(
                    "Light.Resources.GUI.SettingGUI.CloseButton.png", 150f, 2, 1, 0, 0, 0, 0);
                return _closeButtonSprites!;
            }
            catch (Exception ex)
            {
                LightLogger.LogError("[SpriteSheetLoader.get]", ex); return default;
            }
        }
    }
    public static Sprite CloseNormal => CloseButtonSprites[0];
    public static Sprite CloseHover => CloseButtonSprites[1];

    // GUI/NavButton.png — 2列×2行
    private static Sprite[]? _navButtonSprites;
    public static Sprite[] NavButtonSprites
    {
        get
        {
            try
            {
                if (_navButtonSprites != null) return _navButtonSprites;
                _navButtonSprites = SpriteSheetLoader.LoadDivided(
                    "Light.Resources.GUI.SettingGUI.NavButton.png", 150f, 2, 2, 0, 0, 0, 0);
                return _navButtonSprites!;
            }
            catch (Exception ex)
            {
                LightLogger.LogError("[SpriteSheetLoader.get]", ex); return default;
            }
        }
    }
    public static Sprite NavLeftNormal => NavButtonSprites[0];
    public static Sprite NavLeftHover => NavButtonSprites[1];
    public static Sprite NavRightNormal => NavButtonSprites[2];
    public static Sprite NavRightHover => NavButtonSprites[3];

    // GUI/Background_Frame.png — 九宫格
    private static Sprite? _frameSprite;
    public static Sprite FrameSprite
    {
        get
        {
            try
            {
                if (_frameSprite != null) return _frameSprite;
                _frameSprite = SpriteSheetLoader.LoadSliced(
                    "Light.Resources.GUI.SettingGUI.Background_Frame.png", 100f, 12, 12, 12, 12);
                return _frameSprite;
            }
            catch (Exception ex)
            {
                LightLogger.LogError("[SpriteSheetLoader.get]", ex); return default;
            }
        }
    }

    // GUI/Background_Inner.png — 九宫格
    private static Sprite? _innerSprite;
    public static Sprite InnerSprite
    {
        get
        {
            try
            {
                if (_innerSprite != null) return _innerSprite;
                _innerSprite = SpriteSheetLoader.LoadSliced(
                    "Light.Resources.GUI.SettingGUI.Background_Inner.png", 100f, 8, 8, 8, 8);
                return _innerSprite;
            }
            catch (Exception ex)
            {
                LightLogger.LogError("[SpriteSheetLoader.get]", ex); return default;
            }
        }
    }


    // ── SettingGUI 系列（设置界面专用素材，Light.Resources.GUI.SettingGUI.*）──

    private static Sprite? _settingFrameSprite;
    public static Sprite SettingFrameSprite
    {
        get
        {
            try
            {
                if (_settingFrameSprite != null) return _settingFrameSprite;
                _settingFrameSprite = SpriteSheetLoader.LoadSliced(
                    "Light.Resources.GUI.SettingGUI.Background_Frame.png", 100f, 12, 12, 12, 12);
                return _settingFrameSprite;
            }
            catch (Exception ex)
            {
                LightLogger.LogError("[SpriteSheetLoader.get]", ex); return default;
            }
        }
    }

    private static Sprite? _settingInnerSprite;
    public static Sprite SettingInnerSprite
    {
        get
        {
            try
            {
                if (_settingInnerSprite != null) return _settingInnerSprite;
                _settingInnerSprite = SpriteSheetLoader.LoadSliced(
                    "Light.Resources.GUI.SettingGUI.Background_Inner.png", 100f, 8, 8, 8, 8);
                return _settingInnerSprite;
            }
            catch (Exception ex)
            {
                LightLogger.LogError("[SpriteSheetLoader.get]", ex); return default;
            }
        }
    }

    private static Sprite[]? _settingCloseSprites;
    public static Sprite[] SettingCloseSprites
    {
        get
        {
            try
            {
                if (_settingCloseSprites != null) return _settingCloseSprites;
                _settingCloseSprites = SpriteSheetLoader.LoadDivided(
                    "Light.Resources.GUI.SettingGUI.CloseButton.png", 150f, 2, 1, 0, 0, 0, 0);
                return _settingCloseSprites!;
            }
            catch (Exception ex)
            {
                LightLogger.LogError("[SpriteSheetLoader.get]", ex); return default;
            }
        }
    }
    public static Sprite SettingCloseNormal => SettingCloseSprites[0];
    public static Sprite SettingCloseHover => SettingCloseSprites[1];
    // GUI/ColorButton.png — 九宫格（薄边框）
    private static Sprite? _colorButtonSprite;
    public static Sprite ColorButtonSprite
    {
        get
        {
            try
            {
                if (_colorButtonSprite != null) return _colorButtonSprite;
                _colorButtonSprite = SpriteSheetLoader.LoadSliced(
                    "Light.Resources.GUI.ColorButton.png", 100f, 4, 4, 4, 4);
                return _colorButtonSprite;
            }
            catch (Exception ex)
            {
                LightLogger.LogError("[SpriteSheetLoader.get]", ex); return default;
            }
        }
    }

    // GUI/ColorButtonSelected.png — 九宫格（选中态）
    private static Sprite? _colorButtonSelectedSprite;
    public static Sprite ColorButtonSelectedSprite
    {
        get
        {
            try
            {
                if (_colorButtonSelectedSprite != null) return _colorButtonSelectedSprite;
                _colorButtonSelectedSprite = SpriteSheetLoader.LoadSliced(
                    "Light.Resources.GUI.ColorButtonSelected.png", 100f, 4, 4, 4, 4);
                return _colorButtonSelectedSprite;
            }
            catch (Exception ex)
            {
                LightLogger.LogError("[SpriteSheetLoader.get]", ex); return default;
            }
        }
    }

    // GUI/ColorFullBase.png — 白色底
    private static Sprite? _whiteSprite;
    public static Sprite WhiteSprite
    {
        get
        {
            try
            {
                if (_whiteSprite != null) return _whiteSprite;
                _whiteSprite = SpriteSheetLoader.Load(
                    "Light.Resources.GUI.ColorFullBase.png", 100f);
                return _whiteSprite;
            }
            catch (Exception ex)
            {
                LightLogger.LogError("[SpriteSheetLoader.get]", ex); return default;
            }
        }
    }
}
