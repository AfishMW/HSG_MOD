using System;
using System.IO;
using LightInDark.Core;
using System.Text.Json;
using UnityEngine;
using Light.Utilities;

namespace Light.UI;
public static class Cursor
{
    public static int Index { get; private set; }
    private static string ConfigPath => Path.Combine(LightPlugin.CursurDataPath, "Cursor_LID.json");
    public static Texture2D Cur_1 = ResourceHelper.LoadTextureFromResoucesTOUE("Resources.Cursor.1.png");
    public static Texture2D Cur_2 = ResourceHelper.LoadTextureFromResoucesTOUE("Resources.Cursor.2.png");

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
            LightLogger.LogError($"[Cursor] 更换异常",ex);
            return null;
        }
    }

    static int LoadIndexFromJson()
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
            LightLogger.LogWarning($"[Cursor] 读取配置失败: {ex.Message}");
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
            LightLogger.LogWarning($"[Cursor] 保存配置失败: {ex.Message}");
        }
    }

}