

## Codely Structured Memories

### User

### Feedback
- [2026-08-05 13:49:32] AbilityButton uses config-object API (AbilityButtonConfig), NOT Nebula's chainable builder pattern. User explicitly wanted different calling conventions from Nebula. Old IModAbilityButton/ModAbilityButton/AbilityButtonFactory/AbilityButtonImpl/IAbilityButton all deleted.
- [2026-08-05 20:11:47] AbilityButton.IsDeadObject must check _gameObject == null ONLY, not activeInHierarchy. Button starts as Active(false) so activeInHierarchy check causes GameManager to remove it before Update() can show it.
- [2026-08-05 21:29:56] AbilityButton must use PassiveButton (not UnityEngine.UI.Button) for click binding. Pattern: pb.OnClick = new ButtonClickedEvent(); pb.OnClick.AddListener((UnityAction)handler). MiraAPI uses same pattern. Button component returns null on cloned ActionButton in Il2Cpp.
- [2026-08-06 14:18:01] LidRPC RpcPrefix must use a static bool flag (_isLocalExecuting) to prevent infinite recursion when calling Method.Invoke() for local execution. Harmony patches trigger Prefix on every Invoke call, causing StackOverflow without the flag. Pattern: check flag at start → if true return true (pass through) → set true before Invoke → set false in finally.

### Project
- [2026-08-04 21:18:53] UI/Window system uses Nebula's SpriteRenderer-based approach (SpriteRenderer + TextMeshPro + BoxCollider2D + PassiveButton), NOT Unity Canvas/UI Image. Matches Nebula's Virial.Media.GUI ~90%. User wants it implemented like Nebula including button sprites/materials, TextAttribute system, Anchor positioning, and SetUpButton extension.
- [2026-08-05 13:26:31] UI/HudUI system mirrors Nebula's MetaScreen exactly (ClassInjector MonoBehaviour, GenerateScreen/GenerateWindow/SetUpNavButton, BackgroundSetting.Modern with Frame+Inner sprites, BlackScreen with Sliced 30x30, ClickGuard). Uses custom sprite sheets from Resources/GUI/ (Button 3x2, CloseButton 2x1, NavButton 2x2, Background_Frame/Inner 9-slice, ColorButton/Selected 9-slice). Buttons auto-size to text (preferredWidth+margin*2) when size=null, matching Nebula GUIButton. TextMeshPro sortingOrder=15 (above SpriteRenderer 10). Font from VersionShower.text.font. SetUpButton has playSound param (default false) so ClickGuard is silent. WindowSize presets match NebulaN (RoleSelect=7.6x4.2).
- [2026-08-05 13:49:39] Player system rewritten: Player.cs has IsLocal, PlayerColor (from Control.Data.DefaultOutfit.ColorId), RoleCategory, IsRole(name), Is<T>(), HasRole. PlayerManager.cs and PlayerData.cs deleted (redundant). Configuration/GameConfig.cs holds hardcoded config constants. Next step: UI integration with GameConfig + Preset system.
- [2026-08-05 19:11:00] CustomRPC system replaces Reactor [MethodRpc]. Uses AmongUsClient.StartRpcImmediately with callId=128, string-hash-based dispatch, Harmony patch on PlayerControl.HandleRpc. Files: RPCs/CustomRPC.cs (core), RPCs/RpcDefinitions.cs (all handlers). Old RPC files (RPCManager.cs, Chat.cs, etc.) still exist but are dead code.

### Reference

