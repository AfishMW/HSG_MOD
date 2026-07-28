using Nebula.Behavior;
using Nebula.Modules;
using Nebula.Modules.Cosmetics;
using Nebula.Modules.GUIWidget;
using Nebula.Roles.Abilities;
using System.Reflection.Metadata;
using UnityEngine.Rendering;
using Virial.Media;
using Virial.Text;

namespace Nebula.SpecialModes.PaintQuiz;

internal class PaintQuizReviewPhase
{
    private readonly GameObject holder;
    private bool confirmed = false;
    public bool IsConfirmed => confirmed;

    private struct DrawingCell
    {
        public byte PlayerId;
        public GameObject Holder;
        public MeshRenderer DrawingMesh;
        public Material DrawingMaterial;
        public GameObject StampHolder;
        public TMPro.TextMeshPro? ScoreText;
        public GameObject? GradePreviewHolder;
    }

    private DrawingCell[] cells = [];
    private byte? selectedId = null;
    private readonly Dictionary<byte, byte> grades = [];
    private readonly (byte id, (byte r, byte g, byte b, byte width, byte beginX, byte beginY, byte[] trajectory)[] trajectories)[] allDrawings;
    private bool amHost;
    private byte lastGrade = 3;
    private GameObject? confirmBtn = null;
    private GameObject? answerObj = null;
    private GameObject? waitScoringObj = null;
    private GameObject? hintObj = null;

    private float drawW, drawH;

    private const float UV_MARGIN = (7f / 16f / 2f);

    private GamePlayer hostPlayer;

    public PaintQuizReviewPhase(
        GUIWidgetSupplier? relatedInformation,
        Transform parent,
        (byte id, (byte r, byte g, byte b, byte width, byte beginX, byte beginY, byte[] trajectory)[] trajectories)[] drawings,
        string questionText, string answerText, bool amHost, bool hasAnswer)
    {
        this.allDrawings = drawings;
        this.amHost = amHost;
        if (!GamePlayer.AllPlayers.Find(p => p.AmHost, out hostPlayer!)) hostPlayer = GamePlayer.LocalPlayer!;

        holder = UnityHelper.CreateObject("ReviewPhase", parent, new(0f, 0f, -30f));

        var questionObj = new NoSGUIText(GUIAlignment.Center,
            GUI.API.GetAttribute(AttributeAsset.OverlayTitle),
            new RawTextComponent(questionText))
            .Instantiate(new(9f, 1f), out _);
        if (questionObj != null)
        {
            questionObj.AddComponent<SortingGroup>();
            var transform = questionObj.transform;
            transform.SetParent(holder.transform);
            transform.localPosition = new(0f, 2.4f, -0.1f);
        }

        if (!amHost)
        {
            waitScoringObj = new NoSGUIText(GUIAlignment.Center,
                GUI.API.GetAttribute(AttributeAsset.OverlayContent),
                new RawTextComponent(Language.Translate("paintQuiz.ui.waitScoring"))).Instantiate(new(9f, 0.4f), out _);
            if (waitScoringObj != null)
            {
                waitScoringObj.AddComponent<SortingGroup>();
                var transform = waitScoringObj.transform;
                transform.SetParent(holder.transform);
                transform.localPosition = new(0f, 2.05f, -0.1f);
            }
        }

        answerObj = new NoSGUIText(GUIAlignment.Center,
            GUI.API.GetAttribute(AttributeAsset.OverlayContent),
            new RawTextComponent(hasAnswer ? $"{Language.Translate("paintQuiz.ui.answer")}：{answerText}" : Language.Translate("paintQuiz.ui.noAnswer")))
        { OverlayWidget = relatedInformation }
            .Instantiate(new(9f, 0.4f), out _);
        if (answerObj != null)
        {
            answerObj.AddComponent<SortingGroup>();
            var transform = answerObj.transform;
            transform.SetParent(holder.transform);
            transform.localPosition = new(0f, 2.05f, -0.1f);
            answerObj.SetActive(amHost);
        }

        BuildGrid(drawings);

        if (amHost)
        {
            var confirmWidget = GUI.API.RawButton(GUIAlignment.Center,
                GUI.API.GetAttribute(AttributeAsset.StandardMediumMasked), Language.Translate("paintQuiz.ui.finishReview"),
                _ =>
                {
                    confirmed = true;
                    confirmBtn?.SetActive(false);
                });
            confirmBtn = confirmWidget.Instantiate(new(10f, 10f), out _);
            if (confirmBtn != null)
            {
                confirmBtn.transform.SetParent(holder.transform);
                confirmBtn.transform.localPosition = new(0f, -2.4f, -0.2f);
                confirmBtn.AddComponent<SortingGroup>();
            }

            Tutorial.ShowTutorial(new TutorialBuilder(new(0f, -3.7f)).AsSimpleTitledOnceTextWidget("paintQuiz.review").ShowWhile(() => !confirmed).Duration(15f));

            GUIWidget MakeNavEntry(int mouseButtonIndex, string text) =>
                GUI.API.HorizontalHolder(GUIAlignment.Left,
                    new NoSGUIImage(GUIAlignment.Left, NebulaSettingMenu.MouseButton.AsLoader(mouseButtonIndex), new(0.25f, 0.25f)) { IsMasked = false },
                    GUI.API.RawText(GUIAlignment.Left, GUI.API.GetAttribute(AttributeAsset.OverlayContent), text));

            var hintWidget = GUI.API.HorizontalHolder(GUIAlignment.Left,
                MakeNavEntry(0, Language.Translate("paintQuiz.ui.hint.leftClick")),
                GUI.API.HorizontalMargin(0.15f),
                MakeNavEntry(1, Language.Translate("paintQuiz.ui.hint.rightClick")));

            hintObj = hintWidget.Instantiate(new(6f, 0.5f), out var size);
            if (hintObj.AsBoolFast())
            {
                hintObj!.AddComponent<SortingGroup>();
                hintObj.transform.SetParent(holder.transform);
                var aspectPosition = hintObj.AddComponent<AspectPosition>();
                aspectPosition.Alignment = AspectPosition.EdgeAlignments.RightBottom;
                aspectPosition.DistanceFromEdge = new(size.Width * 0.5f + 0.1f, size.Height * 0.5f + 0.1f);
                aspectPosition.AdjustPosition();
            }
        }
    }

