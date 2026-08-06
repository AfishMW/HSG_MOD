using System;
using System.Collections.Generic;
using Il2CppInterop.Runtime.Injection;
using LightInDark.UI.Window;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Rendering;
using UnityEngine.UI;
using Button = UnityEngine.UI.Button;
using Object = UnityEngine.Object;

namespace LightInDark.UI.HudUI;

/// <summary>
/// 按钮尺寸预设
/// </summary>
public static class ButtonSize
{
    public static readonly Vector2 Square = new(0.8f, 0.8f);
    public static readonly Vector2 Rectangle = new(2.5f, 0.5f);
    public static readonly Vector2 SmallSquare = new(0.4f, 0.4f);
    public static readonly Vector2 LongRectangle = new(3.5f, 0.5f);
    public static readonly Vector2 Wide = new(4.0f, 0.6f);
}

/// <summary>
/// 窗口尺寸预设，参考 NebulaN 的 PatchManager 调用
/// </summary>
public static class WindowSize
{
    /// <summary>角色选择窗口（NebulaN PatchManager 使用）</summary>
    public static readonly Vector2 RoleSelect = new(7.6f, 4.2f);

    /// <summary>标准窗口</summary>
    public static readonly Vector2 Standard = new(5f, 3f);

    /// <summary>小窗口</summary>
    public static readonly Vector2 Small = new(3.5f, 1.5f);

    /// <summary>确认对话框</summary>
    public static readonly Vector2 Confirm = new(3.9f, 1.14f);

    /// <summary>大窗口</summary>
    public static readonly Vector2 Large = new(8f, 5f);

    /// <summary>帮助窗口</summary>
    public static readonly Vector2 Help = new(9f, 5.5f);

    /// <summary>带 ScrollView 的窗口内容区域（NebulaN 使用 6.9×3.0）</summary>
    public static readonly Vector2 ScrollViewContent = new(6.9f, 3.0f);
}

/// <summary>
/// 背景样式枚举，对应 Nebula 的 BackgroundSetting
/// </summary>
public enum BackgroundSetting
{
    Off,
    Old,
    Modern,
}

/// <summary>
/// 字体缓存，从 Among Us 原版 VersionShower 克隆。
/// Among Us 使用 Barlow 字体（Barlow-Black SDF / Barlow-BoldItalic SDF）。
/// VersionShower 有公开字段 text (TextMeshPro)，字体在其 prefab 中序列化。
/// </summary>
public static class HudUIFont
{
    private static TMP_FontAsset? _fontAsset;
    private static Material? _fontMaterial;
    private static bool _triedInit;

    public static TMP_FontAsset FontAsset
    {
        get { EnsureLoaded(); return _fontAsset!; }
    }

    public static Material FontMaterial
    {
        get { EnsureLoaded(); return _fontMaterial!; }
    }

    public static void EnsureLoaded()
    {
        if (_triedInit) return;
        _triedInit = true;

        // 方案1：从 VersionShower.text 获取（VersionShower 有公开字段 text）
        try
        {
            var versionShower = Object.FindObjectOfType<VersionShower>();
            if (versionShower != null && versionShower.text != null && versionShower.text.font != null)
            {
                _fontAsset = versionShower.text.font;
                _fontMaterial = versionShower.text.fontMaterial;
                return;
            }
        }
        catch { }

        // 方案2：从 HudManager.Instance.Dialogue.target 获取
        try
        {
            if (HudManager.Instance != null && HudManager.Instance.Dialogue != null &&
                HudManager.Instance.Dialogue.target != null && HudManager.Instance.Dialogue.target.font != null)
            {
                _fontAsset = HudManager.Instance.Dialogue.target.font;
                _fontMaterial = HudManager.Instance.Dialogue.target.fontMaterial;
                return;
            }
        }
        catch { }

        // 方案3：从场景中任意 TextMeshPro 获取
        try
        {
            var anyTmp = Object.FindObjectOfType<TextMeshPro>();
            if (anyTmp != null && anyTmp.font != null)
            {
                _fontAsset = anyTmp.font;
                _fontMaterial = anyTmp.fontMaterial;
                return;
            }
        }
        catch { }

        // 方案4：TMP_Settings 默认
        try
        {
            if (TMP_Settings.defaultFontAsset != null)
            {
                _fontAsset = TMP_Settings.defaultFontAsset;
                _fontMaterial = TMP_Settings.defaultFontAsset.material;
            }
        }
        catch { }
    }
}

