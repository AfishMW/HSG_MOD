using LightInDark.Core;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using Button = UnityEngine.UI.Button;
using Object = UnityEngine.Object;

namespace LightInDark.UI.Window;

/// <summary>
/// GUI 屏幕，基于 SpriteRenderer 体系。
/// 已通过 ClassInjector 注册为 Il2Cpp 类型。
/// </summary>
public class MetaScreen : MonoBehaviour, IGUIScreen
{
    private GameObject? _root;
    private GameObject? _screen;
    private GameObject? _background;
    private GameObject? _closeButton;
    private GameObject? _blackScreen;
    private Size _screenSize;

    /// <summary>
    /// 生成一个完整的窗口
    /// </summary>
    public static MetaScreen GenerateWindow(
        Vector2 size,
        Transform parent,
        Vector3 localPosition,
        bool withCloseButton = true,
        bool withBlackScreen = true)
    {
        var root = new GameObject("MetaWindow");
        root.layer = LayerExpansion.GetUILayer();
        root.transform.SetParent(parent, false);
        root.transform.localPosition = localPosition;
        root.transform.localScale = Vector3.one;

        // 黑色遮罩
        GameObject? blackScreen = null;
        if (withBlackScreen)
        {
            blackScreen = new GameObject("BlackScreen");
            blackScreen.layer = LayerExpansion.GetUILayer();
            blackScreen.transform.SetParent(root.transform, false);
            blackScreen.transform.localPosition = new Vector3(0f, 0f, 1f);
            var blackRenderer = blackScreen.AddComponent<SpriteRenderer>();
            blackRenderer.sprite = VanillaAsset.FullScreenSprite;
            blackRenderer.drawMode = SpriteDrawMode.Sliced;
            blackRenderer.size = new Vector2(30f, 30f);
            blackRenderer.color = new UColor(0f, 0f, 0f, 0.35f);
            blackRenderer.sortingOrder = 50;
        }

        // 窗口背景
        var bgObj = new GameObject("WindowBackground");
        bgObj.layer = LayerExpansion.GetUILayer();
        bgObj.transform.SetParent(root.transform, false);
        bgObj.transform.localPosition = Vector3.zero;
        var bgRenderer = bgObj.AddComponent<SpriteRenderer>();
        bgRenderer.sprite = VanillaAsset.PopUpBackSprite;
        bgRenderer.drawMode = SpriteDrawMode.Sliced;
        bgRenderer.size = size;
        bgRenderer.color = new UColor(1f, 1f, 1f, 0.8f);
        bgRenderer.sortingOrder = 51;

        // 内容屏幕
        var screenObj = new GameObject("Screen");
        screenObj.layer = LayerExpansion.GetUILayer();
        screenObj.transform.SetParent(bgObj.transform, false);
        screenObj.transform.localPosition = new Vector3(0f, 0f, -1f);
        var screen = screenObj.AddComponent<MetaScreen>();
        screen._root = root;
        screen._screen = screenObj;
        screen._background = bgObj;
        screen._blackScreen = blackScreen;
        screen._screenSize = new Size(size);

        // 关闭按钮
        if (withCloseButton)
        {
            screen._closeButton = CreateCloseButton(root.transform, new Vector3(size.x * 0.5f - 0.3f, size.y * 0.5f - 0.3f, -10f), screen);
        }

        return screen;
    }

    /// <summary>
    /// 生成透明窗口
    /// </summary>
    public static MetaScreen GenerateBlankWindow(Vector2 size, Transform parent, Vector3 localPosition)
    {
        var root = new GameObject("MetaWindow");
        root.layer = LayerExpansion.GetUILayer();
        root.transform.SetParent(parent, false);
        root.transform.localPosition = localPosition;
        root.transform.localScale = Vector3.one;

        var screenObj = new GameObject("Screen");
        screenObj.layer = LayerExpansion.GetUILayer();
        screenObj.transform.SetParent(root.transform, false);
        screenObj.transform.localPosition = Vector3.zero;
        var screen = screenObj.AddComponent<MetaScreen>();
        screen._root = root;
        screen._screen = screenObj;
        screen._screenSize = new Size(size);

        return screen;
    }

