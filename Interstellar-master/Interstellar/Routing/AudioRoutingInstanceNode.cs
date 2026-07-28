using Interstellar.NAudio.Provider;
using Interstellar.Routing.Node;
using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Interstellar.Routing;

internal class AudioRoutingInstanceNode
{
    private AudioMixer? mixer;
    private AudioBuffer? buffer;
    private ISampleProvider processor;
    public AudioRoutingInstanceNode(List<AudioBuffer> bufferList, ISampleProvider source, Func<ISampleProvider, ISampleProvider> constructor, bool hasMultipleInput, bool hasMultipleOutput, int channels, int groupId)
    {
        if (hasMultipleInput)
        {
            mixer = new AudioMixer(channels);
            if(source != null) mixer.AddInput(source, -1);
        }
        else
        {
            mixer = null;
            if(source.WaveFormat.Channels == 1 && channels == 2) source = new MonoToStereoSampleProvider(source);
        }
        processor = constructor(mixer ?? source);
        if(hasMultipleOutput)
        {
            buffer = new AudioBuffer(processor, groupId);
            bufferList.Add(buffer);
        }
        else
        {
            buffer = null;
        }
    }

    internal void AddInput(ISampleProvider input, int groupId) => mixer?.AddInput(input, groupId);
    internal void RemoveInput(int groupId) => mixer?.RemoveInput(groupId);
    internal ISampleProvider Output => buffer ?? processor;
    internal ISampleProvider Processor => processor;
}
