using LightInDark.Core;
using LightInDark.Game;
using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace LightInDark.UI
{
    public class ModAbilityButton : IModAbilityButton
    {

        private readonly ILifespan _lifespan;
        private readonly Player _player;
        private readonly GameObject _gameObject;
        private readonly ActionButton _actionButton;  // Among Us 的 ActionButton

        private string _label = "";
        private Sprite _image;
        private KeyCode _key = KeyCode.None;
        private float _cooldownMax = 0f;
        private float _cooldownTimer = 0f;
        private bool _isInCooldown = false;
        private Func<bool> _availabilityPredicate = () => true;
        private Func<bool> _visibilityPredicate = () => true;

        private UnityAction _cachedClickAction;
        private Action _onClick;
        private Action _onEffectStart;
        private Action _onEffectEnd;

        // ---- IGameOperator / ILifespan ----
        public bool IsDeadObject => _gameObject == null || !_gameObject.activeInHierarchy;

        // ---- 构造函数 ----
        public ModAbilityButton(ILifespan lifespan, Player player)
        {
            _lifespan = lifespan;
            _player = player;

            var template = HudManager.Instance.KillButton;
            _gameObject = UnityEngine.Object.Instantiate(template.gameObject, template.transform.parent);
            _gameObject.name = "ModAbilityButton";
            _actionButton = _gameObject.GetComponent<ActionButton>();

            this.Register(lifespan);
            _gameObject.SetActive(false);
            LightLogger.Log($"[ModAbilityButton] 创建按钮，玩家: {player.Name}, 寿命存活: {!lifespan.IsDeadObject}");
        }

        // ---- 链式配置 ----
        public IModAbilityButton SetImage(Sprite sprite)
        {
            _image = sprite;
            if (_actionButton != null)
                _actionButton.graphic.sprite = sprite;
            return this;
        }

        public IModAbilityButton SetLabel(string label)
        {
            _label = label;
            if (_actionButton != null)
                _actionButton.buttonLabelText.text = label;
            return this;
        }

        public IModAbilityButton BindKey(KeyCode key)
        {
            _key = key;
            return this;
        }

        public IModAbilityButton SetCooldown(float seconds)
        {
            _cooldownMax = seconds;
            _cooldownTimer = 0f;
            _isInCooldown = false;
            return this;
        }

        public IModAbilityButton SetAvailability(Func<bool> predicate)
        {
            _availabilityPredicate = predicate ?? (() => true);
            return this;
        }

        public IModAbilityButton SetVisibility(Func<bool> predicate)
        {
            _visibilityPredicate = predicate ?? (() => true);
            return this;
        }

        // ---- 运行时 ----
        public void StartCoolDown()
        {
            if (_cooldownMax <= 0) return;
            _cooldownTimer = _cooldownMax;
            _isInCooldown = true;
            LightLogger.Log($"[ModAbilityButton] 开始冷却，持续时间: {_cooldownMax}s");
        }

        public bool IsInCooldown => _isInCooldown;

        public void SetVisible(bool visible)
        {
            if (_gameObject != null)
                _gameObject.SetActive(visible);
        }

        public void SetEnabled(bool enabled)
        {
            if (_actionButton != null)
                _actionButton.enabled = enabled;
        }

        public Action OnClick
        {
            set
            {
                _onClick = value;
                if (_actionButton != null)
                {
                    var btn = _actionButton.GetComponent<Button>();
                    btn.onClick.RemoveAllListeners();

                    btn.onClick.AddListener((UnityAction)OnButtonClick);
                }
            }
        }

        private void OnButtonClick()
        {
            LightLogger.Log($"[ModAbilityButton] 按钮被点击，冷却状态: {_isInCooldown}, 可用性: {_availabilityPredicate()}");
            if (_isInCooldown) return;
            if (!_availabilityPredicate()) return;
            _onClick?.Invoke();
        }
        public Action OnEffectStart { set => _onEffectStart = value; }
        public Action OnEffectEnd { set => _onEffectEnd = value; }

        // ---- 构建 ----
        public IModAbilityButton Build()
        {
            // 应用可见性
            SetVisible(_visibilityPredicate());
            SetEnabled(_availabilityPredicate());
            return this;
        }

        // ---- 每帧更新（由 GameManager 调用） ----
        public void Update()
        {
            if (IsDeadObject) return;

            // 更新冷却
            if (_isInCooldown)
            {
                _cooldownTimer -= Time.deltaTime;
                if (_cooldownTimer <= 0f)
                {
                    _cooldownTimer = 0f;
                    _isInCooldown = false;
                }
                // 更新 UI 冷却显示
                _actionButton?.SetCoolDown(_cooldownTimer, _cooldownMax);
            }

            // 更新可见性
            bool shouldShow = _visibilityPredicate();
            if (_gameObject.activeSelf != shouldShow)
                _gameObject.SetActive(shouldShow);

            // 更新可用性
            if (shouldShow)
            {
                bool enabled = _availabilityPredicate() && !_isInCooldown;
                _actionButton.enabled = enabled;
            }

            // 按键绑定
            if (_key != KeyCode.None && Input.GetKeyDown(_key))
            {
                if (_gameObject.activeSelf && _availabilityPredicate() && !_isInCooldown)
                {
                    _onClick?.Invoke();
                }
            }
        }

        // ---- IReleasable ----
        public void Release()
        {
            LightLogger.Log($"[ModAbilityButton] 释放按钮");
            if (_gameObject != null)
                UnityEngine.Object.Destroy(_gameObject);
        }

        void IGameOperator.OnReleased() { }

    }
}