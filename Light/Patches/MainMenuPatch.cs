using AmongUs.Data;
using BepInEx.Unity.IL2CPP.Utils.Collections;
using HarmonyLib;
using Light.UI;
using Light.UI.Help;
using Light.UI.Window;
using Light.Utilities;
using LightInDark.Core;
using LightInDark.Utilities;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace Light.Patches;

/// <summary>
/// 定制主界面布局：重排左侧按钮、替换右侧面板、新增自定义屏幕与背景画廊等。
/// 注意：类级所有补丁属性均标注在方法上，因此必须在此提供类级 [HarmonyPatch]，
/// 否则 Harmony.PatchAll() 会跳过整个类（只扫描"类级带 [HarmonyPatch] 的类"）。
/// </summary>
[HarmonyPatch]
public static class MainMenuPatch
{
    private static bool _showingPanel;
    private static GameObject? _rightPanel;
    private static Vector3 _rightPanelOp;
    private static GameObject? _lightScreen;
    private static GameObject? _lightSubScreen;
    private static ImageGalleryPanel? _galleryPanel;
    private static bool _bgMoved;
    private static float _bgMoveDelay;
    private static bool _updaterChecked;

    private static GameObject? FindGO(string name) => GameObject.Find(name);

    /// <summary>安全取子物体，越界时返回 null，避免 GetChild 抛错。</summary>
    private static Transform? GetChild(Transform parent, int index)
    {
        if (parent == null || index < 0 || index >= parent.childCount) return null;
        return parent.GetChild(index);
    }

    private static Dictionary<string, PassiveButton> FindButtons()
    {
        try
        {
            var dict = new Dictionary<string, PassiveButton>();
            var leftPanel = GameObject.Find("LeftPanel");
            if (leftPanel == null) return dict;
            foreach (var b in leftPanel.GetComponentsInChildren<PassiveButton>(true))
                if (b != null && !dict.ContainsKey(b.name))
                    dict[b.name] = b;
            LightLogger.LogWarning($"[Light.UI] 找到 {dict.Count} 个按钮");
            return dict;
        }
        catch (Exception ex)
        {
            LightLogger.LogError("[MainMenuPatch.FindButtons]", ex);
            return new Dictionary<string, PassiveButton>();
        }
    }

