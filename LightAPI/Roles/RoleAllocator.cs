using System.Collections.Generic;
using LightInDark.Configuration;

namespace LightInDark.Roles;

/// <summary>分配表：记录每名玩家的职业分配结果（实现在主插件）</summary>
public interface IRoleTable
{
    void SetRole(byte playerId, DefinedRole role, int[] arguments = null);
    IEnumerable<(byte PlayerId, DefinedRole Role)> GetPlayers(RoleCategory category);
    void Determine();
}

/// <summary>职业分配器接口（实现在主插件）</summary>
public interface IRoleAllocator
{
    void Assign(List<byte> impostors, List<byte> others);
}
