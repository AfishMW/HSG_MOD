using System.Collections.Generic;
using System.Linq;
using LightInDark.Configuration;
using LightInDark.Roles;
using LightInDark.RPCs;

namespace Light.Roles.Assignment;

/// <summary>分配表：记录每名玩家的职业分配结果，Determine 时逐玩家下发 RPC</summary>
public class RoleTable : IRoleTable
{
    private readonly Dictionary<byte, (DefinedRole Role, int[] Arguments)> _assignments = new();

    public void SetRole(byte playerId, DefinedRole role, int[] arguments = null)
        => _assignments[playerId] = (role, arguments ?? role.DefaultArguments);

    public IEnumerable<(byte PlayerId, DefinedRole Role)> GetPlayers(RoleCategory category)
        => _assignments.Where(kv => kv.Value.Role.Category == category).Select(kv => (kv.Key, kv.Value.Role));

    /// <summary>玩家是否已有分配</summary>
    public bool HasRole(byte playerId) => _assignments.ContainsKey(playerId);

    public void Determine()
    {
        foreach (var kv in _assignments)
            RpcDefinitions.SetRole(kv.Key, kv.Value.Role.Id, kv.Value.Arguments);
    }
}
