using System;
using System.Collections.Generic;
using HarmonyLib;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using LightInDark.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Light.UI.HudUI;
using LightInDark;
using LightInDark.Utilities;
using UColor = UnityEngine.Color;

namespace Light.Patches;

/// <summary>
/// 在设置菜单中添加 "Light" 选项卡。
/// 照搬 Nebula 的 OptionStart 模式。
/// </summary>
[HarmonyPatch(typeof(OptionsMenuBehaviour), nameof(OptionsMenuBehaviour.Start))]
public static class SettingsTabPatch
{
    private static GameObject? _lightTabContent;
    private static OptionsMenuBehaviour? _menu;
    private static readonly List<LightOptionButton> _buttons = new();
    private static int _lightTabIndex = -1;

    public static void Postfix(OptionsMenuBehaviour __instance)
    {
        try
        {
            _menu = __instance;

            // 创建 Light 选项卡内容面板
            _lightTabContent = new GameObject("LightTabContent");
            _lightTabContent.transform.SetParent(__instance.transform);
            _lightTabContent.transform.localScale = Vector3.one;
            _lightTabContent.SetActive(false);

            var tabs = new List<TabGroup>(__instance.Tabs.ToArray());
            if (tabs.Count < 2) return;

            // 照搬 Nebula：替换最后一个 tab（DoneButton）为 Light tab
            tabs[^1] = GameObject.Instantiate(tabs[1], null);
            var lightButton = tabs[^1];
            lightButton.gameObject.name = "LightButton";
            lightButton.transform.SetParent(tabs[0].transform.parent);
            lightButton.transform.localScale = Vector3.one;
            lightButton.Content = _lightTabContent;

            // 修改文字
            var textObj = lightButton.transform.FindChild("Text_TMP")?.gameObject;
            if (textObj != null)
            {
                var tr = textObj.GetComponent<TextTranslatorTMP>();
                if (tr != null) tr.enabled = false;
                var tmp = textObj.GetComponent<TextMeshPro>();
                if (tmp != null) tmp.text = "Light";
            }

            _lightTabIndex = tabs.Count - 1;

            // 绑定点击事件 — 照搬 Nebula
            var pb = lightButton.gameObject.GetComponent<PassiveButton>();
            if (pb != null)
            {
                pb.OnClick = new UnityEngine.UI.Button.ButtonClickedEvent();
                pb.OnClick.AddListener((UnityEngine.Events.UnityAction)(() =>
                {
                    __instance.OpenTabGroup(_lightTabIndex);
                    RebuildButtons();
                }));
            }

            // 重排 tab 按钮位置 — 照搬 Nebula
            float y = tabs[0].transform.localPosition.y;
            float z = tabs[0].transform.localPosition.z;
            if (tabs.Count == 4)
            {
                for (int i = 0; i < 3; i++)
                    tabs[i].transform.localPosition = new Vector3(1.7f * (float)(i - 1), y, z);
                tabs[3].transform.localPosition = new Vector3(1.7f * (float)(2 - 1), y, z);
            }
            else
            {
                float spacing = 1.7f;
                float startX = -spacing * (tabs.Count - 1) * 0.5f;
                for (int i = 0; i < tabs.Count; i++)
                    tabs[i].transform.localPosition = new Vector3(startX + spacing * i, y, z);
            }

            __instance.Tabs = new Il2CppReferenceArray<TabGroup>(tabs.ToArray());

            // 注册示例按钮
            AddToggleButton("示例开关", false, on => LightLogger.Log($"[Settings] 示例开关: {on}"));
            AddSelectorButton("示例选择器", new[] { "选项A", "选项B", "选项C" }, 0, idx => LightLogger.Log($"[Settings] 选择器: {idx}"));
            AddActionButton("示例动作按钮", () =>
            {
                LightInDark.Utilities.LightUtils.ShowCustomDisconnectWindow("这是一个示例动作按钮！");
            });
            AddEmptyButton("空按钮（无效果）");
        }
        catch (Exception ex)
        {
            LightLogger.LogError("[SettingsTabPatch.Postfix]", ex);
        }
    }

    // ════════════════════════════════════════════
    //  公共方法
    // ════════════════════════════════════════════

    public static LightOptionButton AddToggleButton(string label, bool initialValue, Action<bool> onToggle)
    {
        var btn = new LightOptionButton(label, initialValue ? "启用" : "禁用");
        btn.OnClick = () =>
        {
            btn.IsOn = !btn.IsOn;
            btn.ValueText = btn.IsOn ? "启用" : "禁用";
            onToggle?.Invoke(btn.IsOn);
            RebuildButtons();
        };
        _buttons.Add(btn);
        if (_lightTabContent != null && _lightTabContent.activeSelf) RebuildButtons();
        return btn;
    }

    public static LightOptionButton AddSelectorButton(string label, string[] options, int initialIndex, Action<int> onSelect)
    {
        var btn = new LightOptionButton(label, options[initialIndex]);
        btn.SelectorOptions = options;
        btn.SelectorIndex = initialIndex;
        btn.OnClick = () =>
        {
            btn.SelectorIndex = (btn.SelectorIndex + 1) % options.Length;
            btn.ValueText = options[btn.SelectorIndex];
            onSelect?.Invoke(btn.SelectorIndex);
            RebuildButtons();
        };
        _buttons.Add(btn);
        if (_lightTabContent != null && _lightTabContent.activeSelf) RebuildButtons();
        return btn;
    }

