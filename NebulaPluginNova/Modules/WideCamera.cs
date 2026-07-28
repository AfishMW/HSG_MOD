using Il2CppInterop.Runtime.Injection;
using UnityEngine;
using Virial;
using Virial.Events.Game;
using Virial.Game;

namespace Nebula.Modules;

public interface INoisedCamera
{
    int CameraRoughness { get; }
}

public interface CameraAttention : ILifespan
{
    public record Attention(float eulerAngle, float view, VVector2 center);
    public Attention GetAttention();
}

public class SimpleAttention : CameraAttention
{
    private float eulerAngle, view;
    private VVector2 center;
    private ILifespan myLifespan;
    public SimpleAttention(float eulerAngle, float view, VVector2 center, ILifespan lifespan)
    {
        this.eulerAngle = eulerAngle;
        this.view = view;
        this.center = center;
        this.myLifespan = lifespan;
    }

    public bool IsDeadObject => myLifespan.IsDeadObject;

    CameraAttention.Attention CameraAttention.GetAttention() => new(eulerAngle, view, center);
}

public class FunctionalAttention : CameraAttention
{
    private Func<float> eulerAngle, view;
    private Func<Vector2> center;
    private ILifespan myLifespan;
    public FunctionalAttention(Func<float> eulerAngle, Func<float> view, Func<Vector2> center, ILifespan lifespan)
    {
        this.eulerAngle = eulerAngle;
        this.view = view;
        this.center = center;
        this.myLifespan = lifespan;
    }

    public bool IsDeadObject => myLifespan.IsDeadObject;

    CameraAttention.Attention CameraAttention.GetAttention() => new(eulerAngle(), view(), center());
}

public interface CustomCameraBehaviour
{
    void OnSet(ICustomWideCamera camera);
    float OrthographicSize { get; }
    void UpdateCamera(ICustomWideCamera camera, out VVector3 localPosition, out VVector2 localScale, out float localAngle);
}

public interface ICustomWideCamera
{
    void UpdateMesh(float x, float y);
    void UseRectShader();
    void UpdateRect(float u, float v);
    void SetSaturation(float saturation);
    void SetHue(float hue);
    void SetBrightness(float brightness);
}

public class WideCamera : ICustomWideCamera
{
    private GameObject myHolder;
    private Camera myCamera;
    private Virial.Compat.ModGameObject myCameraObj;

    private float targetRate = 1f; // エフェクト効果に依らない目標拡大率 Wideカメラを有効にしている時のみ掛け合わせられる。

    public bool IsShown => myCamera.gameObject.active;
    public float TargetRate { get => targetRate; set => targetRate = value; }
    public float CurrentRate => orthographicCache / 3f;
    public Camera Camera => myCamera;
    private MeshRenderer meshRenderer;
    private Virial.Compat.ModGameObject meshRendererObj;
    private MeshFilter meshFilter;
    private float meshAngleZ = 0f;
    private float orthographicCache = 3f;
    private Camera shadowCamera = null!;
    public Camera SubShadowCam { get; private set; } = null!;

    private CameraAttention? attention = null;
    private CameraAttention.Attention? attentionCache = null;
    private float attentionRate = 0f;

    public void SetAttention(CameraAttention attention)
    {
        this.attention = attention;
    }

    private CustomCameraBehaviour? customBehviour = null;
    public void SetCustomBehaviour(CustomCameraBehaviour? behaviour)
    {
        customBehviour = behaviour;
        customBehviour?.OnSet(this);
    }
    public void UseRectShader()
    {
        meshRenderer.material = new Material(NebulaAsset.HSVRectShader);
    }
    /// <summary>
    /// UseRectShaderを使用している場合にのみ効果あり。クリッピング範囲を設定する。
    /// </summary>
    /// <param name="u"></param>
    /// <param name="v"></param>
    public void UpdateRect(float u, float v)
    {
        var mat = meshRenderer.material;
        mat.SetFloat("_ClipU", u);
        mat.SetFloat("_ClipV", v);
    }

    public Virial.Compat.ModGameObject ViewerTransform => meshRendererObj;

