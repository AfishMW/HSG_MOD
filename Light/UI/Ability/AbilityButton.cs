using System;
using System.Collections.Generic;
using HarmonyLib;
using LightInDark.Core;
using LightInDark.Game;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace Light.UI.Ability
{
    // =====================================================================
    // 配置
    // =====================================================================

    /// <summary>
    /// 按钮配置
    /// </summary>
    public class AbilityButtonConfig
    {
        /// <summary>直接文本（优先用 LabelKey 走语言键，避免硬编码）。</summary>
        public string Label = "";

        /// <summary>标签语言键（如 "Button.KILL.label"）。设置后按语言解析，缺省回退 <see cref="Label"/>。</summary>
        public string LabelKey;

        /// <summary>解析后的标签文本。</summary>
        public string ResolvedLabel
        {
            get
            {
                if (!string.IsNullOrEmpty(LabelKey))
                {
                    string v = LightInDark.Language.Language.GetStringOrKey(LabelKey, Label);
                    return string.IsNullOrEmpty(v) ? Label : v;
                }
                return Label;
            }
        }

        public Sprite Icon;
        public KeyCode Hotkey = KeyCode.None;
        public float Cooldown = 0f;
        public Func<bool> CanUse = () => true;
        public Func<bool> CanShow = () => true;
        public bool IsKillButton = false;
        public bool AlwaysShow = false;

        /// <summary>最大使用次数。0 = 无限。</summary>
        public int MaxUses = 0;
    }

    public class EffectButtonConfig : AbilityButtonConfig
    {
        public float EffectDuration = 0f;
        public bool IsToggle = false;
        public bool ShowEffectCountdown = true;
    }

    // =====================================================================
    // AbilityButtonManager
    // =====================================================================

    public static class AbilityButtonManager
    {
        private static readonly List<AbilityButton> _buttons = new();
        private static readonly List<PendingButton> _pending = new();

        private class PendingButton
        {
            public ILifespan Lifespan;
            public Player Player;
            public AbilityButtonConfig Config;
            public Action OnClick;
            public Action<AbilityButton>? OnCreated;
        }

        public static void Register(ILifespan lifespan, Player player,
            AbilityButtonConfig config, Action onClick, Action<AbilityButton>? onCreated = null)
        {
            try
            {
                LightLogger.Log($"[AbilityButtonManager] Register: {config.Label}, HudManager.InstanceExists={HudManager.InstanceExists}");

                if (HudManager.InstanceExists && HudManager.Instance != null
                    && HudManager.Instance.AbilityButton != null && HudManager.Instance.KillButton != null)
                {
                    try
                    {
                        var button = new AbilityButton(lifespan, player, config, onClick);
                        _buttons.Add(button);
                        onCreated?.Invoke(button);
                        LightLogger.Log($"[AbilityButtonManager] 按钮立即创建成功: {config.Label}");
                    }
                    catch (Exception ex)
                    {
                        LightLogger.LogWarning($"[AbilityButtonManager] 立即创建失败: {ex.Message}");
                        _pending.Add(new PendingButton { Lifespan = lifespan, Player = player, Config = config, OnClick = onClick, OnCreated = onCreated });
                    }
                }
                else
                {
                    _pending.Add(new PendingButton { Lifespan = lifespan, Player = player, Config = config, OnClick = onClick, OnCreated = onCreated });
                    LightLogger.Log($"[AbilityButtonManager] 加入 pending: {config.Label}");
                }
            }
            catch (Exception ex)
            {
                LightLogger.LogError("[AbilityButton.Register]", ex);
            }
        }

        public static void CreateAllPending()
        {
            try
            {
                foreach (var p in _pending)
                {
                    try
                    {
                        var button = new AbilityButton(p.Lifespan, p.Player, p.Config, p.OnClick);
                        _buttons.Add(button);
                        p.OnCreated?.Invoke(button);
                        LightLogger.Log($"[AbilityButtonManager] 按钮创建成功(pending): {p.Config.Label}");
                    }
                    catch (Exception ex)
                    {
                        LightLogger.LogWarning($"[AbilityButtonManager] 按钮创建失败: {p.Config.Label}: {ex.Message}");
                    }
                }
                _pending.Clear();
            }
            catch (Exception ex)
            {
                LightLogger.LogError("[AbilityButton.CreateAllPending]", ex);
            }
        }

        public static void UpdateAll()
        {
            try
            {
                for (int i = _buttons.Count - 1; i >= 0; i--)
                {
                    var button = _buttons[i];
                    if (button.IsDeadObject) { _buttons.RemoveAt(i); continue; }
                    try { button.Update(); }
                    catch (Exception ex) { LightLogger.LogWarning($"[AbilityButtonManager] Update 失败: {ex.Message}"); }
                }
            }
            catch (Exception ex)
            {
                LightLogger.LogError("[AbilityButton.UpdateAll]", ex);
            }
        }

        public static void SetAllVisible(bool visible)
        {
            try
            {
                foreach (var button in _buttons)
                    button.SetHudActive(visible);
            }
            catch (Exception ex)
            {
                LightLogger.LogError("[AbilityButton.SetAllVisible]", ex);
            }
        }

        public static void Clear()
        {
            try
            {
                _buttons.Clear();
                _pending.Clear();
            }
            catch (Exception ex)
            {
                LightLogger.LogError("[AbilityButton.Clear]", ex);
            }
        }
    }

    // =====================================================================
    // AbilityButton — 普通能力按钮
    // =====================================================================

    public class AbilityButton : IGameOperator, ILifespan
    {
        protected readonly ILifespan _lifespan;
        protected readonly Player _player;
        protected readonly GameObject _gameObject;
        protected readonly ActionButton _actionButton;
        protected readonly PassiveButton _passiveButton;

        protected AbilityButtonConfig Config;
        protected float _cooldownTimer;
        protected bool _inCooldown;
        protected bool _hudActive = true;
        protected int _usesLeft;

        protected Action OnClickHandler;

        public bool IsDeadObject => _gameObject == null;

        public bool IsInCooldown => _inCooldown;
        public bool IsVisible => _gameObject.activeSelf;
        public int UsesLeft => _usesLeft;
        public bool HasLimitedUses => Config.MaxUses > 0;

        public AbilityButton(ILifespan lifespan, Player player, AbilityButtonConfig config, Action onClick)
        {
            try
            {
                _lifespan = lifespan;
                _player = player;
                Config = config;
                OnClickHandler = onClick;
                _usesLeft = config.MaxUses;

                LightLogger.Log($"[AbilityButton] 步骤1: 选择模板, IsKillButton={config.IsKillButton}");
                ActionButton template = config.IsKillButton
                    ? HudManager.Instance.KillButton
                    : HudManager.Instance.AbilityButton;

                if (template == null) throw new Exception("[AbilityButton] 模板为 null");
                if (template.transform.parent == null) throw new Exception("[AbilityButton] 模板 parent 为 null");

                LightLogger.Log($"[AbilityButton] 步骤2: 克隆 GameObject");
                _gameObject = Object.Instantiate(template.gameObject, template.transform.parent);
                _gameObject.name = $"AbilityButton_{config.Label}";

                LightLogger.Log($"[AbilityButton] 步骤3: 获取组件");
                _actionButton = _gameObject.GetComponent<ActionButton>();
                if (_actionButton == null) throw new Exception("[AbilityButton] ActionButton 组件为 null");

                _passiveButton = _gameObject.GetComponent<PassiveButton>();
                if (_passiveButton == null) throw new Exception("[AbilityButton] PassiveButton 组件为 null");

                // 关键修复：克隆材质实例！
                // 原版按钮和克隆按钮共享同一个 material 实例，
                // 导致 _Percent（冷却进度）被原版按钮覆盖。
                if (_actionButton.graphic != null && _actionButton.graphic.material != null)
                {
                    _actionButton.graphic.material = new Material(_actionButton.graphic.material);
                }

                LightLogger.Log($"[AbilityButton] 步骤4: 应用配置");
                ApplyConfig();

                LightLogger.Log($"[AbilityButton] 步骤5: 注册");
                this.Register(lifespan);

                LightLogger.Log($"[AbilityButton] 步骤6: 绑定点击 (PassiveButton)");
                // 替换 OnClick 事件
                _passiveButton.OnClick = new Button.ButtonClickedEvent();
                _passiveButton.OnMouseOver = new UnityEvent();
                _passiveButton.OnMouseOut = new UnityEvent();
                _passiveButton.OnClick.AddListener((UnityAction)HandleClick);

                // 设置使用次数显示
                if (HasLimitedUses)
                    _actionButton.SetUsesRemaining(_usesLeft);
                else
                    _actionButton.SetInfiniteUses();

                _gameObject.SetActive(false);
                LightLogger.Log($"[AbilityButton] 创建成功: {config.Label}");
            }
            catch (Exception ex)
            {
                LightLogger.LogError("[AbilityButton.AbilityButton]", ex);
            }
        }

        protected void ApplyConfig()
        {
            try
            {
                if (_actionButton == null) return;
                if (Config.Icon != null)
                    _actionButton.graphic.sprite = Config.Icon;
                string label = Config.ResolvedLabel;
                if (!string.IsNullOrEmpty(label))
                    _actionButton.OverrideText(label);
                if (Config.Cooldown > 0f)
                {
                    _actionButton.SetCoolDown(0f, Config.Cooldown);
                    _actionButton.cooldownTimerText.gameObject.SetActive(false);
                }
            }
            catch (Exception ex)
            {
                LightLogger.LogError("[AbilityButton.ApplyConfig]", ex);
            }
        }

        protected virtual void HandleClick()
        {
            try
            {
                LightLogger.Log($"[AbilityButton] HandleClick: {Config.Label}, inCooldown={_inCooldown}, canUse={Config.CanUse()}, usesLeft={_usesLeft}");

                if (_inCooldown) return;
                if (!Config.CanUse()) return;

                // 检查使用次数
                if (HasLimitedUses && _usesLeft <= 0) return;

                // 执行回调
                OnClickHandler?.Invoke();

                // 扣减使用次数
                if (HasLimitedUses)
                {
                    _usesLeft--;
                    _actionButton.SetUsesRemaining(_usesLeft);
                    LightLogger.Log($"[AbilityButton] 使用次数: {_usesLeft}/{Config.MaxUses}");
                }

                // 开始冷却
                if (Config.Cooldown > 0f)
                    StartCooldown();
            }
            catch (Exception ex)
            {
                LightLogger.LogError("[AbilityButton.HandleClick]", ex);
            }
        }

        public void StartCooldown()
        {
            try
            {
                _cooldownTimer = Config.Cooldown;
                _inCooldown = true;
                LightLogger.Log($"[AbilityButton] 开始冷却: {Config.Cooldown}s");
            }
            catch (Exception ex)
            {
                LightLogger.LogError("[AbilityButton.StartCooldown]", ex);
            }
        }

        public void SetVisible(bool visible) {
            try
            {     if (_gameObject != null) _gameObject.SetActive(visible);
            }
            catch (Exception ex)
            {
                LightLogger.LogError("[AbilityButton.SetVisible]", ex);
            } }
        public void SetHudActive(bool active) {
            try
            {     _hudActive = active;
            }
            catch (Exception ex)
            {
                LightLogger.LogError("[AbilityButton.SetHudActive]", ex);
            } }
        public void UpdateConfig(AbilityButtonConfig config) {
            try
            {     Config = config; ApplyConfig();
            }
            catch (Exception ex)
            {
                LightLogger.LogError("[AbilityButton.UpdateConfig]", ex);
            } }

        public Action OnClick { set => OnClickHandler = value; }

        public virtual void Update()
        {
            try
            {
                if (IsDeadObject) return;

                // 冷却更新
                if (_inCooldown)
                {
                    _cooldownTimer -= Time.deltaTime;

                    if (_cooldownTimer <= 0f)
                    {
                        _cooldownTimer = 0f;
                        _inCooldown = false;
                        // 清除冷却显示
                        _actionButton?.SetCooldownFill(0f);
                        if (_actionButton?.cooldownTimerText != null)
                            _actionButton.cooldownTimerText.gameObject.SetActive(false);
                        LightLogger.Log($"[AbilityButton] 冷却结束: {Config.Label}");
                    }
                    else
                    {
                        // 原版 SetCoolDown 计算百分比 = timer/maxTimer
                        // 直接使用 SetCooldownFill 确保正确
                        float percent = _cooldownTimer / Config.Cooldown;
                        _actionButton?.SetCooldownFill(percent);
                        if (_actionButton?.cooldownTimerText != null)
                        {
                            _actionButton.cooldownTimerText.text = Mathf.CeilToInt(_cooldownTimer).ToString();
                            _actionButton.cooldownTimerText.gameObject.SetActive(true);
                        }
                    }
                }
                else
                {
                    // 非冷却状态确保清除
                    _actionButton?.SetCooldownFill(0f);
                }

                // 可见性
                bool shouldShow;
                if (Config.AlwaysShow)
                    shouldShow = _hudActive && Config.CanShow();
                else
                    shouldShow = _hudActive && _player.IsLocal && !_player.IsDead
                        && MeetingHud.Instance == null && Config.CanShow();

                if (_gameObject.activeSelf != shouldShow)
                    _gameObject.SetActive(shouldShow);

                // 可用性
                if (shouldShow)
                {
                    bool canUse = Config.CanUse() && !_inCooldown && (!HasLimitedUses || _usesLeft > 0);
                    if (canUse) _actionButton?.SetEnabled();
                    else _actionButton?.SetDisabled();
                }

                // 按键
                if (Config.Hotkey != KeyCode.None && Input.GetKeyDown(Config.Hotkey))
                {
                    if (_gameObject.activeSelf && Config.CanUse() && !_inCooldown && (!HasLimitedUses || _usesLeft > 0))
                        HandleClick();
                }
            }
            catch (Exception ex)
            {
                LightLogger.LogError("[AbilityButton.Update]", ex);
            }
        }

        public void Release()
        {
            try
            {
                if (_gameObject != null) Object.Destroy(_gameObject);
            }
            catch (Exception ex)
            {
                LightLogger.LogError("[AbilityButton.Release]", ex);
            }
        }

        void IGameOperator.OnReleased() { }
    }

    // =====================================================================
    // EffectButton — 效果按钮
    // =====================================================================

    public class EffectButton : AbilityButton
    {
        private new EffectButtonConfig Config => (EffectButtonConfig)base.Config;

        private float _effectTimer;
        private bool _inEffect;

        private static readonly UnityEngine.Color EffectColor = new(0f, 1f, 0f, 1f);
        private static readonly UnityEngine.Color NormalColor = UnityEngine.Color.white;

        public bool IsInEffect => _inEffect;
        public float EffectTimeRemaining => _effectTimer;

        private Action _onEffectStart;
        private Action _onEffectEnd;

        public Action OnEffectStart { set => _onEffectStart = value; }
        public Action OnEffectEnd { set => _onEffectEnd = value; }

        public EffectButton(ILifespan lifespan, Player player, EffectButtonConfig config, Action onClick)
            : base(lifespan, player, config, onClick) { }

        protected override void HandleClick()
        {
            try
            {
                LightLogger.Log($"[EffectButton] HandleClick: {Config.Label}, inEffect={_inEffect}, inCooldown={_inCooldown}");

                if (_inEffect && Config.IsToggle)
                {
                    InterruptEffect();
                    return;
                }

                if (_inCooldown) return;
                if (!Config.CanUse()) return;
                if (HasLimitedUses && _usesLeft <= 0) return;

                OnClickHandler?.Invoke();

                if (HasLimitedUses)
                {
                    _usesLeft--;
                    _actionButton.SetUsesRemaining(_usesLeft);
                }

                if (Config.EffectDuration > 0f) StartEffect();
                if (Config.Cooldown > 0f) StartCooldown();
            }
            catch (Exception ex)
            {
                LightLogger.LogError("[AbilityButton.HandleClick]", ex);
            }
        }

        public void StartEffect()
        {
            try
            {
                _effectTimer = Config.EffectDuration;
                _inEffect = true;
                _onEffectStart?.Invoke();
                if (Config.ShowEffectCountdown && _actionButton?.buttonLabelText != null)
                    _actionButton.buttonLabelText.color = EffectColor;
            }
            catch (Exception ex)
            {
                LightLogger.LogError("[AbilityButton.StartEffect]", ex);
            }
        }

        public void InterruptEffect()
        {
            try
            {
                if (!_inEffect) return;
                _inEffect = false;
                _effectTimer = 0f;
                _onEffectEnd?.Invoke();
                if (Config.ShowEffectCountdown && _actionButton?.buttonLabelText != null)
                {
                    _actionButton.buttonLabelText.color = NormalColor;
                    _actionButton.buttonLabelText.text = Config.ResolvedLabel;
                }
            }
            catch (Exception ex)
            {
                LightLogger.LogError("[AbilityButton.InterruptEffect]", ex);
            }
        }

        public override void Update()
        {
            try
            {
                if (IsDeadObject) return;

                if (_inEffect)
                {
                    _effectTimer -= Time.deltaTime;

                    if (Config.ShowEffectCountdown && _actionButton?.buttonLabelText != null)
                        _actionButton.buttonLabelText.text = Mathf.CeilToInt(_effectTimer).ToString();

                    // 效果期间：冷却优先显示，否则显示效果进度
                    if (_inCooldown)
                    {
                        float cdPercent = _cooldownTimer / Config.Cooldown;
                        _actionButton?.SetCooldownFill(cdPercent);
                    }
                    else
                    {
                        // 效果进度：从满到空
                        float fill = _effectTimer / Config.EffectDuration;
                        _actionButton?.SetCooldownFill(fill);
                        if (_actionButton?.cooldownTimerText != null)
                        {
                            _actionButton.cooldownTimerText.text = Mathf.CeilToInt(_effectTimer).ToString();
                            _actionButton.cooldownTimerText.gameObject.SetActive(true);
                        }
                    }

                    if (_effectTimer <= 0f)
                    {
                        _inEffect = false;
                        _effectTimer = 0f;
                        _onEffectEnd?.Invoke();
                        if (Config.ShowEffectCountdown && _actionButton?.buttonLabelText != null)
                        {
                            _actionButton.buttonLabelText.color = NormalColor;
                            _actionButton.buttonLabelText.text = Config.Label;
                        }
                        _actionButton?.SetCooldownFill(0f);
                        _actionButton!.cooldownTimerText.gameObject.SetActive(false);
                    }
                }
                else
                {
                    // 普通冷却
                    if (_inCooldown)
                    {
                        _cooldownTimer -= Time.deltaTime;
                        if (_cooldownTimer <= 0f)
                        {
                            _cooldownTimer = 0f;
                            _inCooldown = false;
                            _actionButton?.SetCooldownFill(0f);
                            _actionButton!.cooldownTimerText.gameObject.SetActive(false);
                        }
                        else
                        {
                            float percent = _cooldownTimer / Config.Cooldown;
                            _actionButton?.SetCooldownFill(percent);
                            if (_actionButton?.cooldownTimerText != null)
                            {
                                _actionButton.cooldownTimerText.text = Mathf.CeilToInt(_cooldownTimer).ToString();
                                _actionButton.cooldownTimerText.gameObject.SetActive(true);
                            }
                        }
                    }
                }

                bool shouldShow;
                if (Config.AlwaysShow)
                    shouldShow = _hudActive && Config.CanShow();
                else
                    shouldShow = _hudActive && _player.IsLocal && !_player.IsDead
                        && MeetingHud.Instance == null && Config.CanShow();

                if (_gameObject.activeSelf != shouldShow)
                    _gameObject.SetActive(shouldShow);

                if (shouldShow)
                {
                    bool canUse = _inEffect || (Config.CanUse() && !_inCooldown && (!HasLimitedUses || _usesLeft > 0));
                    if (canUse) _actionButton?.SetEnabled();
                    else _actionButton?.SetDisabled();
                }

                if (Config.Hotkey != KeyCode.None && Input.GetKeyDown(Config.Hotkey))
                {
                    if (_gameObject.activeSelf)
                        HandleClick();
                }
            }
            catch (Exception ex)
            {
                LightLogger.LogError("[AbilityButton.Update]", ex);
            }
        }
    }

    // =====================================================================
    // Harmony 补丁
    // =====================================================================

    [HarmonyPatch(typeof(HudManager), nameof(HudManager.Start))]
    public static class HudManagerStartPatch
    {
        public static void Postfix()
        {
            try
            {
                AbilityButtonManager.CreateAllPending();
            }
            catch (System.Exception ex)
            {
                LightLogger.LogWarning("[Light] HudManagerStartPatch.Postfix NRE: " + ex.Message + "\n" + ex.StackTrace);
            }
        }
    }

    [HarmonyPatch(typeof(HudManager), nameof(HudManager.SetHudActive),
        typeof(PlayerControl), typeof(RoleBehaviour), typeof(bool))]
    public static class HudManagerSetHudActivePatch
    {
        public static void Postfix(bool isActive)
        {
            try
            {
                AbilityButtonManager.SetAllVisible(isActive);
            }
            catch (System.Exception ex)
            {
                LightLogger.LogWarning("[Light] HudManagerSetHudActivePatch.Postfix NRE: " + ex.Message + "\n" + ex.StackTrace);
            }
        }
    }

    [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.FixedUpdate))]
    public static class PlayerControlFixedUpdatePatch
    {
        public static void Postfix(PlayerControl __instance)
        {
            try
            {
                if (!__instance.AmOwner) return;
                AbilityButtonManager.UpdateAll();
            }
            catch (System.Exception ex)
            {
                LightLogger.LogWarning("[Light] PlayerControlFixedUpdatePatch.Postfix NRE: " + ex.Message + "\n" + ex.StackTrace);
            }
        }
    }
}
