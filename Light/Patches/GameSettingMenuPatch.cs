using System;
using HarmonyLib;
using LightInDark.Core;
using LightInDark.Language;
using LightInDark.UI.Window;
using Light.UI.Settings;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using Object = UnityEngine.Object;

namespace Light.Patches;

/// <summary>
/// 原版规则编辑界面（GameSettingMenu）增强（参考 Nebula，复制粘贴式改造）：
///  - 把原版三个选项卡按钮（预设/游戏设置/角色设置）克隆成三份，删除原版按钮，
///    顶部排开：原版设置 | MOD设置 | 预设（SelectButton 高亮，当前页常亮）；
///  - 原版设置 → 原版游戏设置页；预设 → 原版预设页；MOD设置 → 内嵌的模组配置页；
///  - MOD 配置页 = 克隆原版"游戏设置"内容页（含原版滚动容器），清空后内嵌 LIDGUI 内容；
///  - 最底层加半透明遮罩，遮住层级混乱的背景。
/// </summary>
[HarmonyPatch]
public static class GameSettingMenuPatch
{
    private static readonly PassiveButton?[] _tabButtons = new PassiveButton?[3];
    private static int _activeTab = -1;
    private static GameObject? _modPage;
    private static GameObject? _mask;

    private static Transform? FindChildRecursive(Transform parent, string name)
    {
        try
        {
            if (parent == null) return null;
            for (int i = 0; i < parent.childCount; i++)
            {
                var child = parent.GetChild(i);
                if (child.name == name) return child;
                var r = FindChildRecursive(child, name);
                if (r != null) return r;
            }
            return null;
        }
        catch (Exception ex)
        {
            LightLogger.LogError("[GameSettingMenuPatch.FindChildRecursive]", ex);
            return null;
        }
    }

    private static PassiveButton? FindButtonByName(GameSettingMenu menu, params string[] names)
    {
        foreach (var n in names)
        {
            var t = FindChildRecursive(menu.transform, n);
            if (t != null)
            {
                var pb = t.GetComponent<PassiveButton>();
                if (pb != null) return pb;
            }
        }
        return null;
    }

    private static PassiveButton? FindButtonByText(GameSettingMenu menu, string keyword)
    {
        try
        {
            var btns = menu.GetComponentsInChildren<PassiveButton>(true);
            foreach (var pb in btns)
            {
                if (pb == null) continue;
                string text = GetButtonText(pb);
                if (!string.IsNullOrEmpty(text) && text.Contains(keyword)) return pb;
            }
        }
        catch (Exception ex)
        {
            LightLogger.LogError("[GameSettingMenuPatch.FindButtonByText]", ex);
        }
        return null;
    }

    private static string GetButtonText(PassiveButton btn)
    {
        try
        {
            TextMeshPro? tmp = null;
            var fp = btn.transform.FindChild("FontPlacer");
            if (fp != null && fp.childCount > 0)
                tmp = fp.GetChild(0).GetComponent<TextMeshPro>();
            if (tmp == null)
                tmp = btn.GetComponentInChildren<TextMeshPro>(true);
            return tmp != null ? tmp.text : "";
        }
        catch { return ""; }
    }

    [HarmonyPatch(typeof(GameSettingMenu), "Start")]
    [HarmonyPostfix]
    public static void StartPostfix(GameSettingMenu __instance)
    {
        try
        {
            EnsureMask(__instance);
            SetupTabs(__instance);
        }
        catch (Exception ex)
        {
            LightLogger.LogError("[GameSettingMenuPatch.StartPostfix]", ex);
        }
    }

    [HarmonyPatch(typeof(GameSettingMenu), "Close")]
    [HarmonyPostfix]
    public static void ClosePostfix()
    {
        ModSettingsScreen.Close();
        _modPage = null;
        for (int i = 0; i < _tabButtons.Length; i++) _tabButtons[i] = null;
        _activeTab = -1;
        _mask = null;
    }