    public WideCamera()
    {
        myHolder = UnityHelper.CreateObject("WideCam", HudManager.Instance.transform.parent, new(0f, 0f, 0f), out var myHolderTransform);

        myCamera = UnityHelper.CreateObject<Camera>("SubCam", myHolderTransform, new(0f, 0f, 0f));
        myCameraObj = myCamera.ModGameObject(true);
        myCamera.backgroundColor = Color.black;
        myCamera.allowHDR = false;
        myCamera.allowMSAA = false;
        myCamera.clearFlags = CameraClearFlags.SolidColor;
        myCamera.depth = 5;
        myCamera.nearClipPlane = -1000f;
        myCamera.orthographic = true;
        orthographicCache = myCamera.orthographicSize = 3;
        var customIgnoreShadow = myCameraObj.AddComponent<CustomIgnoreShadowCamera>();
        customIgnoreShadow.IgnoreShadow = () => !DrawShadow;
        SetDrawShadow(true);

        var blackCam = UnityHelper.CreateObject<Camera>("BlackCam", myHolderTransform, new(0f, 0f, 0f));
        blackCam.backgroundColor = Color.black;
        blackCam.allowHDR = false;
        blackCam.allowMSAA = false;
        blackCam.clearFlags = CameraClearFlags.SolidColor;
        blackCam.cullingMask = 0;
        blackCam.depth = 4;
        blackCam.nearClipPlane = -1000f;
        blackCam.orthographic = true;
        blackCam.orthographicSize = 3;

        var collider = UnityHelper.CreateObject<BoxCollider2D>("ClickGuard", myHolderTransform, new(0f, 0f, -1f));
        collider.size = new(100f, 100f);
        collider.isTrigger = true;
        collider.gameObject.layer = LayerExpansion.GetShipLayer();
        var button = collider.gameObject.SetUpButton();
        button.OnClick.AddListener(() =>
        {
            var cameraPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            UnityEngine.Vector2 worldPos = ConvertToWorldPos(cameraPos); //OverlapPointの引数で使用
            int layer = (1 << LayerExpansion.GetShortObjectsLayer()) | (1 << LayerExpansion.GetObjectsLayer());

            PassiveUiElement? passiveButton = null;
            foreach (var button in PassiveButtonManager.Instance.Buttons.GetFastEnumerator())
            {
                //船およびオブジェクトレイヤーのボタンが対象
                if (((1 << button.gameObject.layer) & layer) == 0) continue;
                if (!button.Colliders.Any(c => c && c.OverlapPoint(worldPos))) continue;
                if (passiveButton.AsBoolFast() && passiveButton!.transform.GetPositionFast().z < button.transform.GetPositionFast().z) continue;

                //Debug.Log("Button");
                passiveButton = button;
            }

            if (passiveButton.AsBoolFast()) passiveButton.ReceiveClickDown();
        });

        myHolder.gameObject.SetActive(false);

        (meshRenderer, meshFilter) = UnityHelper.CreateMeshRenderer("mesh", myHolderTransform, new(0f, 0f, 10f), LayerExpansion.GetUILayer());
        meshRendererObj = meshRenderer.ModGameObject();
        meshRenderer.material = new Material(NebulaAsset.HSVNAShader);
        rendererSharedMaterial = meshRenderer.sharedMaterial;

        hueVal = new ValueObserver<float>(0f, val => rendererSharedMaterial.SetFloat("_Hue", val));
        saturationVal = new ValueObserver<float>(1f, val => rendererSharedMaterial.SetFloat("_Sat", val));
        brightnessVal = new ValueObserver<float>(1f, val => rendererSharedMaterial.SetFloat("_Val", val));

        SetUp();
        SetUpShadowCam();
    }

    private Material rendererSharedMaterial;

    public bool DrawShadow => drawShadow && !(NebulaGameManager.Instance?.IgnoreWalls ?? false);
    private bool drawShadow = false;
    public void SetDrawShadow(bool drawShadow) => SetCustomShadow(drawShadow, true, true);

