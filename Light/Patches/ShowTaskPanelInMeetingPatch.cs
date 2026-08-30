using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Light.Patches;

// From : AUnlocker
[HarmonyPatch(typeof(HudManager), nameof(HudManager.SetHudActive), typeof(PlayerControl), typeof(RoleBehaviour), typeof(bool))]
public static class HudManager_SetHudActive
{
    public static void Postfix(HudManager __instance, RoleBehaviour role, bool isActive)
    {
        if (!LightPlugin.LightSettingsData.ShowTaskPanelInMeeting) return;
        if (!MeetingHud.Instance) return;

        // Modify openPosition so the task panel appears on top of the meeting screen
        var openPosition = __instance.TaskPanel.openPosition;
        openPosition.z = -20f;
        __instance.TaskPanel.openPosition = openPosition;

        __instance.TaskPanel.gameObject.SetActive(true);
    }
}