/// <summary>
/// 创建 TextMeshPro，使用 Among Us 原版字体。
/// </summary>
internal static class HudUITextHelper
{
    public static TextMeshPro Create(Transform parent)
    {
        var obj = new GameObject("Text");
        obj.layer = LayerMask.NameToLayer("UI");
        obj.transform.SetParent(parent, false);
        obj.transform.localPosition = Vector3.zero;

        var tmp = obj.AddComponent<TextMeshPro>();

        HudUIFont.EnsureLoaded();
        if (HudUIFont.FontAsset != null)
        {
            tmp.font = HudUIFont.FontAsset;
            tmp.fontSharedMaterial = HudUIFont.FontMaterial;
        }

        tmp.enableAutoSizing = false;
        tmp.enableWordWrapping = false;
        tmp.raycastTarget = false;
        tmp.outlineWidth = 0.15f;
        tmp.outlineColor = UnityEngine.Color.black;

        return tmp;
    }
}

// =====================================================================
// MetaScreen — 完全按照 Nebula 的 MetaScreen 实现
// =====================================================================

/// <summary>
/// GUI 屏幕，MonoBehaviour，通过 ClassInjector 注册。
/// 完全对应 Nebula 的 MetaScreen 类。
/// </summary>
public class MetaScreen : MonoBehaviour
{
    static MetaScreen()
    {
        ClassInjector.RegisterTypeInIl2Cpp<MetaScreen>();
    }

    public void Awake()
    {
        gameObject.AddComponent<SortingGroup>();
    }

    private GameObject? _combinedObject;
    private Vector2 _border;

    public Vector2 Border
    {
        get => _border;
        private set => _border = value;
    }

    /// <summary>
    /// 设置 Widget（旧式 IMetaWidgetOld 接口暂不支持，仅支持 GUIWidget）
    /// </summary>
    public void SetWidget(GUIWidget? widget, out Size actualSize)
    {
        SetWidget(widget, new Vector2(0f, 1f), out actualSize);
    }

    public void SetWidget(GUIWidget? widget, Vector2 anchor, out Size actualSize)
    {
        ClearWidget();

        if (widget == null)
        {
            actualSize = Size.Zero;
            return;
        }

        var anchorRecord = new Anchor(anchor,
            new Vector3(_border.x * (anchor.x - 0.5f), _border.y * (anchor.y - 0.5f), -0.1f));

        var obj = widget.Instantiate(anchorRecord, new Size(_border), out actualSize);
        if (obj != null)
        {
            obj.transform.SetParent(transform, false);
        }
    }

