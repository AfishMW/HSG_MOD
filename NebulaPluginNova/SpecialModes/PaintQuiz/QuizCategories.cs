using Nebula.Roles;
using Nebula.Roles.Assignment;
using Rewired.Utils.Classes.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Virial.Assignable;
using Virial.Media;
using Virial.Text;

namespace Nebula.SpecialModes.PaintQuiz;

internal enum QuizCategories
{
    BlurbToRole, // フレーバーから役職
    RoleToIcon, // 役職からアイコン
    TitleToRole, // 称号から役職
    ChallengeTitleToRole, // チャレンジ称号から役職
    RoleToBlurb, // 役職からフレーバー
    RoleToChallengeTitle, // 役職からチャレンジ称号
    PlayerToBlurb,
    PlayerToTitle,
}

internal interface QuizCategoryStrategy
{
    protected const string BrokenData = "BROKEN DATA";

    QuizCategories Category { get; }

    string CategoryId { get; }

    string BaseRuleText => (Language.Translate("paintQuiz.category.hint") + "<br>" + Language.Translate($"paintQuiz.category.{CategoryId}.hint")).Sized(18);
    string GetRandomedRuleText(int randomSeed, int length) => BaseRuleText.Replace("%RANDOM%", Language.Translate($"paintQuiz.category.{CategoryId}.hint.random." + (randomSeed % length)));
    string RuleText => BaseRuleText;

    bool HasAnswer => true;

    /// <summary>
    /// 自身の進行状況から問題を提案します。
    /// </summary>
    int[] SuggestMyCandidate(int numOfQuizzes);

    /// <summary>
    /// 自身の進行状況から称号の獲得状況を返します。
    /// </summary>
    /// <param name="numTitleId"></param>
    /// <returns></returns>
    bool HaveAchievedAlready(int numTitleId);

    /// <summary>
    /// クイズのテキストを取得します。
    /// </summary>
    /// <param name="quizSeed"></param>
    /// <param name="achieved"></param>
    /// <returns></returns>
    (string question, string? achieved) GetQuizText(int quizSeed, GamePlayer[] achieved);

    /// <summary>
    /// クイズの答えを取得します。
    /// </summary>
    /// <param name="quizSeed"></param>
    /// <returns></returns>
    string GetAnswerText(int quizSeed);

    /// <summary>
    /// クイズを作成します。
    /// </summary>
    /// <returns></returns>
    int? GenerateQuizSeed();

    void OnReceivePreSharing(int[] candidates);

    GUIWidgetSupplier? RelatedInformation(int quizSeed) => null;

    bool PoolIsEmpty();
    void ResetPoolToDefault();

    static internal QuizCategoryStrategy Create(QuizCategories category, int randomSeed)
    {
        return category switch
        {
            QuizCategories.BlurbToRole => new BlurbToRoleQuizCategory(),
            QuizCategories.RoleToIcon => new RoleToIconQuizCategory(),
            QuizCategories.TitleToRole => new TitleToRoleQuizCategory(),
            QuizCategories.ChallengeTitleToRole => new ChallengeTitleToRoleQuizCategory(),
            QuizCategories.RoleToBlurb => new RoleToBlurbQuizCategory(),
            QuizCategories.RoleToChallengeTitle => new RoleToChallengeTitleQuizCategory(),
            QuizCategories.PlayerToBlurb => new PlayerToBlurbQuizCategory(randomSeed),
            QuizCategories.PlayerToTitle => new PlayerToTitleQuizCategory(randomSeed),
            _ => throw new NotImplementedException($"QuizCategoryStrategy for {category} is not implemented."),
        };
    }

