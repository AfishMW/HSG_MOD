using System;
using System.Runtime.InteropServices;

namespace Nebula.Utilities;

internal static class IncrementalGcInvalidator
{
    static readonly bool Is64Bit = Environment.Is64BitProcess;

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    static extern nint GetModuleHandleW(string? lpModuleName);
    [DllImport("kernel32.dll", CharSet = CharSet.Ansi, SetLastError = true)]
    static extern nint GetProcAddress(nint hModule, string procName);

    [DllImport("GameAssembly", EntryPoint = "il2cpp_gc_is_incremental", CallingConvention = CallingConvention.Cdecl)]
    static extern bool IsIncrementalNative();

    public static bool IsIncrementalGcActive()
    {
        try { return IsIncrementalNative(); }
        catch { return false; }
    }

    static unsafe nint ResolveJmpThunk(nint addr, int maxHops = 5)
    {
        for (int hop = 0; hop < maxHops; hop++)
        {
            byte* p = (byte*)addr;
            if (p[0] != 0xE9) break;
            int rel = *(int*)(p + 1);
            addr = addr + 5 + rel;
        }
        return addr;
    }

    static unsafe nint FindFirstCallTarget(nint funcAddr, int scanBytes)
    {
        byte* p = (byte*)funcAddr;
        for (int i = 0; i < scanBytes - 4; i++)
        {
            if (p[i] == 0xE8)
                return funcAddr + i + 5 + *(int*)(p + i + 1);
        }
        return 0;
    }

    static unsafe nint FindGlobalReadTarget(nint funcAddr, int scanBytes)
    {
        byte* p = (byte*)funcAddr;
        for (int i = 0; i < scanBytes - 6; i++)
        {
            if (p[i] == 0x8B && p[i + 1] == 0x05)
            {
                int disp = *(int*)(p + i + 2);
                if (Is64Bit)
                {
                    nint nextInstrAddr = funcAddr + i + 6; // 8B 05 <disp32> で命令長6バイト
                    return nextInstrAddr + disp;
                }
                return (nint)disp;
            }
            if (!Is64Bit && p[i] == 0xA1)
            {
                return (nint)(*(int*)(p + i + 1));
            }
        }
        return 0;
    }

    static unsafe nint DiscoverFlagAddress()
    {
        nint hGameAssembly = GetModuleHandleW("GameAssembly.dll");
        if (hGameAssembly == 0) return 0;

        nint exportAddr = GetProcAddress(hGameAssembly, "il2cpp_gc_is_incremental");
        if (exportAddr == 0) return 0;

        nint realFunc = ResolveJmpThunk(exportAddr);

        // export解決先に直接グローバル読み出しがある
        nint found = FindGlobalReadTarget(realFunc, 24);
        if (found != 0) return found;

        // 最初のcall先
        nint callTarget = FindFirstCallTarget(realFunc, 24);
        if (callTarget != 0)
        {
            callTarget = ResolveJmpThunk(callTarget);
            found = FindGlobalReadTarget(callTarget, 24);
            if (found != 0) return found;
        }
        return 0;
    }

    public static unsafe bool TryDisable()
    {
        nint flagAddr = DiscoverFlagAddress();
        if (flagAddr == 0)
            return false;

        bool apiSaysIncremental;
        try { apiSaysIncremental = IsIncrementalNative(); }
        catch (Exception ex) { return false; }

        int* p = (int*)flagAddr;
        int before = *p;
        bool memSaysIncremental = before != 0;

        // 発見したアドレスの戻り値がAPIの戻り値と食い違う
        if (memSaysIncremental != apiSaysIncremental)
            return false;

        if (!apiSaysIncremental)
            return true;

        *p = 0;
        int after = *p;
        bool validated = !IsIncrementalNative();
        return true;
    }
}