    private static (int cols, int rows, float previewD, float resultSize) GetLayout(int count) => count switch
    {
        <= 1 => (1, 1, 0.3f, 1f),
        <= 2 => (2, 1, 0.3f, 1f),
        <= 4 => (2, 2, 0.3f, 1f),
        <= 6 => (3, 2, 0.3f, 0.8f),
        <= 9 => (3, 3, 0.3f, 0.7f),
        <= 12 => (4, 3, 0.22f, 0.5f),
        <= 15 => (5, 3, 0.15f, 0.5f),
        <= 16 => (4, 4, 0.22f, 0.45f),
        <= 20 => (5, 4, 0.15f, 0.45f),
        _ => (6, 4, 0.1f, 0.45f),
    };


    private float resultSize;
    private void BuildGrid((byte id, (byte, byte, byte, byte, byte, byte, byte[])[] traj)[] drawings)
    {
        int count = drawings.Length;
        var (cols, rows, previewD, resultSize) = GetLayout(count);
        this.resultSize = resultSize;

        float areaW = 9.0f;
        float areaH = 3.8f;

        float cellW = areaW / cols;
        float cellH = areaH / rows;
        drawW = cellW * 0.88f;
        drawH = drawW * 9f / 16f;
        if (drawH > cellH * 0.75f)
        {
            drawH = cellH * 0.75f;
            drawW = drawH * 16f / 9f;
        }

        cells = new DrawingCell[count];

        var materialColor = new Color(0.85f, 0.85f, 0.85f);
        var hoveredColor = new Color(1f, 1f, 1f);

        for (int i = 0; i < count; i++)
        {
            int col = i % cols;
            int row = i / cols;
            float cx = (col - (cols - 1) * 0.5f) * cellW;
            float cy = ((rows - 1) * 0.5f - row) * cellH - 0.1f;

            var cellHolder = UnityHelper.CreateObject("Cell_" + i, holder.transform, new(cx, cy, 0f));

            var meshObj = UnityHelper.CreateMeshRenderer("Mesh", cellHolder.transform, new(0f, 0f, 0f), LayerExpansion.GetUILayer());
            meshObj.filter.CreateRectMesh(new(drawW, drawH), null, 0f, 1f, UV_MARGIN, 1f - UV_MARGIN);
            var meshObjRenderer = meshObj.renderer;
            meshObjRenderer.material = UnityHelper.GetMeshRendererMaterial();
            var meshObjMaterial = meshObjRenderer.material;
            meshObjMaterial.color = materialColor;

            var nameObj = new NoSGUIText(GUIAlignment.Center,
                GUI.API.GetAttribute(AttributeAsset.OverlayContent),
                new RawTextComponent(GetPlayerName(drawings[i].id)))
                .Instantiate(new(cellW, 0.3f), out _);
            if (nameObj != null)
            {
                var transform = nameObj.transform;
                transform.SetParent(cellHolder.transform);
                transform.localPosition = new(0f, -drawH * 0.5f - 0.12f, -0.1f);
                nameObj.AddComponent<SortingGroup>();
            }

            TMPro.TextMeshPro? scoreText = null;
            new NoSGUIText(GUIAlignment.Center, GUI.API.GetAttribute(AttributeAsset.CenteredBoldFixed), new RawTextComponent(""))
            {
                PostBuilder = t =>
                {
                    var transform = t.transform;
                    transform.SetParent(cellHolder.transform);
                    transform.localPosition = new(0f, drawH * 0.5f - 0.01f, -0.1f);
                    transform.gameObject.SetActive(false);
                    scoreText = t;
                }
            }.Instantiate(new(cellW, 0.35f), out _);

            var stampHolder = UnityHelper.CreateObject("Stamp", cellHolder.transform, new(0f, 0f, -0.2f));
            stampHolder.SetActive(false);

            // 採点プレビュー
            GameObject? gradePreviewHolder = null;
            if (amHost)
            {
                gradePreviewHolder = UnityHelper.CreateObject("GradePreview", cellHolder.transform,
                    new(drawW * 0.5f - previewD, -drawH * 0.5f + previewD, -0.15f));
                gradePreviewHolder.SetActive(false);
            }

            var clickArea = cellHolder.AddComponent<BoxCollider2D>();
            clickArea.size = new(drawW, drawH);
            clickArea.isTrigger = true;
            var btn = cellHolder.SetUpButton();
            var capturedCellHolder = cellHolder;
            int capturedI = i;
            if (amHost)
            {
                btn.OnClick.AddListener(() =>
                {
                    if (!confirmed) OpenGradeMenu(capturedI, capturedCellHolder);
                });
                cellHolder.AddComponent<ExtraPassiveBehaviour>().OnRightClicked = () =>
                {
                    if (!confirmed) ApplyLastGrade(capturedI);
                };
            }
            btn.OnMouseOver.AddListener(() => meshObjMaterial.color = hoveredColor);
            btn.OnMouseOut.AddListener(() => meshObjMaterial.color = materialColor);

            cells[i] = new DrawingCell
            {
                PlayerId = drawings[i].id,
                Holder = cellHolder,
                DrawingMesh = meshObjRenderer,
                DrawingMaterial = meshObjMaterial,
                StampHolder = stampHolder,
                ScoreText = scoreText,
                GradePreviewHolder = gradePreviewHolder,
            };

            grades[drawings[i].id] = 0;
        }
    }

