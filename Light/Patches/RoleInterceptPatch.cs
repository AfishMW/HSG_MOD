using System.Linq;
using HarmonyLib;
using Hazel;
using InnerNet;
using LightInDark.Core;
using LightInDark.Events;
using LightInDark.Game;
using LightInDark.Roles;
using Light.Roles.Assignment;
using UnityEngine;

namespace Light.Patches
{
    /// <summary>
    /// 原版逻辑拦截 + 事件注入。
    /// 阻挡原版绝大多数逻辑，用 Harmony 注入新逻辑。
    /// </summary>

    // =====================================================================
    // 游戏流程
    // =====================================================================

    [HarmonyPatch(typeof(GameManager), nameof(GameManager.StartGame))]
    public static class GameManagerStartPatch
    {
        public static void Postfix(GameManager __instance)
        {
            try
            {
                __instance.ShouldCheckForGameEnd = false;
                EventSystem.RunEvent(new GameStartEvent
                {
                    PlayerCount = PlayerControl.AllPlayerControls.Count
                });
                LightLogger.Log("[Patch] 游戏开始，已禁用原版结束检查");
            }
            catch (System.Exception ex)
            {
                LightLogger.LogWarning("[Light] GameManagerStartPatch.Postfix NRE: " + ex.Message + "\n" + ex.StackTrace);
            }
        }
    }

    [HarmonyPatch(typeof(GameManager), nameof(GameManager.CheckTaskCompletion))]
    public static class BlockTaskCompletionPatch
    {
        public static bool Prefix(ref bool __result)
        {
            try
            {
                __result = false;
                return false;
            }
            catch (System.Exception ex)
            {
                LightLogger.LogWarning("[Light] BlockTaskCompletionPatch.Prefix NRE: " + ex.Message + "\n" + ex.StackTrace);
                return false;
            }
        }
    }

    [HarmonyPatch(typeof(GameManager), nameof(GameManager.CheckEndGameViaTasks))]
    public static class BlockEndGameViaTasksPatch
    {
        public static bool Prefix(ref bool __result)
        {
            try
            {
                var ev = new GameTryEndEvent { CrewmatesWin = true, Reason = "task" };
                EventSystem.RunEvent(ev);
                if (ev.IsCanceled) { __result = false; return false; }
                __result = ev.CrewmatesWin && !ev.ImpostorsWin;
                return false;
            }
            catch (System.Exception ex)
            {
                LightLogger.LogWarning("[Light] BlockEndGameViaTasksPatch.Prefix NRE: " + ex.Message + "\n" + ex.StackTrace);
                return false;
            }
        }
    }

    [HarmonyPatch(typeof(LogicGameFlowNormal), nameof(LogicGameFlowNormal.IsGameOverDueToDeath))]
    public static class BlockGameOverDueToDeathPatch
    {
        public static bool Prefix(ref bool __result)
        {
            try
            {
                __result = false;
                return false;
            }
            catch (System.Exception ex)
            {
                LightLogger.LogWarning("[Light] BlockGameOverDueToDeathPatch.Prefix NRE: " + ex.Message + "\n" + ex.StackTrace);
                return false;
            }
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
            try
            {
                LightLogger.Log("[Patch] 拦截 SelectRoles，开始自定义分配");
                LightInDark.Game.GameManager.Instance.Initialize();

                // 复制到系统 List 并洗牌（AllPlayerControls 不支持 System.Linq）
                var players = new System.Collections.Generic.List<PlayerControl>();
                foreach (var pc in PlayerControl.AllPlayerControls) players.Add(pc);
                for (int i = players.Count - 1; i > 0; i--)
                {
                    int j = UnityEngine.Random.Range(0, i + 1);
                    (players[i], players[j]) = (players[j], players[i]);
                }

                int impNum = Mathf.Clamp(GameOptionsManager.Instance.CurrentGameOptions.NumImpostors, 1, Mathf.Max(1, players.Count - 1));
                var impostors = players.Take(impNum).Select(p => p.PlayerId).ToList();
                var others = players.Skip(impNum).Select(p => p.PlayerId).ToList();

                new StandardRoleAllocator().Assign(impostors, others);
                return false;
            }
            catch (System.Exception ex)
            {
                LightLogger.LogWarning("[Light] RoleSelectPatch.Prefix NRE: " + ex.Message + "\n" + ex.StackTrace);
                return false;
            }
        }
    }

    [HarmonyPatch(typeof(RoleManager), nameof(RoleManager.AssignRoleOnDeath))]
    public static class BlockGhostRolePatch
    {
        public static bool Prefix()
        {
            try
            {
                return false;
            }
            catch (System.Exception ex)
            {
                LightLogger.LogWarning("[Light] BlockGhostRolePatch.Prefix NRE: " + ex.Message + "\n" + ex.StackTrace);
                return false;
            }
        }
    }

