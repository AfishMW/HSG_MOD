using HarmonyLib;
using Light;
using LightInDark.Core;
using TMPro;
namespace Light.Patches;

[HarmonyPatch(typeof(VersionShower), nameof(VersionShower.Start))]
public static class VersionPatch
{
    public static void Postfix(VersionShower __instance)
    {
        try
        {
            LightPlugin.AUVersion = __instance.text.text;
            string auVer = __instance.text.text;
            int i = auVer.IndexOf('(');
            if (i >= 0) auVer = auVer[..i].Trim();
            string lightText = "<color=#D3C678>L</color><color=#DCCF86>i</color>" +
                               "<color=#CABC6E>g</color><color=#D5C87C>h</color><color=#C0B361>t</color>";
            string inText = "<color=#4FD1C5>I</color><color=#38B2AC>n</color>";
            string darkText = "<color=#9F7AEA>D</color><color=#805AD5>a</color>" +
                              "<color=#6B46C1>r</color><color=#553C9A>k</color>";
            string modVersion = $"{lightText} {inText} {darkText} - {LightPlugin.RichVersion}";
            auVer = "  Among Us " + auVer;

            __instance.text.alignment = TextAlignmentOptions.Left;
            __instance.text.text = auVer;

            // 原
            var vs = __instance.gameObject;
            var ap = vs.GetComponent<AspectPosition>();
            ap.Alignment = AspectPosition.EdgeAlignments.LeftBottom;
            ap.DistanceFromEdge = new Vector3(2.369f, -0.3f);
            ap.updateAlways = true;

            // mod ver
            var clone = UnityEngine.Object.Instantiate(__instance.text, __instance.transform.parent);
            clone.name = "LightModVersion";
            clone.text = modVersion;
            clone.alignment = TextAlignmentOptions.Left;
            clone.fontSize = 2.8f;
            clone.alpha = 0.8f;
            var apClone = clone.gameObject.AddComponent<AspectPosition>();
            apClone.Alignment = AspectPosition.EdgeAlignments.LeftBottom;
            apClone.DistanceFromEdge = new Vector3(2.369f,-0.45f);
            apClone.updateAlways = true;

            ModManager.Instance.ShowModStamp();
        }
        catch (Exception ex)
        {
            LightLogger.LogWarning("[Light] VersionPatch.Postfix NRE: " + ex.Message + "\n" + ex.StackTrace);
        }
    }
}


//[HarmonyPatch(typeof(FindGameButton), nameof(FindGameButton.OnClick))]
//public class FindGameButtonPatch
//{
//    public static bool Prefix(FindGameButton __instance)
//    {
//        var popup = DestroyableSingleton<DisconnectPopup>.Instance;
//        if (popup != null)
//        {
//            popup._textArea.text = "猪禁止游玩Among Us！";
//            popup.OnTextChanged();
//            popup.gameObject.SetActive(true);
//        }
//        return false;
//    }
//}