    private void ClearWidget()
    {
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            var child = transform.GetChild(i);
            if (child.name != "BorderLine")
                Object.Destroy(child.gameObject);
        }
    }

    /// <summary>
    /// 关闭窗口
    /// </summary>
    public void CloseScreen()
    {
        try { Object.Destroy(_combinedObject ?? gameObject); }
        catch { }
    }

    // ---- 静态资源 ----

    // 背景精灵图（九宫格）
    private static Sprite FrameSprite => HudUIAssets.FrameSprite;
    private static Sprite InnerSprite => HudUIAssets.InnerSprite;

    // 关闭按钮精灵图
    private static Sprite CloseButtonNormal => HudUIAssets.CloseNormal;
    private static Sprite CloseButtonHover => HudUIAssets.CloseHover;

    // 导航按钮精灵图
    private static Sprite NavLeftNormal => HudUIAssets.NavLeftNormal;
    private static Sprite NavLeftHover => HudUIAssets.NavLeftHover;
    private static Sprite NavRightNormal => HudUIAssets.NavRightNormal;
    private static Sprite NavRightHover => HudUIAssets.NavRightHover;

    // ---- 生成窗口 ----

    /// <summary>
    /// 生成屏幕（对应 Nebula 的 GenerateScreen）
    /// </summary>
    public static MetaScreen GenerateScreen(Vector2 size, Transform? parent, Vector3 localPos,
        BackgroundSetting backgroundSetting, bool withBlackScreen, bool withClickGuard)
    {
        var window = CreateObject("MetaWindow", parent, localPos);

        if (backgroundSetting == BackgroundSetting.Old)
        {
            var renderer = CreateObject<SpriteRenderer>("Background", window.transform, new Vector3(0, 0, 0.1f));
            renderer.sprite = LightInDark.UI.Window.VanillaAsset.PopUpBackSprite;
            renderer.drawMode = SpriteDrawMode.Sliced;
            renderer.tileMode = SpriteTileMode.Continuous;
            renderer.size = size + new Vector2(0.45f, 0.35f);
            renderer.gameObject.layer = LayerExpansion.GetUILayer();
        }
        else if (backgroundSetting == BackgroundSetting.Modern)
        {
            var inner = CreateObject<SpriteRenderer>("Inner", window.transform, new Vector3(0, 0, 0.1f));
            inner.sprite = InnerSprite;
            inner.drawMode = SpriteDrawMode.Sliced;
            inner.tileMode = SpriteTileMode.Continuous;
            inner.size = size + new Vector2(0.75f, 0.75f);
            inner.gameObject.layer = LayerExpansion.GetUILayer();
            inner.color = UnityEngine.Color.white.RGBMultiplied(0.55f);

            var frame = CreateObject<SpriteRenderer>("Frame", window.transform, new Vector3(0, 0, -0.01f));
            frame.sprite = FrameSprite;
            frame.drawMode = SpriteDrawMode.Sliced;
            frame.tileMode = SpriteTileMode.Continuous;
            frame.size = size + new Vector2(0.75f, 0.75f);
            frame.gameObject.layer = LayerExpansion.GetUILayer();
        }

        if (withBlackScreen)
        {
            var renderer = CreateObject<SpriteRenderer>("BlackScreen", window.transform, new Vector3(0, 0, 0.2f));
            renderer.sprite = LightInDark.UI.Window.VanillaAsset.FullScreenSprite;
            renderer.drawMode = SpriteDrawMode.Sliced;
            renderer.size = new Vector2(30f, 30f);
            renderer.color = new UnityEngine.Color(0, 0, 0, 0.4226f);
            renderer.gameObject.layer = LayerExpansion.GetUILayer();
        }

        if (withClickGuard)
        {
            var collider = CreateObject<BoxCollider2D>("ClickGuard", window.transform, new Vector3(0, 0, 0.2f));
            collider.isTrigger = true;
            collider.gameObject.layer = LayerExpansion.GetUILayer();
            collider.size = new Vector2(100f, 100f);
            collider.gameObject.SetUpButton(false, playSound: false);
        }

        var screen = CreateObject<MetaScreen>("Screen", window.transform, Vector3.zero);
        screen.Border = size;
        screen._combinedObject = window;

        return screen;
    }

    /// <summary>
    /// 生成窗口（对应 Nebula 的 GenerateWindow）
    /// </summary>
    public static MetaScreen GenerateWindow(Vector2 size, Transform? parent, Vector3 localPos,
        bool withBlackScreen = true, bool closeOnClickOutside = false,
        BackgroundSetting background = BackgroundSetting.Modern, bool withCloseButton = true)
    {
        var screen = GenerateScreen(size, parent, localPos, background, withBlackScreen, true);
        var obj = screen.transform.parent.gameObject;

        if (withCloseButton)
        {
            if (background == BackgroundSetting.Modern)
            {
                // Modern 风格关闭按钮 — 左上角外侧
                var collider = CreateObject<BoxCollider2D>("CloseButton", obj.transform,
                    new Vector3(-size.x / 2f - 0.3f, size.y / 2f + 0.2f, -1f));
                collider.transform.localScale = new Vector3(0.57f, 0.57f, 1f);
                collider.isTrigger = true;
                collider.gameObject.layer = LayerExpansion.GetUILayer();
                collider.size = new Vector2(0.85f, 0.85f);

                var renderer = collider.gameObject.AddComponent<SpriteRenderer>();
                renderer.sprite = CloseButtonNormal;

                var button = collider.gameObject.SetUpButton(true, renderer, playSound: true);
                button.OnClick.AddListener((UnityAction)(() => Object.Destroy(obj)));
                button.OnMouseOver.AddListener((UnityAction)(() => renderer.sprite = CloseButtonHover));
                button.OnMouseOut.AddListener((UnityAction)(() => renderer.sprite = CloseButtonNormal));
            }
            else
            {
                // Old 风格关闭按钮
                var collider = CreateObject<BoxCollider2D>("CloseButton", obj.transform,
                    new Vector3(-size.x / 2f - 0.3f, size.y / 2f + 0.2f, -1f));
                collider.transform.localScale = new Vector3(0.57f, 0.57f, 1f);
                collider.isTrigger = true;
                collider.gameObject.layer = LayerExpansion.GetUILayer();
                collider.size = new Vector2(0.85f, 0.85f);

                var renderer = collider.gameObject.AddComponent<SpriteRenderer>();
                renderer.sprite = CloseButtonNormal;

                var button = collider.gameObject.SetUpButton(true, renderer, playSound: true);
                button.OnClick.AddListener((UnityAction)(() => Object.Destroy(obj)));
                button.OnMouseOver.AddListener((UnityAction)(() => renderer.sprite = CloseButtonHover));
                button.OnMouseOut.AddListener((UnityAction)(() => renderer.sprite = CloseButtonNormal));
            }
        }

        if (closeOnClickOutside)
        {
            var clickGuard = obj.transform.FindChild("ClickGuard");
            if (clickGuard != null)
            {
                clickGuard.GetComponent<PassiveButton>().OnClick.AddListener((UnityAction)(() => Object.Destroy(obj)));
            }
        }

        return screen;
    }

    /// <summary>
    /// 生成带导航按钮的窗口（对应 Nebula 的 GenerateWindow with nav）
    /// </summary>
    public static MetaScreen GenerateWindow(Func<int, GUIWidget> widgetGenerator, int index, (int min, int max) range,
        Vector2 size, Transform? parent, Vector3 localPos,
        bool withBlackScreen = true, bool closeOnClickOutside = false, bool withCloseButton = true)
    {
        var window = GenerateWindow(size, parent, localPos, withBlackScreen, closeOnClickOutside,
            BackgroundSetting.Modern, withCloseButton);

        SetUpNavButton(window, increment =>
        {
            if (increment)
            {
                index++;
                if (index >= range.max) index = range.min;
            }
            else
            {
                index--;
                if (index < range.min) index = range.max - 1;
            }
            window.SetWidget(widgetGenerator.Invoke(index), out _);
        });

        window.SetWidget(widgetGenerator.Invoke(index), out _);
        return window;
    }

    /// <summary>
    /// 设置导航按钮（对应 Nebula 的 SetUpNavButton）
    /// </summary>
    public static void SetUpNavButton(MetaScreen screen, Action<bool> navFunc)
    {
        var obj = screen.transform.parent.gameObject;

        PassiveButton GenerateButton(float x, int img)
        {
            var collider = CreateObject<BoxCollider2D>("NavButton", obj.transform,
                new Vector3(screen.Border.x / 2f + 0.3f - x, screen.Border.y / 2f + 0.25f, -1f));
            collider.transform.localScale = new Vector3(0.57f, 0.57f, 1f);
            collider.isTrigger = true;
            collider.gameObject.layer = LayerExpansion.GetUILayer();
            collider.size = new Vector2(0.65f, 0.65f);

            var renderer = collider.gameObject.AddComponent<SpriteRenderer>();
            renderer.sprite = img == 0 ? NavLeftNormal : NavRightNormal;

            var button = collider.gameObject.SetUpButton(true, renderer, playSound: true);
            button.OnMouseOver.AddListener((UnityAction)(() => renderer.sprite = img == 0 ? NavLeftHover : NavRightHover));
            button.OnMouseOut.AddListener((UnityAction)(() => renderer.sprite = img == 0 ? NavLeftNormal : NavRightNormal));
            return button;
        }

        // 左箭头（上一页）
        GenerateButton(0.7f, 0).OnClick.AddListener((UnityAction)(() => navFunc.Invoke(false)));
        // 右箭头（下一页）
        GenerateButton(0.3f, 2).OnClick.AddListener((UnityAction)(() => navFunc.Invoke(true)));
    }

    // ---- 辅助方法 ----

    private static GameObject CreateObject(string name, Transform? parent, Vector3 localPos)
    {
        var obj = new GameObject(name);
        obj.layer = LayerExpansion.GetUILayer();
        if (parent != null) obj.transform.SetParent(parent, false);
        obj.transform.localPosition = localPos;
        obj.transform.localScale = Vector3.one;
        return obj;
    }

    private static T CreateObject<T>(string name, Transform? parent, Vector3 localPos) where T : Component
    {
        var obj = CreateObject(name, parent, localPos);
        return obj.AddComponent<T>();
    }
}

