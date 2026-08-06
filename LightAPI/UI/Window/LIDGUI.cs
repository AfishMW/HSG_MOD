using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;

namespace LightInDark.UI.Window;

/// <summary>
/// GUI 工厂实现，对应 Nebula 的 NebulaGUIWidgetEngine
/// </summary>
public class LIDGUI : IGUI
{
    public static LIDGUI Instance { get; } = new();

    private readonly Dictionary<AttributeParams, TextAttribute> _allAttr = new();
    private readonly Dictionary<AttributeAsset, TextAttribute> _allAttrAsset = new();

    public GUIWidget EmptyWidget => GUIEmptyWidget.Default;

    public GUIWidget Text(GUIAlignment alignment, TextAttribute attribute, TextComponent text)
        => new NoSGUIText(alignment, attribute, text);

    public GUIWidget RealtimeText(GUIAlignment alignment, TextAttribute attribute, Func<string> textSupplier, float width)
    {
        var widget = new NoSGUIText(alignment, attribute, new RawTextComponent(""))
        {
            PostBuilder = text =>
            {
                text.text = textSupplier();
                text.ForceMeshUpdate();
                text.gameObject.AddComponent<RealtimeTextBehaviour>().TMP = text;
                text.gameObject.GetComponent<RealtimeTextBehaviour>().Supplier = textSupplier;
            }
        };
        return widget;
    }

    public GUIWidget Button(GUIAlignment alignment, TextAttribute attribute, TextComponent text, GUIClickAction onClick,
        GUIClickAction? onMouseOver = null, GUIClickAction? onMouseOut = null, GUIClickAction? onRightClick = null,
        Color? color = null, Color? selectedColor = null, float? margin = null)
        => new GUIButton(alignment, attribute, text)
        {
            OnClick = onClick,
            OnMouseOver = onMouseOver,
            OnMouseOut = onMouseOut,
            OnRightClick = onRightClick,
            ButtonColor = color,
            SelectedColor = selectedColor,
            TextMargin = margin
        };

    public GUIWidget Image(GUIAlignment alignment, Sprite sprite, FuzzySize size, GUIClickAction? onClick = null, GUIWidgetSupplier? overlay = null, bool isMasked = true)
        => new NoSGUIImage(alignment, sprite, size, null, onClick, isMasked);

    public GUIWidget RawText(GUIAlignment alignment, TextAttribute attribute, string rawText)
        => Text(alignment, attribute, new RawTextComponent(rawText));

    public GUIWidget RawButton(GUIAlignment alignment, TextAttribute attribute, string rawText, GUIClickAction onClick,
        GUIClickAction? onMouseOver = null, GUIClickAction? onMouseOut = null, GUIClickAction? onRightClick = null,
        Color? color = null, Color? selectedColor = null, float? margin = null)
        => Button(alignment, attribute, new RawTextComponent(rawText), onClick, onMouseOver, onMouseOut, onRightClick, color, selectedColor, margin);

    public GUIWidget VerticalMargin(float margin) => Margin(new FuzzySize(null, margin));
    public GUIWidget HorizontalMargin(float margin) => Margin(new FuzzySize(margin, null));

    public TextComponent TextComponent(Color color, string translationKey)
        => new ColorTextComponent(color, new TranslateTextComponent(translationKey));

    public GUIWidget VerticalHolder(GUIAlignment alignment, IEnumerable<GUIWidget?> widgets, float? fixedWidth = null)
        => new VerticalWidgetsHolder(alignment, widgets) { FixedWidth = fixedWidth };

    public GUIWidget VerticalHolder(GUIAlignment alignment, params GUIWidget?[] widgets)
        => new VerticalWidgetsHolder(alignment, widgets) { FixedWidth = null };

    public GUIWidget HorizontalHolder(GUIAlignment alignment, IEnumerable<GUIWidget?> widgets, float? fixedHeight = null)
        => new HorizontalWidgetsHolder(alignment, widgets) { FixedHeight = fixedHeight };

    public GUIWidget HorizontalHolder(GUIAlignment alignment, params GUIWidget?[] widgets)
        => new HorizontalWidgetsHolder(alignment, widgets) { FixedHeight = null };

    public GUIWidget Arrange(GUIAlignment alignment, IEnumerable<GUIWidget?> widgets, int perLine)
    {
        var rows = new List<GUIWidget>();
        var current = new List<GUIWidget>();

        foreach (var w in widgets)
        {
            if (w == null) continue;
            current.Add(w);
            if (current.Count == perLine)
            {
                rows.Add(HorizontalHolder(alignment, current.ToArray()));
                current.Clear();
            }
        }
        if (current.Count > 0)
            rows.Add(HorizontalHolder(alignment, current.ToArray()));

        return VerticalHolder(alignment, rows.ToArray());
    }

