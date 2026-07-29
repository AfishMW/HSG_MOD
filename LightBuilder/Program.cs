using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;
using Octokit;

// ===== 配置类 =====
public class BuilderConfig
{
    [JsonPropertyName("builderVersion")]
    public string BuilderVersion { get; set; } = "1.0.0.0";

    [JsonPropertyName("gamePath")]
    public string GamePath { get; set; } = "";

    [JsonPropertyName("outputPath")]
    public string OutputPath { get; set; } = "../OutPut";

    [JsonPropertyName("pluginProjectPath")]
    public string PluginProjectPath { get; set; } = "../LightPluginMain/LightPluginMain.csproj";

    [JsonPropertyName("githubRepoOwner")]
    public string GitHubRepoOwner { get; set; } = "AfishMW";

    [JsonPropertyName("githubRepoName")]
    public string GitHubRepoName { get; set; } = "HSG_MOD";

    [JsonPropertyName("githubToken")]
    public string GitHubToken { get; set; } = "";

    [JsonPropertyName("pluginGuid")]
    public string PluginGuid { get; set; } = "com.light.inthedark";

    [JsonPropertyName("pluginName")]
    public string PluginName { get; set; } = "LightInTheDark";

    [JsonPropertyName("pluginVersion")]
    public string PluginVersion { get; set; } = "1.0.0.0";

    [JsonPropertyName("visualVersion")]
    public string VisualVersion { get; set; } = "v1.0.0.0";
}

// ===== 主程序 =====
class Program
{
    static string ConfigPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config.json");

    static void Main()
    {
        var config = LoadConfig();
        string? githubReleaseUrl = null;

        GitHubClient? githubClient = null;
        try
        {
            // 优先使用配置中的 Token，如果为空则从环境变量读取
            var token = config.GitHubToken;
            if (string.IsNullOrEmpty(token) || token == "LIGHT_GITHUB_TOKEN")
                token = Environment.GetEnvironmentVariable("LIGHT_GITHUB_TOKEN") ?? "";

            if (!string.IsNullOrEmpty(token))
            {
                githubClient = new GitHubClient(new ProductHeaderValue("LightMod"))
                {
                    Credentials = new Credentials(token)
                };
            }
            else
            {
                Console.WriteLine("警告: 未配置 GitHub Token，发布功能不可用。");
                Console.WriteLine("提示: 可在 config.json 中设置 githubToken，或设置环境变量 LIGHT_GITHUB_TOKEN。");
            }
        }
        catch
        {
            Console.WriteLine("警告: GitHub 客户端初始化失败，发布功能不可用。");
        }

        Console.WriteLine("==============================");
        Console.WriteLine("  Light In The Dark 构建工具");
        Console.WriteLine("  构建工具版本: " + config.BuilderVersion);
        Console.WriteLine("  模组版本:     " + config.VisualVersion);
        Console.WriteLine("==============================");

        bool exit = false;
        while (!exit)
        {
            Console.WriteLine("\n请选择操作:");
            Console.WriteLine("  1. 编译模组 (Build DLL)");
            Console.WriteLine("  2. 打开 GitHub 发布页");
            Console.WriteLine("  3. 修改并复制发布版名称");
            Console.WriteLine("  4. 修改并复制版本标签");
            Console.WriteLine("  5. 发布最新版");
            Console.WriteLine("  6. 退出");
            Console.Write("> ");

            var key = Console.ReadKey(true).Key;

            switch (key)
            {
                case ConsoleKey.D1:
                case ConsoleKey.NumPad1:
                    BuildMod(config);
                    break;

                case ConsoleKey.D2:
                case ConsoleKey.NumPad2:
                    OpenGitHubReleasePage(config);
                    break;

                case ConsoleKey.D3:
                case ConsoleKey.NumPad3:
                    ModifyAndCopyDisplayName(config);
                    break;

                case ConsoleKey.D4:
                case ConsoleKey.NumPad4:
                    ModifyAndCopyTagVersion(config);
                    break;

                case ConsoleKey.D5:
                case ConsoleKey.NumPad5:
                    if (githubClient != null)
                        githubReleaseUrl = PublishRelease(config, githubClient);
                    else
                        Console.WriteLine("错误: GitHub 客户端未初始化，请检查配置。");
                    break;

                case ConsoleKey.D6:
                case ConsoleKey.NumPad6:
                    Console.WriteLine("退出程序。");
                    exit = true;
                    break;

                default:
                    Console.WriteLine("无效输入，请按 1-6 选择。");
                    break;
            }
        }
    }

