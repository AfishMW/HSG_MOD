using AmongUs.GameOptions;
using Discord;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using Il2CppSystem.Security.Cryptography;
using Virial.Events.Game;
using Virial.Game;
using static UnityEngine.UI.Image;

namespace Nebula.Patches;

[HarmonyPatch(typeof(ShipStatus), nameof(ShipStatus.CalculateLightRadius))]
public class LightPatch
{
    public static float lastRange = 1f;
    public static float lastRangeForDive = 1f;
    public static float LastCalculatedRange = 1f;


    public static bool Prefix(ref float __result, ShipStatus __instance, [HarmonyArgument(0)] NetworkedPlayerInfo? player)
    {
        if (__instance == null)
        {
            lastRange = 1f;
            return true;
        }

        var gameMap = ModSingleton<GameMap>.Instance;
        if (gameMap == null || gameMap.IsDeadObject)
        {
            __result = __instance.MaxLightRadius;
            return false;
        }
        if (player == null || player.IsDead)
        {
            __result = __instance.MaxLightRadius;
            return false;
        }

        if ((NebulaGameManager.Instance?.GameState ?? NebulaGameStates.NotStarted) == NebulaGameStates.NotStarted) return true;

        ISystemType? systemType = __instance.Systems.ContainsKey(SystemTypes.Electrical) ? __instance.Systems[SystemTypes.Electrical] : null;

        SwitchSystem? switchSystem = systemType?.TryCast<SwitchSystem>();

        float t = (float)(switchSystem?.Value ?? 255f) / 255f;


        var info = GamePlayer.LocalPlayer;
        var modinfo = info?.Unbox();
        bool hasImpostorVision = modinfo?.Role.HasImpostorVision ?? false;
        bool ignoreBlackOut = (modinfo?.Role.IgnoreBlackout ?? true) || (info?.AllAbilities.Any(a => a.IgnoreBlackout) ?? false);

        if (ignoreBlackOut) t = 1f;

        float radiusRate = Mathn.Lerp(gameMap.ShipMinLightRadius, gameMap.ShipMaxLightRadius, t);
        float range = hasImpostorVision ? gameMap.ImpostorLightMod : gameMap.CrewmateLightMod;
        var ev = GameOperatorManager.Instance?.Run(LightRangeUpdateEvent.Get(1f));
        float rate = ev?.LightRange ?? 1f;
        float quickRate = ev?.LightQuickRange ?? 1f;

        rate *= GamePlayer.LocalPlayer?.Unbox().CalcAttributeVal(PlayerAttributes.Eyesight) ?? 1f;

        lastRange -= (lastRange - rate).Delta(0.7f * (ev?.LightSpeed ?? 1f), 0.005f);
        __result = radiusRate * range * lastRange;
        LastCalculatedRange = __result;

        if (info?.IsDived ?? false)
            lastRangeForDive = 0f;
        else
            lastRangeForDive += (1f - lastRangeForDive).Delta(6f, 0.005f);

        __result *= lastRangeForDive;
        __result *= quickRate;

        return false;
    }
}

//影貫通

[HarmonyPatch(typeof(LightSourceGpuRenderer), nameof(LightSourceGpuRenderer.GPUShadows))]
public static class LightSourceGpuRendererPatch
{
    static Il2CppReferenceArray<Collider2D> origArray = null!;
    static Il2CppReferenceArray<Collider2D> zeroArray = new(0L);