// =====================================================================
// HudUIButton — 自定义按钮，使用精灵图材质
// =====================================================================

/// <summary>
/// 自定义按钮，使用精灵图材质。支持普通/悬停/选中三态。
/// </summary>
public class HudUIButton
{
    public GameObject GameObject { get; private set; }
    public SpriteRenderer Renderer { get; private set; }
    public TextMeshPro Text { get; private set; }
    public PassiveButton Button { get; private set; }
    public BoxCollider2D Collider { get; private set; }

    private Sprite _normalSprite;
    private Sprite _hoverSprite;
    private Sprite _selectedSprite;
    private Sprite _selectedHoverSprite;
    private bool _isSelected;
    private Vector2 _size;

    private HudUIButton(GameObject obj)
    {
        GameObject = obj;
        Renderer = obj.GetComponent<SpriteRenderer>();
        Text = obj.GetComponentInChildren<TextMeshPro>();
        Button = obj.GetComponent<PassiveButton>();
        Collider = obj.GetComponent<BoxCollider2D>();
    }

    /// <summary>
    /// 创建现代风格按钮（使用 GUI/Button.png 精灵图）。
    /// 当 size 为 null 时，按钮尺寸自动匹配文本（参考 Nebula GUIButton）。
    /// </summary>
    public static HudUIButton Create(Transform parent, string text = "",
        Vector2? size = null, Action? onClick = null)
    {
        var obj = new GameObject("HudUIButton");
        obj.layer = LayerExpansion.GetUILayer();
        obj.transform.SetParent(parent, false);
        obj.transform.localPosition = Vector3.zero;
        obj.transform.localScale = Vector3.one;

        var renderer = obj.AddComponent<SpriteRenderer>();
        renderer.sprite = HudUIAssets.ButtonNormal;
        renderer.drawMode = SpriteDrawMode.Sliced;
        renderer.tileMode = SpriteTileMode.Continuous;
        renderer.sortingOrder = 10;

        var collider = obj.AddComponent<BoxCollider2D>();
        collider.isTrigger = true;

        obj.AddComponent<SortingGroup>();

        var tmp = HudUITextHelper.Create(obj.transform);
        tmp.text = text;
        tmp.fontSize = 2f;
        tmp.fontSizeMax = 2f;
        tmp.fontSizeMin = 1f;
        tmp.m_fontSizeBase = 3f;
        tmp.enableAutoSizing = true;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = UnityEngine.Color.white;
        tmp.raycastTarget = false;
        tmp.rectTransform.pivot = new Vector2(0.5f, 0.5f);
        tmp.rectTransform.localPosition = new Vector3(0f, 0f, -0.1f);
        tmp.ForceMeshUpdate();

        var hudButton = new HudUIButton(obj);
        hudButton._normalSprite = HudUIAssets.ButtonNormal;
        hudButton._hoverSprite = HudUIAssets.ButtonHover;
        hudButton._selectedSprite = HudUIAssets.ButtonSelected;
        hudButton._selectedHoverSprite = HudUIAssets.ButtonSelectedHover;

        // 尺寸：指定了用指定的，没指定用文本 × 1.5
        var btnSize = size ?? CalcAutoSize(tmp);
        hudButton.ApplySize(btnSize);
        tmp.sortingOrder = 15;

        var button = obj.AddComponent<PassiveButton>();
        button.OnMouseOver = new UnityEvent();
        button.OnMouseOut = new UnityEvent();
        button.OnClick = new Button.ButtonClickedEvent();

        button.OnMouseOver.AddListener((UnityAction)(() =>
        {
            renderer.sprite = hudButton._isSelected ? hudButton._selectedHoverSprite : hudButton._hoverSprite;
            try { SoundManager.Instance.PlaySound(LightInDark.UI.Window.VanillaAsset.FindSoundClip("UI_Hover"), false, 0.8f); } catch { }
        }));
        button.OnMouseOut.AddListener((UnityAction)(() =>
        {
            renderer.sprite = hudButton._isSelected ? hudButton._selectedSprite : hudButton._normalSprite;
        }));

        var localOnClick = onClick;
        button.OnClick.AddListener((UnityAction)(() =>
        {
            try { SoundManager.Instance.PlaySound(LightInDark.UI.Window.VanillaAsset.FindSoundClip("UI_Select"), false, 0.8f); } catch { }
            localOnClick?.Invoke();
        }));

        return hudButton;
    }

