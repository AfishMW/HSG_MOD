using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Interstellar.Routing.Router;

public class DistortionFilter : AbstractAudioNodeProvider<DistortionFilter.Property>
{
    public class Property : ISampleProvider
    {
        private ISampleProvider sourceProvider;
        public float Threshold { get; set; }
        public bool Amplification { get; set; }

        WaveFormat ISampleProvider.WaveFormat => sourceProvider.WaveFormat;

        int ISampleProvider.Read(float[] buffer, int offset, int count)
        {
            int read = sourceProvider.Read(buffer, offset, count);
            if (Amplification)
            {
                float amp = 1f / Threshold;
                for (int i = 0; i < read; i++)
                {
                    if (buffer[offset + i] > Threshold)
                        buffer[offset + i] = 1f;
                    else if (buffer[offset + i] < -Threshold)
                        buffer[offset + i] = -1f;
                    else 
                        buffer[offset + i] *= amp;
                }
            }
            else
            {
                for (int i = 0; i < read; i++)
                {
                    if (buffer[offset + i] > Threshold)
                        buffer[offset + i] = Threshold;
                    else if (buffer[offset + i] < -Threshold)
                        buffer[offset + i] = -Threshold;

                }
            }
            return read;
        }

        internal Property(ISampleProvider source)
        {
            this.sourceProvider = source;
        }
    }
    public float DefaultThreshold { get; set; } = 1f;
    public bool DefaultAmplification { get; set; } = false;
    protected internal override bool ShouldBeGivenStereoInput => false;
    protected internal override bool IsEndpoint => false;
    internal override ISampleProvider GenerateProcessor(ISampleProvider source)
    {
        return new Property(source) { Threshold = DefaultThreshold, Amplification = DefaultAmplification };
    }


}