using Il2CppInterop.Runtime;
using System.Runtime.CompilerServices;
using UnityEngine;
using Virial.Utilities;

namespace Virial.Helpers;


internal static unsafe class FastMethods
{

    private static readonly int CachedPtrOffset;

    private static readonly FastIl2Cpp.Method object_getInstanceIdArg;
    private static readonly delegate* unmanaged[Cdecl]<nint, nint, int> object_getInstanceId;

    private static readonly FastIl2Cpp.Method gameObject_getTransformArg;
    private static readonly delegate* unmanaged[Cdecl]<nint, nint, nint> gameObject_getTransform;

    private static readonly FastIl2Cpp.Method gameObject_tryGetComponentArg;
    private static readonly delegate* unmanaged[Cdecl]<nint, nint, nint*, nint, bool> gameObject_tryGetComponent;

    private static readonly FastIl2Cpp.Method gameObject_activeInHierarchyArg;
    private static readonly delegate* unmanaged[Cdecl]<nint, nint, bool> gameObject_activeInHierarchy;

    private static readonly FastIl2Cpp.Method gameObject_activeSelfArg;
    private static readonly delegate* unmanaged[Cdecl]<nint, nint, bool> gameObject_activeSelf;

    private static readonly FastIl2Cpp.Method gameObject_setActiveArg;
    private static readonly delegate* unmanaged[Cdecl]<nint, bool, nint, void> gameObject_setActive;

    private static readonly FastIl2Cpp.Method component_getTransformArg;
    private static readonly delegate* unmanaged[Cdecl]<nint, nint, nint> component_getTransform;

    private static readonly FastIl2Cpp.Method component_getGameObjectArg;
    private static readonly delegate* unmanaged[Cdecl]<nint, nint, nint> component_getGameObject;

    private static readonly FastIl2Cpp.Method transform_TransformPointArg;
    private static readonly delegate* unmanaged[Cdecl]<nint, float, float, float, nint, UnityEngine.Vector3> transform_TransformPoint;

    private static readonly FastIl2Cpp.Method physics2D_OverlapCircleNonAllocArg;
    private static readonly delegate* unmanaged[Cdecl]<UnityEngine.Vector2, float, nint, int, nint, int> physics2D_OverlapCircleNonAlloc;

    private static readonly FastIl2Cpp.Method physics2D_RaycastNonAllocArg;
    private static readonly delegate* unmanaged[Cdecl]<UnityEngine.Vector2, UnityEngine.Vector2, nint, float, int, nint, int> physics2D_RaycastNonAlloc;

    private static readonly FastIl2Cpp.Method transform_getPositionArg;
    private static readonly delegate* unmanaged[Cdecl]<nint, nint, UnityEngine.Vector3> transform_getPosition;

    private static readonly FastIl2Cpp.Method transform_setPositionArg;
    private static readonly delegate* unmanaged[Cdecl]<nint, UnityEngine.Vector3, nint, void> transform_setPosition;

    private static readonly FastIl2Cpp.Method transform_getLocalPositionArg;
    private static readonly delegate* unmanaged[Cdecl]<nint, nint, UnityEngine.Vector3> transform_getLocalPosition;

    private static readonly FastIl2Cpp.Method transform_setLocalPositionArg;
    private static readonly delegate* unmanaged[Cdecl]<nint, UnityEngine.Vector3, nint, void> transform_setLocalPosition;

    private static readonly FastIl2Cpp.Method transform_getLocalScaleArg;
    private static readonly delegate* unmanaged[Cdecl]<nint, nint, UnityEngine.Vector3> transform_getLocalScale;

    private static readonly FastIl2Cpp.Method transform_setLocalScaleArg;
    private static readonly delegate* unmanaged[Cdecl]<nint, UnityEngine.Vector3, nint, void> transform_setLocalScale;

    private static readonly FastIl2Cpp.Method transform_getLossyScaleArg;
    private static readonly delegate* unmanaged[Cdecl]<nint, nint, UnityEngine.Vector3> transform_getLossyScale;

    private static readonly FastIl2Cpp.Method transform_getLocalEulerAngleArg;
    private static readonly delegate* unmanaged[Cdecl]<nint, nint, UnityEngine.Vector3> transform_getLocalEulerAngle;