    public void SetCustomShadow(bool drawShadow, bool drawPlayer, bool drawArrow) {
        myCamera.cullingMask = drawShadow ? 97047 : 31511;
        if(drawArrow) myCamera.cullingMask |= 1 << LayerExpansion.GetArrowLayer();
        if(drawShadow && drawPlayer) myCamera.cullingMask |= 1 << LayerExpansion.GetPlayerWithShadowLayer();
        if (!drawPlayer) myCamera.cullingMask &= ~(1 << LayerExpansion.GetPlayersLayer());

        this.drawShadow = drawShadow;
    }

    private void SetUp()
    {
        myCamera.backgroundColor = Color.black;
        myHolder.gameObject.SetActive(true);
        var shadowCollab = AmongUsUtil.GetShadowCollab();
        shadowCollab.ShadowQuad.transform.SetParent(myCamera.transform, false);
        shadowCollab.ShadowCamera.transform.SetParent(myCamera.transform, false);
        shadowCamera = shadowCollab.ShadowCamera;

        Roughness = 1;
    }

    private void SetUpShadowCam()
    {
        var shadowCam = AmongUsUtil.GetShadowCollab().ShadowCamera;
        var newCam = UnityHelper.CreateRenderingCamera("SubShadowCam", shadowCam.transform, Vector3.zero, shadowCam.orthographicSize, shadowCam.cullingMask);
        newCam.depth = shadowCam.depth;
        shadowCam.cullingMask |= 1 << LayerExpansion.GetVanillaShadowLightLayer();
        shadowCam.cullingMask |= 1 << LayerExpansion.GetPlayerWithShadowLayer();
        var origTexture = shadowCam.targetTexture;
        var newTexture = new RenderTexture(origTexture.width, origTexture.height, origTexture.depth, origTexture.format, origTexture.mipmapCount);
        newCam.targetTexture = newTexture;
        newCam.backgroundColor = new(0f, 0f, 0f, 0f);
        SubShadowCam = newCam;
    }

    public void OnGameStart()
    {
        ReflectShipColor();
    }

    public void ReflectShipColor()
    {
        myCamera.backgroundColor = AmongUsLLImpl.ShipStatusInstance.CameraColor;
    }
    private static int gcd(int n1, int n2)
    {
        static int gcdInner(int _n1, int _n2) => _n2 == 0 ? _n1 : gcdInner(_n2, _n1 % _n2);
        return n1 > n2 ? gcdInner(n1, n2) : gcdInner(n2, n1);
    }
    
    private int roughness = 1;
    private int lastCommandRoughness = 1;
    public int Roughness { get => roughness * (int)((AmongUsUtil.CurrentCamTarget as INoisedCamera)?.CameraRoughness ?? 1f); set
        {

            int max = gcd(NebulaAPI.AmongUs.ScreenHeight, NebulaAPI.AmongUs.ScreenWidth);
            if (max < value) roughness = value;

            int temp = value;
            while (temp < max && (NebulaAPI.AmongUs.ScreenHeight % temp != 0 || NebulaAPI.AmongUs.ScreenWidth % temp != 0)) temp++;
            roughness = temp;
        }
    } 

    private int consideredWidth => (NebulaAPI.AmongUs.ScreenWidth / Roughness);
    private int consideredHeight => (NebulaAPI.AmongUs.ScreenHeight / Roughness);

    public bool HasAttention => attention != null;

    public void CheckPlayerState(out VVector3 localScale, out float localRotateZ)
    {
        localScale = new(1f, 1f, 1f);

        var p = GamePlayer.LocalPlayer;
        if (p == null)
        {
            localRotateZ = 0f;
            return;
        }

        if (p.Unbox().CountAttribute(PlayerAttributes.FlipX) % 2 == 1) localScale.x = -1f;
        if (p.Unbox().CountAttribute(PlayerAttributes.FlipY) % 2 == 1) localScale.y = -1f;
        localRotateZ = 180f * p.Unbox().CountAttribute(PlayerAttributes.FlipXY);
    }

    //カメラ上の位置を表すワールド座標を計算します。
    public VVector3 ConvertToWideCameraPos(VVector3 worldPosition)
    {
        var localPos = (worldPosition - myCameraObj.Position);
        //カメラの拡大縮小
        localPos /= myCamera.orthographicSize / 3f;
        //反転エフェクトの効果
        var localScale = ViewerTransform.LocalScale;
        localPos.x *= localScale.x;
        localPos.y *= localScale.y;
        return myCameraObj.Position + localPos.RotateZ(ViewerTransform.LocalEulerAngles.z);
    }

