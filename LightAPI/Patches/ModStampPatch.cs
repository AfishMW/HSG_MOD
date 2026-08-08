using HarmonyLib;
using LightInDark.Core;
using LightInDark.Utilities;
using Reactor.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace LightInDark.Patches;

[HarmonyPatch(typeof(ModManager), nameof(ModManager.ShowModStamp))]
public static class ModStampPatch
{
    public static void Postfix()
    {
        Dispatcher.Instance.Enqueue(() =>
        {
            var modStamp = GameObject.Find("ModStamp");
            if(modStamp == null)
            {
                LightLogger.LogWarning("[ModStampPatch] ModStamp not found!");
                return;
            }
            modStamp.transform.localScale = Vector3.one * 0.06f;
            modStamp.GetComponent<SpriteRenderer>().sprite = ResourceHelper.LoadSpriteFromResource("LightInDark.Resources.ModStamp.png");

        });

    }
}