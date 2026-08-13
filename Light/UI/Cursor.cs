using System;
using System.IO;
using LightInDark.Core;
using System.Text.Json;
using UnityEngine;
using Light.Utilities;

namespace Light.UI;

/// <summary>
/// 光标管理器（纯静态，不继承 MonoBehaviour，避免 Il2Cpp AddComponent 异常）
/// </summary>
public static class Cursor
{
    public static int Index { get; private set; }
    private static string ConfigPath => Path.Combine(LightPlugin.CursurDataPath, "Cursor_LID.json");
    public static Texture2D Cur_1 = ResourceHelper.LoadTexture("Cursor/1.png");
    public static Texture2D Cur_2 = ResourceHelper.LoadTexture("Cursor/2.png");

    /// <summary>初始化光标（在 LightPlugin.Load 中调用，替代原 AddComponent）</summary>
    public static void Initialize()
    {
        try
        {
            Index = LoadIndexFromJson();
            ChangeCursorFromIndex(Index);
        }
        catch (Exception ex)
        {
            LightLogger.LogError("[Cursor.Initialize]", ex);
        }
    }
    /// <summary>
    /// 
    /// </summary>
    /// <param name="index"></param>
    /// <returns>返回true更换成功，返回false为无索引，返回null为触发异常。</returns>
    public static bool? ChangeCursorFromIndex(int index)
    {
        try
        {
            switch (index)
            {
                case 0:
                    UnityEngine.Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
                    LightLogger.Log($"[Cursor] index {index}");
                    return true;
                case 1:
                    UnityEngine.Cursor.SetCursor(Cur_1, Vector2.zero, CursorMode.Auto);
                    LightLogger.Log($"[Cursor] index {index}");
                    return true;
                case 2:
                    UnityEngine.Cursor.SetCursor(Cur_2, Vector2.zero, CursorMode.Auto);
                    LightLogger.Log($"[Cursor] index {index}");
                    return true;
                default:
                    LightLogger.Log($"[Cursor] 未知索引： {index} ");
                    return false;
            }
        }
        catch(Exception ex)
        {
            LightLogger.LogError($"[Cursor] 更换异常：{ex.Message} 堆栈->\n{ex}");
            return null;
        }
    }

    private static int LoadIndexFromJson()
    {
        try
        {
            if (!File.Exists(ConfigPath))
            {
                var defaultData = new { index = 0 };
                string json = JsonSerializer.Serialize(defaultData, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(ConfigPath, json);
                return 0;
            }

            string content = File.ReadAllText(ConfigPath);
            using var doc = JsonDocument.Parse(content);
            return doc.RootElement.TryGetProperty("index", out var el) ? el.GetInt32() : 0;
        }
        catch (Exception ex)
        {
            LightInDark.Core.LightLogger.LogWarning($"[Cursor] 读取配置失败: {ex.Message}");
            return 0;
        }
    }

    public static void SaveIndex(int index)
    {
        try
        {
            var data = new { index };
            string json = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(ConfigPath, json);
        }
        catch (Exception ex)
        {
            LightInDark.Core.LightLogger.LogWarning($"[Cursor] 保存配置失败: {ex.Message}");
        }
    }

}