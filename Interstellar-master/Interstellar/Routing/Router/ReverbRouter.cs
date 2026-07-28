using Interstellar.NAudio.Provider;
using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Interstellar.Routing.Router;

/// <summary>
/// ディレイ効果をもたらすオーディオルーター。
/// </summary>

public class ReverbRouter : AbstractAudioNodeProvider<ReverbRouter.Property>
{
    public class Property : ISampleProvider
    {
        private ReverbSampleProvider sampleProvider;
        public float Decay { get => sampleProvider.Decay; set => sampleProvider.Decay = value; }
        public float WetDryMix { get => sampleProvider.WetDryMix; set => sampleProvider.WetDryMix = value; }

        WaveFormat ISampleProvider.WaveFormat => sampleProvider.WaveFormat;

        int ISampleProvider.Read(float[] buffer, int offset, int count) => sampleProvider.Read(buffer, offset, count);

        internal Property(ISampleProvider source, int delayMilliseconds, float decay, float wetDryMix)
        {
            sampleProvider = new ReverbSampleProvider(source, delayMilliseconds, decay, wetDryMix);
        }
    }
    protected internal override bool ShouldBeGivenStereoInput => false;
    protected internal override bool IsEndpoint => false;
    private int delayMilliseconds;
    private float defaultDecay { get; set; } = 0.3f;
    private float defaultWetDryMix { get; set; } = 0.5f;
    public ReverbRouter(int delayMilliseconds, float defaultDecay = 0.3f, float defaultWetDryMix = 0.5f)
    {
        if (delayMilliseconds < 1) throw new ArgumentOutOfRangeException("delayMilliseconds must be greater than 0.");
        if (defaultDecay < 0.0f || defaultDecay > 1.0f) throw new ArgumentOutOfRangeException("defaultDecay must be between 0.0 and 1.0.");
        if (defaultWetDryMix < 0.0f || defaultWetDryMix > 1.0f) throw new ArgumentOutOfRangeException("defaultWetDryMix must be between 0.0 and 1.0.");
        this.delayMilliseconds = delayMilliseconds;
        this.defaultDecay = defaultDecay;
        this.defaultWetDryMix = defaultWetDryMix;
    }

    internal override ISampleProvider GenerateProcessor(ISampleProvider source)
    {
        return new Property(source, delayMilliseconds, defaultDecay, defaultWetDryMix);
    }

}
