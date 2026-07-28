using Interstellar.Messages.Messages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Interstellar.Messages.Variation;

public class NoticeDisconnectMessage : IMessage
{
    public int ClientId { get; }

    public NoticeDisconnectMessage(int clientId)
    {
        this.ClientId = clientId;
    }

    int IMessage.Serialize(Span<byte> bytes)
    {
        int length = 0;
        length += IMessage.SerializeTag(ref bytes, MessageTag.NoticeDisconnect);
        length += IMessage.SerializeInt32(ref bytes, ClientId);
        return length;
    }

    static public NoticeDisconnectMessage DeserializeWithoutTag(ReadOnlySpan<byte> bytes, out int read)
    {
        read = 0;
        read += IMessage.DeserializeInt32(ref bytes, out var clientId);
        return new(clientId);
    }
}
