using UnityEngine;
using System.Reflection;
using System.IO;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using LightInDark.Core;
namespace Light.Utilities;

public static class ResourceHelper
{
    private const string DefaultResourceRoot = "Light.Resources.";
    public static string ResourceRoot { get; set; } = DefaultResourceRoot;
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
    public unsafe static Texture2D LoadTextureFromResoucesTOUE(string path)
    {
        try
        {
            Texture2D texture = new(2,2,TextureFormat.ARGB32,true);
            Assembly assembly = Assembly.GetExecutingAssembly();
            Stream stream = assembly.GetManifestResourceStream(path);
            long length = stream.Length;
            Il2CppStructArray<byte> byteTexture = new(length);
            stream.Read(new Span<byte>(IntPtr.Add(byteTexture.Pointer,IntPtr.Size*4).ToPointer(),(int)length));
            ImageConversion.LoadImage(texture, byteTexture, false);
            return texture;
        }
        catch(Exception ex)
        {
            LightLogger.LogError("[ResourcesHelper] Error1",ex);
        }
        return null;
    }
    public static Sprite LoadSpriteFromResource(string resourcePath, float pixelsPerUnit = 100f, Vector2? pivot = null)
    {
        Texture2D tex = LoadTextureFromResource(resourcePath);
        if (tex == null) return null;

        Vector2 pivotPoint = pivot ?? new Vector2(0.5f, 0.5f);
        return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), pivotPoint, pixelsPerUnit);
    }
    public static Texture2D LoadTexture(string relativePath)
    {
        string fullPath = ResourceRoot + relativePath.Replace('/', '.').Replace('\\', '.');
        return LoadTextureFromResource(fullPath);
    }
    public static Sprite LoadSprite(string relativePath, float pixelsPerUnit = 100f, Vector2? pivot = null)
    {
        Texture2D tex = LoadTexture(relativePath);
        if (tex == null) return null;
        Vector2 pivotPoint = pivot ?? new Vector2(0.5f, 0.5f);
        return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), pivotPoint, pixelsPerUnit);
    }
}