using HarmonyLib;
using Light.Utilities;
using TMPro;
using UnityEngine;
using static Light.LightPlugin;

namespace Light.Patches;

/// <summary>
/// 主菜单 UI 完全对齐 NebulaPluginNova + nos 参考
/// 实现方式：仅使用 GameObject.Find / Transform.FindChild / GetComponentsInChildren
/// 零私有字段访问（兼容 IL2CPP）
/// </summary>
[HarmonyPatch]
public static class MainMenuPatch
{
    // ── 静态状态 ──
    private static bool _showingPanel;
    private static GameObject? _rightPanel;
    private static Vector3 _rightPanelOp;
    private static float _bgMoveDelay;
    private static bool _bgMoved;
    private static GameObject? _lightScreen;
    private static Dictionary<string, PassiveButton> FindButtons()
    {
        var dict = new Dictionary<string, PassiveButton>();
        var leftPanel = GameObject.Find("LeftPanel");
        if (leftPanel == null) return dict;
        foreach (var b in leftPanel.GetComponentsInChildren<PassiveButton>(true))
            if (b != null && !dict.ContainsKey(b.name))
                dict[b.name] = b;
        StaticLog.LogWarning(string.Format("[Light.UI] 找到 {0} 个按钮", dict.Count));
        return dict;
    }

    private static GameObject? FindGO(string name) => GameObject.Find(name);

