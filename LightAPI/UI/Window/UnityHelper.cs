using System;
using LightInDark.Core;
using UnityEngine;

namespace LightInDark.UI.Window;

/// <summary>
/// Unity 辅助方法
/// </summary>
public static class UnityHelper
{
    /// <summary>
    /// 创建带组件的 GameObject
    /// </summary>
    public static T CreateObject<T>(string name, Transform? parent, Vector3 localPos, int layer = -1) where T : Component
    {
        try
        {
            var obj = new GameObject(name);
            if (layer >= 0) obj.layer = layer;
            else obj.layer = LayerExpansion.GetUILayer();
            if (parent != null) obj.transform.SetParent(parent, false);
            obj.transform.localPosition = localPos;
            obj.transform.localScale = Vector3.one;
            return obj.AddComponent<T>();
        }
        catch (Exception ex)
        {
            LightLogger.LogError("UnityHelper.CreateObject<T>", ex);
            return default;
        }
    }

    /// <summary>
    /// 创建 GameObject
    /// </summary>
    public static GameObject CreateObject(string name, Transform? parent, Vector3 localPos, int layer = -1)
    {
        try
        {
            var obj = new GameObject(name);
            if (layer >= 0) obj.layer = layer;
            else obj.layer = LayerExpansion.GetUILayer();
            if (parent != null) obj.transform.SetParent(parent, false);
            obj.transform.localPosition = localPos;
            obj.transform.localScale = Vector3.one;
            return obj;
        }
        catch (Exception ex)
        {
            LightLogger.LogError("UnityHelper.CreateObject", ex);
            return null;
        }
    }

    /// <summary>查找渲染指定图层的相机（优先 UI 相机，找不到时回退主相机）</summary>
    public static Camera? FindCamera(int layer)
    {
        try
        {
            int mask = 1 << layer;
            foreach (var cam in Camera.allCameras)
                if ((cam.cullingMask & mask) != 0) return cam;
            return Camera.main;
        }
        catch (Exception ex)
        {
            LightLogger.LogError("UnityHelper.FindCamera", ex);
            return null;
        }
    }
}

/// <summary>
/// 层级扩展
/// </summary>
public static class LayerExpansion
{
    private static int _uiLayer = -1;

    public static int GetUILayer()
    {
        try
        {
            if (_uiLayer >= 0) return _uiLayer;
            _uiLayer = LayerMask.NameToLayer("UI");
            if (_uiLayer < 0) _uiLayer = 5; // Unity 默认 UI 层
            return _uiLayer;
        }
        catch (Exception ex)
        {
            LightLogger.LogError("LayerExpansion.GetUILayer", ex);
            return default;
        }
    }

    private static int _defaultLayer = -1;
    public static int GetDefaultLayer()
    {
        try
        {
            if (_defaultLayer >= 0) return _defaultLayer;
            _defaultLayer = LayerMask.NameToLayer("Default");
            if (_defaultLayer < 0) _defaultLayer = 0;
            return _defaultLayer;
        }
        catch (Exception ex)
        {
            LightLogger.LogError("LayerExpansion.GetDefaultLayer", ex);
            return default;
        }
    }
}
