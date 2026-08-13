using System;
using System.Collections.Generic;
using LightInDark.UI.Window;
using UnityEngine;
using LightInDark.Core;
using Color = LightInDark.Color;

namespace Light.UI.Window;

/// <summary>
/// 高级 UI 辅助类
/// </summary>
public static class MetaUI
{
    /// <summary>
    /// 按钮窗口选项
    /// </summary>
    public readonly struct ButtonOption
    {
        public readonly string Label;
        public readonly Action OnClick;
        public readonly Color? Color;

        public ButtonOption(string label, Action onClick, Color? color = null)
        {
            try
            {
                Label = label;
                OnClick = onClick;
                Color = color;
            }
            catch (Exception ex)
            {
                LightLogger.LogError("[MetaUI.ButtonOption]", ex);
            }
        }
    }

    /// <summary>
    /// 打开一个按钮选择窗口
    /// </summary>
    public static MetaScreen OpenButtonWindow(string title, params ButtonOption[] options)
    {
        try
        {
            var gui = LIDGUI.Instance;
            var attr = gui.GetAttribute(AttributeAsset.CenteredBold);
            var widgets = new List<GUIWidget?>();

            widgets.Add(gui.RawText(GUIAlignment.Center, gui.GetAttribute(AttributeAsset.DocumentTitle), title));
            widgets.Add(gui.VerticalMargin(0.3f));

            foreach (var opt in options)
            {
                var localOpt = opt;
                widgets.Add(gui.RawButton(
                    GUIAlignment.Center,
                    attr,
                    localOpt.Label,
                    clickable => { localOpt.OnClick?.Invoke(); },
                    color: localOpt.Color
                ));
                widgets.Add(gui.VerticalMargin(0.1f));
            }

            widgets.Add(gui.VerticalMargin(0.2f));
            widgets.Add(gui.RawButton(
                GUIAlignment.Center,
                gui.GetAttribute(AttributeAsset.DocumentStandard),
                "关闭",
                clickable => { },
                color: Color.Gray
            ));

            var content = gui.VerticalHolder(GUIAlignment.Center, widgets.ToArray());

            var screen = MetaScreen.GenerateWindow(
                new Vector2(4.5f, 3f),
                GetParent(),
                new Vector3(0f, 0f, -50f),
                withCloseButton: true,
                withBlackScreen: true
            );

            screen.SetWidget(content, out _);
            return screen;
        }
        catch (Exception ex)
        {
            LightLogger.LogError("[MetaUI.OpenButtonWindow]", ex); return default;
        }
    }

    /// <summary>
    /// 打开一个确认对话框（是/否）
    /// </summary>
    public static MetaScreen OpenConfirmDialog(string message, Action onConfirm, Action? onCancel = null)
    {
        try
        {
            var gui = LIDGUI.Instance;
            var attr = gui.GetAttribute(AttributeAsset.CenteredBold);
            var localOnCancel = onCancel;

            var content = gui.VerticalHolder(
                GUIAlignment.Center,
                gui.RawText(GUIAlignment.Center, attr, message),
                gui.VerticalMargin(0.3f),
                gui.HorizontalHolder(
                    GUIAlignment.Center,
                    gui.RawButton(GUIAlignment.Center, attr, "是",
                        clickable => { onConfirm?.Invoke(); }, color: Color.Green),
                    gui.HorizontalMargin(0.3f),
                    gui.RawButton(GUIAlignment.Center, attr, "否",
                        clickable => { localOnCancel?.Invoke(); }, color: Color.Red)
                )
            );

            var screen = MetaScreen.GenerateWindow(
                new Vector2(3.9f, 1.5f),
                GetParent(),
                new Vector3(0f, 0f, -50f),
                withCloseButton: false,
                withBlackScreen: true
            );

            screen.SetWidget(content, out _);
            return screen;
        }
        catch (Exception ex)
        {
            LightLogger.LogError("[MetaUI.OpenConfirmDialog]", ex); return default;
        }
    }

    /// <summary>
    /// 打开一个消息对话框（仅含"确定"按钮）
    /// </summary>
    public static MetaScreen OpenMessageDialog(string message, Action? onClose = null)
    {
        try
        {
            var gui = LIDGUI.Instance;
            var attr = gui.GetAttribute(AttributeAsset.CenteredBold);
            var localOnClose = onClose;

            var content = gui.VerticalHolder(
                GUIAlignment.Center,
                gui.RawText(GUIAlignment.Center, attr, message),
                gui.VerticalMargin(0.3f),
                gui.RawButton(GUIAlignment.Center, attr, "确定",
                    clickable => { localOnClose?.Invoke(); })
            );

            var screen = MetaScreen.GenerateWindow(
                new Vector2(3.5f, 1.5f),
                GetParent(),
                new Vector3(0f, 0f, -50f),
                withCloseButton: false,
                withBlackScreen: true
            );

            screen.SetWidget(content, out _);
            return screen;
        }
        catch (Exception ex)
        {
            LightLogger.LogError("[MetaUI.OpenMessageDialog]", ex); return default;
        }
    }

    /// <summary>
    /// 打开一个文本展示窗口（带标题）
    /// </summary>
    public static MetaScreen OpenTextWindow(string title, string text)
    {
        try
        {
            var gui = LIDGUI.Instance;

            var content = gui.VerticalHolder(
                GUIAlignment.Center,
                gui.RawText(GUIAlignment.Center, gui.GetAttribute(AttributeAsset.DocumentTitle), title),
                gui.VerticalMargin(0.2f),
                gui.RawText(GUIAlignment.Left, gui.GetAttribute(AttributeAsset.DocumentStandard), text)
            );

            var screen = MetaScreen.GenerateWindow(
                new Vector2(5f, 3f),
                GetParent(),
                new Vector3(0f, 0f, -50f),
                withCloseButton: true,
                withBlackScreen: true
            );

            screen.SetWidget(content, out _);
            return screen;
        }
        catch (Exception ex)
        {
            LightLogger.LogError("[MetaUI.OpenTextWindow]", ex); return default;
        }
    }

    /// <summary>
    /// 打开一个自定义内容窗口
    /// </summary>
    public static MetaScreen OpenCustomWindow(string title, GUIWidget contentWidget, Vector2? windowSize = null)
    {
        try
        {
            var gui = LIDGUI.Instance;

            var content = gui.VerticalHolder(
                GUIAlignment.Center,
                gui.RawText(GUIAlignment.Center, gui.GetAttribute(AttributeAsset.DocumentTitle), title),
                gui.VerticalMargin(0.2f),
                contentWidget
            );

            var screen = MetaScreen.GenerateWindow(
                windowSize ?? new Vector2(5f, 3f),
                GetParent(),
                new Vector3(0f, 0f, -50f),
                withCloseButton: true,
                withBlackScreen: true
            );

            screen.SetWidget(content, out _);
            return screen;
        }
        catch (Exception ex)
        {
            LightLogger.LogError("[MetaUI.OpenCustomWindow]", ex); return default;
        }
    }

    private static Transform GetParent()
    {
        try
        {
            if (HudManager.Instance != null)
                return HudManager.Instance.transform;

            var go = new GameObject("MetaUI_TempParent");
            UnityEngine.Object.DontDestroyOnLoad(go);
            return go.transform;
        }
        catch (Exception ex)
        {
            LightLogger.LogError("[MetaUI.GetParent]", ex); return default;
        }
    }
}
