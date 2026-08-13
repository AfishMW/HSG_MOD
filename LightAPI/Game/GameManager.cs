using LightInDark.Core;
using LightInDark.Events;
using LightInDark.UI;
using System;
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
            try
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
            catch (Exception ex)
            {
                LightLogger.LogError("GameManager.Initialize", ex);
            }
        }

        public Player GetPlayer(byte playerId)
        {
            try
            {
                return _entities.OfType<Player>().FirstOrDefault(p => p.Control.PlayerId == playerId);
            }
            catch (Exception ex)
            {
                LightLogger.LogError("GameManager.GetPlayer", ex);
                return null;
            }
        }

        public IEnumerable<Player> AllPlayers => _entities.OfType<Player>();

        public void RegisterEntity(IGameOperator entity, ILifespan lifespan)
        {
            try
            {
                _entities.Add(entity);
            }
            catch (Exception ex)
            {
                LightLogger.LogError("GameManager.RegisterEntity", ex);
            }
        }

        public void UnregisterEntity(IGameOperator entity)
        {
            try
            {
                _entities.Remove(entity);
            }
            catch (Exception ex)
            {
                LightLogger.LogError("GameManager.UnregisterEntity", ex);
            }
        }

        public void Update()
        {
            try
            {
                _entities.RemoveAll(e => e.IsDeadObject);
                // 按钮更新由 PlayerControl.FixedUpdate 补丁驱动
                // 能力更新也由补丁驱动
            }
            catch (Exception ex)
            {
                LightLogger.LogError("GameManager.Update", ex);
            }
        }

        public new void Release()
        {
            try
            {
                base.Release();
                _instance = null;
            }
            catch (Exception ex)
            {
                LightLogger.LogError("GameManager.Release", ex);
            }
        }
    }
}
