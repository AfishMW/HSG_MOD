using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Interstellar.Messages.Variation;

public class UpdateMuteStatusMessage : IMessage
{
    public bool Mute { get; }
    public UpdateMuteStatusMessage(bool mute)
    {
        this.Mute = mute;
    }

    int IMessage.Serialize(Span<byte> bytes)
    {
        int length = 0;
        length += IMessage.SerializeTag(ref bytes, MessageTag.UpdateMuteStatus);
        length += IMessage.SerializeBoolean(ref bytes, Mute);
        return length;
    }

    static public UpdateMuteStatusMessage DeserializeWithoutTag(ReadOnlySpan<byte> bytes, out int read)
    {
        read = 0;
        read += IMessage.DeserializeBoolean(ref bytes, out var mute);
        return new(mute);
    }
}
