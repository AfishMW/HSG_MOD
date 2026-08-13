using LightInDark.UI.Window;
using UnityEngine;
using LightInDark.Core;
using System;

namespace Light.UI.Window;

/// <summary>
/// Widget 扩展方法
/// </summary>
public static class GUIWidgetHelpers
{
    public static GUIWidget WithRoom(this GUIWidget inner, Vector2 margin)
    {
        try
        {
            var gui = LIDGUI.Instance;

            if (margin.x > 0f)
            {
                var xMargin = gui.HorizontalMargin(margin.x * 0.5f);
                inner = gui.HorizontalHolder(inner.Alignment, xMargin, inner, xMargin);
            }
            if (margin.y > 0f)
            {
                var yMargin = gui.VerticalMargin(margin.y * 0.5f);
                inner = gui.VerticalHolder(inner.Alignment, yMargin, inner, yMargin);
            }
            return inner;
        }
        catch (Exception ex)
        {
            LightLogger.LogError("[GUIWidgetHelpers.WithRoom]", ex); return default;
        }
    }

    public static GUIWidget Move(this GUIWidget inner, Vector2 diff)
    {
        try
        {
            var gui = LIDGUI.Instance;

            if (diff.x > 0f)
                inner = gui.HorizontalHolder(inner.Alignment, gui.HorizontalMargin(diff.x), inner);
            if (diff.x < 0f)
                inner = gui.HorizontalHolder(inner.Alignment, inner, gui.HorizontalMargin(-diff.x));

            if (diff.y > 0f)
                inner = gui.VerticalHolder(inner.Alignment, gui.VerticalMargin(diff.y), inner);
            if (diff.y < 0f)
                inner = gui.VerticalHolder(inner.Alignment, inner, gui.VerticalMargin(-diff.y));

            return inner;
        }
        catch (Exception ex)
        {
            LightLogger.LogError("[GUIWidgetHelpers.Move]", ex); return default;
        }
    }

    public static GUIWidget FixSize(this GUIWidget inner, Size size)
        => new GUISizeFixer(inner, size);

    public static GUIWidget WithLogic(this GUIWidget inner, System.Action<GameObject?, Size> logic)
        => new LogicGUIWidget(inner, logic);
}
