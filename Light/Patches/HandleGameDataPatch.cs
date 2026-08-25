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
    public static void Prefix(InnerNetClient __instance, MessageReader reader)
    {
        // 閸欘亜顦╅悶鍡楁躬缁炬寧鐖堕幋蹇庣瑬娑撳秵妲搁幋澶稿瘜閻ㄥ嫭鍎忛崘纰夌礄閹恒儲鏁圭粩顖ょ礆
        if (__instance.AmHost || __instance.NetworkMode != NetworkModes.OnlineGame)
            return;

        // 鐟欙絾鐎介幍鈧張澶婄摍濞戝牊浼?
        // 閻㈠彉绨?reader 閸欘垵鍏橀崠鍛儓婢舵矮閲滅€涙劖绉烽幁顖ょ礉閹存垳婊戦棁鈧憰渚€浜堕崢?
        // 娣囨繂鐡ㄨぐ鎾冲娴ｅ秶鐤嗛敍灞间簰娓氬じ绗夎ぐ鍗炴惙閸氬海鐢绘径鍕倞
        int startPos = reader.Position;
        try
        {
            while (reader.BytesRemaining > 0)
            {
                // 鐠囪褰囩€涙劖绉烽幁顖氥仈闁煉绱檛ag 閸滃矂鏆辨惔锔肩吹娴ｅ棗鐤勯梽鍛瑐 GameData 鐎涙劖绉烽幁顖涚壐瀵骏绱板☉鍫熶紖闂€鍨閿涘牆褰夐梹鍖＄礆+ 閺嶅洨顒烽敍鍫濆綁闂€鍖＄吹閿涘绱?
                // 鐎圭偤妾稉濠傛躬 InnerNetClient.HandleGameData 娑擃叏绱濈€涙劖绉烽幁顖涚壐瀵繑妲搁敍姘舵毐鎼达讣绱檖acked int32閿?+ tag閿涘潌yte閿?+ 閸愬懎顔愰妴?
                // 閹存垳婊戦棁鈧憰浣瑰閸斻劏顕伴崣鏍モ偓?
                int length = reader.ReadPackedInt32();   // 鐎涙劖绉烽幁顖炴毐鎼?
                byte tag = reader.ReadByte();            // 鐎涙劖绉烽幁顖涚垼缁?
                if (tag == byte.MaxValue)
                {
                    // 鏉╂瑦妲搁幋鎴滄粦閼奉亜鐣炬稊澶屾畱鐎涙劖绉烽幁?
                    // 鐠囪褰囬弽鍥х箶
                    byte flag = reader.ReadByte();
                    if (flag == 0) // SetKickReason
                    {
                        string reason = reader.ReadString();
                        // 鐎涙ê鍋嶉崢鐔锋礈閿涘奔绗岃ぐ鎾冲鐎广垺鍩涚粩?ID 缂佹垵鐣鹃敍鍫滅稻鏉╂瑩鍣烽幋鎴滄粦娑撳秶鐓￠柆鎾存Ц閸欐垹绮扮拫浣烘畱閿涘苯娲滄稉楦款嚉濞戝牊浼呴弰顖氱暰閸氭垵褰傞柅浣虹舶閺堫剙顓归幋椋庮伂閻ㄥ嫸绱濋幍鈧禒銉ュ讲娴犮儳娲块幒銉ョ摠閸岊煉绱?
                        // 濞夈劍鍓伴敍姘劃濞戝牊浼呴弰顖炩偓姘崇箖 GameDataTo 閸欐垿鈧胶娈戦敍灞肩稻 HandleGameData 閺€璺哄煂閻ㄥ嫬鍑＄紒蹇旀Ц鐟欙絽瀵橀崥搴ｆ畱閿涘本鍨滄禒顑跨瑝闂団偓鐟曚礁鍙ц箛鍐窗閺?ID閿涘苯娲滄稉鍝勫涧閺堝娲伴弽鍥ь吂閹撮顏导姘暪閸掕埇鈧?
                        LightInDark.Utilities.KickHelper.SetPendingReason(__instance.ClientId, reason);
                    }
                    // 鐠哄疇绻冮崜鈺€缍戦崘鍛啇閿涘牆顩ч弸婊嗙箷閺堝绱?
                    // 閻㈠彉绨幋鎴滄粦瀹歌尙绮＄拠璇插絿娴滃棙澧嶉張澶婂敶鐎圭櫢绱欓崢鐔锋礈鐎涙顑佹稉鎻掑嚒鐠囦紮绱氶敍灞炬￥闂団偓妫版繂顦婚幙宥勭稊閵?
                    break; // 閸欘亜顦╅悶鍡曠娑?
                }
                else
                {
                    // 鐠哄疇绻冮崗鏈电稇鐎涙劖绉烽幁顖氬敶鐎圭櫢绱欐稉宥嗘Ц閹存垳婊戦惃鍕剁礉娑撳秴顦╅悶鍡礆
                    // 閹存垳婊戦棁鈧憰浣界儲鏉╁洤澧挎担娆忕摟閼哄偊绱板鑼病鐠囪褰囨禍?length 閸?tag閿涘苯澧挎担?length-1 鐎涙濡?
                    int remaining = length - 1;
                    if (remaining > 0)
                        reader.Position += remaining;
                }
            }
        }
        catch
        {
            // 韫囩晫鏆愮憴锝嗙€介柨娆掝嚖
        }
        finally
        {
            // 閹垹顦茬拠璇插絿娴ｅ秶鐤嗛敍宀冾唨閸樼喓澧楅柅鏄忕帆缂佈呯敾婢跺嫮鎮?
            reader.Position = startPos;
        }
    }
}