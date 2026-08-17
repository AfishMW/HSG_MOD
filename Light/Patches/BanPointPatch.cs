using AmongUs.Data.Player;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Light.Patches;

/// <summary>
/// 树懒你咋想的游戏一开始BanPoint就+1，游戏结束了-1.5啊。  
/// 你没考虑过土豆吗。
/// </summary>
[HarmonyPatch(typeof(PlayerBanData),nameof(PlayerBanData.BanPoints),MethodType.Setter)]
public static class BanPointPatch
{
    [HarmonyPrefix]
    public static bool SetBanPoints_Prefix(float value)
    {
        return false;
    }
}
