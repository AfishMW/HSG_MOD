using System.Collections.Generic;

namespace LightInDark.Game
{
    public interface IGame : ILifespan
    {
        Player LocalPlayer { get; }
        IEnumerable<Player> AllPlayers { get; }
        Player GetPlayer(byte playerId);
        void RegisterEntity(IGameOperator entity, ILifespan lifespan);
        void UnregisterEntity(IGameOperator entity);
    }
}