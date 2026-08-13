using LightInDark.Core;
using System;

namespace LightInDark.Game
{
    public static class GameEntityExtension
    {
        /// <summary>
        /// 将实体注册到当前游戏，并绑定到指定的寿命
        /// </summary>
        public static T Register<T>(this T entity, ILifespan lifespan) where T : IGameOperator
        {
            try
            {
                var game = GameManager.Instance;
                if (game != null && !game.IsDeadObject)
                {
                    game.RegisterEntity(entity, lifespan);
                }
                return entity;
            }
            catch (Exception ex)
            {
                LightLogger.LogError("GameEntityExtension.Register", ex);
                return default;
            }
        }

        /// <summary>
        /// 将实体注册到当前游戏，并绑定到自身的寿命（即 entity 自身作为 ILifespan）
        /// </summary>
        public static T RegisterSelf<T>(this T entity) where T : IGameOperator, ILifespan
        {
            try
            {
                return entity.Register(entity);
            }
            catch (Exception ex)
            {
                LightLogger.LogError("GameEntityExtension.RegisterSelf", ex);
                return default;
            }
        }
    }
}