    public VVector2 ConvertToWorldPos(VVector2 cameraWorldPosition)
    {
        VVector2 cameraPos = myCameraObj.Position;
        var localPos = cameraWorldPosition - cameraPos;
        localPos = localPos.Rotate(-ViewerTransform.LocalEulerAngles.z);
        try
        {
            localPos.x /= ViewerTransform.LocalScale.x;
        }
        catch
        {
            localPos.x = 0f;
        }
        try
        {
            localPos.y /= ViewerTransform.LocalScale.y;
        }
        catch
        {
            localPos.y = 0f;
        }
        localPos *= myCamera.orthographicSize / 3f;
        return cameraPos + localPos;
    }

    private void FixVentArrow()
    {
        var localPlayer = GamePlayer.LocalPlayer;
        if (AmongUsLLImpl.TryGetShipStatus(out var ship) && localPlayer != null)
        {
            var playerPos = localPlayer.Position;
            if (ship.AllVents.Count > 0)
            {
                var vent = ship.AllVents.GetFastEnumerator().MinBy(v => v.ModGameObject(false).Position.Distance(playerPos));
                if (vent)
                {
                    var myVentPos = NebulaGameManager.Instance!.WideCamera.ConvertToWideCameraPos(vent!.ModGameObject(false).Position);

                    int length = vent!.NearbyVents.Length;
                    for (int i = 0; i < length; i++)
                    {
                        var targetVent = vent.NearbyVents[i];
                        if (targetVent)
                        {
                            var targetVentPos = NebulaGameManager.Instance!.WideCamera.ConvertToWideCameraPos(targetVent.ModGameObject(false).Position);

                            var diff = (targetVentPos - myVentPos).AsVector2().Normalized;
                            diff *= 0.7f + vent.spreadShift;
                            var pos = (myVentPos + diff.AsVector3());
                            pos.z = -10f;
                            var transform = vent.Buttons[i].ModGameObject(false);
                            transform.Position = pos;
                            transform.LocalEulerAngles = new(0f, 0f, Mathn.Atan2(diff.y, diff.x) / Mathn.PI * 180f);
                        }
                    }
                }
            }
        }
    }

    /// <summary>
    /// メッシュの大きさを変更する。
    /// </summary>
    /// <param name="x"></param>
    /// <param name="y"></param>
    void ICustomWideCamera.UpdateMesh(float x, float y) {
        meshRenderer.sharedMaterial.mainTexture = myCamera.SetCameraRenderTexture((int)(x * 100), (int)(y * 100));
        meshFilter.CreateRectMesh(new(x, y));
    }