    public GUIWidget Margin(FuzzySize margin)
        => new NoSGUIMargin(GUIAlignment.Center, new Vector2(margin.Width ?? 0f, margin.Height ?? 0f));

    public TextAttribute GetAttribute(AttributeAsset attribute)
    {
        if (_allAttrAsset.TryGetValue(attribute, out var attr))
            return attr;

        attr = attribute switch
        {
            AttributeAsset.StandardMediumMasked => new(TextAlignment.Center, GetFont(FontAsset.GothicMasked), FontStyle.Bold, new(1.6f, 0.8f, 1.6f), new(1.45f, 0.3f), Color.White, false),
            AttributeAsset.StandardLargeWideMasked => new(TextAlignment.Center, GetFont(FontAsset.GothicMasked), FontStyle.Bold, new(1.7f, 1f, 1.7f), new(2.9f, 0.45f), Color.White, false),
            AttributeAsset.CenteredBold => new(TextAlignment.Center, GetFont(FontAsset.Gothic), FontStyle.Bold, new(1.9f, 1f, 1.9f), new(8f, 8f), Color.White, true),
            AttributeAsset.CenteredBoldFixed => new(TextAlignment.Center, GetFont(FontAsset.Gothic), FontStyle.Bold, new(1.9f, 1f, 1.9f), new(1.1f, 0.32f), Color.White, false),
            AttributeAsset.LeftBoldFixed => new(TextAlignment.Left, GetFont(FontAsset.Gothic), FontStyle.Bold, new(1.9f, 1f, 1.9f), new(1.1f, 0.32f), Color.White, false),
            AttributeAsset.DocumentStandard => new(TextAlignment.Left, GetFont(FontAsset.Gothic), FontStyle.Normal, new(1.2f, 0.6f, 1.2f), new(7f, 6f), Color.White, true),
            AttributeAsset.DocumentBold => new(TextAlignment.Left, GetFont(FontAsset.Gothic), FontStyle.Bold, new(1.2f, 0.6f, 1.2f), new(5f, 6f), Color.White, true),
            AttributeAsset.DocumentTitle => new(TextAlignment.Left, GetFont(FontAsset.Gothic), FontStyle.Bold, new(2.2f, 0.6f, 2.2f), new(5f, 6f), Color.White, true),
            AttributeAsset.DocumentSubtitle1 => new(TextAlignment.Left, GetFont(FontAsset.Gothic), FontStyle.Bold, new(1.9f, 0.6f, 1.9f), new(5f, 6f), Color.White, true),
            AttributeAsset.DocumentSubtitle2 => new(TextAlignment.Left, GetFont(FontAsset.Gothic), FontStyle.Bold, new(1.6f, 0.6f, 1.6f), new(5f, 6f), Color.White, true),
            AttributeAsset.OptionsTitle => new(TextAlignment.Left, GetFont(FontAsset.GothicMasked), FontStyle.Bold, new(1.8f, 1f, 2f), new(4f, 0.4f), Color.White, false),
            AttributeAsset.OptionsTitleHalf => new(TextAlignment.Left, GetFont(FontAsset.GothicMasked), FontStyle.Bold, new(1.8f, 1f, 2f), new(1.8f, 0.4f), Color.White, false),
            AttributeAsset.OptionsTitleShortest => new(TextAlignment.Left, GetFont(FontAsset.GothicMasked), FontStyle.Bold, new(1.8f, 1f, 2f), new(1f, 0.4f), Color.White, false),
            AttributeAsset.OptionsValue => new(TextAlignment.Center, GetFont(FontAsset.GothicMasked), FontStyle.Bold, new(1.8f, 1f, 2f), new(1.1f, 0.4f), Color.White, false),
            AttributeAsset.OptionsValueShorter => new(TextAlignment.Center, GetFont(FontAsset.GothicMasked), FontStyle.Bold, new(1.8f, 1f, 2f), new(0.7f, 0.4f), Color.White, false),
            AttributeAsset.OptionsButton => new(TextAlignment.Center, GetFont(FontAsset.GothicMasked), FontStyle.Bold, new(1.8f, 1f, 2f), new(0.32f, 0.22f), Color.White, false),
            AttributeAsset.OptionsButtonLonger => new(TextAlignment.Center, GetFont(FontAsset.GothicMasked), FontStyle.Bold, new(1.8f, 1f, 2f), new(1.8f, 0.22f), Color.White, false),
            AttributeAsset.OptionsButtonMedium => new(TextAlignment.Center, GetFont(FontAsset.GothicMasked), FontStyle.Bold, new(1.8f, 1f, 2f), new(0.9f, 0.22f), Color.White, false),
            AttributeAsset.OptionsFlexible => new(TextAlignment.Center, GetFont(FontAsset.GothicMasked), FontStyle.Bold, new(1.8f, 1f, 2f), new(6f, 0.22f), Color.White, true),
            AttributeAsset.OptionsGroupTitle => new(TextAlignment.Center, GetFont(FontAsset.GothicMasked), FontStyle.Normal, new(1.5f, 1f, 1.6f), new(6f, 0.22f), Color.White, true, 0f),
            AttributeAsset.MetaRoleButton => new(TextAlignment.Center, GetFont(FontAsset.GothicMasked), FontStyle.Bold, new(1.8f, 1f, 2f), new(1.4f, 0.26f), Color.White, false),
            AttributeAsset.OverlayTitle => new(TextAlignment.Left, GetFont(FontAsset.Gothic), FontStyle.Bold, new(1.8f, 1f, 1.8f), new(5f, 6f), Color.White, true),
            AttributeAsset.OverlayContent => new(TextAlignment.Left, GetFont(FontAsset.Gothic), FontStyle.Normal, new(1.5f, 1.1f, 1.5f), new(5f, 6f), Color.White, true),
            AttributeAsset.OverlayBold => new(TextAlignment.Left, GetFont(FontAsset.Gothic), FontStyle.Bold, new(1.5f, 1.1f, 1.5f), new(5f, 6f), Color.White, true),
            _ => new(TextAlignment.Center, GetFont(FontAsset.Gothic), FontStyle.Normal, new(2.2f, 1.2f, 2.5f), new(3f, 0.5f), Color.White, true),
        };
        _allAttrAsset[attribute] = attr;
        return attr;
    }

