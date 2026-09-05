using Il2CppInterop.Runtime;
using LightInDark;
using LightInDark.UI.Window;
using TMPro;
using Twitch;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using LightInDark.Core;
using System;
using Object = UnityEngine.Object;
using Button = UnityEngine.UI.Button;
using Color = LightInDark.Color;
using UnityHelper = Light.Utilities.UnityHelper;

namespace Light.UI.Window;

/// <summary>
/// Among Us 原生资源缓存。
/// 延迟加载，用户可自定义所有材质。
/// </summary>
public static class VanillaAsset
{
    private static Sprite? _popUpBackSprite;
    private static Sprite? _textButtonSprite;
    private static Sprite? _fullScreenSprite;
    private static Sprite? _closeButtonSprite;
    private static TextMeshPro? _standardTextPrefab;
    private static Material? _standardMaskedFontMaterial;
    private static Material? _oblongMaskedFontMaterial;
    private static TMP_FontAsset? _versionFont;
    private static TMP_FontAsset? _preSpawnFont;
    private static TMP_FontAsset? _brookFont;
    private static PlayerCustomizationMenu? _playerOptionsMenuPrefab;
    private static bool _triedInit;

    /// <summary>弹窗背景 Sprite</summary>
    public static Sprite PopUpBackSprite => TryGetSprite(ref _popUpBackSprite);
    /// <summary>按钮背景 Sprite</summary>
    public static Sprite TextButtonSprite => TryGetSprite(ref _textButtonSprite);
    /// <summary>全屏遮罩 Sprite</summary>
    public static Sprite FullScreenSprite => TryGetSprite(ref _fullScreenSprite);
    /// <summary>关闭按钮 Sprite</summary>
    public static Sprite CloseButtonSprite => TryGetSprite(ref _closeButtonSprite);

    /// <summary>标准文本预制体（TextMeshPro）</summary>
    public static TextMeshPro StandardTextPrefab
    {
        get
        {
            try
            {
                EnsureLoaded();
                return _standardTextPrefab!;
            }
            catch (Exception ex)
            {
                LightLogger.LogError("[VanillaAsset.get]", ex); return default;
            }
        }
    }

    /// <summary>遮罩字体材质</summary>
    public static Material StandardMaskedFontMaterial
    {
        get
        {
            try
            {
                EnsureLoaded();
                return _standardMaskedFontMaterial!;
            }
            catch (Exception ex)
            {
                LightLogger.LogError("[VanillaAsset.get]", ex); return default;
            }
        }
    }

    /// <summary>Barlow 字体</summary>
    public static TMP_FontAsset VersionFont
    {
        get
        {
            try
            {
                EnsureLoaded();
                return _versionFont;
            }
            catch (Exception ex)
            {
                LightLogger.LogError("[VanillaAsset.get]", ex); return default;
            }
        }
    }

    /// <summary>DIN_Pro 字体</summary>
    public static TMP_FontAsset PreSpawnFont
    {
        get
        {
            try
            {
                EnsureLoaded();
                return _preSpawnFont!;
            }
            catch (Exception ex)
            {
                LightLogger.LogError("[VanillaAsset.get]", ex); return default;
            }
        }
    }

    /// <summary>Brook 字体</summary>
    public static TMP_FontAsset BrookFont
    {
        get
        {
            try
            {
                EnsureLoaded();
                return _brookFont!;
            }
            catch (Exception ex)
            {
                LightLogger.LogError("[VanillaAsset.get]", ex); return default;
            }
        }
    }

