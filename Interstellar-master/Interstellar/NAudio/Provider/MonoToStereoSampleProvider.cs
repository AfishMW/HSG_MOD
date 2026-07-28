using Interstellar.Messages;
using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Interstellar.NAudio.Provider;

internal class MonoToStereoSampleProvider : ISampleProvider
{
    static private readonly WaveFormat waveFormat = WaveFormat.CreateIeeeFloatWaveFormat(AudioHelpers.ClockRate, 2);
    public WaveFormat WaveFormat => waveFormat;
    private ISampleProvider sourceProvider;
    public MonoToStereoSampleProvider(ISampleProvider monoProvider)
    {
        this.sourceProvider = monoProvider;
    }
    
    public int Read(float[] buffer, int offset, int count)
    {
        sourceProvider.Read(buffer, offset, count / 2);
        for(int i = count / 2 - 1; i >= 0; i--)
        {
            buffer[offset + i * 2] = buffer[offset + i];
            buffer[offset + i * 2 + 1] = buffer[offset + i];
        }
        return count;
    }
}
