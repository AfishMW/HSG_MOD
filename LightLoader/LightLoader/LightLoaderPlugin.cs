using BepInEx;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;
using Reactor;

namespace LightLoader
{
    [BepInPlugin("com.hvtxsvcmaomao.lid", "Light in Dark", "1.0.0")]
    [BepInProcess("Among Us.exe")]
    [BepInDependency(ReactorPlugin.Id)]
    public partial class LightLoaderPlugin : BasePlugin
    {
        public Harmony Harmony { get; } = new("LightLoaderPatch");
        public override void Load()
        {
            var lLogger = BepInEx.Logging.Logger.CreateLogSource("LightLoader");
            lLogger.LogInfo("开始加载Light Loader自身。");
            string apiPath;
            string modPath;
            string apiEx;
            string modEx;
            lLogger.LogInfo("Light Loader开始加载。");

            if(!APILoader.TryGetAPIPath(out apiPath))
            {
                lLogger.LogError("API文件不存在！将退出Load方法。");
                return;
            }
            if(!ModLoader.TryGetModPath(out modPath))
            {
                lLogger.LogError("MOD文件不存在！将退出Load方法。");
                return;
            }
            if (!APILoader.LoadAPI(apiPath,out apiEx))
            {
                lLogger.LogError($"API加载失败-> \n{apiEx}");
                return;
            }
            if(!ModLoader.LoadMod(modPath,out modEx))
            {
                lLogger.LogError($"MOD加载失败-> \n{modEx}");
                return;
            }

            Harmony.PatchAll();
        }
    }
}