    // ===== 配置管理 =====
    static BuilderConfig LoadConfig()
    {
        try
        {
            if (File.Exists(ConfigPath))
            {
                var json = File.ReadAllText(ConfigPath);
                return JsonSerializer.Deserialize<BuilderConfig>(json) ?? new BuilderConfig();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"读取配置文件失败: {ex.Message}");
        }
        return new BuilderConfig();
    }

    static void SaveConfig(BuilderConfig config)
    {
        try
        {
            var json = JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(ConfigPath, json);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"保存配置文件失败: {ex.Message}");
        }
    }

    // ===== 1. 编译模组 =====
    static void BuildMod(BuilderConfig config)
    {
        if (string.IsNullOrEmpty(config.GamePath))
        {
            Console.WriteLine("错误: 未配置游戏路径 (gamePath)，请在 config.json 中设置。");
            return;
        }

        var projectPath = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, config.PluginProjectPath));
        var outputPath = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, config.OutputPath));

        if (!File.Exists(projectPath))
        {
            Console.WriteLine($"错误: 未找到项目文件: {projectPath}");
            return;
        }

        // 确保输出目录存在
        Directory.CreateDirectory(outputPath);

        Console.WriteLine($"编译项目: {projectPath}");
        Console.WriteLine($"输出目录: {outputPath}");

        var process = new Process();
        process.StartInfo.FileName = "dotnet";
        process.StartInfo.Arguments = $"build \"{projectPath}\" -c Release -p:GamePath=\"{config.GamePath}\" -o \"{outputPath}\"";
        process.StartInfo.UseShellExecute = false;
        process.StartInfo.RedirectStandardOutput = true;
        process.StartInfo.RedirectStandardError = true;
        process.Start();

        string output = process.StandardOutput.ReadToEnd();
        string error = process.StandardError.ReadToEnd();
        process.WaitForExit();

        Console.WriteLine(output);

        if (process.ExitCode == 0)
        {
            var dllPath = Path.Combine(outputPath, "Light.dll");
            if (File.Exists(dllPath))
                Console.WriteLine($"\n编译成功！DLL 已输出至: {dllPath}");
            else
                Console.WriteLine("\n编译成功！但未找到 Light.dll，请检查输出目录。");
        }
        else
        {
            Console.WriteLine($"\n编译失败 (退出码: {process.ExitCode})");
            if (!string.IsNullOrEmpty(error))
                Console.WriteLine(error);
        }
    }

    // ===== 2. 打开 GitHub =====
    static void OpenGitHubReleasePage(BuilderConfig config)
    {
        var url = $"https://github.com/{config.GitHubRepoOwner}/{config.GitHubRepoName}/releases/new";
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = url,
                UseShellExecute = true
            });
            Console.WriteLine($"已打开 GitHub 发布页: {url}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"打开网页失败: {ex.Message}");
        }
    }

    // ===== 3. 修改并复制发布版名称 =====
    static void ModifyAndCopyDisplayName(BuilderConfig config)
    {
        Console.WriteLine($"当前发布版名称: {config.VisualVersion}");
        Console.Write("输入新的版本名称 (直接回车保持不变): ");
        var input = Console.ReadLine()?.Trim();
        if (!string.IsNullOrEmpty(input))
        {
            config.VisualVersion = input;
            SaveConfig(config);
        }

        var displayName = $"Light In The Dark {config.VisualVersion}";
        CopyToClipboard(displayName);
        Console.WriteLine($"已复制: {displayName}");
    }

    // ===== 4. 修改并复制版本标签 =====
    static void ModifyAndCopyTagVersion(BuilderConfig config)
    {
        Console.WriteLine($"当前版本号: {config.PluginVersion}");
        Console.Write("输入新的版本号 (直接回车保持不变): ");
        var input = Console.ReadLine()?.Trim();
        if (!string.IsNullOrEmpty(input))
        {
            config.PluginVersion = input;
            SaveConfig(config);
        }

        var tag = $"v{config.PluginVersion}";
        CopyToClipboard(tag);
        Console.WriteLine($"已复制: {tag}");
    }

    // ===== 5. 发布最新版 =====
    static string? PublishRelease(BuilderConfig config, GitHubClient client)
    {
        var defaultDllPath = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, config.OutputPath, "Light.dll"));

        // 1. 输入 DLL 路径
        Console.WriteLine($"默认 DLL 路径: {defaultDllPath}");
        Console.Write("输入 DLL 路径 (直接回车使用默认路径, 输入 0 取消): ");
        var dllInput = Console.ReadLine()?.Trim();
        if (dllInput == "0")
        {
            Console.WriteLine("已取消发布。");
            return null;
        }

        var dllPath = string.IsNullOrEmpty(dllInput) ? defaultDllPath : Path.GetFullPath(dllInput);

        if (!File.Exists(dllPath))
        {
            Console.WriteLine("错误: 未找到 DLL 文件，请检查路径。");
            return null;
        }

        // 2. 输入版本号
        Console.WriteLine($"当前版本号: {config.PluginVersion} (发布名称: {config.VisualVersion})");
        Console.Write("输入版本号 (直接回车保持当前版本, 输入 0 取消): ");
        var versionInput = Console.ReadLine()?.Trim();
        if (versionInput == "0")
        {
            Console.WriteLine("已取消发布。");
            return null;
        }

        var version = string.IsNullOrEmpty(versionInput) ? config.PluginVersion : versionInput;
        var visualVersion = $"v{version}";

        // 3. 输入发布说明
        Console.WriteLine("输入发布说明 (支持 <br> 换行，空行取消):");
        Console.Write("> ");
        var description = Console.ReadLine()?.Trim();
        if (string.IsNullOrEmpty(description))
        {
            Console.WriteLine("已取消发布。");
            return null;
        }

        try
        {
            var tagName = $"v{version}";
            var releaseName = $"Light In The Dark v{version}";
            var body = description.Replace("<br>", "\r\n");

            Console.WriteLine("正在创建 GitHub Release...");

            var newRelease = new NewRelease(tagName)
            {
                Name = releaseName,
                Body = body,
                Draft = false,
                Prerelease = false
            };

            var release = client.Repository.Release.Create(config.GitHubRepoOwner, config.GitHubRepoName, newRelease).Result;

            Console.WriteLine("正在上传 DLL...");
            using var stream = File.OpenRead(dllPath);
            var asset = new ReleaseAssetUpload($"Light_{version}.dll", "application/octet-stream", stream, null);
            client.Repository.Release.UploadAsset(release, asset).Wait();

            // 保存本次发布的版本号到配置
            config.PluginVersion = version;
            config.VisualVersion = visualVersion;
            SaveConfig(config);

            Console.WriteLine("发布成功！");
            CopyToClipboard(release.HtmlUrl);
            Console.WriteLine($"Release URL: {release.HtmlUrl} (已复制到剪贴板)");

            return release.HtmlUrl;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"发布失败: {ex.Message}");
            return null;
        }
    }

    // ===== 工具方法 =====
    static void CopyToClipboard(string text)
    {
        try
        {
            var thread = new Thread(() =>
            {
                System.Windows.Forms.Clipboard.SetText(text);
            });
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            thread.Join();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"复制到剪贴板失败: {ex.Message}");
        }
    }
}
