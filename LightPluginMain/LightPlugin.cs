global using HarmonyLib;
global using System.Collections;
global using UnityEngine;
using BepInEx;
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

    public override void Load()
    {
        Harmony.PatchAll();
        Log.LogInfo($"模组 {Name} v{Version} 已加载！");
    }
}
