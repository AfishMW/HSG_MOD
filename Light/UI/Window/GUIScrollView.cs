using LightInDark.UI.Window;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace Light.UI.Window;

/// <summary>
/// 滚动视图 Widget（原版 Scroller 组件 + 克隆原版滚动条）
/// </summary>
public class GUIScrollView : AbstractGUIWidget
{
    /// <summary>
    /// 滚动内容屏幕，负责销毁重建内容并更新滚动范围
    /// </summary>
    public class InnerScreen
    {
        public bool IsValid => _screen != null;

        private GameObject? _screen;
        private Size _innerSize;
        private Scroller? _scroller;
        private Collider2D? _scrollerCollider;
        private float _viewHeight;

        public InnerScreen(GameObject screen, Size innerSize, Scroller scroller, Collider2D? scrollerCollider, float scrollViewSizeY)
        {
            _screen = screen;
            _innerSize = innerSize;
            _scroller = scroller;
            _scrollerCollider = scrollerCollider;
            _viewHeight = scrollViewSizeY;
        }

        public void SetWidget(GUIWidget? widget, out Size actualSize)
        {
            if (_screen == null)
            {
                actualSize = Size.Zero;
                return;
            }

            // 销毁旧内容
            for (int i = _screen.transform.childCount - 1; i >= 0; i--)
                Object.Destroy(_screen.transform.GetChild(i).gameObject);

            if (widget != null)
            {
                var obj = widget.Instantiate(_innerSize, out actualSize);
                if (obj != null)
                {
                    obj.transform.SetParent(_screen.transform, false);
                    obj.transform.localPosition = Vector3.zero;
                    ApplyMask(obj);

                    // 内容按钮 ClickMask 设为滚动区域碰撞体，避免窗口大 ClickGuard 挡住点击
                    if (_scrollerCollider != null)
                        foreach (var button in _screen.GetComponentsInChildren<PassiveButton>(true))
                            button.ClickMask = _scrollerCollider;

                    _scroller!.SetBounds(new FloatRange(0, Mathf.Max(0f, actualSize.Height - _viewHeight)), null);
                    _scroller.ScrollRelative(Vector2.zero);
                    return;
                }
            }

            actualSize = Size.Zero;
        }
    }

    public Size ScrollSize { get; init; }
    public bool WithMask { get; init; } = true;
    public GUIWidgetSupplier? Inner { get; init; }

    private InnerScreen? _artifact;
    public InnerScreen? Artifact => _artifact;

    public GUIScrollView(GUIAlignment alignment, Size size, GUIWidgetSupplier? inner) : base(alignment)
    {
        ScrollSize = size;
        Inner = inner;
    }

    public override GameObject? Instantiate(Size size, out Size actualSize)
    {
        var view = UnityHelper.CreateObject("ScrollView", null, Vector3.zero, LayerExpansion.GetUILayer());
        view.AddComponent<SortingGroup>();

        var innerSize = new Size(ScrollSize.Width - 0.4f, ScrollSize.Height);

        if (WithMask)
        {
            var mask = UnityHelper.CreateObject<SpriteMask>("Mask", view.transform, new Vector3(-0.2f, 0f, 0f));
            mask.sprite = VanillaAsset.FullScreenSprite;
            mask.transform.localScale = new Vector3(innerSize.Width, innerSize.Height, 1f);
        }

        var inner = UnityHelper.CreateObject("Inner", view.transform, new Vector3(-0.2f, 0f, -0.1f));

        var scroller = VanillaAsset.GenerateScroller(ScrollSize.ToUnityVector(), view.transform,
            new Vector3(ScrollSize.Width / 2f - 0.15f, 0f, 0f), inner.transform,
            new FloatRange(0, ScrollSize.Height), ScrollSize.Height);

        // 滚动区域碰撞体（GenerateScroller 内部添加的 BoxCollider2D）
        var scrollerCollider = scroller.GetComponent<Collider2D>();

        _artifact = new InnerScreen(inner, innerSize, scroller, scrollerCollider, ScrollSize.Height);
        _artifact.SetWidget(Inner?.Invoke(), out _);

        actualSize = new Size(ScrollSize.Width + 0.15f, ScrollSize.Height + 0.08f);
        return view;
    }

    /// <summary>
    /// 内容树遮罩设置：SpriteRenderer 收进 SpriteMask，文本换 masked 材质
    /// </summary>
    private static void ApplyMask(GameObject root)
    {
        foreach (var sr in root.GetComponentsInChildren<SpriteRenderer>(true))
            sr.maskInteraction = SpriteMaskInteraction.VisibleInsideMask;
        foreach (var tmp in root.GetComponentsInChildren<TextMeshPro>(true))
            tmp.fontSharedMaterial = VanillaAsset.StandardMaskedFontMaterial;
    }
}