    /// <summary>最底层半透明遮罩：遮住层级混乱的背景，只让设置 UI 在上面。</summary>
    private static void EnsureMask(GameSettingMenu menu)
    {
        try
        {
            if (_mask != null) return;
            var maskObj = new GameObject("LightMask");
            maskObj.layer = LayerExpansion.GetUILayer();
            maskObj.transform.SetParent(menu.transform, false);
            maskObj.transform.localPosition = new Vector3(0f, 0f, -30f);
            var sr = maskObj.AddComponent<SpriteRenderer>();
            sr.sprite = Light.UI.Window.VanillaAsset.FullScreenSprite;
            sr.drawMode = SpriteDrawMode.Sliced;
            sr.size = new Vector2(30f, 30f);
            sr.color = new UnityEngine.Color(0f, 0f, 0f, 0.35f);
            _mask = maskObj;
        }
        catch (Exception ex)
        {
            LightLogger.LogError("[GameSettingMenuPatch.EnsureMask]", ex);
        }
    }

    private static void SetupTabs(GameSettingMenu menu)
    {
        try
        {
            var settingsBtn = FindButtonByName(menu, "GameSettingsButton", "GameSettings", "SettingsButton")
                ?? FindButtonByText(menu, "游戏设置") ?? FindButtonByText(menu, "Game Setting");
            var presetsBtn = FindButtonByName(menu, "GamePresetsButton", "GamePresets", "PresetsButton")
                ?? FindButtonByText(menu, "预设") ?? FindButtonByText(menu, "Preset");
            var rolesBtn = FindButtonByName(menu, "RoleSettingsButton", "RolesSettingsButton", "RolesButton", "RoleSettings")
                ?? FindButtonByText(menu, "角色设置") ?? FindButtonByText(menu, "Role Setting");

            LightLogger.Log($"[GameSettingMenuPatch] 找到选项卡: settings={(settingsBtn != null ? settingsBtn.gameObject.name : "null")} " +
                $"presets={(presetsBtn != null ? presetsBtn.gameObject.name : "null")} roles={(rolesBtn != null ? rolesBtn.gameObject.name : "null")}");

            // 删除原版按钮（隐藏），克隆三份替换
            foreach (var b in new[] { settingsBtn, presetsBtn, rolesBtn })
                if (b != null) b.gameObject.SetActive(false);

            for (int i = 0; i < 3; i++)
            {
                var old = FindChildRecursive(menu.transform, $"LightTab{i}");
                if (old != null) Object.Destroy(old.gameObject);
                _tabButtons[i] = null;
            }

            var template = rolesBtn ?? settingsBtn ?? presetsBtn;
            if (template == null)
            {
                LightLogger.LogWarning("[GameSettingMenuPatch] 未找到任何原版选项卡按钮，跳过增强");
                return;
            }

            var parent = template.transform.parent;
            var baseAnchor = new Vector2(0.5f, 0.86f);
            var aspT = template.GetComponent<AspectPosition>();
            if (aspT != null) baseAnchor = aspT.anchorPoint;

            var labels = new[]
            {
                Language.Translate("gss.tab.vanilla", "原版设置"),
                Language.Translate("gss.tab.mod", "MOD设置"),
                Language.Translate("gss.tab.preset", "预设"),
            };

            var scalerList = Object.FindObjectOfType<SlicedAspectScaler>();
            for (int i = 0; i < 3; i++)
            {
                var clone = Object.Instantiate(template.gameObject, parent);
                clone.name = $"LightTab{i}";
                clone.SetActive(true);
                var cond = clone.GetComponent<ConditionalHide>();
                if (cond != null) Object.Destroy(cond);
                // 顶部排开：x 三等分，y 保持原版选项卡行
                var asp = clone.GetComponent<AspectPosition>();
                if (asp != null)
                {
                    asp.anchorPoint = new Vector2(0.34f + i * 0.16f, baseAnchor.y);
                    asp.AdjustPosition();
                }
                var pb = clone.GetComponent<PassiveButton>();
                if (pb == null) continue;
                SetButtonText(pb, labels[i]);
                pb.OnClick = new UnityEngine.UI.Button.ButtonClickedEvent();
                int idx = i;
                pb.OnClick.AddListener((UnityAction)(() => SelectTab(menu, idx)));
                _tabButtons[i] = pb;
                if (scalerList != null)
                {
                    var scaled = clone.GetComponent<AspectScaledAsset>();
                    if (scaled != null) scalerList.objectsToScale.Add(scaled);
                }
            }

            SelectTab(menu, 0);
        }
        catch (Exception ex)
        {
            LightLogger.LogError("[GameSettingMenuPatch.SetupTabs]", ex);
        }
    }

