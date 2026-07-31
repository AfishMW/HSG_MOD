using Il2CppInterop.Runtime;
using Il2CppInterop.Runtime.Injection;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

// 如果你没有 UColor 别名，取消下行注释
// using UColor = UnityEngine.Color;

public class MetaScreen : MonoBehaviour
{
    // 图层（SpriteRenderer 部分）
    private const int OverlayOrder = 900;
    private const int GlowOrder = 999;
    private const int BgOrder = 1000;
    private const int BorderOrder = 1001;
    private const int TextOrder = 1002;

    private SpriteRenderer _bgRenderer;
    private SpriteRenderer _borderRenderer;
    private SpriteRenderer _glowRenderer;
    private TextMeshPro _titleText;
    private TextMeshPro _footerText;

    // UGUI 关闭按钮
    private Button _closeButton;
    private Image _closeImage;

    private bool _closing;
    private float _closeTimer;
    private const float CloseDuration = 0.2f;

    private bool _animating;
    private float _animTimer;
    private const float OpenDuration = 0.25f;

    private Texture2D _bgTexture;
    private Texture2D _borderTexture;
    private Texture2D _glowTexture;
    private Sprite _bgSprite;
    private Sprite _borderSprite;
    private Sprite _glowSprite;

    private static GameObject _overlay;
    private static SpriteRenderer _overlayRenderer;
    private static int _openWindowCount;
    private static Sprite _whiteSprite;

    private static readonly Vector3 CameraLocalPos = new Vector3(0f, 0f, 1f);

    // ------------------------------------------------------------------
    // 创建窗口
    // ------------------------------------------------------------------
    public static MetaScreen CreateWindow(
        string title = "",
        Vector2 size = default,
        UColor? topColor = null,
        UColor? bottomColor = null,
        bool withCloseButton = true,
        bool withAnimation = true,
        bool withGlow = true,
        float glowIntensity = 0.25f,
        string footer = null,
        Sprite backgroundSprite = null,
        Transform parent = null)
    {
        if (size == default)
            size = new Vector2(7.5f, 5f);

        UColor top = topColor ?? new UColor(1f, 0.85f, 0.6f, 1f);
        UColor bottom = bottomColor ?? new UColor(0.8f, 0.5f, 0.25f, 1f);

        ShowOverlay(0.3f);

        var rootObj = new GameObject("MetaScreen");

        if (parent == null)
        {
            var cam = Camera.main;
            if (cam != null)
                parent = cam.transform;
        }
        if (parent != null)
            rootObj.transform.SetParent(parent, false);
        else
            DontDestroyOnLoad(rootObj);

        rootObj.transform.localPosition = CameraLocalPos;
        rootObj.transform.localRotation = Quaternion.identity;

        var screen = rootObj.AddComponent(Il2CppType.Of<MetaScreen>()) as MetaScreen;
        if (screen == null)
            return null;

        screen._animating = withAnimation;

        // 背景：黑色直角矩形
        screen.CreateBackground(size, top, bottom, backgroundSprite);

        // 白色直角描边
        screen.CreateBorder(size);

        // 金色光晕
        if (withGlow)
            screen.CreateGlow(size, glowIntensity);

        // 标题（金色）
        screen.CreateTextObject(
            "Title",
            title,
            new Vector3(0f, size.y / 2f - 0.5f, -0.1f),
            new UColor(1f, 0.85f, 0.5f, 1f),
            1.8f,
            true,
            out screen._titleText);

        // 底部文字
        if (!string.IsNullOrEmpty(footer))
        {
            screen.CreateTextObject(
                "Footer",
                footer,
                new Vector3(0f, -size.y / 2f + 0.25f, -0.1f),
                new UColor(1f, 1f, 1f, 0.7f),
                1f,
                false,
                out screen._footerText);
        }

        // 左上角关闭按钮（UGUI）
        if (withCloseButton)
            screen.CreateCloseButton(size);

        return screen;
    }

    // ==================================================================
    // 以下为窗口主体（SpriteRenderer）生成逻辑
    // ==================================================================

