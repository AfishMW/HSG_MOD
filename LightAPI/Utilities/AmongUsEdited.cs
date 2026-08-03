using BepInEx.Unity.IL2CPP.Utils;
using InnerNet;
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


public static class AmongUsEdited
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

    public static void PlayFlash()
    {
        PlayFlash(DefaultColor, DefaultFadeIn, DefaultHold, DefaultFadeOut);
    }

    public static void PlayFlash(Color color)
    {
        PlayFlash(color, DefaultFadeIn, DefaultHold, DefaultFadeOut);
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
        int i = AmongUsClient.Instance.GameId;
        string code = GameCode.IntToGameNameV2(i);

        foreach(var player in PlayerControl.AllPlayerControls)
            player.RpcSendChat($"<color=red>玩家 {p.Control.Data.PlayerName} 被踢出房间，原因：{reason ?? "无"}</color>");

        string realReason = reason ?? "无";
        string kickerDisplay = string.IsNullOrEmpty(kickerName) ? "" : $"<b>{kickerName}</b>";
        string prefix = $"你被{kickerDisplay}踢出了 {code} 。\n原因：{realReason}";
        RPC.KickPlayerWithReason(p.Control, prefix);
    }
    public static class KickManager
    {
        public static string kickReason = string.Empty;
        public static float kickReasonWaitUntil = 0f;
        public static float kickReasonConsumeUntil = 0f;

        public static void Clear()
        {
            kickReason = string.Empty;
            kickReasonWaitUntil = 0f;
            kickReasonConsumeUntil = 0f;
        }
    }
    public static Player ToLIDPlayer(this PlayerControl pc)
    {
        if (pc == null) return null;
        return Game.GameManager.Instance?.GetPlayer(pc.PlayerId);
    }



    public static void ShowCustomDisconnectWindow(string text)
    {
        var popup = DestroyableSingleton<DisconnectPopup>.Instance;
        if (popup != null)
        {
            popup._textArea.text = text;
            popup.OnTextChanged();
            popup.gameObject.SetActive(true);
        }
    }


}