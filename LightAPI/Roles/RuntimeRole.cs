using System;
using System.Collections.Generic;
using LightInDark.Ability;
using LightInDark.Core;
using LightInDark.Events;
using LightInDark.Game;
using TMPro;
using UnityEngine;
using Object = UnityEngine.Object;

namespace LightInDark.Roles
{
    /// <summary>
    /// 角色运行时实例，绑定到 Player。
    /// 角色名+任务数显示在玩家名字上方的 Info 子对象中。
    /// </summary>
    public abstract class RuntimeRole : IBindPlayer, IGameOperator, ILifespan
    {
        public DefinedRole Definition { get; }
        public Player MyPlayer { get; }
        public bool AmOwner => MyPlayer.AmOwner;

        protected List<IPlayerAbility> Abilities { get; } = new();

        public bool IsDeadObject => MyPlayer.IsDeadObject;

        private TextMeshPro? _infoText;

        protected RuntimeRole(DefinedRole definition, Player player, int[] arguments)
        {
            try
            {
                Definition = definition;
                MyPlayer = player;
                Arguments = arguments ?? System.Array.Empty<int>();
                this.Register(player);
                EventSystem.RegisterInstance(this);

                try { OnActivated(); }
                catch (System.Exception ex) { LightLogger.LogWarning($"[RuntimeRole] OnActivated 失败: {ex.Message}"); }

                try { UpdateNameDisplay(); }
                catch (System.Exception ex) { LightLogger.LogWarning($"[RuntimeRole] UpdateNameDisplay 失败: {ex.Message}"); }
            }
            catch (Exception ex)
            {
                LightLogger.LogError("RuntimeRole.RuntimeRole", ex);
            }
        }

        /// <summary>实例化参数（来自分配机或默认参数）</summary>
        public int[] Arguments { get; }

        protected virtual void OnActivated()
        {
            try
            {
            }
            catch (Exception ex)
            {
                LightLogger.LogError("RuntimeRole.OnActivated", ex);
            }
        }

        /// <summary>换职时清理逻辑入口（子类覆写）</summary>
        protected virtual void OnInactivated()
        {
            try
            {
            }
            catch (Exception ex)
            {
                LightLogger.LogError("RuntimeRole.OnInactivated", ex);
            }
        }

        /// <summary>使角色失活（换职/移除时调用）</summary>
        public void Inactivate()
        {
            try
            {
                try { OnInactivated(); }
                catch (System.Exception ex) { LightLogger.LogWarning($"[RuntimeRole] OnInactivated 失败: {ex.Message}"); }
                Release();
            }
            catch (Exception ex)
            {
                LightLogger.LogError("RuntimeRole.Inactivate", ex);
            }
        }

        protected void AddAbility(IPlayerAbility ability)
        {
            try
            {
                Abilities.Add(ability);
                ability.Register(this);
            }
            catch (Exception ex)
            {
                LightLogger.LogError("RuntimeRole.AddAbility", ex);
            }
        }

        public IEnumerable<IPlayerAbility> GetAbilities()
        {
            try
            {
                return Abilities;
            }
            catch (Exception ex)
            {
                LightLogger.LogError("RuntimeRole.GetAbilities", ex);
                return default;
            }
        }

        /// <summary>
        /// 更新玩家名字显示。
        /// - 名字颜色 = 角色颜色（仅自己）/ 白色（他人）
        /// - 名字上方创建 Info 子文本，显示：角色名 (已完成/总任务)
        /// </summary>
        public void UpdateNameDisplay()
        {
            try
            {
                var control = MyPlayer?.Control;
                if (control == null) return;
                if (control.cosmetics == null) return;
                if (control.cosmetics.nameText == null) return;

                try
                {
                    // 名字颜色 = 角色颜色
                    control.cosmetics.nameText.color = AmOwner
                        ? Definition.Color.ToUnityColor()
                        : UnityEngine.Color.white;

                    // 创建/获取 Info 子文本（显示在名字上方）
                    if (_infoText == null)
                    {
                        var nameText = control.cosmetics.nameText;
                        _infoText = Object.Instantiate(nameText, nameText.transform);
                        _infoText.gameObject.name = "Info";
                        _infoText.fontSize = nameText.fontSize * 0.75f;
                        // 放在名字上方，稍微靠近
                        _infoText.transform.localPosition = new Vector3(0f, 0.15f, 0f);
                        _infoText.alignment = TextAlignmentOptions.Bottom;
                        _infoText.enableWordWrapping = false;
                        _infoText.raycastTarget = false;
                    }

                    // 构建 Info 文本
                    string roleColorHex = ColorToHex(Definition.Color);
                    string roleStr = $"<color=#{roleColorHex}>{Definition.Name}</color>";

                    string taskStr = "";
                    if (control.Data?.Tasks != null && control.Data.Tasks.Count > 0)
                    {
                        int completed = 0;
                        int total = control.Data.Tasks.Count;
                        foreach (var task in control.Data.Tasks)
                        {
                            if (task != null && task.Complete) completed++;
                        }
                        taskStr = $" <color=#FAD934FF>({completed}/{total})</color>";
                    }

                    if (AmOwner)
                    {
                        _infoText.text = $"{roleStr}{taskStr}";
                        _infoText.gameObject.SetActive(true);
                    }
                    else
                    {
                        _infoText.text = "";
                        _infoText.gameObject.SetActive(false);
                    }
                }
                catch { }
            }
            catch (Exception ex)
            {
                LightLogger.LogError("RuntimeRole.UpdateNameDisplay", ex);
            }
        }

        private static string ColorToHex(Color color)
        {
            try
            {
                byte r = (byte)(color.R * 255f);
                byte g = (byte)(color.G * 255f);
                byte b = (byte)(color.B * 255f);
                byte a = (byte)(color.A * 255f);
                return $"{r:X2}{g:X2}{b:X2}{a:X2}";
            }
            catch (Exception ex)
            {
                LightLogger.LogError("RuntimeRole.ColorToHex", ex);
                return default;
            }
        }

        /// <summary>
        /// 任务完成事件监听。更新名字显示中的任务计数。
        /// </summary>
        [EventPriorityAttribute(0)]
        void OnTaskComplete(PlayerTaskCompleteEvent ev)
        {
            try
            {
                if (ev.Player == MyPlayer.Control)
                {
                    UpdateNameDisplay();
                }
            }
            catch (Exception ex)
            {
                LightLogger.LogError("RuntimeRole.OnTaskComplete", ex);
            }
        }

        public void Release()
        {
            try
            {
                EventSystem.UnregisterInstance(this);
                foreach (var ability in Abilities)
                    ability.Release();
                Abilities.Clear();

                try
                {
                    if (MyPlayer?.Control?.cosmetics?.nameText != null)
                        MyPlayer.Control.cosmetics.nameText.color = UnityEngine.Color.white;
                }
                catch { }

                if (_infoText != null)
                {
                    try { Object.Destroy(_infoText.gameObject); } catch { }
                    _infoText = null;
                }
            }
            catch (Exception ex)
            {
                LightLogger.LogError("RuntimeRole.Release", ex);
            }
        }

        void IGameOperator.OnReleased()
        {
            try
            {
            }
            catch (Exception ex)
            {
                LightLogger.LogError("RuntimeRole.OnReleased", ex);
            }
        }
    }
}
