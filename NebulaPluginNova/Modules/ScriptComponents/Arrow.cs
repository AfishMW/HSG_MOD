using Nebula.Modules.Cosmetics;
using Nebula.Patches;
using Virial;
using Virial.Events.Game;
using Virial.Game;

namespace Nebula.Modules.ScriptComponents;

public class Arrow : FlexibleLifespan, IGameOperator
{
    public enum DisappearanceType
    {
        FadeOut,
        Reduction,
    }

    private SpriteRenderer? arrowRenderer;
    private Virial.Compat.ModGameObject arrowRendererObj;
    private SpriteRenderer? smallRenderer = null;
    private Virial.Compat.ModGameObject? smallRendererObj = null;
    private SpriteRenderer? subRenderer = null;
    public VVector2 TargetPos;

    public GameObject ArrowObject => arrowRenderer!.gameObject;

    public bool IsAffectedByComms { get; set; } = false;
    public bool IsSmallenNearPlayer { get; set; } = true;
    public bool IsActive { get; set; } = true;
    public bool FixedAngle { get; set; } = false;
    public bool IsDisappearing { get; set; } = false;
    public DisappearanceType DisappearanceEffect { get; set; } = DisappearanceType.FadeOut;
    public bool WithSmallArrow => smallRenderer != null;
    public bool OnJustPoint { get; set; } = false;
    private bool showOnlyOutside = false;
    public bool ShowOnlyOutside { get => showOnlyOutside; set
        {
            showOnlyOutside = value;
            if (!showOnlyOutside) arrowRenderer?.gameObject.SetActive(false);
        }
    }
    private float disappearProgress = 0f;
    private static SpriteLoader arrowSprite = SpriteLoader.FromResource("Nebula.Resources.Arrow.png", 185f);
    private static SpriteLoader arrowSmallSprite = SpriteLoader.FromResource("Nebula.Resources.ArrowSmall.png", 360f);

    public Arrow(Sprite? sprite = null, bool usePlayerMaterial = true, bool withSmallArrow = false, bool withSubRenderer = false)
    {
        arrowRenderer = UnityHelper.CreateObject<SpriteRenderer>("Arrow", HudManager.Instance.transform, new(0, 0, -10f), LayerExpansion.GetArrowLayer());
        arrowRendererObj = arrowRenderer.ModGameObject(true);
        arrowRenderer.sprite = sprite ?? arrowSprite.GetSprite();
        arrowRenderer.sharedMaterial = usePlayerMaterial ? HatManager.Instance.PlayerMaterial : HatManager.Instance.DefaultShader;
        if (usePlayerMaterial) SetColor(VColor.White, VColor.Gray);
        if (withSmallArrow)
        {
            smallRenderer = UnityHelper.CreateObject<SpriteRenderer>("Arrow", arrowRenderer.transform, new(0, 0, -0.1f), LayerExpansion.GetArrowLayer());
            smallRendererObj = smallRenderer.ModGameObject();
            smallRenderer.sprite = arrowSmallSprite.GetSprite();
            FixedAngle = true;
        }
        if (withSubRenderer)
        {
            subRenderer = UnityHelper.CreateObject<SpriteRenderer>("Subrenderer", arrowRenderer.transform, new(0, 0, 0.01f), LayerExpansion.GetArrowLayer());
            subRenderer.sprite = null;
        }

        //if (ShowOnlyOutside) 
        arrowRendererObj.SetActive(false);
    }

    public void SetSprite(Sprite? sprite) => arrowRenderer!.sprite = sprite;
    public void SetSubSprite(Sprite? sprite, float z)
    {
        if(subRenderer.AsBoolFast())
        {
            subRenderer!.sprite = sprite;
            subRenderer.transform.localPosition = new(0f,0f,z);
        }
    }

    public Arrow HideArrowSprite()
    {
        arrowRenderer!.sprite = null;
        return this;
    }

    public Arrow SetColorByOutfit(NetworkedPlayerInfo.PlayerOutfit outfit)
    {
        return SetColor(DynamicPalette.PlayerColors[outfit.ColorId], DynamicPalette.ShadowColors[outfit.ColorId]);
    }

    public Arrow SetColor(VColor mainColor, VColor shadowColor)
    {
        arrowRenderer?.material.SetColor(PlayerMaterial.BackColor, shadowColor.ToUnityColor());
        arrowRenderer?.material.SetColor(PlayerMaterial.BodyColor, mainColor.ToUnityColor());
        return this;
    }

    public Arrow SetColor(VColor mainColor) => SetColor(mainColor, mainColor * 0.65f);
    public Arrow SetSmallColor(VColor smallColor)
    {
        if (smallRenderer.AsBoolFast()) smallRenderer!.color = smallColor.ToUnityColor();
        return this;
    }
    void IGameOperator.OnReleased()
    {
        if (arrowRenderer.AsBoolFast()) GameObject.Destroy(arrowRenderer!.gameObject);
        arrowRenderer = null;
    }

