using LightInDark.Game;
using LightInDark.Roles;

namespace LightInDark.Abilities
{
    /// <summary>
    /// 玩家能力接口。
    /// 绑定到 Player 和 Role。
    /// </summary>
    public interface IPlayerAbility : IGameOperator, IBindPlayer
    {
        RuntimeRole Role { get; }
        bool IsActive { get; }
        void OnActivate();
        void OnDeactivate();
        void OnUpdate();
    }
}
