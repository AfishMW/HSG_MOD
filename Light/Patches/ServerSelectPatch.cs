using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using LightInDark.Core;
using LightInDark.Utilities;

namespace Light.Patches;

[HarmonyPatch(typeof(CreateGameOptions), nameof(CreateGameOptions.Start))]
public class ServerSelectPatch
{
    public static void Postfix(CreateGameOptions __instance)
    {
        try
        {
            __instance.tooltip.transform.parent.gameObject.SetActive(false);
            __instance.SelectMode(0, true);
            __instance.modeButtons[0].transform.parent.gameObject.SetActive(false);
            __instance.mapPicker.transform.SetLocalY(-1.245f);
            __instance.capacityOption.transform.SetLocalY(-1.15f);
            __instance.levelButtons[0].transform.parent.gameObject.SetActive(false);
            __instance.serverButton.transform.parent.SetLocalY(-1.84f);
            __instance.serverDropdown.transform.SetLocalY(-2.63f);
            __instance.capacityOption.ValidRange.max = LightUtils.IsCustomServer() ? 24 : 15;
            __instance.capacityOption.ValidRange.min = 4;
        }
        catch (System.Exception ex)
        {
            LightLogger.LogWarning("[Light] ServerSelectPatch.Postfix NRE: " + ex.Message + "\n" + ex.StackTrace);
        }
    }
}
