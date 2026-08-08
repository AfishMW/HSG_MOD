using HarmonyLib;
using Il2CppSystem.Linq.Expressions.Interpreter;
using InnerNet;
using LightInDark.Core;
using LightInDark.Game;
using LightInDark.Roles;
using Light.Roles.Crewmates;
using LightInDark.RPCs;
using Light.UI.HudUI;
using Light.UI.Window;
using LightInDark.Utilities;
using Steamworks;
using System;
using System.Linq;
using System.Linq.Expressions;
using UnityEngine;
using static UnityEngine.GraphicsBuffer;

namespace Light.ChatCommands;

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
                RpcDefinitions.Suicide(PlayerControl.LocalPlayer);
                __instance.freeChatField.Clear();
                return false;
            case "/showchat":
                RpcDefinitions.ShowChat();
                __instance.freeChatField.Clear();
                return false;
            case "/hidechat":
                RpcDefinitions.HideChat();
                __instance.freeChatField.Clear();
                return false;
            case "t":
                SendLocalMessage("t-Complate");
                __instance.freeChatField.Clear();
                return false;
            case "/sr":
                try
                {
                    var gil = LightInDark.Game.GameManager.Instance.LocalPlayer;
                    LightLogger.Log(gil.ToString());
                    if (gil == null) return false;
                    gil.SetRole(RoleRegistry.Get<Caller>());
                    __instance.freeChatField.Clear();
                    return false;
                }
                catch(Exception ex)
                {
                    LightLogger.LogError($"设置角色失败:{ex.Message}");
                    __instance.freeChatField.Clear();
                    return false;
                }
            case "/sw":
                try
                {
                    HudUI.OpenButtonWindow("测试窗口"
                        ,new HudUI.ButtonOption("按钮1", () => SendLocalMessage("按钮1被点击"))
                        , new HudUI.ButtonOption("按钮2", () => SendLocalMessage("按钮2被点击"))
                        );
                    __instance.freeChatField.Clear();
                    return false;
                }
                catch(Exception ex)
                {
                    LightLogger.LogWarning($"window异常：{ex.Message}");
                    __instance.freeChatField.Clear();
                    return false;
                }
            case "/code":
                int i = AmongUsClient.Instance.GameId;
                string code = GameCode.IntToGameNameV2(i);
                SendLocalMessage($"当前房间码为:{code}");
                __instance.freeChatField.Clear();
                return false;
            case "/kick":
                try
                {
                    if (!isHost) return false;
                    if (parts.Length < 2)
                    {
                        SendLocalMessage("请指定要踢出的玩家ID");
                        __instance.freeChatField.Clear();
                        return false;
                    }
                    PlayerControl target = null;
                    string targetName = parts[1];
                    foreach (var p in PlayerControl.AllPlayerControls)
                        if (p.Data.PlayerName.Contains(targetName, StringComparison.OrdinalIgnoreCase)) { target = p; break; }
                    if (target == null)
                    {
                        SendLocalMessage("未找到指定玩家");
                        __instance.freeChatField.Clear();
                        return false;
                    }
                    string reason = parts.Length >= 3 ? string.Join(' ', parts.Skip(2)) : "无";
                    AmongUsEdited.KickPlayer(target.ToLIDPlayer(), PlayerControl.LocalPlayer.name, reason);
                    __instance.freeChatField.Clear();
                    return false;
                }
                catch(Exception ex)
                {
                    LightLogger.LogWarning($"踢人失败:{ex.Message}");
                    __instance.freeChatField.Clear();
                    return false;
                }
        }

        return true;
    }
}
