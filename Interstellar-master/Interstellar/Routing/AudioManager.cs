using Interstellar.Messages;
using Interstellar.NAudio.Provider;
using Interstellar.Routing.Node;
using NAudio.CoreAudioApi;
using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Interstellar.Routing;

internal class AudioManager : IHasAudioPropertyNode
{
    AbstractAudioRouter router;
    ISampleProvider? endpoint = null;
    int nodeLength;
    AudioRoutingInstanceNode[] globalNodes;
    List<AudioBuffer> buffers = [];

    private class SampleProviderWrapper : ISampleProvider
    {
        ISampleProvider source;
        AudioManager manager;
        public SampleProviderWrapper(ISampleProvider source, AudioManager manager)
        {
            this.source = source;
            this.manager = manager;
        }

        WaveFormat ISampleProvider.WaveFormat => source.WaveFormat;

        int ISampleProvider.Read(float[] buffer, int offset, int count)
        {
            foreach (var b in manager.buffers) b.Clear();
            return source.Read(buffer, offset, count);
        }
    }

    internal AudioManager(AbstractAudioRouter audioRouter, int bufferLength = 2048, int bufferMaxLength = 4096)
    {
        this.bufferLength = bufferLength;
        this.bufferMaxLength = bufferMaxLength;
        this.router = audioRouter;
        nodeLength = FixStructure();
        GenerateGlobalNodes();
        if(endpoint == null) throw new InvalidOperationException("No endpoint found in the audio routing structure.");
    }

    private int bufferLength, bufferMaxLength;

    private int FixStructure()
    {
        int availableId = 0;
        void SetChannelStereo(AbstractAudioRouter router)
        {
            router.Channels = 2;
            foreach (var c in router.GetChildRouters()) SetChannelStereo(c);
        }
        void SetId(AbstractAudioRouter router, bool shouldBeGivenStereoChannel, bool inGlobalRoute)
        {
            if (router.Id == -1)
            {
                router.Id = availableId++;
                if (router.IsGlobalRouter && !inGlobalRoute) router.HasMultipleInput = true;
                if (shouldBeGivenStereoChannel || router.ShouldBeGivenStereoInput) router.Channels = 2;
                foreach(var c in router.GetChildRouters()) SetId(c, shouldBeGivenStereoChannel || router.OutputChannels == 2, router.IsGlobalRouter || inGlobalRoute);
            }
            else
            {
                router.HasMultipleInput = true;
                if (router.Channels == 1 && shouldBeGivenStereoChannel) SetChannelStereo(router);
            }
        }
        SetId(router, false, false);
        return availableId;
    }

    internal void GenerateGlobalNodes()
    {
        globalNodes = new AudioRoutingInstanceNode[nodeLength];
        void GenerateInner(AbstractAudioRouter router, ISampleProvider? parent, bool isInGlobalArea)
        {
            if (router.IsGlobalRouter)
            {
                if (globalNodes[router.Id] == null)
                {
                    globalNodes[router.Id] = new AudioRoutingInstanceNode(buffers, parent!, router.GenerateProcessor, router.HasMultipleInput, router.HasMultipleOutput, router.Channels, -1);
                    if (router.IsEndpoint)
                    {
                        var processor = globalNodes[router.Id].Processor;
                        endpoint = new SampleProviderWrapper(processor, this);
                    }
                }
                else if(parent != null)
                {
                    globalNodes[router.Id].AddInput(parent!, -1);
                }
            }
            else
            {
                if(isInGlobalArea) throw new InvalidDataException("A non-global router cannot be a child of a global router.");
            }
                foreach (var c in  router.GetChildRouters()) GenerateInner(c, globalNodes[router.Id]?.Processor, router.IsGlobalRouter);
        }
        GenerateInner(router, null, false);
    }

    public AudioRoutingInstance Generate(int groupId)
    {
        AudioRoutingInstanceNode[] nodes = new AudioRoutingInstanceNode[globalNodes.Length];
        Array.Copy(globalNodes, nodes, globalNodes.Length);
        void GenerateInner(AbstractAudioRouter router, ISampleProvider? parent)
        {
            if (nodes[router.Id] == null)
            {
                nodes[router.Id] = new AudioRoutingInstanceNode(this.buffers, parent!, router.GenerateProcessor, router.HasMultipleInput, router.HasMultipleOutput, router.Channels, groupId);
            }
            else if(parent != null)
            {
                nodes[router.Id].AddInput(parent, groupId);
            }
            if (!router.IsGlobalRouter) foreach (var c in router.GetChildRouters()) GenerateInner(c, nodes[router.Id]?.Processor);
        }
        BufferedSampleProvider sourceProvider = new(WaveFormat.CreateIeeeFloatWaveFormat(AudioHelpers.ClockRate, 1), bufferMaxLength) { DiscardOnBufferOverflow = true, BufferCutSize = bufferMaxLength, BufferCutToSize = bufferLength };
        GenerateInner(router, sourceProvider);
        return new(this.buffers, nodes, sourceProvider, groupId);
    }

    public ISampleProvider? Endpoint => endpoint;


    public void Remove(int clientId)
    {
        foreach(var node in this.globalNodes)
        {
            node?.RemoveInput(clientId);
        }
        buffers.RemoveAll(b => b.GroupId == clientId);
    }

    AudioRoutingInstanceNode IHasAudioPropertyNode.GetProperty(int propertyId) => globalNodes[propertyId];
}