    private static Sprite? _whiteSprite;
    /// <summary>白色降级 Sprite</summary>
    public static Sprite WhiteSprite
    {
        get
        {
            try
            {
                if (_whiteSprite != null) return _whiteSprite;
                var tex = Texture2D.whiteTexture;
                _whiteSprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f);
                return _whiteSprite;
            }
            catch (Exception ex)
            {
                LightLogger.LogError("[VanillaAsset.get]", ex); return default;
            }
        }
    }

    private static Sprite TryGetSprite(ref Sprite? field)
    {
        try
        {
            EnsureLoaded();
            return field ?? WhiteSprite;
        }
        catch (Exception ex)
        {
            LightLogger.LogError("[VanillaAsset.TryGetSprite]", ex); return default;
        }
    }

    private static void EnsureLoaded()
    {
        try
        {
            // TwitchManager 未就绪时不锁死标记，下次再尝试
            if (_standardTextPrefab != null) return;

            try
            {
                var twitch = TwitchManager.Instance;
                if (twitch == null) return;
                var popUp = twitch.transform.GetChild(0);

                _fullScreenSprite = popUp.GetChild(0).GetComponent<SpriteRenderer>().sprite;
                _textButtonSprite = popUp.GetChild(2).GetComponent<SpriteRenderer>().sprite;
                _popUpBackSprite = popUp.GetChild(3).GetComponent<SpriteRenderer>().sprite;

                // 克隆文本预制体
                _standardTextPrefab = Object.Instantiate(popUp.GetChild(1).GetComponent<TextMeshPro>(), null);
                _standardTextPrefab.gameObject.hideFlags = HideFlags.HideAndDontSave;
                Object.Destroy(_standardTextPrefab.GetComponent<SpriteRenderer>());
                Object.DontDestroyOnLoad(_standardTextPrefab.gameObject);

                _triedInit = true;
            }
            catch
            {
                // TwitchManager 未就绪或结构变化，下次重试
            }

            try
            {
                _closeButtonSprite = FindAsset<Sprite>("closeButton");
            }
            catch { }

            try
            {
                _standardMaskedFontMaterial = FindAsset<Material>("LiberationSans SDF - BlackOutlineMasked");
            }
            catch { }

            try
            {
                _oblongMaskedFontMaterial = FindAsset<Material>("Brook Atlas Material Masked");
            }
            catch { }

            try { _versionFont = FindAsset<TMP_FontAsset>("Barlow-Medium SDF"); } catch { }
            try { _preSpawnFont = FindAsset<TMP_FontAsset>("DIN_Pro_Bold_700 SDF"); } catch { }
            try { _brookFont = FindAsset<TMP_FontAsset>("Brook SDF"); } catch { }
        }
        catch (Exception ex)
        {
            LightLogger.LogError("[VanillaAsset.EnsureLoaded]", ex);
        }
    }

    /// <summary>游戏内兜底标准文本预制体（多级来源，任一可用即克隆）</summary>
    public static TextMeshPro FallbackTextPrefab
    {
        get
        {
            try
            {
                if (_fallbackTextPrefab != null) return _fallbackTextPrefab;

                // 来源1：左下角版本号文本
                TextMeshPro? src = null;
                try
                {
                    var vs = Object.FindObjectOfType<VersionShower>();
                    if (vs != null && vs.text != null) src = vs.text;
                }
                catch { }

                // 来源2：对话框目标文本（游戏中必然存在）
                if (src == null)
                {
                    try
                    {
                        var dialogue = HudManager.Instance?.Dialogue?.target;
                        if (dialogue != null) src = dialogue;
                    }
                    catch { }
                }

                // 来源3：场景内任意 TextMeshPro
                if (src == null)
                {
                    try
                    {
                        var any = Object.FindObjectOfType<TextMeshPro>();
                        if (any != null) src = any;
                    }
                    catch { }
                }

                if (src != null)
                {
                    try
                    {
                        _fallbackTextPrefab = Object.Instantiate(src, null);
                        _fallbackTextPrefab.gameObject.hideFlags = HideFlags.HideAndDontSave;
                        Object.DontDestroyOnLoad(_fallbackTextPrefab.gameObject);
                    }
                    catch { }
                }
                return _fallbackTextPrefab!;
            }
            catch (Exception ex)
            {
                LightLogger.LogError("[VanillaAsset.get]", ex); return default;
            }
        }
    }

    private static TextMeshPro? _fallbackTextPrefab;

    /// <summary>获取可用的标准文本预制体（优先 TwitchManager，失败时走多级兜底）</summary>
    public static TextMeshPro GetStandardTextPrefab()
    {
        try
        {
            var prefab = StandardTextPrefab;
            if (prefab != null) return prefab;
            return FallbackTextPrefab;
        }
        catch (Exception ex)
        {
            LightLogger.LogError("[VanillaAsset.GetStandardTextPrefab]", ex); return default;
        }
    }

    /// <summary>主菜单预加载（尽早缓存资源，避免游戏内首次打开时资源缺失）</summary>
    public static void Preload()
    {
        try
        {
            EnsureLoaded();
            _ = GetStandardTextPrefab();
        }
        catch (Exception ex)
        {
            LightLogger.LogError("[VanillaAsset.Preload]", ex);
        }
    }

    private static T? FindAsset<T>(string name) where T : Object
    {
        var type = Il2CppType.Of<T>();
        // 含未加载资产（Nebula 同款方式），否则部分材质/字体找不到
        foreach (var obj in Object.FindObjectsOfTypeIncludingAssets(type))
        {
            if (obj.name == name)
                return obj.TryCast<T>();
        }
        return null;
    }

    /// <summary>玩家自定义菜单预制体（含原版滚动条），懒加载，找不到返回 null</summary>
    public static PlayerCustomizationMenu? PlayerOptionsMenuPrefab
    {
        get
        {
            try
            {
                if (_playerOptionsMenuPrefab == null)
                    _playerOptionsMenuPrefab = FindAsset<PlayerCustomizationMenu>("LobbyPlayerCustomizationMenu");
                return _playerOptionsMenuPrefab;
            }
            catch (Exception ex)
            {
                LightLogger.LogError("[VanillaAsset.get]", ex); return default;
            }
        }
    }

    public static AudioClip? FindSoundClip(string name)
    {
        try
        {
            var type = Il2CppType.Of<AudioClip>();
            foreach (var obj in Resources.FindObjectsOfTypeAll(type))
            {
                if (obj.name == name)
                    return obj.TryCast<AudioClip>();
            }
            return null;
        }
        catch (Exception ex)
        {
            LightLogger.LogError("[VanillaAsset.FindSoundClip]", ex); return default;
        }
    }

    /// <summary>获取可用按钮背景</summary>
    public static Sprite GetButtonSprite() => TextButtonSprite;
    /// <summary>获取可用窗口背景</summary>
    public static Sprite GetWindowSprite() => PopUpBackSprite;
    /// <summary>获取可用全屏遮罩</summary>
    public static Sprite GetFullScreenSprite() => FullScreenSprite;

    /// <summary>
    /// 创建原版滚动组件（克隆原版玩家菜单的滚动条）
    /// </summary>
    public static Scroller GenerateScroller(Vector2 size, Transform parent, Vector3 scrollBarLocalPos,
        Transform target, FloatRange bounds, float scrollerHeight)
    {
        try
        {
            var scroller = UnityHelper.CreateObject<Scroller>("Scroller", parent, new Vector3(0f, 0f, 5f));
            var collider = scroller.gameObject.AddComponent<BoxCollider2D>();
            collider.size = size;

            // 主菜单等场景无此 prefab 时跳过滚动条克隆，仅保留滚轮滚动
            var prefab = PlayerOptionsMenuPrefab;
            if (prefab != null)
            {
                var barBack = Object.Instantiate(prefab.transform.GetChild(4).FindChild("UI_ScrollbarTrack").gameObject, parent);
                var bar = Object.Instantiate(prefab.transform.GetChild(4).FindChild("UI_Scrollbar").gameObject, parent);
                barBack.transform.localPosition = scrollBarLocalPos + new Vector3(0.12f, 0f, 0f);
                bar.transform.localPosition = scrollBarLocalPos;

                var scrollBar = bar.GetComponent<Scrollbar>();

                scrollBar.parent = scroller;
                scrollBar.graphic = bar.GetComponent<SpriteRenderer>();
                scrollBar.trackGraphic = barBack.GetComponent<SpriteRenderer>();
                scrollBar.trackGraphic.size = new Vector2(scrollBar.trackGraphic.size.x, scrollerHeight);

                var ratio = scrollerHeight / 3.88f;
                scroller.ScrollbarYBounds = new FloatRange(-1.8f * ratio + scrollBarLocalPos.y + 0.4f, 1.8f * ratio + scrollBarLocalPos.y - 0.4f);
                scroller.ScrollbarY = scrollBar;
            }

            scroller.Inner = target;
            scroller.SetBounds(bounds, null);
            scroller.allowY = true;
            scroller.allowX = false;
            scroller.active = true;
            scroller.name = "Scroller";
            scroller.ScrollToTop();

            return scroller;
        }
        catch (Exception ex)
        {
            LightLogger.LogError("[VanillaAsset.GenerateScroller]", ex); return default;
        }
    }
}

