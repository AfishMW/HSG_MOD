using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using AmongUs.Data;
using LightInDark.Core;

namespace LightInDark.Language;

public static class Language
{
    private static readonly Dictionary<string, Dictionary<string, string>> _all = new();
    private static string _current = "English";
    private static readonly string _langFolder;
    private static readonly object _fileLock = new();

    static Language()
    {
        try
        {
            _langFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Language");
        }
        catch (Exception ex)
        {
            LightLogger.LogError("Language.Language(static ctor)", ex);
        }
    }

    public static void Load()
    {
        try
        {
            if (!Directory.Exists(_langFolder))
                Directory.CreateDirectory(_langFolder);

            foreach (string file in Directory.GetFiles(_langFolder, "*.json"))
            {
                string name = Path.GetFileNameWithoutExtension(file);
                try
                {
                    string json = File.ReadAllText(file, Encoding.UTF8);
                    var dict = JsonSerializer.Deserialize<Dictionary<string, string>>(json);
                    if (dict != null)
                        _all[name] = dict;
                }
                catch { }
            }

            UpdateCurrentLanguage();
        }
        catch (Exception ex)
        {
            LightLogger.LogError("Language.Load", ex);
        }
    }

    public static void UpdateCurrentLanguage()
    {
        try
        {
            string lang = DataManager.Settings.Language.CurrentLanguage.ToString();
            if (_all.ContainsKey(lang))
                _current = lang;
            else if (_all.ContainsKey("English"))
                _current = "English";
            else
                _current = _all.Keys.FirstOrDefault() ?? "English";

            if (!_all.ContainsKey(_current))
            {
                _all[_current] = new Dictionary<string, string>();
                SaveLanguageFile(_current, _all[_current]);
            }
        }
        catch (Exception ex)
        {
            LightLogger.LogError("Language.UpdateCurrentLanguage", ex);
        }
    }

    public static string Translate(string key, string fallback = null)
    {
        try
        {
            // 命中且值不是历史遗留的"占位"时返回翻译
            if (_all.TryGetValue(_current, out var dict) && dict.TryGetValue(key, out string val) && val != "占位")
                return val;

            if (_current != "English" && _all.TryGetValue("English", out var enDict) && enDict.TryGetValue(key, out string enVal) && enVal != "占位")
                return enVal;

            // 未命中：返回 fallback ?? key，不再把"占位"写进语言文件（否则下次命中"占位"导致 UI 显示占位符）
            return fallback ?? key;
        }
        catch (Exception ex)
        {
            LightLogger.LogError("Language.Translate", ex);
            return fallback ?? key;
        }
    }

    private static void SaveLanguageFile(string langName, Dictionary<string, string> dict)
    {
        try
        {
            string path = Path.Combine(_langFolder, langName + ".json");
            string json = JsonSerializer.Serialize(dict, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(path, json, Encoding.UTF8);
        }
        catch (Exception ex)
        {
            LightLogger.LogError("Language.SaveLanguageFile", ex);
        }
    }

    public static string GetString(string key)
    {
        try
        {
            return Translate(key, "*" + key);
        }
        catch (Exception ex)
        {
            LightLogger.LogError("Language.GetString", ex);
            return default;
        }
    }

    /// <summary>
    /// 取语言键对应的文本；若键不存在，则返回 <paramref name="fallback"/>
    /// （fallback 为空时返回键本身）。
    /// </summary>
    public static string GetStringOrKey(string key, string fallback = "")
    {
        try
        {
            string result = Translate(key, null);
            if (result == null || result == key) // 未命中
                return string.IsNullOrEmpty(fallback) ? key : fallback;
            return result;
        }
        catch (Exception ex)
        {
            LightLogger.LogError("Language.GetStringOrKey", ex);
            return string.IsNullOrEmpty(fallback) ? key : fallback;
        }
    }

    public static bool TryGetString(string key, ref string output)
    {
        try
        {
            string result = Translate(key, null);
            if (result != key)
            {
                output = result;
                return true;
            }
            return false;
        }
        catch (Exception ex)
        {
            LightLogger.LogError("Language.TryGetString", ex);
            return default;
        }
    }
}
