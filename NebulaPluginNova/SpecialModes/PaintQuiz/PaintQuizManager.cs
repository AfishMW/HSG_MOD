using Nebula.Modules.Cosmetics;
using Nebula.Roles.Abilities;
using System.Text;
using Virial;
using Virial.Assignable;
using Virial.DI;
using Virial.Game;

namespace Nebula.SpecialModes.PaintQuiz;

[NebulaRPCHolder]
internal class PaintQuizSenario : AbstractModuleContainer, IModule, IGameModePaintQuiz, IPaintQuizScoreBoardInteraction
{
    internal PaintQuizSenario()
    {
        ModSingleton<PaintQuizSenario>.Instance = this;
        CoPlaySenario().StartOnScene();
    }

    bool IGameModeModule.AllowSpecialGameEnd => false;
    bool IGameModeModule.ShowMap => false;
    bool IGameModeModule.ShowStatistics => false;
    bool IGameModeModule.CanUseStampOnly => true;
    bool IGameModeModule.CanOpenHelpScreen => false;
    bool IGameModeModule.ShowButtons => false;
    string? IGameModeModule.GetAlternativeWinOrLoseText()
    {
        if (GamePlayer.AllOrderedPlayers.Count <= 1)
        {
            return String.Format("{0:N0}", ScoreOrderedPlayers[0].score) + "pt";
        }
        int rank = 0;
        int players = 0;
        float score = -1;
        foreach (var p in ScoreOrderedPlayers)
        {
            if (score != p.score)
            {
                rank = players + 1;
                score = p.score;
            }
            players++;
            if (p.player.AmOwner) break;
        }
        return Language.Translate("aeroGuesser.rank." + rank) + ": " + String.Format("{0:0.#}", score) + "pt";
    }

    static private readonly Virial.Color[] RankColor = [new(255, 196, 0), new(170, 180, 186), new(193, 119, 31)];

    (GamePlayer player, float score)[] ScoreOrderedPlayers => GamePlayer.AllPlayers.Select(p => (p, scoreMap.GetScore(p.PlayerId))).OrderBy(p => -p.Item2).ToArray();
    string? IGameModeModule.GetAlternativePlayerStatusText()
    {
        var orderedPlayers = ScoreOrderedPlayers;
        if (orderedPlayers.Length == 0) return "";
        StringBuilder sb = new();

        int rank = 0;
        int players = 0;
        var lastScore = orderedPlayers[0].score;
        foreach (var entry in orderedPlayers)
        {
            var score = entry.score;
            if (lastScore != score) rank = players;
            lastScore = score;
            players++;

            sb.Append(Language.Translate("aeroGuesser.rank." + (rank + 1)).Color(rank < RankColor.Length ? RankColor[rank] : VColor.White));
            sb.Append("<indent=3.4em>");
            sb.Append(String.Format("{0:0.#}", score) + "pt");
            sb.Append("</indent>");
            sb.Append("<indent=10.4em>");
            sb.Append(entry.player.Name);
            sb.Append("</indent>");
            sb.AppendLine();
        }
        return sb.ToString();
    }

    float IPaintQuizScoreBoardInteraction.ScoreBoardQ => scoreBoardQ.Value;

    // ゲーム設定
    QuizCategoryStrategy? category = null;
    int numOfQuizzes = 0;

    // ゲームオブジェクト
    private GameObject baseObject = null!;
    private SpriteRenderer backgroundRenderer = null!;
    private GameObject clickGuard = null!;
    private PaintQuizScoreBoard? scoreBoard = null;

    // クイズ間でリセットされる受信データ
    private (int seed, int num, int max, byte[] achieverIds)? unprocessedQuiz = null;
    private Dictionary<byte, (byte r, byte g, byte b, byte width, byte beginX, byte beginY, byte[] trajectory)[]> collectedDrawings = [];
    private bool drawingClosed = false;
    private (byte[] playerIds, byte[] grades)? unprocessedScores = null;
    private bool proceedingReceived = false;

    // 達成状況収集（ホスト用）
    private Dictionary<byte, bool> achievementReports = [];

