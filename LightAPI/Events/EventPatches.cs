using HarmonyLib;
using InnerNet;
using LightInDark.Core;
using System;
using UnityEngine;

namespace LightInDark.Events
{
    // =====================================================================
    //  游戏帧更新事件
    // =====================================================================

    [HarmonyPatch(typeof(HudManager), nameof(HudManager.Update))]
    public static class GameUpdatePatch
    {
        public static void Postfix()
        {
            try
            {
                if (AmongUsClient.Instance?.GameState != InnerNetClient.GameStates.Started) return;
                EventTriggers.OnGameUpdate(Time.deltaTime);
                EventTriggers.OnGameHudUpdate(Time.deltaTime);
                EventTriggers.OnGameLateUpdate(Time.deltaTime);
            }
            catch (Exception ex) { LightLogger.LogError("[EventPatch] GameUpdatePatch", ex); }
        }
    }

    // =====================================================================
    //  HUD 激活状态
    // =====================================================================

    [HarmonyPatch(typeof(HudManager), nameof(HudManager.SetHudActive),
        typeof(PlayerControl), typeof(RoleBehaviour), typeof(bool))]
    public static class HudActivePatch
    {
        public static void Postfix(bool isActive)
        {
            try { EventTriggers.OnHudActiveChange(isActive); }
            catch (Exception ex) { LightLogger.LogError("[EventPatch] HudActivePatch", ex); }
        }
    }

    // =====================================================================
    //  玩家移动 / 击杀
    // =====================================================================

