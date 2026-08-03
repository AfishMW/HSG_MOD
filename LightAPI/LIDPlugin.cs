global using Color = LightInDark.Color;
global using UColor = UnityEngine.Color;
using BepInEx;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;
using Il2CppInterop.Runtime.Injection;
using LightInDark.Core;
using LightInDark.Events;
using LightInDark.Patches;
using LightInDark.UI;
using Reactor;
using Reactor.Networking;
using Reactor.Networking.Attributes;
using System.Collections;
using UnityEngine;
using Reactor.Utilities;


namespace LightInDark;

[BepInPlugin("com.hvtxsvcmaomao.lid","Light in Dark","1.0.0")]
[BepInProcess("Among Us.exe")]
[BepInDependency(ReactorPlugin.Id)]
[ReactorModFlags(ModFlags.RequireOnAllClients)]
public partial class LIDPlugin : BasePlugin
{
    public Harmony Harmony { get; } = new("LightAPI.harmony");
    public const string VisualVersion = "Dev 1.0.0";
    public const string RichVersion = "<color=#4FD1C5>ver</color> <color=#38B2AC>1.0.0</color>";
    public static string AUVersion;
    public override void Load()
    {
        Harmony.PatchAll();
        LoadCommand();
        ClassInjector.RegisterTypeInIl2Cpp<MetaScreen>();
        EventSystem.ScanAndRegisterAll();
        Language.Language.Load();
        
        LightLogger.Log("Mod加载成功");
    }
    static void LoadCommand()
    {
        var harmony = new Harmony("Light.cmd.harmony");
        var orig = AccessTools.Method(typeof(ChatController), "SendChat");
        if (orig == null)
        {
            return;
        }

        var prefixMethod = AccessTools.Method(typeof(PatchManager), nameof(PatchManager.OnSendChat));
        if (prefixMethod == null)
        {
            return;
        }

        var prefix = new HarmonyMethod(prefixMethod);
        harmony.Patch(orig, prefix);
    }
}
[HarmonyPatch(typeof(KeyboardJoystick),nameof(KeyboardJoystick.Update))]
public static class ShowChatPatch
{
    public static bool NeedShowFreeChat = false;
    [HarmonyPostfix]
    public static void Postfix()
    {
        if (HudManager.Instance?.Chat == null) return;
        //if (!NeedShowFreeChat) return;
        HudManager.Instance.Chat.gameObject.SetActive(true);
    }
}
[HarmonyPatch(typeof(GameManager), nameof(GameManager.StartGame))]
public static class GameManager_StartGame_Patch
{
    public static void Postfix()
    {
        LightLogger.Log("[游戏] 游戏开始，初始化 GameManager");
        Game.GameManager.Instance.Initialize();
        EventSystem.RunEvent(new GameStartEvent() { PlayerCount = PlayerControl.AllPlayerControls.Count });
    }
}

