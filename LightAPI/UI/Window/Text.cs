using System;
using LightInDark.Core;
using TMPro;
using UnityEngine;

namespace LightInDark.UI.Window;

/// <summary>
/// 文本对齐方式
/// </summary>
public enum TextAlignment
{
    Left = TextAlignmentOptions.Left,
    Right = TextAlignmentOptions.Right,
    Center = TextAlignmentOptions.Center,
}

/// <summary>
/// 字体样式
/// </summary>
[Flags]
public enum FontStyle
{
    Normal = FontStyles.Normal,
    Bold = FontStyles.Bold,
    Italic = FontStyles.Italic,
}

/// <summary>
/// 字体大小设定
/// </summary>
public class FontSize
{
    public float FontSizeDefault { get; }
    public float FontSizeMin { get; }
    public float FontSizeMax { get; }
    public bool AllowAutoSizing { get; }

    public FontSize(float fontSize, float fontSizeMin, float fontSizeMax, bool allowAutoSizing = true)
    {
        try
        {
            FontSizeDefault = fontSize;
            FontSizeMin = fontSizeMin;
            FontSizeMax = fontSizeMax;
            AllowAutoSizing = allowAutoSizing;
        }
        catch (Exception ex)
        {
            LightLogger.LogError("FontSize.FontSize", ex);
        }
    }

    public FontSize(float fontSize, bool allowAutoSizing = true)
        : this(fontSize, fontSize, fontSize, allowAutoSizing)
    {
    }
}

/// <summary>
/// 字体资源标识
/// </summary>
public enum FontAsset
{
    Prespawn,
    Barlow,
    Gothic,
    GothicMasked,
    Oblong,
    OblongMasked,
}

/// <summary>
/// 字体信息
/// </summary>
public class Font
{
    public TMP_FontAsset? FontAsset { get; init; }
    public Material? FontMaterial { get; init; }
    public bool IsMasked { get; init; }
}

/// <summary>
/// 文本属性
/// </summary>
public class TextAttribute
{
    public TextAlignment Alignment { get; init; }
    public Font Font { get; init; }
    public FontStyle Style { get; init; }
    public FontSize FontSize { get; init; }
    public Size Size { get; init; }
    public Color Color { get; init; }
    public bool IsFlexible { get; init; }
    public bool Wrapping { get; init; } = false;
    public float? OutlineWidth { get; init; } = null;

    public TextAttribute(TextAlignment alignment, Font font, FontStyle style, FontSize fontSize, Size size, Color color, bool isFlexible, float? outlineWidth = null)
    {
        try
        {
            Alignment = alignment;
            Font = font;
            Style = style;
            FontSize = fontSize;
            Size = size;
            Color = color;
            IsFlexible = isFlexible;
            OutlineWidth = outlineWidth;
        }
        catch (Exception ex)
        {
            LightLogger.LogError("TextAttribute.TextAttribute", ex);
        }
    }

    public TextAttribute(TextAttribute other)
    {
        try
        {
            Alignment = other.Alignment;
            Font = other.Font;
            Style = other.Style;
            FontSize = other.FontSize;
            Size = other.Size;
            Color = other.Color;
            IsFlexible = other.IsFlexible;
            Wrapping = other.Wrapping;
            OutlineWidth = other.OutlineWidth;
        }
        catch (Exception ex)
        {
            LightLogger.LogError("TextAttribute.TextAttribute(copy)", ex);
        }
    }
}

/// <summary>
/// 文本属性参数（位标志）
/// </summary>
[Flags]
public enum AttributeParams
{
    None = 0,
    // 对齐
    AlignmentLeft = 0b0001,
    AlignmentRight = 0b0010,
    AlignmentMask = AlignmentLeft | AlignmentRight,
    // 字体
    FontStandard = 0b0100,
    FontOblong = 0b1000,
    FontBarlow = 0b11000,
    FontMask = FontStandard | FontOblong | FontBarlow,
    // 材质
    MaterialBared = 0b100000,
    MaterialMask = MaterialBared,
    // 样式
    StyleBold = 0b1000000,
    // 弹性
    IsFlexible = 0b10000000,

    // 预设
    Standard = FontStandard | MaterialBared,
    StandardMasked = FontStandard,
    StandardBold = FontStandard | MaterialBared | StyleBold,
    StandardBoldMasked = FontStandard | StyleBold,
    StandardLeft = FontStandard | MaterialBared | AlignmentLeft,
    StandardBoldLeft = FontStandard | MaterialBared | StyleBold | AlignmentLeft,
    StandardBoldNonFlexible = FontStandard | MaterialBared | StyleBold,
    StandardBaredBoldLeft = FontStandard | MaterialBared | StyleBold | AlignmentLeft,
    StandardBaredLeft = FontStandard | MaterialBared | AlignmentLeft,
    StandardBaredBoldNonFlexible = FontStandard | MaterialBared | StyleBold,
}

