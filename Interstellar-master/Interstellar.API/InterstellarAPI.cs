using Interstellar.API.VoiceChat;
using Interstellar.API.VoiceChat.Strategy;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("Interstellar")]

namespace Interstellar.API;

public static class InterstellarAPI
{
    static private IInterstellar _instance = null!;
    static internal void SetInstance(IInterstellar instance) => _instance = instance;

    /// <summary>
    /// VCを作成します。
    /// </summary>
    /// <typeparam name="Player"></typeparam>
    /// <param name="roomStrategy"></param>
    /// <returns></returns>
    static public IVCRoom<Player> CreateVCRoom<Player>(IRoomStrategy<Player> roomStrategy) => _instance.CreateVCRoom(roomStrategy);
}

internal interface IInterstellar
{
    IVCRoom<Player> CreateVCRoom<Player>(IRoomStrategy<Player> roomStrategy);
}
