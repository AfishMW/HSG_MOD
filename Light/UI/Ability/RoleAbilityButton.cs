using System;
using LightInDark.Roles;

namespace Light.UI.Ability
{
    /// <summary>
    /// 角色技能按钮基类（Role/Button 拆分体系的 Button 侧）。
    /// 每个技能一个 Button 子类（如 CallerButton : RoleAbilityButton&lt;CallerRuntime&gt;），
    /// 实际效果全部写在 Button 里；Role 只负责定义角色与事件监听。
    /// </summary>
    public abstract class RoleAbilityButton<TRuntime> : AbilityButton where TRuntime : RuntimeRole
    {
        protected RoleAbilityButton(RuntimeRole role, AbilityButtonConfig config, Action onClick)
            : base(role, role.MyPlayer, config, onClick)
        {
        }

        /// <summary>
        /// 通过 role is Type 模式匹配获得本按钮绑定的类型化角色引用（按钮与角色的连接点）。
        /// 类型不符返回 null。
        /// </summary>
        public TRuntime GetRole() => _player.Role is TRuntime typed ? typed : null;
    }
}
