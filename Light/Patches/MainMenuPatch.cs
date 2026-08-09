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

namespace Light.Patches;

/// <summary>
/// 主菜单 UI
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
    private static GameObject? _lightScreen;
    private static GameObject? _lightSubScreen;
    private static ImageGalleryPanel? _galleryPanel;
    private static bool _bgMoved;
    private static float _bgMoveDelay;

    // ── 工具：从场景查找按钮 ──
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

    private static bool _updaterChecked;
    private static bool _lateUpdateLogged;

    // ════════════════════════════════════════════
    //  主入口：MainMenuManager.Start Postfix
    // ════════════════════════════════════════════
    [HarmonyPatch(typeof(MainMenuManager), "Start")]
    [HarmonyPostfix]
    public static void Postfix(MainMenuManager __instance)
    {
        try
        {
            // 0. 清除上一场景的残留引用（对象已被销毁）
            _showingPanel = false;
            _rightPanel = null;
            _lightScreen = null;
            _lightSubScreen = null;
            _galleryPanel = null;

            StaticLog.LogWarning("[Light.UI] === 开始布局 ===");

            // 0. 预加载 UI 资源（TwitchManager 此时已就绪）
            Light.UI.Window.VanillaAsset.Preload();

            // 1. 替换模组图标 + 隐藏干扰文字 + 禁用 Ambience 下的 PlayerParticles
            var modStamp = FindGO("ModStamp");
            if (modStamp != null)
            {
                modStamp.SetActive(true);
                modStamp.transform.localScale = Vector3.one * 0.06f;
                var sr = modStamp.GetComponent<SpriteRenderer>();
                if (sr != null)
                    sr.sprite = ResourceHelper.LoadSpriteFromResource("Light.Resources.ModStamp.png");
            }
            FindGO("ReactorVersion")?.SetActive(false);
            var ambience = FindGO("Ambience");
            if (ambience != null)
            {
                ambience.transform.FindChild("PlayerParticles")?.gameObject.SetActive(false);
                ambience.transform.GetChild(0).gameObject.SetActive(false);
            }
            UnityEngine.Object.FindObjectOfType<VersionShower>()?.gameObject.SetActive(false);

            // 2. 背景激活（每次重新滚动，不持久化 _bgMoved）
            _bgMoved = false;
            _bgMoveDelay = 0f;
            var bg = FindGO("BackgroundTexture");
            if (bg != null)
            {
                bg.SetActive(true);
            }

            // 3. 查按钮
            var btns = FindButtons();

            // 4. AU Logo 缩放+位移
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

            // 5. 计算按钮间距 + 上移
            float height = 0.7f; // 默认值
            if (btns.TryGetValue("NewsButton", out var news) && btns.TryGetValue("AcountButton", out var acct))
                height = news.transform.localPosition.y - acct.transform.localPosition.y;

            // 遍历所有按钮上移（从 btns 字典，确保非空）
            foreach (var kvp in btns)
            {
                var btn = kvp.Value;
                if (btn != null && Mathf.Abs(btn.transform.localPosition.x) < 0.1f)
                    btn.transform.localPosition += new Vector3(0f, height, 0f);
            }

            // Divider 上移
            var divider = leftPanel?.transform.FindChild("Main Buttons")?.FindChild("Divider");
            if (divider != null)
                divider.localPosition += new Vector3(0f, height, 0f);

            // 6. ReworkedLeftPanel
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

            // 7. OnlineButtons 布局
            // onlineButtonsContainer 路径：mainMenuUI/AspectScaler/Online Buttons
            var onlineRoot = __instance.mainMenuUI.transform
                .FindChild("AspectScaler")?.FindChild("Online Buttons");
            if (onlineRoot != null)
            {
                // 使用 GetChild(1) 作为 scaler
                for (int i = 0; i < onlineRoot.childCount; i++)
                {
                    var child = onlineRoot.GetChild(i);
                    // 查找 scaler 下的按钮
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

            // 8. 创建 LightButton
            CreateLightButton(__instance, btns, height);

            // 9. 按钮图标调整
            AdjustIcons();

            // 10. 按钮配色（在解构 LeftPanel 之前执行，否则找不到子按钮）
            ColorAllButtons();

            // 10b. 按钮呼吸灯效果
            ApplyButtonEffects(__instance);

            // 11. LeftPanel 解构（子释放 + 隐藏）
            if (leftPanel != null)
            {
                leftPanel.transform.localScale = new Vector3(0.9f, 0.9f, 0.9f);
                for (int i = 0; i < leftPanel.transform.childCount; i++)
                    leftPanel.transform.GetChild(i).SetParent(leftPanel.transform.parent);
                leftPanel.SetActive(false);
            }

            // 12. Divider 隐藏
            FindGO("Divider")?.SetActive(false);

            // 13. RightPanel
            SetupRightPanel(__instance);

            // 14. 创建 LightScreen（点击 LightButton 后展开的子菜单）
            SetupLightScreen(__instance);

            // 14b. 创建子面板（点击"更多功能"后展开）
            SetupSubScreen(__instance);

            // 14c. 创建图片画廊面板（点击"功能A"后展开）
            SetupGalleryScreen(__instance);

            // 14d. 恢复自定义背景（场景切换后 _bgObj 被销毁，需重建）
            _galleryPanel?.RestoreBackground();

            // 14. ScreenTint
            MoveScreenTint(__instance);

            // 15. BottomButtonBounds
            var bounds = FindGO("BottomButtonBounds");
            if (bounds != null)
                bounds.transform.localPosition -= new Vector3(0f, 0.1f, 0f);

            // 16. 美化：添加底部装饰渐变条
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

            // 17. 隐藏多余按钮（与 FS 一致：SetActive(false)，不 Destroy）
            foreach (var obj in UnityEngine.Resources.FindObjectsOfTypeAll<GameObject>())
                if (obj.name is "FreePlayButton" or "HowToPlayButton")
                    obj.SetActive(false);

            StaticLog.LogWarning("[Light.UI] === 布局完成 ===");

            // 延迟检测更新器是否存在（等待 DisconnectPopup 初始化）
            if (!_updaterChecked)
            {
                _updaterChecked = true;
                __instance.StartCoroutine(CoCheckUpdater().WrapToIl2Cpp());
            }
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

        try
        {
            string updaterPath = Path.Combine(BepInEx.Paths.GameRootPath, VersionMaker.UpdaterExeName);
            if (!File.Exists(updaterPath))
            {
                LightLogger.LogWarning($"未找到更新脚本：{VersionMaker.UpdaterExeName}");
                AmongUsEdited.ShowCustomDisconnectWindow($"未找到更新脚本 {VersionMaker.UpdaterExeName}！\n请将其放置在游戏根目录下，否则无法正常检查更新。");
            }
        }
        catch { }
    }

    private static void CreateLightButton(MainMenuManager __instance, Dictionary<string, PassiveButton> btns, float height)
    {
        if (!btns.TryGetValue("SettingsButton", out var settings)) return;

        var clone = GameObject.Instantiate(settings.gameObject, settings.transform.parent);
        clone.name = "LightButton";
        clone.transform.localPosition += new Vector3(0f, -height, 0f);

        // 图标替换
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

        // 点击：显示 LightScreen
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
            if (name != "LightButton") icon.localScale += new Vector3(0.12f, 0.12f, 0f);
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

    private static void FormatBtn(PassiveButton btn, Color inactive, Color active,
        Color inactText, Color actText)
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

    private static void HideShine(GameObject? parent)
    {
        if (parent == null) return;
        parent.transform.FindChild("Shine")?.gameObject.SetActive(false);
    }

    private static void ApplyButtonEffects(MainMenuManager __instance)
    {
        ButtonBreathEffect.Init();
    }

    // ════════════════════════════════════════════
    //  RightPanel
    // ════════════════════════════════════════════
    private static void SetupRightPanel(MainMenuManager __instance)
    {
        _rightPanel = FindGO("RightPanel");
        if (_rightPanel == null) return;

        var asp = _rightPanel.GetComponent<AspectPosition>();
        if (asp != null) UnityEngine.Object.Destroy(asp);

        _rightPanelOp = _rightPanel.transform.localPosition;
        _rightPanel.transform.localPosition = _rightPanelOp + new Vector3(10f, 0f, 0f);

        var sr = _rightPanel.GetComponent<SpriteRenderer>();
        if (sr != null) sr.color = new Color32(173, 214, 255, 255);

        // LOGO — 挂到 playButton 下
        if (__instance.playButton == null) return;

        var tex = GraphicsHelper.LoadTextureFromResources("Light.Resources.Lobby.LightInDark.png");
        if (tex == null) return;
        var spr = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height),
            new Vector2(0.5f, 0.5f), 100f);

        var logo = UnityHelper.CreateObject<SpriteRenderer>("LightLogo",
            __instance.playButton.transform, new Vector3(0.1f, 1f, 1f));
        logo.transform.localScale = new Vector3(0.12f, 0.12f, 1f);
        logo.sprite = spr;

        var glow = UnityHelper.CreateObject<SpriteRenderer>("LightLogoGlow",
            __instance.playButton.transform, new Vector3(0.1f, 1f, 1f));
        glow.transform.localScale = new Vector3(0.12f, 0.12f, 1f);
        glow.sprite = spr;
        glow.color = Color.white;
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
        if (titleText != null)
        {
            titleText.text = "Light In The Dark";
            titleText.fontSize = 4.5f;
        }
        var titleTrans = _lightScreen.transform.GetChild(0).GetChild(0)
            .GetComponent<TextTranslatorTMP>();
        if (titleTrans != null) titleTrans.enabled = false;

        // 删除第4个孩子
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
        SetUpBtn("关于模组", () => Light.UI.Help.HelpScreen.TryOpenHelpScreen());
        SetUpBtn("成就", () => StaticLog.LogWarning("[Light] 成就 - 待实现"));
        SetUpBtn("Discord", () => Application.OpenURL("https://discord.gg/"));
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

        // 打开子面板
        SetUpBtn("更多功能", () =>
        {
            if (_lightScreen != null) _lightScreen.SetActive(false);
            if (_lightSubScreen != null) _lightSubScreen.SetActive(true);
        });

        // 注册到 scalerList
        var scalerList = GameObject.FindObjectOfType<SlicedAspectScaler>();
        if (scalerList != null)
            foreach (var asset in _lightScreen.GetComponentsInChildren<AspectScaledAsset>())
                scalerList.objectsToScale.Add(asset);

        _lightScreen.SetActive(false);
    }

    // ════════════════════════════════════════════
    //  SubScreen（子面板）
    // ════════════════════════════════════════════
    private static void SetupSubScreen(MainMenuManager __instance)
    {
        var panel = new LightPanel(__instance, "LightSubScreen", "更多功能");

        // 添加按钮（默认两列排版）
        // "功能A" → 打开图片画廊面板
        panel.AddButton("背景板", () =>
        {
            if (_lightSubScreen != null) _lightSubScreen.SetActive(false);
            _galleryPanel?.Show();
        });
        panel.AddButton("功能B", () => LightLogger.Log("[Light] 功能B - 待实现"));
        panel.AddButton("功能C", () => LightLogger.Log("[Light] 功能C - 待实现"));
        panel.AddButton("功能D", () => LightLogger.Log("[Light] 功能D - 待实现"));

        // 返回按钮（自定义坐标，回到主面板）
        if (_lightScreen != null)
            panel.AddBackButton(_lightScreen);

        panel.RegisterToScaler();
        panel.Hide();

        _lightSubScreen = panel.Panel;
    }

    // ════════════════════════════════════════════
    //  GalleryScreen（图片画廊面板）
    // ════════════════════════════════════════════
    private static void SetupGalleryScreen(MainMenuManager __instance)
    {
        var gallery = new ImageGalleryPanel("60768f3185be4d77d40de1d75df44140704cead716175ea8915bd42e08d7c8e5");

        gallery.AddImage("At the Polus Core", "曦曦", "60768f3185be4d77d40de1d75df44140704cead716175ea8915bd42e08d7c8e5",
            index => { /* 下载逻辑 */ });
        gallery.AddImage("Default", "Inner Sloth", "",
            index => { /* 下载逻辑 */ });

        gallery.SetHashFailedFallback(() =>
        {
            LightLogger.LogWarning("[Light] BG_Default 哈希验证失败，请检查资源完整性");
        });

        gallery.SetApplyCallback(index =>
        {
            LightLogger.Log($"[Light] 应用背景图 #{index}");
            gallery.Reload();
            ButtonBreathEffect.Reload();
        });

        if (_lightSubScreen != null)
            gallery.OnBack = () => _lightSubScreen.SetActive(true);

        gallery.ApplyDefaultBackground();

        _galleryPanel = gallery;
    }

    /// <summary>
    /// 可复用子面板，基于 accountButtons 克隆结构。
    /// 支持添加按钮、自定义坐标（默认两列排版）、返回按钮。
    /// </summary>
    private class LightPanel
    {
        private readonly GameObject _panel;
        private readonly Transform _buttonTemplate;
        private int _index;

        public LightPanel(MainMenuManager instance, string panelName, string title)
        {
            _panel = GameObject.Instantiate(instance.accountButtons,
                instance.accountButtons.transform.parent);
            _panel.name = panelName;

            var titleText = _panel.transform.GetChild(0).GetChild(0)
                .GetComponent<TextMeshPro>();
            if (titleText != null) titleText.text = title;
            var titleTrans = _panel.transform.GetChild(0).GetChild(0)
                .GetComponent<TextTranslatorTMP>();
            if (titleTrans != null) titleTrans.enabled = false;

            var child4 = _panel.transform.GetChild(4);
            if (child4 != null) GameObject.Destroy(child4.gameObject);

            _buttonTemplate = _panel.transform.GetChild(3);
        }

        /// <summary>
        /// 添加按钮。
        /// </summary>
        /// <param name="text">按钮文字</param>
        /// <param name="clickAction">点击回调</param>
        /// <param name="position">自定义坐标，为 null 时使用默认两列排版</param>
        public void AddButton(string text, System.Action clickAction, Vector3? position = null)
        {
            GameObject obj = _buttonTemplate.gameObject;
            if (_index > 0) obj = GameObject.Instantiate(obj, obj.transform.parent);

            var tmp = obj.transform.GetChild(0).GetChild(0).GetComponent<TextMeshPro>();
            if (tmp != null) tmp.text = text;
            var tr = obj.transform.GetChild(0).GetChild(0)
                .GetComponent<TextTranslatorTMP>();
            if (tr != null) tr.enabled = false;

            var pb = obj.GetComponent<PassiveButton>();
            pb.OnClick = new UnityEngine.UI.Button.ButtonClickedEvent();
            pb.OnClick.AddListener((UnityEngine.Events.UnityAction)(() => clickAction()));

            obj.transform.localPosition = position ?? new Vector3(
                (_index % 2 == 0) ? -1.45f : 1.45f,
                0.98f - (_index / 2) * 0.59f, 0f);
            obj.transform.localScale = new Vector3(0.72f, 0.72f, 1f);
            _index++;
        }

        /// <summary>
        /// 添加返回按钮，点击后隐藏本面板并显示目标面板。
        /// </summary>
        /// <param name="targetPanel">返回时要显示的面板</param>
        /// <param name="position">自定义坐标，为 null 时放在底部居中</param>
        public void AddBackButton(GameObject targetPanel, Vector3? position = null)
        {
            AddButton("返回", () =>
            {
                _panel.SetActive(false);
                targetPanel.SetActive(true);
            }, position ?? new Vector3(0f, -1.5f, 0f));
        }

        public void RegisterToScaler()
        {
            var scalerList = GameObject.FindObjectOfType<SlicedAspectScaler>();
            if (scalerList != null)
                foreach (var asset in _panel.GetComponentsInChildren<AspectScaledAsset>())
                    scalerList.objectsToScale.Add(asset);
        }

        public GameObject Panel => _panel;
        public void Show() => _panel.SetActive(true);
        public void Hide() => _panel.SetActive(false);
    }

    // ════════════════════════════════════════════
    //  ScreenTint
    // ════════════════════════════════════════════
    private static void MoveScreenTint(MainMenuManager __instance)
    {
        // 直接用实例的 screenTint 字段
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

    // ════════════════════════════════════════════
    //  LateUpdate 动画
    // ════════════════════════════════════════════
    [HarmonyPatch(typeof(MainMenuManager), "LateUpdate")]
    [HarmonyPostfix]
    public static void LateUpdate()
    {
      try
      {
        if (FindGO("MainUI") == null) _showingPanel = false;

        ButtonBreathEffect.Update();

        // 调试：确认 LateUpdate postfix 正在运行（仅第一帧输出）
        if (!_lateUpdateLogged)
        {
            _lateUpdateLogged = true;
            LightLogger.Log("[Light] LateUpdate postfix 已激活");
        }

        // LightScreen / SubScreen 跟随 _showingPanel 自动显隐
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

        // FS 模式：背景上移动画（只执行一次），完成后 SetActive(false)（与 FS 一致）
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

        // 背景视差：鼠标移动时自定义背景微移
        if (_bgMoved && _galleryPanel != null && Camera.main != null)
        {
            var mousePos = Input.mousePosition;
            var screenCenter = new Vector3(Screen.width * 0.5f, Screen.height * 0.5f, 0f);
            var offset = (mousePos - screenCenter) / screenCenter.magnitude;
            _galleryPanel.UpdateParallax(offset);
        }
      }
      catch (System.Exception ex)
      {
          LightLogger.LogWarning("[Light] LateUpdate NRE: " + ex.Message + "\n" + ex.StackTrace);
          // 清除可能已失效的引用
          _rightPanel = null;
          _lightScreen = null;
          _lightSubScreen = null;
          _galleryPanel = null;
      }
    }
    // ════════════════════════════════════════════
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
}
