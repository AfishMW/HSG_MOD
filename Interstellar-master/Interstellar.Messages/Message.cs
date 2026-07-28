using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Interstellar.Messages;

public interface IMessage
{
    internal int Serialize(Span<byte> bytes);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static protected int SerializeBytes(ref Span<byte> bytes, byte[] array)
    {
        var length = array?.Length ?? -1;
        SerializeInt32(ref bytes, length);
        if (length > 0)
        {
            array.CopyTo(bytes.Slice(0));
            bytes = bytes.Slice(length);
        }
        return 4 + length;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static protected int DeserializeBytes(ref ReadOnlySpan<byte> bytes, out byte[] value)
    {
        DeserializeInt32(ref bytes, out var strLength);
        if (strLength == -1)
        {
            value = null!;
            return 4;
        }
        else
        {
            value = bytes.Slice(0, strLength).ToArray();
            bytes = bytes.Slice(strLength);
            return 4 + strLength;
        }
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static protected int SerializeString(ref Span<byte> bytes, string str) => SerializeBytes(ref bytes, str == null ? [] : Encoding.UTF8.GetBytes(str));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static protected int DeserializeString(ref ReadOnlySpan<byte> bytes, out string value)
    {
        int returnVal = DeserializeBytes(ref bytes, out var array);
        value = Encoding.UTF8.GetString(array);
        return returnVal;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static protected int SerializeStringCompress(ref Span<byte> bytes, string str) => SerializeBytes(ref bytes, str == null ? [] : GZipMessage.CompressString(str));
    

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static protected int DeserializeStringCompress(ref ReadOnlySpan<byte> bytes, out string value)
    {
        int returnVal = DeserializeBytes(ref bytes, out var array);
        value = GZipMessage.DecompressString(array);
        return returnVal;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static protected int SerializeTag(ref Span<byte> bytes, MessageTag tag) => SerializeByte(ref bytes, (byte)tag);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static protected int DeserializeTag(ref ReadOnlySpan<byte> bytes, out MessageTag tag)
    {
        tag = (MessageTag)bytes[0];
        bytes = bytes.Slice(1);
        return 1;
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static protected int SerializeBoolean(ref Span<byte> bytes, bool value) => SerializeByte(ref bytes, (byte)(value ? 1 : 0));
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static protected int DeserializeBoolean(ref ReadOnlySpan<byte> bytes, out bool value)
    {
        var length = DeserializeByte(ref bytes, out var byteValue);
        value = byteValue != 0;
        return length;
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static protected int SerializeByte(ref Span<byte> bytes, byte value)
    {
        bytes[0] = value;
        bytes = bytes.Slice(1);
        return 1;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static protected int DeserializeByte(ref ReadOnlySpan<byte> bytes, out byte value)
    {
        value = bytes[0];
        bytes = bytes.Slice(1);
        return 1;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static protected int SerializeInt32(ref Span<byte> bytes, int value)
    {
        BitConverter.TryWriteBytes(bytes, value);
        bytes = bytes.Slice(4);
        return 4;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static protected int DeserializeInt32(ref ReadOnlySpan<byte> bytes, out int value)
    {
        value = BitConverter.ToInt32(bytes);
        bytes = bytes.Slice(4);
        return 4;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static protected int SerializeInt64(ref Span<byte> bytes, long value)
    {
        BitConverter.TryWriteBytes(bytes, value);
        bytes = bytes.Slice(8);
        return 8;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static protected int DeserializeInt64(ref ReadOnlySpan<byte> bytes, out long value)
    {
        value = BitConverter.ToInt64(bytes);
        bytes = bytes.Slice(8);
        return 8;
    }
}
