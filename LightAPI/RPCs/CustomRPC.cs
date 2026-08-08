using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using Hazel;
using InnerNet;
using LightInDark.Core;
using UnityEngine;

namespace LightInDark.RPCs
{
    // =====================================================================
    // 自定义 RPC 系统
    //
    // 使用 callId = byte.MaxValue (255)
    // 通过 Harmony patch 在 InnerNetObject.HandleRpc 层面拦截
    // 发送时同时执行本地逻辑
    // =====================================================================

    /// <summary>
    /// 自定义 RPC 管理器。
    /// callId = 255 (byte.MaxValue)。
    /// </summary>
    public static class CustomRPC
    {
        /// <summary>自定义 RPC 的 callId（使用 255）</summary>
        public const byte RpcCallId = byte.MaxValue;

        private static readonly Dictionary<int, Action<MessageReader>> _handlers = new();

        public static void Register(string hash, Action<MessageReader> handler)
        {
            _handlers[hash.ComputeConstantHash()] = handler;
            LightLogger.Log($"[CustomRPC] 注册: {hash} (hash={hash.ComputeConstantHash()})");
        }

        /// <summary>
        /// 发送 RPC 到所有客户端，并在本地立即执行。
        /// </summary>
        public static void Send(string hash, Action<MessageWriter> writer, bool reliable = true)
        {
            var client = AmongUsClient.Instance;
            if (client == null) return;

            var player = PlayerControl.LocalPlayer;
            if (player == null) return;

            int hashValue = hash.ComputeConstantHash();

            // 网络发送
            if (client.AmClient && client.GameState == InnerNetClient.GameStates.Started)
            {
                try
                {
                    var msgWriter = client.StartRpcImmediately(
                        player.NetId, RpcCallId,
                        reliable ? SendOption.Reliable : SendOption.None, -1);

                    msgWriter.Write(hashValue);
                    writer?.Invoke(msgWriter);

                    client.FinishRpcImmediately(msgWriter);
                }
                catch (Exception ex)
                {
                    LightLogger.LogWarning($"[CustomRPC] 发送失败: {hash}: {ex.Message}");
                }
            }

            // 本地执行
            // 在发送端也执行方法体
            // 不需要序列化/反序列化——直接用一个单独的 Action 调用
            // handler 接收 MessageReader，但我们无法轻松地本地构造它
            // 所以：LocalExecute 由 [LidRPC] 的 Prefix 处理（直接 Invoke 方法）
            // 这里只负责网络发送
            // LidRPC.Prefix 已经处理了本地执行
        }

        /// <summary>
        /// 发送 RPC 到指定玩家（不本地执行）
        /// </summary>
        public static void SendTo(PlayerControl target, string hash, Action<MessageWriter> writer, bool reliable = true)
        {
            var client = AmongUsClient.Instance;
            if (client == null) return;

            var player = PlayerControl.LocalPlayer;
            if (player == null) return;

            int hashValue = hash.ComputeConstantHash();

            try
            {
                var msgWriter = client.StartRpcImmediately(
                    player.NetId, RpcCallId,
                    reliable ? SendOption.Reliable : SendOption.None,
                    target.OwnerId);

                msgWriter.Write(hashValue);
                writer?.Invoke(msgWriter);

                client.FinishRpcImmediately(msgWriter);
            }
            catch (Exception ex)
            {
                LightLogger.LogWarning($"[CustomRPC] SendTo 失败: {hash}: {ex.Message}");
            }
        }

        /// <summary>
        /// 仅发送 RPC（不本地执行）
        /// </summary>
        public static void SendOnly(string hash, Action<MessageWriter> writer, bool reliable = true)
        {
            var client = AmongUsClient.Instance;
            if (client == null || !client.AmClient) return;

            var player = PlayerControl.LocalPlayer;
            if (player == null) return;

            int hashValue = hash.ComputeConstantHash();

            try
            {
                var msgWriter = client.StartRpcImmediately(
                    player.NetId, RpcCallId,
                    reliable ? SendOption.Reliable : SendOption.None, -1);

                msgWriter.Write(hashValue);
                writer?.Invoke(msgWriter);

                client.FinishRpcImmediately(msgWriter);
            }
            catch (Exception ex)
            {
                LightLogger.LogWarning($"[CustomRPC] SendOnly 失败: {hash}: {ex.Message}");
            }
        }

        /// <summary>
        /// 处理收到的自定义 RPC。
        /// 从 reader 读取 hash，查找 handler，执行。
        /// </summary>
        internal static void HandleRpc(MessageReader reader)
        {
            try
            {
                int hash = reader.ReadInt32();
                if (_handlers.TryGetValue(hash, out var handler))
                {
                    handler.Invoke(reader);
                }
                else
                {
                    LightLogger.LogWarning($"[CustomRPC] 未知 RPC hash: {hash}");
                }
            }
            catch (Exception ex)
            {
                LightLogger.LogWarning($"[CustomRPC] 处理失败: {ex.Message}\n{ex.StackTrace}");
            }
        }
    }

    // =====================================================================
    // Harmony Patch — 在 InnerNetObject 层面拦截 HandleRpc
    // =====================================================================

    /// <summary>
    /// 拦截所有 InnerNetObject 的 HandleRpc。
    /// </summary>
    [HarmonyPatch(typeof(InnerNetObject), nameof(InnerNetObject.HandleRpc))]
    public static class CustomRpcHandlePatch
    {
        public static bool Prefix(InnerNetObject __instance, byte callId, MessageReader reader)
        {
            if (callId != CustomRPC.RpcCallId) return true; // 不是我们的 RPC，正常处理

            CustomRPC.HandleRpc(reader);
            return false; // 阻止原版处理
        }
    }

    // =====================================================================
    // 扩展方法
    // =====================================================================

    public static class MessageWriterExtensions
    {
        public static void WritePlayer(this MessageWriter writer, PlayerControl player)
            => writer.Write(player?.PlayerId ?? byte.MaxValue);

        public static void WriteVector2(this MessageWriter writer, Vector2 value)
        {
            writer.Write(value.x);
            writer.Write(value.y);
        }

        public static void WriteVector3(this MessageWriter writer, Vector3 value)
        {
            writer.Write(value.x);
            writer.Write(value.y);
            writer.Write(value.z);
        }
    }

    public static class MessageReaderExtensions
    {
        public static PlayerControl ReadPlayer(this MessageReader reader)
        {
            byte playerId = reader.ReadByte();
            if (playerId == byte.MaxValue) return null;
            foreach (var pc in PlayerControl.AllPlayerControls)
                if (pc.PlayerId == playerId) return pc;
            return null;
        }

        public static Vector2 ReadVector2(this MessageReader reader)
            => new(reader.ReadSingle(), reader.ReadSingle());

        public static Vector3 ReadVector3(this MessageReader reader)
            => new(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
    }

    internal static class HashHelper
    {
        public static int ComputeConstantHash(this string str)
        {
            int hash = 0;
            foreach (char c in str)
                hash = (hash * 31) + c;
            return hash;
        }
    }
}
