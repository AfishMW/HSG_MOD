using Nebula.Modifications;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nebula.Patches;

/// <summary>
/// PlayerAnimationsのBodyTypeが更新されたら新たな値をキャッシュする。
/// </summary>
[HarmonyPatch(typeof(PlayerAnimations), nameof(PlayerAnimations.SetBodyType))]
internal class PlayerAnimationsSetBodyTypePatch
{
    static void Postfix(PlayerAnimations __instance)
    {
        var myId = __instance.GetInstanceIdFast();
        if(GamePlayer.AllPlayers.Find(p => (p as PlayerModInfo)?.MyAnimations.AnimationsInstanceId == myId, out var p))
        {
            (p as PlayerModInfo)?.MyAnimations.UpdateAnimationGroup(__instance.group);
        }
    }
}
