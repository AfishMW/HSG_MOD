using Interstellar.Messages.Messages;
using System;
using System.Collections.Generic;
using System.Data.SqlTypes;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Interstellar.Messages.Variation;

public class SdpOfferMessage : IMessage
{
    public string Sdp { get; }
    public long Mask { get; }

    public SdpOfferMessage(string sdp, long mask)
    {
        Sdp = sdp;
        Mask = mask;
    }

    int IMessage.Serialize(Span<byte> bytes)
    {
        int length = 0;
        length += IMessage.SerializeTag(ref bytes, MessageTag.SdpOffer);
        length += IMessage.SerializeInt64(ref bytes, Mask);
        length += IMessage.SerializeStringCompress(ref bytes, Sdp);
        return length;
    }

    static public SdpOfferMessage DeserializeWithoutTag(ReadOnlySpan<byte> bytes, out int read)
    {
        read = 0;
        read += IMessage.DeserializeInt64(ref bytes, out var mask);
        read += IMessage.DeserializeStringCompress(ref bytes, out var sdp);
        return new(sdp, mask);
    }
}
