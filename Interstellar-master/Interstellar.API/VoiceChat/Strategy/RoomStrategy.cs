using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Interstellar.API.VoiceChat.Strategy;

public interface IRoomStrategy<Player>
{
    /// <summary>
    /// プレイヤー名をもとにプレイヤーを解決します。
    /// </summary>
    /// <param name="playerName">プレイヤー名</param>
    /// <returns>紐づくプレイヤーがいる場合、プレイヤー</returns>
    Player? TryResolvePlayer(string playerName);
}