    private static readonly FastIl2Cpp.Method transform_setLocalEulerAnglesArg;
    private static readonly delegate* unmanaged[Cdecl]<nint, UnityEngine.Vector3, nint, void> transform_setLocalEulerAngles;

    private static readonly FastIl2Cpp.Method transform_getParentArg;
    private static readonly delegate* unmanaged[Cdecl]<nint, nint, nint> transform_getParent;

    private static readonly FastIl2Cpp.Method transform_getChildCountArg;
    private static readonly delegate* unmanaged[Cdecl]<nint, nint, int> transform_getChildCount;

    private static readonly FastIl2Cpp.Method transform_getChildArg;
    private static readonly delegate* unmanaged[Cdecl]<nint, int, nint, nint> transform_getChild;

    private static readonly FastIl2Cpp.Method time_deltaTimeArg;
    private static readonly delegate* unmanaged[Cdecl]<nint, float> time_deltaTime;

    private static readonly FastIl2Cpp.Method time_fixedDeltaTimeArg;
    private static readonly delegate* unmanaged[Cdecl]<nint, float> time_fixedDeltaTime;

    private static readonly FastIl2Cpp.Method time_timeArg;
    private static readonly delegate* unmanaged[Cdecl]<nint, float> time_time;

    private static readonly FastIl2Cpp.Method gl_vertex3Arg;
    private static readonly delegate* unmanaged[Cdecl]<float, float, float, nint, void> gl_vertex3;




