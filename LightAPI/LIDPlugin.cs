global using Color = LightInDark.Color;
using BepInEx;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;
using LightInDark.Core;
using LightInDark.Patch;
using LightInDark.Events;
using Reactor;
using Reactor.Networking;
using Reactor.Networking.Attributes;
using System.Collections;
using UnityEngine;

namespace LightInDark;

[BepInPlugin("com.hvtxsvcmaomao.lid","Light in Dark","1.0.0")]
[BepInProcess("Among Us.exe")]
[BepInDependency(ReactorPlugin.Id)]
[ReactorModFlags(ModFlags.RequireOnAllClients)]
public partial class LIDPlugin : BasePlugin
{
    public Harmony Harmony { get; } = new("LightAPI.harmony");
    public override void Load()
    {
        LoadCommand();
        Harmony.PatchAll();
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
        if (!NeedShowFreeChat) return;
        HudManager.Instance.Chat.gameObject.SetActive(true);
    }
}
[HarmonyPatch(typeof(GameObject), "SetActive")]
public class Patch_GameObject_SetActive
{
    public static void Postfix(GameObject __instance, bool value)
    {
        string logMessage = $"[{Time.time}] GameObject '{__instance.name}' 被设置为 {(value ? "显示" : "隐藏")}";
        LightLogger.Log(logMessage);
    }
}
[HarmonyPatch(typeof(UnityEngine.Events.UnityEvent), "Invoke")]
public class Patch_UnityEvent_Invoke
{
    public static void Prefix(UnityEngine.Events.UnityEvent __instance)
    {
        LightLogger.Log($"一个UnityEvent被触发了: {__instance}");
    }
}
