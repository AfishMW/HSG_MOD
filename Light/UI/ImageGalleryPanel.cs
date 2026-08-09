using System;
using System.Collections.Generic;
using System.Reflection;
using System.Security.Cryptography;
using LightInDark.Core;
using LightInDark.UI.Window;
using Light.UI.HudUI;
using Light.UI.Window;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Rendering;
using UnityEngine.UI;
using MetaScreen = Light.UI.HudUI.MetaScreen;

namespace Light.UI;

public class ImageGalleryPanel
{
    public class ImageEntry
    {
        public string Name = "";
        public string Author = "";
        public string Hash = "";
        public Action<int>? OnDownload;
    }

    private const string ResourcePrefix = "Light.Resources.MainMenuBackground.BG_";
    private const string DefaultName = "Default";

    private static readonly Dictionary<string, Sprite> _spriteCache = new();

    private readonly List<ImageEntry> _entries = new();
    private readonly string _defaultHash;
    private readonly Vector2 _windowSize = new(7f, 4.5f);

    private MetaScreen? _screen;
    private GameObject? _windowObj;
    private GameObject? _bgObj;
    private SpriteRenderer _bgSr = null!;
    private SpriteRenderer _previewImage = null!;
    private TextMeshPro _nameText = null!;
    private TextMeshPro _authorText = null!;
    private TextMeshPro _applyText = null!;
    private HudUIButton _applyButton = null!;

    private int _currentIndex;
    private int _appliedIndex = -1;
    private Action<int>? _onApply;
    private Action? _onHashFailedFallback;
    private bool _built;

    public int CurrentIndex => _currentIndex;
    public int AppliedIndex => _appliedIndex;

    public ImageGalleryPanel(string defaultHash = "")
    {
        _defaultHash = defaultHash;
    }

    // ════════════════════════════════════════════
    //  Public API
    // ════════════════════════════════════════════

    public void AddImage(string name, string author, string hash,
        Action<int>? onDownload = null)
    {
        _entries.Add(new ImageEntry
        {
            Name = name,
            Author = author,
            Hash = hash,
            OnDownload = onDownload
        });
    }

    public void SetApplyCallback(Action<int> onApply) => _onApply = onApply;
    public void SetHashFailedFallback(Action fallback) => _onHashFailedFallback = fallback;

    // ════════════════════════════════════════════
    //  Build / Show / Hide
    // ════════════════════════════════════════════

    private void Build()
    {
        if (_built) return;
        _built = true;

        var parent = DestroyableSingleton<MainMenuManager>.Instance.transform;
        _screen = MetaScreen.GenerateWindow(_windowSize, parent, new Vector3(0, 0, -20f),
            withBlackScreen: true, closeOnClickOutside: false,
            background: BackgroundSetting.Modern, withCloseButton: true);
        _windowObj = _screen.transform.parent.gameObject;

        // 预览图
        var imgObj = new GameObject("GalleryPreview");
        imgObj.layer = LayerExpansion.GetUILayer();
        imgObj.transform.SetParent(_screen.transform, false);
        imgObj.transform.localPosition = new Vector3(0f, 0.5f, -0.1f);
        _previewImage = imgObj.AddComponent<SpriteRenderer>();
        _previewImage.color = Color.white;

        // 大字名称
        _nameText = CreateTmp("GalleryName", new Vector3(0f, -0.7f, -0.1f),
            new Vector3(1.2f, 1.2f, 1f), 3f, FontStyles.Bold);

        // 作者
        _authorText = CreateTmp("GalleryAuthor", new Vector3(0f, -1.1f, -0.1f),
            new Vector3(0.7f, 0.7f, 1f), 1.8f, FontStyles.Normal);

        // 左右导航（HudUI NavButton 精灵图）
        CreateNavArrow(isLeft: true, new Vector3(-_windowSize.x / 2f + 0.3f, 0.5f, -0.1f),
            () => SwitchPage(-1));
        CreateNavArrow(isLeft: false, new Vector3(_windowSize.x / 2f - 0.3f, 0.5f, -0.1f),
            () => SwitchPage(1));

        // 下载按钮
        HudUIButton.Create(_screen.transform, "下载", new Vector2(1.0f, 0.35f), () =>
        {
            if (_currentIndex >= 0 && _currentIndex < _entries.Count)
                _entries[_currentIndex].OnDownload?.Invoke(_currentIndex);
        })?.SetPosition(new Vector3(-1.0f, -1.6f, -0.1f));

        // 应用按钮
        _applyButton = HudUIButton.Create(_screen.transform, "应用", new Vector2(1.0f, 0.35f), () =>
        {
            _appliedIndex = _currentIndex;
            UpdateApplyText();
            ApplyBackground();
            _onApply?.Invoke(_currentIndex);
        });
        _applyButton.SetPosition(new Vector3(1.0f, -1.6f, -0.1f));
        _applyText = _applyButton.Text;

        // 返回按钮
        HudUIButton.Create(_screen.transform, "返回", new Vector2(1.0f, 0.35f), () =>
        {
            Hide();
            OnBack?.Invoke();
        })?.SetPosition(new Vector3(0f, -2.1f, -0.1f));

        _windowObj.SetActive(false);
    }

