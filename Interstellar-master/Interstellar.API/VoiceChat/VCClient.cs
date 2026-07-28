using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Interstellar.API.VoiceChat;

public interface IVCClient<Player>
{
    Player MyPlayer { get; }
    IVoiceStream? CurrentVoiceStream { get; }
}
