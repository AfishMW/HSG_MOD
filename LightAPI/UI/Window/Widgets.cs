using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using Object = UnityEngine.Object;
using Button = UnityEngine.UI.Button;

namespace LightInDark.UI.Window;

// =====================================================================
// AbstractGUIWidget — 基类，实现 Anchor 定位
// =====================================================================

public abstract class AbstractGUIWidget : GUIWidget
{
    private readonly GUIAlignment _alignment;
    public override GUIAlignment Alignment => _alignment;

    protected AbstractGUIWidget(GUIAlignment alignment)
    {
        _alignment = alignment;
    }

    protected static float CalcWidth(GUIAlignment alignment, float myWidth, float maxWidth)
        => Calc(alignment, myWidth, maxWidth, GUIAlignment.Left, GUIAlignment.Right);

    protected static float CalcHeight(GUIAlignment alignment, float myHeight, float maxHeight)
        => Calc(alignment, myHeight, maxHeight, GUIAlignment.Bottom, GUIAlignment.Top);

    private static float Calc(GUIAlignment alignment, float myParam, float maxParam, GUIAlignment lower, GUIAlignment higher)
    {
        if ((alignment & lower) != 0) return (myParam - maxParam) * 0.5f;
        if ((alignment & higher) != 0) return (maxParam - myParam) * 0.5f;
        return 0f;
    }
}

// =====================================================================
// GUIEmptyWidget
// =====================================================================

public class GUIEmptyWidget : AbstractGUIWidget
{
    public static GUIEmptyWidget Default { get; } = new();

    public GUIEmptyWidget(GUIAlignment alignment = GUIAlignment.Left) : base(alignment) { }

    public override GameObject? Instantiate(Size size, out Size actualSize)
    {
        actualSize = Size.Zero;
        return null;
    }
}

// =====================================================================
// NoSGUIMargin
// =====================================================================

public class NoSGUIMargin : AbstractGUIWidget
{
    private readonly Vector2 _margin;

    public NoSGUIMargin(GUIAlignment alignment, Vector2 margin) : base(alignment)
    {
        _margin = margin;
    }

    public override GameObject? Instantiate(Size size, out Size actualSize)
    {
        actualSize = new Size(_margin);
        return null;
    }
}

// =====================================================================
// LogicGUIWidget
// =====================================================================

public class LogicGUIWidget : GUIWidget
{
    private readonly GUIWidget _inner;
    private readonly Action<GameObject?, Size> _logic;

    public LogicGUIWidget(GUIWidget inner, Action<GameObject?, Size> logic)
    {
        _inner = inner;
        _logic = logic;
    }

    public override GUIAlignment Alignment => _inner.Alignment;

    public override GameObject? Instantiate(Size size, out Size actualSize)
    {
        var obj = _inner.Instantiate(size, out actualSize);
        _logic.Invoke(obj, actualSize);
        return obj;
    }

    public override GameObject? Instantiate(Anchor anchor, Size size, out Size actualSize)
    {
        var obj = _inner.Instantiate(anchor, size, out actualSize);
        _logic.Invoke(obj, actualSize);
        return obj;
    }

    public override bool PostponesConsideringSize
    {
        get => _inner.PostponesConsideringSize;
        set => _inner.PostponesConsideringSize = value;
    }
}

// =====================================================================
// GUISizeFixer
// =====================================================================

public class GUISizeFixer : AbstractGUIWidget
{
    private readonly Size _size;
    private readonly GUIWidget _inner;

    public GUISizeFixer(GUIWidget inner, Size size) : base(inner.Alignment)
    {
        _size = size;
        _inner = inner;
    }

    public override GameObject? Instantiate(Size size, out Size actualSize)
    {
        actualSize = _size;
        return _inner.Instantiate(size, out _);
    }
}

// =====================================================================
// NoSGUIText — 文本 Widget（基于 TextMeshPro，克隆 StandardTextPrefab）
// =====================================================================

public class NoSGUIText : AbstractGUIWidget
{
    protected TextAttribute Attr;
    protected TextComponent? Text;
    public Action<TextMeshPro>? PostBuilder;