    private void CreateBackground(Vector2 size, UColor top, UColor bottom, Sprite backgroundSprite)
    {
        var bgObj = new GameObject("Background");
        bgObj.transform.SetParent(transform, false);
        bgObj.transform.localPosition = Vector3.zero;

        _bgRenderer = bgObj.AddComponent<SpriteRenderer>();
        _bgRenderer.sortingOrder = BgOrder;
        _bgRenderer.color = UColor.white;

        if (backgroundSprite != null)
        {
            _bgRenderer.sprite = backgroundSprite;
            var b = backgroundSprite.bounds.size;
            bgObj.transform.localScale = new Vector3(
                size.x / (b.x == 0f ? 1f : b.x),
                size.y / (b.y == 0f ? 1f : b.y),
                1f);
        }
        else
        {
            _bgTexture = CreateSolidTexture(256, UColor.black);
            _bgSprite = Sprite.Create(
                _bgTexture,
                new Rect(0, 0, 256, 256),
                new Vector2(0.5f, 0.5f),
                256f / size.x);

            _bgRenderer.sprite = _bgSprite;
            bgObj.transform.localScale = new Vector3(1f, size.y / size.x, 1f);
        }
    }

    private void CreateBorder(Vector2 size)
    {
        var borderObj = new GameObject("Border");
        borderObj.transform.SetParent(transform, false);
        borderObj.transform.localPosition = Vector3.zero;

        _borderRenderer = borderObj.AddComponent<SpriteRenderer>();
        _borderRenderer.sortingOrder = BorderOrder;
        _borderRenderer.color = UColor.white;

        _borderTexture = CreateBorderTexture(256, 4);
        _borderSprite = Sprite.Create(
            _borderTexture,
            new Rect(0, 0, 256, 256),
            new Vector2(0.5f, 0.5f),
            256f / size.x);

        _borderRenderer.sprite = _borderSprite;
        borderObj.transform.localScale = new Vector3(1f, size.y / size.x, 1f);
    }

    private void CreateGlow(Vector2 size, float intensity)
    {
        _glowTexture = CreateRadialGlowTexture(64);
        _glowSprite = Sprite.Create(
            _glowTexture,
            new Rect(0, 0, 64, 64),
            new Vector2(0.5f, 0.5f),
            64f / (size.x * 1.15f));

        var glowObj = new GameObject("Glow");
        glowObj.transform.SetParent(transform, false);
        glowObj.transform.localPosition = Vector3.zero;

        _glowRenderer = glowObj.AddComponent<SpriteRenderer>();
        _glowRenderer.sprite = _glowSprite;
        _glowRenderer.color = new UColor(1f, 0.84f, 0.5f, Mathf.Clamp01(intensity));
        _glowRenderer.sortingOrder = GlowOrder;
    }

    private void CreateTextObject(string name, string content, Vector3 localPos, UColor color, float fontSize, bool bold, out TextMeshPro tmp)
    {
        var obj = new GameObject(name);
        obj.transform.SetParent(transform, false);
        obj.transform.localPosition = localPos;

        tmp = obj.AddComponent<TextMeshPro>();
        tmp.text = content;
        tmp.fontSize = fontSize;
        tmp.color = color;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.sortingOrder = TextOrder;

        if (bold)
            tmp.fontStyle = FontStyles.Bold;

        var defaultFont = TMP_Settings.defaultFontAsset;
        if (defaultFont == null)
        {
            var fonts = Resources.FindObjectsOfTypeAll<TMP_FontAsset>();
            if (fonts != null && fonts.Length > 0)
                defaultFont = fonts[0];
        }
        if (defaultFont != null)
            tmp.font = defaultFont;
    }