    /// <summary>
    /// 创建简单颜色按钮（使用 ColorButton.png / ColorButtonSelected.png）。
    /// 当 size 为 null 时，按钮尺寸自动匹配文本。
    /// </summary>
    public static HudUIButton CreateColorButton(Transform parent, string text = "",
        Vector2? size = null, Action? onClick = null, Color? color = null)
    {
        var btnColor = color?.ToUnityColor() ?? UnityEngine.Color.white;

        var obj = new GameObject("HudUIColorButton");
        obj.layer = LayerExpansion.GetUILayer();
        obj.transform.SetParent(parent, false);
        obj.transform.localPosition = Vector3.zero;
        obj.transform.localScale = Vector3.one;

        var renderer = obj.AddComponent<SpriteRenderer>();
        renderer.sprite = HudUIAssets.ColorButtonSprite;
        renderer.drawMode = SpriteDrawMode.Sliced;
        renderer.tileMode = SpriteTileMode.Continuous;
        renderer.color = btnColor;
        renderer.sortingOrder = 10;

        var collider = obj.AddComponent<BoxCollider2D>();
        collider.isTrigger = true;

        obj.AddComponent<SortingGroup>();

        var tmp = HudUITextHelper.Create(obj.transform);
        tmp.text = text;
        tmp.fontSize = 2f;
        tmp.fontSizeMax = 2f;
        tmp.fontSizeMin = 1f;
        tmp.m_fontSizeBase = 3f;
        tmp.enableAutoSizing = true;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = UnityEngine.Color.white;
        tmp.raycastTarget = false;
        tmp.rectTransform.pivot = new Vector2(0.5f, 0.5f);
        tmp.rectTransform.localPosition = new Vector3(0f, 0f, -0.1f);
        tmp.ForceMeshUpdate();

        var hudButton = new HudUIButton(obj);
        hudButton._normalSprite = HudUIAssets.ColorButtonSprite;
        hudButton._hoverSprite = HudUIAssets.ColorButtonSelectedSprite;
        hudButton._selectedSprite = HudUIAssets.ColorButtonSelectedSprite;
        hudButton._selectedHoverSprite = HudUIAssets.ColorButtonSelectedSprite;

        var btnSize = size ?? CalcAutoSize(tmp);
        hudButton.ApplySize(btnSize);
        tmp.sortingOrder = 15;

        var button = obj.AddComponent<PassiveButton>();
        button.OnMouseOver = new UnityEvent();
        button.OnMouseOut = new UnityEvent();
        button.OnClick = new Button.ButtonClickedEvent();

        var normalColor = btnColor;
        var hoverColor = new UnityEngine.Color(
            Mathf.Min(btnColor.r * 1.3f, 1f),
            Mathf.Min(btnColor.g * 1.3f, 1f),
            Mathf.Min(btnColor.b * 1.3f, 1f),
            btnColor.a);

        button.OnMouseOver.AddListener((UnityAction)(() =>
        {
            renderer.sprite = hudButton._hoverSprite;
            renderer.color = hoverColor;
        }));
        button.OnMouseOut.AddListener((UnityAction)(() =>
        {
            renderer.sprite = hudButton._normalSprite;
            renderer.color = normalColor;
        }));

        var localOnClick = onClick;
        button.OnClick.AddListener((UnityAction)(() =>
        {
            try { SoundManager.Instance.PlaySound(LightInDark.UI.Window.VanillaAsset.FindSoundClip("UI_Select"), false, 0.8f); } catch { }
            localOnClick?.Invoke();
        }));

        return hudButton;
    }