    // スコア
    private PaintQuizScoreMap scoreMap = new();
    private IFunctionalValue<float> scoreBoardQ = Arithmetic.FloatZero;

    static public IEnumerator CoIntro(bool amHost)
    {
        if (amHost) RpcIntro.Invoke(((QuizCategories)GeneralConfigurations.PaintQuizCategoryOption.GetValue(), GeneralConfigurations.NumOfPaintQuizOption, System.Random.Shared.Next(10000)));

        SpecialModeFunctions.IntroSetUp();
        yield break;
    }

    private void SetUp(QuizCategories quizCategory, int quizCount, int randomSeed)
    {
        category = QuizCategoryStrategy.Create(quizCategory, randomSeed);
        numOfQuizzes = quizCount;
    }

    private void Initialize()
    {
        var hud = HudManager.Instance;
        hud.roomTracker.gameObject.SetActive(false);

        baseObject = UnityHelper.CreateObject("PaintQuiz", hud.transform, Vector3.zero);

        backgroundRenderer = UnityHelper.CreateObject<SpriteRenderer>("Background", baseObject.transform, new(0f, 0f, 5f));
        backgroundRenderer.sprite = VanillaAsset.TitleBackgroundSprite;
        backgroundRenderer.material = new(NebulaAsset.HSVNAShader);
        backgroundRenderer.sharedMaterial.SetFloat("_Sat", 0.55f);
        backgroundRenderer.sharedMaterial.SetFloat("_Hue", 280f);
        backgroundRenderer.sharedMaterial.SetFloat("_Val", 0.8f);

        clickGuard = UnityHelper.CreateObject("ClickGuard", baseObject.transform, new(0f, 0f, -20f));
        clickGuard.SetUpButton(false);
        var cg = clickGuard.AddComponent<BoxCollider2D>();
        cg.size = new(20f, 20f);
        cg.isTrigger = false;
        clickGuard.SetActive(true);

        scoreBoard = new PaintQuizScoreBoard(baseObject.transform, this);
    }

    private IEnumerator CoPlaySenario()
    {
        yield return null;

        StampHelpers.SetStampShowerToUnderHud();
        Initialize();

        var synchronizer = NebulaAPI.CurrentGame?.GetModule<Synchronizer>();
        bool amHost = AmongUsClient.Instance.AmHost;

        //進捗
        new StaticAchievementToken("stats.paintQuiz.gamePlay");
        if(amHost) new StaticAchievementToken("stats.paintQuiz.gamePlay.host");

        IEnumerator CoCheckAndResume()
        {
            while (true)
            {
                if (AmongUsLLImpl.AmongUsClientInstance.AmHost) NebulaGameManager.Instance?.RpcInvokeForciblyWin(NebulaGameEnd.NoGame, 0);
                yield return Effects.Wait(3f);
            }
        }
        if (!amHost) CoCheckAndResume().StartOnScene();

        yield return synchronizer?.CoSync(SynchronizeTag.PreStartGame, true);

        HudManager.Instance.StartCoroutine(HudManager.Instance.CoFadeFullScreen(Color.black, Color.clear, 0.8f));

        // 事前共有 (フェードアウト待機に先んじて送っておく)
        using (RPCRouter.CreateSection("PaintQuiz.PreSharing", true))
        {
            if (amHost) RpcShareStamps.Invoke(PaintQuizStampConfig.GetAllStampIds());
            RpcPreSharing.Invoke(category?.SuggestMyCandidate(numOfQuizzes) ?? []);
            synchronizer?.SendSync(SynchronizeTag.PaintQuizPreSharing);
        }

        yield return Effects.Wait(0.6f);
        yield return synchronizer?.CoSync(SynchronizeTag.PaintQuizPreSharing, true);

        // ルール説明
        var fadeQ = Arithmetic.Sequential((() => Arithmetic.Decel(0f, 1f, 0.3f), 0.3f), (() => Arithmetic.FloatOne, 5f), (() => Arithmetic.Decel(1f, 0, 0.55f), 0.6f));
        NebulaAPI.CurrentGame?.GetModule<TitleShower>()?.SetText(category!.RuleText, VColor.White, new(shower =>
        {
            shower.SetTextAlpha(fadeQ.Value);
        }));
        yield return Effects.Wait(6f);

        // クイズループ
        for (int i = 0; i < numOfQuizzes; i++)
        {
            if (i != 0)
            {
                yield return Effects.All(CoShareQuiz(synchronizer, i, amHost).WrapToIl2Cpp(), CoShowRanking().WrapToIl2Cpp());

                scoreBoardQ = Arithmetic.Decel(1f, 0f, 0.3f);
                yield return Effects.Wait(0.4f);
            }
            else
            {
                yield return CoShareQuiz(synchronizer, i, amHost);
            }

            while (!unprocessedQuiz.HasValue) yield return null;

            var (quizSeed, num, maxNum, quizAchievers) = unprocessedQuiz!.Value;
            unprocessedQuiz = null;

            var achievedList = quizAchievers.Select(GamePlayer.GetPlayer).Where(p => p != null);

            yield return CoQuizIntro(num, maxNum);

            yield return CoPlayOneQuiz(quizSeed, amHost, achievedList.ToArray()!);
        }

        if (amHost)
        {
            var orderedPlayers = GamePlayer.AllPlayers
                .Select(p => (p, scoreMap.GetScore(p.PlayerId)))
                .OrderByDescending(e => e.Item2)
                .ToArray();
            if (orderedPlayers.Length > 0)
            {
                float maxScore = orderedPlayers[0].Item2;
                int winnersMask = 0;
                foreach (var (p, s) in orderedPlayers)
                {
                    if (s < maxScore) break;
                    winnersMask |= 1 << p.PlayerId;
                }
                NebulaGameManager.Instance?.RpcInvokeForciblyWin(NebulaGameEnd.GameEnd, winnersMask);
            }
            else
            {
                NebulaGameManager.Instance?.RpcInvokeForciblyWin(NebulaGameEnd.NoGame, 0);
            }
        }
    }

