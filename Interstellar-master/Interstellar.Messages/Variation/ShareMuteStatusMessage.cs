using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Interstellar.Messages.Variation;

public class ShareMuteStatusMessage : IMessage
{
    public byte ClientId { get; }
    public bool IsMute { get; }

    public ShareMuteStatusMessage(byte clientId, bool isMute)
    {
        this.ClientId = clientId;
        this.IsMute = isMute;
    }

    int IMessage.Serialize(Span<byte> bytes)
    {
        int length = 0;
        length += IMessage.SerializeTag(ref bytes, MessageTag.ShareMuteStatus);
        length += IMessage.SerializeByte(ref bytes, ClientId);
        length += IMessage.SerializeBoolean(ref bytes, IsMute);
        return length;
    }

    static public ShareMuteStatusMessage DeserializeWithoutTag(ReadOnlySpan<byte> bytes, out int read)
    {
        read = 0;
        read += IMessage.DeserializeByte(ref bytes, out var clientId);
        read += IMessage.DeserializeBoolean(ref bytes, out var isMute);
        return new(clientId, isMute);
    }
}
