using Il2CppInterop.Runtime;
using TMPro;
using Twitch;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using Object = UnityEngine.Object;
using Button = UnityEngine.UI.Button;

namespace LightInDark.UI.Window;

/// <summary>
/// Among Us 原生资源缓存，对应 Nebula 的 VanillaAsset。
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
            EnsureLoaded();
            return _standardTextPrefab!;
        }
    }

    /// <summary>遮罩字体材质</summary>
    public static Material StandardMaskedFontMaterial
    {
        get
        {
            EnsureLoaded();
            return _standardMaskedFontMaterial!;
        }
    }

    /// <summary>Barlow 字体</summary>
    public static TMP_FontAsset VersionFont
    {
        get
        {
            EnsureLoaded();
            return _versionFont!;
        }
    }

    /// <summary>DIN_Pro 字体</summary>
    public static TMP_FontAsset PreSpawnFont
    {
        get
        {
            EnsureLoaded();
            return _preSpawnFont!;
        }
    }

    /// <summary>Brook 字体</summary>
    public static TMP_FontAsset BrookFont
    {
        get
        {
            EnsureLoaded();
            return _brookFont!;
        }
    }

    private static Sprite? _whiteSprite;
    /// <summary>白色降级 Sprite</summary>
    public static Sprite WhiteSprite
    {
        get
        {
            if (_whiteSprite != null) return _whiteSprite;
            var tex = Texture2D.whiteTexture;
            _whiteSprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f);
            return _whiteSprite;
        }
    }

    private static Sprite TryGetSprite(ref Sprite? field)
    {
        EnsureLoaded();
        return field ?? WhiteSprite;
    }

    private static void EnsureLoaded()
    {
        if (_triedInit) return;
        _triedInit = true;

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
        }
        catch
        {
            // TwitchManager 未就绪
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

    private static T? FindAsset<T>(string name) where T : Object
    {
        var type = Il2CppType.Of<T>();
        foreach (var obj in Resources.FindObjectsOfTypeAll(type))
        {
            if (obj.name == name)
                return obj.TryCast<T>();
        }
        return null;
    }

    public static AudioClip? FindSoundClip(string name)
    {
        var type = Il2CppType.Of<AudioClip>();
        foreach (var obj in Resources.FindObjectsOfTypeAll(type))
        {
            if (obj.name == name)
                return obj.TryCast<AudioClip>();
        }
        return null;
    }

    /// <summary>获取可用按钮背景</summary>
    public static Sprite GetButtonSprite() => TextButtonSprite;
    /// <summary>获取可用窗口背景</summary>
    public static Sprite GetWindowSprite() => PopUpBackSprite;
    /// <summary>获取可用全屏遮罩</summary>
    public static Sprite GetFullScreenSprite() => FullScreenSprite;
}

/// <summary>
/// PassiveButton 设置扩展方法，对应 Nebula 的 SetUpButton
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
        var normalColor = color?.ToUnityColor() ?? UColor.white;
        var hoverColor = selectedColor?.ToUnityColor() ?? new UColor(0f, 1f, 42f / 255f, 1f);

        var button = obj.GetComponent<PassiveButton>();
        if (button == null)
            button = obj.AddComponent<PassiveButton>();

        button.OnMouseOver = new UnityEvent();
        button.OnMouseOut = new UnityEvent();
        button.OnClick = new Button.ButtonClickedEvent();

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
}
