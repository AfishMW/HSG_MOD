using Il2CppInterop.Runtime;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using Il2CppInterop.Runtime.Runtime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Virial.Helpers;

namespace Virial.Compat;

/// <summary>
/// UnityEngine.GameObjectとUnityEngine.Transformのラッパーです。
/// これらのメソッド・プロパティを直接呼ぶより高速です。
/// </summary>
public class ModGameObject
{
    private bool hasGameObject = false;
    private UnityEngine.GameObject gameObject = null!;
    private IntPtr gameObjectPtr = IntPtr.Zero; // 初期化時、代入されていない場合がある。 

    private bool hasTransform = false;
    private UnityEngine.Transform transform = null!;
    private IntPtr transformPtr = IntPtr.Zero; // 常に保持する。

    public ModGameObject(UnityEngine.GameObject gameObject, bool keep)
    {
        this.hasGameObject = true;
        this.gameObject = gameObject;
        this.gameObjectPtr = gameObject.Pointer;
        this.transformPtr = FastMethods.GetTransformFast(gameObject);
        if (keep)
        {
            hasTransform = true;
            this.transform = new(transformPtr);
        }
    }

    public ModGameObject(UnityEngine.Component component, bool keep)
    {
        transformPtr = FastMethods.GetTransformFast(component);
        if (keep)
        {
            hasTransform = true;
            this.transform = new(transformPtr);
        }
    }

    public ModGameObject(UnityEngine.Transform transform)
    {
        hasTransform = true;
        this.transform = transform;
        transformPtr = transform.Pointer;
    }

    private ModGameObject(IntPtr transformPtr, bool keep)
    {
        this.transformPtr = transformPtr;
        if (keep)
        {
            hasTransform = true;
            this.transform = new(transformPtr);
        }
    }

    /// <summary>
    /// このGameObjectがシーン上に存在する場合、trueを返します。
    /// </summary>
    public bool Exists
    {
        get
        {
            if (hasGameObject) return gameObject.AsBoolFast();
            if (hasTransform) return transform.AsBoolFast();
            throw new InvalidOperationException("GameObject is not tracked.");
        }
    }

    public bool ActiveSelf
    {
        get
        {
            return FastMethods.GetActiveSelfFast(GetTempGameObjectPointer());
        }
    }

    public bool ActiveInHierarchy
    {
        get
        {
            return FastMethods.GetActiveInHierarchyFast(GetTempGameObjectPointer());
        }
    }

    public void SetActive(bool value)
    {
        FastMethods.SetActiveFast(GetTempGameObjectPointer(), value);
    }

    internal T GetComponent<T>() where T : UnityEngine.Component => GetUnityObject().GetComponent<T>();
    internal T AddComponent<T>() where T : UnityEngine.Component => GetUnityObject().AddComponent<T>();
    internal bool TryGetComponent<T>(out T component) where T : UnityEngine.Component => GetUnityObject().TryGetComponent<T>(out component);
    internal bool HasComponent(Il2CppSystem.Type componentType)
    {
        GetTempGameObjectPointer();
        return FastMethods.TryGetComponent(gameObjectPtr, componentType);
    }
    internal bool HasComponent<T>(Il2CppSystem.Type componentType) where T : UnityEngine.Component => HasComponent(Il2CppType.Of<T>());

    public Virial.Compat.Vector3 Position
    {
        get => FastMethods.GetPositionFast(transformPtr);
        set => FastMethods.SetPositionFast(transformPtr, new(value.x, value.y, value.z));
    }

    internal UnityEngine.Vector3 UnityPosition
    {
        get => FastMethods.GetPositionFast(transformPtr);
        set => FastMethods.SetPositionFast(transformPtr, value);
    }

    public Virial.Compat.Vector3 LocalPosition
    {
        get => FastMethods.GetLocalPositionFast(transformPtr);
        set => FastMethods.SetLocalPositionFast(transformPtr, new(value.x, value.y, value.z));
    }

    internal UnityEngine.Vector3 UnityLocalPosition
    {
        get => FastMethods.GetLocalPositionFast(transformPtr);
        set => FastMethods.SetLocalPositionFast(transformPtr, value);
    }

