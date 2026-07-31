using System;
using LightInDark.Game;
using UnityEngine;

namespace LightInDark.UI
{
    public static class AbilityButtonFactory
    {
        /// <summary>
        /// 创建 Nebula 风格的能力按钮（链式配置）
        /// </summary>
        public static IModAbilityButton CreateNormal(ILifespan lifespan, Player player)
        {
            return new ModAbilityButton(lifespan, player);
        }

        /// <summary>
        /// 快速创建（一次性配置）
        /// </summary>
        public static IModAbilityButton Create(
            ILifespan lifespan,
            Player player,
            string label,
            Sprite image,
            Action onClick,
            KeyCode key = KeyCode.None,
            float cooldown = 0f)
        {
            var button = new ModAbilityButton(lifespan, player)
                .SetLabel(label)
                .SetImage(image)
                .SetCooldown(cooldown)
                .BindKey(key);

            button.OnClick = onClick;
            return button.Build();
        }
    }
}