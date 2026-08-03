using LightInDark.Core;
using LightInDark.Events;
using Reactor.Networking.Attributes;
using System.Linq;
using UnityEngine;
using static LightInDark.Utilities.AmongUsEdited;
namespace LightInDark.RPCs
{
    public partial class RPC
    {
        /// <summary>
        /// 请求踢出一个玩家。
        /// </summary>
        /// <param name="target"></param>
        /// <param name="playerId">将优先使用本参数。非特殊情况无需对此项传参，仅提供pc也行，我也不知道我留这玩意干啥。</param>
        [MethodRpc((uint)RpcCalls.KickPlayer)]
        public static void KickPlayer(PlayerControl target,byte playerId = 255)
        {
            if (!AmongUsClient.Instance.AmHost) return;
            var realTarget = playerId switch
            {
                255 => target,
                _ => PlayerControl.AllPlayerControls.ToArray().FirstOrDefault(p => p.PlayerId == playerId)
            };
            AmongUsClient.Instance.KickPlayer(Utilities.AmongUsEdited.GetClient(realTarget)!.Id,false);
        }

        [MethodRpc((uint)RpcCalls.SetKickReason)]
        public static void SetKickReason(ShipStatus _,byte targetPlayerId, string reason)
        {
            var local = PlayerControl.LocalPlayer;
            if (local != null && local.PlayerId == targetPlayerId)
            {
                KickManager.kickReason = reason;
                KickManager.kickReasonWaitUntil = Time.realtimeSinceStartup + 30f;
                KickManager.kickReasonConsumeUntil = 0f;
            }
        }

        [MethodRpc((uint)RpcCalls.KickPlayerWithReason)]
        public static void KickPlayerWithReason(PlayerControl target, string reason)
        {
            if (!AmongUsClient.Instance.AmHost) return;

            if (PlayerControl.LocalPlayer.PlayerId == target.PlayerId)
            {
                KickManager.kickReason = reason;
                KickManager.kickReasonConsumeUntil = Time.realtimeSinceStartup + 5f;
            }

            var client = GetClient(target);
            if (client != null)
                AmongUsClient.Instance.KickPlayer(client.Id, false);
        }
    }
}