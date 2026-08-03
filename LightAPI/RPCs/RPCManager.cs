

using LightInDark.Roles;
using Reactor.Networking.Attributes;

namespace LightInDark.RPCs
{
    public enum RpcCalls : uint
    {
        Suicide = 67,
        MurederPlayer = 68,
        ShowChat = 69,
        HideChat = 70,
        SyncRole = 71,
        KickPlayer = 72,
        SetKickReason = 73,
        KickPlayerWithReason = 74
    }
    public partial class RPC
    {
        [MethodRpc((uint)RpcCalls.SyncRole)]
        public static void SyncRole(PlayerControl player, DefinedRole role)
        {
            if (player == null || role == null) return;

            var gamePlayer = Game.GameManager.Instance.GetPlayer(player.PlayerId);
            if (gamePlayer == null) return;

            // 客户端接收到RPC，本地切换角色
            gamePlayer.SetRoleLocal(role);
        }
    }
}
