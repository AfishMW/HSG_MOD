

## Codely Structured Memories

### User

### Feedback
- [2026-08-09 18:14:22] User prefers LightLogger (project's custom logger in LightInDark.Core) over BepInEx StaticLog for all logging. **Why:** user explicitly asked to "日志尽量少用Be的，用自带封装". **How to apply:** use LightLogger.Log / LightLogger.LogWarning instead of StaticLog in all new code under Light project.

### Project
- [2026-08-09 18:14:20] Two MetaScreen classes exist: Light.UI.HudUI.MetaScreen (has GenerateWindow/GenerateScreen, used for HudUI windows) and Light.UI.Window.MetaScreen (implements IGUIScreen). Use `using MetaScreen = Light.UI.HudUI.MetaScreen;` alias when both namespaces are imported. Similarly, two UnityHelper classes exist: Light.Utilities.UnityHelper (internal, Light project) and LightInDark.UI.Window.UnityHelper (public, LightAPI project). Both have CreateObject<T> but different signatures.
### Reference

