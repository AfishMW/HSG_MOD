using System;
using System.IO;
using System.Reflection;
using Il2CppInterop.Runtime;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using UnityEngine;

namespace Light.Utilities;

internal static class GraphicsHelper
{
    private delegate bool d_LoadImage(IntPtr tex, IntPtr data, bool markNonReadable);
    private static d_LoadImage? iCall_LoadImage;

    public static Texture2D LoadTextureFromResources(string path)
    {
        Texture2D texture = new(2, 2, TextureFormat.ARGB32, false);
        Assembly assembly = Assembly.GetExecutingAssembly();
        using Stream? stream = assembly.GetManifestResourceStream(path);
        if (stream == null) return null!;

        byte[] byteTexture = new byte[stream.Length];
        stream.Read(byteTexture, 0, (int)stream.Length);
        LoadImage(texture, byteTexture, true);
        return texture;
    }

    private static bool LoadImage(Texture2D tex, byte[] data, bool markNonReadable)
    {
        iCall_LoadImage ??= IL2CPP.ResolveICall<d_LoadImage>("UnityEngine.ImageConversion::LoadImage");
        var il2cppArray = (Il2CppStructArray<byte>)data;
        tex.wrapMode = TextureWrapMode.Clamp;
        return iCall_LoadImage.Invoke(tex.Pointer, il2cppArray.Pointer, markNonReadable);
    }
}