    private const float DefaultMargin = 0.26f;

    /// <summary>
    /// 计算文本自适应的按钮尺寸（文本尺寸 × 1.5）
    /// </summary>
    private static Vector2 CalcAutoSize(TextMeshPro tmp)
    {
        float textW = tmp.preferredWidth;
        float textH = tmp.preferredHeight;
        return new Vector2(textW * 1.5f, textH * 1.5f);
    }

    /// <summary>
    /// 应用尺寸到所有组件
    /// </summary>
    private void ApplySize(Vector2 size)
    {
        _size = size;
        Renderer.size = size;
        Collider.size = size;
        if (Text != null)
        {
            Text.rectTransform.sizeDelta = size - new Vector2(DefaultMargin * 0.6f, DefaultMargin * 0.6f);
            Text.ForceMeshUpdate();
        }
    }

    public void SetSelected(bool selected)
    {
        _isSelected = selected;
        Renderer.sprite = selected ? _selectedSprite : _normalSprite;
    }

    /// <summary>
    /// 设置按钮尺寸。传入 null 则恢复为文本自适应（文本 × 1.5）。
    /// </summary>
    public void SetSize(Vector2? size)
    {
        if (size.HasValue)
        {
            ApplySize(size.Value);
        }
        else if (Text != null)
        {
            Text.ForceMeshUpdate();
            ApplySize(CalcAutoSize(Text));
        }
    }

