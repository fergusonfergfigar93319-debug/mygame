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
        private RectTransform avatarMiniArea;
        private Text boosterPreviewText;

        private Transform panelLayer;
        private GameObject currentPanel;
        private string currentPanelName;
        private RectTransform dockWrap;

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
        }
        private readonly System.Collections.Generic.List<DockItem> dockItems = new();

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
            EnsureLandscape();
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
            BuildPrimaryCtaForegroundStrip();
            // 全屏面板必须在顶栏/主菜单之上，否则任务头等控件会与首页重叠且无法正确覆盖
            panelLayer.SetAsLastSibling();
            snackbar = Snackbar.EnsureUnder(canvas);
            snackbar.transform.SetAsLastSibling();
        }

        /// <summary>将开始游戏 + 今日挑战挂到全屏前景根下并提到底坞之上，射线命中顺序稳定。</summary>
        private void BuildPrimaryCtaForegroundStrip()
        {
            if (canvas == null) return;
            if (startGameButton == null && dailyChallengeButton == null) return;

            var stripGo = new GameObject("MenuPrimaryCtaStrip", typeof(RectTransform));
            stripGo.transform.SetParent(canvas.transform, false);
            var strt = (RectTransform)stripGo.transform;
            strt.anchorMin = Vector2.zero;
            strt.anchorMax = Vector2.one;
            strt.sizeDelta = Vector2.zero;
            strt.anchoredPosition = Vector2.zero;
            strt.offsetMin = Vector2.zero;
            strt.offsetMax = Vector2.zero;

            if (startGameButton != null)
                startGameButton.SetParent(stripGo.transform, worldPositionStays: true);
            if (dailyChallengeButton != null)
                dailyChallengeButton.SetParent(stripGo.transform, worldPositionStays: true);

            stripGo.transform.SetAsLastSibling();
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
            UiBuilder.AddOutline(avatarBg.gameObject, new Color(1f, 0.82f, 0.35f, 0.75f), new Vector2(1.5f, -1.5f));

            avatarMiniArea = UiBuilder.CreateRect("AvatarInner", avatarBg,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(60f, 60f), Vector2.zero,
                new Color(0.06f, 0.2f, 0.38f, 0.98f), circle: true);
            DrawMiniPenguinAvatar(avatarMiniArea);

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

        private void BuildMountainSilhouettes()
        {
            var back = UiBuilder.CreateRect("MountainLayerBack", canvas.transform,
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(1320f, 300f), new Vector2(0f, 410f));
            back.GetComponent<RectTransform>().pivot = new Vector2(0.5f, 0f);
            var backDrift = back.gameObject.AddComponent<DriftTween>();
            backDrift.amplitude = 10f;
            backDrift.speed = 0.18f;

            CreateMountain(back, "PeakA", new Vector2(-430f, 0f), new Vector2(300f, 250f), new Color(0.035f, 0.075f, 0.15f, 0.58f));
            CreateMountain(back, "PeakB", new Vector2(-80f, 0f), new Vector2(360f, 280f), new Color(0.04f, 0.085f, 0.17f, 0.52f));
            CreateMountain(back, "PeakC", new Vector2(310f, 0f), new Vector2(320f, 250f), new Color(0.035f, 0.075f, 0.15f, 0.52f));

            var front = UiBuilder.CreateRect("MountainLayerFront", canvas.transform,
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(1320f, 230f), new Vector2(0f, 370f));
            front.GetComponent<RectTransform>().pivot = new Vector2(0.5f, 0f);
            var frontDrift = front.gameObject.AddComponent<DriftTween>();
            frontDrift.amplitude = 16f;
            frontDrift.speed = 0.24f;

            CreateMountain(front, "FrontPeakA", new Vector2(-330f, 0f), new Vector2(260f, 180f), new Color(0.06f, 0.12f, 0.22f, 0.46f));
            CreateMountain(front, "FrontPeakB", new Vector2(180f, 0f), new Vector2(360f, 210f), new Color(0.07f, 0.13f, 0.24f, 0.42f));
        }

        private static void CreateMountain(Transform parent, string name, Vector2 pos, Vector2 size, Color color)
        {
            var peak = UiBuilder.CreateTriangle(name, parent,
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                size, pos + new Vector2(0f, size.y * 0.5f), color);
            peak.GetComponent<Image>().raycastTarget = false;

            var snow = UiBuilder.CreateTriangle(name + "Snowcap", parent,
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                size * 0.28f, pos + new Vector2(0f, size.y * 0.86f),
                new Color(0.8f, 0.95f, 1f, 0.34f));
            snow.GetComponent<Image>().raycastTarget = false;
        }

        private void BuildIceRunway()
        {
            var ground = UiBuilder.CreateRect("IceRunway", canvas.transform,
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(1180f, 160f), new Vector2(0f, 286f),
                new Color(0.42f, 0.78f, 1f, 0.16f), rounded: true);
            ground.GetComponent<Image>().raycastTarget = false;
            UiBuilder.AddOutline(ground.gameObject, new Color(0.76f, 1f, 1f, 0.14f), new Vector2(1f, -1f));

            var brightBand = UiBuilder.CreateRect("IceRunwayShine", ground,
                new Vector2(0f, 0.66f), new Vector2(1f, 0.84f),
                Vector2.zero, Vector2.zero,
                new Color(0.85f, 1f, 1f, 0.1f), rounded: true);
            brightBand.GetComponent<Image>().raycastTarget = false;

            for (var i = 0; i < 12; i++)
            {
                var line = UiBuilder.CreateRect("IceMotionLine", ground,
                    new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                    new Vector2(Random.Range(50f, 110f), Random.Range(2f, 4f)),
                    new Vector2(-540f + i * 98f, Random.Range(-38f, 38f)),
                    new Color(0.92f, 1f, 1f, Random.Range(0.12f, 0.24f)), rounded: true);
                line.localRotation = Quaternion.Euler(0f, 0f, Random.Range(-5f, 5f));
                line.GetComponent<Image>().raycastTarget = false;
                var scroll = line.gameObject.AddComponent<RunwayScrollTween>();
                scroll.speed = Random.Range(70f, 130f);
                scroll.resetLeft = -620f;
                scroll.resetRight = 620f;
            }

            for (var i = 0; i < 9; i++)
            {
                var chip = UiBuilder.CreateTriangle("IceChip", ground,
                    new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                    new Vector2(24f, 18f), new Vector2(-520f + i * 130f, -6f),
                    new Color(0.8f, 1f, 1f, 0.14f));
                chip.localRotation = Quaternion.Euler(0f, 0f, i % 2 == 0 ? 180f : 0f);
                chip.GetComponent<Image>().raycastTarget = false;
            }
        }

        private void BuildHero()
        {
            // Title — separated to avoid rich text shadow artifacts
            var titleStr = "企鹅快跑";
            var title = UiBuilder.CreateText(
                "MainTitle", canvas.transform,
                titleStr,
                92, FontStyle.Bold, TextAnchor.MiddleCenter, new Color(1f, 1f, 1f, 1f));
            var trt = (RectTransform)title.transform;
            trt.anchorMin = new Vector2(0.5f, 1f);
            trt.anchorMax = new Vector2(0.5f, 1f);
            trt.sizeDelta = new Vector2(840f, 170f);
            trt.anchoredPosition = new Vector2(0f, -118f);
            trt.localRotation = Quaternion.Euler(0f, 0f, -0.8f);

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

            var arrowRoot = new GameObject("TitleArrowMarquee", typeof(RectTransform));
            arrowRoot.transform.SetParent(canvas.transform, false);
            var art = (RectTransform)arrowRoot.transform;
            art.anchorMin = new Vector2(0.5f, 1f);
            art.anchorMax = new Vector2(0.5f, 1f);
            art.sizeDelta = new Vector2(160f, 30f);
            art.anchoredPosition = new Vector2(0f, -328f);
            var arrows = new Text[3];
            for (var i = 0; i < arrows.Length; i++)
            {
                arrows[i] = UiBuilder.CreateText("Arrow" + i, arrowRoot.transform, "»", 24, FontStyle.Bold,
                    TextAnchor.MiddleCenter, new Color(0.45f, 1f, 1f, 0.3f));
                var rt = (RectTransform)arrows[i].transform;
                rt.anchorMin = new Vector2(0f, 0f);
                rt.anchorMax = new Vector2(0f, 1f);
                rt.sizeDelta = new Vector2(44f, 0f);
                rt.anchoredPosition = new Vector2(44f + i * 34f, 0f);
            }
            arrowRoot.AddComponent<MarqueeTween>().texts = arrows;

            BuildMountainSilhouettes();
            BuildIceRunway();

            var speedLayer = new GameObject("SpeedLineLayer", typeof(RectTransform));
            speedLayer.transform.SetParent(canvas.transform, false);
            var spRt = (RectTransform)speedLayer.transform;
            spRt.anchorMin = Vector2.zero;
            spRt.anchorMax = Vector2.one;
            spRt.sizeDelta = Vector2.zero;
            speedLayer.AddComponent<SpeedLineEmitter>();

            var mascotGlow = UiBuilder.CreateRect(
                "MascotGlow", canvas.transform,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(560f, 520f), new Vector2(0f, 52f),
                new Color(0.3f, 0.7f, 1f, 0.12f), glow: true);
            mascotGlow.GetComponent<Image>().raycastTarget = false;

            var mascotRing = UiBuilder.CreateRect(
                "MascotRing", canvas.transform,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(430f, 410f), new Vector2(0f, 52f),
                new Color(0.5f, 0.85f, 1f, 0.11f), glow: true);
            mascotRing.GetComponent<Image>().raycastTarget = false;

            // Mascot (Center) — 由 UpdateMascotCosmetic 在装扮切换时重绘
            mascotArea = UiBuilder.CreateRect("MascotArea", canvas.transform,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(410f, 430f), new Vector2(0f, 44f));
            DrawPenguinMascot(mascotArea);
            SetRaycastTargets(mascotArea, false);
            var mascotFloat = mascotArea.gameObject.AddComponent<FloatTween>();
            mascotFloat.amplitude = 5f;
            mascotFloat.speed = 1.2f;

            var snowLayer = new GameObject("SnowfallLayer", typeof(RectTransform));
            snowLayer.transform.SetParent(canvas.transform, false);
            var snowRt = (RectTransform)snowLayer.transform;
            snowRt.anchorMin = Vector2.zero;
            snowRt.anchorMax = Vector2.one;
            snowRt.sizeDelta = Vector2.zero;
            snowLayer.AddComponent<SnowfallEmitter>();

            // 主操作按钮（CTA）— 位置上调避免与底栏重叠
            // 外层光晕
            var ctaGlow = UiBuilder.CreateRect("CtaGlow", canvas.transform,
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(760f, 300f), new Vector2(0f, 342f),
                new Color(1f, 0.7f, 0.2f, 0.16f), glow: true);
            ctaGlow.GetComponent<Image>().raycastTarget = false;
            var ctaPulse = ctaGlow.gameObject.AddComponent<PulseTween>();
            ctaPulse.speed = 8.5f;

            // 「下一局加成」预览
            var boosterChip = UiBuilder.CreateRect("BoosterPreview", canvas.transform,
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(460f, 44f), new Vector2(0f, 454f),
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
            startGameButton = startBtn.transform;
            var sbtnRt = (RectTransform)startBtn.transform;
            sbtnRt.anchorMin = new Vector2(0.5f, 0f);
            sbtnRt.anchorMax = new Vector2(0.5f, 0f);
            sbtnRt.sizeDelta = new Vector2(560f, 148f);
            sbtnRt.anchoredPosition = new Vector2(0f, 356f);
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
            var startPulse = startBtn.gameObject.AddComponent<PulseTween>();
            startPulse.speed = 8.5f;
            startPulse.amplitude = 0.012f;
            startBtn.gameObject.AddComponent<ClickScaleTween>();

            var startBase = UiBuilder.CreateRect("StartButtonBase", startBtn.transform,
                new Vector2(0f, 0f), new Vector2(1f, 0f),
                new Vector2(0f, 16f), new Vector2(0f, -6f),
                new Color(0.55f, 0.32f, 0f, 0.92f), rounded: true);
            startBase.GetComponent<Image>().raycastTarget = false;
            startBase.offsetMin = new Vector2(18f, startBase.offsetMin.y);
            startBase.offsetMax = new Vector2(-18f, startBase.offsetMax.y);
            startBase.SetAsFirstSibling();

            // 顶部高光条
            var shine = UiBuilder.CreateRect(
                "StartShine", startBtn.transform,
                new Vector2(0f, 1f), new Vector2(1f, 1f),
                new Vector2(0f, 8f), new Vector2(0f, -8f),
                new Color(1f, 1f, 1f, 0.48f), rounded: true);
            shine.GetComponent<Image>().raycastTarget = false;
            shine.offsetMin = new Vector2(24f, shine.offsetMin.y);
            shine.offsetMax = new Vector2(-24f, shine.offsetMax.y);
            shine.SetAsFirstSibling();

            var playArrow = UiBuilder.CreateTriangle("StartArrow", startBtn.transform,
                new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
                new Vector2(28f, 28f), new Vector2(42f, 4f),
                new Color(0.12f, 0.07f, 0.01f, 0.9f));
            playArrow.localRotation = Quaternion.Euler(0f, 0f, -90f);
            playArrow.GetComponent<Image>().raycastTarget = false;

            // 底部阴影条
            var btnBottomShade = UiBuilder.CreateRect(
                "StartBottomShade", startBtn.transform,
                new Vector2(0f, 0f), new Vector2(1f, 0f),
                new Vector2(0f, 8f), new Vector2(0f, 7f),
                new Color(0.6f, 0.4f, 0.05f, 0.32f), rounded: true);
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
            dailyChallengeButton = dailyBtn.transform;
            var drt = (RectTransform)dailyBtn.transform;
            drt.anchorMin = new Vector2(0.5f, 0f);
            drt.anchorMax = new Vector2(0.5f, 0f);
            drt.sizeDelta = new Vector2(380f, 76f);
            drt.anchoredPosition = new Vector2(0f, 204f);

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
            dailyBtn.transition = Selectable.Transition.None;

            UiBuilder.AddShadow(dailyBtn.gameObject, new Color(0f, 0f, 0f, 0.45f), new Vector2(0, -5f));
            UiBuilder.AddOutline(dailyBtn.gameObject, new Color(0f, 0.85f, 0.95f, 0.5f), new Vector2(1.5f, -1.5f));
            dailyBtn.gameObject.AddComponent<ClickScaleTween>();

            // 左侧图标徽章
            var dailyIcon = UiBuilder.CreateRect("DailyIcon", dailyBtn.transform,
                new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
                new Vector2(46f, 46f), new Vector2(34f, 0f),
                new Color(0f, 0.85f, 0.95f, 0.95f), circle: true);
            dailyIcon.GetComponent<Image>().raycastTarget = false;
            UiBuilder.AddOutline(dailyIcon.gameObject, new Color(0.7f, 1f, 1f, 0.6f), new Vector2(1f, -1f));
            var dailyIconGlow = UiBuilder.CreateRect("DailyIconGlow", dailyBtn.transform,
                new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
                new Vector2(68f, 68f), new Vector2(34f, 0f),
                new Color(0f, 0.85f, 0.95f, 0.12f), glow: true);
            dailyIconGlow.GetComponent<Image>().raycastTarget = false;
            dailyIconGlow.SetAsFirstSibling();
            UiBuilder.CreateText("DailyIconText", dailyIcon.transform, "\u2600", 26, FontStyle.Bold,
                TextAnchor.MiddleCenter, new Color(0.04f, 0.1f, 0.18f, 1f));

            // 主标
            var dailyTitle = UiBuilder.CreateText("DailyTitle", dailyBtn.transform,
                "今日挑战", 26, FontStyle.Bold, TextAnchor.LowerLeft, Color.white);
            var dtRt = (RectTransform)dailyTitle.transform;
            dtRt.anchorMin = new Vector2(0f, 0.5f);
            dtRt.anchorMax = new Vector2(1f, 1f);
            dtRt.offsetMin = new Vector2(76f, 0f);
            dtRt.offsetMax = new Vector2(-20f, -4f);

            // 副标
            var dailySub = UiBuilder.CreateText("DailySub", dailyBtn.transform,
                "限定关卡 · 双倍鱼干", 16, FontStyle.Normal, TextAnchor.UpperLeft,
                new Color(0.7f, 0.9f, 1f, 0.78f));
            var dsRt = (RectTransform)dailySub.transform;
            dsRt.anchorMin = new Vector2(0f, 0f);
            dsRt.anchorMax = new Vector2(1f, 0.5f);
            dsRt.offsetMin = new Vector2(76f, 4f);
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
            CreateDockButton(layoutGo.transform, "\u2726", "营地", OpenCampShop, true, "CampShop", UiBuilder.Warning);
            missionBadge = CreateDockButton(layoutGo.transform, "\u2630", "任务", OpenMissions, true, "Missions", UiBuilder.AccentCyan);
            CreateDockButton(layoutGo.transform, "\u2605", "排行", OpenLeaderboard, false, "Leaderboard", UiBuilder.WarmGold);
            CreateDockButton(layoutGo.transform, "\u25A1", "图鉴", OpenCodex, false, "Codex", new Color(0.7f, 0.55f, 0.95f, 1f));
            CreateDockButton(layoutGo.transform, "\u2699", "设置", OpenSettings, false, "Settings", new Color(0.65f, 0.72f, 0.85f, 1f));
        }

        /// <summary>底栏导航 Tab：图标 + 文字标签，选中时顶部亮条 + 文字变白变大。</summary>
        private GameObject CreateDockButton(Transform parent, string glyph, string label, System.Action onClick, bool withBadge, string panelName, Color accent)
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
            bColors.highlightedColor = new Color(accent.r, accent.g, accent.b, 0.08f);
            bColors.pressedColor = new Color(accent.r, accent.g, accent.b, 0.16f);
            bColors.selectedColor = new Color(0f, 0f, 0f, 0f);
            itemBtn.colors = bColors;
            itemBtn.targetGraphic = itemImg;
            itemGo.AddComponent<ClickScaleTween>();

            itemBtn.onClick.AddListener(() => onClick?.Invoke());

            // 顶部选中指示条（默认隐藏）
            var activeLine = UiBuilder.CreateRect("ActiveLine", itemGo.transform,
                new Vector2(0.15f, 1f), new Vector2(0.85f, 1f),
                new Vector2(0f, 3f), new Vector2(0f, -1f),
                new Color(accent.r, accent.g, accent.b, 0.92f), rounded: true);
            activeLine.GetComponent<Image>().raycastTarget = false;
            activeLine.gameObject.SetActive(false);

            // 图标（圆形背景 + 字符符号）
            var iconCircle = UiBuilder.CreateRect("IconBg", itemGo.transform,
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(66f, 66f), new Vector2(0f, -32f),
                new Color(0.12f, 0.24f, 0.42f, 0.85f), circle: true);
            iconCircle.pivot = new Vector2(0.5f, 1f);
            var iconBgImg = iconCircle.GetComponent<Image>();
            var bounce = iconCircle.gameObject.AddComponent<BounceTween>();
            bounce.enabled = false;

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
                accentColor = accent,
                wasSelected = false,
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

            var bodyColor = new Color(0.09f, 0.16f, 0.3f);
            var bellyColor = new Color(0.96f, 0.99f, 1f);
            var beakColor = new Color(1f, 0.58f, 0.22f);

            // Shadow under the penguin
            UiBuilder.CreateRect("MascotShadow", parent,
                new Vector2(0.25f, 0.05f), new Vector2(0.74f, 0.13f),
                Vector2.zero, new Vector2(0f, -2f),
                new Color(0f, 0f, 0f, 0.34f), rounded: true);

            var leftFlipper = UiBuilder.CreateRect("FlipL", parent,
                new Vector2(0.16f, 0.43f), new Vector2(0.31f, 0.63f),
                Vector2.zero, new Vector2(-4f, 2f),
                bodyColor, rounded: true);
            leftFlipper.localRotation = Quaternion.Euler(0f, 0f, 10f);
            var rightFlipper = UiBuilder.CreateRect("FlipR", parent,
                new Vector2(0.68f, 0.39f), new Vector2(0.84f, 0.58f),
                Vector2.zero, new Vector2(4f, -2f),
                bodyColor, rounded: true);
            rightFlipper.localRotation = Quaternion.Euler(0f, 0f, -12f);

            var bodyGroup = UiBuilder.CreateRect("RunnerBodyGroup", parent,
                new Vector2(0f, 0f), new Vector2(1f, 1f),
                Vector2.zero, Vector2.zero);
            bodyGroup.localRotation = Quaternion.Euler(0f, 0f, -2.5f);

            // Body - main dark blue-gray
            var body = UiBuilder.CreateRect("MascotBody", bodyGroup,
                new Vector2(0.29f, 0.16f), new Vector2(0.72f, 0.64f),
                Vector2.zero, Vector2.zero,
                bodyColor, rounded: true);

            // Belly - white/cream colored
            UiBuilder.CreateRect("MascotBelly", body,
                new Vector2(0.22f, 0.1f), new Vector2(0.78f, 0.7f),
                Vector2.zero, Vector2.zero,
                bellyColor, rounded: true);

            // Scarf - 取自所选围巾色
            UiBuilder.CreateRect("MascotScarf", bodyGroup,
                new Vector2(0.25f, 0.52f), new Vector2(0.78f, 0.62f),
                Vector2.zero, Vector2.zero,
                scarfMain, rounded: true);

            // Head - same color as body
            var head = UiBuilder.CreateRect("MascotHead", bodyGroup,
                new Vector2(0.28f, 0.58f), new Vector2(0.72f, 0.9f),
                Vector2.zero, new Vector2(3f, 0f),
                bodyColor, rounded: true);

            // Face area (white)
            UiBuilder.CreateRect("MascotFace", head,
                new Vector2(0.18f, 0.14f), new Vector2(0.82f, 0.7f),
                Vector2.zero, Vector2.zero,
                bellyColor, rounded: true);

            // Cheeks - soft pink
            UiBuilder.CreateRect("MascotCheekL", head,
                new Vector2(0.23f, 0.27f), new Vector2(0.35f, 0.39f),
                Vector2.zero, Vector2.zero,
                new Color(1f, 0.65f, 0.7f, 0.5f), circle: true);
            UiBuilder.CreateRect("MascotCheekR", head,
                new Vector2(0.66f, 0.28f), new Vector2(0.78f, 0.4f),
                Vector2.zero, Vector2.zero,
                new Color(1f, 0.65f, 0.7f, 0.5f), circle: true);

            // Eyes - white sclera
            UiBuilder.CreateRect("EyeWhiteL", head,
                new Vector2(0.32f, 0.48f), new Vector2(0.43f, 0.6f),
                Vector2.zero, Vector2.zero,
                Color.white, circle: true);
            UiBuilder.CreateRect("EyeWhiteR", head,
                new Vector2(0.57f, 0.48f), new Vector2(0.68f, 0.6f),
                Vector2.zero, Vector2.zero,
                Color.white, circle: true);

            // Pupils - dark blue
            UiBuilder.CreateRect("EyeL", head,
                new Vector2(0.36f, 0.51f), new Vector2(0.42f, 0.58f),
                Vector2.zero, Vector2.zero,
                new Color(0.12f, 0.42f, 0.88f), circle: true);
            UiBuilder.CreateRect("EyeR", head,
                new Vector2(0.6f, 0.51f), new Vector2(0.66f, 0.58f),
                Vector2.zero, Vector2.zero,
                new Color(0.12f, 0.42f, 0.88f), circle: true);

            // Eye highlights
            UiBuilder.CreateRect("EyeSparkL", head,
                new Vector2(0.38f, 0.54f), new Vector2(0.41f, 0.58f),
                Vector2.zero, Vector2.zero,
                new Color(1f, 1f, 1f, 0.9f), circle: true);
            UiBuilder.CreateRect("EyeSparkR", head,
                new Vector2(0.62f, 0.54f), new Vector2(0.65f, 0.58f),
                Vector2.zero, Vector2.zero,
                new Color(1f, 1f, 1f, 0.9f), circle: true);

            // Beak - orange
            UiBuilder.CreateRect("Beak", head,
                new Vector2(0.45f, 0.2f), new Vector2(0.56f, 0.33f),
                Vector2.zero, Vector2.zero,
                beakColor, rounded: true);
            var beakPoint = UiBuilder.CreateTriangle("BeakSmile", head,
                new Vector2(0.5f, 0.18f), new Vector2(0.5f, 0.18f),
                new Vector2(22f, 16f), Vector2.zero,
                new Color(0.75f, 0.26f, 0.08f, 0.86f));
            beakPoint.localRotation = Quaternion.Euler(0f, 0f, 180f);
            beakPoint.GetComponent<Image>().raycastTarget = false;

            // Scarf knot
            var knot = UiBuilder.CreateRect("MascotScarfKnot", bodyGroup,
                new Vector2(0.58f, 0.41f), new Vector2(0.72f, 0.54f),
                Vector2.zero, new Vector2(4f, 0f),
                scarfKnot, rounded: true);
            knot.localRotation = Quaternion.Euler(0f, 0f, -8f);

            // Hat - 取自所选帽子色
            UiBuilder.CreateRect("MascotHat", bodyGroup,
                new Vector2(0.26f, 0.85f), new Vector2(0.75f, 0.96f),
                Vector2.zero, Vector2.zero,
                hatCrown, rounded: true);
            UiBuilder.CreateRect("MascotHatBand", bodyGroup,
                new Vector2(0.28f, 0.82f), new Vector2(0.72f, 0.88f),
                Vector2.zero, Vector2.zero,
                hatBand, rounded: true);

            // Feet - orange
            var leftFoot = UiBuilder.CreateRect("FootL", parent,
                new Vector2(0.36f, 0.08f), new Vector2(0.49f, 0.15f),
                Vector2.zero, new Vector2(-5f, 5f),
                beakColor, rounded: true);
            leftFoot.localRotation = Quaternion.Euler(0f, 0f, 8f);
            var rightFoot = UiBuilder.CreateRect("FootR", parent,
                new Vector2(0.52f, 0.06f), new Vector2(0.66f, 0.13f),
                Vector2.zero, new Vector2(7f, -1f),
                beakColor, rounded: true);
            rightFoot.localRotation = Quaternion.Euler(0f, 0f, -8f);

            var run = parent.gameObject.AddComponent<RunCycleTween>();
            run.leftFlipper = leftFlipper;
            run.rightFlipper = rightFlipper;
            run.leftFoot = leftFoot;
            run.rightFoot = rightFoot;
            run.bodyGroup = bodyGroup;
        }

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
            var runTweens = mascotArea.GetComponents<RunCycleTween>();
            foreach (var tween in runTweens)
            {
                if (tween != null) Destroy(tween);
            }
            for (var i = mascotArea.childCount - 1; i >= 0; i--)
            {
                Destroy(mascotArea.GetChild(i).gameObject);
            }
            DrawPenguinMascot(mascotArea);
            SetRaycastTargets(mascotArea, false);
            if (avatarMiniArea != null) DrawMiniPenguinAvatar(avatarMiniArea);
        }

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
                {
                    item.iconBg.color = isSelected
                        ? new Color(item.accentColor.r, item.accentColor.g, item.accentColor.b, 0.3f)
                        : new Color(item.accentColor.r, item.accentColor.g, item.accentColor.b, 0.18f);

                    if (isSelected && !item.wasSelected)
                    {
                        var bounce = item.iconBg.GetComponent<BounceTween>();
                        if (bounce != null)
                        {
                            bounce.enabled = true;
                        }
                    }
                }

                // 图标符号颜色
                if (item.iconText != null)
                    item.iconText.color = isSelected
                        ? item.accentColor
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