    [HarmonyPatch(typeof(MainMenuManager), nameof(MainMenuManager.Start))]
    [HarmonyPostfix]
    public static void Postfix(MainMenuManager __instance)
    {
        try
        {
            // 清理上次创建的克隆按钮（场景切换后旧的被销毁但缩放器列表里仍有死引用）
            var oldScaler = Object.FindObjectOfType<SlicedAspectScaler>();
            if (oldScaler != null)
            {
                for (int i = oldScaler.objectsToScale.Count - 1; i >= 0; i--)
                {
                    var item = oldScaler.objectsToScale[i];
                    if (item == null || item.gameObject == null ||
                        item.gameObject.name.StartsWith("LightButton") ||
                        item.gameObject.name.StartsWith("CustomButton"))
                    {
                        oldScaler.objectsToScale.RemoveAt(i);
                    }
                }
            }
            // 清理可能残留的旧 GameObject
            for (int i = 0; i < 4; i++)
            {
                var old = FindGO($"CustomButton{i}");
                if (old != null) Object.Destroy(old);
            }
            var oldLight = FindGO("LightButton");
            if (oldLight != null) Object.Destroy(oldLight);

            _showingPanel = false;
            _rightPanel = null;
            _lightScreen = null;
            _lightSubScreen = null;
            _galleryPanel = null;
            LightLogger.LogWarning("[Light.UI] === 开始布局 ===");

            VanillaAsset.Preload();
            var modStamp = FindGO("ModStamp");
            if (modStamp != null)
            {
                modStamp.SetActive(true);
                modStamp.transform.localScale = Vector3.one * 0.06f;
                var sr = modStamp.GetComponent<SpriteRenderer>();
                sr?.sprite = ResourceHelper.LoadSpriteFromResource("Light.Resources.ModStamp.png");
            }
            var ambience = FindGO("Ambience");
            if (ambience != null)
            {
                ambience.transform.FindChild("PlayerParticles")?.gameObject.SetActive(false);
                if (ambience.transform.childCount > 0)
                    ambience.transform.GetChild(0).gameObject.SetActive(false);
            }
            _bgMoved = false;
            _bgMoveDelay = 0f;
            var bg = FindGO("BackgroundTexture");
            bg?.SetActive(true);

            var btns = FindButtons();

            var leftPanel = FindGO("LeftPanel");
            if (leftPanel != null)
            {
                var sizer = leftPanel.transform.FindChild("Sizer");
                if (sizer != null)
                {
                    var auLogo = sizer.GetComponent<AspectSize>();
                    if (auLogo != null)
                    {
                        auLogo.PercentWidth = 0.14f;
                        auLogo.DoSetUp();
                        auLogo.transform.localPosition += new Vector3(-0.8f, 0.25f, 0f);
                    }
                }
            }
            float height = 0.7f;
            if (btns.TryGetValue("NewsButton", out var news) && btns.TryGetValue("AcountButton", out var acct))
                height = news.transform.localPosition.y - acct.transform.localPosition.y;

            foreach (var kvp in btns)
            {
                var btn = kvp.Value;
                if (btn != null && Mathf.Abs(btn.transform.localPosition.x) < 0.1f)
                    btn.transform.localPosition += new Vector3(0f, height, 0f);
            }
            var divider = leftPanel?.transform.FindChild("Main Buttons")?.FindChild("Divider");
            divider?.localPosition += new Vector3(0f, height, 0f);
            if (leftPanel != null)
            {
                var reworked = UnityHelper.CreateObject<SpriteRenderer>(
                    "ReworkedLeftPanel", leftPanel.transform, new Vector3(0f, height * 0.5f, 0f));
                var oldSr = leftPanel.GetComponent<SpriteRenderer>();
                if (oldSr != null)
                {
                    reworked.sprite = oldSr.sprite;
                    reworked.tileMode = oldSr.tileMode;
                    reworked.drawMode = oldSr.drawMode;
                    reworked.size = oldSr.size + new Vector2(0f, 0.5f);
                    oldSr.enabled = false;
                }
            }
            var onlineRoot = __instance.mainMenuUI.transform
                .FindChild("AspectScaler")?.FindChild("Online Buttons");
            if (onlineRoot != null)
            {
                for (int i = 0; i < onlineRoot.childCount; i++)
                {
                    var child = onlineRoot.GetChild(i);
                    var scaler = child.Find("Scaler") ?? child;
                    if (scaler == null) continue;
                    for (int j = 0; j < scaler.childCount; j++)
                    {
                        var btn = scaler.GetChild(j);
                        var btnName = btn.name.ToLowerInvariant();
                        if (btnName.Contains("createlobby") || btnName.Contains("host"))
                            btn.localPosition = new(-1f, 0.5f, 0f);
                        else if (btnName.Contains("joingame") || btnName.Contains("join"))
                            btn.localPosition = new(1.5f, 0.5f, 0f);
                        else if (btnName.Contains("findgame"))
                            btn.localPosition = new(0f, -20f, 0f);
                        else if (btnName.Contains("line") || btnName.Contains("divider"))
                            btn.localPosition = new(0f, -20f, 0f);
                    }
                }
            }
            CreateLightButton(__instance, btns, height);
            AdjustIcons();
            ColorAllButtons();
            CloneTitleToMainMenu(__instance);
            if (leftPanel != null)
            {
                var mainButtons = leftPanel.transform.FindChild("Main Buttons");
                leftPanel.transform.localScale = new Vector3(0.7f, 0.7f, 0.7f);
                for (int i = 0; i < leftPanel.transform.childCount; i++)
                    leftPanel.transform.GetChild(i).SetParent(leftPanel.transform.parent);
                if (mainButtons != null)
                    mainButtons.transform.localPosition = new Vector3(-4.0f, 0f, 0f);
                leftPanel.SetActive(false);
            }
            FindGO("Divider")?.SetActive(false);
            SetupExtraButtons(__instance);
            ApplyButtonEffects(__instance);
            SetupRightPanel(__instance);
            SetupLightScreen(__instance);
            SetupSubScreen(__instance);
            SetupGalleryScreen(__instance);
            _galleryPanel?.RestoreBackground();
            MoveScreenTint(__instance);
            var decoTex = new Texture2D(1, 1);
            decoTex.SetPixel(0, 0, Color.white);
            decoTex.Apply();
            var decoSpr = Sprite.Create(decoTex, new Rect(0, 0, 1, 1),
                new Vector2(0.5f, 0.5f), 100f);
            var deco = UnityHelper.CreateObject<SpriteRenderer>("LightDecoLine",
                __instance.mainMenuUI.transform, new Vector3(0f, -3.2f, -2f));
            deco.sprite = decoSpr;
            deco.drawMode = SpriteDrawMode.Sliced;
            deco.size = new Vector2(8f, 0.02f);
            deco.color = new Color(1f, 1f, 1f, 0.12f);
            foreach (var obj in Resources.FindObjectsOfTypeAll<GameObject>())
                if (obj.name is "FreePlayButton" or "HowToPlayButton")
                    obj.transform.localPosition = new Vector3(0f, -20f, 0f);

            LightLogger.LogWarning("[Light.UI] === 布局完成 ===");

            // 制作人员/退出：在“LIGHT”下方一个间距处对齐（LIGHT 已占据“设置”下方一格）
            if (btns.TryGetValue("LightButton", out var lBtn) && lBtn != null &&
                btns.TryGetValue("AcountButton", out var aBtn) && aBtn != null &&
                btns.TryGetValue("CreditsButton", out var cBtn) && cBtn != null)
            {
                float spacingY = Mathf.Abs(lBtn.transform.position.y - aBtn.transform.position.y);
                float targetY = lBtn.transform.position.y - spacingY;
                var bounds = FindGO("BottomButtonBounds");
                Transform anchor = bounds != null ? bounds.transform : cBtn.transform.parent;
                if (anchor != null)
                {
                    float deltaY = targetY - cBtn.transform.position.y;
                    anchor.position += new Vector3(0f, deltaY, 0f);
                    LightLogger.LogWarning(
                        $"[Light.UI][diag] 制作人员/退出调整 {deltaY:F3}，目标Y={targetY:F3}");
                }
            }

            if (!_updaterChecked)
            {
                _updaterChecked = true;
                __instance.StartCoroutine(CoCheckUpdater().WrapToIl2Cpp());
            }
        }
        catch (System.Exception ex)
        {
            LightLogger.LogWarning("[Light.UI] 异常: " + ex);
        }
    }

