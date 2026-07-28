using Interstellar.NAudio.Provider;
using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Interstellar.Routing.Router;

public class VolumeRouter : AbstractAudioNodeProvider<VolumeRouter.Property>
{
    public class Property : ISampleProvider
    {
        private ISampleProvider sourceProvider;
        public float Volume { get; set; }

        WaveFormat ISampleProvider.WaveFormat => sourceProvider.WaveFormat;

        int ISampleProvider.Read(float[] buffer, int offset, int count)
        {
            if(Volume > 0f)
            {
                int read = sourceProvider.Read(buffer, offset, count);
                if(Volume != 1.0f)
                {
                    for (int i = 0; i < count; i++)
                    {
                        buffer[offset + i] *= Volume;
                    }
                }
                return read;
            }
            else
            {
                for (int i = 0; i < count; i++) buffer[offset + i] = 0f;
                return count;
            }
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