    public static bool Prefix(LightSourceGpuRenderer __instance, [HarmonyArgument(0)] Vector2 origin)
    {
        //追加の前処理
        origArray = __instance.hits;
        if (NebulaGameManager.Instance?.IgnoreWalls ?? false) __instance.hits = zeroArray;

        //本来の処理の改変ここから
        __instance.ClearEdges();
        var lightSource = __instance.lightSource;
        var viewDistance3 = lightSource.ViewDistance * 3f;
        lightSource.LightChild.transform.localScale = new Vector3(viewDistance3, viewDistance3, 1f);
        Camera main = Camera.main;
        VVector2 vector = main.ModGameObject(false).Position - lightSource.ModGameObject(false).Position;
        float mainOrthographicSize = main.orthographicSize;
        VVector2 vector2 = new(mainOrthographicSize * main.aspect, mainOrthographicSize);
        float num = vector2.Magnitude + vector.Magnitude;
        float num2 = Mathn.Min(lightSource.ViewDistance, num);
        num2 *= NebulaGameManager.Instance?.WideCamera?.CurrentRate ?? 1f; //改変箇所
        foreach (NoShadowBehaviour noShadowBehaviour in LightSource.NoShadows.Values)
        {
            noShadowBehaviour.CheckHit(num2, origin);
        }
        int num3 = FastMethods.OverlapCircleNonAllocFast(origin, num2, __instance.hits, Constants.ShadowMask);

        var addEdgeFunc = __instance.AddEdge;
        for (int i = 0; i < num3; i++)
        {
            Collider2D collider2D = __instance.hits[i];
            NoShadowBehaviour noShadowBehaviour2;
            OneWayShadows oneWayShadows;
            if (!collider2D.isTrigger && (!LightSource.NoShadows.TryGetValue(collider2D.gameObject, out noShadowBehaviour2) || !(noShadowBehaviour2.hitOverride.EqualsFast(collider2D))) && (!LightSource.OneWayShadows.TryGetValue(collider2D.gameObject, out oneWayShadows) || !oneWayShadows.IsIgnored(lightSource)))
            {
                var colliderTransform = collider2D.ModGameObject();

                EdgeCollider2D? edgeCollider2D = collider2D.TryCast<EdgeCollider2D>();
                if (edgeCollider2D.AsBoolFast())
                {
                    var points = edgeCollider2D!.points;
                    var pointsLength = points.Length;
                    for (int j = 0; j < pointsLength - 1; j++)
                    {
                        Vector3 vector3 = colliderTransform.TransformPoint(points[j]);
                        Vector3 vector4 = colliderTransform.TransformPoint(points[j + 1]);
                        addEdgeFunc.Invoke(vector3, vector4);
                    }
                }
                else
                {
                    PolygonCollider2D? polygonCollider2D = collider2D.TryCast<PolygonCollider2D>();
                    if (polygonCollider2D.AsBoolFast())
                    {
                        var points2 = polygonCollider2D!.points;
                        var points2Length = points2.Length;
                        for (int k = 0; k < points2Length; k++)
                        {
                            int num4 = k + 1;
                            if (num4 == points2Length)
                            {
                                num4 = 0;
                            }
                            Vector3 vector5 = colliderTransform.TransformPoint(points2[k]);
                            Vector3 vector6 = colliderTransform.TransformPoint(points2[num4]);
                            addEdgeFunc.Invoke(vector5, vector6);
                        }
                    }
                    else
                    {
                        BoxCollider2D? boxCollider2D = collider2D.TryCast<BoxCollider2D>();
                        if (boxCollider2D.AsBoolFast())
                        {
                            VVector2 size = boxCollider2D!.size;
                            VVector2 vector7 = new(size.x / 2f, size.y / 2f);
                            VVector2 offset = boxCollider2D.offset;
                            VVector2 vector7_minus = offset - vector7;
                            VVector2 vector7_plus = offset + vector7;
                            VVector2 vector8 = colliderTransform.TransformPoint(vector7_minus.AsVector3());
                            VVector2 vector9 = colliderTransform.TransformPoint(vector7_plus.AsVector3());
                            VVector3 vector10 = vector8.AsVector3();
                            VVector3 vector11 = vector8.AsVector3();
                            vector11.y = vector9.y;
                            addEdgeFunc.Invoke(vector10, vector11);
                            vector10.y = vector9.y;
                            vector11.x = vector9.x;
                            addEdgeFunc.Invoke(vector10, vector11);
                            vector10 = vector9.AsVector3();
                            vector11.y = vector8.y;
                            addEdgeFunc.Invoke(vector10, vector11);
                            vector10.y = vector8.y;
                            vector11 = vector8.AsVector3();
                            addEdgeFunc.Invoke(vector10, vector11);
                        }
                    }
                }
            }
        }
        __instance.UpdateOccMesh();
        __instance.DrawOcclusion(num2);
        //本来の処理の改変ここまで

        //追加の後処理
        if (__instance.hits != origArray) __instance.hits = origArray;

        return false;
    }
}


