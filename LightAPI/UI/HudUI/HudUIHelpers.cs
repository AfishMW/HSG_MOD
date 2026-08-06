using System;
using TMPro;
using UnityEngine;

namespace LightInDark.UI.HudUI;

/// <summary>
/// HudUI 高级辅助方法，对应 Nebula 的 MetaUI。
/// </summary>
public static class HudUI
{
    public readonly struct ButtonOption
    {
        public readonly string Label;
        public readonly Action OnClick;
        public readonly Color? Color;
        public readonly Vector2? Size;

        public ButtonOption(string label, Action onClick, Color? color = null, Vector2? size = null)
        {
            Label = label;
            OnClick = onClick;
            Color = color;
            Size = size;
        }
    }

    /// <summary>
    /// 打开一个按钮选择窗口
    /// </summary>
    public static HudUIWindow OpenButtonWindow(string title, params ButtonOption[] options)
    {
        var window = HudUIWindow.Create(title, new Vector2(4.5f, 1.5f + options.Length * 0.65f));

        window.AddText(title, 2.2f, TextAlignmentOptions.Center);
        window.AddMargin(0.2f);

        foreach (var opt in options)
        {
            var localOpt = opt;
            window.AddButton(localOpt.Label, () =>
            {
                localOpt.OnClick?.Invoke();
                window.Close();
            }, localOpt.Size ?? ButtonSize.Rectangle, localOpt.Color);
        }

        return window;
    }

    /// <summary>
    /// 打开一个确认对话框（是/否）
    /// </summary>
    public static HudUIWindow OpenConfirmDialog(string message, Action onConfirm, Action? onCancel = null)
    {
        var window = HudUIWindow.Create("", WindowSize.Confirm);

        window.AddText(message, 1.5f, TextAlignmentOptions.Center);
        window.AddMargin(0.2f);

        var localOnConfirm = onConfirm;
        var localOnCancel = onCancel;

        window.AddButton("是", () =>
        {
            localOnConfirm?.Invoke();
            window.Close();
        }, ButtonSize.Rectangle, Color.Green);

        window.AddButton("否", () =>
        {
            localOnCancel?.Invoke();
            window.Close();
        }, ButtonSize.Rectangle, Color.Red);

        return window;
    }

    /// <summary>
    /// 打开一个消息对话框（仅含"确定"按钮）
    /// </summary>
    public static HudUIWindow OpenMessageDialog(string message, Action? onClose = null)
    {
        var window = HudUIWindow.Create("", WindowSize.Small);

        window.AddText(message, 1.5f, TextAlignmentOptions.Center);
        window.AddMargin(0.2f);

        var localOnClose = onClose;
        window.AddButton("确定", () =>
        {
            localOnClose?.Invoke();
            window.Close();
        }, ButtonSize.Rectangle);

        return window;
    }

    /// <summary>
    /// 打开一个文本展示窗口（带标题）
    /// </summary>
    public static HudUIWindow OpenTextWindow(string title, string text)
    {
        var window = HudUIWindow.Create(title, new Vector2(5f, 3.5f));

        window.AddText(title, 2.2f, TextAlignmentOptions.Center);
        window.AddMargin(0.2f);
        window.AddText(text, 1.2f, TextAlignmentOptions.Left);

        return window;
    }

    /// <summary>
    /// 打开一个自定义内容窗口（支持自定义尺寸）
    /// </summary>
    public static HudUIWindow OpenCustomWindow(string title, Action<HudUIWindow> builder, Vector2? windowSize = null)
    {
        var window = HudUIWindow.Create(title, windowSize ?? WindowSize.Standard);

        if (!string.IsNullOrEmpty(title))
        {
            window.AddText(title, 2.2f, TextAlignmentOptions.Center);
            window.AddMargin(0.2f);
        }

        builder?.Invoke(window);
        return window;
    }
}
