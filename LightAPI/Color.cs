using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace LightInDark;

public struct Color
{
    public float R, G, B, A;
    public Color(float r,float g,float b,float a = 1f)
    {
        R = r; G = g; B = b; A = a;
    }
    public Color(int r, int g, int b, float a = 1f)
    {
        R = Math.Clamp(r, 0, 255) / 255f;
        G = Math.Clamp(g, 0, 255) / 255f;
        B = Math.Clamp(b, 0, 255) / 255f;
        A = a;
    }
    static public Color ImpostorColor { get; set; } = Palette.ImpostorRed.ToLIDColor();
    static public Color CrewmateColor { get; set; } = Palette.CrewmateBlue.ToLIDColor();
    static public Color Red { get;  set; } = new(1f, 0f, 0f, 1f);
    static public Color Yellow { get;  set; } = new(1f, 1f, 0f, 1f);
    static public Color Green { get;  set; } = new(0f, 1f, 0f, 1f);
    static public Color Cyan { get;  set; } = new(0f, 1f, 1f, 1f);
    static public Color Blue { get;  set; } = new(0f, 0f, 1f, 1f);
    static public Color Magenta { get;  set; } = new(1f, 0f, 1f, 1f);
    static public Color White { get;  set; } = new(1f, 1f, 1f, 1f);
    static public Color Black { get; set; } = new(0f, 0f, 0f, 1f);
    static public Color Gray { get;  set; } = new(0.5f, 0.5f, 0.5f, 1f);
    static public Color Clear { get; internal set; } = new(0f, 0f, 0f, 0f);

}
public static class ColorHelper
{
    public static UColor ToUnityColor(this Color color) => new(color.R, color.G, color.B, color.A);
    public static Color ToLIDColor(this UnityEngine.Color color) => new(color.r, color.g, color.b, color.a);
}