    public TextAttribute GenerateAttribute(AttributeParams param, Color color, FontSize fontSize, Size size)
    {
        var flag = (AttributeTemplateFlag)(int)param;

        TextAlignment alignment = (flag & AttributeTemplateFlag.AlignmentMask) switch
        {
            AttributeTemplateFlag.AlignmentLeft => TextAlignment.Left,
            AttributeTemplateFlag.AlignmentRight => TextAlignment.Right,
            _ => TextAlignment.Center
        };

        FontAsset fontAsset = (flag & (AttributeTemplateFlag.FontMask | AttributeTemplateFlag.MaterialMask)) switch
        {
            AttributeTemplateFlag.FontStandard | AttributeTemplateFlag.MaterialBared => FontAsset.Gothic,
            AttributeTemplateFlag.FontStandard => FontAsset.GothicMasked,
            AttributeTemplateFlag.FontOblong | AttributeTemplateFlag.MaterialBared => FontAsset.Oblong,
            AttributeTemplateFlag.FontOblong => FontAsset.OblongMasked,
            AttributeTemplateFlag.FontBarlow | AttributeTemplateFlag.MaterialBared => FontAsset.Barlow,
            AttributeTemplateFlag.FontBarlow => FontAsset.Barlow,
            _ => FontAsset.GothicMasked,
        };

        FontStyle style = 0;
        if ((flag & AttributeTemplateFlag.StyleBold) != 0) style |= FontStyle.Bold;

        bool isFlexible = (flag & AttributeTemplateFlag.IsFlexible) != 0;

        return new TextAttribute(alignment, GetFont(fontAsset), style, fontSize, size, color, isFlexible);
    }

    public Font GetFont(FontAsset fontAsset)
    {
        return fontAsset switch
        {
            FontAsset.Prespawn => new Font { FontAsset = VanillaAsset.PreSpawnFont, IsMasked = false },
            FontAsset.Barlow => new Font { FontAsset = VanillaAsset.VersionFont, IsMasked = false },
            FontAsset.Gothic => new Font { FontAsset = VanillaAsset.StandardTextPrefab?.font, IsMasked = false },
            FontAsset.GothicMasked => new Font { FontAsset = VanillaAsset.StandardTextPrefab?.font, FontMaterial = VanillaAsset.StandardMaskedFontMaterial, IsMasked = true },
            FontAsset.Oblong => new Font { FontAsset = VanillaAsset.BrookFont, IsMasked = false },
            FontAsset.OblongMasked => new Font { FontAsset = VanillaAsset.BrookFont, FontMaterial = VanillaAsset.StandardMaskedFontMaterial, IsMasked = true },
            _ => new Font { FontAsset = VanillaAsset.StandardTextPrefab?.font, IsMasked = false },
        };
    }

    public TextComponent RawTextComponent(string rawText) => new RawTextComponent(rawText);
    public TextComponent TranslateTextComponent(string translationKey) => new TranslateTextComponent(translationKey);
    public TextComponent ColorTextComponent(Color color, TextComponent component) => new ColorTextComponent(color, component);
}

/// <summary>
/// 实时文本更新组件
/// </summary>
public class RealtimeTextBehaviour : MonoBehaviour
{
    public TextMeshPro? TMP;
    public Func<string>? Supplier;
    private string? _lastText;

    public void Update()
    {
        if (TMP == null || Supplier == null) return;
        var current = Supplier();
        if (current != _lastText)
        {
            _lastText = current;
            TMP.text = current;
            TMP.ForceMeshUpdate();
        }
    }
}
