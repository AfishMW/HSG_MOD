using System.Collections.Generic;

namespace LightInDark.Utilities
{
    /// <summary>
    /// 踢人原因辅助：<see cref="SetPendingReason"/> 记录待显示的踢出原因，
    /// <see cref="ConsumePendingReason"/> 在收到踢出/断线时取出并清除。
    /// </summary>
    public static class KickHelper
    {
        private static readonly Dictionary<int, string> _pending = new();

        public static void SetPendingReason(int clientId, string reason)
        {
            _pending[clientId] = reason;
        }

        public static string ConsumePendingReason(int clientId)
        {
            if (_pending.TryGetValue(clientId, out var reason))
            {
                _pending.Remove(clientId);
                return reason;
            }
            return null;
        }

        public static void Clear()
        {
            _pending.Clear();
        }
    }
}
