using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Light.Patches;

[HarmonyPatch(typeof(AccountTab),nameof(AccountTab.Awake))]
public static class FriendBarPatch
{
    public static void Prefix()
    {
        var bar = GameObject.Find("BarSprite");
        bar?.GetComponent<SpriteRenderer>().color = Color.clear;
        //这东西我找了十多分钟，树懒你是真能藏啊
    }
}