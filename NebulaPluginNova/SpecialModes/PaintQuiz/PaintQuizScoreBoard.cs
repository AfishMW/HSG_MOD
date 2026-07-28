using Nebula.Behavior;
using Nebula.Modules.GUIWidget;
using TMPro;
using UnityEngine.Rendering;
using Virial.Text;

namespace Nebula.SpecialModes.PaintQuiz;

internal interface IPaintQuizScoreBoardInteraction
{
    float ScoreBoardQ { get; }
}

internal class PaintQuizScoreBoard
{
    private readonly IPaintQuizScoreBoardInteraction interaction;
    private IFunctionalValue<float> contentQ = Arithmetic.FloatZero;
    private bool requireUpdate = false;
    private GameObject holder = null!;

    private static TextAttribute NameAttr = GUI.API.GenerateAttribute(
        (AttributeParams)(AttributeTemplateFlag.FontBarlow | AttributeTemplateFlag.MaterialBared | AttributeTemplateFlag.AlignmentLeft),
        Virial.Color.White, new FontSize(1.6f, false), new(2.7f, 1f));

    private static TextAttribute ScoreAttr = GUI.API.GenerateAttribute(
        (AttributeParams)(AttributeTemplateFlag.FontBarlow | AttributeTemplateFlag.MaterialBared | AttributeTemplateFlag.AlignmentRight),
        Virial.Color.White, new FontSize(1.6f, false), new(1.2f, 1f));

    private class RankingEntry
    {
        public int SingleRank;
        public float SingleScore;
        public int SumRank;
        public float SumScore;
    }

    private (GamePlayer player, GameObject holder, TextMeshPro nameText, TextMeshPro scoreText)[] texts = [];
    private Dictionary<byte, RankingEntry>? rankingCache = null;

    internal PaintQuizScoreBoard(Transform parent, IPaintQuizScoreBoardInteraction interaction)
    {
        this.interaction = interaction;
        SetUp(parent);
    }

    private void SetUp(Transform parent)
    {
        holder = UnityHelper.CreateObject("PaintQuizRanking", parent, new(0f, 0f, -30f));
        holder.AddComponent<SortingGroup>();

        TextMeshPro nameText = null!, scoreText = null!;
        var nameWidget = new NoSGUIText(Virial.Media.GUIAlignment.Center, NameAttr, GUI.API.RawTextComponent("")) { PostBuilder = t => nameText = t };
        var scoreWidget = new NoSGUIText(Virial.Media.GUIAlignment.Center, ScoreAttr, GUI.API.RawTextComponent("")) { PostBuilder = t => scoreText = t };

        texts = GamePlayer.AllPlayers.Select(p =>
        {
            var myHolder = UnityHelper.CreateObject(p.Name, holder.transform, Vector3.zero);
            var nameObj = nameWidget.Instantiate(new(10f, 10f), out _)!;
            var scoreObj = scoreWidget.Instantiate(new(10f, 10f), out _)!;
            nameObj.transform.SetParent(myHolder.transform);
            nameObj.transform.localPosition = new(-0.2f, 0f, p.PlayerId * 0.002f);
            scoreObj.transform.SetParent(myHolder.transform);
            scoreObj.transform.localPosition = new(1f, 0f, p.PlayerId * 0.002f);
            nameText.text = p.PlayerName;
            return (p, myHolder, nameText, scoreText);
        }).ToArray();

        float lastQ = -1f;
        holder.AddComponent<ScriptBehaviour>().UpdateHandler += () =>
        {
            float alpha = interaction.ScoreBoardQ;
            foreach (var entry in texts)
            {
                Color c = (entry.player.AmOwner ? Color.yellow : Color.white).AlphaMultiplied(alpha);
                entry.nameText.color = c;
                entry.scoreText.color = c;
            }

            if (alpha > 0f && rankingCache != null)
            {
                float q = contentQ.Value;
                if (lastQ != q || requireUpdate)
                {
                    int count = rankingCache.Count;
                    foreach (var entry in texts)
                    {
                        if (!rankingCache.TryGetValue(entry.player.PlayerId, out var re)) continue;

                        float score = Mathn.Lerp(re.SingleScore, re.SumScore, q);
                        float pos = Mathn.Lerp(re.SingleRank, re.SumRank, q);
                        entry.scoreText.text = PaintQuizScoreMap.FormatScore(score) + "pt";
                        entry.holder.transform.localPosition = new(0f, ((count - 1) * 0.5f - pos) * 0.25f, 0f);
                    }
                    lastQ = q;
                    requireUpdate = false;
                }
            }
        };
    }

    public void ShowContent(float target, bool immediately, bool bloop = false)
    {
        if (immediately)
            contentQ = Arithmetic.Constant(target);
        else
        {
            contentQ = Arithmetic.Decel(contentQ.Value, target, 0.45f);
            if (bloop) NebulaManager.Instance.StartCoroutine(Effects.Bloop(0.45f, holder.transform));
        }
        requireUpdate = true;
    }

    public void UpdateRanking(PaintQuizScoreMap scoreMap)
    {
        rankingCache = BuildRanking(scoreMap);
        requireUpdate = true;
    }

    private static Dictionary<byte, RankingEntry> BuildRanking(PaintQuizScoreMap scoreMap)
    {
        var dic = new Dictionary<byte, RankingEntry>();
        foreach (var p in GamePlayer.AllPlayers) dic[p.PlayerId] = new();

        var singleOrder = GamePlayer.AllPlayers.Select(p => (p, scoreMap.GetLastAddedScore(p.PlayerId))).OrderBy(e => -e.Item2).ToArray();
        var sumOrder = GamePlayer.AllPlayers.Select(p => (p, scoreMap.GetScore(p.PlayerId))).OrderBy(e => -e.Item2).ToArray();

        for (int i = 0; i < singleOrder.Length; i++)
        {
            var e = dic[singleOrder[i].p.PlayerId];
            e.SingleRank = i;
            e.SingleScore = singleOrder[i].Item2;
        }
        for (int i = 0; i < sumOrder.Length; i++)
        {
            var e = dic[sumOrder[i].p.PlayerId];
            e.SumRank = i;
            e.SumScore = sumOrder[i].Item2;
        }
        return dic;
    }
}