    public void SetText(string text)
    {
        if (Text != null) { Text.text = text; Text.ForceMeshUpdate(); }
    }

    public void SetVisible(bool visible) => GameObject.SetActive(visible);
    public void SetPosition(Vector3 localPos) => GameObject.transform.localPosition = localPos;
    public void SetColor(Color color) => Renderer.color = color.ToUnityColor();

    public void SetOnClick(Action onClick)
    {
        Button.OnClick.RemoveAllListeners();
        var localOnClick = onClick;
        Button.OnClick.AddListener((UnityAction)(() =>
        {
            try { SoundManager.Instance.PlaySound(LightInDark.UI.Window.VanillaAsset.FindSoundClip("UI_Select"), false, 0.8f); } catch { }
            localOnClick?.Invoke();
        }));
    }

    public void Destroy() => Object.Destroy(GameObject);
}

// =====================================================================
// HudUIWindow — 便捷窗口，封装 MetaScreen
// =====================================================================

/// <summary>
/// 便捷窗口封装，基于 MetaScreen。
/// 提供简单的 AddText / AddButton API。
/// </summary>
public class HudUIWindow
{
    public MetaScreen Screen { get; private set; }
    public GameObject GameObject { get; private set; }
    private readonly List<GameObject> _contentObjects = new();
    private float _currentY;
    private Vector2 _windowSize;

