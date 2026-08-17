using System;
using System.Collections.Generic;
using System.Linq;
using LightInDark.Core;
using LightInDark.Events;
using LightInDark.Game;

namespace LightInDark.Modifiers
{
    /// <summary>
    /// 修饰器管理器：为玩家添加/移除/查询修饰器。
    /// 每名玩家持有若干 <see cref="RuntimeModifier"/>，可叠加。
    /// </summary>
    public static class ModifierManager
    {
        private static readonly Dictionary<byte, List<RuntimeModifier>> _byPlayer = new();

        /// <summary>
        /// 为玩家添加修饰器。若已存在同名修饰器则不再重复添加。
        /// </summary>
        public static RuntimeModifier AddModifier(Player player, Modifier modifier)
        {
            try
            {
                if (player == null || modifier == null || player.Control == null) return null;

                if (!_byPlayer.TryGetValue(player.Control.PlayerId, out var list))
                {
                    list = new List<RuntimeModifier>();
                    _byPlayer[player.Control.PlayerId] = list;
                }

                if (list.Any(m => m.Definition.Key == modifier.Key))
                {
                    LightLogger.Log($"[Modifier] {player.Name} 已有修饰器 {modifier.Name}，忽略重复添加。");
                    return null;
                }

                var runtime = modifier.CreateInstance(player);
                list.Add(runtime);
                EventSystem.RunEvent(new ModifierAddedEvent(player.Control, modifier));
                LightLogger.Log($"[Modifier] {player.Name} 添加 {modifier.Name}");
                return runtime;
            }
            catch (Exception ex)
            {
                LightLogger.LogError("ModifierManager.AddModifier", ex);
                return null;
            }
        }

        /// <summary>按修饰器 Key 为玩家移除修饰器。</summary>
        public static bool RemoveModifier(Player player, string modifierKey)
        {
            try
            {
                if (player == null || player.Control == null) return false;
                if (!_byPlayer.TryGetValue(player.Control.PlayerId, out var list)) return false;

                var target = list.FirstOrDefault(m => m.Definition.Key == modifierKey);
                if (target == null) return false;

                list.Remove(target);
                target.Remove();
                EventSystem.RunEvent(new ModifierRemovedEvent(player.Control, target.Definition));
                LightLogger.Log($"[Modifier] {player.Name} 移除 {target.Definition.Name}");
                return true;
            }
            catch (Exception ex)
            {
                LightLogger.LogError("ModifierManager.RemoveModifier", ex);
                return false;
            }
        }

        /// <summary>获取玩家身上的所有修饰器。</summary>
        public static IReadOnlyList<RuntimeModifier> GetModifiers(Player player)
        {
            try
            {
                if (player?.Control == null) return Array.Empty<RuntimeModifier>();
                return _byPlayer.TryGetValue(player.Control.PlayerId, out var list)
                    ? (IReadOnlyList<RuntimeModifier>)list
                    : Array.Empty<RuntimeModifier>();
            }
            catch (Exception ex)
            {
                LightLogger.LogError("ModifierManager.GetModifiers", ex);
                return Array.Empty<RuntimeModifier>();
            }
        }

        /// <summary>玩家是否拥有指定 Key 的修饰器。</summary>
        public static bool HasModifier(Player player, string modifierKey)
            => GetModifiers(player).Any(m => m.Definition.Key == modifierKey);

        /// <summary>新一轮开始时清空所有修饰器。</summary>
        public static void ClearAll()
        {
            try
            {
                foreach (var list in _byPlayer.Values)
                {
                    foreach (var m in list)
                    {
                        try { m.Remove(); } catch { }
                    }
                }
                _byPlayer.Clear();
            }
            catch (Exception ex)
            {
                LightLogger.LogError("ModifierManager.ClearAll", ex);
            }
        }
    }
}
