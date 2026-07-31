using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using AmongUs.Data;

namespace LightInDark.Language;

public static class Language
{
    private static readonly Dictionary<string, Dictionary<string, string>> _all = new();
    private static string _current = "English";
    private static readonly string _langFolder;
    private static readonly object _fileLock = new();

    static Language()
    {
        _langFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Language");
    }

    public static void Load()
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

    public static void UpdateCurrentLanguage()
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

    public static string Translate(string key, string fallback = null)
    {
        if (_all.TryGetValue(_current, out var dict) && dict.TryGetValue(key, out string val))
            return val;

        if (_current != "English" && _all.TryGetValue("English", out var enDict) && enDict.TryGetValue(key, out string enVal))
            return enVal;

        if (_all.TryGetValue(_current, out var currentDict))
        {
            lock (_fileLock)
            {
                if (!currentDict.ContainsKey(key))
                {
                    currentDict[key] = "占位";
                    SaveLanguageFile(_current, currentDict);
                }
            }
        }

        return fallback ?? key;
    }

    private static void SaveLanguageFile(string langName, Dictionary<string, string> dict)
    {
        string path = Path.Combine(_langFolder, langName + ".json");
        string json = JsonSerializer.Serialize(dict, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(path, json, Encoding.UTF8);
    }

    public static string GetString(string key) => Translate(key, "*" + key);
    public static bool TryGetString(string key, ref string output)
    {
        string result = Translate(key, null);
        if (result != key)
        {
            output = result;
            return true;
        }
        return false;
    }
}