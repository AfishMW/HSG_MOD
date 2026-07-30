
using LightInDark.RPCs;
using UnityEngine;

namespace LightInDark.Game
{
    public class PlayerManager : IPlayer
    {
        public PlayerControl Control{  get; private set; }
        public PlayerManager(PlayerControl control)
        {
            Control = control;
        }
        public bool isDead => Control.Data.IsDead;


        public string Name => Control.Data.PlayerName;
        public Vector2 Position => Control.transform.position;


        public void MurderPlayer(PlayerControl killer, PlayerControl victim)
        {
            RPC.MurederPlayer(killer, victim);
        }

        public void Suicide()
        {
            RPC.Suicide(PlayerControl.LocalPlayer);
        }
        
    }
    public class MyPlayer : IPlayer
    {
        private PlayerControl pc = PlayerControl.LocalPlayer;
        public bool isDead => pc.Data.IsDead;

        public string Name => pc.Data.PlayerName;

        public Vector2 Position => pc.transform.position;

        public void MurderPlayer(PlayerControl killer, PlayerControl victim)
        {
            RPC.MurederPlayer(killer,victim);
        }

        public void Suicide()
        {
            RPC.Suicide(PlayerControl.LocalPlayer,false);
        }
    }
}