    public Action? OnBack { get; set; }

    private TextMeshPro CreateTmp(string name, Vector3 pos, Vector3 scale,
        float fontSize, FontStyles style)
    {
        var obj = new GameObject(name);
        obj.layer = LayerExpansion.GetUILayer();
        obj.transform.SetParent(_screen!.transform, false);
        obj.transform.localPosition = pos;
        obj.transform.localScale = scale;
        var tmp = obj.AddComponent<TextMeshPro>();
        HudUIFont.EnsureLoaded();
        if (HudUIFont.FontAsset != null)
        {
            tmp.font = HudUIFont.FontAsset;
            tmp.fontSharedMaterial = HudUIFont.FontMaterial;
        }
        tmp.text = "";
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.fontSize = fontSize;
        tmp.fontStyle = style;
        tmp.raycastTarget = false;
        return tmp;
    }

    private void CreateNavArrow(bool isLeft, Vector3 pos, Action onClick)
    {
        var obj = new GameObject(isLeft ? "NavLeft" : "NavRight");
        obj.layer = LayerExpansion.GetUILayer();
        obj.transform.SetParent(_screen!.transform, false);
        obj.transform.localPosition = pos;
        obj.transform.localScale = new Vector3(0.57f, 0.57f, 1f);

        var collider = obj.AddComponent<BoxCollider2D>();
        collider.isTrigger = true;
        collider.size = new Vector2(0.65f, 0.65f);

        var renderer = obj.AddComponent<SpriteRenderer>();
        var normal = isLeft ? HudUIAssets.NavLeftNormal : HudUIAssets.NavRightNormal;
        var hover = isLeft ? HudUIAssets.NavLeftHover : HudUIAssets.NavRightHover;
        renderer.sprite = normal;

        obj.AddComponent<SortingGroup>();

        var button = obj.SetUpButton(true, renderer, playSound: true);
        button.OnMouseOver.AddListener((UnityAction)(() => renderer.sprite = hover));
        button.OnMouseOut.AddListener((UnityAction)(() => renderer.sprite = normal));
        button.OnClick.AddListener((UnityAction)(() => onClick()));
    }

    // ════════════════════════════════════════════
    //  Image loading + hash verification
    // ════════════════════════════════════════════

    private static byte[] ReadResourceBytes(string resourcePath)
    {
        var assembly = Assembly.GetExecutingAssembly();
        using var stream = assembly.GetManifestResourceStream(resourcePath);
        if (stream == null) return null!;
        var bytes = new byte[stream.Length];
        stream.Read(bytes, 0, (int)stream.Length);
        return bytes;
    }

    private static string ComputeHash(byte[] data)
    {
        using var sha = SHA256.Create();
        return BitConverter.ToString(sha.ComputeHash(data))
            .Replace("-", "").ToLowerInvariant();
    }

