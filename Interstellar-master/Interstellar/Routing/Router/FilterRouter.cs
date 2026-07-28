using Interstellar.Messages;
using Interstellar.NAudio.Provider;
using NAudio.Dsp;
using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Interstellar.Routing.Router;

public class FilterRouter : AbstractAudioRouter
{
    public class FilteredMonoProvider : ISampleProvider
    {
        private BiQuadFilter filter;
        private ISampleProvider source;
        WaveFormat ISampleProvider.WaveFormat => source.WaveFormat;

        int ISampleProvider.Read(float[] buffer, int offset, int count)
        {
            int read = source.Read(buffer, offset, count);
            for (int n = 0; n < read; n++)
            {
                buffer[offset + n] = filter.Transform(buffer[offset + n]);
            }
            return read;
        }

        internal FilteredMonoProvider(ISampleProvider source, BiQuadFilter filter)
        {
            this.source = source;
            this.filter = filter;
        }
    }

    public class FilteredStereoProvider : ISampleProvider
    {
        private BiQuadFilter filterL, filterR;
        private ISampleProvider source;
        WaveFormat ISampleProvider.WaveFormat => source.WaveFormat;

        int ISampleProvider.Read(float[] buffer, int offset, int count)
        {
            int read = source.Read(buffer, offset, count);
            for (int n = 0; n < read; n++)
            {
                if(n % 2 == 0)
                    buffer[offset + n] = filterL.Transform(buffer[offset + n]);
                else
                    buffer[offset + n] = filterR.Transform(buffer[offset + n]);
            }
            return read;
        }

        internal FilteredStereoProvider(ISampleProvider source, BiQuadFilter filterL, BiQuadFilter filterR)
        {
            this.source = source;
            this.filterL = filterL;
            this.filterR = filterR;
        }
    }
    protected internal override bool ShouldBeGivenStereoInput => false;
    protected internal override bool IsEndpoint => false;
    private Func<BiQuadFilter> filterGenerator;
    
    private FilterRouter(Func<BiQuadFilter> filterGenerator, bool isGlobalRouter = false)
    {
        this.filterGenerator = filterGenerator;
        IsGlobalRouter = isGlobalRouter;
    }

    /// <summary>
    /// 高周波数の音を除去し、低周波数の音を通過させるローパスフィルタを作成します。
    /// </summary>
    /// <param name="cutoffFrequency"></param>
    /// <param name="qFactor"></param>
    /// <param name="isGlobalRouter"></param>
    /// <returns></returns>
    static public FilterRouter CreateLowPassFilter(float cutoffFrequency, float qFactor, bool isGlobalRouter = false)
    {
        return new FilterRouter(() => BiQuadFilter.LowPassFilter(AudioHelpers.ClockRate, cutoffFrequency, qFactor), isGlobalRouter);
    }

    /// <summary>
    /// 低周波数の音を除去し、高周波数の音を通過させるハイパスフィルタを作成します。
    /// </summary>
    /// <param name="cutoffFrequency"></param>
    /// <param name="qFactor"></param>
    /// <param name="isGlobalRouter"></param>
    /// <returns></returns>
    static public FilterRouter CreateHighPassFilter(float cutoffFrequency, float qFactor, bool isGlobalRouter = false)
    {
        return new FilterRouter(() => BiQuadFilter.HighPassFilter(AudioHelpers.ClockRate, cutoffFrequency, qFactor), isGlobalRouter);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="centerFrequency"></param>
    /// <param name="qFactor"></param>
    /// <param name="isGlobalRouter"></param>
    /// <returns></returns>
    static public FilterRouter CreateBandPassFilter(float centerFrequency, float qFactor, bool isGlobalRouter = false)
    {
        return new FilterRouter(() => BiQuadFilter.BandPassFilterConstantPeakGain(AudioHelpers.ClockRate, centerFrequency, qFactor), isGlobalRouter);
    }

    /// <summary>
    /// 特定の帯域の音を除去するノッチフィルタを作成します。
    /// </summary>
    /// <param name="centerFrequency"></param>
    /// <param name="qFactor"></param>
    /// <param name="isGlobalRouter"></param>
    /// <returns></returns>
    static public FilterRouter CreateNotchFilter(float centerFrequency, float qFactor, bool isGlobalRouter = false)
    {
        return new FilterRouter(() => BiQuadFilter.NotchFilter(AudioHelpers.ClockRate, centerFrequency, qFactor), isGlobalRouter);
    }



    internal override ISampleProvider GenerateProcessor(ISampleProvider source)
    {
        if (source.WaveFormat.Channels == 2)
        {
            return new FilteredStereoProvider(source, filterGenerator(), filterGenerator());
        }
        else
        {
            return new FilteredMonoProvider(source, filterGenerator());
        }
    }

}
