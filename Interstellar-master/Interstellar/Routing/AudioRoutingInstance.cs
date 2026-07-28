using Concentus;
using Interstellar.Messages;
using Interstellar.NAudio.Provider;
using Interstellar.Routing.Node;
using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Interstellar.Routing;

public class AudioRoutingInstance : IHasAudioPropertyNode
{
    private List<AudioBuffer> buffers = [];
    private AudioRoutingInstanceNode[] nodes;
    private BufferedSampleProvider sourceProvider;
    AudioRoutingInstanceNode IHasAudioPropertyNode.GetProperty(int propertyId) => nodes[propertyId];
    public int ClientId { get; private init; }
    
    internal AudioRoutingInstance(List<AudioBuffer> buffers, AudioRoutingInstanceNode[] nodes, BufferedSampleProvider sourceProvider, int clientId)
    {
        this.ClientId = clientId;
        this.buffers = buffers;
        this.nodes = nodes;
        this.sourceProvider = sourceProvider;
    }

    public void AddSamples(float[] samples, int offset, int count)
    {
        sourceProvider.AddSamples(samples, offset, count);
        LastReceiptTime = System.DateTime.Now.Ticks;
    }

    private long LastReceiptTime = System.DateTime.Now.Ticks;
    public int ElapsedSinceLastReceipt => (int)((System.DateTime.Now.Ticks - LastReceiptTime) / 10000);//ミリ秒単位
}
