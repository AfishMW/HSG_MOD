using System;
using HarmonyLib;
using InnerNet;
using LightInDark.Core;
using UnityEngine;

namespace LightInDark.Events
{
    // =====================================================================
    //  深度游戏生命周期拦截（Nebula 风格）
    //
    //  在 Among Us 最底层的原版生命周期方法上拦截并注入自定义逻辑，
    //  同时触发 <see cref="LightInDark.Events"> 的深度自定义事件。
    //
    //  自定义逻辑通过 Unity 的协程注入，确保在原版协程推进的窗口内执行，
    //  避免在原版 Update / coroutine 中直接同步调用导致的开局卡死。
    // =====================================================================

    /// <summary>
    /// 开机动画 <see cref="HudManager.CoShowIntro"/> 拦截。
    /// CoShowIntro 原版流程：等 ShipStatus -> 实例化 IntroCutscene -> CoBegin -> 玩家落点 -> GameManager.StartGame。
    /// 我们在此注入：开始前触发 <see cref="IntroBeginEvent"/>。
    /// </summary>
    [HarmonyPatch(typeof(HudManager), nameof(HudManager.CoShowIntro))]
    public static class CoShowIntroPatch
    {
        public static void Prefix()
        {
            try
            {
                EventTriggers.OnIntroBegin();
            }
            catch (Exception ex)
            {
                LightLogger.LogError("[DeepLifecycle] CoShowIntroPatch.Prefix", ex);
            }
        }
    }

    /// <summary>
    /// 船只任务分配 <see cref="ShipStatus.Begin"/> 拦截。
    /// ShipStatus.Begin 在主机 CoStartGameHost 加载完成船只后调用，是最早的可判定“真正开局”的同步点。
    /// </summary>
    [HarmonyPatch(typeof(ShipStatus), nameof(ShipStatus.Begin))]
    public static class ShipBeginPatch
    {
        public static void Prefix()
        {
            try
            {
                LightLogger.Log("[DeepLifecycle] ShipStatus.Begin 触发（角色分配完成后）");
                EventTriggers.OnShipBegin();
            }
            catch (Exception ex)
            {
                LightLogger.LogError("[DeepLifecycle] ShipBeginPatch.Prefix", ex);
            }
        }
    }

    /// <summary>
    /// 玩家全部落位后（AmongUsClient.CoStartGame 末尾，所有玩家 moveable），
    /// 通过 <see cref="ShipStatus.Begin"/> 之后由本补丁触发玩家生成完毕事件。
    /// </summary>
    [HarmonyPatch(typeof(ShipStatus), nameof(ShipStatus.Begin))]
    public static class PlayersSpawnedPatch
    {
        public static void Postfix()
        {
            try
            {
                if (!AmongUsClient.Instance?.AmHost ?? true) return;
                EventTriggers.OnPlayersSpawned();
            }
            catch (Exception ex)
            {
                LightLogger.LogError("[DeepLifecycle] PlayersSpawnedPatch.Postfix", ex);
            }
        }
    }

    /// <summary>
    /// 开局广播（主机）<see cref="AmongUsClient.StartGame"/> 拦截。
    /// 在主机真正发出开局指令、即将进入 CoStartGame 前触发开局加载事件。
    /// </summary>
    [HarmonyPatch(typeof(AmongUsClient), nameof(AmongUsClient.StartGame))]
    public static class StartGameBroadcastPatch
    {
        public static void Prefix(AmongUsClient __instance)
        {
            try
            {
                if (!__instance.AmHost) return;
                int mapId = 0;
                if (GameOptionsManager.Instance?.CurrentGameOptions != null)
                    mapId = GameOptionsManager.Instance.CurrentGameOptions.MapId;
                EventTriggers.OnGameLoadingStart(mapId);
            }
            catch (Exception ex)
            {
                LightLogger.LogError("[DeepLifecycle] StartGameBroadcastPatch.Prefix", ex);
            }
        }
    }

    /// <summary>
    /// 本局正式结束 <see cref="GameManager.RpcEndGame"/> 拦截（原版唯一的结局触发点）。
    /// 原版 CheckEndCriteria / CheckEndGameViaTasks 都会调用它来广播一局结束。
    /// 在此注入自定义收尾事件，params 携带原版胜负原因。
    /// </summary>
    [HarmonyPatch(typeof(GameManager), nameof(GameManager.RpcEndGame))]
    public static class EndGamePatch
    {
        public static void Prefix(GameManager __instance, GameOverReason endReason, bool showAd)
        {
            try
            {
                string reason = endReason.ToString();
                bool impWin = IsImpostorWin(endReason);
                EventTriggers.OnGamePreEnd(!impWin, impWin, reason);
            }
            catch (Exception ex)
            {
                LightLogger.LogError("[DeepLifecycle] EndGamePatch.Prefix", ex);
            }
        }

        /// <summary>按原版 GameOverReason 判定内鬼是否获胜。</summary>
        private static bool IsImpostorWin(GameOverReason reason)
        {
            switch (reason)
            {
                case GameOverReason.ImpostorsByVote:
                case GameOverReason.ImpostorsByKill:
                case GameOverReason.ImpostorsBySabotage:
                case GameOverReason.ImpostorDisconnect:
                case GameOverReason.HideAndSeek_ImpostorsByKills:
                    return true;
                default:
                    return false;
            }
        }
    }
}