    private static Sprite BytesToSprite(byte[] bytes, float pixelsPerUnit = 100f)
    {
        var tex = new Texture2D(2, 2, TextureFormat.ARGB32, false);
        if (!ImageConversion.LoadImage(tex, bytes, false)) return null!;
        tex.wrapMode = TextureWrapMode.Clamp;
        return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height),
            new Vector2(0.5f, 0.5f), pixelsPerUnit);
    }

    private static string ResourcePathFor(string name) =>
        ResourcePrefix + name + ".png";

    private Sprite? LoadVerifiedSprite(string name, string expectedHash, float pixelsPerUnit = 100f)
    {
        string cacheKey = name + "_" + expectedHash + "_" + pixelsPerUnit;
        if (_spriteCache.TryGetValue(cacheKey, out var cached))
            return cached;

        var path = ResourcePathFor(name);
        var bytes = ReadResourceBytes(path);
        if (bytes == null)
        {
            LightLogger.LogWarning($"[Gallery] 资源未找到: {path}");
            return TryLoadDefault(pixelsPerUnit);
        }

        // 空哈希表示跳过校验（如 "Default" 兜底条目）
        if (string.IsNullOrEmpty(expectedHash) || ComputeHash(bytes) == expectedHash.ToLowerInvariant())
        {
            var spr = BytesToSprite(bytes, pixelsPerUnit);
            if (spr != null) _spriteCache[cacheKey] = spr;
            return spr;
        }

        LightLogger.LogWarning($"[Gallery] 哈希不匹配: {name}");
        return TryLoadDefault(pixelsPerUnit);
    }

    private Sprite? TryLoadDefault(float pixelsPerUnit = 100f)
    {
        string cacheKey = DefaultName + "_" + _defaultHash + "_" + pixelsPerUnit;
        if (_spriteCache.TryGetValue(cacheKey, out var cached))
            return cached;

        var bytes = ReadResourceBytes(ResourcePathFor(DefaultName));
        if (bytes == null)
        {
            LightLogger.LogWarning("[Gallery] BG_Default.png 未找到");
            _onHashFailedFallback?.Invoke();
            return null;
        }

        if (!string.IsNullOrEmpty(_defaultHash) &&
            ComputeHash(bytes) != _defaultHash.ToLowerInvariant())
        {
            LightLogger.LogWarning("[Gallery] BG_Default 哈希不匹配");
            _onHashFailedFallback?.Invoke();
            return null;
        }

        var spr = BytesToSprite(bytes, pixelsPerUnit);
        if (spr != null) _spriteCache[cacheKey] = spr;
        return spr;
    }

    // ════════════════════════════════════════════
    //  Background replacement
    // ════════════════════════════════════════════

    private void EnsureBgObject()
    {
        if (_bgObj != null)
        {
            try { _bgObj.SetActive(true); return; }
            catch { _bgObj = null; }
        }
        // 清理可能残留的旧 LightBackground（场景切换后 C# 引用丢失但 GameObject 仍在）
        var existing = GameObject.Find("LightBackground");
        if (existing != null)
            UnityEngine.Object.Destroy(existing);
        _bgObj = new GameObject("LightBackground");
        _bgObj.transform.position = new Vector3(0f, 0f, 520f);
        _bgObj.transform.localPosition = new Vector3(0f, 0f, 520f);
        var scale = Mathf.Max((float)Screen.width / Screen.height / (16f / 9f), 1f);
        _bgObj.transform.localScale = new Vector3(scale, scale, 1f);
        _bgSr = _bgObj.AddComponent<SpriteRenderer>();
    }

    /// <summary>
    /// 重新确保背景可见（场景切换后 _bgObj 可能被销毁）。
    /// 如果之前应用过背景，重新创建并应用。
    /// </summary>
    public void RestoreBackground()
    {
        if (_appliedIndex >= 0 && _appliedIndex < _entries.Count)
        {
            _currentIndex = _appliedIndex;
            ApplyBackground();
            if (_bgObj != null)
                _bgObj.transform.localPosition = new Vector3(0f, 0f, 520f);
        }
    }

    /// <summary>
    /// 重建背景对象并重新应用当前选中的背景。
    /// 场景切换后 _bgObj 被销毁，此方法重新创建。
    /// </summary>
    public void Reload()
    {
        _bgObj = null;
        _bgSr = null!;
        RestoreBackground();
    }

    private void ApplyBackground()
    {
        if (_currentIndex < 0 || _currentIndex >= _entries.Count) return;
        var entry = _entries[_currentIndex];
        var sprite = LoadVerifiedSprite(entry.Name, entry.Hash, 350f);
        if (sprite == null) return;

        EnsureBgObject();
        _bgSr.sprite = sprite;
    }

    public void ApplyDefaultBackground()
    {
        if (_entries.Count == 0) return;
        _currentIndex = 0;
        _appliedIndex = 0;
        ApplyBackground();
    }

    public void UpdateParallax(Vector2 mouseOffset)
    {
        if (_bgObj == null) return;
        if (_windowObj != null && !_windowObj.activeSelf) return;
        try
        {
            var target = new Vector3(
                mouseOffset.x * 0.3f,
                mouseOffset.y * 0.2f,
                520f);
            _bgObj.transform.localPosition = Vector3.Lerp(
                _bgObj.transform.localPosition, target, Time.deltaTime * 3f);
        }
        catch { _bgObj = null; }
    }

    // ════════════════════════════════════════════
    //  Internal
    // ════════════════════════════════════════════

    private void SwitchPage(int delta)
    {
        if (_entries.Count == 0) return;
        _currentIndex = (_currentIndex + delta + _entries.Count) % _entries.Count;
        Refresh();
    }

    private void Refresh()
    {
        if (_currentIndex < 0 || _currentIndex >= _entries.Count) _currentIndex = 0;
        var entry = _entries[_currentIndex];

        var spr = LoadVerifiedSprite(entry.Name, entry.Hash);
        if (spr != null)
        {
            _previewImage.sprite = spr;
            float maxW = 4f, maxH = 2f;
            float scaleX = maxW / (spr.texture.width / 100f);
            float scaleY = maxH / (spr.texture.height / 100f);
            float scale = Mathf.Min(scaleX, scaleY);
            _previewImage.transform.localScale = new Vector3(scale, scale, 1f);
        }

        _nameText.text = entry.Name;
        _authorText.text = "by " + entry.Author;
        UpdateApplyText();
    }

    private void UpdateApplyText()
    {
        if (_applyText != null)
            _applyText.text = _currentIndex == _appliedIndex ? "已应用" : "应用";
    }

    public void Show()
    {
        Build();
        if (_currentIndex >= _entries.Count) _currentIndex = 0;
        Refresh();
        RestoreBackground();
        _windowObj!.SetActive(true);
    }

    public void Hide()
    {
        if (_windowObj != null)
        {
            try { _windowObj.SetActive(false); }
            catch { _windowObj = null; }
        }
    }
}
