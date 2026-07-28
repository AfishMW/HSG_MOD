using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Interstellar.Messages.Variation;

public class ShareProfileMessage : IMessage
{
    public byte AudioId { get; }
    public string PlayerName { get; }
    public byte PlayerId { get; }

    public ShareProfileMessage(byte audioId, string playerName, byte playerId)
    {
        this.AudioId = audioId;
        this.PlayerName = playerName;
        this.PlayerId = playerId;
    }

    int IMessage.Serialize(Span<byte> bytes)
    {
        int length = 0;
        length += IMessage.SerializeTag(ref bytes, MessageTag.ShareProfile);
        length += IMessage.SerializeByte(ref bytes, AudioId);
        length += IMessage.SerializeByte(ref bytes, PlayerId);
        length += IMessage.SerializeString(ref bytes, PlayerName);
        return length;
    }

    static public ShareProfileMessage DeserializeWithoutTag(ReadOnlySpan<byte> bytes, out int read)
    {
        read = 0;
        read += IMessage.DeserializeByte(ref bytes, out var audioId);
        read += IMessage.DeserializeByte(ref bytes, out var playerId);
        read += IMessage.DeserializeString(ref bytes, out var playerName);
        return new(audioId, playerName, playerId);
    }
}