    // クイズ番号表示
    private IEnumerator CoQuizIntro(int num, int maxQuiz)
    {
        var fastQ = Arithmetic.Decel(1f, 0f, 0.3f);
        var slowQ = Arithmetic.Decel(1f, 0f, 1.15f);
        var fadeOutQ = Arithmetic.Sequential((() => Arithmetic.FloatOne, 1.4f), (() => Arithmetic.Decel(1f, 0, 0.4f), 1f));
        NebulaAPI.CurrentGame?.GetModule<TitleShower>()?.SetText($"{num}<size=80%>/{maxQuiz}</size>", Color.white, new(shower =>
        {
            shower.Transform.localEulerAngles = new(0f, 0f, fastQ.Value * 220f);
            var scale = slowQ.Value * 0.4f + 0.6f;
            shower.Transform.localScale = new(scale, scale, 1f);
            shower.SetTextAlpha(fadeOutQ.Value);
        }));
        yield return Effects.Wait(1.8f);
    }

    IEnumerator CoShareQuiz(Synchronizer synchronizer, int numOfQuiz, bool amHost)
    {
        if (amHost)
        {
            if (category!.PoolIsEmpty())
            {
                category.ResetPoolToDefault();
                DebugScreen.Push(Language.Translate("paintQuiz.ui.resetToDefault"), 5f);
            }

            int? seed = category!.GenerateQuizSeed();
            if (!seed.HasValue) yield break;

            achievementReports.Clear();
            RpcCheckAchievement.Invoke(seed.Value);
            yield return synchronizer?.CoSyncAndReset(SynchronizeTag.PaintQuizCheckAchievement, true);

            byte[] achieverIds = achievementReports
                .Where(kv => kv.Value).Select(kv => kv.Key).Shuffle().ToArray();
            RpcSendQuiz.Invoke((seed.Value, numOfQuiz + 1, numOfQuizzes, achieverIds));
        }
        else
        {
            yield return synchronizer?.CoSyncAndReset(SynchronizeTag.PaintQuizCheckAchievement, true);
        }
    }