    private static float perc = 0.925f;
    void HudUpdate(GameHudUpdateEvent ev)
    {
        if (!arrowRenderer.AsBoolFast()) return;

        bool ovalMode = ClientOption.GetValue(ClientOption.ClientOptionType.ArrowRework) == 1;

        //視点中心からのベクトル

        //表示するカメラ
        Camera main = NebulaGameManager.Instance?.WideCamera.Camera ?? UnityHelper.FindCamera(LayerExpansion.GetUILayer())!;
        float mainOrthographicSize = main.orthographicSize;

        //距離を測るための表示用のカメラ
        Camera worldCam = (NebulaGameManager.Instance?.WideCamera.IsShown ?? false) ? NebulaGameManager.Instance.WideCamera.Camera : Camera.main;
        float worldOrthographicSize = worldCam.orthographicSize;

        float cameraMainOrthographicSize = Camera.main.orthographicSize;


        var del = (TargetPos - (VVector2)main.ModGameObject().Position);

        //目的地との見た目上の離れ具合
        float num = del.Magnitude / (worldOrthographicSize * perc);

        //近くの矢印を隠す
        bool flag = IsActive && (!IsSmallenNearPlayer || (double)num > 0.3);
        arrowRendererObj.SetActive(flag);
        if (!flag) return;

        bool Between(float value, float min, float max) => value > min && value < max;

        //スクリーン上の位置
        VVector2 viewportPoint = worldCam.WorldToViewportPoint(TargetPos.AsUnityVector3());

        if (ArrowUpdatePatch.InArea(ovalMode, viewportPoint))
        {
            if (ShowOnlyOutside) arrowRendererObj.SetActive(false);
            else
            {
                //画面内を指す矢印
                arrowRendererObj.LocalPosition = (del - (OnJustPoint ? VVector2.Zero : del.Normalized * (WithSmallArrow ? 0.9f : 0.6f) * (worldOrthographicSize / cameraMainOrthographicSize))).AsUnityVector3(2f);
                arrowRendererObj.LocalScale = IsSmallenNearPlayer ? VVector3.One * Mathn.Clamp(num, 0f, 1f) : VVector3.One;
            }
        }
        else
        {
            //画面外を指す矢印
            VVector2 vector3 = ArrowUpdatePatch.AdjustVector(ovalMode, viewportPoint);
            
            //UIのカメラに合わせて位置を調節する
            float num3 = mainOrthographicSize * main.aspect;
            VVector3 vector4 = new VVector3(Mathn.LerpUnclamped(0f, num3 * (WithSmallArrow ? 0.82f : 0.88f), vector3.x), Mathn.LerpUnclamped(0f, mainOrthographicSize * (WithSmallArrow ? 0.72f : 0.79f), vector3.y), 2f);
            arrowRendererObj.LocalPosition = vector4;
            arrowRendererObj.LocalScale = VVector3.One;
        }

        arrowRendererObj.LocalScale *= (worldOrthographicSize / cameraMainOrthographicSize);


        //角度の計算のために正規化する(しなくてもいいのかも)
        del = del.Normalized;

        if(FixedAngle)
            arrowRendererObj.LocalEulerAngles = new(0f, 0f, 0f);
        else
            arrowRendererObj.LocalEulerAngles = new(0f, 0f, Mathn.Atan2(del.y, del.x) * 180f / Mathn.PI);

        if(smallRenderer.AsBoolFast())
        {
            if (FixedAngle)
            {
                var angle = Mathn.Atan2(del.y, del.x) * 180f / Mathn.PI;
                smallRendererObj!.LocalPosition = (VVector2.Right.Rotate(angle) * 0.45f).AsVector3();
                smallRendererObj.LocalEulerAngles = new(0f, 0f, angle);
            }
            else
            {
                smallRendererObj!.LocalPosition = (VVector2.Right * 0.45f).AsVector3();
            }
        }

        if (IsDisappearing)
        {
            float a = 1f - disappearProgress;

            if (DisappearanceEffect == DisappearanceType.FadeOut)
            {
                disappearProgress += ev.DeltaTime * 0.85f;

                var lastColor = arrowRenderer.color;
                arrowRenderer.color = new Color(lastColor.r, lastColor.g, lastColor.b, a);
                if (smallRenderer != null) smallRenderer.color = new(1f, 1f, 1f, a);
            }else if(DisappearanceEffect == DisappearanceType.Reduction)
            {
                disappearProgress += ev.DeltaTime * 3.2f;
                arrowRendererObj.LocalScale *= a;
            }

            if (disappearProgress > 1f)
            {
                this.Release();
                disappearProgress = 1f;
            }
        }
    }

    public void MarkAsDisappering()
    {
        IsDisappearing = true;
    }

    public IEnumerator CoWaitAndDisappear(float waiting)
    {
        yield return new WaitForSeconds(waiting);
        IsDisappearing = true;
    }
}
