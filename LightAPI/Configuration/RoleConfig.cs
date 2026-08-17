using System;
using System.Collections.Generic;
using BepInEx.Configuration;
using LightInDark.Core;

namespace LightInDark.Configuration
{
    /// <summary>
    /// 职业配置项系统（目前写入 BepInEx .cfg，后续接入 UI）。
    ///
    /// 默认配置项（每个职业）：
    ///  - <c>MaxCount</c>（最大数量）
    ///  - <c>Chance</c>（分配概率 0-100）
    ///
    /// 可自定义配置项：通过 <see cref="GetInt"/>、<see cref="GetBool"/>、<see cref="GetFloat"/>、
    /// <see cref="GetString"/> 以（职业内部名 或 自定义键）绑定形参读取。
    /// </summary>
    public static class RoleConfig
    {
        private static ConfigFile _config;
        private const string Section = "Roles";
        private const int CountMax = 15;

        /// <summary>必须先在插件 Load() 中调用，传入 BepInEx 的 ConfigFile。</summary>
        public static void Initialize(ConfigFile config)
        {
            _config = config;
        }

        private static bool HasConfig => _config != null;

        /// <summary>读取职业最大数量配置（默认 = <paramref name="defaultCount"/>，范围 0-<see cref="CountMax"/>）。</summary>
        public static int GetRoleCount(string roleKey, int defaultCount)
        {
            var def = new ConfigDefinition(Section, $"MaxCount {roleKey}");
            return HasConfig ? _config.Bind(def, defaultCount, new ConfigDescription($"职业 {roleKey} 最大出现数量", new AcceptableValueRange<int>(0, CountMax))).Value
                             : defaultCount;
        }

        /// <summary>读取职业分配概率配置（默认 = <paramref name="defaultChance"/>，范围 0-100）。</summary>
        public static int GetRoleChance(string roleKey, int defaultChance)
        {
            var def = new ConfigDefinition(Section, $"Chance {roleKey}");
            return HasConfig ? _config.Bind(def, defaultChance, new ConfigDescription($"职业 {roleKey} 分配概率 %", new AcceptableValueRange<int>(0, 100))).Value
                             : defaultChance;
        }

        /// <summary>自定义 Int 配置项，以 roleKey.optionName 绑定形参读取。</summary>
        public static int GetInt(string roleKey, string optionName, int defaultValue, int min = 0, int max = 100)
        {
            var def = new ConfigDefinition(Section, $"{roleKey}.{optionName}");
            return HasConfig ? _config.Bind(def, defaultValue, new ConfigDescription($"{roleKey}.{optionName}", new AcceptableValueRange<int>(min, max))).Value
                             : defaultValue;
        }

        /// <summary>自定义 Bool 配置项。</summary>
        public static bool GetBool(string roleKey, string optionName, bool defaultValue)
        {
            var def = new ConfigDefinition(Section, $"{roleKey}.{optionName}");
            return HasConfig ? _config.Bind(def, defaultValue).Value : defaultValue;
        }

        /// <summary>自定义 Float 配置项。</summary>
        public static float GetFloat(string roleKey, string optionName, float defaultValue, float min = 0f, float max = 100f)
        {
            var def = new ConfigDefinition(Section, $"{roleKey}.{optionName}");
            return HasConfig ? _config.Bind(def, defaultValue, new ConfigDescription($"{roleKey}.{optionName}", new AcceptableValueRange<float>(min, max))).Value
                             : defaultValue;
        }

        /// <summary>自定义 String 配置项。</summary>
        public static string GetString(string roleKey, string optionName, string defaultValue)
        {
            var def = new ConfigDefinition(Section, $"{roleKey}.{optionName}");
            return HasConfig ? _config.Bind(def, defaultValue).Value : defaultValue;
        }
    }
}
