using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using LightInDark.Configuration;
using LightInDark.Core;
using LightInDark.Roles;

namespace Light.Configuration;

/// <summary>
/// 预设系统（.lid 文件）。
///  - 目录：游戏根目录/Light_Data/Settings/（与 exe 同级）；
///  - current.lid：当前配置的实时镜像（每次配置变更即重写，预设只从 .lid 读取）；
///  - 命名预设：xxx.lid，列表/保存/加载/删除；
///  - 文件内容为 JSON：{ "version": "1.0.0", "roles": { "角色键": { "配置键": 值 } } }；
///  - 版本规则：预设版本与当前 MOD 主版本相差 >= 1 个主版本（如 1.x 与 2.x）拒绝加载；
///  - 键对比：预设键与当前注册配置键不一致（缺/多）时提示"版本可能不正确，是否继续"。
/// </summary>
public static class PresetManager
{
    private const string PresetExt = ".lid";
    private const string CurrentName = "current";

    public static string SettingsDir => Path.Combine(BepInEx.Paths.GameRootPath, "Light_Data", "Settings");
    public static string CurrentPresetPath => Path.Combine(SettingsDir, CurrentName + PresetExt);

    /// <summary>当前 MOD 版本号（与 LightPlugin.Version 一致）。</summary>
    public static string ModVersion => LightPlugin.Version;

    private sealed class PresetData
    {
        public string version { get; set; } = "";
        public Dictionary<string, Dictionary<string, JsonElement>> roles { get; set; } = new();
    }

    public static int MajorOf(string version)
    {
        try
        {
            if (string.IsNullOrEmpty(version)) return 0;
            int dot = version.IndexOf('.');
            string major = dot < 0 ? version : version.Substring(0, dot);
            return int.TryParse(major.Trim(), out int v) ? v : 0;
        }
        catch { return 0; }
    }

    /// <summary>构建当前配置的序列化文本（version + 全部注册职业配置项）。</summary>
    public static string Serialize()
    {
        try
        {
            var data = new PresetData { version = ModVersion };
            foreach (var role in RoleRegistry.AllRoles)
            {
                var opts = RoleConfig.GetRoleOptions(role.CodeName);
                if (opts.Count == 0) continue;
                var map = new Dictionary<string, JsonElement>();
                foreach (var o in opts)
                {
                    object v = o.GetValue();
                    if (v is int i) map[o.Key] = JsonSerializer.SerializeToElement(i);
                    else if (v is float f) map[o.Key] = JsonSerializer.SerializeToElement(f);
                    else if (v is bool b) map[o.Key] = JsonSerializer.SerializeToElement(b);
                    else map[o.Key] = JsonSerializer.SerializeToElement(Convert.ToString(v) ?? "");
                }
                data.roles[role.CodeName] = map;
            }
            return JsonSerializer.Serialize(data);
        }
        catch (Exception ex)
        {
            LightLogger.LogError("[PresetManager.Serialize]", ex);
            return "{}";
        }
    }