    static FastMethods()
    {
        var klass = Il2CppClassPointerStore<UnityEngine.Object>.NativeClassPtr;

        IntPtr cachedPtrField = IL2CPP.GetIl2CppField(klass, "m_CachedPtr");
        CachedPtrOffset = (int)IL2CPP.il2cpp_field_get_offset(cachedPtrField);

        object_getInstanceIdArg = FastIl2Cpp.ResolveMethod<UnityEngine.Object>(nameof(UnityEngine.Object.GetInstanceID), typeof(int), Type.EmptyTypes);
        object_getInstanceId = (delegate* unmanaged[Cdecl]<nint, nint, int>)object_getInstanceIdArg.MethodPointer;

        gameObject_getTransformArg = FastIl2Cpp.ResolveMethod<UnityEngine.GameObject>("get_transform", typeof(UnityEngine.Transform), Type.EmptyTypes);
        gameObject_getTransform = (delegate* unmanaged[Cdecl]<nint, nint, nint>)gameObject_getTransformArg.MethodPointer;

        gameObject_tryGetComponentArg = FastIl2Cpp.ResolveMethod<UnityEngine.GameObject>(nameof(UnityEngine.GameObject.TryGetComponent), typeof(bool), [typeof(Il2CppSystem.Type), typeof(UnityEngine.Component).MakeByRefType()]);
        gameObject_tryGetComponent = (delegate* unmanaged[Cdecl]<nint, nint, nint*, nint, bool>)gameObject_tryGetComponentArg.MethodPointer;

        gameObject_activeInHierarchyArg = FastIl2Cpp.ResolveMethod<UnityEngine.GameObject>("get_activeInHierarchy", typeof(bool), Type.EmptyTypes);
        gameObject_activeInHierarchy = (delegate* unmanaged[Cdecl]<nint, nint, bool>)gameObject_activeInHierarchyArg.MethodPointer;

        gameObject_activeSelfArg = FastIl2Cpp.ResolveMethod<UnityEngine.GameObject>("get_activeSelf", typeof(bool), Type.EmptyTypes);
        gameObject_activeSelf = (delegate* unmanaged[Cdecl]<nint, nint, bool>)gameObject_activeSelfArg.MethodPointer;
        
        gameObject_setActiveArg = FastIl2Cpp.ResolveMethod<UnityEngine.GameObject>(nameof(UnityEngine.GameObject.SetActive), typeof(void), [typeof(bool)]);
        gameObject_setActive = (delegate* unmanaged[Cdecl]<nint, bool, nint, void>)gameObject_setActiveArg.MethodPointer;

        component_getTransformArg = FastIl2Cpp.ResolveMethod<UnityEngine.Component>("get_transform", typeof(UnityEngine.Transform), Type.EmptyTypes);
        component_getTransform = (delegate* unmanaged[Cdecl]<nint, nint, nint>)component_getTransformArg.MethodPointer;

        component_getGameObjectArg = FastIl2Cpp.ResolveMethod<UnityEngine.Component>("get_gameObject", typeof(UnityEngine.GameObject), Type.EmptyTypes);
        component_getGameObject = (delegate* unmanaged[Cdecl]<nint, nint, nint>)component_getGameObjectArg.MethodPointer;

        transform_TransformPointArg = FastIl2Cpp.ResolveMethod<UnityEngine.Transform>(nameof(UnityEngine.Transform.TransformPoint), typeof(UnityEngine.Vector3), [typeof(float), typeof(float), typeof(float)]);
        transform_TransformPoint = (delegate* unmanaged[Cdecl]<nint, float, float, float, nint, UnityEngine.Vector3>)transform_TransformPointArg.MethodPointer;

        physics2D_OverlapCircleNonAllocArg = FastIl2Cpp.ResolveMethod<UnityEngine.Physics2D>(nameof(UnityEngine.Physics2D.OverlapCircleNonAlloc), typeof(int), [typeof(UnityEngine.Vector2), typeof(float), typeof(Il2CppInterop.Runtime.InteropTypes.Arrays.Il2CppReferenceArray<Collider2D>), typeof(int)], true);
        physics2D_OverlapCircleNonAlloc = (delegate* unmanaged[Cdecl]<UnityEngine.Vector2, float, nint, int, nint, int>)physics2D_OverlapCircleNonAllocArg.MethodPointer;

        physics2D_RaycastNonAllocArg = FastIl2Cpp.ResolveMethod<UnityEngine.Physics2D>(nameof(UnityEngine.Physics2D.RaycastNonAlloc), typeof(int), [typeof(UnityEngine.Vector2), typeof(UnityEngine.Vector2), typeof(Il2CppInterop.Runtime.InteropTypes.Arrays.Il2CppStructArray<RaycastHit2D>), typeof(float), typeof(int)], true);
        physics2D_RaycastNonAlloc = (delegate* unmanaged[Cdecl]<UnityEngine.Vector2, UnityEngine.Vector2, nint, float, int, nint, int>)physics2D_RaycastNonAllocArg.MethodPointer;

        transform_getPositionArg = FastIl2Cpp.ResolveMethod<UnityEngine.Transform>("get_position", typeof(UnityEngine.Vector3), Type.EmptyTypes);
        transform_getPosition = (delegate* unmanaged[Cdecl]<nint, nint, UnityEngine.Vector3>)transform_getPositionArg.MethodPointer;

        transform_setPositionArg = FastIl2Cpp.ResolveMethod<UnityEngine.Transform>("set_position", typeof(void), [typeof(UnityEngine.Vector3)]);
        transform_setPosition = (delegate* unmanaged[Cdecl]<nint, UnityEngine.Vector3, nint, void>)transform_setPositionArg.MethodPointer;

        transform_getLocalPositionArg = FastIl2Cpp.ResolveMethod<UnityEngine.Transform>("get_localPosition", typeof(UnityEngine.Vector3), Type.EmptyTypes);
        transform_getLocalPosition = (delegate* unmanaged[Cdecl]<nint, nint, UnityEngine.Vector3>)transform_getLocalPositionArg.MethodPointer;

        transform_setLocalPositionArg = FastIl2Cpp.ResolveMethod<UnityEngine.Transform>("set_localPosition", typeof(void), [typeof(UnityEngine.Vector3)]);
        transform_setLocalPosition = (delegate* unmanaged[Cdecl]<nint, UnityEngine.Vector3, nint, void>)transform_setLocalPositionArg.MethodPointer;

        transform_getLocalScaleArg = FastIl2Cpp.ResolveMethod<UnityEngine.Transform>("get_localScale", typeof(UnityEngine.Vector3), Type.EmptyTypes);
        transform_getLocalScale = (delegate* unmanaged[Cdecl]<nint, nint, UnityEngine.Vector3>)transform_getLocalScaleArg.MethodPointer;

        transform_setLocalScaleArg = FastIl2Cpp.ResolveMethod<UnityEngine.Transform>("set_localScale", typeof(void), [typeof(UnityEngine.Vector3)]);
        transform_setLocalScale = (delegate* unmanaged[Cdecl]<nint, UnityEngine.Vector3, nint, void>)transform_setLocalScaleArg.MethodPointer;

        transform_getLossyScaleArg = FastIl2Cpp.ResolveMethod<UnityEngine.Transform>("get_lossyScale", typeof(UnityEngine.Vector3), Type.EmptyTypes);
        transform_getLossyScale = (delegate* unmanaged[Cdecl]<nint, nint, UnityEngine.Vector3>)transform_getLossyScaleArg.MethodPointer;

        transform_getLocalEulerAngleArg = FastIl2Cpp.ResolveMethod<UnityEngine.Transform>("get_localEulerAngles", typeof(UnityEngine.Vector3), Type.EmptyTypes);
        transform_getLocalEulerAngle = (delegate* unmanaged[Cdecl]<nint, nint, UnityEngine.Vector3>)transform_getLocalEulerAngleArg.MethodPointer;

        transform_setLocalEulerAnglesArg = FastIl2Cpp.ResolveMethod<UnityEngine.Transform>("set_localEulerAngles", typeof(void), [typeof(UnityEngine.Vector3)]);
        transform_setLocalEulerAngles = (delegate* unmanaged[Cdecl]<nint, UnityEngine.Vector3, nint, void>)transform_setLocalEulerAnglesArg.MethodPointer;

        transform_getParentArg = FastIl2Cpp.ResolveMethod<UnityEngine.Transform>("get_parent", typeof(UnityEngine.Transform), Type.EmptyTypes);
        transform_getParent = (delegate* unmanaged[Cdecl]<nint, nint, nint>)transform_getParentArg.MethodPointer;

        transform_getChildCountArg = FastIl2Cpp.ResolveMethod<UnityEngine.Transform>("get_childCount", typeof(int), Type.EmptyTypes);
        transform_getChildCount = (delegate* unmanaged[Cdecl]<nint, nint, int>)transform_getChildCountArg.MethodPointer;

        transform_getChildArg = FastIl2Cpp.ResolveMethod<UnityEngine.Transform>(nameof(UnityEngine.Transform.GetChild), typeof(UnityEngine.Transform), [typeof(int)]);
        transform_getChild = (delegate* unmanaged[Cdecl]<nint, int, nint, nint>)transform_getChildArg.MethodPointer;

        time_deltaTimeArg = FastIl2Cpp.ResolveMethod<UnityEngine.Time>("get_deltaTime", typeof(float), Type.EmptyTypes, true);
        time_deltaTime = (delegate* unmanaged[Cdecl]<nint, float>)time_deltaTimeArg.MethodPointer;

        time_fixedDeltaTimeArg = FastIl2Cpp.ResolveMethod<UnityEngine.Time>("get_fixedDeltaTime", typeof(float), Type.EmptyTypes, true);
        time_fixedDeltaTime = (delegate* unmanaged[Cdecl]<nint, float>)time_fixedDeltaTimeArg.MethodPointer;

        time_timeArg = FastIl2Cpp.ResolveMethod<UnityEngine.Time>("get_time", typeof(float), Type.EmptyTypes, true);
        time_time = (delegate* unmanaged[Cdecl]<nint, float>)time_timeArg.MethodPointer;

        gl_vertex3Arg = FastIl2Cpp.ResolveMethod<UnityEngine.GL>(nameof(UnityEngine.GL.Vertex3), typeof(void), [typeof(float), typeof(float), typeof(float)]);
        gl_vertex3 = (delegate* unmanaged[Cdecl]<float, float, float, nint, void>)gl_vertex3Arg.MethodPointer;
    }

