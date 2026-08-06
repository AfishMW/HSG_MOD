using LightInDark.Events;
using LightInDark.UI;
using System.Collections.Generic;
using System.Linq;

namespace LightInDark.Game
{
    public class GameManager : SimpleLifespan, IGame
    {
        private static GameManager _instance;
        public static GameManager Instance => _instance ??= new GameManager();

        private List<IGameOperator> _entities = new();

        public Player LocalPlayer { get; private set; }

        private GameManager() { }

        public void Initialize()
        {
            _entities.Clear();
            if (PlayerControl.LocalPlayer != null)
            {
                LocalPlayer = new Player(PlayerControl.LocalPlayer);
                RegisterEntity(LocalPlayer, this);
            }
            foreach (var pc in PlayerControl.AllPlayerControls)
            {
                if (pc == PlayerControl.LocalPlayer) continue;
                var player = new Player(pc);
                RegisterEntity(player, this);
            }
        }

        public Player GetPlayer(byte playerId)
        {
            return _entities.OfType<Player>().FirstOrDefault(p => p.Control.PlayerId == playerId);
        }

        public IEnumerable<Player> AllPlayers => _entities.OfType<Player>();

        public void RegisterEntity(IGameOperator entity, ILifespan lifespan)
        {
            _entities.Add(entity);
        }

        public void UnregisterEntity(IGameOperator entity)
        {
            _entities.Remove(entity);
        }

        public void Update()
        {
            _entities.RemoveAll(e => e.IsDeadObject);
            // 按钮更新由 PlayerControl.FixedUpdate 补丁驱动（参考 MiraAPI）
            // 能力更新也由补丁驱动
        }

        public new void Release()
        {
            base.Release();
            _instance = null;
        }
    }
}
