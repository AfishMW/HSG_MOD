using System;
using LightInDark.Game;
using UnityEngine;

namespace LightInDark.UI
{
    public interface IAbilityButton : IGameOperator
    {
        void SetVisible(bool visible);
        void SetEnabled(bool enabled);
        void SetCooldown(float current, float max);
        Action OnClick { set; }
    }
}