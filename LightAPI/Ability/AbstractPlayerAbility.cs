using LightInDark.Game;
using LightInDark.Roles;

namespace LightInDark.Abilities
{
    /// <summary>
    /// 能力基类。
    /// </summary>
    public abstract class AbstractPlayerAbility : DependentLifespan, IPlayerAbility
    {
        public Player MyPlayer { get; }
        public RuntimeRole Role { get; }
        public bool AmOwner => MyPlayer.AmOwner;
        public bool IsActive { get; private set; }

        protected AbstractPlayerAbility(RuntimeRole role)
        {
            Role = role;
            MyPlayer = role.MyPlayer;
        }

        public virtual void OnActivate() { IsActive = true; }
        public virtual void OnDeactivate() { IsActive = false; }
        public virtual void OnUpdate() { }

        public virtual void Release() { }
        void IGameOperator.OnReleased() { }
    }
}