    static protected string? GetAchievedPlayersName(GamePlayer[] achieved)
    {
        string? achievedLine = null;
        if (achieved.Length <= 2)
            achievedLine = Language.Translate("paintQuiz.hint.title").Replace("%PLAYERS%", string.Join(Language.Translate("paintQuiz.hint.comma"), achieved.Select(p => p.ColoredName)));
        else
            achievedLine = Language.Translate("paintQuiz.hint.title.moreThan2").Replace("%PLAYERS%", string.Join(Language.Translate("paintQuiz.hint.comma"), achieved.Take(2).Select(p => p.ColoredName))).Replace("%OTHERS%", (achieved.Length - 2).ToString());
        return achievedLine;
    }

    static protected string? GetPlayedPlayersName(GamePlayer[] achieved)
    {
        string? achievedLine = null;
        if (achieved.Length <= 2)
            achievedLine = Language.Translate("paintQuiz.hint.role").Replace("%PLAYERS%", string.Join(Language.Translate("paintQuiz.hint.comma"), achieved.Select(p => p.ColoredName)));
        else
            achievedLine = Language.Translate("paintQuiz.hint.role.moreThan2").Replace("%PLAYERS%", string.Join(Language.Translate("paintQuiz.hint.comma"), achieved.Take(2).Select(p => p.ColoredName))).Replace("%OTHERS%", (achieved.Length - 2).ToString());
        return achievedLine;
    }

    static protected int AssignableToInt(DefinedAssignable assignable)
    {

        int id = assignable switch
        {
            DefinedRole => 0b01,
            DefinedModifier => 0b10,
            DefinedGhostRole => 0b11,
            _ => 0b00
        };
        return id | (assignable.Id << 2);
    }

    static protected DefinedAssignable? IntToAssignable(int id)
    {
        int mask = id & 0b11;
        id >>= 2;
        if(mask == 0b01)
        {
            return Roles.Roles.GetRole(id);
        }
        if (mask == 0b10)
        {
            return Roles.Roles.GetModifier(id);
        }
        if (mask == 0b11)
        {
            return Roles.Roles.GetGhostRole(id);
        }
        return null;
    }

}

internal class BlurbToRoleQuizCategory : QuizCategoryStrategy
{
    List<int> idCandidates;
    
    public BlurbToRoleQuizCategory()
    {
        if (GeneralConfigurations.RestrictToSpawnablesOption)
        {
            var flags = AssignmentPreview.CalcPreview(Mathn.Max(GamePlayer.AllPlayers.Count(), 10), out var gameParam);
            AssignmentPreview.AssignmentFlag allFlag = 0;
            foreach (var f in flags) allFlag |= f;
            var summary = AssignmentPreview.CalcSummary(allFlag, gameParam);

            HashSet<int> assignablesSet = [];
            void AddAll(IEnumerable<DefinedAssignable> assignables)
            {
                foreach (var a in assignables)
                {
                    if (a.ShowOnHelpScreen) assignablesSet.Add(QuizCategoryStrategy.AssignableToInt(a));
                }
            }

            AddAll(summary.Roles.Select(r => r.Role));
            AddAll(summary.Modifiers.Select(r => r.Assignable));
            AddAll(summary.GhostRoles.Select(r => r.Assignable));
            AddAll(summary.Additionals.Select(r => r.Role));
            AddAll(summary.Specials.Select(r => r.Role));

            idCandidates = assignablesSet.ToList();
        }
        else
        {
            idCandidates = Roles.Roles.AllAssignables().Where(r => r.ShowOnHelpScreen).Select(QuizCategoryStrategy.AssignableToInt).ToList();
        }
    }
    

    virtual public QuizCategories Category => QuizCategories.BlurbToRole;

    string QuizCategoryStrategy.CategoryId => "blurbToRole";

    int[] QuizCategoryStrategy.SuggestMyCandidate(int numOfQuizzes) => [];

    virtual protected string AssignableToQuestion(DefinedAssignable assignable) => Language.Translate("paintQuiz.category.blurbToRole.quiz").Replace("%BLURB%", assignable.GeneralBlurb);
    virtual protected string AssignableToAnswer(DefinedAssignable assignable) => assignable.DisplayColoredName;

