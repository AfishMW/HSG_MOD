using UnityEngine;
using System.Reflection;
using System.IO;
namespace LightInDark.Utilities;

public static class ResourceHelper
{
    /// <summary>
    /// 从当前程序集的嵌入资源加载 Texture2D
    /// </summary>
    /// <param name="resourcePath">完整资源路径</param>
    public static Texture2D LoadTextureFromResource(string resourcePath)
    {
        var assembly = Assembly.GetExecutingAssembly();
        using (Stream stream = assembly.GetManifestResourceStream(resourcePath))
        {
            if (stream == null)
            {
                Debug.LogError($"Resource not found: {resourcePath}");
                return null;
            }
            byte[] bytes = new byte[stream.Length];
            stream.Read(bytes, 0, bytes.Length);

            Texture2D texture = new Texture2D(2, 2, TextureFormat.ARGB32, false);
            if (!ImageConversion.LoadImage(texture, bytes, false))
            {
                Debug.LogError($"Failed to load image from resource: {resourcePath}");
                return null;
            }
            texture.wrapMode = TextureWrapMode.Clamp;
            return texture;
        }
    }

    public static Sprite LoadSpriteFromResource(string resourcePath, float pixelsPerUnit = 100f, Vector2? pivot = null)
    {
        Texture2D tex = LoadTextureFromResource(resourcePath);
        if (tex == null) return null;

        Vector2 pivotPoint = pivot ?? new Vector2(0.5f, 0.5f);
        return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), pivotPoint, pixelsPerUnit);
    }
}