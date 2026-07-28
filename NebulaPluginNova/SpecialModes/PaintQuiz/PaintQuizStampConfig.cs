using Nebula.Modules.Cosmetics;

namespace Nebula.SpecialModes.PaintQuiz;

/// <summary>
/// グレード(0-4)とスタンプの対応を管理する。
/// grade 0 = スタンプ無し, 1=-1pt, 2=0.5pt, 3=1pt, 4=2pt
/// </summary>
internal static class PaintQuizStampConfig
{
    private static readonly DataSaver MySaver = new("PaintQuizStamps");

    // インデックス 0=grade1(-1pt), 1=grade2(0.5pt), 2=grade3(1pt), 3=grade4(2pt)
    private static readonly StringArrayDataEntry GradeStamps = new("GradeStamps", MySaver, [
        "stamp_セオノ_評価 バツ",
        "stamp_セオノ_評価 三角",
        "stamp_セオノ_評価 マル",
        "stamp_セオノ_評価 花マル",
    ]);

    // ゲームセッション中にホストから受信したスタンプID (非永続)
    private static string[]? _sessionStampIds = null;

    public static string GetStampId(byte grade)
    {
        if (grade < 1 || grade > 4) return "";
        var table = GradeStamps.Value;
        int idx = grade - 1;
        return idx < table.Length ? table[idx] : "";
    }

    // ホストのDataSaver設定を全て取得する (RPC送信用)
    public static string[] GetAllStampIds()
    {
        var table = GradeStamps.Value;
        var result = new string[4];
        for (int i = 0; i < 4; i++)
            result[i] = i < table.Length ? table[i] : "";
        return result;
    }

    // ホストから受信したスタンプIDをセッション用に保存する (DataSaverは変更しない)
    public static void SetSessionStampIds(string[] ids) => _sessionStampIds = ids;

    public static CosmicStamp? GetStamp(byte grade)
    {
        var id = GetStampId(grade);
        if (string.IsNullOrEmpty(id)) return null;
        return MoreCosmic.AllStamps.TryGetValue(id, out var stamp) ? stamp : null;
    }

    public static void SetStampId(byte grade, string stampId)
    {
        if (grade < 1 || grade > 4) return;
        var table = GradeStamps.Value.ToArray();
        int idx = grade - 1;
        if (idx >= table.Length)
        {
            var newTable = new string[idx + 1];
            for (int i = 0; i < table.Length; i++) newTable[i] = table[i];
            table = newTable;
        }
        table[idx] = stampId;
        GradeStamps.Value = table;
    }
}
