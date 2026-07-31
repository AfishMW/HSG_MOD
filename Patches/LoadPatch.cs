using BepInEx.Unity.IL2CPP.Utils.Collections;
using HarmonyLib;
using Light.Utilities;
using LightInDark;
using System.Collections;
using TMPro;
using UnityEngine;

namespace Light.Patches;

[HarmonyPriority(Priority.HigherThanNormal)]
[HarmonyPatch(typeof(SplashManager), nameof(SplashManager.Update))]
public static class LoadPatch
{
    private static readonly string[] Quotes =
    [
        "\"在战争中，没有什么比胜利更令人振奋的了。\" —— 温斯顿·丘吉尔",
        "\"我们将在海滩作战，我们将在登陆点作战，我们将在田野和街头作战。\" —— 温斯顿·丘吉尔",
        "\"我所能奉献的唯有热血、辛劳、眼泪和汗水。\" —— 温斯顿·丘吉尔",
        "\"胜利越来之不易，胜利的荣耀就越伟大。\" —— 温斯顿·丘吉尔",
        "\"在战争与屈辱面前，你选择了屈辱，但将来你还得面对战争。\" —— 温斯顿·丘吉尔",
        "\"闪电战的本质是以最快的速度集中最强力量打击敌人最弱点。\" —— 海因茨·古德里安",
        "\"老兵永不死，只是渐凋零。\" —— 道格拉斯·麦克阿瑟",
        "\"我们唯一的恐惧就是恐惧本身。\" —— 富兰克林·D·罗斯福",
        "\"昨天，1941年12月7日——将永远成为耻辱的象征。\" —— 富兰克林·D·罗斯福",
        "\"租借法案是美国对捍卫自由事业的最大贡献。\" —— 富兰克林·D·罗斯福",
        "\"战争的目的不是为国捐躯，而是让敌人为国捐躯。\" —— 乔治·S·巴顿",
        "\"勇气就是面对恐惧时依然坚持到底。\" —— 乔治·S·巴顿",
        "\"在俄国广阔的草原上，德国的闪电战终于碰上了它的对手。\" —— 格奥尔吉·朱可夫",
        "\"如果我知道德国会入侵，我会在一周前就动员军队。\" —— 约瑟夫·斯大林",
        "\"这场战争也是全人类的战争，是正义与邪恶的较量。\" —— 德怀特·D·艾森豪威尔"
    ];

    private const float MinLoadTime = 2f;
    private const float QuoteInterval = 4f;
    // 等待原版 Among Us 水獭图标展示完成后再显示模组 Logo
    private const float SplashDisplayTime = 4.5f;

    private static Sprite? logoSprite;
    private static SpriteRenderer? logo;
    private static SpriteRenderer? logoGlow;
    private static TextMeshPro? loadText;
    private static TextMeshPro? quoteText;
    private static TextMeshPro? versionText;
    private static int currentQuoteIndex;
    private static float quoteTimer;
    private static float loadStageTimer;

    private static bool loaded;
    private static bool cachedDoneLoadingRefData;

    public static string LoadingText
    {
        set
        {
            if (loadText != null) loadText.text = value;
        }
    }

    private static string GetNextLoadStage(float elapsed)
    {
        return elapsed switch
        {
            < 0.1f => "正在加载资源...",
            < 0.3f => "正在解压资源包...",
            < 0.5f => "正在初始化模组核心...",
            < 0.7f => "正在注册组件...",
            < 0.9f => "正在配置 Harmony 补丁...",
            < 1.2f => "正在加载语言数据...",
            < 1.5f => "正在准备游戏环境...",
            < 1.8f => "正在校准模组参数...",
            < 2.0f => "正在建立通信管道...",
            _ => "准备就绪..."
        };
    }

