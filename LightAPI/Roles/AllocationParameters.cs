namespace LightInDark.Roles;

/// <summary>职业分配参数（代码内配置）</summary>
public struct AllocationParameters
{
    public int MaxCount;        // 最大出现数量（0 表示不参与分配）
    public int GuaranteedCount; // 必出数量（100% 概率）
    public int Chance;          // 出现概率 0-100
}
