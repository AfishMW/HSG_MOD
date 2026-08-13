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
        public string DisplayName = "";
        public string ResourceName = "";
        public string Author = "";
        public string Description = "";
        public string Hash = "";
    }

    private const string ResourcePrefix = "Light.Resources.MainMenuBackground.BG_";
    private const string DefaultName = "Default";

    private static readonly Dictionary<string, Sprite> _spriteCache = new();

    private readonly List<ImageEntry> _entries = new();
    private readonly string _defaultHash;
    private readonly Vector2 _windowSize = new(7f, 4.5f);

    private MetaScreen? _screen;
    private GameObject? _windowObj;
    internal GameObject? _bgObj;
    private SpriteRenderer _bgSr = null!;
    private SpriteRenderer _previewImage = null!;
    private TextMeshPro _nameText = null!;
    private TextMeshPro _descText = null!;
    private TextMeshPro _authorText = null!;
    private TextMeshPro _applyText = null!;
    private HudUIButton _applyButton = null!;
    private readonly List<GameObject> _checkboxes = new();
    private readonly List<bool> _checkboxStates = new();

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

    public void AddImage(string displayName, string resourceName, string author,
        string description, string hash)
    {
        _entries.Add(new ImageEntry
        {
            DisplayName = displayName,
            ResourceName = resourceName,
            Author = author,
            Description = description,
            Hash = hash
        });
    }

    public void SetApplyCallback(Action<int> onApply) => _onApply = onApply;
    public void SetHashFailedFallback(Action fallback) => _onHashFailedFallback = fallback;

    private void Build()
    {
        try
        {
            if (_built)
            {
                bool alive = true;
                if (_windowObj == null) alive = false;
                else try { var _ = _windowObj.activeSelf; } catch { alive = false; }
                if (!alive)
                {
                    _built = false;
                    _windowObj = null;
                    _screen = null;
                    _previewImage = null!;
                    _nameText = null!;
                    _descText = null!;
                    _authorText = null!;
                    _applyText = null!;
                    _applyButton = null!;
                    _checkboxes.Clear();
                }
                else return;
            }
            _built = true;

            var parent = DestroyableSingleton<MainMenuManager>.Instance.transform;
            _screen = MetaScreen.GenerateWindow(_windowSize, parent, new Vector3(0, 0, -20f),
                withBlackScreen: true, closeOnClickOutside: false,
                background: BackgroundSetting.Modern, withCloseButton: true);
            _windowObj = _screen.transform.parent.gameObject;

            var font = GetFont();

            // 预览图
            var imgObj = new GameObject("GalleryPreview");
            imgObj.layer = LayerExpansion.GetUILayer();
            imgObj.transform.SetParent(_screen.transform, false);
            imgObj.transform.localPosition = new Vector3(0f, 0.5f, -0.1f);
            _previewImage = imgObj.AddComponent<SpriteRenderer>();
            _previewImage.color = Color.white;

            _nameText = CreateTmp("GalleryName", new Vector3(0f, 0f, -0.1f),
                new Vector3(1.0f, 1.0f, 1f), 2.5f, FontStyles.Bold, font,
                TextAlignmentOptions.Center, new Color(1f, 1f, 1f, 0.9f));

            _descText = CreateTmp("GalleryDesc", new Vector3(1.5f, -0.4f, -0.1f),
                new Vector3(0.45f, 0.45f, 1f), 0.9f, FontStyles.Italic, font,
                TextAlignmentOptions.TopLeft, new Color(0.6f, 0.6f, 0.6f, 0.7f));

            _authorText = CreateTmp("GalleryAuthor", new Vector3(0f, -0.8f, -0.1f),
                new Vector3(0.65f, 0.65f, 1f), 1.5f, FontStyles.Normal, font,
                TextAlignmentOptions.Center, new Color(0.8f, 0.8f, 0.8f, 0.8f));

            CreateNavArrow(isLeft: true, new Vector3(-_windowSize.x / 2f + 0.3f, 0.5f, -0.1f),
                () => SwitchPage(-1));
            CreateNavArrow(isLeft: false, new Vector3(_windowSize.x / 2f - 0.3f, 0.5f, -0.1f),
                () => SwitchPage(1));

            for (int i = 0; i < 3; i++)
            {
                var cb = CreateCheckbox(i, new Vector3(-2.5f, 0.5f - i * 0.7f, -0.1f));
                _checkboxes.Add(cb);
                _checkboxStates.Add(false);
            }

            _applyButton = HudUIButton.Create(_screen.transform, "应用", new Vector2(1.0f, 0.35f), () =>
            {
                if (_currentIndex == _appliedIndex) return;
                _appliedIndex = _currentIndex;
                UpdateApplyText();
                ApplyBackground();
                _onApply?.Invoke(_currentIndex);
            });
            _applyButton.SetPosition(new Vector3(0f, -1.8f, -0.1f));
            _applyText = _applyButton.Text;

            _windowObj.SetActive(false);
        }
        catch (Exception ex)
        {
            LightLogger.LogError("[ImageGalleryPanel.Build]", ex);
        }
    }

    public Action? OnBack { get; set; }

    private static TMP_FontAsset? GetFont()
    {
        try
        {
            HudUIFont.EnsureLoaded();
            return HudUIFont.FontAsset;
        }
        catch { return null; }
    }

    private TextMeshPro CreateTmp(string name, Vector3 pos, Vector3 scale,
        float fontSize, FontStyles style, TMP_FontAsset? font,
        TextAlignmentOptions alignment, Color color)
    {
        var obj = new GameObject(name);
        obj.layer = LayerExpansion.GetUILayer();
        obj.transform.SetParent(_screen!.transform, false);
        obj.transform.localPosition = pos;
        obj.transform.localScale = scale;
        var tmp = obj.AddComponent<TextMeshPro>();
        if (font != null)
        {
            tmp.font = font;
            tmp.fontSharedMaterial = HudUIFont.FontMaterial;
        }
        tmp.text = "";
        tmp.alignment = alignment;
        tmp.fontSize = fontSize;
        tmp.fontStyle = style;
        tmp.color = color;
        tmp.raycastTarget = false;
        return tmp;
    }

    private GameObject CreateCheckbox(int index, Vector3 pos)
    {
        var labels = new[] { "选项A", "选项B", "选项C" };
        var btn = HudUIButton.Create(_screen!.transform, labels[index], new Vector2(0.8f, 0.3f), () =>
        {
            _checkboxStates[index] = !_checkboxStates[index];
            var sr = _checkboxes.Count > index ? _checkboxes[index]?.GetComponentInChildren<SpriteRenderer>() : null;
            if (sr != null)
                sr.color = _checkboxStates[index] ? new Color(0.3f, 1f, 0.3f, 1f) : Color.white;
        });
        btn?.SetPosition(pos);
        return btn?.GameObject ?? new GameObject($"Checkbox{index}");
    }

    private void CreateNavArrow(bool isLeft, Vector3 pos, Action onClick)
    {
        try
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
        catch (Exception ex)
        {
            LightLogger.LogError("[ImageGalleryPanel.CreateNavArrow]", ex);
        }
    }

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

    private static string PreviewPathFor(string name) =>
        ResourcePrefix + name + "_preview.png";

    private Sprite? LoadVerifiedSprite(string name, string expectedHash, float pixelsPerUnit = 100f)
    {
        try
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

            if (string.IsNullOrEmpty(expectedHash) || ComputeHash(bytes) == expectedHash.ToLowerInvariant())
            {
                var spr = BytesToSprite(bytes, pixelsPerUnit);
                if (spr != null) _spriteCache[cacheKey] = spr;
                return spr;
            }

            LightLogger.LogWarning($"[Gallery] 哈希不匹配: {name}");
            return TryLoadDefault(pixelsPerUnit);
        }
        catch (Exception ex)
        {
            LightLogger.LogError("[ImageGalleryPanel.LoadVerifiedSprite]", ex); return default;
        }
    }

    private Sprite? TryLoadDefault(float pixelsPerUnit = 100f)
    {
        try
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
        catch (Exception ex)
        {
            LightLogger.LogError("[ImageGalleryPanel.TryLoadDefault]", ex); return default;
        }
    }

    private Sprite? LoadPreviewSprite(string name)
    {
        try
        {
            var previewPath = PreviewPathFor(name);
            var bytes = ReadResourceBytes(previewPath);
            if (bytes != null)
                return BytesToSprite(bytes, 100f);
            // fallback: 用原图
            return LoadVerifiedSprite(name, "", 100f);
        }
        catch { return null; }
    }

    private void EnsureBgObject()
    {
        try
        {
            if (_bgObj != null)
            {
                try { _bgObj.SetActive(true); return; }
                catch { _bgObj = null; }
            }
            var existing = GameObject.Find("LightBackground");
            if (existing != null)
                UnityEngine.Object.Destroy(existing);
            _bgObj = new GameObject("LightBackground");
            _bgObj.transform.position = new Vector3(0f, 0f, 520f);
            _bgObj.transform.localPosition = new Vector3(0f, 0f, 520f);
            var scale = Mathf.Max((float)Screen.width / Screen.height / (16f / 9f), 1f);
            _bgObj.transform.localScale = new Vector3(scale, scale, 1f);
            _bgSr = _bgObj.AddComponent<SpriteRenderer>();
            UnityEngine.Object.DontDestroyOnLoad(_bgObj);
        }
        catch (Exception ex)
        {
            LightLogger.LogError("[ImageGalleryPanel.EnsureBgObject]", ex);
        }
    }

    public void RestoreBackground()
    {
        try
        {
            if (_appliedIndex >= 0 && _appliedIndex < _entries.Count)
            {
                _currentIndex = _appliedIndex;
                ApplyBackground();
                if (_bgObj != null)
                {
                    try { _bgObj.transform.localPosition = new Vector3(0f, 0f, 520f); }
                    catch { _bgObj = null; }
                }
            }
        }
        catch (Exception ex)
        {
            LightLogger.LogError("[ImageGalleryPanel.RestoreBackground]", ex);
        }
    }

    public void Reload()
    {
        _bgObj = null;
        _bgSr = null!;
        RestoreBackground();
    }

    private void ApplyBackground()
    {
        try
        {
            if (_currentIndex < 0 || _currentIndex >= _entries.Count) return;
            var entry = _entries[_currentIndex];
            var sprite = LoadVerifiedSprite(entry.ResourceName, entry.Hash, 350f);
            if (sprite == null) return;

            EnsureBgObject();
            try { _bgSr.sprite = sprite; }
            catch { _bgSr = null!; _bgObj = null; }
        }
        catch (Exception ex)
        {
            LightLogger.LogError("[ImageGalleryPanel.ApplyBackground]", ex);
        }
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
        try
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
        catch (Exception ex)
        {
            LightLogger.LogError("[ImageGalleryPanel.UpdateParallax]", ex);
        }
    }

    private void SwitchPage(int delta)
    {
        if (_entries.Count == 0) return;
        _currentIndex = (_currentIndex + delta + _entries.Count) % _entries.Count;
        Refresh();
    }

    private void Refresh()
    {
        try
        {
            if (_currentIndex < 0 || _currentIndex >= _entries.Count) _currentIndex = 0;
            var entry = _entries[_currentIndex];

            // 预览图：优先用 _preview.png
            var previewSpr = LoadPreviewSprite(entry.ResourceName);
            if (previewSpr == null)
                previewSpr = LoadVerifiedSprite(entry.ResourceName, entry.Hash);

            try
            {
                if (previewSpr != null && _previewImage != null)
                {
                    _previewImage.sprite = previewSpr;
                    float maxW = 4f, maxH = 2f;
                    float scaleX = maxW / (previewSpr.texture.width / 100f);
                    float scaleY = maxH / (previewSpr.texture.height / 100f);
                    float scale = Mathf.Min(scaleX, scaleY);
                    _previewImage.transform.localScale = new Vector3(scale, scale, 1f);
                }
            }
            catch { _previewImage = null!; }

            try { _nameText.text = entry.DisplayName; } catch { _nameText = null!; }
            try { _descText.text = entry.Description; } catch { _descText = null!; }
            try { _authorText.text = "by " + entry.Author; } catch { _authorText = null!; }
            UpdateApplyText();
        }
        catch (Exception ex)
        {
            LightLogger.LogError("[ImageGalleryPanel.Refresh]", ex);
        }
    }

    private void UpdateApplyText()
    {
        try
        {
            if (_applyText != null)
                _applyText.text = _currentIndex == _appliedIndex ? "已应用" : "应用";
        }
        catch (Exception ex)
        {
            LightLogger.LogError("[ImageGalleryPanel.UpdateApplyText]", ex);
        }
    }

    public void Show()
    {
        try
        {
            Build();
            if (_currentIndex >= _entries.Count) _currentIndex = 0;
            Refresh();
            RestoreBackground();
            if (_windowObj != null)
            {
                try { _windowObj.SetActive(true); }
                catch { _windowObj = null; _built = false; }
            }
        }
        catch (Exception ex)
        {
            LightLogger.LogError("[ImageGalleryPanel.Show]", ex);
        }
    }

    public void Hide()
    {
        try
        {
            if (_windowObj != null)
            {
                try { _windowObj.SetActive(false); }
                catch { _windowObj = null; _built = false; }
            }
        }
        catch (Exception ex)
        {
            LightLogger.LogError("[ImageGalleryPanel.Hide]", ex);
        }
    }
}
