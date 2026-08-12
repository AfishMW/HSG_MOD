using System;
using System.IO;
using HarmonyLib;
using InnerNet;
using LightInDark.Core;
using LightInDark.Events;
using LightInDark.Game;
using LightInDark.Roles;

namespace Light.Patches;

[HarmonyPatch(typeof(AmongUsClient), nameof(AmongUsClient.OnGameEnd))]
public static class GameEndPatch
{
    public static void Postfix(AmongUsClient __instance, ref EndGameResult endGameResult)
    {
        try
        {
            // 判断胜负
            var reason = endGameResult?.GameOverReason;
            int reasonVal = reason.HasValue ? (int)reason.Value : -1;
            // 0-2 = Humans win (task, vote, disconnect), 3-6 = Impostor win (kill, vote, disconnect, sabotage)
            bool crewWin = reasonVal >= 0 && reasonVal <= 2;
            bool impWin = reasonVal >= 3 && reasonVal <= 6;

            LightPlayerDataManager.CrewmatesWin = crewWin;
            LightPlayerDataManager.ImpostorsWin = impWin;
            LightPlayerDataManager.WinReason = reason?.ToString() ?? "Unknown";

            // 房间号
            try
            {
                var gameId = AmongUsClient.Instance.GameId;
                LightPlayerDataManager.RoomCode = GameCode.IntToGameNameV2(gameId);
            }
            catch { LightPlayerDataManager.RoomCode = "Unknown"; }

            // 检测本地/练习模式
            LightPlayerDataManager.IsLocalMode = AmongUsClient.Instance.NetworkMode == NetworkModes.LocalGame;
            LightPlayerDataManager.IsPracticeMode = false; // AU 没有明确的练习模式标记

            // 触发 GameEndEvent
            EventTriggers.OnGameEnd(crewWin, impWin, LightPlayerDataManager.WinReason);

            // 触发胜利检查事件
            foreach (var pc in PlayerControl.AllPlayerControls)
            {
                EventTriggers.OnPlayerCheckWin(pc, LightPlayerDataManager.WinReason);
                EventTriggers.OnPlayerCheckExtraWin(pc, LightPlayerDataManager.WinReason);
                bool isWinner = (crewWin && !pc.Data.Role.IsImpostor) || (impWin && pc.Data.Role.IsImpostor);
                EventTriggers.OnPlayerBlockWin(pc, isWinner, LightPlayerDataManager.WinReason);
            }

            LightLogger.Log($"[GameEnd] 船员胜={crewWin}, 内鬼胜={impWin}, 原因={LightPlayerDataManager.WinReason}");

            // Autosave
            if (LightPlayerDataManager.AutoSaveEnabled)
            {
                SaveReplayToFile();
            }
        }
        catch (Exception ex)
        {
            LightLogger.LogWarning("[Light] GameEndPatch.Postfix NRE: " + ex.Message + "\n" + ex.StackTrace);
        }
    }

    /// <summary>保存复盘信息到文件</summary>
    public static void SaveReplayToFile()
    {
        try
        {
            var now = DateTime.Now;
            var roomDisplay = LightPlayerDataManager.IsLocalMode ? "本地模式"
                            : LightPlayerDataManager.IsPracticeMode ? "练习"
                            : LightPlayerDataManager.RoomCode;
            var fileName = $"{now:yyyy_MM_dd_HH_mm_ss}_{roomDisplay}.txt";

            string dir = Path.Combine(BepInEx.Paths.BepInExRootPath, "Replay");
            Directory.CreateDirectory(dir);
            string fullPath = Path.Combine(dir, fileName);

            var content = LightPlayerDataManager.BuildReplayText();
            File.WriteAllText(fullPath, content);
            LightLogger.Log($"[GameEnd] 复盘已保存: {fullPath}");
        }
        catch (Exception ex)
        {
            LightLogger.LogWarning($"[GameEnd] 保存复盘失败: {ex.Message}");
        }
    }
}