    /// <summary>
    /// 设置屏幕上显示的 Widget
    /// </summary>
    public void SetWidget(GUIWidget? widget, out Size actualSize)
    {
        // 清除已有内容
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            Object.Destroy(transform.GetChild(i).gameObject);
        }

        if (widget == null)
        {
            actualSize = Size.Zero;
            return;
        }

        var obj = widget.Instantiate(new Size(100f, 100f), out actualSize);
        if (obj != null)
        {
            obj.transform.SetParent(transform, false);
            obj.transform.localPosition = Vector3.zero;
        }
    }

    /// <summary>
    /// 关闭并销毁窗口
    /// </summary>
    public void Close()
    {
        if (_root != null)
        {
            Object.Destroy(_root);
            _root = null;
        }
    }

    void OnDestroy()
    {
        if (_root != null)
        {
            Object.Destroy(_root);
            _root = null;
        }
    }

    /// <summary>
    /// 创建关闭按钮
    /// </summary>
    private static GameObject CreateCloseButton(Transform parent, Vector3 localPos, MetaScreen screen)
    {
        var obj = new GameObject("CloseButton");
        obj.layer = LayerExpansion.GetUILayer();
        obj.transform.SetParent(parent, false);
        obj.transform.localPosition = localPos;
        obj.transform.localScale = Vector3.one;

        var btnSize = 0.4f;
        var renderer = obj.AddComponent<SpriteRenderer>();
        renderer.sprite = VanillaAsset.CloseButtonSprite;
        renderer.drawMode = SpriteDrawMode.Sliced;
        renderer.size = new Vector2(btnSize, btnSize);
        renderer.color = new UColor(0.8f, 0.2f, 0.2f, 1f);
        renderer.sortingOrder = 53;

        var collider = obj.AddComponent<BoxCollider2D>();
        collider.size = new Vector2(btnSize, btnSize);
        collider.isTrigger = true;

        var button = obj.AddComponent<PassiveButton>();
        button.OnMouseOver = new UnityEvent();
        button.OnMouseOut = new UnityEvent();
        button.OnClick = new Button.ButtonClickedEvent();
        button.OnClick.AddListener((UnityAction)(() => screen.Close()));
        button.OnMouseOver.AddListener((UnityAction)(() => { renderer.color = new UColor(1f, 0.4f, 0.4f, 1f); }));
        button.OnMouseOut.AddListener((UnityAction)(() => { renderer.color = new UColor(0.8f, 0.2f, 0.2f, 1f); }));

        return obj;
    }

    /// <summary>
    /// 快捷创建文本窗口（兼容现有调用）
    /// </summary>
    public static MetaScreen CreateWindow(string text)
    {
        var parent = HudManager.Instance != null ? HudManager.Instance.transform : null;
        if (parent == null)
        {
            LightLogger.LogWarning("[MetaScreen] HudManager 未就绪");
            return null!;
        }

        var gui = LIDGUI.Instance;
        var content = gui.VerticalHolder(
            GUIAlignment.Center,
            gui.RawText(GUIAlignment.Center, gui.GetAttribute(AttributeAsset.DocumentTitle), text),
            gui.VerticalMargin(0.3f),
            gui.RawButton(GUIAlignment.Center, gui.GetAttribute(AttributeAsset.CenteredBold), "关闭", _ => { })
        );

        var screen = GenerateWindow(new Vector2(4f, 1.5f), parent, new Vector3(0f, 0f, -50f));
        screen.SetWidget(content, out _);
        return screen;
    }

    /// <summary>
    /// 快捷创建带标题和内容的窗口
    /// </summary>
    public static MetaScreen CreateWindow(string title, GUIWidget content)
    {
        var parent = HudManager.Instance != null ? HudManager.Instance.transform : null;
        if (parent == null)
        {
            LightLogger.LogWarning("[MetaScreen] HudManager 未就绪");
            return null!;
        }

        var gui = LIDGUI.Instance;
        var fullContent = gui.VerticalHolder(
            GUIAlignment.Center,
            gui.RawText(GUIAlignment.Center, gui.GetAttribute(AttributeAsset.DocumentTitle), title),
            gui.VerticalMargin(0.2f),
            content
        );

        var screen = GenerateWindow(new Vector2(5f, 3f), parent, new Vector3(0f, 0f, -50f));
        screen.SetWidget(fullContent, out _);
        return screen;
    }
}
