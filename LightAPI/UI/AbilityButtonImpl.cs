using System;
using LightInDark.Game;
using UnityEngine;

namespace LightInDark.UI
{
    public class AbilityButtonImpl : IAbilityButton
    {
        private ActionButton _button;
        private ILifespan _lifespan;
        private bool _isDead;

        public bool IsDeadObject => _isDead || (_lifespan?.IsDeadObject ?? false);

        public AbilityButtonImpl(ILifespan lifespan, Player player, string label, Sprite icon, Action onClick)
        {
            _lifespan = lifespan;
            // 实际创建按钮的代码（这里用伪代码）
            // _button = UnityEngine.Object.Instantiate(HudManager.Instance.KillButton);
            // _button.graphic.sprite = icon;
            // _button.buttonLabelText.text = label;
            // 设置点击事件
            // _button.OnClick.AddListener(() => onClick?.Invoke());
            // 注册到游戏
            this.Register(lifespan);
        }

        public void SetVisible(bool visible) { /* 实现 */ }
        public void SetEnabled(bool enabled) { /* 实现 */ }
        public void SetCooldown(float current, float max) { /* 实现 */ }
        public Action OnClick { set { /* 绑定点击 */ } }

        public void Release()
        {
            _isDead = true;
            if (_button != null)
                UnityEngine.Object.Destroy(_button.gameObject);
        }
    }
}