    bool QuizCategoryStrategy.HaveAchievedAlready(int numTitleId) => PlayerModInfo.IsPlayedRecently(QuizCategoryStrategy.IntToAssignable(numTitleId));
    (string question, string? achieved) QuizCategoryStrategy.GetQuizText(int quizSeed, GamePlayer[] achieved)
    {
        var assignable = QuizCategoryStrategy.IntToAssignable(quizSeed);
        if (assignable == null) return (QuizCategoryStrategy.BrokenData, null);

        var baseText = AssignableToQuestion(assignable);
        if (achieved.Length == 0) return (baseText, null);

        return (baseText, QuizCategoryStrategy.GetPlayedPlayersName(achieved));
    }

    string QuizCategoryStrategy.GetAnswerText(int quizSeed)
    {
        var assignable = QuizCategoryStrategy.IntToAssignable(quizSeed);
        if (assignable == null) return QuizCategoryStrategy.BrokenData;
        return AssignableToAnswer(assignable);
    }

    void QuizCategoryStrategy.OnReceivePreSharing(int[] candidates)
    {
    }

    int? QuizCategoryStrategy.GenerateQuizSeed()
    {
        if (this.idCandidates.Count == 0) return null;
        
        var index = System.Random.Shared.Next(this.idCandidates.Count);
        var selected = this.idCandidates[index];
        this.idCandidates.RemoveAt(index);
        return selected;
    }

    GUIWidgetSupplier? QuizCategoryStrategy.RelatedInformation(int quizSeed) {
        var assignable = QuizCategoryStrategy.IntToAssignable(quizSeed);
        var component = assignable?.ConfigurationHolder?.Detail;
        if (component == null) return null;
        var holder = GUI.API.VerticalHolder(GUIAlignment.Left, 
            GUI.API.RawText(GUIAlignment.Left, AttributeAsset.OverlayTitle, assignable!.DisplayColoredName),
            GUI.API.Text(GUIAlignment.Left, AttributeAsset.OverlayContent, component)
            );
        holder.BackImage = assignable.ConfigurationHolder?.Illustration;
        return holder;
    }

    bool QuizCategoryStrategy.PoolIsEmpty() => idCandidates.Count == 0;
    
    void QuizCategoryStrategy.ResetPoolToDefault()
    {
        idCandidates = Roles.Roles.AllAssignables().Where(r => r.ShowOnHelpScreen).Select(QuizCategoryStrategy.AssignableToInt).ToList();
    }
}

internal class RoleToIconQuizCategory : BlurbToRoleQuizCategory, QuizCategoryStrategy
{
    override public QuizCategories Category => QuizCategories.RoleToIcon;
    string QuizCategoryStrategy.CategoryId => "roleToIcon";
    override protected string AssignableToQuestion(DefinedAssignable assignable) => Language.Translate("paintQuiz.category.roleToIcon.quiz").Replace("%ROLE%", assignable.DisplayColoredName);
    override protected string AssignableToAnswer(DefinedAssignable assignable) => assignable.GetRoleIconTag(false, 200);
}

internal class RoleToBlurbQuizCategory : BlurbToRoleQuizCategory, QuizCategoryStrategy
{
    override public QuizCategories Category => QuizCategories.RoleToBlurb;
    string QuizCategoryStrategy.CategoryId => "roleToBlurb";
    override protected string AssignableToQuestion(DefinedAssignable assignable) => Language.Translate("paintQuiz.category.roleToBlurb.quiz").Replace("%ROLE%", assignable.DisplayColoredName);
    override protected string AssignableToAnswer(DefinedAssignable assignable) => assignable.GeneralBlurb;
}

internal class TitleToRoleQuizCategory : QuizCategoryStrategy
{
    HashSet<int> idCandidates = [];
    List<INebulaAchievement>? achCandidates = null; //ホスト以外は作成する必要がない

    virtual public QuizCategories Category => QuizCategories.TitleToRole;
    string QuizCategoryStrategy.CategoryId => "titleToRole";

