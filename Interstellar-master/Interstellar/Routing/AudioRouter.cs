using Interstellar.NAudio.Provider;
using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Interstellar.Routing;

public abstract class AbstractAudioRouter
{
    internal int Id { get; set; } = -1;
    internal bool HasMultipleInput { get; set; } = false;
    internal bool HasMultipleOutput => children.Count() > 1;
    abstract protected internal bool ShouldBeGivenStereoInput { get; }
    public bool IsGlobalRouter { get; set; } = false;
    abstract protected internal bool IsEndpoint { get; }
    internal int Channels = 1;
    virtual internal int OutputChannels => Channels;
    internal IEnumerable<AbstractAudioRouter> GetChildRouters() => children;
    private List<AbstractAudioRouter> children = [];

    public void Connect(AbstractAudioRouter child)
    {
        if (child.Id != -1 || Id != -1) throw new InvalidOperationException("Cannot use a finalized component.");
        children.Add(child);
    }

    abstract internal ISampleProvider GenerateProcessor(ISampleProvider source);
}

public interface IHasAudioPropertyNode
{
    internal AudioRoutingInstanceNode GetProperty(int propertyId);
}

public abstract class AbstractAudioNodeProvider<AudioProperty> : AbstractAudioRouter where AudioProperty : class, ISampleProvider
{
    public AudioProperty GetProperty(IHasAudioPropertyNode nodeHolder) => (nodeHolder.GetProperty(Id).Processor as AudioProperty)!;
}