
using LightInDark.Events;
using Reactor.Networking.Attributes;

namespace LightInDark.RPCs
{
    public partial class RPC
    {
        [MethodRpc((uint)RpcCalls.MurederPlayer)]
        public static void MurederPlayer(PlayerControl killer,PlayerControl victim)
        {
            killer.RpcMurderPlayer(victim,true);
            var ev = new PlayerMurderEvent
            {
                Player = killer,
                Victim = victim
            };
            EventSystem.RunEvent(ev);
        }
    }
}
