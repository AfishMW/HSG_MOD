using Interstellar.Messages.Messages;
using System;
using System.Collections.Generic;
using System.Data.SqlTypes;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Interstellar.Messages.Variation;

public class SdpAnswerMessage : IMessage
{
    public string Sdp { get; }

    public SdpAnswerMessage(string sdp)
    {
        Sdp = sdp;
    }

    int IMessage.Serialize(Span<byte> bytes)
    {
        int length = 0;
        length += IMessage.SerializeTag(ref bytes, MessageTag.SdpAnswer);
        length += IMessage.SerializeStringCompress(ref bytes, Sdp);
        return length;
    }

    static public SdpAnswerMessage DeserializeWithoutTag(ReadOnlySpan<byte> bytes, out int read)
    {
        read = 0;
        read += IMessage.DeserializeStringCompress(ref bytes, out var sdp);
        return new(sdp);
    }
}
