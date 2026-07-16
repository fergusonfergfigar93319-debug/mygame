using PenguinRun.Game.Mission;
using PenguinRun.Game.Save;
using PenguinRun.Game.Shop;
using PenguinRun.Game.Leaderboard;
using PenguinRun.Game;
using PenguinRun.Game.Growth;
using PenguinRun.Game.UI.Lobby;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace PenguinRun.Game.UI
{
    /// <summary>
    /// MainMenu 场景入口：构建 Canvas + 顶栏 + 主按钮 + 底部底坞，并管理面板的打开/关闭。
    /// 所有 UI 程序化构建，避免手写 .unity YAML。
    /// </summary>
    public sealed class MainMenuBootstrap : MonoBehaviour
    {
        private Canvas canvas;
        private Snackbar snackbar;

        private Text nicknameText;
        private Text fishSnacksText;
        private Text levelText;

        /// <summary>顶栏下方单行活动 Ticker。</summary>
        private Text topTickerText;

        /// <summary>今日挑战卡片右侧的鱼干预览。</summary>
        private Text dailyFishRewardChipText;

        private Text strategyHeadlineText;
        private Text strategyDetailText;
        private Text strategyButtonText;
        private Button strategyButton;
        private StrategyAction currentStrategyAction;

        private RectTransform mascotArea;
        private RectTransform avatarMiniArea;
        private Text boosterPreviewText;
        /// <summary>主 CTA 上方可点击难度芯片文案。</summary>
        private Text difficultyHintText;
        private Button difficultyChipButton;
        private GameObject profileStatsOverlay;
        private Transform panelLayer;
        private LobbyPanelRouter panelRouter;
        private LobbyTopRibbon lobbyTopRibbon;
        private LobbyJournalPage lobbyJournalPage;
        private LobbyChapterPage lobbyChapterPage;
        private LobbyCampScene lobbyCampScene;
        private LobbyDock lobbyDock;
        private readonly LobbyModalStack modalStack = new();
        private readonly RunLaunchGate runLaunchGate = new();
        private RectTransform dockWrap;
        private RectTransform versionLabel;

        /// <summary>「开始游戏」：归入 MenuPrimaryCtaStrip，避免叠层顺序导致点击被吞。</summary>
        private Transform startGameButton;

        /// <summary>「今日挑战」：同上。</summary>
        private Transform dailyChallengeButton;

        private class DockItem
        {
            public string panelName;
            public Image iconBg;
            public Text iconText;
            public Text labelText;
            public Image labelBg;
            public Image activeLine;
            public Color accentColor;
            public bool wasSelected;
            public GameObject iconGlow;
            public GameObject badgeRoot;
            public Text badgeCountText;
        }
        private readonly System.Collections.Generic.List<DockItem> dockItems = new();

        private enum DockTabBadgeKind
        {
            None,
            NumericMission,
            NumericCamp,
        }

        private enum StrategyAction
        {
            CheckIn,
            OpenMissions,
            OpenCampShop,
            OpenCodex,
            OpenLeaderboard,
            StartEndless,
            StartDaily,
        }

        private enum DifficultyOverlayMode
        {
            BeforeStoryRun,
            BeforeEndlessRun,
            BeforeDailyRun,
            SettingsOnly,
        }

        private static void EnsureLandscape()
        {
#if UNITY_ANDROID || UNITY_IOS
            if (Application.isMobilePlatform)
            {
                Screen.autorotateToPortrait = false;
                Screen.autorotateToPortraitUpsideDown = false;
                Screen.autorotateToLandscapeLeft = true;
                Screen.autorotateToLandscapeRight = true;
                if (Screen.orientation == ScreenOrientation.Portrait ||
                    Screen.orientation == ScreenOrientation.PortraitUpsideDown)
                {
                    Screen.orientation = ScreenOrientation.LandscapeLeft;
                }
                else if (Screen.orientation == ScreenOrientation.AutoRotation ||
                         Screen.orientation == ScreenOrientation.LandscapeLeft ||
                         Screen.orientation == ScreenOrientation.LandscapeRight)
                {
                    // 已是横屏或自动旋转，保留玩家当前握姿
                }
                else
                {
                    Screen.orientation = ScreenOrientation.LandscapeLeft;
                }
            }
#endif
        }

        private void Awake()
        {
            SaveSchema.EnsureMigrated();

            // 确保场景过渡管理器存在
            if (SceneTransitionManager.Instance == null)
            {
                var tm = new GameObject("SceneTransitionManager");
                tm.AddComponent<SceneTransitionManager>();
            }

            EnsureLandscape();
            EnsureEventSystem();
            var isNewLogin = PlayerSave.TryRecordLogin();
            BuildScene();
            var lobbyGo = new GameObject("MenuLobbyAudio");
            lobbyGo.transform.SetParent(transform, false);
            lobbyGo.AddComponent<MenuLobbyAudio>();
            ShowResultOverlayIfAny();

            // 首次进入游戏或每日首次登录时，展示签到引导
            if (isNewLogin && !PlayerSave.IsDailyCheckInClaimed())
            {
                ShowFirstLoginPrompt();
            }
        }

        private void ShowFirstLoginPrompt()
        {
            var streak = Mathf.Max(1, PlayerSave.LoginStreak);
            var reward = PlayerSave.PreviewDailyCheckInReward();
            if (reward <= 0) return;

            var overlay = UiBuilder.CreatePanel("FirstLoginOverlay", canvas.transform,
                new Color(0f, 0f, 0f, 0.65f));
            modalStack.Register(overlay);
            overlay.transform.SetAsLastSibling();

            var card = UiBuilder.CreateRect("WelcomeCard", overlay.transform,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(580f, 460f), Vector2.zero,
                UiBuilder.Surface1, rounded: true);
            UiBuilder.AddOutline(card.gameObject, UiBuilder.BorderAccent, new Vector2(1.5f, -1.5f));
            UiBuilder.AddShadow(card.gameObject, new Color(0f, 0f, 0f, 0.5f), new Vector2(0f, -8f));

            // 企鹅欢迎图标
            var iconBadge = UiBuilder.CreateRect("WelcomeIcon", card,
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(96f, 96f), new Vector2(0f, -36f),
                new Color(0.1f, 0.25f, 0.5f, 1f), circle: true);
            iconBadge.pivot = new Vector2(0.5f, 1f);
            UiBuilder.AddOutline(iconBadge.gameObject, UiBuilder.BorderAccent, new Vector2(2f, -2f));
            UiBuilder.CreateText("IconText", iconBadge.transform, "❖", 44, FontStyle.Bold,
                TextAnchor.MiddleCenter, UiBuilder.AccentCyan);

            // 欢迎标题
            var titleStr = streak <= 1 ? "欢迎来到企鹅快跑！" : $"欢迎回来 · 第 {streak} 天";
            UiBuilder.CreateText("Title", card, titleStr,
                34, FontStyle.Bold, TextAnchor.MiddleCenter, Color.white);
            var trt = (RectTransform)card.Find("Title");
            trt.anchorMin = new Vector2(0f, 1f);
            trt.anchorMax = new Vector2(1f, 1f);
            trt.sizeDelta = new Vector2(0f, 50f);
            trt.anchoredPosition = new Vector2(0f, -144f);

            // 奖励预览
            var todayStr = $"今日签到可得 +{reward} 鱼干";
            UiBuilder.CreateText("Subtitle", card, todayStr,
                28, FontStyle.Bold, TextAnchor.MiddleCenter, UiBuilder.WarmGold);
            var srt = (RectTransform)card.Find("Subtitle");
            srt.anchorMin = new Vector2(0f, 1f);
            srt.anchorMax = new Vector2(1f, 1f);
            srt.sizeDelta = new Vector2(0f, 40f);
            srt.anchoredPosition = new Vector2(0f, -200f);

            // 引导文字
            var guideStr = streak <= 1
                ? "在极夜冰原上奔跑，收集鱼干，击败 Boss！\n左侧查看今日建议，下方入口探索营地与商店。"
                : "左侧查看今日建议，先去营地升级能力，\n然后挑战极夜快跑或今日限定关卡。";
            UiBuilder.CreateText("Guide", card, guideStr,
                18, FontStyle.Normal, TextAnchor.MiddleCenter, UiBuilder.TextSecondary);
            var grt = (RectTransform)card.Find("Guide");
            grt.anchorMin = new Vector2(0f, 1f);
            grt.anchorMax = new Vector2(1f, 1f);
            grt.sizeDelta = new Vector2(0f, 60f);
            grt.anchoredPosition = new Vector2(0f, -260f);

            // 签到按钮
            var checkInBtn = UiBuilder.CreateButton("CheckInNow", card, "立即签到",
                new Color(1f, 0.84f, 0.32f, 1f), new Color(0.08f, 0.05f, 0.01f, 1f), 28, rounded: true);
            var cbrt = (RectTransform)checkInBtn.transform;
            cbrt.anchorMin = new Vector2(0.5f, 0f);
            cbrt.anchorMax = new Vector2(0.5f, 0f);
            cbrt.pivot = new Vector2(0.5f, 0f);
            cbrt.sizeDelta = new Vector2(320f, 68f);
            cbrt.anchoredPosition = new Vector2(0f, 48f);
            UiBuilder.AddOutline(checkInBtn.gameObject, new Color(0.5f, 0.35f, 0.08f, 0.7f), new Vector2(2f, -2f));
            UiBuilder.AddShadow(checkInBtn.gameObject, new Color(0f, 0f, 0f, 0.4f), new Vector2(0f, -4f));
            checkInBtn.onClick.AddListener(() =>
            {
                DismissLobbyModal(overlay);
                OnCheckInClicked();
            });

            // 跳过按钮
            var skipBtn = UiBuilder.CreateButton("Skip", card, "稍后再说",
                new Color(0.12f, 0.22f, 0.36f, 0.9f), UiBuilder.TextSecondary, 22, rounded: true);
            var srtt = (RectTransform)skipBtn.transform;
            srtt.anchorMin = new Vector2(0.5f, 0f);
            srtt.anchorMax = new Vector2(0.5f, 0f);
            srtt.pivot = new Vector2(0.5f, 0f);
            srtt.sizeDelta = new Vector2(260f, 48f);
            srtt.anchoredPosition = new Vector2(0f, -2f);
            UiBuilder.AddOutline(skipBtn.gameObject, UiBuilder.BorderSubtle, new Vector2(1f, -1f));
            skipBtn.onClick.AddListener(() => DismissLobbyModal(overlay));
        }

        private void OnEnable()
        {
            RefreshHeader();
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                if (modalStack.TryDismissTop())
                {
                    return;
                }
                if (panelRouter != null && panelRouter.HasOpenPanel)
                {
                    ClosePanel();
                }
                else
                {
#if UNITY_EDITOR
                    UnityEditor.EditorApplication.isPlaying = false;
#else
                    Application.Quit();
#endif
                }
            }
        }

        private static void EnsureEventSystem()
        {
            if (FindAnyObjectByType<EventSystem>() != null) return;
            var go = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
            DontDestroyOnLoad(go);
        }

        private void BuildScene()
        {
            BuildBackground();
            BuildCanvas();
            lobbyCampScene = LobbyCampScene.Build(canvas.transform,
                new LobbyCampActions(OpenCampShop, OpenCodex));
            lobbyTopRibbon = LobbyTopRibbon.Build(canvas.transform);
            lobbyJournalPage = LobbyJournalPage.Build(canvas.transform);
            lobbyChapterPage = LobbyChapterPage.Build(canvas.transform);
            lobbyDock = LobbyDock.Build(canvas.transform, new LobbyDockActions(
                OpenCampShop, OpenMissions, StartEndless, OpenCodex, OpenSettings));
            dockWrap = lobbyDock.Root;
            panelLayer.SetAsLastSibling();
            snackbar = Snackbar.EnsureUnder(canvas);
            snackbar.transform.SetAsLastSibling();
            RefreshStorybookLobby();

            // 右下角版本号
            var versionText = UiBuilder.CreateText("VersionText", canvas.transform,
                $"v{Application.version} · 企鹅快跑", 14, FontStyle.Normal, TextAnchor.LowerRight,
                new Color(0.4f, 0.6f, 0.8f, 0.35f));
            var vrt = (RectTransform)versionText.transform;
            versionLabel = vrt;
            vrt.anchorMin = new Vector2(1f, 0f);
            vrt.anchorMax = new Vector2(1f, 0f);
            vrt.pivot = new Vector2(1f, 0f);
            vrt.sizeDelta = new Vector2(200f, 24f);
            vrt.anchoredPosition = new Vector2(-24f, 160f);
            versionText.raycastTarget = false;
        }

        /// <summary>将开始游戏 + 今日挑战挂到全屏前景根下并提到底坞之上，射线命中顺序稳定。</summary>
        private void BuildBackground()
        {
            var cam = Camera.main;
            if (cam == null)
            {
                var camGo = new GameObject("MainMenu Camera", typeof(Camera));
                cam = camGo.GetComponent<Camera>();
                camGo.tag = "MainCamera";
            }

            if (cam.GetComponent<AudioListener>() == null)
                cam.gameObject.AddComponent<AudioListener>();

            cam.clearFlags = CameraClearFlags.SolidColor;
            // 更深邃的极夜紫蓝渐变底色，与 UI 设计系统色调一致
            cam.backgroundColor = new Color(0.035f, 0.055f, 0.115f);
            cam.orthographic = true;
        }

        private void BuildCanvas()
        {
            var go = UiBuilder.CreateCanvas("MenuCanvas");
            canvas = go.GetComponent<Canvas>();

            // ========== 背景大气层：极夜极光主题 ==========
            // 控制大面积光层 alpha，避免出现半透明矩形“盖板感”。

            // 顶部夜空辉光（轻量）
            var skyGlow = UiBuilder.CreateRect(
                "SkyGlow", canvas.transform,
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(2200f, 1200f), new Vector2(0f, 220f),
                new Color(0.14f, 0.32f, 0.62f, 0.22f), glow: true);
            skyGlow.GetComponent<Image>().raycastTarget = false;

            // 极光光层1：紫粉（左上）
            var aurora1 = UiBuilder.CreateRect(
                "Aurora1", canvas.transform,
                new Vector2(0.3f, 1f), new Vector2(0.3f, 1f),
                new Vector2(1200f, 560f), new Vector2(-120f, -260f),
                new Color(0.72f, 0.42f, 0.92f, 0.11f), glow: true);
            aurora1.GetComponent<Image>().raycastTarget = false;
            var d1 = aurora1.gameObject.AddComponent<DriftTween>();
            d1.amplitude = 24f; d1.speed = 0.34f;

            // 极光光层2：青绿（右上）
            var aurora2 = UiBuilder.CreateRect(
                "Aurora2", canvas.transform,
                new Vector2(0.7f, 1f), new Vector2(0.7f, 1f),
                new Vector2(1100f, 520f), new Vector2(180f, -330f),
                new Color(0.12f, 0.86f, 0.92f, 0.1f), glow: true);
            aurora2.GetComponent<Image>().raycastTarget = false;
            var d2 = aurora2.gameObject.AddComponent<DriftTween>();
            d2.amplitude = 22f; d2.speed = 0.52f;

            // 标题后方局部柔光（替代横跨全屏的光层）
            var titleHalo = UiBuilder.CreateRect(
                "TitleHalo", canvas.transform,
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(980f, 260f), new Vector2(0f, -180f),
                new Color(0.35f, 0.8f, 1f, 0.08f), glow: true);
            titleHalo.GetComponent<Image>().raycastTarget = false;

            // 中央吉祥物光晕（聚焦用）
            var centerGlow = UiBuilder.CreateRect(
                "CenterGlow", canvas.transform,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(1200f, 1200f), new Vector2(0f, 60f),
                new Color(0.35f, 0.75f, 1f, 0.08f), glow: true);
            centerGlow.GetComponent<Image>().raycastTarget = false;

            // 底部冰面光晕（降低 alpha，避免形成明显横条）
            var iceGlow = UiBuilder.CreateRect(
                "IceGlow", canvas.transform,
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(2200f, 1100f), new Vector2(0f, -70f),
                new Color(0.1f, 0.42f, 0.8f, 0.24f), glow: true);
            iceGlow.GetComponent<Image>().raycastTarget = false;

            // 地平线薄光
            var horizonGlow = UiBuilder.CreateRect(
                "HorizonGlow", canvas.transform,
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(1800f, 200f), new Vector2(0f, 255f),
                new Color(0.5f, 0.9f, 1f, 0.09f), glow: true);
            horizonGlow.GetComponent<Image>().raycastTarget = false;

            // ========== 星点 ==========
            CreateStarParticle(canvas.transform, new Vector2(0.06f, 0.88f), 5f, 0.85f);
            CreateStarParticle(canvas.transform, new Vector2(0.12f, 0.76f), 3.5f, 0.65f);
            CreateStarParticle(canvas.transform, new Vector2(0.20f, 0.94f), 4.5f, 0.8f);
            CreateStarParticle(canvas.transform, new Vector2(0.28f, 0.82f), 3f, 0.55f);
            CreateStarParticle(canvas.transform, new Vector2(0.35f, 0.96f), 4f, 0.7f);
            CreateStarParticle(canvas.transform, new Vector2(0.72f, 0.90f), 5f, 0.9f);
            CreateStarParticle(canvas.transform, new Vector2(0.80f, 0.78f), 3.5f, 0.6f);
            CreateStarParticle(canvas.transform, new Vector2(0.88f, 0.94f), 4.5f, 0.8f);
            CreateStarParticle(canvas.transform, new Vector2(0.94f, 0.72f), 3f, 0.5f);
            CreateStarParticle(canvas.transform, new Vector2(0.04f, 0.68f), 2.5f, 0.45f);
            CreateStarParticle(canvas.transform, new Vector2(0.96f, 0.66f), 2.5f, 0.45f);
            CreateStarParticle(canvas.transform, new Vector2(0.45f, 0.74f), 2f, 0.4f);
            CreateStarParticle(canvas.transform, new Vector2(0.55f, 0.86f), 2.5f, 0.5f);
            CreateStarParticle(canvas.transform, new Vector2(0.15f, 0.62f), 2f, 0.35f);
            CreateStarParticle(canvas.transform, new Vector2(0.85f, 0.58f), 2f, 0.35f);

            // ========== 四角暗角（柔和过渡） ==========
            CreateCornerVignette(canvas.transform, "VigBL", new Vector2(0f, 0f), new Vector2(-460f, -460f), 0.34f);
            CreateCornerVignette(canvas.transform, "VigBR", new Vector2(1f, 0f), new Vector2(460f, -460f), 0.34f);
            CreateCornerVignette(canvas.transform, "VigTL", new Vector2(0f, 1f), new Vector2(-460f, 460f), 0.22f);
            CreateCornerVignette(canvas.transform, "VigTR", new Vector2(1f, 1f), new Vector2(460f, 460f), 0.22f);

            // ========== 面板层 ==========
            panelLayer = new GameObject("PanelLayer", typeof(RectTransform)).transform;
            panelLayer.SetParent(canvas.transform, false);
            var prt = (RectTransform)panelLayer;
            prt.anchorMin = Vector2.zero;
            prt.anchorMax = Vector2.one;
            prt.sizeDelta = Vector2.zero;
            prt.anchoredPosition = Vector2.zero;
            panelRouter = new LobbyPanelRouter(panelLayer, OnPanelRouterChanged, BringLobbyUiToFront);
        }

        /// <summary>四角软暗角：使用大圆，alpha 自然衰减替代硬边矩形 vignette。</summary>
        private static void CreateCornerVignette(Transform parent, string name, Vector2 anchor, Vector2 anchoredPosition, float alpha)
        {
            var v = UiBuilder.CreateRect(name, parent,
                anchor, anchor,
                new Vector2(1500f, 1500f), anchoredPosition,
                new Color(0f, 0f, 0f, alpha), glow: true);
            v.GetComponent<Image>().raycastTarget = false;
        }

        private static void CreateStarParticle(Transform parent, Vector2 normalizedPos, float size, float intensity)
        {
            var star = UiBuilder.CreateRect("Star", parent,
                normalizedPos, normalizedPos,
                new Vector2(size, size), Vector2.zero,
                new Color(1f, 1f, 1f, 0.9f * intensity), glow: true);
            star.GetComponent<Image>().raycastTarget = false;
            var twinkle = star.gameObject.AddComponent<TwinkleTween>();
            twinkle.baseAlpha = 0.9f * intensity;
        }

        private class PulseTween : MonoBehaviour
        {
            public float amplitude = 0.022f;
            public float speed = 3f;
            private Vector3 baseScale;
            private void Start() => baseScale = transform.localScale;
            private void Update() => transform.localScale = baseScale * (1f + Mathf.Sin(Time.time * speed) * amplitude);
        }

        private class FloatTween : MonoBehaviour
        {
            public float amplitude = 10f;
            public float speed = 1.6f;
            private Vector3 basePos;
            private RectTransform rt;
            private void Start()
            {
                rt = GetComponent<RectTransform>();
                basePos = rt.anchoredPosition;
            }
            private void Update() => rt.anchoredPosition = basePos + new Vector3(0, Mathf.Sin(Time.time * speed) * amplitude, 0);
        }

        /// <summary>水平漂浮：用于极光等大气元素。</summary>
        private class DriftTween : MonoBehaviour
        {
            public float amplitude = 50f;
            public float speed = 0.4f;
            private Vector3 basePos;
            private RectTransform rt;
            private float phase;
            private void Start()
            {
                rt = GetComponent<RectTransform>();
                basePos = rt.anchoredPosition;
                phase = Random.Range(0f, Mathf.PI * 2f);
            }
            private void Update() => rt.anchoredPosition = basePos + new Vector3(
                Mathf.Sin(Time.time * speed + phase) * amplitude, 0f, 0f);
        }

        /// <summary>闪烁星点：用于装饰星星。</summary>
        private class TwinkleTween : MonoBehaviour
        {
            public float baseAlpha = 0.9f;
            public float speed = 2.5f;
            private Image img;
            private float phase;
            private void Start()
            {
                img = GetComponent<Image>();
                phase = Random.Range(0f, Mathf.PI * 2f);
                speed = Random.Range(1.6f, 3.4f);
            }
            private void Update()
            {
                if (img == null) return;
                var c = img.color;
                c.a = baseAlpha * (0.4f + 0.6f * Mathf.Abs(Mathf.Sin(Time.time * speed + phase)));
                img.color = c;
            }
        }

        /// <summary>点击缩放微动效。</summary>
        private class ClickScaleTween : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
        {
            public float scaleDown = 0.92f;
            public float speed = 18f;
            private Vector3 targetScale = Vector3.one;
            private Vector3 currentScale = Vector3.one;
            private RectTransform rt;
            private void Start() => rt = GetComponent<RectTransform>();
            private void Update()
            {
                if (rt == null) return;
                currentScale = Vector3.Lerp(currentScale, targetScale, Time.deltaTime * speed);
                rt.localScale = currentScale;
            }
            public void OnPointerDown(PointerEventData eventData) => targetScale = Vector3.one * scaleDown;
            public void OnPointerUp(PointerEventData eventData) => targetScale = Vector3.one;
        }

        private class RunCycleTween : MonoBehaviour
        {
            public RectTransform leftFlipper;
            public RectTransform rightFlipper;
            public RectTransform leftFoot;
            public RectTransform rightFoot;
            public RectTransform bodyGroup;
            public float speed = 8.5f;
            public float limbSwing = 7f;
            private Vector2 leftFootBase;
            private Vector2 rightFootBase;

            private void Start()
            {
                if (leftFoot != null) leftFootBase = leftFoot.anchoredPosition;
                if (rightFoot != null) rightFootBase = rightFoot.anchoredPosition;
            }

            private void Update()
            {
                var wave = Mathf.Sin(Time.time * speed);
                if (leftFlipper != null) leftFlipper.localRotation = Quaternion.Euler(0f, 0f, 10f + wave * limbSwing);
                if (rightFlipper != null) rightFlipper.localRotation = Quaternion.Euler(0f, 0f, -12f - wave * limbSwing);
                if (leftFoot != null)
                {
                    leftFoot.localRotation = Quaternion.Euler(0f, 0f, 8f + wave * 6f);
                    leftFoot.anchoredPosition = leftFootBase + new Vector2(0f, Mathf.Max(0f, wave) * 7f);
                }
                if (rightFoot != null)
                {
                    rightFoot.localRotation = Quaternion.Euler(0f, 0f, -8f - wave * 6f);
                    rightFoot.anchoredPosition = rightFootBase + new Vector2(0f, Mathf.Max(0f, -wave) * 7f);
                }
                if (bodyGroup != null)
                    bodyGroup.localRotation = Quaternion.Euler(0f, 0f, -2.5f + wave * 0.8f);
            }
        }

        private class RunwayScrollTween : MonoBehaviour
        {
            public float speed = 180f;
            public float resetLeft = -680f;
            public float resetRight = 680f;
            private RectTransform rt;

            private void Start() => rt = GetComponent<RectTransform>();

            private void Update()
            {
                if (rt == null) return;
                var p = rt.anchoredPosition;
                p.x -= speed * Time.deltaTime;
                if (p.x < resetLeft) p.x = resetRight + Random.Range(0f, 100f);
                rt.anchoredPosition = p;
            }
        }

        private class MarqueeTween : MonoBehaviour
        {
            public Text[] texts;
            private void Update()
            {
                if (texts == null) return;
                var active = Mathf.FloorToInt(Time.time * 4f) % texts.Length;
                for (var i = 0; i < texts.Length; i++)
                {
                    if (texts[i] == null) continue;
                    var c = texts[i].color;
                    c.a = i == active ? 0.95f : 0.28f;
                    texts[i].color = c;
                }
            }
        }

        private class BounceTween : MonoBehaviour
        {
            public float duration = 0.35f;
            public float peakScale = 1.18f;
            private float elapsed;

            private void OnEnable() => elapsed = 0f;

            private void Update()
            {
                elapsed += Time.deltaTime;
                var t = Mathf.Clamp01(elapsed / duration);
                var pulse = Mathf.Sin(t * Mathf.PI);
                transform.localScale = Vector3.one * (1f + pulse * (peakScale - 1f));
                if (t >= 1f)
                {
                    transform.localScale = Vector3.one;
                    enabled = false;
                }
            }
        }

        private class SnowfallEmitter : MonoBehaviour
        {
            private readonly System.Collections.Generic.List<RectTransform> flakes = new();
            private readonly System.Collections.Generic.List<float> phases = new();

            private void Start()
            {
                for (var i = 0; i < 10; i++)
                {
                    var size = Random.Range(4f, 8f);
                    var flake = UiBuilder.CreateRect("Snowflake", transform,
                        new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                        new Vector2(size, size),
                        new Vector2(Random.Range(-520f, 520f), Random.Range(-120f, 840f)),
                        new Color(1f, 1f, 1f, Random.Range(0.2f, 0.48f)), circle: true);
                    flake.GetComponent<Image>().raycastTarget = false;
                    flakes.Add(flake);
                    phases.Add(Random.Range(0f, Mathf.PI * 2f));
                }
            }

            private void Update()
            {
                for (var i = 0; i < flakes.Count; i++)
                {
                    var flake = flakes[i];
                    if (flake == null) continue;
                    var phase = phases[i];
                    var p = flake.anchoredPosition;
                    p.x += Mathf.Sin(Time.time * 1.4f + phase) * 18f * Time.deltaTime;
                    p.y -= (34f + i * 2.3f) * Time.deltaTime;
                    if (p.y < -360f)
                    {
                        p.x = Random.Range(-520f, 520f);
                        p.y = Random.Range(760f, 920f);
                    }
                    flake.anchoredPosition = p;
                    var img = flake.GetComponent<Image>();
                    var c = img.color;
                    c.a = 0.18f + Mathf.Abs(Mathf.Sin(Time.time * 1.8f + phase)) * 0.28f;
                    img.color = c;
                }
            }
        }

        private class SpeedLineEmitter : MonoBehaviour
        {
            private float nextSpawn;

            private void Update()
            {
                if (Time.time < nextSpawn) return;
                nextSpawn = Time.time + Random.Range(0.55f, 0.9f);
                var line = UiBuilder.CreateRect("SpeedLine", transform,
                    new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                    new Vector2(Random.Range(46f, 92f), Random.Range(2f, 4f)),
                    new Vector2(Random.Range(120f, 240f), Random.Range(-30f, 130f)),
                    new Color(0.7f, 1f, 1f, 0.38f), rounded: true);
                line.GetComponent<Image>().raycastTarget = false;
                var tween = line.gameObject.AddComponent<SpeedLineTween>();
                tween.life = Random.Range(0.34f, 0.52f);
                tween.travel = Random.Range(160f, 240f);
            }
        }

        private class SpeedLineTween : MonoBehaviour
        {
            public float life = 0.45f;
            public float travel = 260f;
            private RectTransform rt;
            private Image img;
            private Vector2 start;
            private float elapsed;

            private void Start()
            {
                rt = GetComponent<RectTransform>();
                img = GetComponent<Image>();
                start = rt.anchoredPosition;
            }

            private void Update()
            {
                elapsed += Time.deltaTime;
                var t = Mathf.Clamp01(elapsed / life);
                if (rt != null) rt.anchoredPosition = start + new Vector2(-travel * t, 0f);
                if (img != null)
                {
                    var c = img.color;
                    c.a = Mathf.Sin(t * Mathf.PI) * 0.38f;
                    img.color = c;
                }
                if (t >= 1f) Destroy(gameObject);
            }
        }

        /// <summary>底栏导航 Tab：图标 + 文字标签；选中时图标外圈光晕 + 顶部亮条 + 文字加粗。</summary>
        private static void DrawMiniPenguinAvatar(RectTransform parent)
        {
            for (var i = parent.childCount - 1; i >= 0; i--)
                UnityEngine.Object.Destroy(parent.GetChild(i).gameObject);

            var scarfPalette = CosmeticPalette.FindScarf(PlayerSave.SelectedScarfId);
            var hatPalette = CosmeticPalette.FindHat(PlayerSave.SelectedHatId);
            var head = UiBuilder.CreateRect("MiniHead", parent,
                new Vector2(0.17f, 0.18f), new Vector2(0.83f, 0.78f),
                Vector2.zero, Vector2.zero,
                new Color(0.09f, 0.16f, 0.3f), rounded: true);
            UiBuilder.CreateRect("MiniFace", head,
                new Vector2(0.2f, 0.15f), new Vector2(0.8f, 0.68f),
                Vector2.zero, Vector2.zero,
                new Color(0.96f, 0.99f, 1f), rounded: true);
            UiBuilder.CreateRect("MiniEyeL", head,
                new Vector2(0.33f, 0.42f), new Vector2(0.42f, 0.52f),
                Vector2.zero, Vector2.zero,
                new Color(0.1f, 0.35f, 0.8f), circle: true);
            UiBuilder.CreateRect("MiniEyeR", head,
                new Vector2(0.58f, 0.42f), new Vector2(0.67f, 0.52f),
                Vector2.zero, Vector2.zero,
                new Color(0.1f, 0.35f, 0.8f), circle: true);
            UiBuilder.CreateRect("MiniBeak", head,
                new Vector2(0.45f, 0.24f), new Vector2(0.56f, 0.35f),
                Vector2.zero, Vector2.zero,
                new Color(1f, 0.58f, 0.22f), rounded: true);
            UiBuilder.CreateRect("MiniScarf", parent,
                new Vector2(0.18f, 0.28f), new Vector2(0.84f, 0.38f),
                Vector2.zero, Vector2.zero,
                scarfPalette.Primary, rounded: true);
            UiBuilder.CreateRect("MiniHat", parent,
                new Vector2(0.19f, 0.73f), new Vector2(0.81f, 0.92f),
                Vector2.zero, Vector2.zero,
                hatPalette.Crown, rounded: true);
            UiBuilder.CreateRect("MiniHatBand", parent,
                new Vector2(0.21f, 0.68f), new Vector2(0.79f, 0.76f),
                Vector2.zero, Vector2.zero,
                hatPalette.Band, rounded: true);
            SetRaycastTargets(parent, false);
        }

        private static LobbyViewModel BuildLobbyViewModel()
        {
            var recommendation = GrowthRecommendation.Build();
            var claimableMissions = MissionStore.CountClaimableMissions();
            var companionHint = PlayerSave.CompanionDrorUnlocked
                ? "团团已整装，准备同行"
                : "再完成几次旅程，新的伙伴会来到营地";
            var activeCompanion = PlayerSave.CompanionDrorUnlocked ? CompanionId.Dror : CompanionId.TuanTuan;
            var bond = CompanionBondStore.Snapshot(activeCompanion);
            var camp = CampRebuildStore.Snapshot();
            var difficulty = DifficultyPreset.ForKind(PlayerSave.SelectedDifficulty);
            var chapterTitle = recommendation.Kind == GrowthRecommendationKind.StartJourney
                ? "雪松遗迹"
                : recommendation.Title;
            var chapterBody = recommendation.Kind == GrowthRecommendationKind.StartJourney
                ? "穿过低雾中的旧木桥，避开倒伏雪松和滚落石块。"
                : recommendation.Body;
            var journalReward = claimableMissions > 0
                ? $"可领取 {claimableMissions} 份任务奖励"
                : "今日奖励：鱼干 × 80";
            var routeReward = HasPlayedDailyToday()
                ? "伙伴手记 · 普通"
                : "今日首胜 · 双倍鱼干";
            return new LobbyViewModel(recommendation.Title, recommendation.Body,
                recommendation.ActionLabel, recommendation.Progress01,
                companionHint, PlayerSave.TotalFishSnacks, claimableMissions,
                bond.Level, bond.Progress01, bond.HelperLabel, camp.Progress01, recommendation.Kind,
                PlayerSave.PlayerLevel, CodexDiscoveryStore.CountDiscovered(), "极光季 · 第三章",
                journalReward, chapterTitle, chapterBody,
                $"{difficulty.DisplayName} · 得分 ×{difficulty.ScoreMultiplier:0.##}", routeReward);
        }

        private void RefreshStorybookLobby()
        {
            var model = BuildLobbyViewModel();
            var camp = CampRebuildStore.Snapshot();
            lobbyCampScene?.Bind(model, camp, PlayerSave.CompanionDrorUnlocked);
            lobbyTopRibbon?.Bind(model);
            lobbyJournalPage?.Bind(model, ExecuteLobbyPrimaryAction);
            lobbyChapterPage?.Bind(model, StartStory);
            lobbyDock?.SetBadges(MissionStore.CountClaimableMissions(), CampUpgrades.CountAffordableUpgrades());
            lobbyDock?.SetSelected(panelRouter?.CurrentPanelName);
        }

        private void ExecuteLobbyPrimaryAction()
        {
            switch (GrowthRecommendation.Build().Kind)
            {
                case GrowthRecommendationKind.ClaimGoal:
                    OpenMissions();
                    break;
                case GrowthRecommendationKind.ReviewDiscovery:
                    OpenCodex();
                    break;
                case GrowthRecommendationKind.RebuildCamp:
                case GrowthRecommendationKind.UpgradeCamp:
                    OpenCampShop();
                    break;
                default:
                    StartEndless();
                    break;
            }
        }

        private void RefreshHeader()
        {
            PlayerSave.EnsureTotalRunsSynced();

            if (nicknameText != null)
                nicknameText.text = PlayerSave.PlayerNickname;
            if (fishSnacksText != null)
                fishSnacksText.text = $"{PlayerSave.TotalFishSnacks:N0}";
            if (levelText != null)
                levelText.text = $"Lv.{PlayerSave.PlayerLevel}";

            if (dailyFishRewardChipText != null)
                dailyFishRewardChipText.text = "+50 鱼干";

            if (difficultyHintText != null)
            {
                var preset = DifficultyPreset.ForKind(PlayerSave.SelectedDifficulty);
                difficultyHintText.text =
                    $"当前难度：{preset.DisplayName} · 得分×{preset.ScoreMultiplier:0.##} · {preset.Tag}  ▾ 修改";
            }

            RefreshActivityTicker();
            UpdateDockBadges();
            RefreshStrategyCard();
            UpdateBoosterPreview();
            RefreshStorybookLobby();
        }

        private void RefreshActivityTicker()
        {
            if (topTickerText == null) return;

            if (!PlayerSave.IsDailyCheckInClaimed())
            {
                var reward = PlayerSave.PreviewDailyCheckInReward();
                topTickerText.text = $"今日签到可得 {reward} 鱼干 · 点击左侧「今日建议」领取";
                return;
            }

            var claimableMissionCount = MissionStore.CountClaimableMissions();
            if (claimableMissionCount > 0)
            {
                topTickerText.text = $"有 {claimableMissionCount} 个任务奖励待领取";
                return;
            }

            var affordableUpgrades = CampUpgrades.CountAffordableUpgrades();
            if (affordableUpgrades > 0)
            {
                topTickerText.text = $"营地 {affordableUpgrades} 项升级可购买 · 鱼干收益永久提升";
                return;
            }

            if (!HasPlayedDailyToday())
            {
                topTickerText.text = "今日挑战尚未开始 · 限定关卡 · 双倍鱼干";
                return;
            }

            var defeatedBosses = CodexUnlocks.BossesDefeatedCount();
            topTickerText.text =
                $"极夜赛季进行中 · 已击败 Boss {defeatedBosses}/{CodexUnlocks.BossEntryTotal}";
        }

        private static bool HasPlayedDailyToday()
        {
            var today = System.DateTime.Today;
            foreach (var r in RunRecords.GetRecent(20))
            {
                if (r.mode == "daily" && r.Timestamp.Date == today)
                    return true;
            }
            return false;
        }

        private void OpenProfileStats()
        {
            if (profileStatsOverlay != null) return;
            profileStatsOverlay = ProfileStatsPanel.Show(canvas.transform, new ProfileStatsPanel.HostCallbacks
            {
                OnClose = () => profileStatsOverlay = null,
                OnOpenLeaderboard = OpenLeaderboard,
                OnOpenCodex = OpenCodex,
                DrawMiniAvatar = DrawMiniPenguinAvatar,
            });
        }

        private void RefreshStrategyCard()
        {
            if (strategyHeadlineText == null || strategyDetailText == null || strategyButtonText == null) return;

            if (!PlayerSave.IsDailyCheckInClaimed())
            {
                var reward = PlayerSave.PreviewDailyCheckInReward();
                var streak = Mathf.Max(1, PlayerSave.LoginStreak);
                ApplyStrategyCard(
                    "领取今日签到",
                    $"连续签到第 {streak} 天，先拿 {reward} 鱼干，再去升级营地或挑战排行榜。",
                    "立即签到",
                    StrategyAction.CheckIn);
                return;
            }

            var claimableMissionCount = MissionStore.CountClaimableMissions();
            if (claimableMissionCount > 0)
            {
                ApplyStrategyCard(
                    "回收今日奖励",
                    $"你有 {claimableMissionCount} 个任务奖励待领取，先收鱼干，再决定是强化营地还是继续跑酷。",
                    "打开任务",
                    StrategyAction.OpenMissions);
                return;
            }

            var affordableUpgrades = CampUpgrades.CountAffordableUpgrades();
            if (affordableUpgrades > 0)
            {
                ApplyStrategyCard(
                    "强化常驻成长",
                    $"当前有 {affordableUpgrades} 项营地升级可购买，优先补强冲刺、磁吸和收益，会让后续每局更舒服。",
                    "前往营地",
                    StrategyAction.OpenCampShop);
                return;
            }

            var defeatedBosses = CodexUnlocks.BossesDefeatedCount();
            if (defeatedBosses < CodexUnlocks.BossEntryTotal)
            {
                ApplyStrategyCard(
                    "推进图鉴收集",
                    $"你已击败 {defeatedBosses}/{CodexUnlocks.BossEntryTotal} 个 Boss，再跑几局就能补全更多图鉴条目。",
                    "查看图鉴",
                    StrategyAction.OpenCodex);
                return;
            }

            var topEntries = LeaderboardStore.GetTopEntries(limit: 20);
            var playerId = PlayerSave.GetOrCreatePlayerId();
            var onBoard = false;
            foreach (var entry in topEntries)
            {
                if (entry.playerId == playerId)
                {
                    onBoard = true;
                    break;
                }
            }

            if (!onBoard)
            {
                ApplyStrategyCard(
                    "冲进排行榜",
                    "你还没有进入本地前 20，现在最适合开一局标准模式，把成绩先挂上榜单。",
                    "开始跑酷",
                    StrategyAction.StartEndless);
                return;
            }

            ApplyStrategyCard(
                "挑战每日限定",
                "当前基础成长已经很完整，去试试今日挑战，顺手把分数和图鉴进度再往上推一段。",
                "开始挑战",
                StrategyAction.StartDaily);
        }

        private void ApplyStrategyCard(string headline, string detail, string actionLabel, StrategyAction action)
        {
            strategyHeadlineText.text = headline;
            strategyDetailText.text = TruncateStrategyDetail(detail);
            strategyButtonText.text = actionLabel;
            currentStrategyAction = action;
        }

        private static string TruncateStrategyDetail(string detail)
        {
            if (string.IsNullOrEmpty(detail)) return detail;
            const int maxChars = 56;
            if (detail.Length <= maxChars) return detail;
            return detail.Substring(0, maxChars - 1) + "…";
        }

        private void ExecuteStrategyAction()
        {
            switch (currentStrategyAction)
            {
                case StrategyAction.CheckIn:
                    OnCheckInClicked();
                    break;
                case StrategyAction.OpenMissions:
                    OpenMissions();
                    break;
                case StrategyAction.OpenCampShop:
                    OpenCampShop();
                    break;
                case StrategyAction.OpenCodex:
                    OpenCodex();
                    break;
                case StrategyAction.OpenLeaderboard:
                    OpenLeaderboard();
                    break;
                case StrategyAction.StartDaily:
                    StartDaily();
                    break;
                default:
                    StartEndless();
                    break;
            }
        }

        private void OnCheckInClicked()
        {
            var reward = PlayerSave.ClaimDailyCheckIn();
            if (reward <= 0) return;
            PlayerSave.Flush();
            ShowCheckInCelebration(reward);
            RefreshHeader();
        }

        private void ShowCheckInCelebration(int reward)
        {
            var streak = PlayerSave.LoginStreak;
            var overlay = UiBuilder.CreatePanel("CheckInOverlay", canvas.transform,
                new Color(0f, 0f, 0f, 0.72f));
            modalStack.Register(overlay);
            overlay.transform.SetAsLastSibling();

            // 居中卡片
            var card = UiBuilder.CreateRect("Card", overlay.transform,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(600f, 520f), Vector2.zero,
                UiBuilder.Surface1, rounded: true);
            UiBuilder.AddOutline(card.gameObject, new Color(1f, 0.84f, 0.32f, 0.7f), new Vector2(2f, -2f));
            UiBuilder.AddShadow(card.gameObject, new Color(0f, 0f, 0f, 0.55f), new Vector2(0f, -10f));

            // 顶部金色辉光
            var topGlow = UiBuilder.CreateRect("TopGlow", card,
                new Vector2(0f, 1f), new Vector2(1f, 1f),
                new Vector2(0f, 180f), new Vector2(0f, -90f),
                new Color(1f, 0.75f, 0.2f, 0.2f), rounded: true);
            topGlow.GetComponent<Image>().raycastTarget = false;

            // 签到图标
            var iconCircle = UiBuilder.CreateRect("CheckIcon", card,
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(100f, 100f), new Vector2(0f, -40f),
                new Color(1f, 0.84f, 0.32f, 0.3f), circle: true);
            iconCircle.pivot = new Vector2(0.5f, 1f);
            UiBuilder.AddOutline(iconCircle.gameObject, new Color(1f, 0.84f, 0.32f, 0.6f), new Vector2(2f, -2f));
            UiBuilder.CreateText("IconText", iconCircle.transform, "☀", 48, FontStyle.Bold,
                TextAnchor.MiddleCenter, new Color(1f, 0.85f, 0.4f, 1f));

            // 标题
            UiBuilder.CreateText("Title", card, "签到成功",
                38, FontStyle.Bold, TextAnchor.MiddleCenter, Color.white);
            var trt = (RectTransform)card.Find("Title");
            trt.anchorMin = new Vector2(0f, 1f);
            trt.anchorMax = new Vector2(1f, 1f);
            trt.sizeDelta = new Vector2(0f, 50f);
            trt.anchoredPosition = new Vector2(0f, -150f);

            // 奖励数字（大号）
            var rewardText = UiBuilder.CreateText("Reward", card, $"+{reward} 鱼干",
                52, FontStyle.Bold, TextAnchor.MiddleCenter, UiBuilder.WarmGold);
            var rrt = (RectTransform)rewardText.transform;
            rrt.anchorMin = new Vector2(0f, 1f);
            rrt.anchorMax = new Vector2(1f, 1f);
            rrt.sizeDelta = new Vector2(0f, 64f);
            rrt.anchoredPosition = new Vector2(0f, -210f);

            // 连续签到信息
            var streakText = UiBuilder.CreateText("Streak", card,
                $"连续签到第 {streak} 天 · 明天可得 {Mathf.Min(50, 10 + (streak % 7) * 5)} 鱼干",
                20, FontStyle.Normal, TextAnchor.MiddleCenter, UiBuilder.TextSecondary);
            var srt = (RectTransform)streakText.transform;
            srt.anchorMin = new Vector2(0f, 1f);
            srt.anchorMax = new Vector2(1f, 1f);
            srt.sizeDelta = new Vector2(0f, 32f);
            srt.anchoredPosition = new Vector2(0f, -280f);

            // 连续天数小圆点
            var dotsRow = UiBuilder.CreateRect("DotsRow", card,
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(300f, 24f), new Vector2(0f, -325f),
                new Color(0f, 0f, 0f, 0f));
            for (var i = 0; i < 7; i++)
            {
                var filled = i < streak % 7 || (streak % 7 == 0 && i < 7);
                var dotColor = filled
                    ? new Color(1f, 0.84f, 0.32f, 1f)
                    : new Color(0.15f, 0.25f, 0.4f, 1f);
                var dot = UiBuilder.CreateRect($"Dot{i}", dotsRow,
                    new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                    new Vector2(18f, 18f), new Vector2((i - 3) * 32f, 0f),
                    dotColor, circle: true);
                dot.GetComponent<Image>().raycastTarget = false;
                if (filled) UiBuilder.AddOutline(dot.gameObject, new Color(1f, 1f, 1f, 0.4f), new Vector2(1f, -1f));
            }

            // 确认按钮
            var okBtn = UiBuilder.CreateButton("OK", card, "好的",
                new Color(1f, 0.84f, 0.32f, 1f), new Color(0.08f, 0.05f, 0.01f, 1f), 28, rounded: true);
            var obrt = (RectTransform)okBtn.transform;
            obrt.anchorMin = new Vector2(0.5f, 0f);
            obrt.anchorMax = new Vector2(0.5f, 0f);
            obrt.pivot = new Vector2(0.5f, 0f);
            obrt.sizeDelta = new Vector2(340f, 72f);
            obrt.anchoredPosition = new Vector2(0f, 36f);
            UiBuilder.AddOutline(okBtn.gameObject, new Color(0.5f, 0.35f, 0.08f, 0.7f), new Vector2(2f, -2f));
            UiBuilder.AddShadow(okBtn.gameObject, new Color(0f, 0f, 0f, 0.45f), new Vector2(0f, -5f));
            okBtn.onClick.AddListener(() => DismissLobbyModal(overlay));
        }

        private void UpdateDockBadges()
        {
            foreach (var item in dockItems)
            {
                if (item.badgeRoot == null || item.badgeCountText == null) continue;
                var n = item.panelName switch
                {
                    "Missions" => MissionStore.CountClaimableMissions(),
                    "CampShop" => CampUpgrades.CountAffordableUpgrades(),
                    _ => 0,
                };
                item.badgeRoot.SetActive(n > 0);
                item.badgeCountText.text = n > 99 ? "99+" : n.ToString();
            }
        }

        /// <summary>下一局即将自动消耗的道具数量预览，挂在主 CTA 下方。</summary>
        private void UpdateBoosterPreview()
        {
            if (boosterPreviewText == null) return;
            var df = ShopStore.InventoryCount(ShopItemKind.BoosterDoubleFish);
            var sb = ShopStore.InventoryCount(ShopItemKind.BoosterScoreBoost);
            var ls = ShopStore.InventoryCount(ShopItemKind.BoosterLuckyStart);
            if (df <= 0 && sb <= 0 && ls <= 0)
            {
                boosterPreviewText.transform.parent.gameObject.SetActive(false);
                return;
            }
            boosterPreviewText.transform.parent.gameObject.SetActive(true);
            var parts = new System.Collections.Generic.List<string>();
            if (df > 0) parts.Add("鱼干 ×2");
            if (sb > 0) parts.Add("得分 ×1.5");
            if (ls > 0) parts.Add("开局 +1 命");
            boosterPreviewText.text = "下一局：" + string.Join(" · ", parts);
        }

        /// <summary>装扮变化后重绘吉祥物。</summary>
        /// <summary>装饰 UI 不应拦截点击；统一关闭整棵子树的 raycast。</summary>
        private static void SetRaycastTargets(Transform root, bool enabled)
        {
            if (root == null) return;
            var graphics = root.GetComponentsInChildren<Graphic>(true);
            foreach (var g in graphics)
            {
                if (g != null)
                    g.raycastTarget = enabled;
            }
        }

        // ── 难度选择 ─────────────────────────────────────────────────────────────
        private DifficultyKind pendingDifficulty = DifficultyKind.Normal;
        private DifficultyOverlayMode difficultyOverlayMode = DifficultyOverlayMode.BeforeEndlessRun;
        private GameObject difficultyOverlay;

        private void StartEndless()
        {
            difficultyOverlayMode = DifficultyOverlayMode.BeforeEndlessRun;
            pendingDifficulty = PlayerSave.SelectedDifficulty;
            ShowDifficultyOverlay(difficultyOverlayMode);
        }

        private void StartStory()
        {
            difficultyOverlayMode = DifficultyOverlayMode.BeforeStoryRun;
            pendingDifficulty = PlayerSave.SelectedDifficulty;
            ShowDifficultyOverlay(difficultyOverlayMode);
        }

        private void StartDaily()
        {
            difficultyOverlayMode = DifficultyOverlayMode.BeforeDailyRun;
            pendingDifficulty = PlayerSave.SelectedDifficulty;
            ShowDifficultyOverlay(difficultyOverlayMode);
        }

        private void ShowDifficultyOverlay(DifficultyOverlayMode mode)
        {
            difficultyOverlayMode = mode;
            if (difficultyOverlay != null) DismissLobbyModal(difficultyOverlay);

            // 半透明遮罩
            var overlay = UiBuilder.CreatePanel("DifficultyOverlay", canvas.transform,
                new Color(0f, 0f, 0f, 0.72f));
            difficultyOverlay = overlay;
            modalStack.Register(overlay, () =>
            {
                if (difficultyOverlay == overlay) difficultyOverlay = null;
            });
            overlay.transform.SetAsLastSibling();

            // 居中卡片
            var card = UiBuilder.CreateRect("Card", overlay.transform,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(900f, 960f), Vector2.zero,
                UiBuilder.Surface1, rounded: true);
            UiBuilder.AddOutline(card.gameObject, UiBuilder.BorderDefault, new Vector2(1.5f, -1.5f));
            UiBuilder.AddShadow(card.gameObject, new Color(0f, 0f, 0f, 0.55f), new Vector2(0f, -12f));

            // 顶部青色高光线
            var topLine = UiBuilder.CreateRect("TopLine", card,
                new Vector2(0f, 1f), new Vector2(1f, 1f),
                new Vector2(0f, 3.5f), new Vector2(0f, -2f),
                new Color(0f, 0.85f, 0.95f, 0.65f), rounded: true);
            topLine.GetComponent<Image>().raycastTarget = false;

            var overlayTitle = mode switch
            {
                DifficultyOverlayMode.BeforeStoryRun => "继续章节 · 选择难度",
                DifficultyOverlayMode.BeforeDailyRun => "今日挑战 · 选择难度",
                DifficultyOverlayMode.BeforeEndlessRun => "极夜快跑 · 选择难度",
                _ => "选择难度",
            };

            // 标题
            var titleText = UiBuilder.CreateText("Title", card, overlayTitle, 44, FontStyle.Bold,
                TextAnchor.MiddleCenter, Color.white);
            var trt = (RectTransform)titleText.transform;
            trt.anchorMin = new Vector2(0f, 1f);
            trt.anchorMax = new Vector2(1f, 1f);
            trt.sizeDelta = new Vector2(0f, 80f);
            trt.anchoredPosition = new Vector2(0f, -52f);
            trt.pivot = new Vector2(0.5f, 1f);
            UiBuilder.AddShadow(titleText.gameObject, new Color(0f, 0.05f, 0.15f, 0.6f), new Vector2(0f, -4f));

            var subText = UiBuilder.CreateText("Sub", card, "难度影响速度、加速度和 Boss 出现时机", 22, FontStyle.Normal,
                TextAnchor.MiddleCenter, UiBuilder.TextSecondary);
            var srt = (RectTransform)subText.transform;
            srt.anchorMin = new Vector2(0f, 1f);
            srt.anchorMax = new Vector2(1f, 1f);
            srt.sizeDelta = new Vector2(0f, 40f);
            srt.anchoredPosition = new Vector2(0f, -110f);
            srt.pivot = new Vector2(0.5f, 1f);

            // 四个难度按钮
            var presets = DifficultyPreset.All;
            var diffColors = new[]
            {
                new Color(0.3f, 0.85f, 0.55f, 1f),   // Easy - 绿色
                new Color(0.3f, 0.75f, 1f, 1f),       // Normal - 青蓝
                new Color(1f, 0.72f, 0.28f, 1f),      // Hard - 橙色
                new Color(1f, 0.35f, 0.45f, 1f),      // Expert - 红色
            };
            var diffBtnRefs = new Image[4];
            var diffBtnHighlight = new Image[4];

            for (int i = 0; i < presets.Length; i++)
            {
                var preset = presets[i];
                var accentColor = diffColors[i];
                var yPos = -175f - i * 168f;

                // 难度行容器
                var rowBg = UiBuilder.CreateRect($"DiffRow_{i}", card,
                    new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                    new Vector2(820f, 148f), new Vector2(0f, yPos),
                    UiBuilder.Surface2, rounded: true);
                rowBg.pivot = new Vector2(0.5f, 1f);
                UiBuilder.AddOutline(rowBg.gameObject, new Color(accentColor.r, accentColor.g, accentColor.b, 0.25f),
                    new Vector2(1.5f, -1.5f));
                diffBtnRefs[i] = rowBg.GetComponent<Image>();

                // 左侧颜色标签条
                var accentBar = UiBuilder.CreateRect("AccentBar", rowBg,
                    new Vector2(0f, 0f), new Vector2(0f, 1f),
                    new Vector2(6f, 0f), new Vector2(3f, 0f),
                    accentColor, rounded: true);
                accentBar.GetComponent<Image>().raycastTarget = false;

                // 难度名
                var nameText = UiBuilder.CreateText("Name", rowBg, preset.DisplayName, 32, FontStyle.Bold,
                    TextAnchor.MiddleLeft, accentColor);
                var nrt = (RectTransform)nameText.transform;
                nrt.anchorMin = new Vector2(0f, 0.5f);
                nrt.anchorMax = new Vector2(1f, 1f);
                nrt.offsetMin = new Vector2(28f, 0f);
                nrt.offsetMax = new Vector2(-20f, -4f);

                // 标签芯片
                var tagChip = UiBuilder.CreateRect("Tag", rowBg,
                    new Vector2(1f, 1f), new Vector2(1f, 1f),
                    new Vector2(110f, 34f), new Vector2(-20f, -18f),
                    new Color(accentColor.r, accentColor.g, accentColor.b, 0.2f), rounded: true);
                tagChip.pivot = new Vector2(1f, 1f);
                UiBuilder.AddOutline(tagChip.gameObject, new Color(accentColor.r, accentColor.g, accentColor.b, 0.5f),
                    new Vector2(1f, -1f));
                tagChip.GetComponent<Image>().raycastTarget = false;
                UiBuilder.CreateText("TagText", tagChip, preset.Tag, 16, FontStyle.Bold,
                    TextAnchor.MiddleCenter, accentColor);

                // 描述
                var descText = UiBuilder.CreateText("Desc", rowBg, preset.Description, 20, FontStyle.Normal,
                    TextAnchor.UpperLeft, UiBuilder.TextSecondary);
                var drt = (RectTransform)descText.transform;
                drt.anchorMin = new Vector2(0f, 0f);
                drt.anchorMax = new Vector2(1f, 0.5f);
                drt.offsetMin = new Vector2(28f, 8f);
                drt.offsetMax = new Vector2(-20f, 0f);

                // 速度 + Boss 提示
                var speedStr = $"初速 {preset.StartSpeed:F0}  →  最高 {preset.MaxSpeed:F0} m/s   |   Boss @{preset.FirstBossDistance:F0}m";
                var statText = UiBuilder.CreateText("Stat", rowBg, speedStr, 17, FontStyle.Normal,
                    TextAnchor.LowerLeft, UiBuilder.TextTertiary);
                var strt = (RectTransform)statText.transform;
                strt.anchorMin = new Vector2(0f, 0f);
                strt.anchorMax = new Vector2(1f, 0.5f);
                strt.offsetMin = new Vector2(28f, -4f);
                strt.offsetMax = new Vector2(-20f, 4f);

                // 选中高亮框
                var hlImg = UiBuilder.CreateRect("Highlight", rowBg,
                    new Vector2(0f, 0f), new Vector2(1f, 1f),
                    Vector2.zero, Vector2.zero,
                    new Color(accentColor.r, accentColor.g, accentColor.b, 0.14f), rounded: true);
                hlImg.GetComponent<Image>().raycastTarget = false;
                diffBtnHighlight[i] = hlImg.GetComponent<Image>();

                // 可点击区域（Button 覆盖整行）
                var idx = i;
                var btn = rowBg.gameObject.AddComponent<Button>();
                btn.targetGraphic = rowBg.GetComponent<Image>();
                var btnColors = btn.colors;
                btnColors.normalColor = UiBuilder.Surface2;
                btnColors.highlightedColor = Color.Lerp(UiBuilder.Surface2, accentColor, 0.12f);
                btnColors.pressedColor = Color.Lerp(UiBuilder.Surface2, accentColor, 0.22f);
                btn.colors = btnColors;
                btn.onClick.AddListener(() =>
                {
                    pendingDifficulty = preset.Kind;
                    PlayerSave.SelectedDifficulty = preset.Kind;
                    RefreshDifficultyHighlights(diffBtnHighlight, diffBtnRefs, diffColors, idx);
                });
            }

            // 初始化高亮
            var initialIdx = (int)pendingDifficulty;
            RefreshDifficultyHighlights(diffBtnHighlight, diffBtnRefs, diffColors, initialIdx);

            // 底部按钮行
            var cancelBtn = UiBuilder.CreateButton("Cancel", card, "取消",
                UiBuilder.Surface2, UiBuilder.TextSecondary, 28, rounded: true);
            var crt2 = (RectTransform)cancelBtn.transform;
            crt2.anchorMin = new Vector2(0f, 0f);
            crt2.anchorMax = new Vector2(0f, 0f);
            crt2.pivot = new Vector2(0f, 0f);
            crt2.sizeDelta = new Vector2(300f, 88f);
            crt2.anchoredPosition = new Vector2(36f, 36f);
            UiBuilder.AddOutline(cancelBtn.gameObject, UiBuilder.BorderDefault, new Vector2(1f, -1f));
            cancelBtn.onClick.AddListener(() =>
            {
                DismissLobbyModal(difficultyOverlay);
            });

            var confirmLabel = mode == DifficultyOverlayMode.SettingsOnly ? "确认" : "开始挑战";
            var startFinalBtn = UiBuilder.CreateButton("StartFinal", card, confirmLabel,
                new Color(1f, 0.84f, 0.32f), new Color(0.08f, 0.05f, 0.01f), 32, rounded: true);
            var sfrt = (RectTransform)startFinalBtn.transform;
            sfrt.anchorMin = new Vector2(1f, 0f);
            sfrt.anchorMax = new Vector2(1f, 0f);
            sfrt.pivot = new Vector2(1f, 0f);
            sfrt.sizeDelta = new Vector2(460f, 88f);
            sfrt.anchoredPosition = new Vector2(-36f, 36f);
            UiBuilder.AddOutline(startFinalBtn.gameObject, new Color(0.5f, 0.35f, 0.08f, 0.7f), new Vector2(2f, -2f));
            UiBuilder.AddShadow(startFinalBtn.gameObject, new Color(0f, 0f, 0f, 0.45f), new Vector2(0f, -6f));
            var startLabel2 = startFinalBtn.GetComponentInChildren<Text>();
            if (startLabel2 != null) startLabel2.supportRichText = false;
            startFinalBtn.onClick.AddListener(() =>
            {
                var startsRun = mode != DifficultyOverlayMode.SettingsOnly;
                if (startsRun && !runLaunchGate.TryBegin()) return;
                startFinalBtn.interactable = false;
                PlayerSave.SelectedDifficulty = pendingDifficulty;
                PlayerSave.Flush();

                if (mode == DifficultyOverlayMode.SettingsOnly)
                {
                    DismissLobbyModal(difficultyOverlay);
                    RefreshHeader();
                    return;
                }

                if (mode == DifficultyOverlayMode.BeforeDailyRun)
                {
                    if (!RunSession.TryPrepareDailyRun(
                            System.DateTime.UtcNow,
                            pendingDifficulty,
                            out var error))
                    {
                        runLaunchGate.Reset();
                        startFinalBtn.interactable = true;
                        OnPanelEvent(PanelEvent.Toast, error);
                        return;
                    }
                }
                else if (mode == DifficultyOverlayMode.BeforeStoryRun)
                    RunSession.PrepareStoryRun(RunSession.NextRunTheme, pendingDifficulty);
                else
                    RunSession.PrepareEndlessRun(difficulty: pendingDifficulty);
                DismissLobbyModal(difficultyOverlay);
                RefreshHeader();
                SceneTransitionManager.Instance.TransitionTo("PenguinRunner");
            });
        }

        private void DismissLobbyModal(GameObject overlay)
        {
            if (overlay == null) return;
            modalStack.Dismiss(overlay);
            if (difficultyOverlay == overlay) difficultyOverlay = null;
        }

        private static void RefreshDifficultyHighlights(Image[] highlights, Image[] rowBgs, Color[] colors, int selectedIdx)
        {
            for (int i = 0; i < highlights.Length; i++)
            {
                if (highlights[i] == null) continue;
                var selected = i == selectedIdx;
                highlights[i].color = selected
                    ? new Color(colors[i].r, colors[i].g, colors[i].b, 0.16f)
                    : new Color(0f, 0f, 0f, 0f);
                if (rowBgs[i] != null)
                {
                    rowBgs[i].color = selected
                        ? Color.Lerp(UiBuilder.Surface2, colors[i], 0.1f)
                        : UiBuilder.Surface2;
                }
            }
        }

        private void OpenMissions() => OpenPanel(MissionPanel.Build(panelLayer, OnPanelEvent), "Missions");
        private void OpenLeaderboard() => OpenPanel(LeaderboardPanel.Build(panelLayer, OnPanelEvent), "Leaderboard");

        /// <summary>统一入口：营地+商店+跑酷记录三合一面板。</summary>
        private void OpenCampShop()
        {
            if (!PlayerSave.VisitedCamp)
            {
                PlayerSave.VisitedCamp = true;
                PlayerSave.Flush();
            }
            if (!PlayerSave.ShopVisited)
            {
                PlayerSave.ShopVisited = true;
                PlayerSave.Flush();
            }
            OpenPanel(CampShopPanel.Build(panelLayer, OnPanelEvent), "CampShop");
        }

        private void OpenCodex() => OpenPanel(CodexPanel.Build(panelLayer, OnPanelEvent), "Codex");
        private void OpenSettings() => OpenPanel(SettingsPanel.Build(panelLayer, OnPanelEvent), "Settings");

        private void OpenPanel(GameObject go, string panelName) => panelRouter.Open(go, panelName);

        private void ClosePanel() => panelRouter.Close();

        private void OnPanelRouterChanged()
        {
            SetLobbySurfaceVisible(panelRouter == null || !panelRouter.HasOpenPanel);
            RefreshHeader();
            RefreshDockState();
            RefreshStorybookLobby();
            BringLobbyUiToFront();
        }

        private void SetLobbySurfaceVisible(bool visible)
        {
            if (lobbyCampScene?.Root != null) lobbyCampScene.Root.gameObject.SetActive(true);
            lobbyCampScene?.SetPanelOpen(!visible);
            lobbyCampScene?.SetInputEnabled(visible);
            if (lobbyTopRibbon?.Root != null) lobbyTopRibbon.Root.gameObject.SetActive(visible);
            if (lobbyJournalPage?.Root != null) lobbyJournalPage.Root.gameObject.SetActive(visible);
            if (lobbyChapterPage?.Root != null) lobbyChapterPage.Root.gameObject.SetActive(visible);
            if (dockWrap != null) dockWrap.gameObject.SetActive(visible);
            if (versionLabel != null) versionLabel.gameObject.SetActive(visible);
        }

        private void BringLobbyUiToFront()
        {
            if (panelLayer != null) panelLayer.SetAsLastSibling();
            if (dockWrap != null && (panelRouter == null || !panelRouter.HasOpenPanel))
                dockWrap.SetAsLastSibling();
            if (snackbar != null) snackbar.transform.SetAsLastSibling();
        }

        private void RefreshDockState()
        {
            lobbyDock?.SetSelected(panelRouter?.CurrentPanelName);
            foreach (var item in dockItems)
            {
                bool isSelected = item.panelName == panelRouter?.CurrentPanelName;

                // 顶部指示条
                if (item.activeLine != null)
                    item.activeLine.gameObject.SetActive(isSelected);

                if (item.iconGlow != null)
                    item.iconGlow.SetActive(isSelected);

                // 圆形图标背景
                if (item.iconBg != null)
                {
                    item.iconBg.color = isSelected
                        ? new Color(item.accentColor.r, item.accentColor.g, item.accentColor.b, 0.92f)
                        : new Color(0.12f, 0.24f, 0.42f, 0.85f);

                    if (isSelected && !item.wasSelected)
                    {
                        var bounce = item.iconBg.GetComponent<BounceTween>();
                        if (bounce != null)
                            bounce.enabled = true;
                    }
                }

                // 图标符号颜色
                if (item.iconText != null)
                    item.iconText.color = isSelected
                        ? UiBuilder.TextOnAccent
                        : Color.Lerp(item.accentColor, Color.white, 0.45f);

                // 文字标签颜色 + 粗细
                if (item.labelText != null)
                {
                    item.labelText.color = isSelected
                        ? item.accentColor
                        : new Color(0.6f, 0.72f, 0.85f, 0.8f);
                    item.labelText.fontStyle = isSelected ? FontStyle.Bold : FontStyle.Normal;
                    item.labelText.fontSize = isSelected ? 20 : 19;
                }

                item.wasSelected = isSelected;
            }
        }

        private void OnPanelEvent(PanelEvent e, string message = null)
        {
            switch (e)
            {
                case PanelEvent.Close:
                    ClosePanel();
                    break;
                case PanelEvent.RefreshHeader:
                    RefreshHeader();
                    break;
                case PanelEvent.Toast:
                    if (!string.IsNullOrEmpty(message)) snackbar.Show(message);
                    RefreshHeader();
                    break;
            }
        }

        private void ShowResultOverlayIfAny()
        {
            var r = RunSession.LastResult;
            if (r == null) return;
            RunResultOverlay.Show(canvas.transform, r, () => { RunSession.ConsumeLastResult(); RefreshHeader(); });
        }

        public enum PanelEvent
        {
            Close,
            RefreshHeader,
            Toast,
        }
    }
}
