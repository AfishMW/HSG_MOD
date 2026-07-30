using UnityEngine;

namespace Light.Utilities;

internal static class UnityHelper
{
    public static T CreateObject<T>(string objName, Transform? parent, Vector3 localPosition, int? layer = null) where T : Component
    {
        var obj = new GameObject(objName);
        var transform = obj.transform;
        transform.SetParent(parent);
        transform.localPosition = localPosition;
        transform.localScale = Vector3.one;
        if (layer.HasValue) obj.layer = layer.Value;
        else if (parent != null) obj.layer = parent.gameObject.layer;
        return obj.AddComponent<T>();
    }

    public static GameObject CreateObject(string objName, Transform? parent, Vector3 localPosition, int? layer = null)
    {
        var obj = new GameObject(objName);
        var transform = obj.transform;
        transform.SetParent(parent);
        transform.localPosition = localPosition;
        transform.localScale = Vector3.one;
        if (layer.HasValue) obj.layer = layer.Value;
        else if (parent != null) obj.layer = parent.gameObject.layer;
        return obj;
    }
}
