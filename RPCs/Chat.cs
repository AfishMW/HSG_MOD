
using LightInDark.Events;
using Reactor.Networking.Attributes;
namespace LightInDark.RPCs
{
    public partial class RPC
    {
        [MethodRpc((uint)RpcCalls.ShowChat)]
        public static void ShowChat(ShipStatus ship)
        {
            LightInDark.ShowChatPatch.NeedShowFreeChat = true;
            EventSystem.RunEvent(new ShowChatEvent());
        }
        [MethodRpc((uint)RpcCalls.HideChat)]
        public static void HideChat(ShipStatus ship)
        {
            LightInDark.ShowChatPatch.NeedShowFreeChat = false;
            EventSystem.RunEvent(new HideChatEvent());
        }
    }
}
