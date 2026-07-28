using Nebula.Behavior;
using Nebula.Modules.GUIWidget;
using Nebula.Roles.Abilities;
using UnityEngine.Rendering;
using Virial.Media;
using Virial.Text;

namespace Nebula.SpecialModes.PaintQuiz;

internal class PaintQuizDrawingPhase
{
    private readonly GameObject holder;
    private readonly DyingMessageCanvas canvas;
    private (byte r, byte g, byte b, byte width, byte beginX, byte beginY, byte[] trajectory)[]? serialized = null;
    public bool IsSubmitted => serialized != null;

    private static readonly VColor[] PaletteColors =
    [
        // 黒・グレー系 + 消しゴム
        new(0.05f,  0.05f,  0.05f),           // 黒
        new(0.5f,   0.5f,   0.5f),            // グレー
        new(200f/255f, 200f/255f, 200f/255f), // 消しゴム (キャンバス背景色)
        // 赤系
        new(0.9f,  0.1f,  0.1f),   // 赤
        new(1.0f,  0.5f,  0.6f),   // ピンク
        new(0.6f,  0.05f, 0.15f),  // えんじ
        // 暖色系
        new(0.95f, 0.5f,  0.05f),  // 橙
        new(0.98f, 0.78f, 0.05f),  // 黄橙
        new(0.95f, 0.95f, 0.05f),  // 黄
        // 緑系
        new(0.55f, 0.9f,  0.15f),  // 黄緑
        new(0.1f,  0.75f, 0.2f),   // 緑
        new(0.05f, 0.45f, 0.1f),   // 深緑
        // 青系
        new(0.25f, 0.85f, 0.95f),  // 水色
        new(0.15f, 0.35f, 0.95f),  // 青
        new(0.05f, 0.1f,  0.6f),   // 紺
        // 紫・その他
        new(0.6f,  0.15f, 0.85f),  // 紫
        new(0.9f,  0.15f, 0.7f),   // マゼンタ
        new(0.55f, 0.3f,  0.05f),  // 茶
    ];

    private static readonly float[] BrushRadii = [0.004f, 0.0065f, 0.009f, 0.0115f];

    private static readonly DividedSpriteLoader BrushButtonSprite = DividedSpriteLoader.FromResource("Nebula.Resources.Quiz.BrushButton.png", 100f, 4, 1);
    private static readonly Image ColorButtonSprite = SpriteLoader.FromResource("Nebula.Resources.Quiz.ColorButton.png", 100f);
    private static readonly DividedSpriteLoader ControllButtonSprite = DividedSpriteLoader.FromResource("Nebula.Resources.Quiz.ControllButton.png", 100f, 2, 1);

    private int selectedBrush = 0;
    private int selectedColor = 0;
    private GameObject rightPanel = null!;
    private readonly Func<int> getSubmittedCount;
    private readonly int totalPlayers;
    private TMPro.TextMeshPro upperText;
    private Transform? questionTransform;

    public PaintQuizDrawingPhase(Transform parent, string questionText, string? achievedText, Func<int> getSubmittedCount)
    {
        this.getSubmittedCount = getSubmittedCount;
        
        this.totalPlayers = GamePlayer.AllPlayers.Count(p => !p.IsDisconnected);

        holder = UnityHelper.CreateObject("DrawingPhase", parent, new(0f, 0f, -50f));

        canvas = UnityHelper.CreateObject<DyingMessageCanvas>("Canvas", holder.transform, new(-0.7f, -0.3f, -0.1f));
        canvas.SetUpAsQuizPaint();
        canvas.SetBrush(new DyingMessageBrush(PaletteColors[0], BrushRadii[0]));

        var questionObj = new NoSGUIText(GUIAlignment.Center,
            GUI.API.GetAttribute(AttributeAsset.CenteredBold),
            new RawTextComponent(achievedText != null ? questionText + "<br>" + achievedText.Sized(80) : questionText))
        { PostBuilder = text => upperText = text }.Instantiate(new(7.5f, 1f), out _);

        if (questionObj != null)
        {
            questionObj.AddComponent<SortingGroup>();
            questionTransform = questionObj.transform;
            questionTransform.SetParent(holder.transform);
            questionTransform.localPosition = new(-0.3f, 1.85f, -0.2f);
        }

        BuildRightPanel();
    }

    private void SelectBrush(int idx) { selectedBrush = idx; UpdateBrush(); }
    private void SelectColor(int idx) { selectedColor = idx; UpdateBrush(); }

    private void UpdateBrush()
        => canvas.SetBrush(new DyingMessageBrush(PaletteColors[selectedColor], BrushRadii[selectedBrush]));