    virtual protected IEnumerable<INebulaAchievement> TargetAchievements => NebulaAchievementManager.AllAchievements;
    virtual protected string TitleToQuestion(INebulaAchievement achievement) => Language.Translate("paintQuiz.category.titleToRole.quiz").Replace("%TITLE%", Language.Translate(achievement.TranslationKey));
    virtual protected string TitleToAnswer(INebulaAchievement achievement) => string.Join(", ", achievement.RelatedRole.Select(r => r.DisplayColoredName));
    int[] QuizCategoryStrategy.SuggestMyCandidate(int numOfQuizzes)
    {
        int numOfCands = Mathn.Clamp(numOfQuizzes * 10, 50, 100);

        List<INebulaAchievement> achievements = [];
        foreach (var ach in TargetAchievements)
        {
            if (ach.RelatedRole.IsEmpty()) continue;
            if (ach.IsCleared) achievements.Add(ach);
        }

        List<int> nums = [];
        var random = System.Random.Shared;
        for (int i = 0; i < numOfCands; i++)
        {
            if (achievements.Count == 0) break;
            int index = random.Next(achievements.Count);
            nums.Add(achievements[index].NumId);
            achievements.RemoveAt(index);
        }
        return nums.ToArray();
    }

    bool QuizCategoryStrategy.HaveAchievedAlready(int numTitleId) => NebulaAchievementManager.GetFromNumId(numTitleId)?.IsCleared ?? false;
    (string question, string? achieved) QuizCategoryStrategy.GetQuizText(int quizSeed, GamePlayer[] achieved)
    {
        var achievement = NebulaAchievementManager.GetFromNumId(quizSeed);
        if (achievement == null) return (QuizCategoryStrategy.BrokenData, null);

        var baseText = TitleToQuestion(achievement);
        if (achieved.Length == 0) return (baseText, null);

        return (baseText, QuizCategoryStrategy.GetAchievedPlayersName(achieved));
    }

    string QuizCategoryStrategy.GetAnswerText(int quizSeed)
    {
        var achievement = NebulaAchievementManager.GetFromNumId(quizSeed);
        if (achievement == null) return QuizCategoryStrategy.BrokenData;
        return TitleToAnswer(achievement);
    }

    void QuizCategoryStrategy.OnReceivePreSharing(int[] candidates)
    {
        foreach (var id in candidates) this.idCandidates.Add(id);
    }

    int? QuizCategoryStrategy.GenerateQuizSeed()
    {
        if (this.achCandidates == null)
        {
            this.achCandidates = new List<INebulaAchievement>(this.idCandidates.Count);
            foreach (var id in this.idCandidates)
            {
                var ach = NebulaAchievementManager.GetFromNumId(id);
                if (ach != null && ach.RelatedRole.Any()) this.achCandidates.Add(ach);
            }
        }
        if (this.achCandidates.Count == 0) return null;

        var index = System.Random.Shared.Next(this.achCandidates.Count);
        var selected = this.achCandidates[index];
        this.achCandidates.RemoveAt(index);
        return selected.NumId;
    }

    GUIWidgetSupplier? QuizCategoryStrategy.RelatedInformation(int quizSeed)
    {
        var ach = NebulaAchievementManager.GetFromNumId(quizSeed);
        return ach?.GetOverlayWidget(false, true, false, true, true, true, false);
    }

    bool QuizCategoryStrategy.PoolIsEmpty() => (achCandidates?.Count ?? 1) == 0;

    void QuizCategoryStrategy.ResetPoolToDefault()
    {
        achCandidates = TargetAchievements.ToList();
    }
}

internal class ChallengeTitleToRoleQuizCategory : TitleToRoleQuizCategory, QuizCategoryStrategy
{
    override public QuizCategories Category => QuizCategories.ChallengeTitleToRole;
    string QuizCategoryStrategy.CategoryId => "challengeToRole";
    override protected IEnumerable<INebulaAchievement> TargetAchievements => NebulaAchievementManager.AllAchievements.Where(ach => ach.AchievementType().Contains(AchievementType.Challenge));
}

