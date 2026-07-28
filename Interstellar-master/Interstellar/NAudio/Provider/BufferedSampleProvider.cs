using NAudio.Utils;
using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Interstellar.NAudio.Provider;

/// <summary>
/// サンプルを書き込めるBufferedProvider。
/// NAudioのBufferedWaveProviderをサンプル用に改変。
/// </summary>
internal class BufferedSampleProvider : ISampleProvider
{
    private CircularFloatBuffer circularBuffer;

    private readonly WaveFormat waveFormat;

    public bool ReadFully { get; set; }
    public int BufferLength { get; set; }

    public int BufferCutSize { get; set; } = int.MaxValue;
    public int BufferCutToSize { get; set; } = int.MaxValue;

    public TimeSpan BufferDuration
    {
        get
        {
            return TimeSpan.FromSeconds((double)BufferLength / (double)WaveFormat.AverageBytesPerSecond);
        }
        set
        {
            BufferLength = (int)(value.TotalSeconds * (double)WaveFormat.AverageBytesPerSecond);
        }
    }

    public bool DiscardOnBufferOverflow { get; set; }
    public int BufferedBytes
    {
        get
        {
            if (circularBuffer != null)
            {
                return circularBuffer.Count;
            }

            return 0;
        }
    }
    public TimeSpan BufferedDuration => TimeSpan.FromSeconds((double)BufferedBytes / (double)WaveFormat.AverageBytesPerSecond);
    public WaveFormat WaveFormat => waveFormat;
    public BufferedSampleProvider(WaveFormat waveFormat, int? bufferLength = null)
    {
        this.waveFormat = waveFormat;
        BufferLength = bufferLength ?? waveFormat.AverageBytesPerSecond * 5;
        ReadFully = true;
    }
    public void AddSamples(float[] buffer, int offset, int count)
    {
        if (circularBuffer == null)
        {
            circularBuffer = new CircularFloatBuffer(BufferLength);
        }
        if (circularBuffer.Write(buffer, offset, count) < count && !DiscardOnBufferOverflow)
        {
            throw new InvalidOperationException("Buffer full");
        }

        if(circularBuffer.Count > BufferCutSize && BufferCutSize > BufferCutToSize)
        {
            circularBuffer.Discard(circularBuffer.Count - BufferCutToSize);
        }
    }

    public int Read(float[] buffer, int offset, int count)
    {
        int lastLength = BufferedBytes;

        int num = 0;
        if (circularBuffer != null)
        {
            num = circularBuffer.Read(buffer, offset, count);
        }

        if (ReadFully && num < count)
        {
            Array.Clear(buffer, offset + num, count - num);
            num = count;
        }

        int currentLength = BufferedBytes;

        return num;
    }

    public void ClearBuffer()
    {
        if (circularBuffer != null)
        {
            circularBuffer.Reset();
        }
    }
}