    private void BuildRightPanel()
    {
        // 右パネル全体を一つの親 GameObject にまとめる
        rightPanel = UnityHelper.CreateObject("RightPanel", holder.transform, Vector3.zero);

        // ブラシサイズ
        for (int i = 0; i < 4; i++)
        {
            int idx = i;
            var sr = UnityHelper.CreateObject<SpriteRenderer>("Brush_" + i, rightPanel.transform,
                new(3.75f + (i - 1.5f) * 0.45f, 1.4f, -0.2f));
            sr.sprite = BrushButtonSprite.GetSprite(i);
            var coll = sr.gameObject.AddComponent<CircleCollider2D>();
            coll.radius = 0.25f;
            coll.isTrigger = true;
            sr.gameObject.SetUpButton(true, sr).OnClick.AddListener(() => SelectBrush(idx));
        }

        // カラーパレット
        for (int i = 0; i < PaletteColors.Length; i++)
        {
            int idx = i;
            int col = i % 3;
            int row = i / 3;
            var sr = UnityHelper.CreateObject<SpriteRenderer>("Color_" + i, rightPanel.transform,
                new(3.75f + (col - 1f) * 0.68f, 0.65f - row * 0.45f, -0.2f));
            sr.sprite = ColorButtonSprite.GetSprite();
            var coll = sr.gameObject.AddComponent<BoxCollider2D>();
            coll.size = new(0.62f, 0.38f);
            coll.isTrigger = true;
            sr.gameObject.SetUpButton(true, sr, PaletteColors[i], PaletteColors[i].RGBMultiplied(0.95f)).OnClick.AddListener(() => SelectColor(idx));
        }

        void GenerateButton(string objName, string label, float x, Sprite buttonSprite, Action onClick, VColor color)
        {
            var renderer = UnityHelper.CreateObject<SpriteRenderer>(objName, rightPanel.transform, new(x, -2.2f, -0.2f), out var rendererObj, out var rendererTransform);
            renderer.sprite = buttonSprite;
            var col = rendererObj.AddComponent<BoxCollider2D>();
            col.size = new(0.5f, 0.5f);
            col.isTrigger = true;
            rendererObj.SetUpButton(true, renderer, selectedColor: color).OnClick.AddListener(onClick);

            var textObj = new NoSGUIText(GUIAlignment.Center, GUI.API.GetAttribute(AttributeAsset.CenteredBold), new RawTextComponent(label)).Instantiate(new(3f, 1f), out _);
            if(textObj.AsBoolFast())
            {
                var transform = textObj!.transform;
                transform.SetParent(rendererTransform);
                transform.localPosition = new(0f, -0.32f, -0.1f);
                textObj.AddComponent<SortingGroup>();
            }
        }

        GenerateButton("ClearButton", Language.Translate("paintQuiz.ui.canvas.clear"), 3.35f, ControllButtonSprite.GetSprite(0), () => canvas.Clear(new(200, 200, 200)), VColor.Red);
        GenerateButton("SubmitButton", Language.Translate("paintQuiz.ui.canvas.submit"), 4.15f, ControllButtonSprite.GetSprite(1), Submit, VColor.Green);
    }

    public void Submit()
    {
        if (serialized != null) return;
        canvas.Lock();
        serialized = canvas.SerializePaintQuiz();

        // 提出オーバーレイ（キャンバスの子として配置し、移動・縮小に追従させる）
        var overlay = UnityHelper.CreateObject<SpriteRenderer>("SubmitOverlay", canvas.transform,
            new(0f, 0f, -0.2f));
        overlay.sprite = NebulaAsset.WhiteImage.GetSprite();
        overlay.color = new Color(1f, 1f, 1f, 0.45f);
        overlay.transform.localScale = new(6.2f, 6.2f * 9f / 16f, 1f);

        // Arithmetic アニメーション値
        // 右パネル：右へスライドアウト
        var panelX = Arithmetic.Decel(0f, 7f, 0.48f);
        // キャンバス：中央少し上寄りへ移動 + 縮小
        var cvX     = Arithmetic.Decel(-0.7f, 0f,   0.48f);
        var cvY     = Arithmetic.Decel(-0.3f, 0.15f, 0.48f);
        var cvScale = Arithmetic.Decel(1f,    0.72f, 0.48f);
        // 問題文：x を中央に寄せる（y は変えない）
        var qtX = Arithmetic.Decel(-0.3f, 0f, 0.48f);
        var capturedQTransform = questionTransform;

        // 回答済み人数テキスト（提出後に表示）
        var capturedGetCount = getSubmittedCount;
        int capturedTotal = totalPlayers;
        var countObj = GUI.API.RealtimeText(GUIAlignment.Center, GUI.API.GetAttribute(AttributeAsset.CenteredBold), () => $"{capturedGetCount.Invoke()}/{capturedTotal} " + Language.Translate("paintQuiz.ui.done"), 25)
            .Instantiate(new(3f, 0.55f), out _);
        if (countObj.AsBoolFast())
        {
            var transform = countObj.transform;
            transform.SetParent(holder.transform);
            transform.localPosition = new(0f, -1.65f, -0.2f);
            countObj.AddComponent<SortingGroup>();
        }

        var rightTransform = rightPanel.transform;
        var canvasTransform = canvas.transform;
        holder.AddComponent<ScriptBehaviour>().UpdateHandler += () =>
        {
            rightTransform.localPosition = new(panelX.Value, 0f, 0f);
            canvasTransform.localPosition = new(cvX.Value, cvY.Value, -0.1f);
            canvasTransform.localScale    = new(cvScale.Value, cvScale.Value, 1f);
            if (capturedQTransform != null)
                capturedQTransform.localPosition = new(qtX.Value, 1.85f, -0.2f);
        };
    }

    public (byte r, byte g, byte b, byte width, byte beginX, byte beginY, byte[] trajectory)[] GetDrawing()
        => serialized ?? canvas.SerializePaintQuiz();

    public void SetActive(bool active) => holder.SetActive(active);

    public void Destroy()
    {
        if (canvas) GameObject.Destroy(canvas.gameObject);
        if (holder) GameObject.Destroy(holder);
    }
}
