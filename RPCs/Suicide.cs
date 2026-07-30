using LightInDark.Core;
using LightInDark.Events;
using Reactor.Networking.Attributes;
namespace LightInDark.RPCs
{
    public partial class RPC
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="__pc__">无用参数，请传任意玩家</param>
        /// <param name="needLog"></param>
        /// <param name="state"></param>
        [MethodRpc((uint)RpcCalls.Suicide)]
        public static void Suicide(PlayerControl __pc__,bool needLog = true,string state = "suicide")
        {
            var MyPlayer = PlayerControl.LocalPlayer;
            if (MyPlayer == null || MyPlayer.Data.IsDead) return;
            MyPlayer.RpcMurderPlayer(MyPlayer, true);
            var ev = new PlayerSuicideEvent { Player = MyPlayer,Reason = state,NeedLog = needLog};
            EventSystem.RunEvent(ev);
            if (needLog) LightLogger.Log($"Player {MyPlayer.name} suicide. state:{state}");
            
        }
    }
}
