using HarmonyLib;
using InnerNet;
using LightInDark.Core;
using UnityEngine;

namespace LightInDark.Events
{
    // =====================================================================
    //  游戏帧更新事件
    // =====================================================================

    /// <summary>HudManager.Update → GameUpdateEvent + GameHudUpdateEvent + GameLateUpdateEvent</summary>
    [HarmonyPatch(typeof(HudManager), nameof(HudManager.Update))]
    public static class GameUpdatePatch
    {
        public static void Postfix()
        {
            if (AmongUsClient.Instance?.GameState != InnerNetClient.GameStates.Started) return;
            EventTriggers.OnGameUpdate(Time.deltaTime);
            EventTriggers.OnGameHudUpdate(Time.deltaTime);
            EventTriggers.OnGameLateUpdate(Time.deltaTime);
        }
    }

    // =====================================================================
    //  HUD 激活状态
    // =====================================================================

    /// <summary>HudManager.SetHudActive → HudActiveChangeEvent</summary>
    [HarmonyPatch(typeof(HudManager), nameof(HudManager.SetHudActive),
        typeof(PlayerControl), typeof(RoleBehaviour), typeof(bool))]
    public static class HudActivePatch
    {
        public static void Postfix(bool isActive)
        {
            EventTriggers.OnHudActiveChange(isActive);
        }
    }

    // =====================================================================
    //  玩家移动 / 击杀
    // =====================================================================

