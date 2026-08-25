using System;
using System.Collections.Generic;
using HarmonyLib;
using LightInDark.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace LightInDark.UI
{
    /// <summary>
    /// 会议中玩家目标按钮（Nebula Target / FS 范式）。
    /// 为每个 <see cref="PlayerVoteArea"/> 克隆一个目标按钮（模板取自原版 CancelButton），
    /// 点击回调可自定义（如 Guesser 选择击杀目标）。会议开始/结束时自动创建与清理。
    /// </summary>
    [HarmonyPatch(typeof(MeetingHud))]
    public static class MeetingTargetButton
    {
        private sealed class CallbackEntry
        {
            public Func<PlayerControl, bool> CanAdd;
            public Action<MeetingHud, PlayerControl> OnClick;
            public Sprite Icon;
        }

        private static readonly List<CallbackEntry> _callbacks = new();
        private static readonly Dictionary<byte, GameObject> _createdButtons = new();

        /// <summary>
        /// 注册一个会议目标按钮。对每位（满足条件的）玩家显示一个可点击按钮。
        /// </summary>
        /// <param name="onClick">点击某位玩家时回调（参数为该玩家 PlayerControl）。</param>
        /// <param name="canAdd">是否对该玩家添加按钮（可空，默认全显示）。</param>
        /// <param name="icon">按钮图标（可空，默认用原版 CancelButton 贴图）。</param>
        public static void Register(Action<MeetingHud, PlayerControl> onClick,
            Func<PlayerControl, bool> canAdd = null,
            Sprite icon = null)
        {
            _callbacks.Add(new CallbackEntry { OnClick = onClick, CanAdd = canAdd, Icon = icon });
        }

        [HarmonyPatch(nameof(MeetingHud.Start))]
        [HarmonyPostfix]
        public static void OnMeetingStart(MeetingHud __instance)
        {
            try
            {
                ClearCreatedButtons();
                if (_callbacks.Count == 0) return;
                if (PlayerControl.LocalPlayer?.Data?.IsDead == true) return;

                for (int i = 0; i < __instance.playerStates.Length; i++)
                {
                    var pva = __instance.playerStates[i];
                    if (pva == null) continue;

                    // 目标玩家
                    var player = GetPlayerById(pva.PlayerId);
                    if (player == null || pva.PlayerId == PlayerControl.LocalPlayer.PlayerId) continue;

                    GameObject created = null;
                    foreach (var cb in _callbacks)
                    {
                        if (cb.CanAdd != null && !cb.CanAdd(player)) continue;

                        var btn = CreateButtonForArea(pva);
                        BindClick(btn, __instance, player, cb);
                        created = btn;
                    }
                    _createdButtons[pva.PlayerId] = created;
                }
            }
            catch (Exception ex)
            {
                LightLogger.LogError("[MeetingTargetButton] OnMeetingStart", ex);
            }
        }

        [HarmonyPatch(nameof(MeetingHud.OnDestroy))]
        [HarmonyPostfix]
        public static void OnMeetingEnd()
        {
            ClearCreatedButtons();
        }

        private static GameObject CreateButtonForArea(PlayerVoteArea pva)
        {
            var tplRoot = pva?.Buttons?.transform?.Find("CancelButton");
            GameObject tpl = tplRoot != null ? tplRoot.gameObject : null;
            if (tpl == null) return null;

            var go = Object.Instantiate(tpl, pva.transform);
            go.name = "CustomTargetButton";
            go.transform.localPosition = new Vector3(-0.95f, 0.03f, -1.31f);
            return go;
        }

        private static void BindClick(GameObject go, MeetingHud meeting, PlayerControl player, CallbackEntry cb)
        {
            if (go == null) return;
            var passive = go.GetComponent<PassiveButton>();
            if (passive == null) return;

            // 换图标
            if (cb.Icon != null)
            {
                var sr = go.GetComponent<SpriteRenderer>();
                if (sr != null) sr.sprite = cb.Icon;
            }

            passive.OnClick = new Button.ButtonClickedEvent();
            passive.OnClick.AddListener((UnityEngine.Events.UnityAction)(() =>
            {
                try { cb.OnClick?.Invoke(meeting, player); }
                catch (Exception ex) { LightLogger.LogError("[MeetingTargetButton] click", ex); }
            }));
        }

        private static void ClearCreatedButtons()
        {
            foreach (var kv in _createdButtons)
                if (kv.Value != null) Object.Destroy(kv.Value);
            _createdButtons.Clear();
        }

        private static PlayerControl GetPlayerById(byte id)
        {
            foreach (var pc in PlayerControl.AllPlayerControls)
                if (pc.PlayerId == id) return pc;
            return null;
        }
    }
}