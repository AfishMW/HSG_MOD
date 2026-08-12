using LightInDark.Core;
using LightInDark.Events;
using LightInDark.Game;
using LightInDark.Roles;
using System;
using UnityEngine;

namespace LightInDark.RPCs
{
    /// <summary>
    /// RPC 定义文件。使用 [LidRPC] 属性，定义即用。
    /// </summary>
    public static class RpcDefinitions
    {
        // ============ 角色同步 ============

        [LidRPC]
        public static void SetRole(byte playerId, int roleId, int[] arguments)
        {
            var definedRole = RoleRegistry.GetById(roleId);
            if (definedRole == null) { LightLogger.LogWarning($"[RPC] 未知角色Id: {roleId}"); return; }
            var gamePlayer = Game.GameManager.Instance.GetPlayer(playerId);
            if (gamePlayer == null) return;
            gamePlayer.SetRoleLocal(definedRole, arguments);
            Game.LightPlayerDataManager.SetRole(playerId, definedRole.Name);
            EventTriggers.OnPlayerRoleSet(gamePlayer.Control, gamePlayer.Role);
        }

        // ============ 玩家操作 ============

        [LidRPC]
        public static void Suicide(PlayerControl player, bool needLog = true, string state = "suicide", PlayerState playerState = PlayerState.Suicide)
        {
            if (player == null || player.Data.IsDead) return;
            player.RpcMurderPlayer(player, true);
            EventTriggers.OnPlayerSuicide(player, state, playerState, needLog);
            LightPlayerDataManager.SetDeath(player.PlayerId, playerState, player.PlayerId, LightPlayerDataManager.CurrentMeetingNumber);
            if (needLog) LightLogger.Log($"[RPC] {player.name} suicide. state:{state}");
        }

        [LidRPC]
        public static void MurderPlayer(PlayerControl killer, PlayerControl victim, PlayerState state = PlayerState.BeKilled)
        {
            if (killer == null || victim == null) return;
            killer.RpcMurderPlayer(victim, true);
            EventTriggers.OnPlayerMurder(killer, victim, state);
            LightPlayerDataManager.SetDeath(victim.PlayerId, state, killer.PlayerId, LightPlayerDataManager.CurrentMeetingNumber);
        }

        /// <summary>恢复玩家（取消死亡状态）</summary>
        [LidRPC(OnlyHost = true)]
        public static void RevivePlayer(byte playerId)
        {
            foreach (var pc in PlayerControl.AllPlayerControls)
                if (pc.PlayerId == playerId && pc.Data.IsDead)
                {
                    pc.Revive();
                    EventTriggers.OnPlayerRevive(pc);
                    LightLogger.Log($"[RPC] {pc.name} 已复活");
                    break;
                }
        }

        /// <summary>设置玩家可见性</summary>
        [LidRPC]
        public static void SetPlayerInvisible(byte playerId, bool invisible)
        {
            foreach (var pc in PlayerControl.AllPlayerControls)
                if (pc.PlayerId == playerId)
                {
                    pc.SetInvisibility(invisible);
                    break;
                }
        }

        /// <summary>传送玩家到指定位置</summary>
        [LidRPC(OnlyHost = true)]
        public static void TeleportPlayer(byte playerId, Vector2 position)
        {
            foreach (var pc in PlayerControl.AllPlayerControls)
                if (pc.PlayerId == playerId)
                {
                    var pos = new UnityEngine.Vector3(position.x, position.y, pc.transform.position.z);
                    pc.NetTransform.SnapTo(pos);
                    LightLogger.Log($"[RPC] {pc.name} 传送到 {position}");
                    break;
                }
        }

        // ============ 会议 ============

        [LidRPC]
        public static void StartMeeting(byte reporterId, byte reportedId)
        {
            if (MeetingHud.Instance != null) return;
            PlayerControl reporter = null;
            foreach (var pc in PlayerControl.AllPlayerControls)
                if (pc.PlayerId == reporterId) { reporter = pc; break; }

            if (reporter == null) return;

            NetworkedPlayerInfo reportedBody = reportedId == byte.MaxValue ? null : null; // TODO
            reporter.RpcStartMeeting(reportedBody);
            LightLogger.Log($"[RPC] 会议开始: reporter={reporter.name}");
        }

        [LidRPC]
        public static void RpcStartMeeting()
        {
            PlayerControl.LocalPlayer?.RpcStartMeeting(null);
        }

        [LidRPC(OnlyHost = true)]
        public static void ForceEndMeeting()
        {
            if (MeetingHud.Instance == null) return;
            // 简单关闭会议
            MeetingHud.Instance.gameObject.SetActive(false);
            LightLogger.Log("[RPC] 强制结束会议");
        }

        [LidRPC(OnlyHost = true)]
        public static void BreakEmergencyButton()
        {
            if (ShipStatus.Instance != null)
            {
                ShipStatus.Instance.BreakEmergencyButton();
                EventTriggers.OnEmergencyButtonBroken();
            }
        }

        // ============ 聊天 ============

        /// <summary>免费聊天显示状态变更（由主插件订阅实现）</summary>
        public static event Action<bool> OnFreeChatStateChanged;

        [LidRPC]
        public static void ShowChat()
        {
            OnFreeChatStateChanged?.Invoke(true);
        }

        [LidRPC]
        public static void HideChat()
        {
            OnFreeChatStateChanged?.Invoke(false);
        }

        [LidRPC]
        public static void SendChatMessage(byte playerId, string message)
        {
            foreach (var pc in PlayerControl.AllPlayerControls)
                if (pc.PlayerId == playerId)
                {
                    HudManager.Instance?.Chat?.AddChat(pc, message, false);
                    EventTriggers.OnChatMessage(pc, message);
                    break;
                }
        }