    public NoSGUIText(GUIAlignment alignment, TextAttribute attribute, TextComponent? text) : base(alignment)
    {
        Attr = attribute;
        Text = text;
    }

    protected static void ReflectAttribute(TextAttribute attr, TextMeshPro text, float width)
    {
        text.color = attr.Color.ToUnityColor();
        text.alignment = (TextAlignmentOptions)attr.Alignment;
        text.fontStyle = (FontStyles)attr.Style;
        text.fontSize = attr.FontSize.FontSizeDefault;
        text.fontSizeMin = attr.FontSize.FontSizeMin;
        text.fontSizeMax = attr.FontSize.FontSizeMax;
        text.enableAutoSizing = attr.FontSize.AllowAutoSizing;
        text.enableWordWrapping = attr.Wrapping;
        text.rectTransform.sizeDelta = new Vector2(Mathf.Min(width, attr.Size.Width), attr.Size.Height);
        text.rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        text.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        text.rectTransform.pivot = new Vector2(0.5f, 0.5f);

        if (attr.Font?.FontAsset != null)
        {
            text.font = attr.Font.FontAsset;
            if (attr.Font.FontMaterial != null)
                text.fontMaterial = attr.Font.FontMaterial;
        }
    }

    public override GameObject? Instantiate(Size size, out Size actualSize)
    {
        if (Text == null)
        {
            actualSize = Size.Zero;
            return null;
        }

        var text = Object.Instantiate(VanillaAsset.StandardTextPrefab, null);
        text.transform.localPosition = Vector3.zero;

        ReflectAttribute(Attr, text, size.Width);
        text.text = Text.GetString();
        text.sortingOrder = 10;
        text.ForceMeshUpdate();

        if (Attr.IsFlexible)
        {
            if (text.enableWordWrapping)
            {
                float w = Mathf.Min(text.rectTransform.sizeDelta.x, text.textBounds.size.x);
                float h = text.textBounds.size.y;
                text.rectTransform.sizeDelta = new Vector2(w, h);
            }
            else
            {
                float pw = Mathf.Min(text.rectTransform.sizeDelta.x, text.preferredWidth);
                float ph = Mathf.Min(text.rectTransform.sizeDelta.y, text.preferredHeight);
                text.rectTransform.sizeDelta = new Vector2(pw, ph);
            }
            text.ForceMeshUpdate();
        }

        PostBuilder?.Invoke(text);

        actualSize = new Size(text.rectTransform.sizeDelta);
        return text.gameObject;
    }
}

// =====================================================================
// GUIButton — 按钮 Widget（SpriteRenderer 背景 + TextMeshPro 文本）
// =====================================================================

public class GUIButton : NoSGUIText
{
    public GUIClickAction? OnClick { get; init; }
    public GUIClickAction? OnRightClick { get; init; }
    public GUIClickAction? OnMouseOver { get; init; }
    public GUIClickAction? OnMouseOut { get; init; }
    public Color? ButtonColor { get; init; }
    public Color? SelectedColor { get; init; }
    public float? TextMargin { get; init; } = null;

    private float GetTextMargin() => TextMargin ?? 0.26f;

    public GUIButton(GUIAlignment alignment, TextAttribute attribute, TextComponent text) : base(alignment, attribute, text)
    {
        Attr = attribute;
    }

