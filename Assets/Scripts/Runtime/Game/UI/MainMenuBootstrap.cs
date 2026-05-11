using PenguinRun.Game.Mission;
using PenguinRun.Game.Save;
using PenguinRun.Game.Shop;
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
        private Text bestScoreText;
        private Text fishSnacksText;
        private GameObject missionBadge;
        private RectTransform mascotArea;
        private Text boosterPreviewText;

        private Transform panelLayer;
        private GameObject currentPanel;
        private string currentPanelName;
        private RectTransform dockWrap;

        private class DockItem
        {
            public string panelName;
            public Image iconBg;
            public Text iconText;
            public Text labelText;
            public Image labelBg;
            public Image activeLine;
        }
        private readonly System.Collections.Generic.List<DockItem> dockItems = new();

        private void Awake()
        {
            EnsureEventSystem();
            BuildScene();
            var lobbyGo = new GameObject("MenuLobbyAudio");
            lobbyGo.transform.SetParent(transform, false);
            lobbyGo.AddComponent<MenuLobbyAudio>();
            ShowResultOverlayIfAny();
        }

        private void OnEnable()
        {
            RefreshHeader();
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                if (currentPanel != null)
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
            BuildTopBar();
            BuildHero();
            BuildBottomDock();
            // 全屏面板必须在顶栏/主菜单之上，否则任务头等控件会与首页重叠且无法正确覆盖
            panelLayer.SetAsLastSibling();
            snackbar = Snackbar.EnsureUnder(canvas);
            snackbar.transform.SetAsLastSibling();
        }

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

        private void BuildTopBar()
        {
            // 左侧玩家信息面板（自适应宽度，防止文字溢出）
            var playerProfile = UiBuilder.CreateRect("PlayerProfile", canvas.transform,
                new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(380f, 100f), new Vector2(24f, -24f),
                new Color(0.08f, 0.16f, 0.28f, 0.9f), rounded: true);
            playerProfile.pivot = new Vector2(0f, 1f);
            UiBuilder.AddOutline(playerProfile.gameObject, new Color(0.4f, 0.85f, 0.95f, 0.4f), new Vector2(1.5f, -1.5f));
            UiBuilder.AddShadow(playerProfile.gameObject, new Color(0f, 0f, 0f, 0.3f), new Vector2(0f, -4f));

            // Avatar（稍微缩小）
            var avatarBg = UiBuilder.CreateRect("AvatarBg", playerProfile,
                new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
                new Vector2(72f, 72f), new Vector2(44f, 0f),
                new Color(0.12f, 0.45f, 0.75f, 0.95f), circle: true);
            UiBuilder.AddOutline(avatarBg.gameObject, new Color(1f, 1f, 1f, 0.45f), new Vector2(1.5f, -1.5f));

            UiBuilder.CreateRect("AvatarInner", avatarBg,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(60f, 60f), Vector2.zero,
                new Color(0.06f, 0.2f, 0.38f, 0.98f), circle: true);
            UiBuilder.CreateText("AvatarText", avatarBg, "企", 26, FontStyle.Bold, TextAnchor.MiddleCenter, new Color(1f, 1f, 1f, 1f));

            // Player name（留足边距防止溢出）
            nicknameText = UiBuilder.CreateText("Nickname", playerProfile, "", 28, FontStyle.Bold, TextAnchor.LowerLeft, new Color(1f, 1f, 1f, 0.95f));
            var nrt = (RectTransform)nicknameText.transform;
            nrt.anchorMin = new Vector2(0f, 0.5f);
            nrt.anchorMax = new Vector2(1f, 1f);
            nrt.offsetMin = new Vector2(100f, 2f);
            nrt.offsetMax = new Vector2(-12f, -2f);

            bestScoreText = UiBuilder.CreateText("BestScore", playerProfile, "", 18, FontStyle.Normal, TextAnchor.UpperLeft, new Color(0.75f, 0.85f, 0.95f, 0.75f));
            var brt = (RectTransform)bestScoreText.transform;
            brt.anchorMin = new Vector2(0f, 0f);
            brt.anchorMax = new Vector2(1f, 0.5f);
            brt.offsetMin = new Vector2(100f, 0f);
            brt.offsetMax = new Vector2(-12f, 2f);

            // 右侧货币胶囊（固定位置，确保不溢出屏幕）
            var currencyPill = UiBuilder.CreateRect("CurrencyPill", canvas.transform,
                new Vector2(1f, 1f), new Vector2(1f, 1f),
                new Vector2(190f, 64f), new Vector2(-24f, -24f),
                new Color(0.1f, 0.2f, 0.35f, 0.9f), rounded: true);
            currencyPill.pivot = new Vector2(1f, 1f);
            UiBuilder.AddOutline(currencyPill.gameObject, new Color(1f, 0.75f, 0.3f, 0.5f), new Vector2(1.5f, -1.5f));
            UiBuilder.AddShadow(currencyPill.gameObject, new Color(0f, 0f, 0f, 0.3f), new Vector2(0f, -3f));

            var fishIcon = UiBuilder.CreateRect("FishIcon", currencyPill,
                new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
                new Vector2(42f, 42f), new Vector2(28f, 0f),
                new Color(1f, 0.82f, 0.35f, 1f), circle: true);
            UiBuilder.AddOutline(fishIcon.gameObject, new Color(0.3f, 0.15f, 0.05f, 0.55f), new Vector2(1f, -1f));
            UiBuilder.CreateText("IconText", fishIcon, "鱼", 17, FontStyle.Bold, TextAnchor.MiddleCenter, new Color(0.15f, 0.08f, 0.02f, 1f));

            fishSnacksText = UiBuilder.CreateText("FishAmount", currencyPill, "0", 24, FontStyle.Bold, TextAnchor.MiddleCenter, new Color(1f, 0.85f, 0.4f, 1f));
            var frt = (RectTransform)fishSnacksText.transform;
            frt.anchorMin = new Vector2(0f, 0f);
            frt.anchorMax = new Vector2(1f, 1f);
            frt.offsetMin = new Vector2(58f, 0f);
            frt.offsetMax = new Vector2(-12f, 0f);
        }

        private void BuildHero()
        {
            // Title — separated to avoid rich text shadow artifacts
            var titleStr = "企鹅快跑";
            var title = UiBuilder.CreateText(
                "MainTitle", canvas.transform,
                titleStr,
                86, FontStyle.Bold, TextAnchor.MiddleCenter, new Color(1f, 1f, 1f, 1f));
            var trt = (RectTransform)title.transform;
            trt.anchorMin = new Vector2(0.5f, 1f);
            trt.anchorMax = new Vector2(0.5f, 1f);
            trt.sizeDelta = new Vector2(840f, 170f);
            trt.anchoredPosition = new Vector2(0f, -130f);

            UiBuilder.AddOutline(title.gameObject, new Color(0.1f, 0.4f, 0.85f, 0.55f), new Vector2(2f, -2f));
            UiBuilder.AddShadow(title.gameObject, new Color(0f, 0.05f, 0.15f, 0.7f), new Vector2(0f, -8f));

            // 标题下的青色细分隔线（左右两段 + 中央菱形点缀）→ 让标题与副标之间有「明确的设计意图」
            // 替代之前被极光矩形误当成框的视觉错觉。细线用纯色精灵避免 9-slice 边缘异常。
            var titleDividerLeft = UiBuilder.CreateRect("TitleDividerL", canvas.transform,
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(120f, 2f), new Vector2(-100f, -204f),
                new Color(0f, 0.85f, 0.95f, 0.55f));
            titleDividerLeft.GetComponent<Image>().raycastTarget = false;

            var titleDividerRight = UiBuilder.CreateRect("TitleDividerR", canvas.transform,
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(120f, 2f), new Vector2(100f, -204f),
                new Color(0f, 0.85f, 0.95f, 0.55f));
            titleDividerRight.GetComponent<Image>().raycastTarget = false;

            var titleDot = UiBuilder.CreateRect("TitleDot", canvas.transform,
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(10f, 10f), new Vector2(0f, -204f),
                new Color(0.4f, 0.95f, 1f, 0.9f), circle: true);
            titleDot.GetComponent<Image>().raycastTarget = false;

            // 副标题：UGUI Text 无字距 API，用全角空格手动撑开做出 Logo lockup 风格
            var subTitle = UiBuilder.CreateText(
                "SubTitle", canvas.transform,
                "P E N G U I N   R U N",
                24, FontStyle.Bold, TextAnchor.UpperCenter, new Color(0.65f, 0.88f, 1f, 0.95f));
            var srt = (RectTransform)subTitle.transform;
            srt.anchorMin = new Vector2(0.5f, 1f);
            srt.anchorMax = new Vector2(0.5f, 1f);
            srt.sizeDelta = new Vector2(800f, 48f);
            srt.anchoredPosition = new Vector2(0f, -278f);
            UiBuilder.AddShadow(subTitle.gameObject, new Color(0f, 0.05f, 0.15f, 0.6f), new Vector2(0f, -3f));

            // Enhanced mascot environment (moved up slightly)
            var mascotGround = UiBuilder.CreateRect(
                "MascotGround", canvas.transform,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(320f, 48f), new Vector2(0f, -70f),
                new Color(0.03f, 0.12f, 0.22f, 0.4f), rounded: true);
            mascotGround.GetComponent<Image>().raycastTarget = false;

            var mascotGlow = UiBuilder.CreateRect(
                "MascotGlow", canvas.transform,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(480f, 480f), new Vector2(0f, 78f),
                new Color(0.3f, 0.7f, 1f, 0.15f), glow: true);
            mascotGlow.GetComponent<Image>().raycastTarget = false;

            var mascotRing = UiBuilder.CreateRect(
                "MascotRing", canvas.transform,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(400f, 400f), new Vector2(0f, 78f),
                new Color(0.5f, 0.85f, 1f, 0.18f), glow: true);
            mascotRing.GetComponent<Image>().raycastTarget = false;

            // Mascot (Center) — 由 UpdateMascotCosmetic 在装扮切换时重绘
            mascotArea = UiBuilder.CreateRect("MascotArea", canvas.transform,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(360f, 360f), new Vector2(0f, 80f));
            DrawPenguinMascot(mascotArea);
            mascotArea.gameObject.AddComponent<FloatTween>();

            // 主操作按钮（CTA）— 位置上调避免与底栏重叠
            // 外层光晕
            var ctaGlow = UiBuilder.CreateRect("CtaGlow", canvas.transform,
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(820f, 320f), new Vector2(0f, 360f),
                new Color(1f, 0.7f, 0.2f, 0.22f), glow: true);
            ctaGlow.GetComponent<Image>().raycastTarget = false;
            ctaGlow.gameObject.AddComponent<PulseTween>();

            // 「下一局加成」预览
            var boosterChip = UiBuilder.CreateRect("BoosterPreview", canvas.transform,
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(460f, 44f), new Vector2(0f, 480f),
                new Color(0f, 0.85f, 0.95f, 0.18f), rounded: true);
            UiBuilder.AddOutline(boosterChip.gameObject, new Color(0f, 0.85f, 0.95f, 0.55f), new Vector2(1f, -1f));
            boosterChip.GetComponent<Image>().raycastTarget = false;

            var boosterIcon = UiBuilder.CreateRect("Icon", boosterChip,
                new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
                new Vector2(26f, 26f), new Vector2(20f, 0f),
                UiBuilder.AccentCyan, circle: true);
            UiBuilder.CreateText("IconText", boosterIcon.transform, "\u2728", 14, FontStyle.Bold,
                TextAnchor.MiddleCenter, new Color(0.04f, 0.08f, 0.14f, 1f));

            boosterPreviewText = UiBuilder.CreateText("Text", boosterChip,
                "下一局：", 18, FontStyle.Bold, TextAnchor.MiddleLeft, UiBuilder.AccentCyan);
            var bptRt = (RectTransform)boosterPreviewText.transform;
            bptRt.anchorMin = new Vector2(0f, 0f);
            bptRt.anchorMax = new Vector2(1f, 1f);
            bptRt.offsetMin = new Vector2(56f, 0f);
            bptRt.offsetMax = new Vector2(-16f, 0f);
            boosterChip.gameObject.SetActive(false);

            // 开始游戏按钮（位置从 410 调到 360，避免和底栏重叠）
            var startBtn = UiBuilder.CreateButton(
                "StartEndless", canvas.transform,
                "开始游戏\n<size=20><color=#3a2509>点击开始 · 极夜快跑</color></size>",
                new Color(1f, 0.84f, 0.32f), new Color(0.1f, 0.05f, 0.01f), 48, rounded: true);
            var sbtnRt = (RectTransform)startBtn.transform;
            sbtnRt.anchorMin = new Vector2(0.5f, 0f);
            sbtnRt.anchorMax = new Vector2(0.5f, 0f);
            sbtnRt.sizeDelta = new Vector2(560f, 148f);
            sbtnRt.anchoredPosition = new Vector2(0f, 360f);
            startBtn.onClick.AddListener(StartEndless);
            var startLabel = startBtn.GetComponentInChildren<Text>();
            startLabel.supportRichText = true;
            startLabel.lineSpacing = 1.12f;

            var startColors = startBtn.colors;
            startColors.normalColor = new Color(1f, 0.84f, 0.32f);
            startColors.highlightedColor = new Color(1f, 0.92f, 0.45f);
            startColors.pressedColor = new Color(0.85f, 0.7f, 0.22f);
            startColors.selectedColor = new Color(1f, 0.88f, 0.38f);
            startBtn.colors = startColors;

            UiBuilder.AddOutline(startBtn.gameObject, new Color(0.4f, 0.25f, 0.05f, 0.7f), new Vector2(2.5f, -2.5f));
            UiBuilder.AddShadow(startBtn.gameObject, new Color(0f, 0f, 0f, 0.55f), new Vector2(0, -8f));
            startBtn.gameObject.AddComponent<PulseTween>();
            startBtn.gameObject.AddComponent<ClickScaleTween>();

            // 顶部高光条
            var shine = UiBuilder.CreateRect(
                "StartShine", startBtn.transform,
                new Vector2(0f, 1f), new Vector2(1f, 1f),
                new Vector2(0f, 8f), new Vector2(0f, -8f),
                new Color(1f, 1f, 1f, 0.45f), rounded: true);
            shine.GetComponent<Image>().raycastTarget = false;
            shine.offsetMin = new Vector2(24f, shine.offsetMin.y);
            shine.offsetMax = new Vector2(-24f, shine.offsetMax.y);
            shine.SetAsFirstSibling();

            // 底部阴影条
            var btnBottomShade = UiBuilder.CreateRect(
                "StartBottomShade", startBtn.transform,
                new Vector2(0f, 0f), new Vector2(1f, 0f),
                new Vector2(0f, 10f), new Vector2(0f, 8f),
                new Color(0.6f, 0.4f, 0.05f, 0.4f), rounded: true);
            btnBottomShade.GetComponent<Image>().raycastTarget = false;
            btnBottomShade.offsetMin = new Vector2(24f, btnBottomShade.offsetMin.y);
            btnBottomShade.offsetMax = new Vector2(-24f, btnBottomShade.offsetMax.y);
            btnBottomShade.SetAsFirstSibling();

            // Secondary CTA — 「今日挑战」位置调到 210f（底栏上方约 70px 间隙）
            var dailyBtn = UiBuilder.CreateButton(
                "StartDaily", canvas.transform,
                string.Empty,
                new Color(0.12f, 0.32f, 0.5f, 0.95f),
                Color.white, 24, rounded: true);
            var drt = (RectTransform)dailyBtn.transform;
            drt.anchorMin = new Vector2(0.5f, 0f);
            drt.anchorMax = new Vector2(0.5f, 0f);
            drt.sizeDelta = new Vector2(340f, 72f);
            drt.anchoredPosition = new Vector2(0f, 210f);

            // 主按钮自带的 Label 不再需要（用复合内容替代）
            var dailyDefaultLabel = dailyBtn.GetComponentInChildren<Text>();
            if (dailyDefaultLabel != null) dailyDefaultLabel.gameObject.SetActive(false);

            var dailyColors = dailyBtn.colors;
            dailyColors.normalColor = new Color(0.12f, 0.32f, 0.5f, 0.95f);
            dailyColors.highlightedColor = new Color(0.2f, 0.45f, 0.65f, 1f);
            dailyColors.pressedColor = new Color(0.08f, 0.22f, 0.4f, 1f);
            dailyColors.selectedColor = new Color(0.16f, 0.4f, 0.6f, 1f);
            dailyBtn.colors = dailyColors;

            dailyBtn.onClick.AddListener(() => {
                Debug.Log("[MainMenu] 今日挑战按钮被点击");
                StartDaily();
            });

            UiBuilder.AddShadow(dailyBtn.gameObject, new Color(0f, 0f, 0f, 0.45f), new Vector2(0, -5f));
            UiBuilder.AddOutline(dailyBtn.gameObject, new Color(0f, 0.85f, 0.95f, 0.5f), new Vector2(1.5f, -1.5f));
            dailyBtn.gameObject.AddComponent<ClickScaleTween>();

            // 左侧图标徽章
            var dailyIcon = UiBuilder.CreateRect("DailyIcon", dailyBtn.transform,
                new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
                new Vector2(48f, 48f), new Vector2(36f, 0f),
                new Color(0f, 0.85f, 0.95f, 0.95f), circle: true);
            UiBuilder.AddOutline(dailyIcon.gameObject, new Color(0.7f, 1f, 1f, 0.6f), new Vector2(1f, -1f));
            UiBuilder.CreateText("DailyIconText", dailyIcon.transform, "\u2600", 26, FontStyle.Bold,
                TextAnchor.MiddleCenter, new Color(0.04f, 0.1f, 0.18f, 1f));

            // 主标
            var dailyTitle = UiBuilder.CreateText("DailyTitle", dailyBtn.transform,
                "今日挑战", 26, FontStyle.Bold, TextAnchor.LowerLeft, Color.white);
            var dtRt = (RectTransform)dailyTitle.transform;
            dtRt.anchorMin = new Vector2(0f, 0.5f);
            dtRt.anchorMax = new Vector2(1f, 1f);
            dtRt.offsetMin = new Vector2(78f, 0f);
            dtRt.offsetMax = new Vector2(-20f, -4f);

            // 副标
            var dailySub = UiBuilder.CreateText("DailySub", dailyBtn.transform,
                "限定关卡 · 双倍鱼干", 16, FontStyle.Normal, TextAnchor.UpperLeft,
                new Color(0.7f, 0.9f, 1f, 0.78f));
            var dsRt = (RectTransform)dailySub.transform;
            dsRt.anchorMin = new Vector2(0f, 0f);
            dsRt.anchorMax = new Vector2(1f, 0.5f);
            dsRt.offsetMin = new Vector2(78f, 4f);
            dsRt.offsetMax = new Vector2(-20f, 0f);

            // 顶部高光（细条，使用纯色精灵避免 9-slice 异常）
            var dailyShine = UiBuilder.CreateRect("DailyShine", dailyBtn.transform,
                new Vector2(0f, 1f), new Vector2(1f, 1f),
                new Vector2(0f, 4f), new Vector2(0f, -3f),
                new Color(1f, 1f, 1f, 0.16f));
            dailyShine.GetComponent<Image>().raycastTarget = false;
            dailyShine.offsetMin = new Vector2(20f, dailyShine.offsetMin.y);
            dailyShine.offsetMax = new Vector2(-20f, dailyShine.offsetMax.y);
            dailyShine.SetAsFirstSibling();
        }

        private void BuildBottomDock()
        {
            // 简洁扁平底栏：纯深色背景 + 顶部细线，无大圆角卡片感
            dockWrap = UiBuilder.CreateRect("DockWrap", canvas.transform,
                new Vector2(0f, 0f), new Vector2(1f, 0f),
                new Vector2(0f, 160f), new Vector2(0f, 0f),
                new Color(0.04f, 0.09f, 0.17f, 0.97f));
            dockWrap.pivot = new Vector2(0.5f, 0f);
            dockWrap.GetComponent<Image>().raycastTarget = true;

            // 顶部青色分隔线
            var dockLine = UiBuilder.CreateRect("DockLine", dockWrap,
                new Vector2(0f, 1f), new Vector2(1f, 1f),
                new Vector2(0f, 1.5f), new Vector2(0f, 0f),
                new Color(0f, 0.85f, 0.95f, 0.38f));
            dockLine.GetComponent<Image>().raycastTarget = false;

            // 顶部微弱辉光
            var dockGlow = UiBuilder.CreateRect("DockGlow", dockWrap,
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(1200f, 80f), new Vector2(0f, 40f),
                new Color(0f, 0.85f, 0.95f, 0.06f), glow: true);
            dockGlow.GetComponent<Image>().raycastTarget = false;

            // HLayout
            var layoutGo = new GameObject("DockLayout", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            layoutGo.transform.SetParent(dockWrap, false);
            var lrt = (RectTransform)layoutGo.transform;
            lrt.anchorMin = Vector2.zero;
            lrt.anchorMax = Vector2.one;
            lrt.sizeDelta = Vector2.zero;
            lrt.offsetMin = new Vector2(0f, 0f);
            lrt.offsetMax = new Vector2(0f, 0f);

            var hlg = layoutGo.GetComponent<HorizontalLayoutGroup>();
            hlg.padding = new RectOffset(0, 0, 0, 0);
            hlg.spacing = 0f;
            hlg.childAlignment = TextAnchor.MiddleCenter;
            hlg.childControlWidth = true;
            hlg.childControlHeight = true;
            hlg.childForceExpandWidth = true;
            hlg.childForceExpandHeight = true;

            // 5 个 Tab 入口
            CreateDockButton(layoutGo.transform, "\u2726", "营地", OpenCampShop, true, "CampShop");
            missionBadge = CreateDockButton(layoutGo.transform, "\u2630", "任务", OpenMissions, true, "Missions");
            CreateDockButton(layoutGo.transform, "\u2605", "排行", OpenLeaderboard, false, "Leaderboard");
            CreateDockButton(layoutGo.transform, "\u25A1", "图鉴", OpenCodex, false, "Codex");
            CreateDockButton(layoutGo.transform, "\u2699", "设置", OpenSettings, false, "Settings");
        }

        /// <summary>底栏导航 Tab：图标 + 文字标签，选中时顶部亮条 + 文字变白变大。</summary>
        private GameObject CreateDockButton(Transform parent, string glyph, string label, System.Action onClick, bool withBadge, string panelName)
        {
            // 整个 Tab 区域（充满 HLayoutGroup 分配的宽度）
            var itemGo = new GameObject("Tab_" + label, typeof(RectTransform), typeof(CanvasRenderer),
                typeof(Image), typeof(Button));
            itemGo.transform.SetParent(parent, false);

            var itemImg = itemGo.GetComponent<Image>();
            itemImg.sprite = UiBuilder.SolidUiSprite;
            itemImg.type = Image.Type.Simple;
            itemImg.color = new Color(0f, 0f, 0f, 0f);

            var itemBtn = itemGo.GetComponent<Button>();
            var bColors = itemBtn.colors;
            bColors.normalColor = new Color(0f, 0f, 0f, 0f);
            bColors.highlightedColor = new Color(0f, 0.85f, 0.95f, 0.06f);
            bColors.pressedColor = new Color(0f, 0.85f, 0.95f, 0.12f);
            bColors.selectedColor = new Color(0f, 0f, 0f, 0f);
            itemBtn.colors = bColors;
            itemBtn.targetGraphic = itemImg;
            itemGo.AddComponent<ClickScaleTween>();

            itemBtn.onClick.AddListener(() => onClick?.Invoke());

            // 顶部选中指示条（默认隐藏）
            var activeLine = UiBuilder.CreateRect("ActiveLine", itemGo.transform,
                new Vector2(0.15f, 1f), new Vector2(0.85f, 1f),
                new Vector2(0f, 3f), new Vector2(0f, -1f),
                new Color(0f, 0.85f, 0.95f, 0.9f), rounded: true);
            activeLine.GetComponent<Image>().raycastTarget = false;
            activeLine.gameObject.SetActive(false);

            // 图标（圆形背景 + 字符符号）
            var iconCircle = UiBuilder.CreateRect("IconBg", itemGo.transform,
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(66f, 66f), new Vector2(0f, -32f),
                new Color(0.12f, 0.24f, 0.42f, 0.85f), circle: true);
            iconCircle.pivot = new Vector2(0.5f, 1f);
            var iconBgImg = iconCircle.GetComponent<Image>();

            var iconText = UiBuilder.CreateText("Icon", iconCircle, glyph, 26, FontStyle.Bold,
                TextAnchor.MiddleCenter, new Color(0.55f, 0.88f, 1f, 1f));
            var itrt = (RectTransform)iconText.transform;
            itrt.anchorMin = Vector2.zero;
            itrt.anchorMax = Vector2.one;
            itrt.sizeDelta = Vector2.zero;

            // 文字标签
            var labelText = UiBuilder.CreateText("Label", itemGo.transform, label, 19, FontStyle.Normal,
                TextAnchor.MiddleCenter, new Color(0.6f, 0.72f, 0.85f, 0.8f));
            var lrt = (RectTransform)labelText.transform;
            lrt.anchorMin = new Vector2(0f, 0f);
            lrt.anchorMax = new Vector2(1f, 0f);
            lrt.pivot = new Vector2(0.5f, 0f);
            lrt.sizeDelta = new Vector2(0f, 36f);
            lrt.anchoredPosition = new Vector2(0f, 14f);

            dockItems.Add(new DockItem
            {
                panelName = panelName,
                iconBg = iconBgImg,
                iconText = iconText,
                labelText = labelText,
                labelBg = null,
                activeLine = activeLine.GetComponent<Image>(),
            });

            if (!withBadge) return null;

            // 通知红点（圆形徽章，放图标右上角）
            var badgeGo = new GameObject("Badge", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            badgeGo.transform.SetParent(iconCircle, false);
            var brt = (RectTransform)badgeGo.transform;
            brt.anchorMin = new Vector2(1f, 1f);
            brt.anchorMax = new Vector2(1f, 1f);
            brt.pivot = new Vector2(0.5f, 0.5f);
            brt.sizeDelta = new Vector2(18f, 18f);
            brt.anchoredPosition = new Vector2(4f, 4f);
            var badgeImg = badgeGo.GetComponent<Image>();
            badgeImg.color = new Color(1f, 0.28f, 0.35f, 0.95f);
            badgeImg.sprite = UiBuilder.CircleSprite;
            badgeGo.SetActive(false);
            return badgeGo;
        }

        private static void DrawPenguinMascot(RectTransform parent)
        {
            // 当前装扮配色（围巾 + 帽子）
            var scarfPalette = CosmeticPalette.FindScarf(PlayerSave.SelectedScarfId);
            var hatPalette = CosmeticPalette.FindHat(PlayerSave.SelectedHatId);
            var scarfMain = scarfPalette.Primary;
            var scarfKnot = scarfPalette.Secondary;
            var hatCrown = hatPalette.Crown;
            var hatBand = hatPalette.Band;

            // Shadow under the penguin
            UiBuilder.CreateRect("MascotShadow", parent,
                new Vector2(0.38f, 0.06f), new Vector2(0.62f, 0.12f),
                Vector2.zero, new Vector2(0f, -2f),
                new Color(0f, 0f, 0f, 0.3f), rounded: true);

            // Wings (flippers) - slightly darker blue-gray
            UiBuilder.CreateRect("FlipL", parent,
                new Vector2(0.15f, 0.35f), new Vector2(0.28f, 0.58f),
                Vector2.zero, new Vector2(-2f, 0f),
                new Color(0.1f, 0.18f, 0.32f), rounded: true);
            UiBuilder.CreateRect("FlipR", parent,
                new Vector2(0.72f, 0.35f), new Vector2(0.85f, 0.58f),
                Vector2.zero, new Vector2(2f, 0f),
                new Color(0.1f, 0.18f, 0.32f), rounded: true);

            // Body - main dark blue-gray
            var body = UiBuilder.CreateRect("MascotBody", parent,
                new Vector2(0.3f, 0.18f), new Vector2(0.7f, 0.66f),
                Vector2.zero, Vector2.zero,
                new Color(0.12f, 0.2f, 0.35f), rounded: true);
            
            // Belly - white/cream colored
            UiBuilder.CreateRect("MascotBelly", body,
                new Vector2(0.22f, 0.1f), new Vector2(0.78f, 0.7f),
                Vector2.zero, Vector2.zero,
                new Color(0.95f, 0.98f, 1f), rounded: true);

            // Scarf - 取自所选围巾色
            UiBuilder.CreateRect("MascotScarf", parent,
                new Vector2(0.25f, 0.52f), new Vector2(0.75f, 0.62f),
                Vector2.zero, Vector2.zero,
                scarfMain, rounded: true);

            // Head - same color as body
            var head = UiBuilder.CreateRect("MascotHead", parent,
                new Vector2(0.28f, 0.58f), new Vector2(0.72f, 0.9f),
                Vector2.zero, Vector2.zero,
                new Color(0.12f, 0.2f, 0.35f), rounded: true);
            
            // Face area (white)
            UiBuilder.CreateRect("MascotFace", head,
                new Vector2(0.18f, 0.14f), new Vector2(0.82f, 0.7f),
                Vector2.zero, Vector2.zero,
                new Color(0.95f, 0.98f, 1f), rounded: true);
            
            // Cheeks - soft pink
            UiBuilder.CreateRect("MascotCheekL", head,
                new Vector2(0.22f, 0.28f), new Vector2(0.34f, 0.4f),
                Vector2.zero, Vector2.zero,
                new Color(1f, 0.65f, 0.7f, 0.5f), circle: true);
            UiBuilder.CreateRect("MascotCheekR", head,
                new Vector2(0.66f, 0.28f), new Vector2(0.78f, 0.4f),
                Vector2.zero, Vector2.zero,
                new Color(1f, 0.65f, 0.7f, 0.5f), circle: true);

            // Eyes - white sclera
            UiBuilder.CreateRect("EyeWhiteL", head,
                new Vector2(0.32f, 0.44f), new Vector2(0.43f, 0.56f),
                Vector2.zero, Vector2.zero,
                Color.white, circle: true);
            UiBuilder.CreateRect("EyeWhiteR", head,
                new Vector2(0.57f, 0.44f), new Vector2(0.68f, 0.56f),
                Vector2.zero, Vector2.zero,
                Color.white, circle: true);
            
            // Pupils - dark blue
            UiBuilder.CreateRect("EyeL", head,
                new Vector2(0.35f, 0.47f), new Vector2(0.41f, 0.54f),
                Vector2.zero, Vector2.zero,
                new Color(0.12f, 0.42f, 0.88f), circle: true);
            UiBuilder.CreateRect("EyeR", head,
                new Vector2(0.59f, 0.47f), new Vector2(0.65f, 0.54f),
                Vector2.zero, Vector2.zero,
                new Color(0.12f, 0.42f, 0.88f), circle: true);
            
            // Eye highlights
            UiBuilder.CreateRect("EyeSparkL", head,
                new Vector2(0.37f, 0.5f), new Vector2(0.4f, 0.54f),
                Vector2.zero, Vector2.zero,
                new Color(1f, 1f, 1f, 0.9f), circle: true);
            UiBuilder.CreateRect("EyeSparkR", head,
                new Vector2(0.61f, 0.5f), new Vector2(0.64f, 0.54f),
                Vector2.zero, Vector2.zero,
                new Color(1f, 1f, 1f, 0.9f), circle: true);

            // Beak - orange
            UiBuilder.CreateRect("Beak", head,
                new Vector2(0.45f, 0.2f), new Vector2(0.55f, 0.32f),
                Vector2.zero, Vector2.zero,
                new Color(1f, 0.6f, 0.25f), rounded: true);

            // Scarf knot
            UiBuilder.CreateRect("MascotScarfKnot", parent,
                new Vector2(0.58f, 0.42f), new Vector2(0.7f, 0.54f),
                Vector2.zero, new Vector2(2f, 0f),
                scarfKnot, rounded: true);

            // Hat - 取自所选帽子色
            UiBuilder.CreateRect("MascotHat", parent,
                new Vector2(0.28f, 0.85f), new Vector2(0.72f, 0.96f),
                Vector2.zero, Vector2.zero,
                hatCrown, rounded: true);
            UiBuilder.CreateRect("MascotHatBand", parent,
                new Vector2(0.3f, 0.82f), new Vector2(0.7f, 0.88f),
                Vector2.zero, Vector2.zero,
                hatBand, rounded: true);

            // Feet - orange
            UiBuilder.CreateRect("FootL", parent,
                new Vector2(0.38f, 0.08f), new Vector2(0.47f, 0.14f),
                Vector2.zero, Vector2.zero,
                new Color(1f, 0.58f, 0.22f), rounded: true);
            UiBuilder.CreateRect("FootR", parent,
                new Vector2(0.53f, 0.08f), new Vector2(0.62f, 0.14f),
                Vector2.zero, Vector2.zero,
                new Color(1f, 0.58f, 0.22f), rounded: true);
        }

        private void RefreshHeader()
        {
            nicknameText.text = $"嗨，{PlayerSave.PlayerNickname}";
            bestScoreText.text = $"历史最高 {PlayerSave.BestScore}";
            fishSnacksText.text = $"{PlayerSave.TotalFishSnacks}  鱼干";
            if (missionBadge != null)
            {
                missionBadge.SetActive(MissionStore.HasClaimableMissions());
            }
            UpdateBoosterPreview();
            UpdateMascotCosmetic();
        }

        /// <summary>下一局即将自动消耗的道具数量预览，挂在主 CTA 下方。</summary>
        private void UpdateBoosterPreview()
        {
            if (boosterPreviewText == null) return;
            var df = ShopStore.InventoryCount(ShopItemKind.BoosterDoubleFish);
            var sb = ShopStore.InventoryCount(ShopItemKind.BoosterScoreBoost);
            if (df <= 0 && sb <= 0)
            {
                boosterPreviewText.transform.parent.gameObject.SetActive(false);
                return;
            }
            boosterPreviewText.transform.parent.gameObject.SetActive(true);
            var parts = new System.Collections.Generic.List<string>();
            if (df > 0) parts.Add("鱼干 ×2");
            if (sb > 0) parts.Add("得分 ×1.5");
            boosterPreviewText.text = "下一局：" + string.Join(" · ", parts);
        }

        /// <summary>装扮变化后重绘吉祥物。</summary>
        private void UpdateMascotCosmetic()
        {
            if (mascotArea == null) return;
            for (var i = mascotArea.childCount - 1; i >= 0; i--)
            {
                Destroy(mascotArea.GetChild(i).gameObject);
            }
            DrawPenguinMascot(mascotArea);
        }

        // ── 难度选择 ─────────────────────────────────────────────────────────────
        private DifficultyKind pendingDifficulty = DifficultyKind.Normal;
        private bool pendingIsDaily;
        private GameObject difficultyOverlay;

        private void StartEndless()
        {
            pendingIsDaily = false;
            pendingDifficulty = PlayerSave.SelectedDifficulty;
            ShowDifficultyOverlay();
        }

        private void StartDaily()
        {
            pendingIsDaily = true;
            pendingDifficulty = PlayerSave.SelectedDifficulty;
            ShowDifficultyOverlay();
        }

        private void ShowDifficultyOverlay()
        {
            if (difficultyOverlay != null) Destroy(difficultyOverlay);

            // 半透明遮罩
            var overlay = UiBuilder.CreatePanel("DifficultyOverlay", canvas.transform,
                new Color(0f, 0f, 0f, 0.72f));
            difficultyOverlay = overlay;
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

            // 标题
            var titleText = UiBuilder.CreateText("Title", card, "选择难度", 44, FontStyle.Bold,
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
                Destroy(difficultyOverlay);
                difficultyOverlay = null;
            });

            var startFinalBtn = UiBuilder.CreateButton("StartFinal", card, "开始挑战",
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
                Destroy(difficultyOverlay);
                difficultyOverlay = null;
                if (pendingIsDaily)
                    RunSession.PrepareDailyRun(difficulty: pendingDifficulty);
                else
                    RunSession.PrepareEndlessRun(difficulty: pendingDifficulty);
                SceneManager.LoadScene("PenguinRunner");
            });
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

        private void OpenPanel(GameObject go, string panelName)
        {
            ClosePanel();
            currentPanel = go;
            currentPanelName = panelName;
            
            if (panelLayer != null) panelLayer.SetAsLastSibling();
            if (dockWrap != null) dockWrap.SetAsLastSibling();
            if (snackbar != null) snackbar.transform.SetAsLastSibling();
            
            RefreshDockState();
        }

        private void ClosePanel()
        {
            if (currentPanel != null)
            {
                Destroy(currentPanel);
                currentPanel = null;
                currentPanelName = null;
                RefreshHeader();
                RefreshDockState();
            }
        }

        private void RefreshDockState()
        {
            foreach (var item in dockItems)
            {
                bool isSelected = (item.panelName == currentPanelName);

                // 顶部指示条
                if (item.activeLine != null)
                    item.activeLine.gameObject.SetActive(isSelected);

                // 圆形图标背景
                if (item.iconBg != null)
                    item.iconBg.color = isSelected
                        ? new Color(0f, 0.85f, 0.95f, 1f)
                        : new Color(0.12f, 0.24f, 0.42f, 0.85f);

                // 图标符号颜色
                if (item.iconText != null)
                    item.iconText.color = isSelected
                        ? new Color(0.04f, 0.08f, 0.14f, 1f)
                        : new Color(0.55f, 0.88f, 1f, 1f);

                // 文字标签颜色 + 粗细
                if (item.labelText != null)
                {
                    item.labelText.color = isSelected
                        ? new Color(0f, 0.9f, 1f, 1f)
                        : new Color(0.6f, 0.72f, 0.85f, 0.8f);
                    item.labelText.fontStyle = isSelected ? FontStyle.Bold : FontStyle.Normal;
                    item.labelText.fontSize = isSelected ? 20 : 19;
                }
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