    IEnumerator CoShowRanking()
    {
        scoreBoard!.UpdateRanking(scoreMap);
        scoreBoardQ = Arithmetic.Decel(0f, 1f, 0.5f);
        scoreBoard.ShowContent(0f, true, false);  // 今問の獲得点数
        yield return Effects.Wait(1.5f);
        scoreBoard.ShowContent(1f, false, true);  // 累計へ移行
        yield return Effects.Wait(2.5f);
    }

    private IEnumerator CoPlayOneQuiz(int quizSeed, bool amHost, GamePlayer[] achievedPlayers)
    {
        collectedDrawings.Clear();
        drawingClosed = false;
        unprocessedScores = null;
        proceedingReceived = false;

        var questionTexts = category!.GetQuizText(quizSeed, achievedPlayers);
        var answerText = category!.GetAnswerText(quizSeed);

        NebulaAsset.PlaySE(NebulaAudioClip.AeroGuesserConfirm);

        var drawPhase = new PaintQuizDrawingPhase(baseObject.transform, questionTexts.question, questionTexts.achieved, () => collectedDrawings.Count);

        while (!drawPhase.IsSubmitted) yield return null;

        var myDrawing = drawPhase.GetDrawing();
        RpcSendDrawing.Invoke((GamePlayer.LocalPlayer!.PlayerId, myDrawing));

        if (amHost)
        {
            yield return Effects.Wait(2f);
            while (!AllDrawingsReceived()) yield return null;
            RpcCloseDrawing.Invoke();
        }

        while (!drawingClosed) yield return null;
        drawingClosed = false;

        drawPhase.SetActive(false);

        // Review
        var drawingsArray = collectedDrawings
            .Select(kv => (id: kv.Key, traj: kv.Value))
            .OrderBy(e => e.id)
            .ToArray();

        var reviewPhase = new PaintQuizReviewPhase(category.RelatedInformation(quizSeed), baseObject.transform, drawingsArray, questionTexts.question, answerText, amHost, category.HasAnswer);

        yield return reviewPhase.CoRenderAllDrawings();

        NebulaAsset.PlaySE(NebulaAudioClip.AeroGuesserQuizEnd);

        reviewPhase.SetActive(true);

        if (amHost)
        {
            while (!reviewPhase.IsConfirmed) yield return null;
            var grades = reviewPhase.GetGrades();
            RpcConfirmScores.Invoke((
                grades.Select(g => g.playerId).ToArray(),
                grades.Select(g => g.grade).ToArray()
            ));
        }

        while (!unprocessedScores.HasValue) yield return null;
        var (playerIds, gradeBytes) = unprocessedScores.Value;
        unprocessedScores = null;

        // Update Score
        NebulaAsset.PlaySE(NebulaAudioClip.QuizScore);
        var confirmedGrades = playerIds.Zip(gradeBytes, (id, g) => (id, g)).ToArray();
        foreach (var (pid, grade) in confirmedGrades)
        {
            float score = PaintQuizScoreMap.GradeToPoints(grade);
            scoreMap.AddScore(pid, score);
            if (GamePlayer.LocalPlayer.PlayerId == pid)
            {
                new AchievementToken<int>("stats.paintQuiz.totalScore.double", (int)(score * 2f), (num, _) => num);
                new StaticAchievementToken("stats.paintQuiz.score.get." + grade);
            }
            if (amHost)
            {
                new StaticAchievementToken("stats.paintQuiz.score.give." + grade);
            }
        }

        reviewPhase.ShowResults(confirmedGrades);
        yield return Effects.Wait(1.5f);

        // Confirm
        GameObject? nextBtn = null;
        if (amHost)
        {
            var nextWidget = GUI.API.RawButton(Virial.Media.GUIAlignment.Center,
                GUI.API.GetAttribute(Virial.Text.AttributeAsset.StandardMediumMasked), Language.Translate("paintQuiz.ui.proceed"),
                _ =>
                {
                    RpcProceeding.Invoke();
                    if (nextBtn != null) nextBtn.SetActive(false);
                });
            nextBtn = nextWidget.Instantiate(new(10f, 10f), out _);
            if (nextBtn != null)
            {
                nextBtn.transform.SetParent(baseObject.transform);
                nextBtn.transform.localPosition = new(0f, -2.35f, -40f);
            }
        }

        while (!proceedingReceived) yield return null;
        proceedingReceived = false;

        if (nextBtn.AsBoolFast()) nextBtn!.SetActive(false);
        reviewPhase.Destroy();
        drawPhase.Destroy();
    }

