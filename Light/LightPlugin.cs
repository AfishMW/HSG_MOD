global using HarmonyLib;
global using System.Collections;
global using UnityEngine;
using BepInEx;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;
using LightInDark.Events;
using LightInDark.Language;
using LightInDark.Roles;
using LightInDark.RPCs;
using Light.ChatCommands;
using Light.Patches;
using Light.Roles.Crewmates;
using Light.Roles.Vanilla;
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

    /// <summary>彩色版本号（标题栏显示用）</summary>
    public const string RichVersion = "<color=#4FD1C5>ver</color> <color=#38B2AC>1.0.0</color>";
    /// <summary>原版 AU 版本号（由 VersionPatch 写入）</summary>
    public static string AUVersion;

    public Harmony Harmony { get; } = new(Id);

    // 静态日志引用，供 Patch 类使用
    internal static ManualLogSource StaticLog { get; private set; } = null!;

    public override void Load()
    {
        StaticLog = Log;
        // 注册角色（注册顺序即 RPC 序号）
        RoleRegistry.Register<Caller>();
        RoleRegistry.Register<VanillaImpostor>(VanillaImpostor.Instance);
        RoleRegistry.Register<VanillaCrewmate>(VanillaCrewmate.Instance);
        Harmony.PatchAll();
        LoadCommand();
        EventSystem.ScanAndRegisterAll();
        ExtractLanguageFiles();
        Language.Load();
        LidRpcRegistry.ScanAndPatch(Harmony);
        // 订阅 API 提供的聊天显示事件，更新聊天框状态
        RpcDefinitions.OnFreeChatStateChanged += show => ShowChatPatch.NeedShowFreeChat = show;
        Log.LogInfo($"模组 {Name} v{Version} 已加载！");
    }

    /// <summary>把嵌入的默认语言文件解压到 BepInEx/Language（已存在则不覆盖，玩家可编辑）</summary>
    private static void ExtractLanguageFiles()
    {
        try
        {
            string folder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Language");
            Directory.CreateDirectory(folder);
            var asm = typeof(LightPlugin).Assembly;
            foreach (var res in new[] { "Light.Resources.Language.SChinese.json", "Light.Resources.Language.English.json" })
            {
                // 资源名形如 ...Language.SChinese.json，取倒数第二个 '.' 之后的段作文件名
                string fileName = res.Substring(res.LastIndexOf('.', res.LastIndexOf('.') - 1) + 1);
                string path = Path.Combine(folder, fileName);
                if (File.Exists(path)) continue;
                using var stream = asm.GetManifestResourceStream(res);
                if (stream == null) continue;
                using var fs = File.Create(path);
                stream.CopyTo(fs);
            }
        }
        catch (Exception ex)
        {
            StaticLog.LogWarning($"解压语言文件失败：{ex.Message}");
        }
    }

    // 聊天指令拦截（ChatController.SendChat Prefix）
    private static void LoadCommand()
    {
        var harmony = new Harmony("Light.cmd.harmony");
        var orig = AccessTools.Method(typeof(ChatController), "SendChat");
        if (orig == null) return;
        var prefixMethod = AccessTools.Method(typeof(PatchManager), nameof(PatchManager.OnSendChat));
        if (prefixMethod == null) return;
        harmony.Patch(orig, new HarmonyMethod(prefixMethod));
    }
}
