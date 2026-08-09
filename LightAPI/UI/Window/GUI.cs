using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using Button = UnityEngine.UI.Button;

namespace LightInDark.UI.Window;

/// <summary>
/// Widget 在容器中的对齐方式
/// </summary>
[Flags]
public enum GUIAlignment
{
    Center = 0b0000,
    Left = 0b0001,
    Right = 0b0010,
    Bottom = 0b0100,
    Top = 0b1000,
    TopLeft = Left | Top,
    TopRight = Right | Top,
    BottomLeft = Left | Bottom,
    BottomRight = Right | Bottom,
}

/// <summary>
/// 尺寸
/// </summary>
public struct Size
{
    public float Width;
    public float Height;

    public Size(float width, float height)
    {
        Width = width;
        Height = height;
    }

    public Size(Vector2 v) : this(v.x, v.y) { }

    public Vector2 ToUnityVector() => new(Width, Height);
    public static Size Zero => new(0f, 0f);
}

/// <summary>
/// 模糊尺寸（可只指定宽或高，另一个自适应）
/// </summary>
public struct FuzzySize
{
    public float? Width;
    public float? Height;

    public FuzzySize(float? width, float? height)
    {
        Width = width;
        Height = height;
    }
}

/// <summary>
/// 锚点：将屏幕上的点与空间上的点对齐
/// </summary>
/// <param name="pivot">屏幕上的基准点 (0-1)</param>
/// <param name="anchoredPosition">空间上的偏移位置</param>
public record Anchor(Vector2 pivot, Vector3 anchoredPosition)
{
    public static Anchor At(Vector2 pivot) => new(pivot, Vector3.zero);
    public static Anchor Center => new(new(0.5f, 0.5f), Vector3.zero);
}

/// <summary>
/// GUI Widget 基类
/// </summary>
public abstract class GUIWidget
{
    public abstract GUIAlignment Alignment { get; }
    public virtual Sprite? BackImage { get; set; }
    public virtual bool GrayoutedBackImage { get; set; }

    /// <summary>
    /// 实例化 Widget
    /// </summary>
    public abstract GameObject? Instantiate(Size size, out Size actualSize);

    /// <summary>
    /// 使用锚点实例化 Widget
    /// </summary>
    public virtual GameObject? Instantiate(Anchor anchor, Size size, out Size actualSize)
    {
        var obj = Instantiate(size, out actualSize);
        if (obj != null)
        {
            var localPos = anchor.anchoredPosition -
                new Vector3(
                    actualSize.Width * (anchor.pivot.x - 0.5f),
                    actualSize.Height * (anchor.pivot.y - 0.5f),
                    0f);
            obj.transform.localPosition = localPos;
        }
        return obj;
    }

    /// <summary>
    /// 延迟尺寸计算的 Widget（同一位置可叠加多个）
    /// </summary>
    public virtual bool PostponesConsideringSize { get; set; } = false;
}

/// <summary>
/// GUI 屏幕接口
/// </summary>
public interface IGUIScreen
{
    void SetWidget(GUIWidget? widget, out Size actualSize);
}

/// <summary>
/// Widget 供应器
/// </summary>
public delegate GUIWidget GUIWidgetSupplier();

/// <summary>
/// 点击回调
/// </summary>
public delegate void GUIClickAction(GUIClickable clickable);

/// <summary>
/// 可点击元素
/// </summary>
public class GUIClickable
{
    public PassiveButton? Button { get; init; }
    public GUIClickable(PassiveButton? button = null) => Button = button;
}

/// <summary>
/// GUI 工厂接口
/// </summary>
public interface IGUI
{
    GUIWidget EmptyWidget { get; }

    GUIWidget RawText(GUIAlignment alignment, TextAttribute attribute, string rawText)
        => Text(alignment, attribute, new RawTextComponent(rawText));

    GUIWidget LocalizedText(GUIAlignment alignment, TextAttribute attribute, string translationKey)
        => Text(alignment, attribute, new TranslateTextComponent(translationKey));

    GUIWidget Text(GUIAlignment alignment, TextAttribute attribute, TextComponent text);

    GUIWidget RealtimeText(GUIAlignment alignment, TextAttribute attribute, Func<string> textSupplier, float width);

    GUIWidget LocalizedButton(GUIAlignment alignment, TextAttribute attribute, string translationKey, GUIClickAction onClick,
        GUIClickAction? onMouseOver = null, GUIClickAction? onMouseOut = null, GUIClickAction? onRightClick = null,
        Color? color = null, Color? selectedColor = null, float? margin = null)
        => Button(alignment, attribute, new TranslateTextComponent(translationKey), onClick, onMouseOver, onMouseOut, onRightClick, color, selectedColor, margin);

    GUIWidget RawButton(GUIAlignment alignment, TextAttribute attribute, string rawText, GUIClickAction onClick,
        GUIClickAction? onMouseOver = null, GUIClickAction? onMouseOut = null, GUIClickAction? onRightClick = null,
        Color? color = null, Color? selectedColor = null, float? margin = null)
        => Button(alignment, attribute, new RawTextComponent(rawText), onClick, onMouseOver, onMouseOut, onRightClick, color, selectedColor, margin);

    GUIWidget Button(GUIAlignment alignment, TextAttribute attribute, TextComponent text, GUIClickAction onClick,
        GUIClickAction? onMouseOver = null, GUIClickAction? onMouseOut = null, GUIClickAction? onRightClick = null,
        Color? color = null, Color? selectedColor = null, float? margin = null);

    GUIWidget Image(GUIAlignment alignment, Sprite sprite, FuzzySize size, GUIClickAction? onClick = null, GUIWidgetSupplier? overlay = null, bool isMasked = true);

    GUIWidget ScrollView(GUIAlignment alignment, Size size, string? scrollerTag, GUIWidget? inner, out object artifact)
        => throw new NotImplementedException();

    GUIWidget VerticalHolder(GUIAlignment alignment, IEnumerable<GUIWidget?> widgets, float? fixedWidth = null);
    GUIWidget HorizontalHolder(GUIAlignment alignment, IEnumerable<GUIWidget?> widgets, float? fixedHeight = null);

    GUIWidget VerticalHolder(GUIAlignment alignment, params GUIWidget?[] widgets)
        => VerticalHolder(alignment, (IEnumerable<GUIWidget?>)widgets, null);

    GUIWidget HorizontalHolder(GUIAlignment alignment, params GUIWidget?[] widgets)
        => HorizontalHolder(alignment, (IEnumerable<GUIWidget?>)widgets, null);

    GUIWidget Arrange(GUIAlignment alignment, IEnumerable<GUIWidget?> widgets, int perLine);

    GUIWidget Margin(FuzzySize margin);
    GUIWidget VerticalMargin(float margin) => Margin(new(null, margin));
    GUIWidget HorizontalMargin(float margin) => Margin(new(margin, null));

    TextAttribute GetAttribute(AttributeAsset attribute);
    TextAttribute GenerateAttribute(AttributeParams param, Color color, FontSize fontSize, Size size);

    TextComponent RawTextComponent(string rawText);
    TextComponent TranslateTextComponent(string translationKey);
    TextComponent ColorTextComponent(Color color, TextComponent component);
}