    /// <summary>切换选项卡：高亮当前 + 切换内容。</summary>
    private static void SelectTab(GameSettingMenu menu, int index)
    {
        try
        {
            _activeTab = index;
            for (int i = 0; i < _tabButtons.Length; i++)
            {
                var b = _tabButtons[i];
                if (b != null) b.SelectButton(i == index);
            }
            switch (index)
            {
                case 0: HideModPage(); menu.ChangeTab(1, false); break;
                case 1: ShowModPage(menu); break;
                default: HideModPage(); menu.ChangeTab(0, false); break;
            }
        }
        catch (Exception ex)
        {
            LightLogger.LogError("[GameSettingMenuPatch.SelectTab]", ex);
        }
    }

    private static void HideVanillaTabs(GameSettingMenu menu)
    {
        foreach (var n in new[] { "PresetsTab", "GameSettingsTab", "RoleSettingsTab", "GamePresetsTab", "GameSettings" })
        {
            var t = FindChildRecursive(menu.transform, n);
            if (t != null) t.gameObject.SetActive(false);
        }
    }

    private static void ShowModPage(GameSettingMenu menu)
    {
        try
        {
            if (_modPage == null) BuildModPage(menu);
            if (_modPage == null) return;
            HideVanillaTabs(menu);
            _modPage.SetActive(true);
        }
        catch (Exception ex)
        {
            LightLogger.LogError("[GameSettingMenuPatch.ShowModPage]", ex);
        }
    }

    private static void HideModPage()
    {
        try
        {
            if (_modPage != null)
            {
                try { _modPage.SetActive(false); } catch { _modPage = null; }
            }
        }
        catch { }
    }

    /// <summary>克隆原版"游戏设置"内容页（含原版滚动容器），清空后内嵌 MOD 配置 UI。</summary>
    private static void BuildModPage(GameSettingMenu menu)
    {
        try
        {
            var gom = menu.GetComponentInChildren<GameOptionsMenu>(true);
            if (gom == null)
            {
                LightLogger.LogWarning("[GameSettingMenuPatch] 未找到 GameOptionsMenu，无法克隆设置页");
                return;
            }

            var sliderInner = gom.gameObject;
            _modPage = Object.Instantiate(sliderInner, sliderInner.transform.parent);
            _modPage.name = "LightModSettings";
            _modPage.transform.localPosition = sliderInner.transform.localPosition;

            // 清空子对象（保留原版滚动条）
            for (int i = _modPage.transform.childCount - 1; i >= 0; i--)
            {
                var child = _modPage.transform.GetChild(i);
                if (child.GetComponent<Scroller>() != null) continue;
                Object.Destroy(child.gameObject);
            }

            var clonedGom = _modPage.GetComponent<GameOptionsMenu>();
            if (clonedGom != null) clonedGom.enabled = false;

            _modPage.SetActive(false);
            ModSettingsScreen.Open(_modPage.transform);
        }
        catch (Exception ex)
        {
            LightLogger.LogError("[GameSettingMenuPatch.BuildModPage]", ex);
        }
    }

    private static void SetButtonText(PassiveButton? btn, string text)
    {
        try
        {
            if (btn == null) return;
            TextMeshPro? tmp = null;
            var fp = btn.transform.FindChild("FontPlacer");
            if (fp != null && fp.childCount > 0)
                tmp = fp.GetChild(0).GetComponent<TextMeshPro>();
            if (tmp == null)
                tmp = btn.GetComponentInChildren<TextMeshPro>(true);
            if (tmp != null)
            {
                tmp.text = text;
                var trs = btn.GetComponentsInChildren<TextTranslatorTMP>(true);
                foreach (var tr in trs)
                    if (tr != null) tr.enabled = false;
            }
        }
        catch (Exception ex)
        {
            LightLogger.LogError("[GameSettingMenuPatch.SetButtonText]", ex);
        }
    }
}