    public Virial.Compat.Vector3 LocalScale
    {
        get => FastMethods.GetLocalScaleFast(transformPtr);
        set => FastMethods.SetLocalScaleFast(transformPtr, new(value.x, value.y, value.z));
    }

    internal UnityEngine.Vector3 UnityLocalScale
    {
        get => FastMethods.GetLocalScaleFast(transformPtr);
        set => FastMethods.SetLocalScaleFast(transformPtr, value);
    }

    public Virial.Compat.Vector3 LossyScale => FastMethods.GetLossyScaleFast(transformPtr);
    internal UnityEngine.Vector3 UnityLossyScale => FastMethods.GetLossyScaleFast(transformPtr);

    public Virial.Compat.Vector3 LocalEulerAngles
    {
        get => FastMethods.GetLocalEulerAnglesFast(transformPtr);
        set => FastMethods.SetLocalEulerAnglesFast(transformPtr, new(value.x, value.y, value.z));
    }

    internal UnityEngine.Vector3 UnityLocalEulerAngle
    {
        get => FastMethods.GetLocalEulerAnglesFast(transformPtr);
        set => FastMethods.SetLocalEulerAnglesFast(transformPtr, value);
    }

    public ModGameObject? GetParent(bool keep = true)
    {
        IntPtr parentPtr = FastMethods.GetParentFast(transformPtr);
        if (parentPtr == IntPtr.Zero) return null!;
        return new ModGameObject(parentPtr, keep);
    }

    public int ChildCount => FastMethods.GetChildCountFast(transformPtr);

    public ModGameObject GetChild(int index, bool keep = true)
    {
        IntPtr childPtr = FastMethods.GetChildFast(transformPtr, index);
        if (childPtr == IntPtr.Zero) throw new IndexOutOfRangeException($"Child index {index} is out of range.");
        return new ModGameObject(childPtr, keep);
    }

    public IEnumerable<ModGameObject> DirectChildren
    {
        get
        {
            int count = ChildCount;
            for (int i = 0; i < count; i++) yield return GetChild(i);
        }
    }

    public IEnumerable<ModGameObject> SelfAndDirectChildren
    {
        get
        {
            yield return this;
            int count = ChildCount;
            for (int i = 0; i < count; i++) yield return GetChild(i);
        }
    }

    public IEnumerable<ModGameObject> AllChildren
    {
        get
        {
            foreach (var child in DirectChildren)
            {
                yield return child;
                foreach (var grandChild in child.AllChildren) yield return grandChild;
            }
        }
    }

    public IEnumerable<ModGameObject> SelfAndAllChildren
    {
        get
        {
            yield return this;
            foreach (var child in AllChildren) yield return child;
        }
    }

    public void SetLocalZ(float localZ)
    {
        var localPos = LocalPosition;
        localPos.z = localZ;
        LocalPosition = localPos;
    }

    public Virial.Compat.Vector3 TransformPoint(Virial.Compat.Vector3 point) => FastMethods.TransformPointFast(transformPtr, point.x, point.y, point.z);
    

    internal UnityEngine.Transform GetUnityTransform()
    {
        if (hasTransform) return transform;
        if (transformPtr == IntPtr.Zero) throw new InvalidOperationException("Transform pointer is null.");
        transform = Il2CppObjectPool.Get<UnityEngine.Transform>(transformPtr);
        hasTransform = true;
        return transform;
    }

    internal UnityEngine.GameObject GetUnityObject()
    {
        if (hasGameObject) return gameObject;
        gameObjectPtr = GetTempGameObjectPointer();
        if (gameObjectPtr == IntPtr.Zero) throw new InvalidOperationException("Transform pointer is broken.");
        gameObject = Il2CppObjectPool.Get<UnityEngine.GameObject>(gameObjectPtr);
        hasGameObject = true;
        return gameObject;
    }

    private IntPtr GetTempGameObjectPointer()
    {
        if(gameObjectPtr == IntPtr.Zero) return FastMethods.GetGameObjectFast(transformPtr);
        return gameObjectPtr;
    }
}
