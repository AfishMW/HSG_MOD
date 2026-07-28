namespace Nebula.Extensions;


static public class LayerExpansion
{
    static int? defaultLayer = null;
    static int? shortObjectsLayer = null;
    static int? objectsLayer = null;
    static int? playersLayer = null;
    static int? ghostLayer = null;
    static int? uiLayer = null;
    static int? shipLayer = null;
    static int? shadowLayer = null;
    static int? drawShadowsLayer = null;

    static public int GetDefaultLayer()
    {
        defaultLayer ??= LayerMask.NameToLayer("Default");
        return defaultLayer.Value;
    }

    static public int GetShortObjectsLayer()
    {
        shortObjectsLayer ??= LayerMask.NameToLayer("ShortObjects");
        return shortObjectsLayer.Value;
    }

    static public int GetObjectsLayer()
    {
        objectsLayer ??= LayerMask.NameToLayer("Objects");
        return objectsLayer.Value;
    }

    static public int GetPlayersLayer()
    {
        playersLayer ??= LayerMask.NameToLayer("Players");
        return playersLayer.Value;
    }

    static public int GetGhostLayer()
    {
        ghostLayer ??= LayerMask.NameToLayer("Ghost");
        return ghostLayer.Value;
    }

    static public int GetUILayer()
    {
        uiLayer ??= LayerMask.NameToLayer("UI");
        return uiLayer.Value;
    }

    static public int GetShipLayer()
    {
        shipLayer ??= LayerMask.NameToLayer("Ship");
        return shipLayer.Value;
    }

    static public int GetShadowLayer()
    {
        shadowLayer ??= LayerMask.NameToLayer("Shadow");
        return shadowLayer.Value;
    }

    static public int GetDrawShadowsLayer()
    {
        drawShadowsLayer ??= LayerMask.NameToLayer("DrawShadows");
        return drawShadowsLayer.Value;
    }

    static public int GetShadowObjectsLayer()
    {
        return 30;
    }

    static public int GetArrowLayer()
    {
        return 29;
    }

    static public int GetRaiderColliderLayer()
    {
        return 28;
    }

    static public int GetVanillaShadowLightLayer()
    {
        return 27;
    }

    static public int GetHookshotWallLayer()
    {
        return 26;
    }

    //プレイヤーを描画しないカメラに映らず、影には映るレイヤーです。
    static public int GetPlayerWithShadowLayer()
    {
        return 25;
    }

    static public int GetLayerMask(params int[] layer)
    {
        int result = 0;
        foreach (var l in layer) result |= 1 << l;
        return result;
    }
}

