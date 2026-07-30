using HarmonyLib;
using LightInDark.RPCs;
using System;
using UnityEngine;
using static UnityEngine.GraphicsBuffer;

namespace LightInDark.Patch;

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
            case "/kill":
                string tName = parts[1];
                PlayerControl target = null;
                foreach (var p in PlayerControl.AllPlayerControls)
                    if (p.Data.PlayerName.Contains(tName, StringComparison.OrdinalIgnoreCase)) { target = p; break; }
                if(target == null) return false;
                RPC.MurederPlayer(PlayerControl.LocalPlayer,target);
                SendNormalMessage("击杀了玩家！");
                __instance.freeChatField.Clear();
                return false;
            case "/sn":
                string tNames= parts[1];
                PlayerControl targets = null;
                foreach (var p in PlayerControl.AllPlayerControls)
                    if (p.Data.PlayerName.Contains(tNames, StringComparison.OrdinalIgnoreCase)) { targets = p; break; }
                if(targets == null) return false;
                targets.RpcSetName("你被模组修改了名字！：）");
                __instance.freeChatField.Clear();
                return false;
        }

        return true;
    }
}