    // ------------------------------------------------------------------
    // 生成辅助纹理
    // ------------------------------------------------------------------
    private static Texture2D CreateSolidTexture(int texSize, UColor color)
    {
        var tex = new Texture2D(texSize, texSize, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.filterMode = FilterMode.Point;
        for (int y = 0; y < texSize; y++)
            for (int x = 0; x < texSize; x++)
                tex.SetPixel(x, y, color);
        tex.Apply();
        return tex;
    }

    private static Texture2D CreateBorderTexture(int texSize, int borderWidth)
    {
        var tex = new Texture2D(texSize, texSize, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.filterMode = FilterMode.Point;
        for (int y = 0; y < texSize; y++)
        {
            for (int x = 0; x < texSize; x++)
            {
                bool border = x < borderWidth || x >= texSize - borderWidth ||
                              y < borderWidth || y >= texSize - borderWidth;
                tex.SetPixel(x, y, border ? UColor.white : UColor.clear);
            }
        }
        tex.Apply();
        return tex;
    }

    private static Texture2D CreateRadialGlowTexture(int texSize)
    {
        var tex = new Texture2D(texSize, texSize, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.filterMode = FilterMode.Bilinear;
        float half = texSize / 2f;
        for (int y = 0; y < texSize; y++)
        {
            for (int x = 0; x < texSize; x++)
            {
                float dx = x - half;
                float dy = y - half;
                float dist = Mathf.Sqrt(dx * dx + dy * dy) / half;
                float alpha = Mathf.Clamp01(1f - dist);
                alpha = alpha * alpha * (3f - 2f * alpha);
                tex.SetPixel(x, y, new UColor(1f, 1f, 1f, alpha));
            }
        }
        tex.Apply();
        return tex;
    }

    // ==================================================================
    // 关闭按钮 —— 使用 UGUI，放在 HudManager 下，不会被遮挡
    // ==================================================================
    private void CreateCloseButton(Vector2 size)
    {
        // 找到游戏现成的两张图片（如果找不到就退化成同一张）
        Sprite normalSprite = FindSprite("buttonClick");
        Sprite hoverSprite = FindSprite("buttonHover");
        if (normalSprite == null)
            normalSprite = GetWhiteSprite();
        if (hoverSprite == null)
            hoverSprite = normalSprite;

        // 用 HUD 的 Canvas 作为父物体，确保 UI 层级
        Transform parent = null;
        if (HudManager.InstanceExists && HudManager.Instance != null)
            parent = HudManager.Instance.transform;

        GameObject buttonObj = new GameObject("MetaCloseButtonUI");
        buttonObj.transform.SetParent(parent, false);

        // 按钮尺寸（正方形）
        float buttonSize = 0.5f;

        // RectTransform
        var rect = buttonObj.AddComponent<RectTransform>();
        rect.sizeDelta = new Vector2(buttonSize * 100f, buttonSize * 100f); // 大概是 UI 单位，这里先按像素
        rect.pivot = new Vector2(0.5f, 0.5f);

        // 计算窗口左上角对应的屏幕坐标
        // 窗口中心在屏幕中央，size 是相机世界单位
        var cam = Camera.main;
        if (cam != null)
        {
            // 世界坐标 → 屏幕像素
            Vector3 worldPos = cam.transform.position + new Vector3(-size.x / 2f, size.y / 2f, 0f);
            Vector3 screenPos = cam.WorldToScreenPoint(worldPos);

            // 屏幕中心偏移量（Canvas 坐标系中，中心是 0,0）
            float x = screenPos.x - Screen.width / 2f;
            float y = screenPos.y - Screen.height / 2f;
            rect.anchoredPosition = new Vector2(x, y);
        }
        else
        {
            rect.anchoredPosition = new Vector2(-400f, 300f); // 兜底
        }

        // Image
        _closeImage = buttonObj.AddComponent<Image>();
        _closeImage.sprite = normalSprite;
        _closeImage.type = Image.Type.Simple;
        _closeImage.preserveAspect = true;
        _closeImage.color = UColor.white;

        // Button
        _closeButton = buttonObj.AddComponent<Button>();
        _closeButton.targetGraphic = _closeImage;

        // SpriteSwap 状态
        var spriteState = new SpriteState();
        spriteState.highlightedSprite = hoverSprite;
        _closeButton.spriteState = spriteState;

        // 点击事件
        _closeButton.onClick.AddListener((UnityEngine.Events.UnityAction)(() => Close()));
    }

    // ------------------------------------------------------------------
    // 查找已加载的 Sprite
    // ------------------------------------------------------------------
    private static Sprite FindSprite(string name)
    {
        foreach (var sprite in Resources.FindObjectsOfTypeAll<Sprite>())
        {
            if (sprite.name == name)
                return sprite;
        }
        return null;
    }

    // ------------------------------------------------------------------
    // 全屏阴影 + 通用白 Sprite
    // ------------------------------------------------------------------
    private static void ShowOverlay(float alpha)
    {
        if (_openWindowCount == 0)
        {
            if (_overlay == null)
            {
                _overlay = new GameObject("MetaScreenOverlay");
                _overlayRenderer = _overlay.AddComponent<SpriteRenderer>();
                _overlayRenderer.sprite = GetWhiteSprite();
                _overlayRenderer.color = new UColor(0f, 0f, 0f, alpha);
                _overlayRenderer.sortingOrder = OverlayOrder;
            }

            var cam = Camera.main;
            if (cam != null)
            {
                _overlay.transform.SetParent(cam.transform, false);
                _overlay.transform.localPosition = new Vector3(0f, 0f, 0.5f);

                float screenH = cam.orthographicSize * 2f;
                float screenW = screenH * cam.aspect;
                _overlay.transform.localScale = new Vector3(screenW, screenH, 1f);
            }

            _overlay.SetActive(true);
        }
        _openWindowCount++;
    }

    private static void HideOverlay()
    {
        _openWindowCount--;
        if (_openWindowCount <= 0)
        {
            _openWindowCount = 0;
            if (_overlay != null)
                _overlay.SetActive(false);
        }
    }

    private static Sprite GetWhiteSprite()
    {
        if (_whiteSprite != null)
            return _whiteSprite;

        var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        for (int x = 0; x < 2; x++)
            for (int y = 0; y < 2; y++)
                tex.SetPixel(x, y, UColor.white);
        tex.Apply();

        _whiteSprite = Sprite.Create(tex, new Rect(0, 0, 2, 2), new Vector2(0.5f, 0.5f), 2f);
        return _whiteSprite;
    }

    // ------------------------------------------------------------------
    // 更新循环（动画 + 按钮禁用）
    // ------------------------------------------------------------------
    private void Update()
    {
        // 开启动画
        if (_animating)
        {
            _animTimer += Time.deltaTime;
            float t = Mathf.Clamp01(_animTimer / OpenDuration);
            float eased = 1f - (1f - t) * (1f - t);
            transform.localScale = Vector3.Lerp(new Vector3(0.85f, 0.85f, 1f), Vector3.one, eased);
            SetAlpha(eased);
            if (t >= 1f)
            {
                _animating = false;
                transform.localScale = Vector3.one;
            }

            // 动画期间禁止点击关闭按钮
            if (_closeButton != null)
                _closeButton.interactable = false;
        }
        else
        {
            if (_closeButton != null)
                _closeButton.interactable = !_closing;
        }

        // 关闭动画
        if (_closing)
        {
            _closeTimer += Time.deltaTime;
            float t = Mathf.Clamp01(_closeTimer / CloseDuration);
            transform.localScale = Vector3.Lerp(Vector3.one, new Vector3(0.8f, 0.8f, 1f), t);
            SetAlpha(1f - t);
            if (t >= 1f)
            {
                Destroy(gameObject);
            }
        }
    }

    private void SetAlpha(float alpha)
    {
        if (_bgRenderer != null)
        {
            var c = _bgRenderer.color;
            c.a = alpha;
            _bgRenderer.color = c;
        }
        if (_borderRenderer != null)
        {
            var c = _borderRenderer.color;
            c.a = alpha;
            _borderRenderer.color = c;
        }
        if (_glowRenderer != null)
        {
            var c = _glowRenderer.color;
            c.a = alpha * _glowRenderer.color.a;
            _glowRenderer.color = c;
        }
        if (_titleText != null)
        {
            var c = _titleText.color;
            c.a = alpha;
            _titleText.color = c;
        }
        if (_footerText != null)
        {
            var c = _footerText.color;
            c.a = alpha;
            _footerText.color = c;
        }
        // 如果是 SpriteRenderer 关闭按钮才需要，现在没有
    }

    // ------------------------------------------------------------------
    // 关闭
    // ------------------------------------------------------------------
    public void Close()
    {
        if (_closing) return;
        _closing = true;
        _closeTimer = 0f;
    }

    private void OnDestroy()
    {
        HideOverlay();

        // 销毁 UGUI 按钮（如果它还存在）
        if (_closeButton != null)
            Destroy(_closeButton.gameObject);

        if (_bgTexture != null) Destroy(_bgTexture);
        if (_borderTexture != null) Destroy(_borderTexture);
        if (_glowTexture != null) Destroy(_glowTexture);
        if (_bgSprite != null) Destroy(_bgSprite);
        if (_borderSprite != null) Destroy(_borderSprite);
        if (_glowSprite != null) Destroy(_glowSprite);
    }
}