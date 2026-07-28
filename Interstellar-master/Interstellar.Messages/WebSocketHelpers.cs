using SIPSorcery.Net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WebSocketSharp;

namespace Interstellar.Messages;

static public class WebSocketHelpers
{
    static public void SendMessage(this WebSocket webSocket, MessageTag tag)
    {
        byte[] bytes = [1, (byte)tag ];
        webSocket.Send(bytes);
    }

    static public void SendMessage(this WebSocket webSocket, IMessage message)
    {
        var bytes = MessagePacker.PackMessage(message);
        webSocket.Send(bytes.ToArray());
    }

    static public void SendMessages(this WebSocket webSocket, params IEnumerable<IMessage> message)
    {
        var bytes = MessagePacker.PackMessages(message);
        webSocket.Send(bytes.ToArray());
    }

    static public RTCConfiguration GetRTCConfiguration()
    {
        return new RTCConfiguration
        {
            iceServers = new List<RTCIceServer> { new RTCIceServer { urls = "stun:stun.l.google.com:19302" } }
        };
    }
}