    public void Update()
    {
        if (myCameraObj.ActiveSelf) {
            if (customBehviour != null)
            {
                customBehviour.UpdateCamera(this, out var localPos, out var localScale, out var localAngle);
                var orthographicSize = customBehviour.OrthographicSize;
                myCamera.orthographicSize = orthographicSize;
                shadowCamera.orthographicSize = orthographicSize;
                SubShadowCam.orthographicSize = orthographicSize;

                meshRendererObj.LocalEulerAngles = new(0f, 0f, localAngle);
                meshRendererObj.LocalPosition = localPos;
                meshRendererObj.LocalScale = localScale.AsUnityVector3(1f);
                return;
            }

            //カメラの注目を制御する
            if (attention?.IsDeadObject ?? false) attention = null;

            bool hasAttention = attention != null;
            if (attention != null) attentionCache = attention.GetAttention();

            //注目の寄与の程度を更新
            if (attentionCache == null)
                attentionRate = 0f;
            else {
                attentionRate += ((hasAttention ? 1f : 0f) - attentionRate).Delta(hasAttention ? 12f : 6f, 0.05f);
                myCameraObj.LocalPosition = (attentionCache.center.AsVector3() - myHolder.ModGameObject(false).Position) * attentionRate;
                myCameraObj.LocalEulerAngles = new(0f, 0f, attentionCache.eulerAngle * attentionRate);
            }

            //
            if (!myCamera.targetTexture.AsBoolFast(out var targetTex) || targetTex.width != consideredWidth || targetTex.height != consideredHeight)
            {
                //割り切れないときは再設定
                if(NebulaAPI.AmongUs.ScreenWidth % roughness != 0 || NebulaAPI.AmongUs.ScreenHeight % roughness != 0) Roughness = roughness;

                meshRenderer.sharedMaterial.mainTexture = myCamera.SetCameraRenderTexture(consideredWidth, consideredHeight);

                meshFilter.CreateRectMesh(new(Camera.main.orthographicSize / NebulaAPI.AmongUs.ScreenHeight * NebulaAPI.AmongUs.ScreenWidth * 2f, Camera.main.orthographicSize * 2f));
            }

            CheckPlayerState(out var goalScale, out var goalRotate);
            while (meshAngleZ - goalRotate > 360f) meshAngleZ -= 360f;
            while (meshAngleZ - goalRotate < -360f) meshAngleZ += 360f;
            meshAngleZ -= (meshAngleZ - goalRotate).Delta(2.7f, 0.11f);

            meshRendererObj.LocalScale -= (meshRendererObj.LocalScale - goalScale).Delta(2.4f, 0.003f);
            meshRendererObj.LocalEulerAngles = new(0f, 0f, meshAngleZ);

            float targetRateByEffect = GamePlayer.LocalPlayer?.Unbox().CalcAttributeVal(PlayerAttributes.ScreenSize, true) ?? 1f;

            float currentOrth = orthographicCache;
            float targetOrth = targetRate * targetRateByEffect * 3f;
            float diff = currentOrth - targetOrth;
            bool reached = Mathn.Abs(diff) < 0.001f;

            if (reached)
                currentOrth = targetOrth;
            else
                currentOrth -= (currentOrth - targetOrth) * FastMethods.GetDeltaTimeFast() * 5f;

            orthographicCache = currentOrth;
            float attentionViewRate = 3f * (attentionCache?.view ?? 1f);
            float actualOrth = Mathn.Lerp(currentOrth, attentionViewRate, attentionRate);
            float actualTargetOrth = Mathn.Lerp(targetOrth, attentionViewRate, attentionRate);
            myCamera.orthographicSize = actualOrth;
            shadowCamera.orthographicSize = actualOrth;
            SubShadowCam.orthographicSize = actualOrth;
            SubShadowCam.aspect = shadowCamera.aspect;
            myCameraObj.LocalScale = new Vector3(actualOrth / 3f, actualOrth / 3f, 1f);

            //コマンドによるモザイクの設定値に変化が生じたら再計算する
            int currentCommandRoughness = Mathn.Max(1, (int?)GamePlayer.LocalPlayer?.Unbox().CalcAttributeVal(PlayerAttributes.Roughening, true) ?? 1);
            if(lastCommandRoughness != currentCommandRoughness)
            {
                lastCommandRoughness = currentCommandRoughness;
                Roughness = lastCommandRoughness;
            }

            var camUpdateEv = GameOperatorManager.Instance?.Run<CameraUpdateEvent>(CameraUpdateEvent.Get());
            SetSaturation(camUpdateEv?.GetSaturation() ?? 1f);
            SetHue(camUpdateEv?.GetHue() ?? 0f);
            SetBrightness(camUpdateEv?.GetBrightness() ?? 1f);
            meshRenderer.sharedMaterial.color = (camUpdateEv?.Color ?? VColor.White).ToUnityColor();

            FixVentArrow();
        }
    }

    public void SetActive(bool active)
    {
        meshRenderer.gameObject.SetActive(active);
    }

    private readonly ValueObserver<float> hueVal;
    private readonly ValueObserver<float> saturationVal;
    private readonly ValueObserver<float> brightnessVal;
    private void SetHue(float hue) => hueVal.Set(hue);
    private void SetSaturation(float saturation) => saturationVal.Set(saturation);
    private void SetBrightness(float brightness) => brightnessVal.Set(brightness);
    void ICustomWideCamera.SetHue(float hue) => SetHue(hue);
    void ICustomWideCamera.SetSaturation(float saturation) => SetSaturation(saturation);
    void ICustomWideCamera.SetBrightness(float brightness) => SetBrightness(brightness);

}
