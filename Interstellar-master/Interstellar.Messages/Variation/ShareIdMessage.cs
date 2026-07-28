using Interstellar.Messages.Messages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Interstellar.Messages.Variation;

/// <summary>
/// 自身のIDを通知するサーバーメッセージ。
/// </summary>
public class ShareIdMessage : IMessage
{
    public int Id;

    public ShareIdMessage(int id)
    {
        Id = id;
    }

    int IMessage.Serialize(Span<byte> bytes)
    {
        int length = 0;
        length += IMessage.SerializeTag(ref bytes, MessageTag.ShareId);
        length += IMessage.SerializeInt32(ref bytes, Id);
        return length;
    }

    static public ShareIdMessage DeserializeWithoutTag(ReadOnlySpan<byte> bytes, out int read)
    {
        read = 0;
        read += IMessage.DeserializeInt32(ref bytes, out var id);
        return new(id);
    }
}