using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using Hazel;
using LightInDark.Core;
using UnityEngine;

namespace LightInDark.RPCs
{
    /// <summary>标记一个静态方法为 RPC。定义即用。</summary>
    [AttributeUsage(AttributeTargets.Method)]
    public class LidRPCAttribute : Attribute
    {
        public bool OnlyHost { get; set; } = false;
        public bool Reliable { get; set; } = true;
        /// <summary>是否在发送端本地执行（默认 true，参考 Reactor Before）</summary>
        public bool LocalExecute { get; set; } = true;
    }

    /// <summary>
    /// RPC 自动注册器。
    /// 参考 Reactor CustomRpcManager：扫描 [LidRPC] 方法，
    /// Harmony Prefix 拦截调用，序列化参数发送，
    /// 同时在本地执行（LocalHandling.Before 模式）。
    /// </summary>
    public static class LidRpcRegistry
    {
        private static bool _initialized;

        private class RpcEntry
        {
            public string HashStr;
            public int Hash;
            public MethodInfo Method;
            public ParameterInfo[] Parameters;
            public bool OnlyHost;
            public bool Reliable;
            public bool LocalExecute;
        }

        private static readonly Dictionary<int, RpcEntry> _entries = new();

        public static void ScanAndPatch(Harmony harmony)
        {
            if (_initialized) return;
            _initialized = true;

            var assembly = Assembly.GetExecutingAssembly();

            foreach (var type in assembly.GetTypes())
            {
                foreach (var method in type.GetMethods(
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static))
                {
                    var attr = method.GetCustomAttribute<LidRPCAttribute>();
                    if (attr == null) continue;

                    var hashStr = $"{type.FullName}.{method.Name}";
                    var hash = hashStr.ComputeConstantHash();

                    var entry = new RpcEntry
                    {
                        HashStr = hashStr,
                        Hash = hash,
                        Method = method,
                        Parameters = method.GetParameters(),
                        OnlyHost = attr.OnlyHost,
                        Reliable = attr.Reliable,
                        LocalExecute = attr.LocalExecute,
                    };
                    _entries[hash] = entry;

                    // 注册接收处理器（接收端执行）
                    var localEntry = entry;
                    CustomRPC.Register(hashStr, reader =>
                    {
                        var args = DeserializeArgs(reader, localEntry.Parameters);
                        localEntry.Method.Invoke(null, args);
                    });

                    // Harmony Prefix：拦截调用，发送 RPC
                    var prefix = new HarmonyMethod(typeof(LidRpcRegistry), nameof(RpcPrefix));
                    harmony.Patch(method, prefix: prefix);

                    LightLogger.Log($"[LidRPC] 注册: {hashStr} (hash={hash}, {entry.Parameters.Length} params, local={entry.LocalExecute})");
                }
            }

            LightLogger.Log($"[LidRPC] 扫描完成，共 {_entries.Count} 个 RPC");
        }

        // 防止无限递归的标志
        private static bool _isLocalExecuting;

        /// <summary>
        /// Harmony Prefix — 拦截 [LidRPC] 方法调用。
        /// 参考 Reactor：发送 RPC + 本地执行。
        /// </summary>
        internal static bool RpcPrefix(MethodBase __originalMethod, object[] __args)
        {
            // 本地执行中：放行，执行方法体
            if (_isLocalExecuting)
                return true;

            var hashStr = $"{__originalMethod.DeclaringType?.FullName}.{__originalMethod.Name}";
            var hash = hashStr.ComputeConstantHash();

            if (!_entries.TryGetValue(hash, out var entry))
                return true;

            // 房主检查
            if (entry.OnlyHost && !AmongUsClient.Instance.AmHost)
                return false;

            // 序列化参数用于网络发送
            var payloadWriter = MessageWriter.Get(SendOption.Reliable);
            SerializeArgs(payloadWriter, __args, entry.Parameters);
            var payloadBytes = payloadWriter.ToByteArray(false);

            // 发送到网络
            CustomRPC.SendOnly(entry.HashStr, writer =>
            {
                writer.Write(payloadBytes, 0, payloadBytes.Length);
            }, entry.Reliable);

            // 本地执行（参考 Reactor LocalHandling.Before）
            if (entry.LocalExecute)
            {
                try
                {
                    _isLocalExecuting = true;
                    entry.Method.Invoke(null, __args);
                }
                catch (Exception ex)
                {
                    LightLogger.LogWarning($"[LidRPC] 本地执行失败: {hashStr}: {ex.Message}");
                }
                finally
                {
                    _isLocalExecuting = false;
                }
            }

            // 跳过原方法体（发送端不重复执行）
            return false;
        }

        // ---- 序列化 ----

        private static void SerializeArgs(MessageWriter writer, object[] args, ParameterInfo[] parameters)
        {
            for (int i = 0; i < parameters.Length; i++)
            {
                SerializeArg(writer, args[i], parameters[i].ParameterType);
            }
        }

        private static void SerializeArg(MessageWriter writer, object value, Type type)
        {
            if (type == typeof(byte)) writer.Write((byte)value);
            else if (type == typeof(int)) writer.Write((int)value);
            else if (type == typeof(float)) writer.Write((float)value);
            else if (type == typeof(bool)) writer.Write((bool)value);
            else if (type == typeof(string)) writer.Write((string?)value ?? "");
            else if (type == typeof(PlayerControl)) writer.WritePlayer((PlayerControl?)value);
            else if (type == typeof(Vector2)) writer.WriteVector2((Vector2)value);
            else if (type == typeof(Vector3)) writer.WriteVector3((Vector3)value);
            else throw new NotSupportedException($"[LidRPC] 不支持的类型: {type.Name}");
        }

        private static object?[] DeserializeArgs(MessageReader reader, ParameterInfo[] parameters)
        {
            var args = new object?[parameters.Length];
            for (int i = 0; i < parameters.Length; i++)
            {
                args[i] = DeserializeArg(reader, parameters[i].ParameterType);
            }
            return args;
        }

        private static object? DeserializeArg(MessageReader reader, Type type)
        {
            if (type == typeof(byte)) return reader.ReadByte();
            if (type == typeof(int)) return reader.ReadInt32();
            if (type == typeof(float)) return reader.ReadSingle();
            if (type == typeof(bool)) return reader.ReadBoolean();
            if (type == typeof(string)) return reader.ReadString();
            if (type == typeof(PlayerControl)) return reader.ReadPlayer();
            if (type == typeof(Vector2)) return reader.ReadVector2();
            if (type == typeof(Vector3)) return reader.ReadVector3();
            throw new NotSupportedException($"[LidRPC] 不支持的类型: {type.Name}");
        }
    }
}