[Flags]
public enum AttributeTemplateFlag
{
    None = 0,
    AlignmentLeft = AttributeParams.AlignmentLeft,
    AlignmentRight = AttributeParams.AlignmentRight,
    AlignmentMask = AttributeParams.AlignmentMask,
    FontStandard = AttributeParams.FontStandard,
    FontOblong = AttributeParams.FontOblong,
    FontBarlow = AttributeParams.FontBarlow,
    FontMask = AttributeParams.FontMask,
    MaterialBared = AttributeParams.MaterialBared,
    MaterialMask = AttributeParams.MaterialMask,
    StyleBold = AttributeParams.StyleBold,
    IsFlexible = AttributeParams.IsFlexible,
}

/// <summary>
/// 预定义文本属性资源
/// </summary>
public enum AttributeAsset
{
    StandardMediumMasked,
    StandardLargeWideMasked,
    CenteredBold,
    CenteredBoldFixed,
    LeftBoldFixed,
    DocumentStandard,
    DocumentBold,
    DocumentTitle,
    DocumentSubtitle1,
    DocumentSubtitle2,
    OptionsTitle,
    OptionsTitleHalf,
    OptionsTitleShortest,
    OptionsValue,
    OptionsValueShorter,
    OptionsButton,
    OptionsButtonLonger,
    OptionsButtonMedium,
    OptionsFlexible,
    OptionsGroupTitle,
    MetaRoleButton,
    OverlayTitle,
    OverlayContent,
    OverlayBold,
}

/// <summary>
/// 文本组件接口
/// </summary>
public interface TextComponent
{
    string GetString();
    string TextForCompare => GetString();
}

/// <summary>
/// 原始文本组件
/// </summary>
public class RawTextComponent : TextComponent
{
    private readonly string _text;
    public RawTextComponent(string text)
    {
        try
        {
            _text = text;
        }
        catch (Exception ex)
        {
            LightLogger.LogError("RawTextComponent.RawTextComponent", ex);
        }
    }
    public string GetString()
    {
        try
        {
            return _text;
        }
        catch (Exception ex)
        {
            LightLogger.LogError("RawTextComponent.GetString", ex);
            return default;
        }
    }
}

/// <summary>
/// 翻译文本组件
/// </summary>
public class TranslateTextComponent : TextComponent
{
    private readonly string _key;
    public TranslateTextComponent(string key)
    {
        try
        {
            _key = key;
        }
        catch (Exception ex)
        {
            LightLogger.LogError("TranslateTextComponent.TranslateTextComponent", ex);
        }
    }
    public string GetString()
    {
        try
        {
            return Language.Language.Translate(_key, _key);
        }
        catch (Exception ex)
        {
            LightLogger.LogError("TranslateTextComponent.GetString", ex);
            return default;
        }
    }
}

/// <summary>
/// 着色文本组件
/// </summary>
public class ColorTextComponent : TextComponent
{
    private readonly Color _color;
    private readonly TextComponent _inner;

    public ColorTextComponent(Color color, TextComponent inner)
    {
        try
        {
            _color = color;
            _inner = inner;
        }
        catch (Exception ex)
        {
            LightLogger.LogError("ColorTextComponent.ColorTextComponent", ex);
        }
    }

    public string GetString()
    {
        try
        {
            var c = _color;
            byte r = (byte)(c.R * 255f);
            byte g = (byte)(c.G * 255f);
            byte b = (byte)(c.B * 255f);
            byte a = (byte)(c.A * 255f);
            return $"<color=#{r:X2}{g:X2}{b:X2}{a:X2}>{_inner.GetString()}</color>";
        }
        catch (Exception ex)
        {
            LightLogger.LogError("ColorTextComponent.GetString", ex);
            return default;
        }
    }
}

/// <summary>
/// 懒加载文本组件
/// </summary>
public class LazyTextComponent : TextComponent
{
    private readonly Func<string> _supplier;
    private readonly string? _textForCompare;
    public LazyTextComponent(Func<string> supplier, string? textForCompare = null)
    {
        try
        {
            _supplier = supplier;
            _textForCompare = textForCompare;
        }
        catch (Exception ex)
        {
            LightLogger.LogError("LazyTextComponent.LazyTextComponent", ex);
        }
    }
    public string GetString()
    {
        try
        {
            return _supplier();
        }
        catch (Exception ex)
        {
            LightLogger.LogError("LazyTextComponent.GetString", ex);
            return default;
        }
    }
    public string TextForCompare
    {
        get
        {
            try
            {
                return _textForCompare ?? GetString();
            }
            catch (Exception ex)
            {
                LightLogger.LogError("LazyTextComponent.TextForCompare", ex);
                return default;
            }
        }
    }
}
