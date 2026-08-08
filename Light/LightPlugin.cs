global using HarmonyLib;
global using System.Collections;
global using UnityEngine;
global using Color = LightInDark.Color;
global using UColor = UnityEngine.Color;
using BepInEx;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;
using Reactor;

namespace Light;

[BepInPlugin(Id, Name, Version)]
[BepInProcess("Among Us.exe")]
[BepInDependency(ReactorPlugin.Id)]
public partial class LightPlugin : BasePlugin
{
    public const string Id = "com.light.inthedark";
    public const string Name = "LightInTheDark";
    public const string Version = "1.0.0.0";

    public const string VisualVersion = "v1.0.0.0";

    public Harmony Harmony { get; } = new(Id);

    // 静态日志引用，供 Patch 类使用
    internal static ManualLogSource StaticLog { get; private set; } = null!;

    public override void Load()
    {
        StaticLog = Log;
        Harmony.PatchAll();
        Log.LogInfo($"模组 {Name} v{Version} 已加载！");
    }
}
