using BepInEx.Unity.IL2CPP.Utils;
using InnerNet;
using LightInDark.Core;
using LightInDark.Game;
using LightInDark.RPCs;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace LightInDark.Utilities;


public static class Utils
{
    #region FlashScreen
    private static GameObject _flashObject;
    private static SpriteRenderer _renderer;
    private static Coroutine _currentCoroutine;
    private static MonoBehaviour _coroutineHost;

    private static readonly Color DefaultColor = Color.Red;
    private const float DefaultFadeIn = 0f;
    private const float DefaultHold = 0.15f;
    private const float DefaultFadeOut = 0.3f;

    class CoroutineHost : MonoBehaviour { }
    public static void PlayFlash(Color color,float fadeIn,float hold,float fadeOut)
    {
        try
        {
            if (_coroutineHost == null)
            {
                var go = new GameObject("ScreenFlashCoroutineHost");
                Object.DontDestroyOnLoad(go);
                _coroutineHost = go.AddComponent<CoroutineHost>();
            }
            if (_currentCoroutine != null)
                _coroutineHost.StopCoroutine(_currentCoroutine);
            if (_flashObject == null)
            {
                _flashObject = new GameObject("ScreenFlash");
                Object.DontDestroyOnLoad(_flashObject);
                _flashObject.transform.SetParent(null);

                _renderer = _flashObject.AddComponent<SpriteRenderer>();
                var texture = Texture2D.whiteTexture;
                _renderer.sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));
                _renderer.sortingOrder = 999999999;
                _renderer.gameObject.layer = LayerMask.NameToLayer("UI");
            }
            _currentCoroutine = _coroutineHost.StartCoroutine(FlashCoroutine(color, fadeIn, hold, fadeOut));
        }
        catch (Exception ex)
        {
            LightLogger.LogError("AmongUsEdited.PlayFlash", ex);
        }
    }

    public static void PlayFlash()
    {
        try
        {
            PlayFlash(DefaultColor, DefaultFadeIn, DefaultHold, DefaultFadeOut);
        }
        catch (Exception ex)
        {
            LightLogger.LogError("AmongUsEdited.PlayFlash", ex);
        }
    }

    public static void PlayFlash(Color color)
    {
        try
        {
            PlayFlash(color, DefaultFadeIn, DefaultHold, DefaultFadeOut);
        }
        catch (Exception ex)
        {
            LightLogger.LogError("AmongUsEdited.PlayFlash", ex);
        }
    }

    static IEnumerator FlashCoroutine(Color color, float fadeIn, float hold, float fadeOut)
    {
        _renderer.color = new Color(color.R, color.G, color.B, 0f).ToUnityColor();
        _renderer.gameObject.SetActive(true);
        if (fadeIn > 0f)
        {
            float t = 0f;
            while (t < fadeIn)
            {
                t += Time.deltaTime;
                float alpha = Mathf.Clamp01(t / fadeIn);
                _renderer.color = new Color(color.R, color.G, color.B, alpha).ToUnityColor();
                yield return null;
            }
        }
        _renderer.color = new Color(color.R, color.G, color.B, 1f).ToUnityColor();

        if (hold > 0f)
            yield return new WaitForSeconds(hold);
        if (fadeOut > 0f)
        {
            float t = 0f;
            while (t < fadeOut)
            {
                t += Time.deltaTime;
                float alpha = 1f - Mathf.Clamp01(t / fadeOut);
                _renderer.color = new Color(color.R, color.G, color.B, alpha).ToUnityColor();
                yield return null;
            }
        }
        _renderer.color = new Color(color.R, color.G, color.B, 0f).ToUnityColor();
        _renderer.gameObject.SetActive(false);
        _currentCoroutine = null;
    }
    #endregion

    public static ClientData? GetClient(PlayerControl player)
    {
        try
        {
            return AmongUsClient.Instance.allClients
                .ToArray().FirstOrDefault(cd => cd.Character?.PlayerId == player.PlayerId);
        }
        catch { return null; }
    }
    public static void KickPlayer(Player p, string kickerName,string reason = null)
    {
        try
        {
            int i = AmongUsClient.Instance.GameId;
            string code = GameCode.IntToGameNameV2(i);

            foreach(var player in PlayerControl.AllPlayerControls)
                player.RpcSendChat($"<color=red>玩家 {p.Control.Data.PlayerName} 被踢出房间，原因：{reason ?? "无"}</color>");

            string realReason = reason ?? "无";
            string kickerDisplay = string.IsNullOrEmpty(kickerName) ? "" : $"<b>{kickerName}</b>";
            string prefix = $"你被{kickerDisplay}踢出了 {code} 。\n原因：{realReason}";
            RpcDefinitions.KickPlayerWithReason(p.Control.PlayerId, prefix);
        }
        catch (Exception ex)
        {
            LightLogger.LogError("AmongUsEdited.KickPlayer", ex);
        }
    }
    public static class KickManager
    {
        public static string kickReason = string.Empty;
        public static float kickReasonWaitUntil = 0f;
        public static float kickReasonConsumeUntil = 0f;

        public static void Clear()
        {
            try
            {
                kickReason = string.Empty;
                kickReasonWaitUntil = 0f;
                kickReasonConsumeUntil = 0f;
            }
            catch (Exception ex)
            {
                LightLogger.LogError("KickManager.Clear", ex);
            }
        }
    }
    /// <summary>
    /// 将PlayerControl转换为LightInDark.Game.Player。
    /// </summary>
    /// <returns></returns>
    public static Player ToLIDPlayer(this PlayerControl pc)
    {
        try
        {
            if (pc == null) return null;
            return Game.GameManager.Instance?.GetPlayer(pc.PlayerId);
        }
        catch (Exception ex)
        {
            LightLogger.LogError("AmongUsEdited.ToLIDPlayer", ex);
            return null;
        }
    }


    /// <summary>
    /// 展示一个类似于断开连接的弹窗，显示自定义文本。
    /// </summary>
    /// <param name="text">要显示的文本</param>
    public static void ShowCustomDisconnectWindow(string text)
    {
        try
        {
            var popup = DestroyableSingleton<DisconnectPopup>.Instance;
            if (popup != null)
            {
                popup._textArea.text = text;
                popup.OnTextChanged();
                popup.gameObject.SetActive(true);
            }
        }
        catch (Exception ex)
        {
            LightLogger.LogError("AmongUsEdited.ShowCustomDisconnectWindow", ex);
        }
    }
    /// <summary>
    /// 关闭ShowCustomDisconnectWindow(string text)的窗口。
    /// </summary>
    public static void CloseCustomDisconnectWindow()
    {
        try
        {
            var popup = DestroyableSingleton<DisconnectPopup>.Instance;
            popup?.gameObject.SetActive(false);
        }
        catch (Exception ex)
        {
            LightLogger.LogError("AmongUsEdited.CloseCustomDisconnectWindow", ex);
        }
    }

    /// <summary>
    /// 检查当前是否为自定义服务器。
    /// </summary>
    /// <returns>如果是自定义服务器，返回true；否则，返回false</returns>
    public static bool IsCustomServer()
    {
        try
        {
            return ServerManager.Instance?.CurrentRegion.TranslateName is StringNames.NoTranslation or null;
        }
        catch (Exception ex)
        {
            LightLogger.LogError("AmongUsEdited.IsCustomServer", ex);
            return default;
        }
    }
    /// <summary>
    /// 检查当前是否在大厅中。
    /// </summary>
    /// <returns>在大厅中时，返回true；否则，返回false</returns>
    public static bool IsInLobby()
    {
        try
        {
            return LobbyBehaviour.Instance != null;
        }
        catch (Exception ex)
        {
            LightLogger.LogError("AmongUsEdited.IsInLobby", ex);
            return default;
        }
    }

}