        // ============ 踢出 ============

        [LidRPC(OnlyHost = true)]
        public static void KickPlayer(byte playerId)
        {
            foreach (var pc in PlayerControl.AllPlayerControls)
                if (pc.PlayerId == playerId)
                {
                    var client = Utilities.AmongUsEdited.GetClient(pc);
                    if (client != null) AmongUsClient.Instance.KickPlayer(client.Id, false);
                    break;
                }
        }

        [LidRPC]
        public static void SetKickReason(byte targetPlayerId, string reason)
        {
            if (PlayerControl.LocalPlayer?.PlayerId == targetPlayerId)
            {
                Utilities.AmongUsEdited.KickManager.kickReason = reason;
                Utilities.AmongUsEdited.KickManager.kickReasonWaitUntil = Time.realtimeSinceStartup + 30f;
                Utilities.AmongUsEdited.KickManager.kickReasonConsumeUntil = 0f;
            }
        }

        [LidRPC(OnlyHost = true)]
        public static void KickPlayerWithReason(byte playerId, string reason)
        {
            foreach (var pc in PlayerControl.AllPlayerControls)
                if (pc.PlayerId == playerId)
                {
                    if (PlayerControl.LocalPlayer.PlayerId == playerId)
                    {
                        Utilities.AmongUsEdited.KickManager.kickReason = reason;
                        Utilities.AmongUsEdited.KickManager.kickReasonConsumeUntil = Time.realtimeSinceStartup + 5f;
                    }
                    var client = Utilities.AmongUsEdited.GetClient(pc);
                    if (client != null) AmongUsClient.Instance.KickPlayer(client.Id, false);
                    break;
                }
        }
    }

    // =====================================================================
    // 可复用的 static 方法
    // =====================================================================

    /// <summary>
    /// 游戏操作工具。提供常用操作的静态方法。
    /// </summary>
    public static class GameActions
    {
        /// <summary>分配角色给玩家（同步）</summary>
        public static void AssignRole(Game.Player player, DefinedRole role)
            => player.SetRole(role, role.DefaultArguments);

        /// <summary>让玩家自杀</summary>
        public static void KillSelf(PlayerControl player, string reason = "suicide")
            => RpcDefinitions.Suicide(player, true, reason);

        /// <summary>击杀玩家</summary>
        public static void Murder(PlayerControl killer, PlayerControl victim)
            => RpcDefinitions.MurderPlayer(killer, victim);

        /// <summary>复活玩家（仅房主）</summary>
        public static void Revive(PlayerControl player)
            => RpcDefinitions.RevivePlayer(player.PlayerId);

        /// <summary>传送玩家</summary>
        public static void Teleport(PlayerControl player, Vector2 position)
            => RpcDefinitions.TeleportPlayer(player.PlayerId, position);

        /// <summary>隐身/显形</summary>
        public static void SetInvisible(PlayerControl player, bool invisible)
            => RpcDefinitions.SetPlayerInvisible(player.PlayerId, invisible);

        /// <summary>召开紧急会议</summary>
        public static void StartMeeting()
            => RpcDefinitions.RpcStartMeeting();

        /// <summary>强制结束会议</summary>
        public static void ForceEndMeeting()
            => RpcDefinitions.ForceEndMeeting();

        /// <summary>破坏紧急按钮</summary>
        public static void BreakEmergencyButton()
            => RpcDefinitions.BreakEmergencyButton();

        /// <summary>显示聊天</summary>
        public static void ShowChat() => RpcDefinitions.ShowChat();

        /// <summary>隐藏聊天</summary>
        public static void HideChat() => RpcDefinitions.HideChat();

        /// <summary>发送聊天消息</summary>
        public static void SendMessage(PlayerControl player, string message)
            => RpcDefinitions.SendChatMessage(player.PlayerId, message);

        /// <summary>踢出玩家</summary>
        public static void Kick(PlayerControl player, string reason = "")
        {
            if (!string.IsNullOrEmpty(reason))
                RpcDefinitions.SetKickReason(player.PlayerId, reason);
            RpcDefinitions.KickPlayer(player.PlayerId);
        }

        /// <summary>获取所有存活玩家</summary>
        public static System.Collections.Generic.IEnumerable<PlayerControl> AlivePlayers()
        {
            foreach (var pc in PlayerControl.AllPlayerControls)
                if (pc != null && !pc.Data.IsDead) yield return pc;
        }

        /// <summary>获取所有内鬼</summary>
        public static System.Collections.Generic.IEnumerable<PlayerControl> Impostors()
        {
            foreach (var pc in PlayerControl.AllPlayerControls)
                if (pc?.Data?.Role?.IsImpostor == true) yield return pc;
        }

        /// <summary>获取最近玩家</summary>
        public static PlayerControl GetClosestPlayer(PlayerControl source, float maxDistance = 2f)
        {
            PlayerControl closest = null;
            float closestDist = maxDistance;
            foreach (var pc in AlivePlayers())
            {
                if (pc == source) continue;
                float dist = Vector2.Distance(source.transform.position, pc.transform.position);
                if (dist < closestDist) { closestDist = dist; closest = pc; }
            }
            return closest;
        }

        /// <summary>获取本地玩家</summary>
        public static Game.Player LocalPlayer => Game.GameManager.Instance?.LocalPlayer;

        /// <summary>是否是房主</summary>
        public static bool IsHost => AmongUsClient.Instance?.AmHost ?? false;

        /// <summary>是否在游戏中</summary>
        public static bool InGame => AmongUsClient.Instance?.GameState == InnerNet.InnerNetClient.GameStates.Started;
    }
}
