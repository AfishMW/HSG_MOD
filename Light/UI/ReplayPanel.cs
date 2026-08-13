using TMPro;
using UnityEngine;
using LightInDark.Core;
using System;

namespace Light.UI;

/// <summary>
/// 复盘面板：在大厅中按 V 键显示上一轮的复盘信息。
/// 位置：左上方。参考 FS 的 LastResult 定位 (-4.5f, 2.6f)。
/// </summary>
public static class ReplayPanel
{
    private static GameObject? _panelObj;
    private static TextMeshPro? _text;

    /// <summary>显示复盘面板</summary>
    public static void Show()
    {
        try
        {
            if (HudManager.Instance == null) return;

            if (_panelObj == null)
            {
                _panelObj = new GameObject("LightReplayPanel");
                _panelObj.transform.SetParent(HudManager.Instance.transform);
                _panelObj.transform.localPosition = new Vector3(-4.5f, 2.6f, -10f);
                _panelObj.transform.localScale = Vector3.one * 0.35f;

                _text = _panelObj.AddComponent<TextMeshPro>();
                _text.fontSize = 3f;
                _text.alignment = TextAlignmentOptions.TopLeft;
                _text.color = Color.white;
                _text.raycastTarget = false;
            }

            _text.text = LightInDark.Game.LightPlayerDataManager.BuildReplayText();
            _panelObj.SetActive(true);
        }
        catch (Exception ex)
        {
            LightLogger.LogError("[ReplayPanel.Show]", ex);
        }
    }

    /// <summary>隐藏复盘面板</summary>
    public static void Hide()
    {
        try
        {
            if (_panelObj != null)
                _panelObj.SetActive(false);
        }
        catch (Exception ex)
        {
            LightLogger.LogError("[ReplayPanel.Hide]", ex);
        }
    }

    /// <summary>切换显示</summary>
    public static bool Toggle()
    {
        try
        {
            if (_panelObj == null || !_panelObj.activeSelf)
            {
                Show();
                return true;
            }
            Hide();
            return false;
        }
        catch (Exception ex)
        {
            LightLogger.LogError("[ReplayPanel.Toggle]", ex); return default;
        }
    }

    /// <summary>销毁面板（场景切换时）</summary>
    public static void Destroy()
    {
        try
        {
            if (_panelObj != null)
            {
                UnityEngine.Object.Destroy(_panelObj);
                _panelObj = null;
                _text = null;
            }
        }
        catch (Exception ex)
        {
            LightLogger.LogError("[ReplayPanel.Destroy]", ex);
        }
    }
}
