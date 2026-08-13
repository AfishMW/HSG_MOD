using BepInEx.Unity.IL2CPP.Utils.Collections;
using HarmonyLib;
using Light.Utilities;
using LightInDark.Utilities;
using TMPro;
using UnityEngine;
using static Light.LightPlugin;
using LightInDark.Core;
using Light.UI;
using System.Collections;
using System;

namespace Light.Patches;

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

    private static Dictionary<string, PassiveButton> FindButtons()
    {
        try
        {
            LightLogger.Log("1");
            var dict = new Dictionary<string, PassiveButton>();
            LightLogger.Log("2");
            var leftPanel = GameObject.Find("LeftPanel");
            LightLogger.Log("3");
            if (leftPanel == null) return dict;
            LightLogger.Log("4");
            foreach (var b in leftPanel.GetComponentsInChildren<PassiveButton>(true))
                if (b != null && !dict.ContainsKey(b.name))
                    dict[b.name] = b;
            StaticLog.LogWarning(string.Format("[Light.UI] 找到 {0} 个按钮", dict.Count));
            LightLogger.Log("5");
            return dict;
        }
        catch (Exception ex)
        {
            LightLogger.LogError("[MainMenuPatch.FindButtons]", ex); return default;
        }
    }

    private static GameObject? FindGO(string name) => GameObject.Find(name);

    private static bool _updaterChecked;
    private static bool _lateUpdateLogged;
    private static int _logoClickCount;
    private static bool _logoAltTexture;
    private static Sprite? _logoOriginalSprite;
    private static Sprite? _logoAltSprite;
    private static Transform? _logoTransform;

    [HarmonyPatch(typeof(MainMenuManager), nameof(MainMenuManager.Start))]
    [HarmonyPostfix]
    public static void Postfix(MainMenuManager __instance)
    {
        try
        {
            // 清理上次创建的克隆按钮（场景切换后旧的被销毁但 SlicedAspectScaler 列表里仍有死引用）
            var oldScaler = UnityEngine.Object.FindObjectOfType<SlicedAspectScaler>();
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
                if (old != null) UnityEngine.Object.Destroy(old);
            }
            var oldLight = FindGO("LightButton");
            if (oldLight != null) UnityEngine.Object.Destroy(oldLight);

            LightLogger.Log("6");
            _showingPanel = false;
            _rightPanel = null;
            _lightScreen = null;
            _lightSubScreen = null;
            _galleryPanel = null;
            LightLogger.Log("7");
            StaticLog.LogWarning("[Light.UI] === 开始布局 ===");

            UI.Window.VanillaAsset.Preload();
            LightLogger.Log("8");
            var modStamp = FindGO("ModStamp");
            if (modStamp != null)
            {
                modStamp.SetActive(true);
                modStamp.transform.localScale = Vector3.one * 0.06f;
                var sr = modStamp.GetComponent<SpriteRenderer>();
                if (sr != null)
                    sr.sprite = ResourceHelper.LoadSpriteFromResource("Light.Resources.ModStamp.png");
                LightLogger.Log("9");
            }
            LightLogger.Log("10");
            FindGO("ReactorVersion")?.SetActive(false);
            var ambience = FindGO("Ambience");
            if (ambience != null)
            {
                ambience.transform.FindChild("PlayerParticles")?.gameObject.SetActive(false);
                var stars = ambience.transform.GetChild(0);
                //var snowFlakes = UnityEngine.Object.Instantiate(stars);
                stars.gameObject.SetActive(false);
                LightLogger.Log("11");
            }
            //UnityEngine.Object.FindObjectOfType<VersionShower>()?.gameObject.SetActive(false);
            LightLogger.Log("12");
            _bgMoved = false;
            _bgMoveDelay = 0f;
            var bg = FindGO("BackgroundTexture");
            bg?.SetActive(true);

            var btns = FindButtons();

            var leftPanel = FindGO("LeftPanel");
            LightLogger.Log("13");
            if (leftPanel != null)
            {
                var sizer = leftPanel.transform.FindChild("Sizer");
                LightLogger.Log("14");
                if (sizer != null)
                {
                    var auLogo = sizer.GetComponent<AspectSize>();
                    LightLogger.Log("15");
                    if (auLogo != null)
                    {
                        auLogo.PercentWidth = 0.14f;
                        auLogo.DoSetUp();
                        auLogo.transform.localPosition += new Vector3(-0.8f, 0.25f, 0f);
                        LightLogger.Log("16");
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
            LightLogger.Log("17");
            var divider = leftPanel?.transform.FindChild("Main Buttons")?.FindChild("Divider");
            if (divider != null)
                divider.localPosition += new Vector3(0f, height, 0f);
            LightLogger.Log("18");
            if (leftPanel != null)
            {
                var reworked = UnityHelper.CreateObject<SpriteRenderer>(
                    "ReworkedLeftPanel", leftPanel.transform, new Vector3(0f, height * 0.5f, 0f));
                var oldSr = leftPanel.GetComponent<SpriteRenderer>();
                LightLogger.Log("19");
                if (oldSr != null)
                {
                    reworked.sprite = oldSr.sprite;
                    reworked.tileMode = oldSr.tileMode;
                    reworked.drawMode = oldSr.drawMode;
                    reworked.size = oldSr.size + new Vector2(0f, 0.5f);
                    oldSr.enabled = false;
                    LightLogger.Log("20");
                }
            }
            LightLogger.Log("21");
            var onlineRoot = __instance.mainMenuUI.transform
                .FindChild("AspectScaler")?.FindChild("Online Buttons");
            // Online Buttons 布局暂时禁用，排查 NRE
            //if (onlineRoot != null)
            //{
            //    LightLogger.Log("22");
            //    for (int i = 0; i < onlineRoot.childCount; i++)
            //    {
            //        var child = onlineRoot.GetChild(i);
            //        var scaler = child.Find("Scaler") ?? child;
            //        if (scaler == null) continue;
            //        for (int j = 0; j < scaler.childCount; j++)
            //        {
            //            var btn = scaler.GetChild(j);
            //            var btnName = btn.name.ToLowerInvariant();
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
            LightLogger.Log("23");
            CreateLightButton(__instance, btns, height);
            LightLogger.Log("24");
            AdjustIcons();
            LightLogger.Log("25");
            ColorAllButtons();
            LightLogger.Log("26");
            if (leftPanel != null)
            {
                var mainButtons = leftPanel.transform.FindChild("Main Buttons");
                leftPanel.transform.localScale = new Vector3(0.7f, 0.7f, 0.7f);
                for (int i = 0; i < leftPanel.transform.childCount; i++)
                    leftPanel.transform.GetChild(i).SetParent(leftPanel.transform.parent);
                if (mainButtons != null)
                    mainButtons.transform.localPosition = new Vector3(-4.0f, 0f, 0f);
                leftPanel.SetActive(false);
                LightLogger.Log("27");
            }
            LightLogger.Log("28");
            FindGO("Divider")?.SetActive(false);
            LightLogger.Log("29");
            ApplyButtonEffects(__instance);
            LightLogger.Log("30");
            SetupRightPanel(__instance);
            LightLogger.Log("31");
            SetupLightScreen(__instance);
            LightLogger.Log("32");
            SetupSubScreen(__instance);
            LightLogger.Log("33");
            SetupGalleryScreen(__instance);
            LightLogger.Log("34");
            _galleryPanel?.RestoreBackground();
            LightLogger.Log("35");
            MoveScreenTint(__instance);
            LightLogger.Log("36");
            var bounds = FindGO("BottomButtonBounds");
            if (bounds != null)
                bounds.transform.localPosition -= new Vector3(0f, 0.1f, 0f);
            LightLogger.Log("37");
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
            LightLogger.Log("38");
            foreach (var obj in UnityEngine.Resources.FindObjectsOfTypeAll<GameObject>())
                if (obj.name is "FreePlayButton" or "HowToPlayButton")
                    obj.transform.localPosition = new Vector3(0f, -20f, 0f);
            LightLogger.Log("149");

            SetupCustomButtons(__instance, btns);
            LightLogger.Log("150");

            StaticLog.LogWarning("[Light.UI] === 布局完成 ===");
            LightLogger.Log("151");

            if (!_updaterChecked)
            {
                _updaterChecked = true;
                __instance.StartCoroutine(CoCheckUpdater().WrapToIl2Cpp());
                LightLogger.Log("152");
            }
            LightLogger.Log("153");
        }
        catch (System.Exception ex)
        {
            StaticLog.LogWarning("[Light.UI] 异常: " + ex);
        }
    }

    private static IEnumerator CoCheckUpdater()
    {
        yield return null;
        yield return null;
        yield return null;
        LightLogger.Log("39");
        try
        {
            string updaterPath = Path.Combine(BepInEx.Paths.GameRootPath, VersionMaker.UpdaterExeName);
            if (!File.Exists(updaterPath))
            {
                LightLogger.LogWarning($"未找到更新脚本：{VersionMaker.UpdaterExeName}");
                AmongUsEdited.ShowCustomDisconnectWindow($"未找到更新脚本 {VersionMaker.UpdaterExeName}！\n请将其放置在游戏根目录下，否则无法正常检查更新。");
                LightLogger.Log("154");
            }
            LightLogger.Log("155");
        }
        catch { }
    }

    private static void CreateLightButton(MainMenuManager __instance, Dictionary<string, PassiveButton> btns, float height)
    {
        try
        {
            if (!btns.TryGetValue("SettingsButton", out var settings)) return;
            LightLogger.Log("40");
            var clone = GameObject.Instantiate(settings.gameObject, settings.transform.parent);
            clone.name = "LightButton";
            clone.transform.localPosition += new Vector3(0f, -height, 0f);

            var tex = GraphicsHelper.LoadTextureFromResources("Light.Resources.Lobby.LightInDark.png");
            if (tex != null)
            {
                var spr = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height),
                    new Vector2(0.5f, 0.5f), 100f);
                clone.ForEachChild((Il2CppSystem.Action<GameObject>)((obj) =>
                {
                    var icon = obj.transform.FindChild("Icon");
                    if (icon != null)
                    {
                        icon.localScale = new Vector3(0.1f, 0.1f, 1f);
                        var sr = icon.GetComponent<SpriteRenderer>();
                        if (sr != null) sr.sprite = spr;
                    }
                }));
            }
            LightLogger.Log("41");
            var passive = clone.GetComponent<PassiveButton>();
            passive.OnClick = new UnityEngine.UI.Button.ButtonClickedEvent();
            passive.OnClick.AddListener((UnityEngine.Events.UnityAction)(() =>
            {
                __instance.ResetScreen();
                _showingPanel = true;
                if (_lightSubScreen != null) _lightSubScreen.SetActive(false);
                if (_galleryPanel != null) _galleryPanel.Hide();
                if (_lightScreen != null) _lightScreen.SetActive(true);
            }));

            var fp = clone.transform.FindChild("FontPlacer");
            if (fp != null && fp.childCount > 0)
            {
                DateTime now = DateTime.Now;
                int m = now.Month;
                int d = now.Day;
                var tmp = fp.GetChild(0).GetComponent<TextMeshPro>();
                LightLogger.Log("42");
                if (tmp != null)
                {
                    if (m == 4 && d == 1) tmp.text = "A JOKE.";
                    else tmp.text = "LIGHT";
                }
                var trans = fp.GetChild(0).GetComponent<TextTranslatorTMP>();
                if (trans != null) trans.enabled = false;
            }
            LightLogger.Log("43");
            var scalerList = GameObject.FindObjectOfType<SlicedAspectScaler>();
            if (scalerList != null)
            {
                var scaled = clone.GetComponent<AspectScaledAsset>();
                if (scaled != null) scalerList.objectsToScale.Add(scaled);
            }
            LightLogger.Log("44");
            btns["LightButton"] = passive;
        }
        catch (Exception ex)
        {
            LightLogger.LogError("[MainMenuPatch.CreateLightButton]", ex);
        }
    }

    private static readonly Color32[] CustomButtonColors =
    [
        new(255, 192, 203, 255),
        new(148, 112, 219, 255),
        new(79, 209, 197, 255),
        new(255, 215, 0, 255),
    ];

    private static readonly string[] CustomButtonNames = { "按钮A", "按钮B", "按钮C", "按钮D" };

    private static void SetupCustomButtons(MainMenuManager __instance, Dictionary<string, PassiveButton> btns)
    {
        try
        {
            if (!btns.TryGetValue("CreditsButton", out var creditsBtn)) return;
            if (!btns.TryGetValue("ExitGameButton", out var exitBtn)) return;
            LightLogger.Log("45");
            var templates = new PassiveButton[] { creditsBtn, exitBtn };
            float leftX = -0.9f;
            float rightX = 0.9f;
            float baseY = -0.7f;
            float spacing = 0.7f;

            for (int i = 0; i < 4; i++)
            {
                int col = i % 2;
                int row = i / 2;
                var template = templates[col];
                LightLogger.Log("46");
                var clone = GameObject.Instantiate(template.gameObject, template.transform.parent);
                clone.name = $"CustomButton{i}";
                LightLogger.Log("47");
                clone.transform.localScale = new Vector3(0.42f, 0.42f, 1f);

                float x = col == 0 ? leftX : rightX;
                float y = baseY - row * spacing;
                clone.transform.localPosition = new Vector3(x, y, 0f);
                LightLogger.Log("48");
                var fp = clone.transform.FindChild("FontPlacer");
                if (fp != null && fp.childCount > 0)
                {
                    var tmp = fp.GetChild(0).GetComponent<TextMeshPro>();
                    if (tmp != null) tmp.text = CustomButtonNames[i];
                    var tr = fp.GetChild(0).GetComponent<TextTranslatorTMP>();
                    if (tr != null) tr.enabled = false;
                    LightLogger.Log("49");
                }
                LightLogger.Log("50");
                var pb = clone.GetComponent<PassiveButton>();
                if (pb != null)
                {
                    FormatBtn(pb, CustomButtonColors[i], new Color(0f, 0f, 0f, 0f), Color.white, Color.white);
                    pb.OnClick = new UnityEngine.UI.Button.ButtonClickedEvent();
                    var idx = i;
                    pb.OnClick.AddListener((UnityEngine.Events.UnityAction)(() =>
                    {
                        LightLogger.Log($"[Light] 自定义按钮 {idx} 被点击");
                    }));
                    LightLogger.Log("51");
                }
                LightLogger.Log("52");
                var scalerList = GameObject.FindObjectOfType<SlicedAspectScaler>();
                if (scalerList != null)
                {
                    var scaled = clone.GetComponent<AspectScaledAsset>();
                    if (scaled != null) scalerList.objectsToScale.Add(scaled);
                }
            }
            LightLogger.Log("53");
        }
        catch (Exception ex)
        {
            LightLogger.LogError("[MainMenuPatch.SetupCustomButtons]", ex);
        }
    }

    private static void AdjustIcons()
    {
        try
        {
            LightLogger.Log("54");
            var leftPanel = FindGO("LeftPanel");
            if (leftPanel == null) return;
            LightLogger.Log("55");
            foreach (var btn in leftPanel.GetComponentsInChildren<PassiveButton>(true))
            {
                if (btn == null || btn.activeSprites == null) continue;
                var name = btn.name;
                bool shouldRotate = name != "LightButton" && name != "Inventory Button";
                bool shouldMove = name != "LightButton";
                var icon = btn.activeSprites.transform.FindChild("Icon");
                if (icon == null) continue;
                LightLogger.Log("56");
                if (shouldRotate) icon.localEulerAngles -= new Vector3(0f, 0f, 10f);
                if (name != "LightButton") icon.localScale += new Vector3(0.12f, 0.12f, 0f);
                if (shouldMove)
                {
                    var asp = icon.GetComponent<AspectPosition>();
                    if (asp != null) { asp.DistanceFromEdge += new Vector3(-0.02f, 0.1f, 0f); asp.AdjustPosition(); }
                    else icon.localPosition += new Vector3(-0.02f, 0.1f, 0f);
                }
                LightLogger.Log("57");
            }
            LightLogger.Log("58");
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
            LightLogger.Log("59");
            var leftPanel = FindGO("LeftPanel");
            if (leftPanel == null) return;
            LightLogger.Log("60");
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
                LightLogger.Log("61");
            }
            LightLogger.Log("62");
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
            LightLogger.Log("63");
            HideShine(btn.activeSprites);
            HideShine(btn.inactiveSprites);
            LightLogger.Log("64");
            if (btn.activeSprites != null)
            {
                var sr = btn.activeSprites.GetComponent<SpriteRenderer>();
                if (sr != null) sr.color = active.a == 0f
                        ? new Color(inactive.r, inactive.g, inactive.b, 1f) : active;
                LightLogger.Log("65");
            }
            if (btn.inactiveSprites != null)
            {
                var sr = btn.inactiveSprites.GetComponent<SpriteRenderer>();
                if (sr != null) sr.color = inactive;
                LightLogger.Log("66");
            }
            btn.activeTextColor = actText;
            btn.inactiveTextColor = inactText;
            LightLogger.Log("67");
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
            LightLogger.Log("68");
            if (parent == null) return;
            parent.transform.FindChild("Shine")?.gameObject.SetActive(false);
            LightLogger.Log("69");
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
            LightLogger.Log("70");
            ButtonBreathEffect.Init();
            LightLogger.Log("71");
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
            LightLogger.Log("72");
            _rightPanel = FindGO("RightPanel");
            if (_rightPanel == null) return;
            LightLogger.Log("73");

            var asp = _rightPanel.GetComponent<AspectPosition>();
            if (asp != null) asp.enabled = false;
            LightLogger.Log("74");

            _rightPanelOp = _rightPanel.transform.localPosition;
            _rightPanel.transform.localPosition = _rightPanelOp + new Vector3(10f, 0f, 0f);
            LightLogger.Log("75");

            var sr = _rightPanel.GetComponent<SpriteRenderer>();
            if (sr != null) sr.color = new Color32(173, 214, 255, 255);
            LightLogger.Log("76");

            if (__instance.playButton == null) return;
            LightLogger.Log("77");

            var tex = GraphicsHelper.LoadTextureFromResources("Light.Resources.Lobby.LightInDark.png");
            if (tex == null) return;
            LightLogger.Log("78");
            var spr = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height),
                new Vector2(0.5f, 0.5f), 100f);

            var logoHolder = new GameObject("LightLogoHolder");
            logoHolder.transform.SetParent(__instance.mainMenuUI.transform);
            logoHolder.transform.localPosition = new Vector3(0f, 0f, -5f);
            logoHolder.transform.localScale = Vector3.one;
            LightLogger.Log("79");

            var logo = UnityHelper.CreateObject<SpriteRenderer>("LightLogo",
                logoHolder.transform, new Vector3(0f, 0f, 0f));
            logo.transform.localScale = new Vector3(0.24f, 0.24f, 1f);
            logo.sprite = spr;
            LightLogger.Log("80");

            var glow = UnityHelper.CreateObject<SpriteRenderer>("LightLogoGlow",
                logoHolder.transform, new Vector3(0f, 0f, -0.1f));
            glow.transform.localScale = new Vector3(0.24f, 0.24f, 1f);
            glow.sprite = spr;
            glow.color = Color.white;
            LightLogger.Log("81");

            var logoCollider = logo.gameObject.AddComponent<BoxCollider2D>();
            logoCollider.isTrigger = true;
            logoCollider.size = new Vector2(2f, 1f);
            _logoOriginalSprite = logo.sprite;
            _logoTransform = logo.transform;
            var logoBtn = logo.gameObject.AddComponent<PassiveButton>();
            logoBtn.OnClick = new UnityEngine.UI.Button.ButtonClickedEvent();
            logoBtn.OnClick.AddListener((UnityEngine.Events.UnityAction)(() =>
            {
                __instance.StartCoroutine(CoLogoBounce(logo.transform).WrapToIl2Cpp());
                _logoClickCount++;
                if (_logoClickCount >= 10)
                {
                    _logoClickCount = 0;
                    __instance.StartCoroutine(CoLogoSpin(logo.transform, __instance).WrapToIl2Cpp());
                }
            }));
            LightLogger.Log("82");
        }
        catch (Exception ex)
        {
            LightLogger.LogError("[MainMenuPatch.SetupRightPanel]", ex);
        }
    }

    private static IEnumerator CoLogoBounce(Transform logo)
    {
        LightLogger.Log("83");
        var original = logo.localScale;
        float t = 0f;
        while (t < 0.6f)
        {
            t += Time.deltaTime;
            float p = t / 0.6f;
            float scale = p < 0.3f
                ? Mathf.Lerp(1f, 1.5f, p / 0.3f)
                : Mathf.Lerp(1.5f, 1f, (p - 0.3f) / 0.7f);
            logo.localScale = original * scale;
            LightLogger.Log("84");
            yield return null;
        }
        logo.localScale = original;
        LightLogger.Log("85");
    }

    private static IEnumerator CoLogoSpin(Transform logo, MainMenuManager __instance)
    {
        var original = logo.localScale;
        var originalRot = logo.localEulerAngles;
        float t = 0f;
        float duration = 0.8f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float p = t / duration;
            logo.localEulerAngles = new Vector3(0f, 0f, 360f * p);
            float s = 1f + 0.3f * Mathf.Sin(p * Mathf.PI);
            logo.localScale = original * s;
            yield return null;
        }
        logo.localEulerAngles = originalRot;
        logo.localScale = original;

        _logoAltTexture = !_logoAltTexture;
        var sr = logo.GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            if (_logoAltTexture && _logoAltSprite == null)
            {
                var tex = GraphicsHelper.LoadTextureFromResources("Light.Resources.Lobby.LightInDark_alt.png");
                if (tex != null)
                    _logoAltSprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height),
                        new Vector2(0.5f, 0.5f), 100f);
            }
            sr.sprite = _logoAltTexture ? _logoAltSprite : _logoOriginalSprite;
        }
    }

    private static void SetupLightScreen(MainMenuManager __instance)
    {
        try
        {
            LightLogger.Log("86");
            _lightScreen = UnityEngine.Object.Instantiate(__instance.accountButtons,
                __instance.accountButtons.transform.parent);
            _lightScreen.name = "LightScreen";
            LightLogger.Log("87");

            var titleText = _lightScreen.transform.GetChild(0).GetChild(0)
                .GetComponent<TextMeshPro>();
            if (titleText != null)
            {
                titleText.text = "Light In The Dark";
                titleText.fontSize = 4.5f;
                LightLogger.Log("88");
            }
            var titleTrans = _lightScreen.transform.GetChild(0).GetChild(0)
                .GetComponent<TextTranslatorTMP>();
            if (titleTrans != null) titleTrans.enabled = false;
            LightLogger.Log("89");

            var child4 = _lightScreen.transform.GetChild(4);
            if (child4 != null) GameObject.Destroy(child4.gameObject);
            LightLogger.Log("90");

            var temp = _lightScreen.transform.GetChild(3);
            int index = 0;
            LightLogger.Log("91");

            void SetUpBtn(string text, System.Action clickAction)
            {
                GameObject obj = temp.gameObject;
                if (index > 0) obj = GameObject.Instantiate(obj, obj.transform.parent);
                LightLogger.Log("92");
                var tmp = obj.transform.GetChild(0).GetChild(0).GetComponent<TextMeshPro>();
                if (tmp != null) tmp.text = text;
                var tr = obj.transform.GetChild(0).GetChild(0)
                    .GetComponent<TextTranslatorTMP>();
                if (tr != null) tr.enabled = false;
                LightLogger.Log("93");
                var pb = obj.GetComponent<PassiveButton>();
                pb.OnClick = new UnityEngine.UI.Button.ButtonClickedEvent();
                pb.OnClick.AddListener((UnityEngine.Events.UnityAction)(() => clickAction()));
                LightLogger.Log("94");
                obj.transform.localPosition = new Vector3(
                    (index % 2 == 0) ? -1.45f : 1.45f,
                    0.98f - (index / 2) * 0.59f, 0f);
                obj.transform.localScale = new Vector3(0.72f, 0.72f, 1f);
                index++;
            }
            LightLogger.Log("95");

            SetUpBtn("模组设置", () => StaticLog.LogWarning("[Light] 模组设置 - 待实现"));
            LightLogger.Log("96");
            SetUpBtn("关于模组", () => Light.UI.Help.HelpScreen.TryOpenHelpScreen());
            SetUpBtn("成就", () => StaticLog.LogWarning("[Light] 成就 - 待实现"));
            SetUpBtn("Discord", () => Application.OpenURL("https://discord.gg/"));
            LightLogger.Log("97");
            SetUpBtn("检查更新", () =>
            {
                string fanHuiZhi = VersionMaker.CheckForUpdate();
                switch (fanHuiZhi)
                {
                    case "need update":
                        VersionMaker.StartUpdateProcess();
                        break;
                    case "no need":
                        AmongUsEdited.ShowCustomDisconnectWindow("当前已是最新版本。");
                        break;
                    case "not installed":
                        AmongUsEdited.ShowCustomDisconnectWindow("模组未安装。你咋点的检查更新并且看到这个窗口？\n也有可能是你把文件改名了，请改回去。");
                        break;
                    case "check error":
                        AmongUsEdited.ShowCustomDisconnectWindow("更新检查失败。\n请将游戏目录下的Light.log发送给开发者或者QQ群中。\n不要直接将此界面截图/拍照给其他人。");
                        break;
                    case "path error":
                        AmongUsEdited.ShowCustomDisconnectWindow($"未找到更新检查器！请检查你的目录下有无 {VersionMaker.UpdaterExeName} 。");
                        break;
                    case "github error":
                        AmongUsEdited.ShowCustomDisconnectWindow("无法访问GitHub来检查版本！请检查您的网络状况。\n当然，最坏的结果是我们删仓跑路了。");
                        break;
                    default:
                        AmongUsEdited.ShowCustomDisconnectWindow("未知返回值。\n请将游戏目录下的Light.log发送给开发者或者QQ群中。\n不要直接将此界面截图/拍照给其他人。");

                        break;

                }

                LightLogger.Log($"Update返回值检测完毕：{fanHuiZhi}");
            }
            );

            SetUpBtn("更多功能", () =>
            {
                if (_lightScreen != null) _lightScreen.SetActive(false);
                if (_lightSubScreen != null) _lightSubScreen.SetActive(true);
            });
            LightLogger.Log("98");

            var scalerList = GameObject.FindObjectOfType<SlicedAspectScaler>();
            if (scalerList != null)
                foreach (var asset in _lightScreen.GetComponentsInChildren<AspectScaledAsset>())
                    scalerList.objectsToScale.Add(asset);
            LightLogger.Log("99");

            _lightScreen.SetActive(false);
            LightLogger.Log("100");
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
            LightLogger.Log("101");
            var panel = new LightPanel(__instance, "LightSubScreen", "更多功能");
            LightLogger.Log("102");

            panel.AddButton("背景板", () =>
            {
                if (_lightSubScreen != null) _lightSubScreen.SetActive(false);
                _galleryPanel?.Show();
            });
            LightLogger.Log("103");
            panel.AddButton("功能B", () => LightLogger.Log("[Light] 功能B - 待实现"));
            panel.AddButton("功能C", () => LightLogger.Log("[Light] 功能C - 待实现"));
            panel.AddButton("功能D", () => LightLogger.Log("[Light] 功能D - 待实现"));
            LightLogger.Log("104");

            if (_lightScreen != null)
                panel.AddBackButton(_lightScreen);
            LightLogger.Log("105");

            panel.RegisterToScaler();
            panel.Hide();
            LightLogger.Log("106");

            _lightSubScreen = panel.Panel;
            LightLogger.Log("107");
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
            LightLogger.Log("108");
            var gallery = new ImageGalleryPanel("60768f3185be4d77d40de1d75df44140704cead716175ea8915bd42e08d7c8e5");
            LightLogger.Log("109");

            gallery.AddImage("At the Polus Core", "At the Polus Core", "曦曦", "波鲁斯星核的壮丽景象", "60768f3185be4d77d40de1d75df44140704cead716175ea8915bd42e08d7c8e5");
            gallery.AddImage("Default", "Default", "Inner Sloth", "原版默认背景", "");
            LightLogger.Log("110");

            gallery.SetHashFailedFallback(() =>
            {
                LightLogger.LogWarning("[Light] BG_Default 哈希验证失败，请检查资源完整性");
            });
            LightLogger.Log("111");

            gallery.SetApplyCallback(index =>
            {
                LightLogger.Log($"[Light] 应用背景图 #{index}");
                ButtonBreathEffect.Reload();
            });
            LightLogger.Log("112");

            if (_lightSubScreen != null)
                gallery.OnBack = () => _lightSubScreen.SetActive(true);
            LightLogger.Log("113");

            gallery.ApplyDefaultBackground();
            LightLogger.Log("114");

            _galleryPanel = gallery;
            LightLogger.Log("115");
        }
        catch (Exception ex)
        {
            LightLogger.LogError("[MainMenuPatch.SetupGalleryScreen]", ex);
        }
    }

    private class LightPanel
    {
        private readonly GameObject _panel;
        private readonly Transform _buttonTemplate;
        private int _index;

        public LightPanel(MainMenuManager instance, string panelName, string title)
        {
            try
            {
                LightLogger.Log("116");
                _panel = GameObject.Instantiate(instance.accountButtons,
                    instance.accountButtons.transform.parent);
                _panel.name = panelName;
                LightLogger.Log("117");

                var titleText = _panel.transform.GetChild(0).GetChild(0)
                    .GetComponent<TextMeshPro>();
                if (titleText != null) titleText.text = title;
                var titleTrans = _panel.transform.GetChild(0).GetChild(0)
                    .GetComponent<TextTranslatorTMP>();
                if (titleTrans != null) titleTrans.enabled = false;
                LightLogger.Log("118");

                var child4 = _panel.transform.GetChild(4);
                if (child4 != null) GameObject.Destroy(child4.gameObject);
                LightLogger.Log("119");

                _buttonTemplate = _panel.transform.GetChild(3);
                LightLogger.Log("120");
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
                LightLogger.Log("121");
                GameObject obj = _buttonTemplate.gameObject;
                if (_index > 0) obj = GameObject.Instantiate(obj, obj.transform.parent);
                LightLogger.Log("122");

                var tmp = obj.transform.GetChild(0).GetChild(0).GetComponent<TextMeshPro>();
                if (tmp != null) tmp.text = text;
                var tr = obj.transform.GetChild(0).GetChild(0)
                    .GetComponent<TextTranslatorTMP>();
                if (tr != null) tr.enabled = false;
                LightLogger.Log("123");

                var pb = obj.GetComponent<PassiveButton>();
                pb.OnClick = new UnityEngine.UI.Button.ButtonClickedEvent();
                pb.OnClick.AddListener((UnityEngine.Events.UnityAction)(() => clickAction()));
                LightLogger.Log("124");

                obj.transform.localPosition = position ?? new Vector3(
                    (_index % 2 == 0) ? -1.45f : 1.45f,
                    0.98f - (_index / 2) * 0.59f, 0f);
                obj.transform.localScale = new Vector3(0.72f, 0.72f, 1f);
                _index++;
                LightLogger.Log("125");
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
                LightLogger.Log("126");
                AddButton("返回", () =>
                {
                    _panel.SetActive(false);
                    targetPanel.SetActive(true);
                }, position ?? new Vector3(0f, -1.5f, 0f));
                LightLogger.Log("127");
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
                LightLogger.Log("128");
                var scalerList = GameObject.FindObjectOfType<SlicedAspectScaler>();
                if (scalerList != null)
                    foreach (var asset in _panel.GetComponentsInChildren<AspectScaledAsset>())
                        scalerList.objectsToScale.Add(asset);
                LightLogger.Log("129");
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
            LightLogger.Log("130");
            SpriteRenderer? tint = __instance.screenTint;
            if (tint == null)
            {
                var go = FindGO("ScreenTint") ?? __instance.transform.FindChild("ScreenTint")?.gameObject;
                if (go == null)
                    go = __instance.mainMenuUI.transform.FindChild("Tint")?.gameObject;
                if (go != null) tint = go.GetComponent<SpriteRenderer>();
                LightLogger.Log("131");
            }
            LightLogger.Log("132");

            if (tint != null && _rightPanel != null)
            {
                tint.transform.SetParent(_rightPanel.transform);
                tint.transform.localPosition = new Vector3(-0.0824f, 0.0513f,
                    tint.transform.localPosition.z);
                tint.transform.localScale = new Vector3(1f, 1.4f, 1f);
                tint.drawMode = SpriteDrawMode.Sliced;
                tint.size = new Vector2(5f, 4f);
                LightLogger.Log("133");
            }
            LightLogger.Log("134");
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
            if (sceneName != "MainMenu" && sceneName != "MatchMaking") { _showingPanel = false; if (_galleryPanel != null && _galleryPanel._bgObj != null) _galleryPanel._bgObj.SetActive(false); return; }
            if (_galleryPanel != null && _galleryPanel._bgObj != null && !_galleryPanel._bgObj.activeSelf)
                _galleryPanel._bgObj.SetActive(true);

            ButtonBreathEffect.Update();

            if (!_lateUpdateLogged)
            {
                _lateUpdateLogged = true;
                LightLogger.Log("[Light] LateUpdate postfix 已激活");
            }
            LightLogger.Log("136");

            if (!_showingPanel)
            {
                if (_lightScreen != null && _lightScreen.activeSelf)
                    _lightScreen.SetActive(false);
                if (_lightSubScreen != null && _lightSubScreen.activeSelf)
                    _lightSubScreen.SetActive(false);
                if (_galleryPanel != null) _galleryPanel.Hide();
                LightLogger.Log("137");
            }
            LightLogger.Log("138");

            if (_rightPanel != null)
            {
                var target = _rightPanelOp + new Vector3(_showingPanel ? 0f : 10f, 0f, 0f);
                var lerp = Vector3.Lerp(_rightPanel.transform.localPosition, target,
                    Time.deltaTime * (_showingPanel ? 3f : 2f));
                if (_showingPanel
                    ? _rightPanel.transform.localPosition.x > _rightPanelOp.x
                    : _rightPanel.transform.localPosition.x < _rightPanelOp.x + 9f)
                    _rightPanel.transform.localPosition = lerp;
                LightLogger.Log("139");
            }
            LightLogger.Log("140");

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
                        LightLogger.Log("141");
                    }
                }
                LightLogger.Log("142");
            }
            LightLogger.Log("143");

            if (_bgMoved && _galleryPanel != null)
            {
                if (_galleryPanel._bgObj == null)
                    _galleryPanel.RestoreBackground();
                LightLogger.Log("144");
            }
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
            LightLogger.Log("145");
            _showingPanel = true;
            if (_lightScreen != null) _lightScreen.SetActive(false);
            if (_lightSubScreen != null) _lightSubScreen.SetActive(false);
            if (_galleryPanel != null) _galleryPanel.Hide();
            LightLogger.Log("146");
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
            LightLogger.Log("147");
            _showingPanel = false;
            if (_lightScreen != null) _lightScreen.SetActive(false);
            if (_lightSubScreen != null) _lightSubScreen.SetActive(false);
            _galleryPanel?.Hide();
            DestroyableSingleton<AccountManager>.Instance
                ?.transform.FindChild("AccountTab/AccountWindow")?.gameObject.SetActive(false);
            LightLogger.Log("148");
        }
        catch (System.Exception ex)
        {
            LightLogger.LogWarning("[Light] MainMenuPatch.HideRightPanel NRE: " + ex.Message + "\n" + ex.StackTrace);
        }
    }
}