    private HudUIWindow(MetaScreen screen, Vector2 windowSize)
    {
        Screen = screen;
        GameObject = screen.gameObject;
        _windowSize = windowSize;
        _currentY = windowSize.y * 0.5f - 0.5f;
    }

    /// <summary>
    /// 创建窗口
    /// </summary>
    public static HudUIWindow Create(string title = "", Vector2? size = null, Transform? parent = null)
    {
        parent ??= HudManager.Instance?.transform;
        if (parent == null) throw new InvalidOperationException("HudManager 未就绪");

        var windowSize = size ?? new Vector2(5f, 3f);
        var screen = MetaScreen.GenerateWindow(windowSize, parent, new Vector3(0f, 0f, -50f),
            withBlackScreen: true, closeOnClickOutside: false,
            background: BackgroundSetting.Modern, withCloseButton: true);

        return new HudUIWindow(screen, windowSize);
    }

    public void Close() => Screen.CloseScreen();

    public void ClearContent()
    {
        foreach (var obj in _contentObjects)
        {
            if (obj != null) Object.Destroy(obj);
        }
        _contentObjects.Clear();
        _currentY = _windowSize.y * 0.5f - 0.5f;
    }

    public TextMeshPro AddText(string text, float fontSize = 2f,
        TextAlignmentOptions alignment = TextAlignmentOptions.Center)
    {
        var obj = new GameObject("Text");
        obj.layer = LayerExpansion.GetUILayer();
        obj.transform.SetParent(Screen.transform, false);
        obj.transform.localPosition = new Vector3(0f, _currentY, -1f);

        var tmp = HudUITextHelper.Create(obj.transform);
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.fontSizeMax = fontSize;
        tmp.fontSizeMin = fontSize * 0.5f;
        tmp.m_fontSizeBase = 3f;
        tmp.alignment = alignment;
        tmp.color = UnityEngine.Color.white;
        tmp.enableWordWrapping = alignment != TextAlignmentOptions.Center;
        tmp.ForceMeshUpdate();

        float textWidth = _windowSize.x - 0.6f;
        float textHeight = tmp.preferredHeight + 0.1f;
        tmp.rectTransform.sizeDelta = new Vector2(textWidth, textHeight);
        tmp.rectTransform.pivot = new Vector2(0.5f, 0.5f);
        tmp.ForceMeshUpdate();

        textHeight = tmp.preferredHeight + 0.1f;
        tmp.rectTransform.sizeDelta = new Vector2(textWidth, textHeight);
        tmp.ForceMeshUpdate();

        _currentY -= tmp.preferredHeight + 0.2f;
        _contentObjects.Add(obj);
        return tmp;
    }

    public HudUIButton AddButton(string text, Action onClick, Vector2? size = null, Color? color = null)
    {
        var btn = HudUIButton.Create(Screen.transform, text, size ?? ButtonSize.Rectangle,
            () => { onClick?.Invoke(); });
        btn.SetPosition(new Vector3(0f, _currentY, -1f));

        var btnHeight = (size ?? ButtonSize.Rectangle).y;
        _currentY -= btnHeight + 0.15f;

        if (color != null) btn.SetColor(color.Value);
        _contentObjects.Add(btn.GameObject);
        return btn;
    }

    public HudUIButton AddColorButton(string text, Action onClick, Vector2? size = null, Color? color = null)
    {
        var btn = HudUIButton.CreateColorButton(Screen.transform, text, size ?? ButtonSize.Rectangle,
            () => { onClick?.Invoke(); }, color);
        btn.SetPosition(new Vector3(0f, _currentY, -1f));

        var btnHeight = (size ?? ButtonSize.Rectangle).y;
        _currentY -= btnHeight + 0.15f;

        _contentObjects.Add(btn.GameObject);
        return btn;
    }

    public void AddMargin(float height) => _currentY -= height;
}
