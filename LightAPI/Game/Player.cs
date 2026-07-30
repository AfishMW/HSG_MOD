

namespace LightInDark.Game
{
    public interface IPlayer
    {
        bool isDead { get; }
        string Name { get; }
        UnityEngine.Vector2 Position { get; }
        /// <summary>
        /// 自杀
        /// </summary>
        void Suicide();
        /// <summary>
        /// 击杀玩家
        /// </summary>
        void MurderPlayer(PlayerControl killer,PlayerControl victim);
    }
}