    internal static int GetInstanceIdNoBox(UnityEngine.Object? obj)
    {
        if (System.Object.ReferenceEquals(obj, null)) return 0;

        IntPtr objPtr = IL2CPP.Il2CppObjectBaseToPtr(obj);
        if (objPtr == IntPtr.Zero) return 0;

        return object_getInstanceId(objPtr, object_getInstanceIdArg.MethodInfo);
    }

    internal static int GetInstanceIdNoBoxFromPtr(IntPtr ptr)
    {
        if (ptr == IntPtr.Zero) return 0;
        return object_getInstanceId(ptr, object_getInstanceIdArg.MethodInfo);
    }



    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static unsafe IntPtr GetCachedPtr(IntPtr objPtr) => *(IntPtr*)((byte*)objPtr + CachedPtrOffset);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static IntPtr NullableObjToPtr(UnityEngine.Object? obj) => obj?.Pointer ?? IntPtr.Zero;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static bool AsBoolFast(this UnityEngine.Object? obj)
    {
        if ((object)obj == null) return false;

        if (obj.WasCollected) return false;

        IntPtr objPtr = NullableObjToPtr(obj);
        if (objPtr == IntPtr.Zero) return false;

        return GetCachedPtr(objPtr) != IntPtr.Zero;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static bool AsBoolFast<T>(this T? obj, out T val) where T : UnityEngine.Object
    {
        val = obj;
        return AsBoolFast(obj);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static bool EqualsFast(this UnityEngine.Object? obj1, UnityEngine.Object? obj2)
    {
        var ptr1 = NullableObjToPtr(obj1);
        var ptr2 = NullableObjToPtr(obj2);
        if (ptr1 == ptr2) return true;

        //片方が無効なポインタな場合、他方のCachedPtrがZeroなら同値
        if (ptr1 == IntPtr.Zero) return GetCachedPtr(ptr2) == IntPtr.Zero;
        if (ptr2 == IntPtr.Zero) return GetCachedPtr(ptr1) == IntPtr.Zero;

        //InstanceIdが同値なら同値
        return GetInstanceIdNoBoxFromPtr(ptr1) == GetInstanceIdNoBoxFromPtr(ptr2);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static int GetInstanceIdFast(this UnityEngine.Object? obj) => GetInstanceIdNoBox(obj);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [Obsolete("Use ModGameObject instead.")]
    internal static UnityEngine.Vector3 TransformPointFast(this UnityEngine.Transform obj, float x, float y, float z) => transform_TransformPoint(FastIl2Cpp.PtrNotNull(obj), x, y, z, transform_TransformPointArg.MethodInfo);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [Obsolete("Use ModGameObject instead.")]
    internal static UnityEngine.Vector3 TransformPointFast(this UnityEngine.Transform obj, float x, float y) => transform_TransformPoint(FastIl2Cpp.PtrNotNull(obj), x, y, 0f, transform_TransformPointArg.MethodInfo);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [Obsolete("Use ModGameObject instead.")]
    internal static UnityEngine.Vector3 TransformPointFast(this UnityEngine.Transform obj, Virial.Compat.Vector3 vector) => TransformPointFast(obj, vector.x, vector.y, vector.z);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [Obsolete("Use ModGameObject instead.")]
    internal static UnityEngine.Vector3 TransformPointFast(this UnityEngine.Transform obj, UnityEngine.Vector3 vector) => TransformPointFast(obj, vector.x, vector.y, vector.z);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static UnityEngine.Vector3 TransformPointFast(IntPtr transform, float x, float y, float z) => transform_TransformPoint(transform, x, y, z, transform_TransformPointArg.MethodInfo);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static int OverlapCircleNonAllocFast(float x, float y, float radius, Il2CppInterop.Runtime.InteropTypes.Arrays.Il2CppReferenceArray<Collider2D> results, int layerMask) => physics2D_OverlapCircleNonAlloc(new Vector2(x, y), radius, FastIl2Cpp.Ptr(results), layerMask, physics2D_OverlapCircleNonAllocArg.MethodInfo);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static int OverlapCircleNonAllocFast(Virial.Compat.Vector2 point, float radius, Il2CppInterop.Runtime.InteropTypes.Arrays.Il2CppReferenceArray<Collider2D> results, int layerMask) => physics2D_OverlapCircleNonAlloc(new Vector2(point.x, point.y), radius, FastIl2Cpp.Ptr(results), layerMask, physics2D_OverlapCircleNonAllocArg.MethodInfo);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static int OverlapCircleNonAllocFast(UnityEngine.Vector2 point, float radius, Il2CppInterop.Runtime.InteropTypes.Arrays.Il2CppReferenceArray<Collider2D> results, int layerMask) => physics2D_OverlapCircleNonAlloc(point, radius, FastIl2Cpp.Ptr(results), layerMask, physics2D_OverlapCircleNonAllocArg.MethodInfo);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static int RaycastNonAllocFast(UnityEngine.Vector2 pos, UnityEngine.Vector2 dir, Il2CppInterop.Runtime.InteropTypes.Arrays.Il2CppStructArray<RaycastHit2D> results, float distance, int layerMask) => physics2D_RaycastNonAlloc(pos, dir, FastIl2Cpp.Ptr(results), distance, layerMask, physics2D_RaycastNonAllocArg.MethodInfo);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [Obsolete("Use ModGameObject instead.")]
    internal static UnityEngine.Vector3 GetPositionFast(this UnityEngine.Transform obj) => transform_getPosition(FastIl2Cpp.PtrNotNull(obj), transform_getPositionArg.MethodInfo);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static UnityEngine.Vector3 GetPositionFast(IntPtr transform) => transform_getPosition(transform, transform_getPositionArg.MethodInfo);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void SetPositionFast(IntPtr transform, UnityEngine.Vector3 position) => transform_setPosition(transform, position, transform_setPositionArg.MethodInfo);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [Obsolete("Use ModGameObject instead.")]
    internal static UnityEngine.Vector3 GetLocalPositionFast(this UnityEngine.Transform obj) => transform_getLocalPosition(FastIl2Cpp.PtrNotNull(obj), transform_getLocalPositionArg.MethodInfo);
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static UnityEngine.Vector3 GetLocalPositionFast(IntPtr transform) => transform_getLocalPosition(transform, transform_getLocalPositionArg.MethodInfo);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void SetLocalPositionFast(IntPtr transform, UnityEngine.Vector3 position) => transform_setLocalPosition(transform, position, transform_setLocalPositionArg.MethodInfo);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static UnityEngine.Vector3 GetLocalScaleFast(IntPtr transform) => transform_getLocalScale(transform, transform_getLocalScaleArg.MethodInfo);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void SetLocalScaleFast(IntPtr transform, UnityEngine.Vector3 scale) => transform_setLocalScale(transform, scale, transform_setLocalScaleArg.MethodInfo);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static UnityEngine.Vector3 GetLossyScaleFast(IntPtr transform) => transform_getLossyScale(transform, transform_getLossyScaleArg.MethodInfo);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static UnityEngine.Vector3 GetLocalEulerAnglesFast(IntPtr transform) => transform_getLocalEulerAngle(transform, transform_getLocalEulerAngleArg.MethodInfo);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void SetLocalEulerAnglesFast(IntPtr transform, UnityEngine.Vector3 euler) => transform_setLocalEulerAngles(transform, euler, transform_setLocalEulerAnglesArg.MethodInfo);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static IntPtr GetParentFast(IntPtr transform) => transform_getParent(transform, transform_getParentArg.MethodInfo);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static int GetChildCountFast(IntPtr transform) => transform_getChildCount(transform, transform_getChildCountArg.MethodInfo);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static IntPtr GetChildFast(IntPtr transform, int index) => transform_getChild(transform, index, transform_getChildArg.MethodInfo);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static bool TryGetComponent(IntPtr gameObject, Il2CppSystem.Type type)
    {
        nint unusedPtr = 0;
        return gameObject_tryGetComponent(gameObject, type.Pointer, &unusedPtr, gameObject_tryGetComponentArg.MethodInfo);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static bool GetActiveInHierarchyFast(IntPtr gameObject) => gameObject_activeInHierarchy(gameObject, gameObject_activeInHierarchyArg.MethodInfo);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static bool GetActiveSelfFast(IntPtr gameObject) => gameObject_activeSelf(gameObject, gameObject_activeSelfArg.MethodInfo);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void SetActiveFast(IntPtr gameObject, bool value) => gameObject_setActive(gameObject, value, gameObject_setActiveArg.MethodInfo);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static float GetDeltaTimeFast() => time_deltaTime(time_deltaTimeArg.MethodInfo);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static float GetFixedDeltaTimeFast() => time_fixedDeltaTime(time_deltaTimeArg.MethodInfo);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static float GetTimeFast() => time_time(time_timeArg.MethodInfo);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static nint GetTransformFast(GameObject gameObject) => gameObject_getTransform(FastIl2Cpp.PtrNotNull(gameObject), gameObject_getTransformArg.MethodInfo);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static nint GetTransformFast(UnityEngine.Component component) => component_getTransform(FastIl2Cpp.PtrNotNull(component), component_getTransformArg.MethodInfo);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static nint GetGameObjectFast(nint component) => component_getGameObject(component, component_getGameObjectArg.MethodInfo);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void GLVertex3Fast(float x, float y, float z) => gl_vertex3(x, y, z, gl_vertex3Arg.MethodInfo);
}
