using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Interstellar.API.VoiceChat;

public interface IVCRoom<Player>
{
    bool TryGetPlayer(Predicate<Player> predicate, [MaybeNullWhen(false)] out IVCClient<Player> player);
    IEnumerable<IVoiceStream> AllVoiceStreams { get; }

    /// <summary>
    /// VCを終了します。
    /// </summary>
    void Close();
}
