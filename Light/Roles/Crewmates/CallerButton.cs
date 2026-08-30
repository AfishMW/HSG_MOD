using LightInDark.Core;
using LightInDark.Roles;
using LightInDark.RPCs;
using Light.UI.Ability;
using System;
using UnityEngine;

namespace Light.Roles.Crewmates;

/// <summary>
/// Caller 技能按钮（Button 侧）：实际效果全部在此实现。
/// 通过基类的 GetRole()（role is Type 模式匹配）获得类型化角色引用。
/// </summary>
public class CallerButton : RoleAbilityButton<CallerRuntime>
{
    private CallerButton(CallerRuntime role, AbilityButtonConfig config, Action onClick)
        : base(role, config, onClick)
    {
    }

    /// <summary>由 CallerRuntime.OnActivated 调用：构建配置并创建按钮。</summary>
    public static void Create(CallerRuntime role)
    {
        try
        {
            if (role == null) return;

            var config = new AbilityButtonConfig
            {
                LabelKey = "Button.Caller.label",
                Hotkey = KeyCode.E,
                // 配置项在 Role 类里定义（[RoleOption]），自动扫描分配后直接读取
                Cooldown = Caller.Cooldown,
                CanUse = () => role.MyPlayer.IsDead == false && role.MyPlayer.Control != null && !role.MyPlayer.Control.inVent,
                CanShow = () => true,
                IsKillButton = false,
                AlwaysShow = false,
            };

            CallerButton button = null!;
            try
            {
                button = new CallerButton(role, config, () => UseAbility(button?.GetRole()));
            }
            catch (Exception ex)
            {
                // HUD 未就绪时转入 pending 队列，稍后由 AbilityButtonManager 创建
                LightLogger.LogWarning($"[CallerButton] 立即创建失败，转 pending: {ex.Message}");
                AbilityButtonManager.Register(role, role.MyPlayer, config, () => UseAbility(role));
            }
        }
        catch (Exception ex)
        {
            LightLogger.LogError("[CallerButton.Create]", ex);
        }
    }

    /// <summary>技能实际效果：角色引用来自按钮 GetRole()（role is Type 模式匹配）。</summary>
    private static void UseAbility(CallerRuntime role)
    {
        try
        {
            if (role == null || !role.AmOwner) return;
            if (role.MyPlayer?.Control == null) return;
            RpcDefinitions.RpcStartMeeting();
            LightLogger.Log("[Ability]Caller ability used.");
        }
        catch (Exception ex)
        {
            LightLogger.LogError("[CallerButton.UseAbility]", ex);
        }
    }
}
