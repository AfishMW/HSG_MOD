using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Interstellar.NAudio.Provider;

internal class StereoSampleProvider : ISampleProvider
{
    private readonly ISampleProvider sourceProvider;
    private float[] lastBuffer = null!;
    private int lastBufferCount = 0;
    private float[] tempBuffer = null!;
    public WaveFormat WaveFormat { get; }

    private float pan = 0.0f;
    private object panLock = new object();
    public float Pan { get { 
            lock (panLock) return pan;
        } set { 
            lock (panLock) pan = Math.Clamp(value, -1.0f, 1.0f);
        } } // -1.0 (左) から 1.0 (右)
    private int lastLDelay = 0;
    private int lastRDelay = 0;
    public float Volume { get; set; } = 1.0f;
    public StereoSampleProvider(ISampleProvider sourceProvider)
    {
        this.sourceProvider = sourceProvider;
        WaveFormat = WaveFormat.CreateIeeeFloatWaveFormat(sourceProvider.WaveFormat.SampleRate, 2);
    }

    public int Read(float[] buffer, int offset, int count)
    {
        if (tempBuffer == null || tempBuffer.Length < count / 2) tempBuffer = new float[count / 2];
        int tempLength = sourceProvider.Read(tempBuffer, 0, count / 2);

        int monoCount = count / 2;
        float pan = this.Pan;
        float lCoeff = pan < 0 ? 0 : pan;
        float rCoeff = pan < 0 ? -pan : 0;
        int lDelay = (int)(lCoeff * 50);
        int rDelay = (int)(rCoeff * 50);
        float lVol = (1.0f - lCoeff * 0.3f) * Volume;
        float rVol = (1.0f - rCoeff * 0.3f) * Volume;
        int lCount = monoCount - lDelay + lastLDelay;
        int rCount = monoCount - rDelay + lastRDelay;

        for (int i = 0; i < monoCount; i++)
        {
            int lIndex = i * lCount / monoCount;//lastBufferに含まれる遅延分を含めた添え字
            if (lIndex < lastLDelay)
                buffer[offset + i * 2] = lastBuffer[lastBufferCount - lastLDelay + lIndex] * lVol;
            else
                buffer[offset + i * 2] = tempBuffer[lIndex - lastLDelay] * lVol;

            int rIndex = i * rCount / monoCount;//lastBufferに含まれる遅延分を含めた添え字
            if (rIndex < lastRDelay)
                buffer[offset + i * 2 + 1] = lastBuffer[lastBufferCount - lastRDelay + rIndex] * rVol;
            else
                buffer[offset + i * 2 + 1] = tempBuffer[rIndex - lastRDelay] * rVol;
        }

        lastLDelay = lDelay;
        lastRDelay = rDelay;

        //配列を入れ替える
        var temp = lastBuffer;
        lastBuffer = tempBuffer;
        tempBuffer = temp;
        lastBufferCount = tempLength;

        return count;
    }
}