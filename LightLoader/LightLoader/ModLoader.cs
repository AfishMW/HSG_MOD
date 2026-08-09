using BepInEx;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Reflection;

namespace LightLoader;

public class ModLoader
{
    private const string ModDllName = "Light.dll";
    private const string DirName = "LID";
    public static bool TryGetModPath(out string modPath)
    {
        try
        {
            modPath = Path.Combine(Paths.BepInExRootPath, DirName, ModDllName);
            if (!File.Exists(modPath))
            {
                modPath = null;
                return false;
            }
            return true;
        }
        catch
        {
            modPath = null;
            return false;
        }
        
    }
    public static bool LoadMod(string modPath,out string exception)
    {
        try
        {
            Assembly assembly = Assembly.LoadFile(modPath);
            if (assembly == null)
            {
                exception = "assembly is null";
                return false;
            }
            var type = assembly.GetType("Light.LightPlugin");
            type?.GetMethod("Load", BindingFlags.Static | BindingFlags.Public)?.Invoke(null, null);
            exception = null;
            return true;
        }
        catch(Exception ex)
        {
            exception = ex.ToString();
            return false;
        }
    }
}