    private void ApplyLastGrade(int cellIndex)
    {
        var playerId = cells[cellIndex].PlayerId;
        grades[playerId] = lastGrade;
        UpdateGradePreview(playerId, lastGrade);
    }

    private void OpenGradeMenu(int cellIndex, GameObject cellHolder)
    {
        selectedId = cells[cellIndex].PlayerId;

        var worldPos = cellHolder.transform.GetPositionFast();
        var hudLocalPos = AmongUsLLImpl.HudManagerBridge.MyTransform.InverseTransformPoint(worldPos);
        NebulaManager.Instance.ShowRingMenu(
            BuildGradeElements(cells[cellIndex].PlayerId),
            () => !confirmed,
            null,
            new VVector2(hudLocalPos.x, hudLocalPos.y));
    }

    private RingMenu.RingMenuElement[] BuildGradeElements(byte playerId)
    {
        var elements = new RingMenu.RingMenuElement[5];
        byte[] gradeOrder = [1, 2, 3, 4, 0];
        for (int i = 0; i < gradeOrder.Length; i++)
        {
            byte grade = gradeOrder[i];
            elements[i] = new RingMenu.RingMenuElement(
                MakeGradeWidget(grade),
                () =>
                {
                    grades[playerId] = grade;
                    if (grade != 0) lastGrade = grade;
                    UpdateGradePreview(playerId, grade);
                });
        }
        return elements;
    }