    private static IEnumerator CoCheckUpdater()
    {
        yield return null;
        yield return null;
        yield return null;
        try
        {
            string updaterPath = Path.Combine(BepInEx.Paths.GameRootPath, VersionMaker.UpdaterExeName);
            if (!File.Exists(updaterPath))
            {
                LightLogger.LogWarning($"未找到更新脚本：{VersionMaker.UpdaterExeName}");
                LightUtils.ShowCustomDisconnectWindow($"未找到更新脚本 {VersionMaker.UpdaterExeName}！\n请将其放置在游戏根目录下，否则无法正常检查更新。");
            }
        }
        catch { }
    }

    private static void CreateLightButton(MainMenuManager __instance, Dictionary<string, PassiveButton> btns, float height)
    {
        try
        {
            if (!btns.TryGetValue("SettingsButton", out var settings)) return;
            var clone = GameObject.Instantiate(settings.gameObject, settings.transform.parent);
            clone.name = "LightButton";
            // LIGHT 落到“设置”按钮下方一个槽位；原版“设置”按钮保留
            clone.transform.localPosition =
                settings.transform.localPosition - new Vector3(0f, height, 0f);

            var tex = GraphicsHelper.LoadTextureFromResources("Light.Resources.Lobby.LightInDark.png");
            if (tex != null)
            {
                var spr = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height),
                    new Vector2(0.5f, 0.5f), 100f);
                for (int i = 0; i < clone.transform.childCount; i++)
                {
                    var child = clone.transform.GetChild(i);
                    var icon = child.FindChild("Icon");
                    if (icon != null)
                    {
                        icon.localScale = new Vector3(0.1f, 0.1f, 1f);
                        var sr = icon.GetComponent<SpriteRenderer>();
                        if (sr != null) sr.sprite = spr;
                    }
                }
            }
            var passive = clone.GetComponent<PassiveButton>();
            if (passive != null)
            {
                passive.OnClick = new Button.ButtonClickedEvent();
                passive.OnClick.AddListener((UnityEngine.Events.UnityAction)(() =>
                {
                    __instance.ResetScreen();
                    _showingPanel = true;
                    if (_lightSubScreen != null) _lightSubScreen.SetActive(false);
                    if (_galleryPanel != null) _galleryPanel.Hide();
                    if (_lightScreen != null) _lightScreen.SetActive(true);
                }));
            }