    private bool AllDrawingsReceived()
        => GamePlayer.AllPlayers.All(p => collectedDrawings.ContainsKey(p.PlayerId) || p.IsDisconnected);

    // ─────────────────── RPC ───────────────────

    static private readonly RemoteProcess<(QuizCategories category, int numOfQuizzes, int randomSeed)> RpcIntro = new("PaintQuiz.Intro", (message, _) =>
    {
        SpecialModeFunctions.InRpcSetUp();
        ModSingleton<PaintQuizSenario>.Instance!.SetUp(message.category, message.numOfQuizzes, message.randomSeed);
    });

    static private readonly RemoteProcess<string[]> RpcShareStamps = new("PaintQuiz.ShareStamps", (stampIds, _) =>
    {
        PaintQuizStampConfig.SetSessionStampIds(stampIds);
    });

    static private readonly RemoteProcess<int[]> RpcPreSharing = new("PaintQuiz.PreSharing", (message, _) =>
    {
        ModSingleton<PaintQuizSenario>.Instance?.category?.OnReceivePreSharing(message);
    });

    // ホストが達成状況を確認するためシードを送信
    // ハンドラ内でRPCSectionを使い、RpcReportAchievementとSendSyncを同時に順序保証して送信する
    static private readonly RemoteProcess<int> RpcCheckAchievement = new("PaintQuiz.CheckAchievement", (seed, _) =>
    {
        if (ModSingleton<PaintQuizSenario>.Instance is { } inst)
        {
            bool achieved = inst.category?.HaveAchievedAlready(seed) ?? false;
            var synchronizer = NebulaAPI.CurrentGame?.GetModule<Synchronizer>();
            using (RPCRouter.CreateSection("PaintQuiz.ReportAchievement"))
            {
                RpcReportAchievement.Invoke((GamePlayer.LocalPlayer!.PlayerId, achieved));
                synchronizer?.SendSync(SynchronizeTag.PaintQuizCheckAchievement);
            }
        }
    });

    // 各プレイヤーが達成状況を全員へ報告
    static private readonly RemoteProcess<(byte playerId, bool achieved)> RpcReportAchievement = new("PaintQuiz.ReportAchievement", (msg, _) =>
    {
        if (ModSingleton<PaintQuizSenario>.Instance is { } inst)
            inst.achievementReports[msg.playerId] = msg.achieved;
    });

    // クイズシード・番号・達成者リストを全員へ配布
    static private readonly RemoteProcess<(int seed, int num, int max, byte[] achieverIds)> RpcSendQuiz = new("PaintQuiz.SendQuiz", (message, _) =>
    {
        if (ModSingleton<PaintQuizSenario>.Instance is { } inst)
            inst.unprocessedQuiz = message;
    });

    static private readonly LongRemoteProcess<(byte playerId, (byte r, byte g, byte b, byte width, byte beginX, byte beginY, byte[] trajectory)[] trajectories)> RpcSendDrawing =
        new("PaintQuiz.SendDrawing", (message, _) =>
        {
            if (ModSingleton<PaintQuizSenario>.Instance is { } inst)
                inst.collectedDrawings[message.playerId] = message.trajectories;
        });

    static private readonly RemoteProcess RpcCloseDrawing = new("PaintQuiz.CloseDrawing", _ =>
    {
        if (ModSingleton<PaintQuizSenario>.Instance is { } inst)
            inst.drawingClosed = true;
    });

    static private readonly RemoteProcess<(byte[] playerIds, byte[] grades)> RpcConfirmScores = new("PaintQuiz.ConfirmScores", (message, _) =>
    {
        if (ModSingleton<PaintQuizSenario>.Instance is { } inst)
            inst.unprocessedScores = message;
    });

    static private readonly RemoteProcess RpcProceeding = new("PaintQuiz.Proceeding", _ =>
    {
        if (ModSingleton<PaintQuizSenario>.Instance is { } inst)
            inst.proceedingReceived = true;
    });
}