    public override GameObject? Instantiate(Size size, out Size actualSize)
    {
        var inner = base.Instantiate(size, out actualSize)!;

        var margin = GetTextMargin();

        // 按钮背景 SpriteRenderer
        var button = UnityHelper.CreateObject<SpriteRenderer>("Button", null, Vector3.zero, LayerExpansion.GetUILayer());
        button.sprite = VanillaAsset.TextButtonSprite;
        button.drawMode = SpriteDrawMode.Sliced;
        button.tileMode = SpriteTileMode.Continuous;
        button.size = actualSize.ToUnityVector() + new Vector2(margin * 0.84f, margin * 0.84f);

        // 文本放在按钮之上
        inner.transform.SetParent(button.transform);
        inner.transform.localPosition += new Vector3(0, 0, -0.05f);

        // 碰撞体
        var collider = button.gameObject.AddComponent<BoxCollider2D>();
        collider.size = actualSize.ToUnityVector() + new Vector2(margin * 0.6f, margin * 0.6f);
        collider.isTrigger = true;

        // PassiveButton
        var passiveButton = button.gameObject.SetUpButton(true, button, ButtonColor, SelectedColor);
        var clickable = new GUIClickable(passiveButton);

        if (OnClick != null)
            passiveButton.OnClick.AddListener((UnityAction)(() => OnClick(clickable)));
        if (OnMouseOver != null)
            passiveButton.OnMouseOver.AddListener((UnityAction)(() => OnMouseOver(clickable)));
        if (OnMouseOut != null)
            passiveButton.OnMouseOut.AddListener((UnityAction)(() => OnMouseOut(clickable)));

        actualSize.Width += margin + 0.1f;
        actualSize.Height += margin + 0.1f;

        return button.gameObject;
    }
}

// =====================================================================
// NoSGUIImage — 图片 Widget（SpriteRenderer）
// =====================================================================

public class NoSGUIImage : AbstractGUIWidget
{
    private readonly Sprite _sprite;
    private readonly FuzzySize _size;
    public Color? TintColor;
    public GUIClickAction? OnClick;
    public bool IsMasked;

    public NoSGUIImage(GUIAlignment alignment, Sprite sprite, FuzzySize size,
        Color? color = null, GUIClickAction? onClick = null, bool isMasked = true) : base(alignment)
    {
        _sprite = sprite;
        _size = size;
        TintColor = color;
        OnClick = onClick;
        IsMasked = isMasked;
    }

    public override GameObject? Instantiate(Size size, out Size actualSize)
    {
        if (_sprite == null)
        {
            actualSize = Size.Zero;
            return null;
        }

        var renderer = UnityHelper.CreateObject<SpriteRenderer>("Image", null, Vector3.zero, LayerExpansion.GetUILayer());
        renderer.sprite = _sprite;
        renderer.sortingOrder = 10;
        if (IsMasked) renderer.maskInteraction = SpriteMaskInteraction.VisibleInsideMask;

        var spriteSize = renderer.sprite.bounds.size;
        float scale = Mathf.Min(
            _size.Width.HasValue ? (_size.Width.Value / spriteSize.x) : float.MaxValue,
            _size.Height.HasValue ? (_size.Height.Value / spriteSize.y) : float.MaxValue
        );
        renderer.transform.localScale = Vector3.one * scale;

        if (TintColor != null) renderer.color = TintColor.Value.ToUnityColor();

        actualSize = new Size(spriteSize.x * scale, spriteSize.y * scale);

        if (OnClick != null)
        {
            var button = renderer.gameObject.SetUpButton(true, renderer, TintColor ?? Color.White);
            var collider = renderer.gameObject.AddComponent<BoxCollider2D>();
            collider.size = renderer.sprite.bounds.size;
            collider.isTrigger = true;

            var clickable = new GUIClickable(button);
            button.OnClick.AddListener((UnityAction)(() => { OnClick.Invoke(clickable); }));
        }

        return renderer.gameObject;
    }
}

// =====================================================================
// WidgetsHolder / VerticalWidgetsHolder / HorizontalWidgetsHolder
// =====================================================================

public abstract class WidgetsHolder : AbstractGUIWidget
{
    protected IEnumerable<GUIWidget> Widgets;

    protected WidgetsHolder(GUIAlignment alignment, IEnumerable<GUIWidget?> widgets) : base(alignment)
    {
        Widgets = widgets.Where(w => w != null)!;
    }
}

public class VerticalWidgetsHolder : WidgetsHolder
{
    public float? FixedWidth { get; init; } = null;

    public VerticalWidgetsHolder(GUIAlignment alignment, IEnumerable<GUIWidget?> widgets) : base(alignment, widgets) { }
    public VerticalWidgetsHolder(GUIAlignment alignment, params GUIWidget?[] widgets) : base(alignment, widgets) { }

