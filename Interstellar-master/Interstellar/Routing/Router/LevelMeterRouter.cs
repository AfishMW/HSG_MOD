using Interstellar.Messages;
using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Interstellar.Routing.Router;

public class LevelMeterRouter : AbstractAudioNodeProvider<LevelMeterRouter.Property>
{
    public class Property : ISampleProvider
    {
        private ISampleProvider sourceProvider;
        public float Decay { get; set; } = 0.5f;
        public float Level { get; private set; } = 0.0f;
        WaveFormat ISampleProvider.WaveFormat => sourceProvider.WaveFormat;

        int ISampleProvider.Read(float[] buffer, int offset, int count)
        {
            int read = sourceProvider.Read(buffer, offset, count);
            Level -= Decay * ((float)count / (float)AudioHelpers.ClockRate);
            if(Level < 0.0f) Level = 0.0f;
            for (int i = 0; i < read; i++)
            {
                if(Level < buffer[offset + i]) Level = buffer[offset + i];
            }
            if (Level > 1.0f) Level = 1.0f;
            return read;
        }

        internal Property(ISampleProvider source)
        {
            this.sourceProvider = source;
        }
    }

    protected internal override bool ShouldBeGivenStereoInput => false;
    protected internal override bool IsEndpoint => false;
    internal override ISampleProvider GenerateProcessor(ISampleProvider source)
    {
        return new Property(source);
    }


}