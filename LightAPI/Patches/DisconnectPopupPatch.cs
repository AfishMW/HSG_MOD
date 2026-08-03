using HarmonyLib;
using UnityEngine;
using static LightInDark.Utilities.AmongUsEdited;

namespace LightInDark.Patches
{
    [HarmonyPatch(typeof(DisconnectPopup), nameof(DisconnectPopup.SetText))]
    public static class DisconnectPopupPatch
    {
        [HarmonyPrefix]
        public static bool Prefix(DisconnectPopup __instance)
        {
            if (string.IsNullOrEmpty(KickManager.kickReason))
                return true;
            float now = Time.realtimeSinceStartup;
            if (KickManager.kickReasonConsumeUntil > 0f)
            {
                if (now > KickManager.kickReasonConsumeUntil)
                {
                    KickManager.Clear();
                    return true;
                }
            }
            else if (now > KickManager.kickReasonWaitUntil)
            {
                KickManager.Clear();
                return true;
            }
            else
            {
                KickManager.kickReasonConsumeUntil = now + 3f;
            }
            __instance._textArea.text = KickManager.kickReason;
            __instance.OnTextChanged();
            return false;
        }
    }
}