

## Codely Structured Memories

### User

### Feedback
- [2026-08-09 18:14:22] User prefers LightLogger (project's custom logger in LightInDark.Core) over BepInEx StaticLog for all logging. **Why:** user explicitly asked to "日志尽量少用Be的，用自带封装". **How to apply:** use LightLogger.Log / LightLogger.LogWarning instead of StaticLog in all new code under Light project.
- [2026-08-10 14:19:07] Il2Cpp interop 的 enum 在 switch 模式匹配中不能直接用命名常量（编译报 CS0266）。**Why:** Il2Cpp 生成的 enum 类型与 C# 原生 enum 有差异，switch 返回值类型不匹配。**How to apply:** 用 `(int)enumValue` 转成 int 再 switch int 值；如 DeathReason 0=Kill 1=Exile 2=Disconnect；GameOverReason 0-2=Humans win 3-6=Impostor win。同理 `Object` 歧义用完整路径 `UnityEngine.Object`。
- [2026-08-12 13:45:30] Il2Cpp 中自定义 MonoBehaviour 必须先 ClassInjector.RegisterTypeInIl2Cpp<T>() 才能 AddComponent<T>()，否则抛 MethodInfoStoreGeneric_AddComponent_Public_T_0 静态构造异常。**Why:** AddComponent 泛型方法在 Il2Cpp 互操作层需要类型注册表查找，未注册的类型无法映射。**How to apply:** 如果组件不需要 Update/Awake 等生命周期回调，优先用纯 static 类替代 MonoBehaviour；必须用 MonoBehaviour 时务必加 static 构造器调用 ClassInjector.RegisterTypeInIl2Cpp。

### Project
- [2026-08-09 18:14:20] Two MetaScreen classes exist: Light.UI.HudUI.MetaScreen (has GenerateWindow/GenerateScreen, used for HudUI windows) and Light.UI.Window.MetaScreen (implements IGUIScreen). Use `using MetaScreen = Light.UI.HudUI.MetaScreen;` alias when both namespaces are imported. Similarly, two UnityHelper classes exist: Light.Utilities.UnityHelper (internal, Light project) and LightInDark.UI.Window.UnityHelper (public, LightAPI project). Both have CreateObject<T> but different signatures.
### Reference