    private void UpdateGradePreview(byte playerId, byte grade)
    {
        var cellIdx = Array.FindIndex(cells, c => c.PlayerId == playerId);
        if (cellIdx < 0) return;

        var holder = cells[cellIdx].GradePreviewHolder;
        if (!holder.AsBoolFast()) return;

        if (grade == 0)
        {
            holder!.SetActive(false);
            return;
        }

        // 既存コンテンツをクリアして再構築
        holder!.transform.DestroyChildren();

        holder.SetActive(true);

        // スタンプ
        var stamp = PaintQuizStampConfig.GetStamp(grade);
        if (stamp != null)
        {
            var stampWidget = stamp.GetStampWidget(null, PlayerControl.LocalPlayer?.PlayerId ?? 0,
                GUIAlignment.Center, false, 0.22f);
            var stampObj = stampWidget.Instantiate(new(10f, 10f), out _);
            if (stampObj != null)
            {
                stampObj.transform.SetParent(holder.transform);
                stampObj.transform.localPosition = new(0f, 0.06f, -0.05f);
            }
        }

        // pt テキスト
        float pts = PaintQuizScoreMap.GradeToPoints(grade);
        TMPro.TextMeshPro? previewTmp = null;
        var textObj = new NoSGUIText(GUIAlignment.Center,
            GUI.API.GetAttribute(AttributeAsset.OverlayContent),
            new RawTextComponent(PaintQuizScoreMap.FormatScoreWithSign(pts) + "pt"))
            { PostBuilder = t => previewTmp = t }
            .Instantiate(new(0.7f, 0.3f), out _);
        if (textObj.AsBoolFast())
        {
            textObj.AddComponent<SortingGroup>();
            var transform = textObj.transform;
            transform.SetParent(holder.transform);
            transform.localPosition = new(0f, -0.13f, -0.05f);
            if (previewTmp != null) previewTmp.color = pts >= 0f ? Color.white : new UnityEngine.Color(0.8f, 0.8f, 0.8f);
        }
    }

    private GUIWidgetSupplier MakeGradeWidget(byte grade) => () =>
    {
        if (grade == 0)
            return GUI.API.VerticalHolder(GUIAlignment.Center, GUI.API.RawText(GUIAlignment.Center, GUI.API.GetAttribute(AttributeAsset.OverlayContent), Language.Translate("paintQuiz.ui.noScore")));

        var stamp = PaintQuizStampConfig.GetStamp(grade);
        float pts = PaintQuizScoreMap.GradeToPoints(grade);
        string ptText = PaintQuizScoreMap.FormatScoreWithSign(pts) + "pt";

        GUIWidget stampWidget = stamp != null ? stamp.GetStampWidget(null, PlayerControl.LocalPlayer?.PlayerId ?? 0, GUIAlignment.Center, false, 0.35f) : GUI.API.RawText(GUIAlignment.Center, GUI.API.GetAttribute(AttributeAsset.OverlayTitle), "?");

        return GUI.API.VerticalHolder(GUIAlignment.Center,
            stampWidget,
            GUI.API.RawText(GUIAlignment.Center, GUI.API.GetAttribute(AttributeAsset.OverlayContent), ptText));
    };

    public (byte playerId, byte grade)[] GetGrades()
        => grades.Select(kv => (kv.Key, kv.Value)).ToArray();

