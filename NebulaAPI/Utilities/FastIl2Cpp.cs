using System;
using System.Runtime.CompilerServices;
using Il2CppInterop.Runtime;
using Il2CppInterop.Runtime.InteropTypes;

namespace Virial.Utilities;

public static unsafe class FastIl2Cpp
{
    public readonly struct Method
    {
        public readonly nint MethodInfo;
        public readonly nint MethodPointer;

        public Method(nint methodInfo)
        {
            if (methodInfo == 0) throw new ArgumentNullException(nameof(methodInfo));

            MethodInfo = methodInfo;
            MethodPointer = *(nint*)methodInfo;

            if (MethodPointer == 0) throw new InvalidOperationException("IL2CPP methodPointer is null.");
        }
    }

    public abstract class MethodBase
    {
        protected readonly nint _methodInfo;
        protected readonly nint _methodPointer;

        protected MethodBase(Method method)
        {
            _methodInfo = method.MethodInfo;
            _methodPointer = method.MethodPointer;
        }

        public nint MethodInfo => _methodInfo;
        public nint MethodPointer => _methodPointer;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected nint MethodInfoOrZero(bool passMethodInfo) => passMethodInfo ? _methodInfo : 0;

    }

    public static Method ResolveMethod<TDeclaring>(string methodName, string returnTypeName, string[] parameterTypeNames, bool isStatic = false)
    {
        nint klass = Il2CppClassPointerStore<TDeclaring>.NativeClassPtr;

        nint methodInfo = IL2CPP.GetIl2CppMethod(klass, isStatic, methodName, returnTypeName, parameterTypeNames);

        if (methodInfo == 0) throw new MissingMethodException(typeof(TDeclaring).FullName, methodName);

        return new Method(methodInfo);
    }

    public static Method ResolveMethod<TDeclaring>(string methodName, Type returnType, Type[] parameterTypes, bool isStatic = false)
    {
        return ResolveMethod<TDeclaring>(methodName, returnType.FullName!, ToTypeNames(parameterTypes), isStatic);
    }

    private static TWrapper ResolveBase<TDeclaring, TWrapper>(string methodName, string returnTypeName, string[] parameterTypeNames, bool isStatic, Func<Method, TWrapper> factory) where TWrapper : MethodBase
    {
        Method method = ResolveMethod<TDeclaring>(methodName, returnTypeName, parameterTypeNames, isStatic);
        return factory(method);
    }

    private static TWrapper ResolveBase<TDeclaring, TWrapper>(string methodName, Type returnType, Type[] parameterTypes, bool isStatic, Func<Method, TWrapper> factory) where TWrapper : MethodBase
    {
        Method method = ResolveMethod<TDeclaring>(methodName, returnType, parameterTypes, isStatic);
        return factory(method);
    }

    public static string[] ToTypeNames(Type[] types)
    {
        if (types.Length == 0) return Array.Empty<string>();

        string[] result = new string[types.Length];

        for (int i = 0; i < types.Length; i++)
        {
            result[i] = types[i].FullName ?? throw new ArgumentException($"Type at index {i} has no FullName.");
        }

        return result;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static nint Ptr(Il2CppObjectBase? obj)
    {
        if (ReferenceEquals(obj, null)) return 0;
        return IL2CPP.Il2CppObjectBaseToPtr(obj);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static nint PtrNotNull(Il2CppObjectBase obj)
    {
        return IL2CPP.Il2CppObjectBaseToPtrNotNull(obj);
    }
}