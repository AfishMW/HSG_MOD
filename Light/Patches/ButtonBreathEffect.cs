using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Light.Patches;

/// <summary>
/// FS-style static button effect manager (no injected MonoBehaviour).
/// Driven by MainMenuManager.LateUpdate postfix; self-stops when MainUI is gone.
/// </summary>
public static class ButtonBreathEffect
{
    private class ButtonState
    {
        public Vector3 BasePos;
        public Vector3 BaseScale;
        public float Phase;
        public int Id;
        public float HoverLerp;
        public float ClickLerp;
        public bool IsHovering;
        public SpriteRenderer? InactiveSr;
        public Color BaseColor;
        public GameObject? Shine;
        public PassiveButton? Button;
    }

    private static readonly Dictionary<GameObject, ButtonState> _states = new();
    private static int _nextId;
    private static readonly List<GameObject> _deadKeys = new();

    private const float FollowRadius = 0.15f;
    private const float FloatAmount = 0.006f;
    private const float BreathAmount = 0.014f;
    private const float ButtonScale = 0.93f;

    public static void Init()
    {
        _states.Clear();
        _nextId = 0;

        var leftPanel = GameObject.Find("LeftPanel");
        if (leftPanel == null) return;

        foreach (var btn in leftPanel.GetComponentsInChildren<PassiveButton>(true))
        {
            if (btn == null) continue;
            Register(btn);
        }
    }

    public static void Reload() => Init();

    private static void Register(PassiveButton pb)
    {
        var go = pb.gameObject;
        if (_states.ContainsKey(go)) return;

        var state = new ButtonState
        {
            BasePos = go.transform.localPosition,
            BaseScale = go.transform.localScale * ButtonScale,
            Phase = UnityEngine.Random.value * Mathf.PI * 2f,
            Id = _nextId++,
            Button = pb
        };
        go.transform.localScale = state.BaseScale;

        if (pb.inactiveSprites != null)
        {
            state.InactiveSr = pb.inactiveSprites.GetComponent<SpriteRenderer>();
            if (state.InactiveSr != null)
                state.BaseColor = state.InactiveSr.color;

            var shine = pb.inactiveSprites.transform.FindChild("Shine");
            if (shine != null)
            {
                state.Shine = shine.gameObject;
                var sr = shine.GetComponent<SpriteRenderer>();
                if (sr != null)
                {
                    var c = sr.color;
                    sr.color = new Color(c.r, c.g, c.b, 0f);
                }
            }
        }

        pb.OnMouseOver?.AddListener((UnityEngine.Events.UnityAction)(() => state.IsHovering = true));
        pb.OnMouseOut?.AddListener((UnityEngine.Events.UnityAction)(() => state.IsHovering = false));
        pb.OnClick?.AddListener((UnityEngine.Events.UnityAction)(() => state.ClickLerp = 1f));

        _states[go] = state;
    }

    public static void Update()
    {
        if (GameObject.Find("MainUI") == null) return;

        _deadKeys.Clear();
        foreach (var kvp in _states)
        {
            var obj = kvp.Key;
            var s = kvp.Value;

            try
            {
                if (obj == null || !obj.activeSelf) continue;
                if (s.Button == null || s.Button.gameObject == null)
                {
                    _deadKeys.Add(obj);
                    continue;
                }

                s.Phase += Time.deltaTime;

                float breath = BreathAmount + BreathAmount * 0.5f * Mathf.Sin(s.Phase * 0.8f + s.Id);
                float floatY = FloatAmount * Mathf.Sin(s.Phase * 1.2f + s.Id * 0.7f);

                float targetHover = s.IsHovering ? 1f : 0f;
                s.HoverLerp = Mathf.Lerp(s.HoverLerp, targetHover, Time.deltaTime * 10f);
                s.ClickLerp = Mathf.Lerp(s.ClickLerp, 0f, Time.deltaTime * 6f);

                float hoverScale = 1f + 0.08f * s.HoverLerp;
                float clickScale = 1f - 0.06f * s.ClickLerp;
                float totalScale = hoverScale * clickScale;

                Vector3 pos = s.BasePos + new Vector3(0f, floatY, 0f);

                if (s.IsHovering && Camera.main != null)
                {
                    Vector3 mouseWorld = Camera.main.ScreenToWorldPoint(Input.mousePosition);
                    Vector3 local = obj.transform.parent.InverseTransformPoint(mouseWorld);
                    Vector3 delta = local - s.BasePos;
                    float dist = delta.magnitude;
                    if (dist > FollowRadius)
                        delta = delta.normalized * FollowRadius;
                    pos += delta * 0.3f;
                }

                obj.transform.localScale = s.BaseScale * (totalScale + breath);
                obj.transform.localPosition = pos;

                if (s.InactiveSr != null)
                {
                    float pulse = Mathf.Sin(s.Phase * 1.5f + s.Id);
                    float brightness = 1f + 0.08f * pulse;
                    float hoverBoost = 0.15f * s.HoverLerp;
                    s.InactiveSr.color = new Color(
                        Mathf.Clamp01(s.BaseColor.r * brightness + hoverBoost),
                        Mathf.Clamp01(s.BaseColor.g * brightness + hoverBoost),
                        Mathf.Clamp01(s.BaseColor.b * brightness + hoverBoost),
                        s.BaseColor.a
                    );
                }

                if (s.Shine != null)
                {
                    s.Shine.SetActive(s.HoverLerp > 0.01f);
                    var sr = s.Shine.GetComponent<SpriteRenderer>();
                    if (sr != null)
                    {
                        var c = sr.color;
                        sr.color = new Color(c.r, c.g, c.b, 0.5f * s.HoverLerp);
                    }

                    if (s.IsHovering && Camera.main != null)
                    {
                        Vector3 mouseWorld = Camera.main.ScreenToWorldPoint(Input.mousePosition);
                        Vector3 local = s.Shine.transform.parent.InverseTransformPoint(mouseWorld);
                        s.Shine.transform.localPosition = new Vector3(
                            Mathf.Clamp(local.x, -0.5f, 0.5f),
                            Mathf.Clamp(local.y, -0.3f, 0.3f),
                            s.Shine.transform.localPosition.z
                        );
                    }
                }
            }
            catch
            {
                _deadKeys.Add(obj);
            }
        }

        foreach (var dead in _deadKeys)
            _states.Remove(dead);
    }
}