    public void ShowResults((byte playerId, byte grade)[] results)
    {
        waitScoringObj?.SetActive(false);
        answerObj?.SetActive(true);

        // 操作ヒントを非表示に
        if (hintObj.AsBoolFast()) hintObj.SetActive(false);

        List<(Transform holder, Transform stamp)> stamps = [];
        UnityEngine.Vector2 zeroVec = Vector2.zero;

        foreach (var (playerId, grade) in results)
        {
            var cellIdx = Array.FindIndex(cells, c => c.PlayerId == playerId);
            if (cellIdx < 0) continue;

            ref var cell = ref cells[cellIdx];

            // 採点プレビューを非表示
            cell.GradePreviewHolder?.SetActive(false);

            float pts = PaintQuizScoreMap.GradeToPoints(grade);
            if (cell.ScoreText != null)
            {
                cell.ScoreText.gameObject.SetActive(true);
                cell.ScoreText.text = grade == 0 ? "" : PaintQuizScoreMap.FormatScoreWithSign(pts) + "pt";
                cell.ScoreText.color = pts > 0 ? new Color(0.2f, 0.9f, 0.2f) : pts < 0 ? new Color(0.9f, 0.2f, 0.2f) : new(0.5f, 0.5f, 0.5f);
            }

            if (grade > 0)
            {
                var stamp = PaintQuizStampConfig.GetStamp(grade);
                if (stamp != null)
                {
                    cell.StampHolder.SetActive(true);
                    var stampWidget = stamp.GetStampWidget(null, hostPlayer.PlayerId, GUIAlignment.Center, false, 1.25f);
                    var stampObj = stampWidget.Instantiate(new(10f, 10f), out _);
                    if (stampObj.AsBoolFast())
                    {
                        var holderTransform = cell.StampHolder.transform;
                        var stampTransform = stampObj!.transform;
                        stampTransform.SetParent(holderTransform);
                        stampTransform.localPosition = zeroVec;
                        stamps.Add((holderTransform, stampTransform));
                    }
                }
            }
        }

        if (stamps.Count > 0) CoAnimateStamp(stamps, drawW, drawH).StartOnScene();
    }

    // 中央でブロップ → 左上角へ滑らかに移動しながら縮小
    private IEnumerator CoAnimateStamp(List<(Transform holder, Transform stamp)> stamps, float drawW, float drawH)
    {
        foreach (var stamp in stamps) Effects.Bloop(0.1f, stamp.holder).StartOnScene();
        yield return Effects.Wait(1.3f);

        VVector3 startPos = new(0f,0f,-0.2f);
        VVector3 targetPos = new(-drawW * 0.5f + 0.22f * resultSize, drawH * 0.5f - 0.22f * resultSize, startPos.z);
        float duration = 0.65f;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathn.Clamp01(elapsed / duration);
            float smooth = t * t * (3f - 2f * t);
            UnityEngine.Vector3 localPos = VVector3.Lerp(startPos, targetPos, smooth);
            UnityEngine.Vector3 localScale = VVector3.One * Mathn.Lerp(1f, 0.55f * resultSize, smooth);

            foreach(var stamp in stamps)
            {
                stamp.holder.localPosition = localPos;
                stamp.holder.localScale = localScale;
            }
            yield return null;
        }

        UnityEngine.Vector3 finalLocalPos = targetPos;
        UnityEngine.Vector3 finalLocalScale = new(0.55f * resultSize, 0.55f * resultSize, 1f);
        foreach (var stamp in stamps)
        {
            stamp.holder.localPosition = finalLocalPos;
            stamp.holder.localScale = finalLocalScale;
        }
        
    }

    public IEnumerator CoRenderAllDrawings()
    {
        UnityEngine.Color white = VColor.White.ToUnityColor();
        bool[] done = new bool[allDrawings.Length];
        for (int i = 0; i < allDrawings.Length; i++)
        {
            int idx = i;
            DyingMessages.GeneratePaintQuizDrawing(allDrawings[i].trajectories, tex =>
            {
                cells[idx].DrawingMaterial.mainTexture = tex;
                cells[idx].DrawingMaterial.color = white;
                done[idx] = true;
            });
        }
        while (!done.All(d => d)) yield return null;
    }


    public void SetActive(bool active) => holder.SetActive(active);

    public void Destroy()
    {
        if (holder) GameObject.Destroy(holder);
    }

    private static string GetPlayerName(byte playerId)
    {
        var player = GamePlayer.AllPlayers.FirstOrDefault(p => p.PlayerId == playerId);
        return player?.PlayerName ?? "?";
    }
}
