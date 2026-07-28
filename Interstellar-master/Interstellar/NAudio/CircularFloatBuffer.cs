using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Interstellar.NAudio;

/// <summary>
/// float型のリングバッファ。
/// NAudioが提供しているCircularBufferをfloat配列向けに改変。
/// </summary>
internal class CircularFloatBuffer
{
    private readonly float[] buffer;
    private readonly object lockObject;
    private int writePosition;
    private int readPosition;
    private int byteCount;
    public int MaxLength => buffer.Length;

    public int Count
    {
        get
        {
            lock (lockObject)
            {
                return byteCount;
            }
        }
    }

    public CircularFloatBuffer(int size)
    {
        buffer = new float[size];
        lockObject = new object();
    }

    public int Write(float[] data, int offset, int count)
    {
        lock (lockObject)
        {
            int num = 0;
            if (count > buffer.Length - byteCount)
            {
                count = buffer.Length - byteCount;
            }

            int num2 = Math.Min(buffer.Length - writePosition, count);
            Array.Copy(data, offset, buffer, writePosition, num2);
            writePosition += num2;
            writePosition %= buffer.Length;
            num += num2;
            if (num < count)
            {
                Array.Copy(data, offset + num, buffer, writePosition, count - num);
                writePosition += count - num;
                num = count;
            }

            byteCount += num;
            return num;
        }
    }

    public void Discard(int count)
    {
        lock (lockObject)
        {
            count = Math.Min(byteCount, count);
            readPosition = (readPosition + count) % buffer.Length;
            byteCount -= count;
        }
    }

    public int Read(float[] data, int offset, int count)
    {
        lock (lockObject)
        {
            if (count > byteCount)
            {
                count = byteCount;
            }

            int num = 0;
            int num2 = Math.Min(buffer.Length - readPosition, count);
            Buffer.BlockCopy(buffer, readPosition * 4, data, offset * 4, num2 * 4);
            num += num2;
            readPosition += num2;
            readPosition %= buffer.Length;
            if (num < count)
            {
                Buffer.BlockCopy(buffer, readPosition * 4, data, (offset + num) * 4, (count - num) * 4);
                readPosition += count - num;
                num = count;
            }

            byteCount -= num;
            return num;
        }
    }

    public void Reset()
    {
        lock (lockObject)
        {
            ResetInner();
        }
    }

    private void ResetInner()
    {
        byteCount = 0;
        readPosition = 0;
        writePosition = 0;
    }

    public void Advance(int count)
    {
        lock (lockObject)
        {
            if (count >= byteCount)
            {
                ResetInner();
                return;
            }

            byteCount -= count;
            readPosition += count;
            readPosition %= MaxLength;
        }
    }
}