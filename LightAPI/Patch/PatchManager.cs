using HarmonyLib;
using LightInDark.Core;
using LightInDark.Game;
using LightInDark.RPCs;
using LightInDark.UI;
using System;
using System.Linq.Expressions;
using UnityEngine;
using static UnityEngine.GraphicsBuffer;

namespace LightInDark.Patches;

[Harmony]
public class PatchManager
{
    public static bool IsHost(PlayerControl player) => AmongUsClient.Instance.AmHost;
    public static void SendLocalMessage(string msg)
    {
        var pc = PlayerControl.LocalPlayer;
        string orig = pc.name;
        pc.SetName("System");
        HudManager.Instance.Chat.AddChat(pc, msg);
        pc.SetName(orig);
    }
    public static bool SendNormalMessage(string msg)
    {
        PlayerControl.LocalPlayer.RpcSendChat(msg);
        return true;
    }
    public static bool OnSendChat(ChatController __instance)
    {
        bool isHost = IsHost(PlayerControl.LocalPlayer);
        string text = __instance.freeChatField.textArea.text.Trim();
        string raw = __instance.freeChatField.textArea.text;
        string[] parts = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0) return true;
        string cmd = parts[0].ToLower();
        switch (cmd)
        {
            case "/test":
                if (!isHost) return false;
                SendLocalMessage("指令测试");
                __instance.freeChatField.Clear();
                return false;
            case "/suicide":
                RPC.Suicide(PlayerControl.LocalPlayer);
                __instance.freeChatField.Clear();
                return false;
            case "/showchat":
                RPC.ShowChat(ShipStatus.Instance);
                __instance.freeChatField.Clear();
                return false;
            case "/hidechat":
                RPC.HideChat(ShipStatus.Instance);
                __instance.freeChatField.Clear();
                return false;
            case "t":
                SendLocalMessage("t-Complate");
                __instance.freeChatField.Clear();
                return false;
            case "/sr":
                var gil = Game.GameManager.Instance.LocalPlayer;
                if (gil == null) return false;
                gil.SetRole(new LightInDark.Roles.ExampleRole());
                SendLocalMessage("成功");
                __instance.freeChatField.Clear();
                return false;
            case "/sw":
                try
                {
                    MetaScreen.CreateWindow("Hello,World!");
                    __instance.freeChatField.Clear();
                    return false;
                }
                catch(Exception ex)
                {
                    LightLogger.LogWarning($"我看看你咋做到的？{ex.Message}");
                    return false;
                }
        }

        return true;
    }
}