    public override GameObject? Instantiate(Size size, out Size actualSize)
    {
        var results = Widgets.Select(c => (c.Instantiate(size, out var acSize), acSize, c)).ToArray();

        float maxWidth = 0f;
        float sumHeight = 0f;
        float? tempHeight = null;

        foreach (var r in results)
        {
            maxWidth = Mathf.Max(maxWidth, r.acSize.Width);
            if (r.c.PostponesConsideringSize)
            {
                tempHeight = Mathf.Max(tempHeight ?? r.acSize.Height, r.acSize.Height);
            }
            else
            {
                sumHeight += Mathf.Max(r.acSize.Height, tempHeight ?? r.acSize.Height);
                tempHeight = 0f;
            }
        }

        if (FixedWidth != null) maxWidth = FixedWidth.Value;

        var myObj = UnityHelper.CreateObject("WidgetsHolder", null, Vector3.zero, LayerExpansion.GetUILayer());

        float height = sumHeight * 0.5f;
        float? maxHeight = null;

        foreach (var r in results)
        {
            if (r.Item1 != null)
            {
                r.Item1.transform.SetParent(myObj.transform);
                r.Item1.transform.localPosition = new Vector3(
                    CalcWidth(r.c.Alignment, r.acSize.Width, maxWidth),
                    height - r.acSize.Height * 0.5f,
                    0f);
            }

            if (r.c.PostponesConsideringSize)
            {
                maxHeight = Mathf.Max(maxHeight ?? r.acSize.Height, r.acSize.Height);
            }
            else
            {
                height -= Mathf.Max(maxHeight ?? r.acSize.Height, r.acSize.Height);
                maxHeight = null;
            }
        }

        actualSize = new Size(maxWidth, sumHeight);
        return myObj;
    }
}

public class HorizontalWidgetsHolder : WidgetsHolder
{
    public float? FixedHeight { get; init; } = null;

    public HorizontalWidgetsHolder(GUIAlignment alignment, IEnumerable<GUIWidget?> widgets) : base(alignment, widgets) { }
    public HorizontalWidgetsHolder(GUIAlignment alignment, params GUIWidget?[] widgets) : base(alignment, widgets) { }

    public override GameObject? Instantiate(Size size, out Size actualSize)
    {
        var results = Widgets.Select(c => (c.Instantiate(size, out var acSize), acSize, c)).ToArray();

        float sumWidth = 0f;
        float maxHeight = 0f;
        float? tempWidth = null;

        foreach (var r in results)
        {
            maxHeight = Mathf.Max(maxHeight, r.acSize.Height);
            if (r.c.PostponesConsideringSize)
            {
                tempWidth = Mathf.Max(tempWidth ?? r.acSize.Width, r.acSize.Width);
            }
            else
            {
                sumWidth += Mathf.Max(r.acSize.Width, tempWidth ?? r.acSize.Width);
                tempWidth = 0f;
            }
        }

        if (FixedHeight != null) maxHeight = FixedHeight.Value;

        var myObj = UnityHelper.CreateObject("WidgetsHolder", null, Vector3.zero, LayerExpansion.GetUILayer());

        float width = -sumWidth * 0.5f;
        float? maxWidth = null;

        foreach (var r in results)
        {
            if (r.Item1 != null)
            {
                r.Item1.transform.SetParent(myObj.transform);
                r.Item1.transform.localPosition = new Vector3(
                    width + r.acSize.Width * 0.5f,
                    CalcHeight(r.c.Alignment, r.acSize.Height, maxHeight),
                    0f);
            }

            if (r.c.PostponesConsideringSize)
            {
                maxWidth = Mathf.Max(maxWidth ?? r.acSize.Width, r.acSize.Width);
            }
            else
            {
                width += Mathf.Max(maxWidth ?? r.acSize.Width, r.acSize.Width);
                maxWidth = null;
            }
        }

        actualSize = new Size(sumWidth, maxHeight);
        return myObj;
    }
}