    public static LightOptionButton AddActionButton(string label, Action onClick)
    {
        var btn = new LightOptionButton(label, "");
        btn.OnClick = () => onClick?.Invoke();
        _buttons.Add(btn);
        if (_lightTabContent != null && _lightTabContent.activeSelf) RebuildButtons();
        return btn;
    }

    public static LightOptionButton AddEmptyButton(string label)
    {
        var btn = new LightOptionButton(label, "");
        btn.OnClick = () => { };
        _buttons.Add(btn);
        if (_lightTabContent != null && _lightTabContent.activeSelf) RebuildButtons();
        return btn;
    }

    public static LightOptionButton AddHudWindowButton(string label, Action<List<HudUI.ButtonOption>> windowBuilder)
    {
        var btn = new LightOptionButton(label, "");
        btn.OnClick = () =>
        {
            try
            {
                var options = new List<HudUI.ButtonOption>();
                windowBuilder?.Invoke(options);
                HudUI.OpenButtonWindow(label, options.ToArray());
            }
            catch (Exception ex) { LightLogger.LogError("[SettingsTabPatch.AddHudWindowButton]", ex); }
        };
        _buttons.Add(btn);
        if (_lightTabContent != null && _lightTabContent.activeSelf) RebuildButtons();
        return btn;
    }

    // ════════════════════════════════════════════
    //  内部：构建按钮 UI
    // ════════════════════════════════════════════

    private static void RebuildButtons()
    {
        try
        {
            if (_lightTabContent == null) return;

            // 清除旧子对象
            for (int i = 0; i < _lightTabContent.transform.childCount; i++)
            {
                var child = _lightTabContent.transform.GetChild(i);
                if (child != null) GameObject.Destroy(child.gameObject);
            }

            // 照搬 Nebula 布局参数
            float startY = 1.8f;
            float spacing = -0.45f;

            for (int i = 0; i < _buttons.Count; i++)
            {
                var info = _buttons[i];
                CreateOptionButton(_lightTabContent.transform, i, startY + spacing * i, info);
            }
        }
        catch (Exception ex)
        {
            LightLogger.LogError("[SettingsTabPatch.RebuildButtons]", ex);
        }
    }

    private static void CreateOptionButton(Transform parent, int index, float yPos, LightOptionButton info)
    {
        try
        {
            // 照搬 Nebula：克隆设置页面已有的按钮作为模板
            // 找到已有选项行作为模板
            var template = parent.parent?.GetComponentsInChildren<PassiveButton>(true)
                ?.FirstOrDefault(b => b.name != "DoneButton" && b.name != "LightButton");
            GameObject go;
            PassiveButton pb;

            if (template != null)
            {
                // 克隆已有按钮
                go = GameObject.Instantiate(template.gameObject, parent);
                go.name = $"LightOption_{index}";
                go.transform.localPosition = new Vector3(0f, yPos, -1f);
                go.transform.localScale = Vector3.one;
                go.SetActive(true);

                // 修改文本
                var tmp = go.GetComponentInChildren<TextMeshPro>(true);
                if (tmp != null)
                {
                    tmp.text = string.IsNullOrEmpty(info.ValueText)
                        ? info.Label
                        : $"{info.Label} : {info.ValueText}";
                }

                pb = go.GetComponent<PassiveButton>();
                if (pb != null)
                {
                    pb.OnClick = new UnityEngine.UI.Button.ButtonClickedEvent();
                    pb.OnClick.AddListener((UnityEngine.Events.UnityAction)(() => info.OnClick?.Invoke()));
                }
            }
            else
            {
                // Fallback：手动创建
                go = new GameObject($"LightOption_{index}");
                go.transform.SetParent(parent);
                go.transform.localPosition = new Vector3(0f, yPos, -1f);
                go.transform.localScale = Vector3.one;

                var textGo = new GameObject("Label");
                textGo.transform.SetParent(go.transform);
                textGo.transform.localPosition = Vector3.zero;
                textGo.transform.localScale = Vector3.one;
                var tmp = textGo.AddComponent<TextMeshPro>();
                tmp.text = string.IsNullOrEmpty(info.ValueText)
                    ? info.Label
                    : $"{info.Label} : {info.ValueText}";
                tmp.fontSize = 2.5f;
                tmp.alignment = TextAlignmentOptions.Center;
                tmp.color = UColor.white;
                tmp.enableWordWrapping = false;
                tmp.raycastTarget = false;

                pb = go.AddComponent<PassiveButton>();
                var collider = go.AddComponent<BoxCollider2D>();
                collider.size = new Vector2(4f, 0.35f);
                collider.isTrigger = true;

                pb.OnClick = new UnityEngine.UI.Button.ButtonClickedEvent();
                pb.OnClick.AddListener((UnityEngine.Events.UnityAction)(() => info.OnClick?.Invoke()));
            }
        }
        catch (Exception ex)
        {
            LightLogger.LogError("[SettingsTabPatch.CreateOptionButton]", ex);
        }
    }
}

/// <summary>
/// Light 设置选项按钮数据。
/// </summary>
public class LightOptionButton
{
    public string Label { get; set; }
    public string ValueText { get; set; }
    public bool IsOn { get; set; }
    public string[]? SelectorOptions { get; set; }
    public int SelectorIndex { get; set; }
    public Action? OnClick { get; set; }

    public LightOptionButton(string label, string valueText)
    {
        Label = label;
        ValueText = valueText;
    }
}
