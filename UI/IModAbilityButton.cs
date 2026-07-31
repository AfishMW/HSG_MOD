using System;
using LightInDark.Game;
using UnityEngine;

namespace LightInDark.UI
{
    /// <summary>
    /// Nebula 风格的能力按钮接口
    /// </summary>
    public interface IModAbilityButton : IGameOperator
    {
        // ---- 链式配置 ----
        IModAbilityButton SetImage(Sprite sprite);
        IModAbilityButton SetLabel(string label);
        IModAbilityButton BindKey(KeyCode key);
        IModAbilityButton SetCooldown(float seconds);
        IModAbilityButton SetAvailability(Func<bool> predicate);
        IModAbilityButton SetVisibility(Func<bool> predicate);

        // ---- 运行时操作 ----
        void StartCoolDown();
        bool IsInCooldown { get; }
        void SetVisible(bool visible);
        void SetEnabled(bool enabled);

        // ---- 事件 ----
        Action OnClick { set; }
        Action OnEffectStart { set; }
        Action OnEffectEnd { set; }

        // ---- 构建 ----
        IModAbilityButton Build();
    }
}