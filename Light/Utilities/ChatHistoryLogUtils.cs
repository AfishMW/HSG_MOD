using LightInDark.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Light.Utilities;

public static class ChatHistoryLogUtils
{
    private static string LogFilePath;
    private static string StartupTime;

    public static void Init()
    {
#if ANDROID
         return;
#endif
        StartupTime = DateTime.Now.ToString("yyyy/M/d HH:mm:ss");
        string gameRootPath = Path.Combine(Directory.GetParent(Application.dataPath)!.FullName, "LightLog.log");
        string determinedPath;
        try
        {
            using (File.Open(gameRootPath, FileMode.OpenOrCreate, FileAccess.Write)) { }
            determinedPath = gameRootPath;
        }
        catch
        {
            string localLow = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "..", "LocalLow", "Innersloth", "Among Us"
            );
            determinedPath = Path.Combine(Path.GetFullPath(localLow), "GiftLog.log");
            string directory = Path.GetDirectoryName(determinedPath)!;
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                Directory.CreateDirectory(directory);
        }

        LogFilePath = determinedPath;
        AppendLog("=====Log Started " + StartupTime + "=====");
    }
    public static void ChatInfo(PlayerControl pc,string msg)
    {
        string realMsg = $"{pc.Data.PlayerName}: {msg}";
        AppendLog($"[{DateTime.Now:HH:mm:ss} Info] {realMsg}");
    }
    static void AppendLog(string content)
    {
#if ANDROID
            return;
#endif
        try
        {
            File.AppendAllText(LogFilePath, content + Environment.NewLine);
        }
        catch { }
    }
}