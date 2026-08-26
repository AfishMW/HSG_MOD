using HarmonyLib;
using Hazel;
using InnerNet;
using LightInDark.Utilities;
using Light.Utilities;
using static LightInDark.Utilities.LightUtils;

namespace Light.Patches;

[HarmonyPatch(typeof(InnerNetClient), nameof(InnerNetClient.HandleGameData))]
public static class HandleGameDataPatch
{
    public static void Prefix(InnerNetClient __instance, MessageReader parentReader)
    {
        if (__instance.AmHost || __instance.NetworkMode != NetworkModes.OnlineGame)
            return;

        int startPos = parentReader.Position;
        try
        {
            while (parentReader.BytesRemaining > 0)
            {
                int length = parentReader.ReadPackedInt32();
                byte tag = parentReader.ReadByte();
                if (tag == byte.MaxValue)
                {
                    byte flag = parentReader.ReadByte();
                    if (flag == 0)
                    {
                        string reason = parentReader.ReadString();
                        KickHelper.SetPendingReason(__instance.ClientId, reason);
                    }
                    break;
                }
                else
                {
                    // 普通数据块：跳过剩余数据（长度-1 字节，因为已读 tag）
                    int remaining = length - 1;
                    if (remaining > 0)
                        parentReader.Position += remaining;
                }
            }
        }
        catch
        {
            // 解析异常时静默忽略，避免影响游戏主流程
        }
        finally
        {
            // 将读取位置复原，保证后续正常处理
            parentReader.Position = startPos;
        }
    }
}