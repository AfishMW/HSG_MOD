using System.Diagnostics.CodeAnalysis;

namespace Interstellar.Messages.Messages;

public class JoinMessage : IMessage
{
    public string RoomCode { get; }
    public string Region { get; }

    public JoinMessage(string roomCode, string region)
    {
        RoomCode = roomCode;
        Region = region;
    }

    int IMessage.Serialize(Span<byte> bytes)
    {
        int length = 0;
        length += IMessage.SerializeTag(ref bytes, MessageTag.Join);
        length += IMessage.SerializeString(ref bytes, RoomCode);
        length += IMessage.SerializeString(ref bytes, Region);
        return length;
    }

    static public JoinMessage DeserializeWithoutTag(ReadOnlySpan<byte> bytes, out int read)
    {
        read = 0;
        read += IMessage.DeserializeString(ref bytes, out var roomCode);
        read += IMessage.DeserializeString(ref bytes, out var region);
        return new(roomCode, region);
    }
}
