using LightInDark.Game;

namespace LightInDark.Abilities
{
    public abstract class AbstractPlayerAbility : DependentLifespan, IPlayerAbility
    {
        public Player MyPlayer { get; }
        public bool AmOwner => MyPlayer.AmOwner;
        public virtual bool HideKillButton => false;

        protected AbstractPlayerAbility(Player player)
        {
            MyPlayer = player;
            // 绑定到自身的寿命（当能力被释放时，会清理）
            // 注意：这里不自动注册到游戏，由调用者（角色）调用 Register
            // 但我们将 Register 延迟到 AddAbility 中
            // 或者这里可以调用 RegisterSelf，但需要确保寿命已绑定
            // 更安全：由外部调用 Register
        }

        public virtual void Release()
        {
            // 清理资源
        }
    }
}