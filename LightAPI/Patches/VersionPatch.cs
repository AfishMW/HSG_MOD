using HarmonyLib;
namespace LightInDark.Patches;

[HarmonyPatch(typeof(VersionShower), nameof(VersionShower.Start))]
public static class VersionPatch
{
    public static void Postfix(VersionShower __instance)
    {
        LIDPlugin.AUVersion = __instance.text.text;
        string realVersion = LIDPlugin.AUVersion;
        string auVer = __instance.text.text;
        int i = auVer.IndexOf('(');
        if (i >= 0)
        {
            auVer = auVer[..i].Trim();
        }
        string lightText = "<color=#D3C678>L</color><color=#DCCF86>i</color>" +
            "<color=#CABC6E>g</color><color=#D5C87C>h</color><color=#C0B361>t</color>";

        string inText = "<color=#4FD1C5>I</color><color=#38B2AC>n</color>";

        string darkText = "<color=#9F7AEA>D</color><color=#805AD5>a</color>" +
            "<color=#6B46C1>r</color><color=#553C9A>k</color>";
        string richVersion = LIDPlugin.RichVersion;

        __instance.text.text = $"{lightText} {inText} {darkText} - {richVersion} | AU {auVer}";
        // #D3C678 #E6F6EB #050B2E
        //ModManager.Instance.ShowModStamp();
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
