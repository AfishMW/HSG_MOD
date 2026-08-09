

## Codely Structured Memories

### User

### Feedback
- [2026-08-09 20:20:08] Il2Cpp injected MonoBehaviour (ClassInjector.RegisterTypeInIl2Cpp) survives scene changes even after the host GameObject is destroyed — its Update() keeps firing and throws NRE every frame. **Why:** Among Us scene unloading destroys PassiveButtons but the injected MonoBehaviour's Update continues to run, causing NullReferenceException spam. **How to apply:** Do NOT inject per-button MonoBehaviours for main menu effects. Instead use MainMenuManager.LateUpdate Harmony postfix with `if (GameObject.Find("MainUI") == null) return;` guard (FS pattern). Store button state in a static Dictionary keyed by GameObject, clean up dead entries during iteration.
- [2026-08-09 20:20:12] User repeatedly says "照搬FS" (copy FinalSuspect's approach). When user references FS, check D:\DownLoads\FinalSuspect-FinalSus\FinalSuspect-FinalSus for the corresponding implementation before writing custom code. **Why:** user trusts FS's proven patterns and wants consistency. **How to apply:** for Among Us UI/patch work, prefer FS's approach over novel solutions.

### Project
- [2026-08-08 22:12:33] DisconnectPopup (DestroyableSingleton<DisconnectPopup>.Instance) is null during MainMenuManager.Start(). To show custom disconnect popups at main menu, use a coroutine with yield return null (3 frames) to wait for DisconnectPopup to initialize. WrapToIl2Cpp() is in namespace BepInEx.Unity.IL2CPP.Utils.Collections.
- [2026-08-09 21:08:56] LightLoader loads Light.dll from BepInEx/LID/Light.dll (ModLoader DirName="LID"), NOT from BepInEx/plugins/. When deploying, copy to BepInEx/LID/Light.dll. The plugins/Light.dll copy is loaded by BepInEx directly as a separate plugin ("Light in Dark" id=com.hvtxsvcmaomao.lid), causing duplicate PatchAll with LightAPI's LIDPlugin — both patch KeyboardJoystick.Update and GameManager.StartGame.

### Reference