    private static IEnumerator CoLoadLight(SplashManager instance)
    {
        // 创建 Logo
        logo = UnityHelper.CreateObject<SpriteRenderer>("LightLogo", null, new Vector3(0, 0.5f, -5f));

        // 创建 Logo 发光层（叠加在 Logo 下方，略大一圈）
        logoGlow = UnityHelper.CreateObject<SpriteRenderer>("LightLogoGlow", null, new Vector3(0, 0.5f, -4.8f));

        // 加载 Logo 纹理
        var texture = GraphicsHelper.LoadTextureFromResources("Light.Resources.Logo.HalfSugarGift.png");
        if (texture != null)
        {
            logoSprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f), 100f);
            logo.sprite = logoSprite;
            logoGlow.sprite = logoSprite;
        }

        // Logo + 发光缩放淡入动画
        float p = 1f;
        while (p > 0f)
        {
            p -= Time.deltaTime * 2.8f;
            float alpha = 1f - p;
            logo.color = new Color(1f, 1f, 1f, alpha).ToUnityColor();
            // 发光层：比 Logo 稍大，透明度先快速提升再缓慢降低
            float glowAlpha = Mathf.Min(1f, alpha * (p * 2f + 0.3f));
            logoGlow.color = new Color(1f, 1f, 1f, glowAlpha * 0.5f).ToUnityColor();
            logo.transform.localScale = Vector3.one * (p * p * 0.012f + 1f);
            logoGlow.transform.localScale = Vector3.one * (p * p * 0.015f + 1.04f);
            yield return null;
        }
        logo.color = UnityEngine.Color.white;
        logo.transform.localScale = Vector3.one;
        // 发光层继续淡入维持
        logoGlow.color = new Color(1f, 1f, 1f, 0.45f).ToUnityColor();
        logoGlow.transform.localScale = Vector3.one * 1.04f;

        // 创建加载进度文字
        loadText = UnityEngine.Object.Instantiate(instance.errorPopup.InfoText, null);
        loadText.transform.localPosition = new Vector3(0f, -0.8f, -10f);
        loadText.fontStyle = FontStyles.Bold;
        loadText.text = "正在加载资源...";
        loadText.color = new Color(1f, 1f, 1f, 0.3f).ToUnityColor();

        // 创建底部名人名言文字（灰色斜体）
        quoteText = UnityEngine.Object.Instantiate(instance.errorPopup.InfoText, null);
        quoteText.transform.localPosition = new Vector3(0f, -2.8f, -10f);
        quoteText.fontStyle = FontStyles.Italic;
        quoteText.color = new Color(0.6f, 0.6f, 0.6f, 0.8f).ToUnityColor();
        quoteText.fontSize *= 0.7f;
        quoteText.text = Quotes[0];

        // 创建右下角版本号
        versionText = UnityEngine.Object.Instantiate(instance.errorPopup.InfoText, null);
        versionText.transform.localPosition = new Vector3(4.5f, -3.2f, -10f);
        versionText.fontStyle = FontStyles.Italic;
        versionText.color = new Color(0.5f, 0.5f, 0.5f, 0.6f).ToUnityColor();
        versionText.fontSize *= 0.55f;
        versionText.alignment = TextAlignmentOptions.BottomRight;
        versionText.text = $"{LIDPlugin.VisualVersion}";

        loadStageTimer = 0f;
        currentQuoteIndex = 0;
        quoteTimer = 0f;

        while (!cachedDoneLoadingRefData || loadStageTimer < MinLoadTime)
        {
            if (cachedDoneLoadingRefData)
                loadStageTimer += Time.deltaTime;

            quoteTimer += Time.deltaTime;

            // 轮播名言
            if (quoteTimer >= QuoteInterval)
            {
                quoteTimer = 0f;
                currentQuoteIndex = (currentQuoteIndex + 1) % Quotes.Length;
                quoteText.text = Quotes[currentQuoteIndex];
            }

            // 更新加载阶段文字
            loadText.text = GetNextLoadStage(loadStageTimer);

            // 发光层脉冲呼吸效果
            float breathe = Mathf.Sin(Time.time * 1.5f) * 0.12f + 0.35f;
            logoGlow.color = new Color(1f, 1f, 1f, breathe).ToUnityColor();

            yield return null;
        }

        // 加载完成闪烁提示
        loadText.text = "加载完成";
        for (int i = 0; i < 3; i++)
        {
            loadText.gameObject.SetActive(false);
            yield return new WaitForSeconds(0.03f);
            loadText.gameObject.SetActive(true);
            yield return new WaitForSeconds(0.03f);
        }

        UnityEngine.Object.Destroy(loadText.gameObject);
        UnityEngine.Object.Destroy(quoteText.gameObject);
        UnityEngine.Object.Destroy(versionText.gameObject);

        // 发光层先淡出
        p = 0f;
        while (p < 1f)
        {
            p += Time.deltaTime * 2f;
            logoGlow.color = new Color(1f, 1f, 1f, 0.45f * (1f - p)).ToUnityColor();
            yield return null;
        }
        UnityEngine.Object.Destroy(logoGlow.gameObject);

        // Logo 淡出
        p = 1f;
        while (p > 0f)
        {
            p -= Time.deltaTime * 1.2f;
            logo.color = new UnityEngine.Color(1f, 1f, 1f, p);
            yield return null;
        }
        logo.color = UnityEngine.Color.clear;

        instance.sceneChanger.AllowFinishLoadingScene();
        instance.startedSceneLoad = true;
    }

    public static bool Prefix(SplashManager __instance)
    {
        cachedDoneLoadingRefData |= __instance.doneLoadingRefdata;
        __instance.doneLoadingRefdata = false;

        if (cachedDoneLoadingRefData
            && !__instance.startedSceneLoad
            && Time.time - __instance.startTime > Mathf.Max(__instance.minimumSecondsBeforeSceneChange, SplashDisplayTime)
            && !loaded)
        {
            loaded = true;
            __instance.StartCoroutine(CoLoadLight(__instance).WrapToIl2Cpp());
        }

        return false;
    }

    public static void Postfix(SplashManager __instance)
    {
        __instance.doneLoadingRefdata = cachedDoneLoadingRefData;
    }
}
