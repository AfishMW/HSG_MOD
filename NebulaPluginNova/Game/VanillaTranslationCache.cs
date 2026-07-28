using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nebula.Game;

internal class VanillaTranslationCache
{
    private Dictionary<StringNames, string> cache = [];
    private static TranslationController tControllerCache = null!;
    internal string GetStringInstance(StringNames id)
    {
        if(!tControllerCache.AsBoolFast())
        {
            if (TranslationController.InstanceExists) tControllerCache = TranslationController.Instance;
            else return "";
        }

        if (cache.TryGetValue(id, out var found)) return found;

        found = tControllerCache.GetString(id); ;
        cache[id] = found;
        return found;
    }

    static public string GetString(StringNames id) => ModSingleton<VanillaTranslationCache>.Instance?.GetStringInstance(id) ?? "";
}