    [HarmonyPatch(typeof(RoleBehaviour), nameof(RoleBehaviour.Initialize))]
    public static class BlockRoleInitializePatch
    {
        public static bool Prefix(RoleBehaviour __instance)
        {
            try
            {
                if (__instance.Player == null) return true;
                if (HudManager.Instance != null && HudManager.Instance.AbilityButton != null)
                    HudManager.Instance.AbilityButton.gameObject.SetActive(false);
                return false;
            }
            catch (System.Exception ex)
            {
                LightLogger.LogWarning("[Light] BlockRoleInitializePatch.Prefix NRE: " + ex.Message + "\n" + ex.StackTrace);
                return true;
            }
        }
    }

    [HarmonyPatch(typeof(RoleBehaviour), nameof(RoleBehaviour.InitializeAbilityButton))]
    public static class BlockAbilityButtonPatch
    {
        public static bool Prefix()
        {
            try
            {
                if (HudManager.Instance != null && HudManager.Instance.AbilityButton != null)
                    HudManager.Instance.AbilityButton.gameObject.SetActive(false);
                return false;
            }
            catch (System.Exception ex)
            {
                LightLogger.LogWarning("[Light] BlockAbilityButtonPatch.Prefix NRE: " + ex.Message + "\n" + ex.StackTrace);
                return false;
            }
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
            try
            {
                EventSystem.RunEvent(new PlayerDeathEvent
                {
                    Player = __instance,
                    Reason = reason
                });
            }
            catch (System.Exception ex)
            {
                LightLogger.LogWarning("[Light] PlayerDeathPatch.Postfix NRE: " + ex.Message + "\n" + ex.StackTrace);
            }
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
            try
            {
                int completed = 0, total = 0;
                if (__instance.Data?.Tasks != null)
                {
                    total = __instance.Data.Tasks.Count;
                    foreach (var task in __instance.Data.Tasks)
                        if (task != null && task.Complete) completed++;
                }
                EventSystem.RunEvent(new PlayerTaskCompleteEvent
                {
                    Player = __instance,
                    CompletedTasks = completed,
                    TotalTasks = total
                });
            }
            catch (System.Exception ex)
            {
                LightLogger.LogWarning("[Light] TaskCompletePatch.Postfix NRE: " + ex.Message + "\n" + ex.StackTrace);
            }
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
            try
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
            catch (System.Exception ex)
            {
                LightLogger.LogWarning("[Light] ReportDeadBodyPatch.Postfix NRE: " + ex.Message + "\n" + ex.StackTrace);
            }
        }
    }

    [HarmonyPatch(typeof(MeetingHud), nameof(MeetingHud.Confirm))]
    public static class MeetingVotePatch
    {
        public static void Postfix(MeetingHud __instance, byte suspectStateIdx)
        {
            try
            {
                EventSystem.RunEvent(new PlayerVoteEvent
                {
                    Player = PlayerControl.LocalPlayer,
                    VotedForPlayerId = suspectStateIdx
                });
            }
            catch (System.Exception ex)
            {
                LightLogger.LogWarning("[Light] MeetingVotePatch.Postfix NRE: " + ex.Message + "\n" + ex.StackTrace);
            }
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
            try
            {
                if (data?.Character != null)
                {
                    EventSystem.RunEvent(new PlayerDisconnectEvent
                    {
                        Player = data.Character
                    });
                }
            }
            catch (System.Exception ex)
            {
                LightLogger.LogWarning("[Light] PlayerLeftPatch.Postfix NRE: " + ex.Message + "\n" + ex.StackTrace);
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
            try
            {
                // ExileController 的 exiled 属性可能在 Il2Cpp 中需要不同访问方式
                // 暂时只记录日志
                LightLogger.Log("[Patch] 放逐动画开始");
                EventSystem.RunEvent(new PlayerExileEvent { });
            }
            catch (System.Exception ex)
            {
                LightLogger.LogWarning("[Light] ExileBeginPatch.Postfix NRE: " + ex.Message + "\n" + ex.StackTrace);
            }
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
            try
            {
                EventSystem.RunEvent(new EmergencyButtonBrokenEvent());
            }
            catch (System.Exception ex)
            {
                LightLogger.LogWarning("[Light] EmergencyButtonBrokenPatch.Postfix NRE: " + ex.Message + "\n" + ex.StackTrace);
            }
        }
    }

    // =====================================================================
    // 破坏系统 — AU 方法签名不同，暂时跳过
    // =====================================================================
}
