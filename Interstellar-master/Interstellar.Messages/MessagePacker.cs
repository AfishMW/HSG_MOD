using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Interstellar.Messages;

public static class MessagePacker
{
    private static byte[] Buffer = new byte[32768];
    public static Span<byte> PackMessage(IMessage message)
    {
        Buffer[0] = 1;
        int length = 1 + message.Serialize(Buffer.AsSpan(1));
        return Buffer.AsSpan(0, length);
    }

    public static Span<byte> PackMessages(IEnumerable<IMessage> message)
    {
        int num = 0;
        int length = 1;
        foreach (var msg in message)
        {
            length += msg.Serialize(Buffer.AsSpan(length));
            num++;
        }
        Buffer[0] = (byte)num;
        return Buffer.AsSpan(0, length);
    }

    public static void UnpackMessages(ReadOnlySpan<byte> bytes, IMessageProcessor messageProcessor)
    {
        int read = 0;
        int num = bytes[0];
        read += 1;
        for (int i = 0; i < num; i++)
        {
            MessageTag tag = (MessageTag)bytes[read];
            read += 1;
            int messageLength = messageProcessor.Process(tag, bytes.Slice(read));
            if(messageLength < 0) throw new InvalidDataException("Invalid tag detected (tag: " + tag +")");
            read += messageLength;
        }
    }
}
