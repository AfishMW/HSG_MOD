using LightInDark.Game;

namespace LightInDark.Abilities
{
    public interface IPlayerAbility : IGameOperator, IBindPlayer
    {
        bool HideKillButton { get; }
    }
}