    [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.FixedUpdate))]
    public static class PlayerMovePatch
    {
        public static void Postfix(PlayerControl __instance)
        {
            try
            {
                if (__instance == null || __instance.Data == null || __instance.Data.IsDead) return;
                EventTriggers.OnPlayerMove(__instance, __instance.transform.position);
            }
            catch (Exception ex) { LightLogger.LogError("[EventPatch] PlayerMovePatch", ex); }
        }
    }

    [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.MurderPlayer))]
    public static class PlayerMurderPatch
    {
        public static bool Prefix(PlayerControl __instance, PlayerControl target)
        {
            try
            {
                if (target == null) return true;
                var canKill = EventTriggers.OnPlayerCheckCanKill(__instance, target);
                if (!canKill.CanKill)
                {
                    EventTriggers.OnPlayerGuard(target, __instance);
                    return false;
                }
                if (!EventTriggers.OnPlayerTryVanillaKill(__instance, target))
                    return false;
                return true;
            }
            catch (Exception ex) { LightLogger.LogError("[EventPatch] PlayerMurderPatch.Prefix", ex); return true; }
        }

        public static void Postfix(PlayerControl __instance, PlayerControl target)
        {
            try
            {
                if (target == null) return;
                EventTriggers.OnPlayerTryMurder(__instance, target);
            }
            catch (Exception ex) { LightLogger.LogError("[EventPatch] PlayerMurderPatch.Postfix", ex); }
        }
    }

    // =====================================================================
    //  小游戏 / 控制台
    // =====================================================================

    [HarmonyPatch(typeof(Console), nameof(Console.Use))]
    public static class ConsoleUsePatch
    {
        public static void Postfix(Console __instance)
        {
            try
            {
                var pc = PlayerControl.LocalPlayer;
                if (pc == null) return;
                EventTriggers.OnPlayerBeginMinigameByConsole(pc, __instance);
            }
            catch (Exception ex) { LightLogger.LogError("[EventPatch] ConsoleUsePatch", ex); }
        }
    }

    [HarmonyPatch(typeof(DoorConsole), nameof(DoorConsole.Use))]
    public static class DoorConsoleUsePatch
    {
        public static void Postfix(DoorConsole __instance)
        {
            try
            {
                var pc = PlayerControl.LocalPlayer;
                if (pc == null) return;
                EventTriggers.OnPlayerBeginMinigameByDoor(pc, __instance);
            }
            catch (Exception ex) { LightLogger.LogError("[EventPatch] DoorConsoleUsePatch", ex); }
        }
    }

    // =====================================================================
    //  任务
    // =====================================================================

    [HarmonyPatch(typeof(NormalPlayerTask), nameof(NormalPlayerTask.Initialize))]
    public static class TaskInitializePatch
    {
        public static void Postfix(NormalPlayerTask __instance)
        {
            try
            {
                if (__instance.Owner == null) return;
                EventTriggers.OnPlayerGetTask(__instance.Owner, __instance);
            }
            catch (Exception ex) { LightLogger.LogError("[EventPatch] TaskInitializePatch", ex); }
        }
    }

    [HarmonyPatch(typeof(PlayerTask), nameof(PlayerTask.Complete))]
    public static class TaskCompleteUpdatePatch
    {
        public static void Postfix(PlayerTask __instance)
        {
            try
            {
                if (__instance.Owner == null) return;
                EventTriggers.OnTaskUpdate(__instance.Owner);

                int completed = 0, total = 0;
                if (__instance.Owner.Data?.Tasks != null)
                {
                    total = __instance.Owner.Data.Tasks.Count;
                    foreach (var t in __instance.Owner.Data.Tasks)
                        if (t != null && t.Complete) completed++;
                }
                if (total > 0 && completed >= total)
                    EventTriggers.OnAllTasksComplete(__instance.Owner);
            }
            catch (Exception ex) { LightLogger.LogError("[EventPatch] TaskCompleteUpdatePatch", ex); }
        }
    }

    // NormalPlayerTask.OnRemove 不存在于当前 AU 版本，已移除

    // =====================================================================
    //  会议
    // =====================================================================

    [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.ReportDeadBody))]
    public static class ReportDeadBodyEventPatch
    {
        public static bool Prefix(PlayerControl __instance, NetworkedPlayerInfo target)
        {
            try
            {
                bool isEmergency = target == null;
                if (isEmergency)
                {
                    var check = EventTriggers.OnCheckCanPushEmergencyButton();
                    if (!check.CanPushButton) return false;
                }
                if (!EventTriggers.OnMeetingTryStart(__instance, target, isEmergency))
                    return false;
                return true;
            }
            catch (Exception ex) { LightLogger.LogError("[EventPatch] ReportDeadBodyEventPatch.Prefix", ex); return true; }
        }

        public static void Postfix(PlayerControl __instance, NetworkedPlayerInfo target)
        {
            try
            {
                bool isEmergency = target == null;
                PlayerControl reported = null;
                if (target != null)
                {
                    foreach (var pc in PlayerControl.AllPlayerControls)
                        if (pc.PlayerId == target.PlayerId) { reported = pc; break; }
                }
                EventTriggers.OnMeetingPreStart(__instance, reported);
                if (isEmergency)
                    EventTriggers.OnCalledEmergencyMeeting(__instance);
                else
                    EventTriggers.OnReportDeadBody(__instance, reported);
            }
            catch (Exception ex) { LightLogger.LogError("[EventPatch] ReportDeadBodyEventPatch.Postfix", ex); }
        }
    }

    [HarmonyPatch(typeof(MeetingHud), nameof(MeetingHud.Start))]
    public static class MeetingStartPatch
    {
        public static void Postfix()
        {
            try { EventTriggers.OnMeetingDiscussionStart(); }
            catch (Exception ex) { LightLogger.LogError("[EventPatch] MeetingStartPatch", ex); }
        }
    }

    [HarmonyPatch(typeof(MeetingHud), nameof(MeetingHud.Confirm))]
    public static class MeetingVoteCastPatch
    {
        public static void Postfix(byte suspectStateIdx)
        {
            try
            {
                PlayerControl voteFor = null;
                if (suspectStateIdx < 254)
                {
                    foreach (var pc in PlayerControl.AllPlayerControls)
                        if (pc.PlayerId == suspectStateIdx) { voteFor = pc; break; }
                }
                EventTriggers.OnPlayerVoteCast(PlayerControl.LocalPlayer, voteFor);
            }
            catch (Exception ex) { LightLogger.LogError("[EventPatch] MeetingVoteCastPatch", ex); }
        }
    }

    [HarmonyPatch(typeof(MeetingHud), nameof(MeetingHud.Close))]
    public static class MeetingClosePatch
    {
        public static bool Prefix(MeetingHud __instance)
        {
            try
            {
                if (!EventTriggers.OnMeetingTryEnd()) return false;
                EventTriggers.OnMeetingPreEnd();
                byte exiledId = byte.MaxValue;
                if (__instance.exiledPlayer != null)
                    exiledId = __instance.exiledPlayer.PlayerId;
                EventTriggers.OnMeetingEnd(exiledId, false);
                return true;
            }
            catch (Exception ex) { LightLogger.LogError("[EventPatch] MeetingClosePatch", ex); return true; }
        }
    }

    // =====================================================================
    //  放逐
    // =====================================================================

    [HarmonyPatch(typeof(ExileController), nameof(ExileController.Begin))]
    public static class ExileBeginEventPatch
    {
        public static void Prefix()
        {
            try { EventTriggers.OnExileScenePreStart(new System.Collections.Generic.List<PlayerControl>()); }
            catch (Exception ex) { LightLogger.LogError("[EventPatch] ExileBeginEventPatch.Prefix", ex); }
        }

        public static void Postfix()
        {
            try
            {
                var exiled = new System.Collections.Generic.List<PlayerControl>();
                EventTriggers.OnExileSceneStart(exiled);
                EventTriggers.OnFixExileText(exiled);
            }
            catch (Exception ex) { LightLogger.LogError("[EventPatch] ExileBeginEventPatch.Postfix", ex); }
        }
    }

    // =====================================================================
    //  管道
    // =====================================================================

    [HarmonyPatch(typeof(Vent), nameof(Vent.Use))]
    public static class VentUsePatch
    {
        public static void Postfix(Vent __instance)
        {
            try
            {
                var pc = PlayerControl.LocalPlayer;
                if (pc == null) return;
                EventTriggers.OnVentUsed(pc, __instance.Id);
                if (pc.inVent)
                    EventTriggers.OnPlayerVentEnter(pc, __instance);
                else
                    EventTriggers.OnPlayerVentExit(pc, __instance);
            }
            catch (Exception ex) { LightLogger.LogError("[EventPatch] VentUsePatch", ex); }
        }
    }

    // =====================================================================
    //  玩家视觉（每帧）
    // =====================================================================

    [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.FixedUpdate))]
    public static class PlayerVisualPatch
    {
        public static void Postfix(PlayerControl __instance)
        {
            try
            {
                if (__instance == null || __instance.Data == null) return;

                var newVis = __instance.Data.IsDead
                    ? PlayerUpdateVisibilityEvent.VisibilityLevel.SemiTransparent
                    : PlayerUpdateVisibilityEvent.VisibilityLevel.Visible;
                EventTriggers.OnPlayerUpdateVisibility(__instance, newVis, PlayerUpdateVisibilityEvent.VisibilityLevel.Visible);

                var rend = __instance.cosmetics?.currentBodySprite?.BodySprite;
                float alpha = rend != null ? rend.color.a : 1f;
                EventTriggers.OnPlayerAlphaUpdate(__instance, alpha);

                if (__instance == PlayerControl.LocalPlayer)
                {
                    EventTriggers.OnPlayerUpdateVentState(__instance);
                    EventTriggers.OnPlayerCheckPlayFootSound(__instance);
                }
            }
            catch (Exception ex) { LightLogger.LogError("[EventPatch] PlayerVisualPatch", ex); }
        }
    }

    // =====================================================================
    //  本地任务完成
    // =====================================================================

    [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.RpcCompleteTask))]
    public static class LocalTaskCompletePatch
    {
        public static void Postfix(PlayerControl __instance)
        {
            try { EventTriggers.OnTaskCompleteLocal(__instance); }
            catch (Exception ex) { LightLogger.LogError("[EventPatch] LocalTaskCompletePatch", ex); }
        }
    }
}
