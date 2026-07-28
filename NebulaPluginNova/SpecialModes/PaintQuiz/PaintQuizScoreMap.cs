namespace Nebula.SpecialModes.PaintQuiz;

internal class PaintQuizScoreMap
{
    private readonly Dictionary<byte, (float cumulative, float lastAdded)> data = [];

    public float GetScore(byte playerId) => data.TryGetValue(playerId, out var v) ? v.cumulative : 0f;
    public float GetLastAddedScore(byte playerId) => data.TryGetValue(playerId, out var v) ? v.lastAdded : 0f;

    public void AddScore(byte playerId, float score)
        => data[playerId] = (GetScore(playerId) + score, score);

    // grade: 0=no stamp(0pt), 1=-1pt, 2=0.5pt, 3=1pt, 4=2pt
    public static float GradeToPoints(byte grade) => grade switch
    {
        1 => -1f,
        2 => 0.5f,
        3 => 1f,
        4 => 2f,
        _ => 0f
    };

    public static string FormatScore(float score)
    {
        if (score == MathF.Floor(score)) return ((int)score).ToString();
        return score.ToString("0.#");
    }

    public static string FormatScoreWithSign(float score)
    {
        string s = FormatScore(score);
        return score > 0f ? "+" + s : s;
    }
}
