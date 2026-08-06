namespace LightInDark.Configuration;

/// <summary>
/// 角色类别
/// </summary>
public enum RoleCategory
{
    Crewmate,
    Impostor,
    Neutral,
}

/// <summary>
/// 硬编码配置，暂不可更改。
/// 下一轮对话将接入 UI Preset 系统。
/// </summary>
public static class GameConfig
{
    // ---- 角色 ----
    public const int MaxCrewmateRoles = 2;
    public const int MaxImpostorRoles = 2;
    public const int MaxNeutralRoles = 1;

    // ---- 击杀 ----
    public const float DefaultKillCooldown = 25f;
    public const float InitialKillCooldown = 10f;

    // ---- 能力 ----
    public const float DefaultAbilityCooldown = 20f;
    public const float InitialAbilityCooldown = 15f;

    // ---- 会议 ----
    public const int MaxMeetings = 10;
    public const float DiscussionTime = 15f;
    public const float VotingTime = 120f;

    // ---- 任务 ----
    public const bool RandomizedWiring = false;
    public const int WiringSteps = 3;

    // ---- UI ----
    public const float WindowAnimDuration = 0.25f;
    public const float BlackScreenAlpha = 0.4226f;

    // ---- 调试 ----
    public const bool DebugMode = true;
    public const bool ShowRoleOnStart = false;
}
