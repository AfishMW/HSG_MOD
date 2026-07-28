using Interstellar.Messages.Messages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Interstellar.Messages.Variation;

public class IceCandMessage : IMessage
{
    public string Candidate { get; }
    public string SdpMid { get; }
    public int SdpMLineIndex { get; }
    public string UsernameFragment { get; }

    public IceCandMessage(string candidate, string sdpMid, int sdpMLineIndex, string usernameFragment)
    {
        this.Candidate = candidate;
        this.SdpMid = sdpMid;
        this.SdpMLineIndex = sdpMLineIndex;
        this.UsernameFragment = usernameFragment;
    }

    int IMessage.Serialize(Span<byte> bytes)
    {
        int length = 0;
        length += IMessage.SerializeTag(ref bytes, MessageTag.AddIceCand);
        length += IMessage.SerializeString(ref bytes, Candidate);
        length += IMessage.SerializeString(ref bytes, SdpMid);
        length += IMessage.SerializeInt32(ref bytes, SdpMLineIndex);
        length += IMessage.SerializeString(ref bytes, UsernameFragment);
        return length;
    }

    static public IceCandMessage DeserializeWithoutTag(ReadOnlySpan<byte> bytes, out int read)
    {
        read = 0;
        read += IMessage.DeserializeString(ref bytes, out var candidate);
        read += IMessage.DeserializeString(ref bytes, out var sdpMid);
        read += IMessage.DeserializeInt32(ref bytes, out var sdpMLineIndex);
        read += IMessage.DeserializeString(ref bytes, out var usernameFragment);
        return new(candidate, sdpMid, sdpMLineIndex, usernameFragment);
    }
}
