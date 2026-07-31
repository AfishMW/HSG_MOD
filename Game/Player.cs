

using LightInDark.Core;
using LightInDark.Events;
using LightInDark.Roles;
using LightInDark.RPCs;
using UnityEngine;

namespace LightInDark.Game
{
    public interface IPlayer
    {
        bool IsDead { get; }
        string Name { get; }
        UnityEngine.Vector2 Position { get; }
    }
    public interface IBindPlayer
    {
        Player MyPlayer { get; }
        bool AmOwner { get; }
    }

    public class Player : IPlayer, IBindPlayer, IGameOperator, ILifespan
    {
        public PlayerControl Control { get; private set; }

        public bool IsDead => Control?.Data?.IsDead ?? true;
        public string Name => Control?.Data?.PlayerName ?? "Unknown";
        public Vector2 Position => Control?.transform?.position ?? Vector2.zero;

        // IBindPlayer
        public Player MyPlayer => this;
        public bool AmOwner => Control == PlayerControl.LocalPlayer;

        public bool IsDeadObject => Control == null || Control.Data == null || Control.Data.Disconnected || Control.Data.IsDead;

        // 角色引用（稍后添加）
        public RuntimeRole Role { get; internal set; }

        public Player(PlayerControl control)
        {
            Control = control;
            EventSystem.RegisterInstance(this);
        }

        public void Suicide() => RPC.Suicide(Control);
        public void MurderPlayer(PlayerControl victim) => RPC.MurederPlayer(Control, victim);
        public void Release()
        {
            EventSystem.UnregisterInstance(this);
        }
        /// <summary>
        /// 切换角色（本地立即切换，并发送RPC同步）
        /// </summary>
        public void SetRole(DefinedRole newRole)
        {
            // 1. 释放旧角色
            if (Role != null)
            {
                Role.Release();
                Role = null;
            }

            // 2. 创建新角色
            Role = newRole.CreateInstance(this);

            // 3. 同步到其他客户端
            RPC.SyncRole(this.Control, newRole);

            LightLogger.Log($"[角色] {Name} 切换为 {newRole.Name}");
        }

        /// <summary>
        /// 仅本地设置角色（用于RPC接收）
        /// </summary>
        internal void SetRoleLocal(DefinedRole newRole)
        {
            if (Role != null)
            {
                Role.Release();
                Role = null;
            }

            Role = newRole.CreateInstance(this);
            LightLogger.Log($"[角色] {Name} (本地) 切换为 {newRole.Name}");
        }
    }

}
