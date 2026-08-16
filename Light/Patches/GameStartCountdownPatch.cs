using System;
using HarmonyLib;
using LightInDark;
using LightInDark.Core;
using LightInDark.Events;
using LightInDark.Utilities;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;
using Color = LightInDark.Color;

namespace Light.Patches;

/// <summary>
/// 开始游戏倒计时增强（参考 FinalSuspect / TownOfNext 实现，布局按需求调整）：
/// 点击开始后 ——
///   · 开始按钮上方（y 轴正方向）显示红色“取消”按钮；
///   · 开始按钮上方显示原版倒计时文本（GameStartText）；
///   · 开始按钮右下角显示淡金色“跳过”按钮。
/// 跳过使用 ReallyBegin(false) 直接干净开局，避免 countDownTimer=0 触发原生流程不稳定的黑屏卡死。
/// </summary>
[HarmonyPatch(typeof(GameStartManager))]
public static class GameStartCountdownPatch
{
    private static PassiveButton _cancelButton;
    private static PassiveButton _skipButton;
    private static Vector3 _gameStartTextOriginalPos;

    [HarmonyPatch(nameof(GameStartManager.Start))]
    [HarmonyPostfix]
    public static void StartPostfix(GameStartManager __instance)
    {
        try
        {
            if (!AmongUsClient.Instance?.AmHost ?? true) return;

            _gameStartTextOriginalPos = __instance.GameStartText.transform.localPosition;

            // ---------- 取消按钮：红色，位于开始按钮上方 ----------
            _cancelButton = Object.Instantiate(__instance.StartButton, __instance.transform);
            var cancelLabel = _cancelButton.buttonText;
            if (cancelLabel != null)
            {
                cancelLabel.DestroyTranslator();
                cancelLabel.text = "取消";
            }
            _cancelButton.transform.localPosition =
                __instance.StartButton.transform.localPosition + Vector3.up * 2.6f;
            _cancelButton.transform.localScale = Vector3.one;

            _cancelButton.inactiveSprites.GetComponent<SpriteRenderer>().color =
                new UnityEngine.Color(0.8f, 0f, 0f, 1f);
            _cancelButton.activeSprites.GetComponent<SpriteRenderer>().color = UnityEngine.Color.red;
            var cancelShine = _cancelButton.inactiveSprites.transform.Find("Shine");
            if (cancelShine != null) cancelShine.gameObject.SetActive(false);

            _cancelButton.activeTextColor = _cancelButton.inactiveTextColor = UnityEngine.Color.white;
            _cancelButton.OnClick = new Button.ButtonClickedEvent();
            _cancelButton.OnClick.AddListener((UnityEngine.Events.UnityAction)(() =>
            {
                try
                {
                    SoundManager.Instance.StopSound(GameStartManager.Instance.gameStartSound);
                    // OnLobbyCancelStart 由 LobbyCancelStartPatch 在 ResetStartState 时触发
                    GameStartManager.Instance.ResetStartState();
                }
                catch (Exception ex)
                {
                    LightLogger.LogError("[GameStartCountdownPatch.Cancel]", ex);
                }
            }));
            _cancelButton.gameObject.SetActive(false);

            // ---------- 跳过按钮：淡金（ModGolden），位于开始按钮右下角 ----------
            _skipButton = Object.Instantiate(__instance.StartButton, __instance.transform);
            var skipLabel = _skipButton.buttonText;
            if (skipLabel != null)
            {
                skipLabel.DestroyTranslator();
                skipLabel.text = "跳过";
            }
            _skipButton.transform.localScale = new Vector3(0.5f, 0.5f, 1f);
            _skipButton.transform.localPosition =
                __instance.StartButton.transform.localPosition + new Vector3(1.6f, -1.0f, 0f);

            var golden = Color.ModGolden.ToUnityColor();
            _skipButton.inactiveSprites.GetComponent<SpriteRenderer>().color = golden;
            _skipButton.activeSprites.GetComponent<SpriteRenderer>().color = golden;
            var skipShine = _skipButton.inactiveSprites.transform.Find("Shine");
            if (skipShine != null) skipShine.gameObject.SetActive(false);

            _skipButton.activeTextColor = _skipButton.inactiveTextColor = UnityEngine.Color.white;
            _skipButton.OnClick = new Button.ButtonClickedEvent();
            _skipButton.OnClick.AddListener((UnityEngine.Events.UnityAction)(() =>
            {
                try
                {
                    SoundManager.Instance.StopSound(GameStartManager.Instance.gameStartSound);
                    var gsm = GameStartManager.Instance;
                    if (gsm == null) return;

                    int playerCount = PlayerControl.AllPlayerControls?.Count ?? 0;

                    // 愚人节恶作剧：使用旧的 ReallyBegin(false) 逻辑（会触发原生流转把
                    // 倒计时重置回 5 秒，假装跳过了但其实没有）。
                    if (LightUtils.IsAprilDay())
                    {
                        gsm.ReallyBegin(false);
                        return;
                    }

                    // 触发“房主跳过倒计时”事件
                    EventTriggers.OnLobbySkipCountdown(playerCount);

                    // 正常逻辑：直接调用 BeginGame()，这与原版倒计时归零时原生 Update 所走的
                    // 完整开局路径完全一致（BeginGame -> 主机校验 -> ReallyBegin -> CoStartGame -> FinallyBegin）。
                    // 不要单独调用 ReallyBegin/FinallyBegin：那样会跳过主机校验与船只生成，
                    // 导致“进了游戏但没进”（黑屏、设置键仍可用）。
                    gsm.BeginGame();
                }
                catch (Exception ex)
                {
                    LightLogger.LogError("[GameStartCountdownPatch.Skip]", ex);
                }
            }));
            _skipButton.gameObject.SetActive(false);
        }
        catch (Exception ex)
        {
            LightLogger.LogError("[GameStartCountdownPatch.Start]", ex);
        }
    }

