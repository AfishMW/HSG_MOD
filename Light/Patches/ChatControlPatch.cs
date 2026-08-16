using AmongUs.Data;
using LightInDark.Language;
using LightInDark.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Light.Patches;

[HarmonyPatch(typeof(ChatController), nameof(ChatController.Update))]
public class ChatControlPatch
{
    [HarmonyPrefix]
    public static void NoQuickChat_Prefix()
    {
        // 如果房主闲的开了个快速聊天那就改成自由聊天
        if (AmongUsClient.Instance.AmHost && DataManager.Settings.Multiplayer.ChatMode == InnerNet.QuickChatModes.QuickChatOnly)
            DataManager.Settings.Multiplayer.ChatMode = InnerNet.QuickChatModes.FreeChatOrQuickChat;
    }
    [HarmonyPostfix]
    public static void ChatFixer_Postfix(ChatController __instance)
    {
        if (!__instance.freeChatField.textArea.hasFocus) return;
        if (Input.GetKeyDown(KeyCode.UpArrow) && ChatCommands.PatchManager.HistoryManager.Count > 0)
        {
            string text = ChatCommands.PatchManager.HistoryManager.MoveUp();
            if (text != null)
                __instance.freeChatField.textArea.SetText(text);
        }
        else if (Input.GetKeyDown(KeyCode.DownArrow) && ChatCommands.PatchManager.HistoryManager.Count > 0)
        {
            string text = ChatCommands.PatchManager.HistoryManager.MoveDown();
            __instance.freeChatField.textArea.SetText(text ?? "");
        }
        if(Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl))
        {
            string origText = __instance.freeChatField.textArea.text;
            if (Input.GetKeyDown(KeyCode.C))
            {
                ClipboardHelper.PutClipboardString(origText);
            }
            else if (Input.GetKeyDown(KeyCode.V))
            {
                ClipboardHelper.PutClipboardString($"{origText}{GUIUtility.systemCopyBuffer}");
            }
            else if (Input.GetKeyDown(KeyCode.X))
            {
                ClipboardHelper.PutClipboardString(origText);
                __instance.freeChatField.textArea.SetText("");
            }
            else if (Input.GetKeyDown(KeyCode.H) && LightUtils.IsAprilDay())
            {
                ChatCommands.PatchManager.SendLocalMessage(Language.Translate(key: "April.joke1", fallback: "Happy birthday to you!"));
            }
        }
        
    }
}
[HarmonyPatch(typeof(FreeChatInputField), nameof(FreeChatInputField.UpdateCharCount))]
public class UpdateCharCountPatch
{
    public static void Postfix(FreeChatInputField __instance)
    {
        int length = __instance.textArea.text.Length;
        __instance.charCountText.SetText(length <= 0 ? LightPlugin.ColorData.ChatText : $"{length}/{__instance.textArea.characterLimit}");
        __instance.charCountText.enableWordWrapping = false;
    }
}

public class ChatHistoryManager
{
    private readonly List<string> _history = new();
    private int _currentIndex = 0;

    public int Count => _history.Count;

    public void AddMessage(string text)
    {
        if (_history.Count == 0 || _history[^1] != text)
            _history.Add(text);
        ResetSelection();
    }

    /// <summary> 向上翻历史</summary>
    public string MoveUp()
    {
        if (_history.Count == 0) return null;
        if (_currentIndex > 0)
            _currentIndex--;
        return _history[_currentIndex];
    }

    /// <summary> 向下翻历史</summary>
    public string MoveDown()
    {
        if (_history.Count == 0) return null;
        if (_currentIndex < _history.Count - 1)
        {
            _currentIndex++;
            return _history[_currentIndex];
        }
        else
        {
            ResetSelection();
            return string.Empty;
        }
    }
    public void ResetSelection()
    {
        _currentIndex = _history.Count;
    }
    public void Clear()
    {
        _history.Clear();
        ResetSelection();
    }
}