internal class RoleToChallengeTitleQuizCategory : ChallengeTitleToRoleQuizCategory, QuizCategoryStrategy
{
    override public QuizCategories Category => QuizCategories.RoleToChallengeTitle;
    string QuizCategoryStrategy.CategoryId => "roleToChallenge";
    override protected IEnumerable<INebulaAchievement> TargetAchievements => NebulaAchievementManager.AllAchievements.Where(ach => ach.AchievementType().Contains(AchievementType.Challenge));
    override protected string TitleToQuestion(INebulaAchievement achievement) => Language.Translate("paintQuiz.category.roleToChallenge.quiz").Replace("%ROLE%", achievement.RelatedRole.FirstOrDefault()!.DisplayColoredName);
    override protected string TitleToAnswer(INebulaAchievement achievement) => Language.Translate(achievement.TranslationKey);
}

internal class PlayerToBlurbQuizCategory : QuizCategoryStrategy
{
    List<GamePlayer> players;
    int randomSeed;

    public PlayerToBlurbQuizCategory(int randomSeed)
    {
        players = GamePlayer.AllPlayers.ToList();
        this.randomSeed = randomSeed % 3;
    }

    bool QuizCategoryStrategy.HasAnswer => false;
    string QuizCategoryStrategy.RuleText => (this as QuizCategoryStrategy).GetRandomedRuleText(randomSeed, 3);
    QuizCategories QuizCategoryStrategy.Category => QuizCategories.PlayerToBlurb;

    string QuizCategoryStrategy.CategoryId => "playerToBlurb";

    int[] QuizCategoryStrategy.SuggestMyCandidate(int numOfQuizzes) => [];

    bool QuizCategoryStrategy.HaveAchievedAlready(int numTitleId) => false;
    (string question, string? achieved) QuizCategoryStrategy.GetQuizText(int quizSeed, GamePlayer[] achieved)
    {
        var player = GamePlayer.GetPlayer((byte)quizSeed);
        if (player == null) return (QuizCategoryStrategy.BrokenData, null);

        return (Language.Translate("paintQuiz.category.playerToBlurb.quiz").Replace("%PLAYER%", player.ColoredName), null);
    }

    string QuizCategoryStrategy.GetAnswerText(int quizSeed) => null!;

    void QuizCategoryStrategy.OnReceivePreSharing(int[] candidates)
    {
    }

    int? QuizCategoryStrategy.GenerateQuizSeed()
    {
        if (this.players.Count == 0) return null;

        var index = System.Random.Shared.Next(this.players.Count);
        var selected = this.players[index];
        this.players.RemoveAt(index);
        return selected.PlayerId;
    }

    GUIWidgetSupplier? QuizCategoryStrategy.RelatedInformation(int quizSeed) => null;

    bool QuizCategoryStrategy.PoolIsEmpty() => players.Count == 0;

    void QuizCategoryStrategy.ResetPoolToDefault()
    {
        players = GamePlayer.AllPlayers.ToList();
    }
}

internal class PlayerToTitleQuizCategory : PlayerToBlurbQuizCategory, QuizCategoryStrategy
{
    public PlayerToTitleQuizCategory(int randomSeed) : base(randomSeed)
    {
    }

    QuizCategories QuizCategoryStrategy.Category => QuizCategories.PlayerToTitle;

    string QuizCategoryStrategy.CategoryId => "playerToTitle";

    (string question, string? achieved) QuizCategoryStrategy.GetQuizText(int quizSeed, GamePlayer[] achieved)
    {
        var player = GamePlayer.GetPlayer((byte)quizSeed);
        if (player == null) return (QuizCategoryStrategy.BrokenData, null);

        return (Language.Translate("paintQuiz.category.playerToTitle.quiz").Replace("%PLAYER%", player.ColoredName), null);
    }
}