            var fp = clone.transform.FindChild("FontPlacer");
            if (fp != null && fp.childCount > 0)
            {
                DateTime now = DateTime.Now;
                int m = now.Month;
                int d = now.Day;
                var tmp = fp.GetChild(0).GetComponent<TextMeshPro>();
                if (tmp != null)
                {
                    if (m == 4 && d == 1) tmp.text = "A JOKE.";
                    else tmp.text = "LIGHT";
                }
                var trans = fp.GetChild(0).GetComponent<TextTranslatorTMP>();
                if (trans != null) trans.enabled = false;
            }
            var scalerList = Object.FindObjectOfType<SlicedAspectScaler>();
            if (scalerList != null)
            {
                var scaled = clone.GetComponent<AspectScaledAsset>();
                if (scaled != null) scalerList.objectsToScale.Add(scaled);
            }
            if (passive != null) btns["LightButton"] = passive;
        }
        catch (Exception ex)
        {
            LightLogger.LogError("[MainMenuPatch.CreateLightButton]", ex);
        }
    }

    private static void AdjustIcons()
    {
        try
        {
            var leftPanel = FindGO("LeftPanel");
            if (leftPanel == null) return;
            foreach (var btn in leftPanel.GetComponentsInChildren<PassiveButton>(true))
            {
                if (btn == null || btn.activeSprites == null) continue;
                var name = btn.name;
                bool shouldRotate = name != "LightButton" && name != "Inventory Button";
                bool shouldMove = name != "LightButton";
                var icon = btn.activeSprites.transform.FindChild("Icon");
                if (icon == null) continue;
                if (shouldRotate) icon.localEulerAngles -= new Vector3(0f, 0f, 10f);
                if (name != "LightButton") icon.localScale += new Vector3(0.12f, 0.12f, 0f);
                if (shouldMove)
                {
                    var asp = icon.GetComponent<AspectPosition>();
                    if (asp != null) { asp.DistanceFromEdge += new Vector3(-0.02f, 0.1f, 0f); asp.AdjustPosition(); }
                    else icon.localPosition += new Vector3(-0.02f, 0.1f, 0f);
                }
            }
        }
        catch (Exception ex)
        {
            LightLogger.LogError("[MainMenuPatch.AdjustIcons]", ex);
        }
    }

    private static void ColorAllButtons()
    {
        try
        {
            var leftPanel = FindGO("LeftPanel");
            if (leftPanel == null) return;
            var pink = new Color32(255, 192, 203, 255);
            var purple = new Color32(148, 112, 219, 255);
            var clear = new Color(0f, 0f, 0f, 0f);

            foreach (var btn in leftPanel.GetComponentsInChildren<PassiveButton>(true))
            {
                if (btn == null) continue;
                var n = btn.name;
                if (n is "NewsButton" or "AcountButton" or "SettingsButton" or "LightButton")
                    FormatBtn(btn, pink, clear, Color.white, Color.white);
                else if (n is "CreditsButton" or "ExitGameButton")
                    FormatBtn(btn, purple, clear, Color.white, Color.white);
            }
        }
        catch (Exception ex)
        {
            LightLogger.LogError("[MainMenuPatch.ColorAllButtons]", ex);
        }
    }

    private static void FormatBtn(PassiveButton btn, Color inactive, Color active,
        Color inactText, Color actText)
    {
        try
        {
            HideShine(btn.activeSprites);
            HideShine(btn.inactiveSprites);
            if (btn.activeSprites != null)
            {
                var sr = btn.activeSprites.GetComponent<SpriteRenderer>();
                if (sr != null) sr.color = active.a == 0f
                        ? new Color(inactive.r, inactive.g, inactive.b, 1f) : active;
            }
            if (btn.inactiveSprites != null)
            {
                var sr = btn.inactiveSprites.GetComponent<SpriteRenderer>();
                if (sr != null) sr.color = inactive;
            }
            btn.activeTextColor = actText;
            btn.inactiveTextColor = inactText;
        }
        catch (Exception ex)
        {
            LightLogger.LogError("[MainMenuPatch.FormatBtn]", ex);
        }
    }

    private static void HideShine(GameObject? parent)
    {
        try
        {
            if (parent == null) return;
            parent.transform.FindChild("Shine")?.gameObject.SetActive(false);
        }
        catch (Exception ex)
        {
            LightLogger.LogError("[MainMenuPatch.HideShine]", ex);
        }
    }

    private static void ApplyButtonEffects(MainMenuManager __instance)
    {
        try
        {
            ButtonBreathEffect.Init();
        }
        catch (Exception ex)
        {
            LightLogger.LogError("[MainMenuPatch.ApplyButtonEffects]", ex);
        }
    }

    private static void SetupRightPanel(MainMenuManager __instance)
    {
        try
        {
            _rightPanel = FindGO("RightPanel");
            if (_rightPanel == null) return;

            var asp = _rightPanel.GetComponent<AspectPosition>();
            if (asp != null) asp.enabled = false;

            _rightPanelOp = _rightPanel.transform.localPosition;
            _rightPanel.transform.localPosition = _rightPanelOp + new Vector3(10f, 0f, 0f);

            var sr = _rightPanel.GetComponent<SpriteRenderer>();
            if (sr != null) sr.color = new Color32(173, 214, 255, 255);

        }
        catch (Exception ex)
        {
            LightLogger.LogError("[MainMenuPatch.SetupRightPanel]", ex);
        }
    }

    private static void SetupLightScreen(MainMenuManager __instance)
    {
        try
        {
            _lightScreen = Object.Instantiate(__instance.accountButtons,
                __instance.accountButtons.transform.parent);
            _lightScreen.name = "LightScreen";

            var titleEntry = GetChild(_lightScreen.transform, 0)?.GetChild(0);
            if (titleEntry != null)
            {
                var titleText = titleEntry.GetComponent<TextMeshPro>();
                if (titleText != null)
                {
                    titleText.text = "Light In The Dark";
                    titleText.fontSize = 4.5f;
                }
                var titleTrans = titleEntry.GetComponent<TextTranslatorTMP>();
                if (titleTrans != null) titleTrans.enabled = false;
            }

            var child4 = GetChild(_lightScreen.transform, 4);
            if (child4 != null) Object.Destroy(child4.gameObject);

            var temp = GetChild(_lightScreen.transform, 3);
            if (temp == null) return;
            int index = 0;

            void SetUpBtn(string text, System.Action clickAction)
            {
                GameObject obj = temp.gameObject;
                if (index > 0) obj = GameObject.Instantiate(obj, obj.transform.parent);
                var label = GetChild(obj.transform, 0)?.GetChild(0);
                if (label != null)
                {
                    var tmp = label.GetComponent<TextMeshPro>();
                    if (tmp != null) tmp.text = text;
                    var tr = label.GetComponent<TextTranslatorTMP>();
                    if (tr != null) tr.enabled = false;
                }
                var pb = obj.GetComponent<PassiveButton>();
                if (pb != null)
                {
                    pb.OnClick = new Button.ButtonClickedEvent();
                    pb.OnClick.AddListener((UnityEngine.Events.UnityAction)(() => clickAction()));
                }
                obj.transform.localPosition = new Vector3(
                    (index % 2 == 0) ? -1.45f : 1.45f,
                    0.98f - (index / 2) * 0.59f, 0f);
                obj.transform.localScale = new Vector3(0.72f, 0.72f, 1f);
                index++;
            }

            SetUpBtn("模组设置", () => LightLogger.LogWarning("[Light] 模组设置 - 待实现"));
            SetUpBtn("关于模组", () => HelpScreen.TryOpenHelpScreen());
            SetUpBtn("成就", () => LightLogger.LogWarning("[Light] 成就 - 待实现"));
            SetUpBtn("Discord", () => Application.OpenURL("https://discord.gg/"));

            SetUpBtn("更多功能", () =>
            {
                if (_lightScreen != null) _lightScreen.SetActive(false);
                if (_lightSubScreen != null) _lightSubScreen.SetActive(true);
            });

            var scalerList = Object.FindObjectOfType<SlicedAspectScaler>();
            if (scalerList != null)
                foreach (var asset in _lightScreen.GetComponentsInChildren<AspectScaledAsset>())
                    scalerList.objectsToScale.Add(asset);

            _lightScreen.SetActive(false);
        }
        catch (Exception ex)
        {
            LightLogger.LogError("[MainMenuPatch.SetupLightScreen]", ex);
        }
    }

    private static void SetupSubScreen(MainMenuManager __instance)
    {
        try
        {
            var panel = new LightPanel(__instance, "LightSubScreen", "更多功能");

            panel.AddButton("功能B", () => LightLogger.Log("[Light] 功能B - 待实现"));
            panel.AddButton("功能C", () => LightLogger.Log("[Light] 功能C - 待实现"));
            panel.AddButton("功能D", () => LightLogger.Log("[Light] 功能D - 待实现"));

            if (_lightScreen != null)
                panel.AddBackButton(_lightScreen);

            panel.RegisterToScaler();
            panel.Hide();

            _lightSubScreen = panel.Panel;
        }
        catch (Exception ex)
        {
            LightLogger.LogError("[MainMenuPatch.SetupSubScreen]", ex);
        }
    }

    private static void SetupGalleryScreen(MainMenuManager __instance)
    {
        try
        {
            var gallery = new ImageGalleryPanel("60768f3185be4d77d40de1d75df44140704cead716175ea8915bd42e08d7c8e5");

            gallery.AddImage("At the Polus Core", "At the Polus Core", "曦曦", "波鲁斯星核的壮丽景象", "60768f3185be4d77d40de1d75df44140704cead716175ea8915bd42e08d7c8e5");
            gallery.AddImage("Default", "Default", "Inner Sloth", "原版默认背景", "");

            gallery.SetHashFailedFallback(() =>
            {
                LightLogger.LogWarning("[Light] BG_Default 哈希验证失败，请检查资源完整性");
            });

            gallery.SetApplyCallback(index =>
            {
                LightLogger.Log($"[Light] 应用背景图 #{index}");
                ButtonBreathEffect.Reload();
            });

            if (_lightSubScreen != null)
                gallery.OnBack = () => _lightSubScreen.SetActive(true);

            gallery.ApplyRandomBackground();

            _galleryPanel = gallery;
        }
        catch (Exception ex)
        {
            LightLogger.LogError("[MainMenuPatch.SetupGalleryScreen]", ex);
        }
    }

    private class LightPanel
    {
        private readonly GameObject _panel;
        private readonly Transform? _buttonTemplate;
        private int _index;

        public LightPanel(MainMenuManager instance, string panelName, string title)
        {
            try
            {
                _panel = GameObject.Instantiate(instance.accountButtons,
                    instance.accountButtons.transform.parent);
                _panel.name = panelName;

                var titleEntry = GetChild(_panel.transform, 0)?.GetChild(0);
                if (titleEntry != null)
                {
                    var titleText = titleEntry.GetComponent<TextMeshPro>();
                    if (titleText != null) titleText.text = title;
                    var titleTrans = titleEntry.GetComponent<TextTranslatorTMP>();
                    if (titleTrans != null) titleTrans.enabled = false;
                }

                var child4 = GetChild(_panel.transform, 4);
                if (child4 != null) Object.Destroy(child4.gameObject);

                _buttonTemplate = GetChild(_panel.transform, 3);
            }
            catch (Exception ex)
            {
                LightLogger.LogError("[MainMenuPatch.LightPanel]", ex);
            }
        }

        public void AddButton(string text, System.Action clickAction, Vector3? position = null)
        {
            try
            {
                if (_buttonTemplate == null) return;
                GameObject obj = _buttonTemplate.gameObject;
                if (_index > 0) obj = GameObject.Instantiate(obj, obj.transform.parent);

                var label = GetChild(obj.transform, 0)?.GetChild(0);
                if (label != null)
                {
                    var tmp = label.GetComponent<TextMeshPro>();
                    tmp?.text = text;
                    var tr = label.GetComponent<TextTranslatorTMP>();
                    if (tr != null) tr.enabled = false;
                }

                var pb = obj.GetComponent<PassiveButton>();
                if (pb != null)
                {
                    pb.OnClick = new Button.ButtonClickedEvent();
                    pb.OnClick.AddListener((UnityEngine.Events.UnityAction)(() => clickAction()));
                }

                obj.transform.localPosition = position ?? new Vector3(
                    (_index % 2 == 0) ? -1.45f : 1.45f,
                    0.98f - (_index / 2) * 0.59f, 0f);
                obj.transform.localScale = new Vector3(0.72f, 0.72f, 1f);
                _index++;
            }
            catch (Exception ex)
            {
                LightLogger.LogError("[MainMenuPatch.AddButton]", ex);
            }
        }

        public void AddBackButton(GameObject targetPanel, Vector3? position = null)
        {
            try
            {
                AddButton("返回", () =>
                {
                    _panel.SetActive(false);
                    targetPanel.SetActive(true);
                }, position ?? new Vector3(0f, -1.5f, 0f));
            }
            catch (Exception ex)
            {
                LightLogger.LogError("[MainMenuPatch.AddBackButton]", ex);
            }
        }

        public void RegisterToScaler()
        {
            try
            {
                var scalerList = Object.FindObjectOfType<SlicedAspectScaler>();
                if (scalerList != null)
                    foreach (var asset in _panel.GetComponentsInChildren<AspectScaledAsset>())
                        scalerList.objectsToScale.Add(asset);
            }
            catch (Exception ex)
            {
                LightLogger.LogError("[MainMenuPatch.RegisterToScaler]", ex);
            }
        }

        public GameObject Panel => _panel;
        public void Show() => _panel.SetActive(true);
        public void Hide() => _panel.SetActive(false);
    }

    private static void MoveScreenTint(MainMenuManager __instance)
    {
        try
        {
            SpriteRenderer? tint = __instance.screenTint;
            if (tint == null)
            {
                var go = FindGO("ScreenTint") ?? __instance.transform.FindChild("ScreenTint")?.gameObject;
                if (go == null)
                    go = __instance.mainMenuUI.transform.FindChild("Tint")?.gameObject;
                if (go != null) tint = go.GetComponent<SpriteRenderer>();
            }

            if (tint != null && _rightPanel != null)
            {
                tint.transform.SetParent(_rightPanel.transform);
                tint.transform.localPosition = new Vector3(-0.0824f, 0.0513f,
                    tint.transform.localPosition.z);
                tint.transform.localScale = new Vector3(1f, 1.4f, 1f);
                tint.drawMode = SpriteDrawMode.Sliced;
                tint.size = new Vector2(5f, 4f);
            }
        }
        catch (Exception ex)
        {
            LightLogger.LogError("[MainMenuPatch.MoveScreenTint]", ex);
        }
    }

    [HarmonyPatch(typeof(MainMenuManager), "LateUpdate")]
    [HarmonyPostfix]
    public static void LateUpdate()
    {
        try
        {
            var sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
            if (sceneName != "MainMenu" && sceneName != "MatchMaking")
            {
                _showingPanel = false;
                if (_galleryPanel != null && _galleryPanel._bgObj != null)
                    _galleryPanel._bgObj.SetActive(false);
                return;
            }
            if (_galleryPanel != null && _galleryPanel._bgObj != null && !_galleryPanel._bgObj.activeSelf)
                _galleryPanel._bgObj.SetActive(true);

            ButtonBreathEffect.Update();

            if (!_showingPanel)
            {
                if (_lightScreen != null && _lightScreen.activeSelf)
                    _lightScreen.SetActive(false);
                if (_lightSubScreen != null && _lightSubScreen.activeSelf)
                    _lightSubScreen.SetActive(false);
                if (_galleryPanel != null) _galleryPanel.Hide();
            }

            if (_rightPanel != null)
            {
                var target = _rightPanelOp + new Vector3(_showingPanel ? 0f : 10f, 0f, 0f);
                var lerp = Vector3.Lerp(_rightPanel.transform.localPosition, target,
                    Time.deltaTime * (_showingPanel ? 3f : 2f));
                if (_showingPanel
                    ? _rightPanel.transform.localPosition.x > _rightPanelOp.x
                    : _rightPanel.transform.localPosition.x < _rightPanelOp.x + 9f)
                    _rightPanel.transform.localPosition = lerp;
            }

            if (!_bgMoved)
            {
                var bg = FindGO("BackgroundTexture");
                if (bg != null && bg.activeSelf)
                {
                    _bgMoveDelay += Time.deltaTime;
                    if (_bgMoveDelay > 3f)
                    {
                        var pos = bg.transform.position;
                        bg.transform.position = Vector3.Lerp(pos,
                            new Vector3(pos.x, 7.1f, pos.z), Time.deltaTime * 1.4f);
                        if (pos.y > 7f)
                        {
                            _bgMoved = true;
                            bg.SetActive(false);
                        }
                    }
                }
            }

            if (_bgMoved && _galleryPanel != null && _galleryPanel._bgObj == null)
                _galleryPanel.RestoreBackground();
        }
        catch (System.Exception ex)
        {
            LightLogger.LogWarning("[Light] LateUpdate NRE: " + ex.Message + "\n" + ex.StackTrace);
            _rightPanel = null;
            _lightScreen = null;
            _lightSubScreen = null;
            _galleryPanel = null;
        }
    }

    [HarmonyPatch(typeof(MainMenuManager), "OpenGameModeMenu")]
    [HarmonyPatch(typeof(MainMenuManager), "OpenAccountMenu")]
    [HarmonyPatch(typeof(MainMenuManager), "OpenCredits")]
    [HarmonyPrefix, HarmonyPriority(0)]
    public static void ShowRightPanel()
    {
        try
        {
            _showingPanel = true;
            if (_lightScreen != null) _lightScreen.SetActive(false);
            if (_lightSubScreen != null) _lightSubScreen.SetActive(false);
            if (_galleryPanel != null) _galleryPanel.Hide();
        }
        catch (System.Exception ex)
        {
            LightLogger.LogWarning("[Light] MainMenuPatch.ShowRightPanel NRE: " + ex.Message + "\n" + ex.StackTrace);
        }
    }

    [HarmonyPatch(typeof(OptionsMenuBehaviour), "Open")]
    [HarmonyPatch(typeof(AnnouncementPopUp), "Show")]
    [HarmonyPrefix, HarmonyPriority(0)]
    public static void HideRightPanel()
    {
        try
        {
            _showingPanel = false;
            if (_lightScreen != null) _lightScreen.SetActive(false);
            if (_lightSubScreen != null) _lightSubScreen.SetActive(false);
            _galleryPanel?.Hide();
            DestroyableSingleton<AccountManager>.Instance
                ?.transform.FindChild("AccountTab/AccountWindow")?.gameObject.SetActive(false);
        }
        catch (System.Exception ex)
        {
            LightLogger.LogWarning("[Light] MainMenuPatch.HideRightPanel NRE: " + ex.Message + "\n" + ex.StackTrace);
        }
    }

    /// <summary>仿照 FS：克隆 quitButton 四份（检查更新 / Github / 两个占位符），
    /// 用 AspectPosition.anchorPoint 网格布局避免按钮重叠。</summary>
    private static void SetupExtraButtons(MainMenuManager __instance)
    {
        try
        {
            if (__instance.quitButton == null) return;
            var template = __instance.quitButton.gameObject;
            var parent = template.transform.parent;

            // 场景切换后旧的克隆会被销毁，这里清理可能残留的死引用对象
            for (int i = 0; i < 4; i++)
            {
                var old = FindGO($"ExtraButton{i}");
                if (old != null) Object.Destroy(old);
            }

            var defs = new (string label, System.Action action)[]
            {
                ("检查更新", CheckForUpdate),
                ("Github", () => Application.OpenURL("https://github.com/AfishMW/LightInDark")),
                ("模组官网", () => Application.OpenURL("https://github.com/AfishMW/LightInDark")),
                (DataManager.Settings.Language.CurrentLanguage==SupportedLangs.SChinese 
                ||
                DataManager.Settings.Language.CurrentLanguage==SupportedLangs.TChinese
                ?
                "QQ群"
                :
                "Discord", 
                () => 
                {
                    bool isQQGroup = DataManager.Settings.Language.CurrentLanguage==SupportedLangs.SChinese||DataManager.Settings.Language.CurrentLanguage==SupportedLangs.TChinese;
                    if (isQQGroup)
                    {
                        Application.OpenURL("https://qm.qq.com/q/mRsF2k5sUE");
                    }
                    else
                    {
                        LightUtils.ShowCustomDisconnectWindow("TODO : No DC");
                    }
                }),
            };

            // 2 列网格布局（思路与 SetUpBtn 一致）：col0 -> x0.42，col1 -> x0.58；每行 y 步进 0.08
            for (int i = 0; i < defs.Length; i++)
            {
                var button = Object.Instantiate(template, parent);
                button.name = $"ExtraButton{i}";
                button.SetActive(true);

                // 原 quitButton 带 ConditionalHide，克隆后若不销毁会因同名同父被隐藏
                var condHide = button.GetComponent<ConditionalHide>();
                if (condHide != null) Object.Destroy(condHide);

                var fp = button.transform.FindChild("FontPlacer");
                if (fp != null && fp.childCount > 0)
                {
                    var tmp = fp.GetChild(0).GetComponent<TextMeshPro>();
                    if (tmp != null) tmp.text = defs[i].label;
                    var tr = fp.GetChild(0).GetComponent<TextTranslatorTMP>();
                    if (tr != null) tr.enabled = false;
                }

                var pb = button.GetComponent<PassiveButton>();
                if (pb != null)
                {
                    // 注意：必须把 i 拷贝到局部变量再捕获，否则闭包引用的是循环变量 i，
                    // 循环结束后 i == defs.Length，点击时会 defs[i] 越界抛 IndexOutOfRangeException
                    int itemIndex = i;
                    pb.OnClick = new Button.ButtonClickedEvent();
                    pb.OnClick.AddListener((UnityEngine.Events.UnityAction)(() => defs[itemIndex].action()));
                }

                var asp = button.GetComponent<AspectPosition>();
                if (asp != null)
                {
                    int col = i % 2;
                    int row = i / 2 + 1;  // 从第 1 行开始，避开原版 Credits/Quit 按钮所在行
                    asp.anchorPoint = new Vector2(col == 0 ? 0.42f : 0.58f, 0.5f - 0.08f * row);
                    asp.AdjustPosition();
                }

                var scalerList = Object.FindObjectOfType<SlicedAspectScaler>();
                if (scalerList != null)
                {
                    var scaled = button.GetComponent<AspectScaledAsset>();
                    if (scaled != null) scalerList.objectsToScale.Add(scaled);
                }
            }
        }
        catch (Exception ex)
        {
            LightLogger.LogError("[MainMenuPatch.SetupExtraButtons]", ex);
        }
    }

    /// <summary>检查更新：need update -> 开始更新，其余情况弹窗提示。</summary>
    private static void CheckForUpdate()
    {
        string result = VersionMaker.CheckForUpdate();
        switch (result)
        {
            case "need update":
                VersionMaker.StartUpdateProcess();
                break;
            case "no need":
                LightUtils.ShowCustomDisconnectWindow("当前已是最新版本。");
                break;
            case "not installed":
                LightUtils.ShowCustomDisconnectWindow("模组未安装。你咋点的检查更新并且看到这个窗口？\n也有可能是你把文件改名了，请改回去。");
                break;
            case "check error":
                LightUtils.ShowCustomDisconnectWindow("更新检查失败。\n请将游戏目录下的Light.log发送给开发者或者QQ群中。\n不要直接将此界面截图/拍照给其他人。");
                break;
            case "path error":
                LightUtils.ShowCustomDisconnectWindow($"未找到更新检查器！请检查你的目录下有无 {VersionMaker.UpdaterExeName} 。");
                break;
            case "github error":
                LightUtils.ShowCustomDisconnectWindow("无法访问GitHub来检查版本！请检查您的网络状况。\n当然，最坏的结果是我们删仓跑路了。");
                break;
            default:
                LightUtils.ShowCustomDisconnectWindow("未知返回值。\n请将游戏目录下的Light.log发送给开发者或者QQ群中。\n不要直接将此界面截图/拍照给其他人。");
                break;
        }
    }

    /// <summary>克隆主界面临时标题（LOGO-AU）挂到主菜单根，替换为模组 Logo。在隐藏 LeftPanel 前调用。</summary>
    private static void CloneTitleToMainMenu(MainMenuManager __instance)
    {
        try
        {
            var leftPanel = FindGO("LeftPanel");
            var sizer = leftPanel != null ? leftPanel.transform.FindChild("Sizer") : null;
            var logo = sizer != null ? sizer.FindChild("LOGO-AU") : null;
            if (logo == null)
            {
                LightLogger.LogWarning("[Light] 未找到 LOGO-AU，跳过标题克隆");
                return;
            }

            var title = Object.Instantiate(logo.gameObject, __instance.transform);
            title.name = "ModTitle";
            foreach (var trans in title.GetComponentsInChildren<AspectSize>(true))
                Object.Destroy(trans);
            foreach (var trans in title.GetComponentsInChildren<AspectPosition>(true))
                Object.Destroy(trans);

            var sr = title.GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                var logoSp = ResourceHelper.LoadSpriteFromResource("Light.Resources.Logo.LightInDark.png");
                if (logoSp != null) sr.sprite = logoSp;
            }

            title.transform.SetParent(__instance.transform, false);
            title.transform.localPosition = new Vector3(-3.3f, 1.98f, -4);
            title.transform.localScale = new Vector3(0.25f, 0.25f, 1f);

            LightLogger.Log("[Light] 已克隆主标题并挂到主界面");
        }
        catch (Exception ex)
        {
            LightLogger.LogError("[Light] CloneTitleToMainMenu", ex);
        }
    }

}