    // ════════════════════════════════════════════
    //  主入口：MainMenuManager.Start Postfix
    // ════════════════════════════════════════════
    [HarmonyPatch(typeof(MainMenuManager), "Start")]
    [HarmonyPostfix]
    public static void Postfix(MainMenuManager __instance)
    {
        try
        {
            StaticLog.LogWarning("[Light.UI] === 开始布局 ===");

            FindGO("ReactorVersion")?.SetActive(false);
            var ambience = FindGO("Ambience");
            if (ambience != null)
                ambience.transform.FindChild("PlayerParticles")?.gameObject.SetActive(false);

            var bg = FindGO("BackgroundTexture");
            if (bg != null) { _bgMoved = false; bg.SetActive(true); }
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
            if (divider != null)
                divider.localPosition += new Vector3(0f, height, 0f);
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
                            btn.gameObject.SetActive(false);
                        else if (btnName.Contains("line") || btnName.Contains("divider"))
                            btn.gameObject.SetActive(false);
                    }
                }
            }

            CreateLightButton(__instance, btns, height);
            AdjustIcons();

            ColorAllButtons();

            if (leftPanel != null)
            {
                leftPanel.transform.localScale = new Vector3(0.9f, 0.9f, 0.9f);
                for (int i = 0; i < leftPanel.transform.childCount; i++)
                    leftPanel.transform.GetChild(i).SetParent(leftPanel.transform.parent);
                leftPanel.SetActive(false);
            }

            // 12. Divider 隐藏
            FindGO("Divider")?.SetActive(false);

            // 13. nos: RightPanel
            SetupRightPanel(btns);

            // 14. 创建 LightScreen（点击 LightButton 后展开的子菜单）
            SetupLightScreen(__instance);

            // 14. nos: ScreenTint
            MoveScreenTint(__instance);

            // 15. nos: BottomButtonBounds
            var bounds = FindGO("BottomButtonBounds");
            if (bounds != null)
                bounds.transform.localPosition -= new Vector3(0f, 0.1f, 0f);

            // 16. 美化：添加底部装饰渐变条
            var decoTex = new Texture2D(1, 1);
            decoTex.SetPixel(0, 0, UColor.white);
            decoTex.Apply();
            var decoSpr = Sprite.Create(decoTex, new Rect(0, 0, 1, 1),
                new Vector2(0.5f, 0.5f), 100f);
            var deco = UnityHelper.CreateObject<SpriteRenderer>("LightDecoLine",
                __instance.mainMenuUI.transform, new Vector3(0f, -3.2f, -2f));
            deco.sprite = decoSpr;
            deco.drawMode = SpriteDrawMode.Sliced;
            deco.size = new Vector2(8f, 0.02f);
            deco.color = new UColor(1f, 1f, 1f, 0.12f);

            // 17. 删除多余按钮
            foreach (var obj in UnityEngine.Resources.FindObjectsOfTypeAll<GameObject>())
                if (obj.name is "FreePlayButton" or "HowToPlayButton")
                    UnityEngine.Object.Destroy(obj);

            StaticLog.LogWarning("[Light.UI] === 布局完成 ===");
        }
        catch (System.Exception ex)
        {
            StaticLog.LogWarning("[Light.UI] 异常: " + ex);
        }
    }

    // ════════════════════════════════════════════
    //  创建 LightButton
    // ════════════════════════════════════════════
    private static void CreateLightButton(MainMenuManager __instance, Dictionary<string, PassiveButton> btns, float height)
    {
        if (!btns.TryGetValue("SettingsButton", out var settings)) return;

        var clone = GameObject.Instantiate(settings.gameObject, settings.transform.parent);
        clone.name = "LightButton";
        clone.transform.localPosition += new Vector3(0f, -height, 0f);

        // 图标替换（对齐 NebulaPluginNova）
        var tex = GraphicsHelper.LoadTextureFromResources("Light.Resources.Logo.LightInDark.png");
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

        // 点击：显示 LightScreen
        var passive = clone.GetComponent<PassiveButton>();
        passive.OnClick = new UnityEngine.UI.Button.ButtonClickedEvent();
        passive.OnClick.AddListener((UnityEngine.Events.UnityAction)(() =>
        {
            __instance.ResetScreen();
            _showingPanel = true;
            if (_lightScreen != null) _lightScreen.SetActive(true);
        }));

        // 文字
        var fp = clone.transform.FindChild("FontPlacer");
        if (fp != null && fp.childCount > 0)
        {
            var tmp = fp.GetChild(0).GetComponent<TextMeshPro>();
            if (tmp != null) tmp.text = "LIGHT";
            var trans = fp.GetChild(0).GetComponent<TextTranslatorTMP>();
            if (trans != null) trans.enabled = false;
        }

        // 注册到 scalerList
        var scalerList = GameObject.FindObjectOfType<SlicedAspectScaler>();
        if (scalerList != null)
        {
            var scaled = clone.GetComponent<AspectScaledAsset>();
            if (scaled != null) scalerList.objectsToScale.Add(scaled);
        }

        // 添加到场景按钮字典
        btns["LightButton"] = passive;
    }

    // ════════════════════════════════════════════
    //  按钮图标旋转/缩放/位移
    // ════════════════════════════════════════════
    private static void AdjustIcons()
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
            icon.localScale += new Vector3(0.12f, 0.12f, 0f);
            if (shouldMove)
            {
                var asp = icon.GetComponent<AspectPosition>();
                if (asp != null) { asp.DistanceFromEdge += new Vector3(-0.02f, 0.1f, 0f); asp.AdjustPosition(); }
                else icon.localPosition += new Vector3(-0.02f, 0.1f, 0f);
            }
        }
    }

    // ════════════════════════════════════════════
    //  按钮配色
    // ════════════════════════════════════════════
    private static void ColorAllButtons()
    {
        var leftPanel = FindGO("LeftPanel");
        if (leftPanel == null) return;
        var pink = new Color32(255, 192, 203, 255);
        var purple = new Color32(148, 112, 219, 255);
        var clear = new UColor(0f, 0f, 0f, 0f);

        foreach (var btn in leftPanel.GetComponentsInChildren<PassiveButton>(true))
        {
            if (btn == null) continue;
            var n = btn.name;
            if (n is "NewsButton" or "AcountButton" or "SettingsButton" or "LightButton")
                FormatBtn(btn, pink, clear, UColor.white, UColor.white);
            else if (n is "CreditsButton" or "ExitGameButton")
                FormatBtn(btn, purple, clear, UColor.white, UColor.white);
        }
    }

    private static void FormatBtn(PassiveButton btn, UColor inactive, UColor active,
        UColor inactText, UColor actText)
    {
        HideShine(btn.activeSprites);
        HideShine(btn.inactiveSprites);
        if (btn.activeSprites != null)
        {
            var sr = btn.activeSprites.GetComponent<SpriteRenderer>();
            if (sr != null) sr.color = active.a == 0f
                    ? new UColor(inactive.r, inactive.g, inactive.b, 1f) : active;
        }
        if (btn.inactiveSprites != null)
        {
            var sr = btn.inactiveSprites.GetComponent<SpriteRenderer>();
            if (sr != null) sr.color = inactive;
        }
        btn.activeTextColor = actText;
        btn.inactiveTextColor = inactText;
    }

    private static void HideShine(GameObject? parent)
    {
        if (parent == null) return;
        parent.transform.FindChild("Shine")?.gameObject.SetActive(false);
    }
    private static void SetupRightPanel(Dictionary<string, PassiveButton> btns)
    {
        _rightPanel = FindGO("RightPanel");
        if (_rightPanel == null) return;

        var asp = _rightPanel.GetComponent<AspectPosition>();
        if (asp != null) UnityEngine.Object.Destroy(asp);

        _rightPanelOp = _rightPanel.transform.localPosition;
        _rightPanel.transform.localPosition = _rightPanelOp + new Vector3(10f, 0f, 0f);

        var sr = _rightPanel.GetComponent<SpriteRenderer>();
        if (sr != null) sr.color = new Color32(173, 214, 255, 255);

        // LOGO — 挂到 playButton 下（nos 做法）
        if (!btns.TryGetValue("PlayButton", out var playBtn)) return;

        var tex = GraphicsHelper.LoadTextureFromResources("Light.Resources.Logo.LightInDark.png");
        if (tex == null) return;
        var spr = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height),
            new Vector2(0.5f, 0.5f), 100f);

        var logo = UnityHelper.CreateObject<SpriteRenderer>("LightLogo",
            playBtn.transform, new Vector3(0.1f, 1f, 1f));
        logo.transform.localScale = new Vector3(0.3f, 0.3f, 1f);
        logo.sprite = spr;

        var glow = UnityHelper.CreateObject<SpriteRenderer>("LightLogoGlow",
            playBtn.transform, new Vector3(0.1f, 1f, 1f));
        glow.transform.localScale = new Vector3(0.3f, 0.3f, 1f);
        glow.sprite = spr;
        glow.color = UColor.white;

        // 版本号
        var binder = UnityHelper.CreateObject("VersionText", logo.transform,
            new Vector3(0.7f, -0.45f, 0f));
        var btnText = playBtn.buttonText;
        if (btnText != null)
        {
            var ver = UnityEngine.Object.Instantiate(btnText, binder.transform);
            ver.gameObject.SetActive(true);
            var tr = ver.GetComponent<TextTranslatorTMP>();
            if (tr != null) tr.enabled = false;
            ver.alignment = TextAlignmentOptions.Center;
            ver.color = UColor.white;
            ver.text = VisualVersion;
        }
    }

    // ════════════════════════════════════════════
    //  LightScreen（点击 LightButton 展开的子菜单）
    // ════════════════════════════════════════════
    private static void SetupLightScreen(MainMenuManager __instance)
    {
        // 克隆 accountButtons 作为子菜单面板
        _lightScreen = GameObject.Instantiate(__instance.accountButtons,
            __instance.accountButtons.transform.parent);
        _lightScreen.name = "LightScreen";

        // 标题
        var titleText = _lightScreen.transform.GetChild(0).GetChild(0)
            .GetComponent<TextMeshPro>();
        if (titleText != null) titleText.text = "Light In The Dark";
        var titleTrans = _lightScreen.transform.GetChild(0).GetChild(0)
            .GetComponent<TextTranslatorTMP>();
        if (titleTrans != null) titleTrans.enabled = false;

        // 删除第4个孩子（对齐 NebulaPluginNova）
        var child4 = _lightScreen.transform.GetChild(4);
        if (child4 != null) GameObject.Destroy(child4.gameObject);

        // 使用第3个孩子作为按钮模板
        var temp = _lightScreen.transform.GetChild(3);
        int index = 0;

        void SetUpBtn(string text, System.Action clickAction)
        {
            GameObject obj = temp.gameObject;
            if (index > 0) obj = GameObject.Instantiate(obj, obj.transform.parent);

            var tmp = obj.transform.GetChild(0).GetChild(0).GetComponent<TextMeshPro>();
            if (tmp != null) tmp.text = text;
            var tr = obj.transform.GetChild(0).GetChild(0)
                .GetComponent<TextTranslatorTMP>();
            if (tr != null) tr.enabled = false;

            var pb = obj.GetComponent<PassiveButton>();
            pb.OnClick = new UnityEngine.UI.Button.ButtonClickedEvent();
            pb.OnClick.AddListener((UnityEngine.Events.UnityAction)(() => clickAction()));

            obj.transform.localPosition = new Vector3(
                (index % 2 == 0) ? -1.45f : 1.45f,
                0.98f - (index / 2) * 0.59f, 0f);
            obj.transform.localScale = new Vector3(0.72f, 0.72f, 1f);
            index++;
        }

        // 添加子按钮（占位，后续可扩展具体功能）
        SetUpBtn("模组设置", () => StaticLog.LogWarning("[Light] 模组设置 - 待实现"));
        SetUpBtn("关于模组", () => StaticLog.LogWarning("[Light] 关于 - 待实现"));
        SetUpBtn("成就", () => StaticLog.LogWarning("[Light] 成就 - 待实现"));
        SetUpBtn("Discord", () => Application.OpenURL("https://discord.gg/"));

        // 注册到 scalerList
        var scalerList = GameObject.FindObjectOfType<SlicedAspectScaler>();
        if (scalerList != null)
            foreach (var asset in _lightScreen.GetComponentsInChildren<AspectScaledAsset>())
                scalerList.objectsToScale.Add(asset);

        _lightScreen.SetActive(false);
    }

    // ════════════════════════════════════════════
    //  ScreenTint
    // ════════════════════════════════════════════
    private static void MoveScreenTint(MainMenuManager __instance)
    {
        // 场景中查找 — mainUI 下 or 根级
        SpriteRenderer? tint = null;
        var go = FindGO("ScreenTint") ?? __instance.transform.FindChild("ScreenTint")?.gameObject;
        // 也查 mainMenuUI 下的 Tint
        if (go == null)
            go = __instance.mainMenuUI.transform.FindChild("Tint")?.gameObject;
        if (go != null) tint = go.GetComponent<SpriteRenderer>();

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

    // ════════════════════════════════════════════
    //  LateUpdate 动画
    // ════════════════════════════════════════════
    [HarmonyPatch(typeof(MainMenuManager), "LateUpdate")]
    [HarmonyPostfix]
    public static void LateUpdate()
    {
        if (FindGO("MainUI") == null) _showingPanel = false;

        // LightScreen 跟随 _showingPanel 自动显隐
        if (!_showingPanel && _lightScreen != null && _lightScreen.activeSelf)
            _lightScreen.SetActive(false);

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

        if (_bgMoved) return;
        var bg = FindGO("BackgroundTexture");
        if (bg == null) return;
        _bgMoveDelay += Time.deltaTime;
        if (_bgMoveDelay <= 3f) return;
        var pos = bg.transform.position;
        bg.transform.position = Vector3.Lerp(pos, new Vector3(pos.x, 7.1f, pos.z),
            Time.deltaTime * 1.4f);
        if (pos.y > 7f) _bgMoved = true;
    }

    // ════════════════════════════════════════════
    //  RightPanel 显示/隐藏触发
    // ════════════════════════════════════════════
    [HarmonyPatch(typeof(MainMenuManager), "OpenGameModeMenu")]
    [HarmonyPatch(typeof(MainMenuManager), "OpenAccountMenu")]
    [HarmonyPatch(typeof(MainMenuManager), "OpenCredits")]
    [HarmonyPrefix, HarmonyPriority(0)]
    public static void ShowRightPanel()
    {
        _showingPanel = true;
        if (_lightScreen != null) _lightScreen.SetActive(false);
    }

    [HarmonyPatch(typeof(MainMenuManager), "Start")]
    [HarmonyPatch(typeof(OptionsMenuBehaviour), "Open")]
    [HarmonyPatch(typeof(AnnouncementPopUp), "Show")]
    [HarmonyPrefix, HarmonyPriority(0)]
    public static void HideRightPanel()
    {
        _showingPanel = false;
        if (_lightScreen != null) _lightScreen.SetActive(false);
        DestroyableSingleton<AccountManager>.Instance
            ?.transform.FindChild("AccountTab/AccountWindow")?.gameObject.SetActive(false);
    }
}