    [HarmonyPatch(nameof(GameStartManager.Update))]
    [HarmonyPostfix]
    public static void UpdatePostfix(GameStartManager __instance)
    {
        try
        {
            if (!AmongUsClient.Instance?.AmHost ?? true) return;
            if (_cancelButton == null || _skipButton == null) return;

            bool counting = __instance.startState == GameStartManager.StartingStates.Countdown;

            _cancelButton.gameObject.SetActive(counting);
            _skipButton.gameObject.SetActive(counting);
            __instance.StartButton.gameObject.SetActive(!counting);

            // 原版倒计时文本移到开始按钮上方（y 轴正方向）；结束时还原位置
            if (counting)
            {
                __instance.GameStartText.transform.localPosition =
                    __instance.StartButton.transform.localPosition + Vector3.up * 1.6f;
            }
            else
            {
                __instance.GameStartText.transform.localPosition = _gameStartTextOriginalPos;
            }
        }
        catch (Exception ex)
        {
            LightLogger.LogWarning("[GameStartCountdownPatch.Update] " + ex.Message);
        }
    }

    private static void DestroyTranslator(this MonoBehaviour mb)
    {
        try
        {
            var tr = mb.GetComponent<TextTranslatorTMP>();
            if (tr != null) Object.Destroy(tr);
        }
        catch { }
    }
}

/// <summary>
/// 大厅生命周期事件：房主开启/取消倒计时时触发 Lobby 事件。
/// （“跳过倒计时”由上方跳过按钮点击里触发 <see cref="EventTriggers.OnLobbySkipCountdown"/>。）
/// </summary>
[HarmonyPatch(typeof(GameStartManager), nameof(GameStartManager.SetStartCounter))]
public static class LobbyCountdownStartPatch
{
    public static void Postfix(GameStartManager __instance, sbyte sec)
    {
        try
        {
            if (sec <= 0) return; // -1=重置/取消，正数=倒计时开始
            if (!AmongUsClient.Instance?.AmHost ?? true) return;

            int playerCount = PlayerControl.AllPlayerControls?.Count ?? 0;
            if (EventTriggers.OnLobbyStartGame(playerCount))
                EventTriggers.OnLobbyCountdownStart(playerCount, sec);
        }
        catch (Exception ex)
        {
            LightLogger.LogWarning("[LobbyCountdownStartPatch.Postfix] " + ex.Message);
        }
    }
}

[HarmonyPatch(typeof(GameStartManager), nameof(GameStartManager.ResetStartState))]
public static class LobbyCancelStartPatch
{
    public static void Postfix(GameStartManager __instance)
    {
        try
        {
            if (!AmongUsClient.Instance?.AmHost ?? true) return;
            int remaining = 0;
            if (__instance.startState == GameStartManager.StartingStates.Countdown)
                remaining = (int)Mathf.Ceil(__instance.countDownTimer);
            EventTriggers.OnLobbyCancelStart(remaining);
        }
        catch (Exception ex)
        {
            LightLogger.LogWarning("[LobbyCancelStartPatch.Postfix] " + ex.Message);
        }
    }
}

#if DEBUG
/// <summary>
/// DEBUG 下允许使用 1 名玩家即可开始游戏（参考 Nebula Free-Play / TownOfNext）。
/// 仅在 DEBUG 编译构建时启用。
/// </summary>
[HarmonyPatch(typeof(GameStartManager))]
public static class DebugMinPlayersPatch
{
    [HarmonyPatch(nameof(GameStartManager.Start))]
    [HarmonyPostfix]
    public static void StartPostfix(GameStartManager __instance)
    {
        try
        {
            if (!AmongUsClient.Instance?.AmHost ?? true) return;
            __instance.MinPlayers = 1;
        }
        catch (Exception ex)
        {
            LightLogger.LogWarning("[DebugMinPlayersPatch.Start] " + ex.Message);
        }
    }

    [HarmonyPatch(nameof(GameStartManager.Update))]
    [HarmonyPostfix]
    public static void UpdatePostfix(GameStartManager __instance)
    {
        try
        {
            if (!AmongUsClient.Instance?.AmHost ?? true) return;
            __instance.MinPlayers = 1;
        }
        catch (Exception ex)
        {
            LightLogger.LogWarning("[DebugMinPlayersPatch.Update] " + ex.Message);
        }
    }
}
#endif
