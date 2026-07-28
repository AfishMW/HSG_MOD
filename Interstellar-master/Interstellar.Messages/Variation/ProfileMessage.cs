using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Interstellar.Messages.Variation;

public class ProfileMessage : IMessage
{
    public string PlayerName { get; }
    public byte PlayerId { get; }

    public ProfileMessage(string playerName, byte playerId)
    {
        this.PlayerName = playerName;
        this.PlayerId = playerId;
    }

    int IMessage.Serialize(Span<byte> bytes)
    {
        int length = 0;
        length += IMessage.SerializeTag(ref bytes, MessageTag.Profile);
        length += IMessage.SerializeByte(ref bytes, PlayerId);
        length += IMessage.SerializeString(ref bytes, PlayerName);
        return length;
    }

    static public ProfileMessage DeserializeWithoutTag(ReadOnlySpan<byte> bytes, out int read)
    {
        read = 0;
        read += IMessage.DeserializeByte(ref bytes, out var playerId);
        read += IMessage.DeserializeString(ref bytes, out var playerName);
        return new(playerName, playerId);
    }
}
