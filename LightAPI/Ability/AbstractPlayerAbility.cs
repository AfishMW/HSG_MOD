using LightInDark.Core;
using LightInDark.Game;
using LightInDark.Roles;
using System;

namespace LightInDark.Ability
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
            try
            {
                Role = role;
                MyPlayer = role.MyPlayer;
            }
            catch (Exception ex)
            {
                LightLogger.LogError("AbstractPlayerAbility.AbstractPlayerAbility", ex);
            }
        }

        public virtual void OnActivate()
        {
            try
            {
                IsActive = true;
            }
            catch (Exception ex)
            {
                LightLogger.LogError("AbstractPlayerAbility.OnActivate", ex);
            }
        }

        public virtual void OnDeactivate()
        {
            try
            {
                IsActive = false;
            }
            catch (Exception ex)
            {
                LightLogger.LogError("AbstractPlayerAbility.OnDeactivate", ex);
            }
        }

        public virtual void OnUpdate()
        {
            try
            {
            }
            catch (Exception ex)
            {
                LightLogger.LogError("AbstractPlayerAbility.OnUpdate", ex);
            }
        }

        public virtual void Release()
        {
            try
            {
            }
            catch (Exception ex)
            {
                LightLogger.LogError("AbstractPlayerAbility.Release", ex);
            }
        }

        void IGameOperator.OnReleased()
        {
            try
            {
            }
            catch (Exception ex)
            {
                LightLogger.LogError("AbstractPlayerAbility.OnReleased", ex);
            }
        }
    }
}
