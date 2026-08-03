using System.Collections.Generic;
using LightInDark.Abilities;
using LightInDark.Events;
using LightInDark.Game;

namespace LightInDark.Roles
{
    public abstract class RuntimeRole : IBindPlayer, IGameOperator, ILifespan
    {
        public DefinedRole Definition { get; }
        public Player MyPlayer { get; }
        public bool AmOwner => MyPlayer.AmOwner;

        // 能力列表
        protected List<IPlayerAbility> Abilities { get; } = new();

        public bool IsDeadObject => MyPlayer.IsDeadObject;

        protected RuntimeRole(DefinedRole definition, Player player)
        {
            Definition = definition;
            MyPlayer = player;
            this.Register(player);
            EventSystem.RegisterInstance(this);
            OnActivated();
        }

        protected virtual void OnActivated() { }

        protected void AddAbility(IPlayerAbility ability)
        {
            Abilities.Add(ability);
            ability.Register(this);
        }

        public void Release()
        {
            EventSystem.UnregisterInstance(this);
            foreach (var ability in Abilities)
                ability.Release();
            Abilities.Clear();
        }

    }
}