    private static PresetData? Deserialize(string json)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(json)) return null;
            return JsonSerializer.Deserialize<PresetData>(json);
        }
        catch (Exception ex)
        {
            LightLogger.LogError("[PresetManager.Deserialize]", ex);
            return null;
        }
    }

    /// <summary>把当前配置实时镜像写入 current.lid。</summary>
    public static void SaveCurrent()
    {
        try
        {
            Directory.CreateDirectory(SettingsDir);
            File.WriteAllText(CurrentPresetPath, Serialize());
        }
        catch (Exception ex)
        {
            LightLogger.LogError("[PresetManager.SaveCurrent]", ex);
        }
    }

    /// <summary>把当前配置保存为命名预设（name.lid）。</summary>
    public static bool SavePreset(string name)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(name)) return false;
            Directory.CreateDirectory(SettingsDir);
            string safe = Sanitize(name);
            File.WriteAllText(Path.Combine(SettingsDir, safe + PresetExt), Serialize());
            return true;
        }
        catch (Exception ex)
        {
            LightLogger.LogError("[PresetManager.SavePreset]", ex);
            return false;
        }
    }

    private static string Sanitize(string name)
    {
        var invalid = Path.GetInvalidFileNameChars();
        foreach (var c in invalid) name = name.Replace(c, '_');
        return name.Trim();
    }

    /// <summary>列出全部命名预设（不含 current.lid）。</summary>
    public static List<string> ListPresets()
    {
        try
        {
            if (!Directory.Exists(SettingsDir)) return new List<string>();
            var list = new List<string>();
            foreach (var f in Directory.GetFiles(SettingsDir, "*" + PresetExt))
            {
                string n = Path.GetFileNameWithoutExtension(f);
                if (n == CurrentName) continue;
                list.Add(n);
            }
            list.Sort(StringComparer.OrdinalIgnoreCase);
            return list;
        }
        catch (Exception ex)
        {
            LightLogger.LogError("[PresetManager.ListPresets]", ex);
            return new List<string>();
        }
    }

    public static void DeletePreset(string name)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(name)) return;
            string path = Path.Combine(SettingsDir, Sanitize(name) + PresetExt);
            if (File.Exists(path)) File.Delete(path);
        }
        catch (Exception ex)
        {
            LightLogger.LogError("[PresetManager.DeletePreset]", ex);
        }
    }

    public enum LoadResult
    {
        Applied,        // 已应用
        Rejected,       // 版本不符，拒绝
        NeedsConfirm,   // 键不一致，需确认后继续
        NotFound,       // 文件不存在
        Error,          // 解析失败
    }

    /// <summary>加载预设：版本检查 + 键对比，返回结果与提示消息。</summary>
    public static LoadResult LoadPreset(string name, out string message, out string path)
    {
        message = "";
        path = "";
        try
        {
            if (string.IsNullOrWhiteSpace(name)) return LoadResult.NotFound;
            string full = Path.Combine(SettingsDir, Sanitize(name) + PresetExt);
            path = full;
            if (!File.Exists(full)) { message = "预设文件不存在: " + name; return LoadResult.NotFound; }

            var data = Deserialize(File.ReadAllText(full));
            if (data == null) { message = "预设文件解析失败: " + name; return LoadResult.Error; }

            // 版本检查：主版本相差 >= 1 拒绝
            int presetMajor = MajorOf(data.version);
            int currentMajor = MajorOf(ModVersion);
            if (Math.Abs(presetMajor - currentMajor) >= 1)
            {
                message = $"预设版本 {data.version} 与当前 MOD 版本 {ModVersion} 主版本不一致，拒绝加载。";
                return LoadResult.Rejected;
            }

            // 键对比：预设键 vs 当前注册配置键
            var currentKeys = new HashSet<string>(StringComparer.Ordinal);
            foreach (var role in RoleRegistry.AllRoles)
                foreach (var o in RoleConfig.GetRoleOptions(role.CodeName))
                    currentKeys.Add(role.CodeName + "." + o.Key);

            var presetKeys = new HashSet<string>(StringComparer.Ordinal);
            foreach (var kv in data.roles)
                foreach (var k in kv.Value.Keys)
                    presetKeys.Add(kv.Key + "." + k);

            bool missing = false, extra = false;
            foreach (var k in presetKeys) if (!currentKeys.Contains(k)) { extra = true; break; }
            foreach (var k in currentKeys) if (!presetKeys.Contains(k)) { missing = true; break; }

            if (missing || extra)
            {
                message = "预设内容与当前配置不一致（缺失或多余选项），版本可能不正确，是否继续？";
                return LoadResult.NeedsConfirm;
            }

            ApplyPresetData(data);
            SaveCurrent();
            message = "预设已加载: " + name;
            return LoadResult.Applied;
        }
        catch (Exception ex)
        {
            LightLogger.LogError("[PresetManager.LoadPreset]", ex);
            message = "加载预设出错: " + ex.Message;
            return LoadResult.Error;
        }
    }

    /// <summary>确认后强制应用预设（跳过键对比提示）。</summary>
    public static void ApplyPresetForce(string name)
    {
        try
        {
            string full = Path.Combine(SettingsDir, Sanitize(name) + PresetExt);
            if (!File.Exists(full)) return;
            var data = Deserialize(File.ReadAllText(full));
            if (data == null) return;
            ApplyPresetData(data);
            SaveCurrent();
        }
        catch (Exception ex)
        {
            LightLogger.LogError("[PresetManager.ApplyPresetForce]", ex);
        }
    }

    /// <summary>应用预设数据：逐个写入匹配的 RoleConfig 条目。</summary>
    private static void ApplyPresetData(PresetData data)
    {
        foreach (var roleKv in data.roles)
        {
            foreach (var optKv in roleKv.Value)
            {
                var entry = RoleConfig.GetOption(roleKv.Key, optKv.Key);
                if (entry == null) continue;
                try
                {
                    object val;
                    if (entry.ValueType == typeof(int)) val = optKv.Value.GetInt32();
                    else if (entry.ValueType == typeof(float)) val = optKv.Value.GetSingle();
                    else if (entry.ValueType == typeof(bool)) val = optKv.Value.GetBoolean();
                    else val = optKv.Value.ToString() ?? "";
                    entry.SetValue(val);
                }
                catch (Exception ex)
                {
                    LightLogger.LogWarning($"[PresetManager] 应用 {roleKv.Key}.{optKv.Key} 失败: {ex.Message}");
                }
            }
        }
    }

    /// <summary>启动时应用 current.lid（恢复上次状态；版本不符则跳过并删除）。</summary>
    public static void ApplyCurrentPreset()
    {
        try
        {
            if (!File.Exists(CurrentPresetPath)) return;
            var data = Deserialize(File.ReadAllText(CurrentPresetPath));
            if (data == null) return;
            if (Math.Abs(MajorOf(data.version) - MajorOf(ModVersion)) >= 1)
            {
                LightLogger.LogWarning($"[PresetManager] current.lid 版本 {data.version} 与当前 {ModVersion} 主版本不符，忽略并删除");
                try { File.Delete(CurrentPresetPath); } catch { }
                return;
            }
            ApplyPresetData(data);
            LightLogger.Log("[PresetManager] 已应用 current.lid 恢复配置");
        }
        catch (Exception ex)
        {
            LightLogger.LogError("[PresetManager.ApplyCurrentPreset]", ex);
        }
    }
}