    /// <summary>PlayerControl.FixedUpdate → PlayerMoveEvent</summary>
    [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.FixedUpdate))]
    public static class PlayerMovePatch
    {
        public static void Postfix(PlayerControl __instance)
        {
            if (__instance == null || __instance.Data == null || __instance.Data.IsDead) return;
            EventTriggers.OnPlayerMove(__instance, __instance.transform.position);
        }
    }

    /// <summary>PlayerControl.MurderPlayer → PlayerTryVanillaKillEvent (Prefix) + PlayerCheckCanKillEvent</summary>
    [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.MurderPlayer))]
    public static class PlayerMurderPatch
    {
        public static bool Prefix(PlayerControl __instance, PlayerControl target)
        {
            if (target == null) return true;

            // 检查能否击杀
            var canKill = EventTriggers.OnPlayerCheckCanKill(__instance, target);
            if (!canKill.CanKill)
            {
                EventTriggers.OnPlayerGuard(target, __instance);
                return false;
            }

            // 尝试击杀（可取消）
            if (!EventTriggers.OnPlayerTryVanillaKill(__instance, target))
            {
                return false;
            }

            return true;
        }

        public static void Postfix(PlayerControl __instance, PlayerControl target)
        {
            if (target == null) return;
            EventTriggers.OnPlayerTryMurder(__instance, target);
        }
    }

    // =====================================================================
    //  小游戏 / 控制台
    // =====================================================================

    /// <summary>Console.Use → PlayerBeginMinigameByConsoleEvent</summary>
    [HarmonyPatch(typeof(Console), nameof(Console.Use))]
    public static class ConsoleUsePatch
    {
        public static void Postfix(Console __instance)
        {
            var pc = PlayerControl.LocalPlayer;
            if (pc == null) return;
            EventTriggers.OnPlayerBeginMinigameByConsole(pc, __instance);
        }
    }

    /// <summary>DoorConsole.Use → PlayerBeginMinigameByDoorEvent</summary>
    [HarmonyPatch(typeof(DoorConsole), nameof(DoorConsole.Use))]
    public static class DoorConsoleUsePatch
    {
        public static void Postfix(DoorConsole __instance)
        {
            var pc = PlayerControl.LocalPlayer;
            if (pc == null) return;
            EventTriggers.OnPlayerBeginMinigameByDoor(pc, __instance);
        }
    }

    // =====================================================================
    //  任务
    // =====================================================================

    /// <summary>NormalPlayerTask.Initialize → PlayerGetTaskEvent</summary>
    [HarmonyPatch(typeof(NormalPlayerTask), nameof(NormalPlayerTask.Initialize))]
    public static class TaskInitializePatch
    {
        public static void Postfix(NormalPlayerTask __instance)
        {
            if (__instance.Owner == null) return;
            EventTriggers.OnPlayerGetTask(__instance.Owner, __instance);
        }
    }

    /// <summary>PlayerTask.Complete → PlayerTaskUpdateEvent + AllTasksCompleteEvent</summary>
    [HarmonyPatch(typeof(PlayerTask), nameof(PlayerTask.Complete))]
    public static class TaskCompleteUpdatePatch
    {
        public static void Postfix(PlayerTask __instance)
        {
            if (__instance.Owner == null) return;
            EventTriggers.OnTaskUpdate(__instance.Owner);

            // 检查是否所有任务完成
            int completed = 0, total = 0;
            if (__instance.Owner.Data?.Tasks != null)
            {
                total = __instance.Owner.Data.Tasks.Count;
                foreach (var t in __instance.Owner.Data.Tasks)
                    if (t != null && t.Complete) completed++;
            }
            if (total > 0 && completed >= total)
            {
                EventTriggers.OnAllTasksComplete(__instance.Owner);
            }
        }
    }

    /// <summary>NormalPlayerTask.OnRemove → PlayerTaskRemoveEvent</summary>
    [HarmonyPatch(typeof(NormalPlayerTask), nameof(NormalPlayerTask.OnRemove))]
    public static class TaskRemovePatch
    {
        public static void Postfix(NormalPlayerTask __instance)
        {
            if (__instance.Owner == null) return;
            EventTriggers.OnPlayerTaskRemove(__instance.Owner, __instance);
        }
    }

    // =====================================================================
    //  会议
    // =====================================================================

    /// <summary>PlayerControl.ReportDeadBody → MeetingTryStartEvent + CheckCanPushEmergencyButton + MeetingPreStartEvent / ReportDeadBodyEvent / CalledEmergencyMeetingEvent</summary>
    [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.ReportDeadBody))]
    public static class ReportDeadBodyEventPatch
    {
        public static bool Prefix(PlayerControl __instance, NetworkedPlayerInfo target)
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

        public static void Postfix(PlayerControl __instance, NetworkedPlayerInfo target)
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
    }

    /// <summary>MeetingHud.Start → MeetingDiscussionStartEvent</summary>
    [HarmonyPatch(typeof(MeetingHud), nameof(MeetingHud.Start))]
    public static class MeetingStartPatch
    {
        public static void Postfix()
        {
            EventTriggers.OnMeetingDiscussionStart();
        }
    }

    /// <summary>MeetingHud.Confirm → PlayerVoteCastEvent</summary>
    [HarmonyPatch(typeof(MeetingHud), nameof(MeetingHud.Confirm))]
    public static class MeetingVoteCastPatch
    {
        public static void Postfix(byte suspectStateIdx)
        {
            PlayerControl voteFor = null;
            if (suspectStateIdx < 254)
            {
                foreach (var pc in PlayerControl.AllPlayerControls)
                    if (pc.PlayerId == suspectStateIdx) { voteFor = pc; break; }
            }
            EventTriggers.OnPlayerVoteCast(PlayerControl.LocalPlayer, voteFor);
        }
    }

    /// <summary>MeetingHud.Close → MeetingTryEndEvent + MeetingPreEndEvent + MeetingEndEvent</summary>
    [HarmonyPatch(typeof(MeetingHud), nameof(MeetingHud.Close))]
    public static class MeetingClosePatch
    {
        public static bool Prefix(MeetingHud __instance)
        {
            if (!EventTriggers.OnMeetingTryEnd()) return false;
            EventTriggers.OnMeetingPreEnd();

            byte exiledId = byte.MaxValue;
            if (__instance.exiledPlayer != null)
                exiledId = __instance.exiledPlayer.PlayerId;

            EventTriggers.OnMeetingEnd(exiledId, false);
            return true;
        }
    }

    // =====================================================================
    //  放逐
    // =====================================================================

    /// <summary>ExileController.Begin → ExileScenePreStartEvent + ExileSceneStartEvent + FixExileTextEvent</summary>
    [HarmonyPatch(typeof(ExileController), nameof(ExileController.Begin))]
    public static class ExileBeginEventPatch
    {
        public static void Prefix()
        {
            EventTriggers.OnExileScenePreStart(new System.Collections.Generic.List<PlayerControl>());
        }

        public static void Postfix()
        {
            var exiled = new System.Collections.Generic.List<PlayerControl>();
            EventTriggers.OnExileSceneStart(exiled);
            EventTriggers.OnFixExileText(exiled);
        }
    }

    // =====================================================================
    //  管道 — 通过 Vent.Use 触发
    // =====================================================================

    /// <summary>Vent.Use → VentUsedEvent + PlayerVentEnterEvent / PlayerVentExitEvent</summary>
    [HarmonyPatch(typeof(Vent), nameof(Vent.Use))]
    public static class VentUsePatch
    {
        public static void Postfix(Vent __instance)
        {
            var pc = PlayerControl.LocalPlayer;
            if (pc == null) return;

            EventTriggers.OnVentUsed(pc, __instance.Id);

            // 根据当前状态判断是进入还是退出
            if (pc.inVent)
                EventTriggers.OnPlayerVentEnter(pc, __instance);
            else
                EventTriggers.OnPlayerVentExit(pc, __instance);
        }
    }

    // =====================================================================
    //  玩家视觉（每帧）
    // =====================================================================

    /// <summary>PlayerControl.FixedUpdate → PlayerUpdateVisibilityEvent + PlayerAlphaUpdateEvent + PlayerUpdateVentStateEvent + PlayerCheckPlayFootSoundEvent</summary>
    [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.FixedUpdate))]
    public static class PlayerVisualPatch
    {
        public static void Postfix(PlayerControl __instance)
        {
            if (__instance == null || __instance.Data == null) return;

            // 可见性更新
            var newVis = __instance.Data.IsDead
                ? PlayerUpdateVisibilityEvent.VisibilityLevel.SemiTransparent
                : PlayerUpdateVisibilityEvent.VisibilityLevel.Visible;
            EventTriggers.OnPlayerUpdateVisibility(__instance, newVis, PlayerUpdateVisibilityEvent.VisibilityLevel.Visible);

            // 透明度更新
            var rend = __instance.cosmetics?.currentBodySprite?.BodySprite;
            float alpha = rend != null ? rend.color.a : 1f;
            EventTriggers.OnPlayerAlphaUpdate(__instance, alpha);

            // 管道状态更新 + 脚步声检查（仅本地玩家）
            if (__instance == PlayerControl.LocalPlayer)
            {
                EventTriggers.OnPlayerUpdateVentState(__instance);
                EventTriggers.OnPlayerCheckPlayFootSound(__instance);
            }
        }
    }

    // =====================================================================
    //  破坏系统 — 通过 ShipStatus.OnDestroy / 电闸 / 反应堆等方法触发
    //  使用 ShipStatus.Start 期间记录 SystemTypes 变化
    // =====================================================================

    /// <summary>Planetmap.OnSystemUpdated → SabotageStartEvent / SabotageEndEvent</summary>
    [HarmonyPatch(typeof(ShipStatus), nameof(ShipStatus.OnEnable))]
    public static class ShipStatusEnablePatch
    {
        public static void Postfix()
        {
            // 在船加载时初始化，实际破坏事件由下面的方法触发
        }
    }

    /// <summary>PlayerControl.RpcCompleteTask → PlayerTaskCompleteLocalEvent (本地任务完成)</summary>
    [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.RpcCompleteTask))]
    public static class LocalTaskCompletePatch
    {
        public static void Postfix(PlayerControl __instance)
        {
            EventTriggers.OnTaskCompleteLocal(__instance);
        }
    }
}
