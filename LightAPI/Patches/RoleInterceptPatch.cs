using HarmonyLib;
using Hazel;
using InnerNet;
using LightInDark.Core;
using LightInDark.Events;
using LightInDark.Game;
using LightInDark.Roles;
using UnityEngine;

namespace LightInDark.Patches
{
    /// <summary>
    /// 原版逻辑拦截 + 事件注入。
    /// 阻挡原版绝大多数逻辑，用 Harmony 注入新逻辑。
    /// 参考 Nebula GameScenarioPatch / ButtonsPatch / PlayerControlPatch / MeetingPatch。
    /// </summary>

    // =====================================================================
    // 游戏流程
    // =====================================================================

    [HarmonyPatch(typeof(GameManager), nameof(GameManager.StartGame))]
    public static class GameManagerStartPatch
    {
        public static void Postfix(GameManager __instance)
        {
            __instance.ShouldCheckForGameEnd = false;
            EventSystem.RunEvent(new GameStartEvent
            {
                PlayerCount = PlayerControl.AllPlayerControls.Count
            });
            LightLogger.Log("[Patch] 游戏开始，已禁用原版结束检查");
        }
    }

    [HarmonyPatch(typeof(GameManager), nameof(GameManager.CheckTaskCompletion))]
    public static class BlockTaskCompletionPatch
    {
        public static bool Prefix(ref bool __result)
        {
            __result = false;
            return false;
        }
    }

    [HarmonyPatch(typeof(GameManager), nameof(GameManager.CheckEndGameViaTasks))]
    public static class BlockEndGameViaTasksPatch
    {
        public static bool Prefix(ref bool __result)
        {
            var ev = new GameTryEndEvent { CrewmatesWin = true, Reason = "task" };
            EventSystem.RunEvent(ev);
            if (ev.IsCanceled) { __result = false; return false; }
            __result = ev.CrewmatesWin && !ev.ImpostorsWin;
            return false;
        }
    }

    [HarmonyPatch(typeof(LogicGameFlowNormal), nameof(LogicGameFlowNormal.IsGameOverDueToDeath))]
    public static class BlockGameOverDueToDeathPatch
    {
        public static bool Prefix(ref bool __result)
        {
            __result = false;
            return false;
        }
    }

    // =====================================================================
    // 角色分配
    // =====================================================================

    [HarmonyPatch(typeof(RoleManager), nameof(RoleManager.SelectRoles))]
    public static class RoleSelectPatch
    {
        public static bool Prefix(RoleManager __instance)
        {
            LightLogger.Log("[Patch] 拦截 SelectRoles");
            // 让原版跑，后续完全替换
            return true;
        }

        public static void Postfix()
        {
            foreach (var pc in PlayerControl.AllPlayerControls)
            {
                if (pc?.Data?.Role != null)
                    EventSystem.RunEvent(new RoleAssignedEvent(pc, pc.Data.Role.ToString()));
            }
        }
    }

    [HarmonyPatch(typeof(RoleManager), nameof(RoleManager.AssignRoleOnDeath))]
    public static class BlockGhostRolePatch
    {
        public static bool Prefix()
        {
            return false;
        }
    }

    [HarmonyPatch(typeof(RoleBehaviour), nameof(RoleBehaviour.Initialize))]
    public static class BlockRoleInitializePatch
    {
        public static bool Prefix(RoleBehaviour __instance)
        {
            if (__instance.Player == null) return true;
            if (HudManager.Instance != null && HudManager.Instance.AbilityButton != null)
                HudManager.Instance.AbilityButton.gameObject.SetActive(false);
            return false;
        }
    }

    [HarmonyPatch(typeof(RoleBehaviour), nameof(RoleBehaviour.InitializeAbilityButton))]
    public static class BlockAbilityButtonPatch
    {
        public static bool Prefix()
        {
            if (HudManager.Instance != null && HudManager.Instance.AbilityButton != null)
                HudManager.Instance.AbilityButton.gameObject.SetActive(false);
            return false;
        }
    }

    // =====================================================================
    // 玩家死亡/击杀
    // =====================================================================

    [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.Die))]
    public static class PlayerDeathPatch
    {
        public static void Postfix(PlayerControl __instance, DeathReason reason)
        {
            EventSystem.RunEvent(new PlayerDeathEvent(__instance, reason));
        }
    }

    // =====================================================================
    // 管道 — AU 方法签名不同，暂时跳过
    // =====================================================================

    // =====================================================================
    // 任务完成
    // =====================================================================

    [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.RpcCompleteTask))]
    public static class TaskCompletePatch
    {
        public static void Postfix(PlayerControl __instance)
        {
            int completed = 0, total = 0;
            if (__instance.Data?.Tasks != null)
            {
                total = __instance.Data.Tasks.Count;
                foreach (var task in __instance.Data.Tasks)
                    if (task != null && task.Complete) completed++;
            }
            EventSystem.RunEvent(new PlayerTaskCompleteEvent(__instance, completed, total));
        }
    }

    // =====================================================================
    // 会议
    // =====================================================================

    [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.ReportDeadBody))]
    public static class ReportDeadBodyPatch
    {
        public static void Postfix(PlayerControl __instance, NetworkedPlayerInfo target)
        {
            bool isEmergency = target == null;
            EventSystem.RunEvent(new MeetingStartEvent
            {
                Reporter = __instance,
                ReportedBody = target,
                IsEmergencyMeeting = isEmergency
            });
            LightLogger.Log($"[Patch] {(isEmergency ? "紧急会议" : "尸体报告")} by {__instance.name}");
        }
    }

    [HarmonyPatch(typeof(MeetingHud), nameof(MeetingHud.Confirm))]
    public static class MeetingVotePatch
    {
        public static void Postfix(MeetingHud __instance, byte suspectStateIdx)
        {
            EventSystem.RunEvent(new PlayerVoteEvent(PlayerControl.LocalPlayer, suspectStateIdx));
        }
    }

    // =====================================================================
    // 断线
    // =====================================================================

    [HarmonyPatch(typeof(AmongUsClient), nameof(AmongUsClient.OnPlayerLeft))]
    public static class PlayerLeftPatch
    {
        public static void Postfix(AmongUsClient __instance, ClientData data)
        {
            if (data?.Character != null)
            {
                EventSystem.RunEvent(new PlayerDisconnectEvent(data.Character));
            }
        }
    }

    // =====================================================================
    // 放逐
    // =====================================================================

    [HarmonyPatch(typeof(ExileController), nameof(ExileController.Begin))]
    public static class ExileBeginPatch
    {
        public static void Postfix(ExileController __instance)
        {
            // ExileController 的 exiled 属性可能在 Il2Cpp 中需要不同访问方式
            // 暂时只记录日志
            LightLogger.Log("[Patch] 放逐动画开始");
            EventSystem.RunEvent(new PlayerExileEvent(null));
        }
    }

    // =====================================================================
    // 紧急按钮
    // =====================================================================

    [HarmonyPatch(typeof(ShipStatus), nameof(ShipStatus.BreakEmergencyButton))]
    public static class EmergencyButtonBrokenPatch
    {
        public static void Postfix()
        {
            EventSystem.RunEvent(new EmergencyButtonBrokenEvent());
        }
    }

    // =====================================================================
    // 破坏系统 — AU 方法签名不同，暂时跳过
    // =====================================================================
}
