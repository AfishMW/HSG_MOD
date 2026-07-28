using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Interstellar.Routing.Router;

public class SimpleRouter : AbstractAudioRouter
{
    protected internal override bool ShouldBeGivenStereoInput => false;
    protected internal override bool IsEndpoint => false;
    internal override ISampleProvider GenerateProcessor(ISampleProvider source)
    {
        return source;
    }

    public SimpleRouter(bool isGlobalRouter = false)
    {
        IsGlobalRouter = isGlobalRouter;
    }

}
