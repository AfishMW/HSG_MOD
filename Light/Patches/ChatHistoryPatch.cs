using Light.Utilities;
namespace Light.Patches;

[HarmonyPatch(typeof(ChatController),nameof(ChatController.AddChat))]
public static class ChatHistoryPatch
{
    [HarmonyPrefix]
    public static void CHPrefix(ChatController __instance,PlayerControl sourcePlayer,string chatText)
    {
        ChatHistoryLogUtils.ChatInfo(sourcePlayer,chatText);
    }
}