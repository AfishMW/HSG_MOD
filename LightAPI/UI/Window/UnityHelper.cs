using UnityEngine;

namespace LightInDark.UI.Window;

/// <summary>
/// Unity 辅助方法，对应 Nebula 的 UnityHelper
/// </summary>
public static class UnityHelper
{
    /// <summary>
    /// 创建带组件的 GameObject
    /// </summary>
    public static T CreateObject<T>(string name, Transform? parent, Vector3 localPos, int layer = -1) where T : Component
    {
        var obj = new GameObject(name);
        if (layer >= 0) obj.layer = layer;
        else obj.layer = LayerExpansion.GetUILayer();
        if (parent != null) obj.transform.SetParent(parent, false);
        obj.transform.localPosition = localPos;
        obj.transform.localScale = Vector3.one;
        return obj.AddComponent<T>();
    }

    /// <summary>
    /// 创建 GameObject
    /// </summary>
    public static GameObject CreateObject(string name, Transform? parent, Vector3 localPos, int layer = -1)
    {
        var obj = new GameObject(name);
        if (layer >= 0) obj.layer = layer;
        else obj.layer = LayerExpansion.GetUILayer();
        if (parent != null) obj.transform.SetParent(parent, false);
        obj.transform.localPosition = localPos;
        obj.transform.localScale = Vector3.one;
        return obj;
    }
}

/// <summary>
/// 层级扩展，对应 Nebula 的 LayerExpansion
/// </summary>
public static class LayerExpansion
{
    private static int _uiLayer = -1;

    public static int GetUILayer()
    {
        if (_uiLayer >= 0) return _uiLayer;
        _uiLayer = LayerMask.NameToLayer("UI");
        if (_uiLayer < 0) _uiLayer = 5; // Unity 默认 UI 层
        return _uiLayer;
    }

    private static int _defaultLayer = -1;
    public static int GetDefaultLayer()
    {
        if (_defaultLayer >= 0) return _defaultLayer;
        _defaultLayer = LayerMask.NameToLayer("Default");
        if (_defaultLayer < 0) _defaultLayer = 0;
        return _defaultLayer;
    }
}
