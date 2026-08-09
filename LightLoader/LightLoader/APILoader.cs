using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using BepInEx;
using Assembly = System.Reflection.Assembly;
using System.Reflection;

namespace LightLoader;
public class APILoader
{
    private const string Dir = "LID";
    private const string ApiFileName = "LightInDark.dll";
    public static bool TryGetAPIPath(out string apiPath)
    {
        try
        {
            apiPath = Path.Combine(Paths.BepInExRootPath, Dir, ApiFileName);
            if (!File.Exists(apiPath))
            {
                apiPath = null;
                return false;
            }
            return true;
        }
        catch
        {
            apiPath = null;
            return false;
        }
    }
    public static bool LoadAPI(string apiPath, out string exception)
    {
        try
        {
            Assembly apiAssembly = Assembly.LoadFile(apiPath);
            if (apiAssembly == null) 
            {
                exception = "assembly is null";
                return false;
            }
            
            var type = apiAssembly.GetType("LightInDark.LIDPlugin");
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