using System;
using System.Collections.Generic;
using Il2CppInterop.Runtime.Injection;
using LightInDark.UI.Window;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using Color = LightInDark.Color;
using Object = UnityEngine.Object;

namespace Light.UI.Window;

/// <summary>
/// 输入框 Widget 包装：将 GUITextField 接入 GUI 布局体系
/// </summary>
public class TextFieldWidget : AbstractGUIWidget
{
    /// <summary>底层输入框（Instantiate 后可用）</summary>
    public GUITextField? Field { get; private set; }

    private readonly Vector2 _size;
    private readonly string _hint;
    private readonly Action<string>? _onEnter;

    public TextFieldWidget(GUIAlignment alignment, Vector2 size, string hint, Action<string>? onEnter) : base(alignment)
    {
        _size = size;
        _hint = hint;
        _onEnter = onEnter;
    }

    public override GameObject? Instantiate(Size size, out Size actualSize)
    {
        Field = GUITextField.Create(null, _size, _hint, _onEnter);
        actualSize = new Size(_size);
        return Field.GameObject;
    }
}

/// <summary>
/// 最小文本输入框（无光标/多行）
/// </summary>
public class GUITextField
{
    private static readonly Dictionary<TextFieldBehaviour, GUITextField> _fields = new();

    /// <summary>当前输入文本</summary>
    public string Text => _behaviour.Value;

    /// <summary>回车确认回调</summary>
    public Action<string>? EnterAction { get; set; }

    /// <summary>根对象</summary>
    public GameObject GameObject { get; }

    private readonly TextFieldBehaviour _behaviour;

    private GUITextField(GameObject obj, TextFieldBehaviour behaviour)
    {
        GameObject = obj;
        _behaviour = behaviour;
        _fields[behaviour] = this;
    }

    /// <summary>
    /// 创建输入框：背景 + 文本 + 点击聚焦
    /// </summary>
    public static GUITextField Create(Transform parent, Vector2 size, string hint = "", Action<string>? onEnter = null)
    {
        var obj = new GameObject("GUITextField");
        obj.layer = LayerExpansion.GetUILayer();
        obj.transform.SetParent(parent, false);
        obj.transform.localPosition = Vector3.zero;

        // 背景
        var renderer = obj.AddComponent<SpriteRenderer>();
        renderer.sprite = VanillaAsset.PopUpBackSprite;
        renderer.drawMode = SpriteDrawMode.Sliced;
        renderer.tileMode = SpriteTileMode.Continuous;
        renderer.size = size;

        // 碰撞体
        var collider = obj.AddComponent<BoxCollider2D>();
        collider.size = size;
        collider.isTrigger = true;

        // 行为组件（聚焦后每帧处理输入）
        var behaviour = obj.AddComponent<TextFieldBehaviour>();
        behaviour.Hint = hint;

        // 文本
        var tmp = Object.Instantiate(VanillaAsset.StandardTextPrefab, obj.transform);
        tmp.transform.localPosition = new Vector3(-size.x * 0.5f + 0.15f, 0f, -0.1f);
        tmp.rectTransform.pivot = new Vector2(0f, 0.5f);
        tmp.rectTransform.sizeDelta = new Vector2(size.x - 0.3f, size.y - 0.06f);
        tmp.fontSize = 1.35f;
        tmp.fontSizeMin = 1f;
        tmp.fontSizeMax = 1.6f;
        tmp.enableAutoSizing = true;
        tmp.alignment = TextAlignmentOptions.Left;
        tmp.raycastTarget = false;
        tmp.text = hint;
        tmp.color = UnityEngine.Color.gray;
        tmp.ForceMeshUpdate();
        behaviour.TMP = tmp;

        // 点击聚焦
        var backColor = new Color(0.16f, 0.16f, 0.16f, 0.85f);
        var hoverColor = new Color(0.3f, 0.3f, 0.3f, 0.9f);
        var button = obj.SetUpButton(true, renderer, backColor, hoverColor, playSound: false);
        button.OnClick.AddListener((UnityAction)(() => behaviour.Focused = true));

        var field = new GUITextField(obj, behaviour);
        field.EnterAction = onEnter;
        return field;
    }

    internal static void NotifyEnter(TextFieldBehaviour behaviour)
    {
        if (_fields.TryGetValue(behaviour, out var field))
            field.EnterAction?.Invoke(field.Text);
    }

    internal static void RemoveField(TextFieldBehaviour behaviour)
    {
        _fields.Remove(behaviour);
    }
}

/// <summary>
/// 输入框行为组件：聚焦后每帧读取 Input.inputString 更新文本
/// </summary>
public class TextFieldBehaviour : MonoBehaviour
{
    static TextFieldBehaviour()
    {
        ClassInjector.RegisterTypeInIl2Cpp<TextFieldBehaviour>();
    }

    public TextMeshPro? TMP;
    public bool Focused;
    public string Value = "";
    public string Hint = "";

    public void Update()
    {
        if (!Focused) return;

        foreach (char c in Input.inputString)
        {
            if (c == '\r' || c == '\n')
            {
                Focused = false;
                GUITextField.NotifyEnter(this);
            }
            else if (c == '\b')
            {
                if (Value.Length > 0)
                    Value = Value.Substring(0, Value.Length - 1);
            }
            else if (c == '\u001b')
            {
                Focused = false;
            }
            else if (!char.IsControl(c))
            {
                Value += c;
            }
        }

        if (TMP != null)
        {
            bool empty = Value.Length == 0;
            TMP.text = empty ? Hint : Value;
            TMP.color = empty ? UnityEngine.Color.gray : UnityEngine.Color.white;
            TMP.ForceMeshUpdate();
        }
    }

    public void OnDestroy()
    {
        GUITextField.RemoveField(this);
    }
}
