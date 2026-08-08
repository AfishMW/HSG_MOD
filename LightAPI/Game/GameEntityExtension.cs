using LightInDark.Core;

namespace LightInDark.Game
{
    public static class GameEntityExtension
    {
        /// <summary>
        /// 将实体注册到当前游戏，并绑定到指定的寿命
        /// </summary>
        public static T Register<T>(this T entity, ILifespan lifespan) where T : IGameOperator
        {
            var game = GameManager.Instance;
            if (game != null && !game.IsDeadObject)
            {
                game.RegisterEntity(entity, lifespan);
            }
            return entity;
        }

        /// <summary>
        /// 将实体注册到当前游戏，并绑定到自身的寿命（即 entity 自身作为 ILifespan）
        /// </summary>
        public static T RegisterSelf<T>(this T entity) where T : IGameOperator, ILifespan
        {
            return entity.Register(entity);
        }
    }
}