/// <summary>
/// PassiveButton 设置扩展方法
/// </summary>
public static class ButtonExtensions
{
    /// <summary>
    /// 在 GameObject 上设置 PassiveButton
    /// </summary>
    public static PassiveButton SetUpButton(this GameObject obj, bool hasOnClickHandler,
        SpriteRenderer? renderer = null, Color? color = null, Color? selectedColor = null,
        bool playSound = false)
    {
        try
        {
            var normalColor = color?.ToUnityColor() ?? UnityEngine.Color.white;
            // 悬浮亮起：比正常色更亮
            var hoverColor = selectedColor?.ToUnityColor()
                ?? UnityEngine.Color.Lerp(normalColor, UnityEngine.Color.white, 0.5f);

            var button = obj.GetComponent<PassiveButton>();
            if (button == null)
                button = obj.AddComponent<PassiveButton>();

            button.OnMouseOver = new UnityEvent();
            button.OnMouseOut = new UnityEvent();
            button.OnClick = new Button.ButtonClickedEvent();

            if (renderer != null)
            {
                // 初始颜色即正常色，悬浮时切换为亮色
                renderer.color = normalColor;
            }

            if (renderer != null)
            {
                button.OnMouseOver.AddListener((UnityAction)(() => { renderer.color = hoverColor; }));
                button.OnMouseOut.AddListener((UnityAction)(() => { renderer.color = normalColor; }));
            }

            if (playSound)
            {
                button.OnClick.AddListener((UnityAction)(() =>
                {
                    try { SoundManager.Instance.PlaySound(VanillaAsset.FindSoundClip("UI_Select"), false, 0.8f); } catch { }
                }));

                if (renderer != null)
                {
                    button.OnMouseOver.AddListener((UnityAction)(() =>
                    {
                        try { SoundManager.Instance.PlaySound(VanillaAsset.FindSoundClip("UI_Hover"), false, 0.8f); } catch { }
                    }));
                }
            }

            return button;
        }
        catch (Exception ex)
        {
            LightLogger.LogError("[VanillaAsset.SetUpButton]", ex); return default;
        }
    }
}