[HarmonyPatch(typeof(LightSourceRaycastRenderer), nameof(LightSourceRaycastRenderer.RaycastShadows))]
public static class LightSourceRaycastRendererPatch
{
    private static Il2CppSystem.Collections.Generic.IComparer<LightSourceRaycastRenderer.VertInfo> comparer = LightSourceRaycastRenderer.AngleComparer.Instance.CastFast<Il2CppSystem.Collections.Generic.IComparer<LightSourceRaycastRenderer.VertInfo>>();
    public static bool Prefix(LightSourceRaycastRenderer __instance, [HarmonyArgument(0)] Vector2 origin)
    {
        var ignoreWalls = (NebulaGameManager.Instance?.IgnoreWalls ?? false);
        if (!ignoreWalls) return true; //壁を無視しないなら元の処理と同じ
        

        __instance.vertCount = 0;
        __instance.lightSource.LightChild.transform.localScale = new(1f, 1f, 1f);

        float validViewDistance = __instance.GetValidViewDistance();
        float num2 = validViewDistance * 1.05f;
        for (int l = 0; l < __instance.requiredDels.Length; l++)
        {
            Vector2 vector6 = num2 * __instance.requiredDels[l];

            float v_num = validViewDistance * 1.5f;
            var normalized = vector6.normalized;
            __instance.GetEmptyVert().Complete(normalized.x * v_num, normalized.y * v_num);
        }

        var vec = __instance.vec;
        var uvs = __instance.uvs;
        var verts = __instance.verts;
        var vertCount = __instance.vertCount;
        var triangles = __instance.triangles;

        __instance.verts.Sort(0, vertCount, comparer);
        __instance.myMesh.Clear();
        if (vec == null || vec.Length < vertCount + 1)
        {
            __instance.vec = new Vector3[vertCount + 1];
            vec = __instance.vec;
            __instance.uvs = new Vector2[vec.Length];
            uvs = __instance.uvs;

        }
        vec[0] = new(0f,0f,0f);
        uvs[0] = new Vector2(vec[0].x, vec[0].y);
        for (int m = 0; m < vertCount; m++)
        {
            int num3 = m + 1;
            vec[num3] = verts[m].Position;
            uvs[num3] = new Vector2(vec[num3].x, vec[num3].y);
        }
        int num4 = vertCount * 3;
        if (num4 > triangles.Length)
        {
            __instance.triangles = new int[num4];
            triangles = __instance.triangles;
        }
        
        int num5 = 0;
        int trianglesLength = triangles.Length;
        for (int n = 0; n < trianglesLength; n += 3)
        {
            if (n < num4)
            {
                triangles[n] = 0;
                triangles[n + 1] = num5 + 1;
                if (n == num4 - 3)
                {
                    triangles[n + 2] = 1;
                }
                else
                {
                    triangles[n + 2] = num5 + 2;
                }
                num5++;
            }
            else
            {
                triangles[n] = 0;
                triangles[n + 1] = 0;
                triangles[n + 2] = 0;
            }
        }

        var myMesh = __instance.myMesh;
        myMesh.vertices = vec;
        myMesh.uv = uvs;
        myMesh.SetIndices(triangles, 0, 0);

        return false;
    }
}

[HarmonyPatch(typeof(OneWayShadows), nameof(OneWayShadows.IsIgnored))]
public static class OneWayShadowsPatch
{
    public static bool Prefix(OneWayShadows __instance, ref bool __result, [HarmonyArgument(0)] LightSource lightSource)
    {
        var info = GamePlayer.LocalPlayer;
        if (info == null) return true;

        __result = (__instance.IgnoreImpostor && info.Role.HasImpostorVision) || __instance.RoomCollider.OverlapPoint(lightSource.transform.GetPositionFast());
        return false;
    }
}

[HarmonyPatch(typeof(LightSourceRaycastRenderer), nameof(LightSourceRaycastRenderer.GetValidViewDistance))]
public static class ValidViewDistancePatch
{
    public static void Postfix(LightSourceRaycastRenderer __instance, ref float __result)
    {
        __result *= NebulaGameManager.Instance?.WideCamera.CurrentRate ?? 1f;
    }
}
