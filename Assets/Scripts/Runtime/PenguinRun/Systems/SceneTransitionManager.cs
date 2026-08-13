using System;
using System.Collections;
using PenguinRun.Game.UI;
using PenguinRun.Game.UI.Storybook;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace PenguinRun
{
    /// <summary>Persistent retained-uGUI scene transition with a storybook loading page.</summary>
    public sealed class SceneTransitionManager : MonoBehaviour
    {
        public static SceneTransitionManager Instance { get; private set; }

        private enum State { Idle, FadeOut, Loading, FadeIn }

        private State state;
        private float timer;
        private readonly float fadeOutDuration = 0.4f;
        private readonly float fadeInDuration = 0.4f;
        private string targetScene;
        private Action onReady;
        private AsyncOperation asyncLoad;
        private GameObject overlayCanvas;
        private CanvasGroup overlayGroup;
        private Image progressFill;
        private TextMeshProUGUI loadingText;
        private TextMeshProUGUI loadingHint;
        private RectTransform mascot;
        private float lastProgress = -1f;
        private float displayedProgress;
        private string[] activeHints = Array.Empty<string>();
        private int hintIndex;
        private float hintTimer;
        private const float HintRotateSeconds = 2.2f;

        private static readonly string[] ToLobbyHints =
        {
            "正在把这段旅程写回营地手记",
            "篝火已经添了柴，营地在等你",
            "图鉴里也许多了一页新发现",
        };

        private static readonly string[] ToRunHints =
        {
            "收好围巾，前方的风雪正在发亮",
            "小提示：下滑可以铲过低矮的横梁",
            "小提示：完美闪避与连续换道都能积累技巧分",
            "小提示：Boss 出招前，先看地面预警圈的颜色",
        };

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
            BuildOverlay();
        }

        public void TransitionTo(string sceneName, Action readyCallback = null)
        {
            if (state != State.Idle || string.IsNullOrWhiteSpace(sceneName)) return;

            targetScene = sceneName;
            onReady = readyCallback;
            state = State.FadeOut;
            timer = 0f;
            lastProgress = -1f;
            displayedProgress = 0f;
            activeHints = sceneName == "MainMenu" ? ToLobbyHints : ToRunHints;
            hintIndex = 0;
            hintTimer = 0f;
            loadingHint.text = activeHints[0];
            overlayCanvas.SetActive(true);
            overlayGroup.blocksRaycasts = true;
            overlayGroup.interactable = true;
        }

        public bool IsTransitioning => state != State.Idle;

        private void Update()
        {
            if (state == State.Idle) return;

            switch (state)
            {
                case State.FadeOut:
                    timer += Time.unscaledDeltaTime;
                    SetOverlayAlpha(Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(timer / fadeOutDuration)));
                    SetLoadingVisible(false);
                    if (timer >= fadeOutDuration) StartLoad();
                    break;

                case State.Loading:
                    SetOverlayAlpha(1f);
                    SetLoadingVisible(true);
                    RefreshProgress();
                    RotateHint();
                    break;

                case State.FadeIn:
                    timer += Time.unscaledDeltaTime;
                    SetLoadingVisible(false);
                    SetOverlayAlpha(1f - Mathf.SmoothStep(0f, 1f,
                        Mathf.Clamp01(timer / fadeInDuration)));
                    if (timer >= fadeInDuration) CompleteTransition();
                    break;
            }
        }

        private void BuildOverlay()
        {
            overlayCanvas = UiBuilder.CreateCanvas("StorybookLoadingCanvas", transform, 500);
            overlayGroup = overlayCanvas.AddComponent<CanvasGroup>();
            var root = UiBuilder.CreateRect("LoadingPage", overlayCanvas.transform, Vector2.zero, Vector2.one,
                Vector2.zero, Vector2.zero, UiTheme.PaperBase);

            var wash = UiBuilder.CreateRect("MistWash", root, Vector2.zero, Vector2.one,
                Vector2.zero, Vector2.zero,
                new Color(UiTheme.MistBlue.r, UiTheme.MistBlue.g, UiTheme.MistBlue.b, 0.26f));
            wash.GetComponent<Image>().raycastTarget = false;

            mascot = UiBuilder.CreateRect("Mascot", root, new Vector2(0.38f, 0.29f),
                new Vector2(0.62f, 0.71f), Vector2.zero, Vector2.zero,
                new Color(0f, 0f, 0f, 0f));
            var mascotImage = mascot.GetComponent<Image>();
            mascotImage.sprite = StorybookArtCatalog.Mascot;
            mascotImage.preserveAspect = true;
            mascotImage.raycastTarget = false;
            mascot.PlayFloat(6f, 2.8f);

            loadingText = UiBuilder.CreateTmpText("LoadingText", root, "翻开下一页…", UiTheme.H1,
                FontStyle.Bold, TextAnchor.MiddleCenter, UiTheme.InkPrimary);
            Place(loadingText.rectTransform, new Vector2(0.20f, 0.18f), new Vector2(0.80f, 0.29f));

            loadingHint = UiBuilder.CreateTmpText("LoadingHint", root, string.Empty, UiTheme.Caption,
                FontStyle.Normal, TextAnchor.MiddleCenter, UiTheme.InkSecondary);
            Place(loadingHint.rectTransform, new Vector2(0.16f, 0.10f), new Vector2(0.84f, 0.19f));

            var track = UiBuilder.CreateProgressFill("Progress", root,
                new Color(UiTheme.MistBlue.r, UiTheme.MistBlue.g, UiTheme.MistBlue.b, 0.50f),
                UiTheme.CoralAction, out progressFill);
            Place(track.rectTransform, new Vector2(0.30f, 0.075f), new Vector2(0.70f, 0.095f));

            overlayGroup.alpha = 0f;
            overlayGroup.blocksRaycasts = false;
            overlayGroup.interactable = false;
            overlayCanvas.SetActive(false);
        }

        private void StartLoad()
        {
            state = State.Loading;
            timer = 0f;
            asyncLoad = SceneManager.LoadSceneAsync(targetScene);
            if (asyncLoad == null)
            {
                Debug.LogError($"SceneTransitionManager could not load scene '{targetScene}'.");
                CompleteTransition();
                return;
            }
            asyncLoad.allowSceneActivation = true;
            StartCoroutine(WaitForSceneLoad());
        }

        private IEnumerator WaitForSceneLoad()
        {
            while (asyncLoad != null && !asyncLoad.isDone) yield return null;
            yield return null;
            state = State.FadeIn;
            timer = 0f;
        }

        private void RefreshProgress()
        {
            // 真实进度是跳变的；显示值向目标平滑推进，条子看起来在"走"而不是"蹦"。
            var target = asyncLoad == null ? 0f : Mathf.Clamp01(asyncLoad.progress / 0.9f);
            displayedProgress = Mathf.MoveTowards(displayedProgress, target, Time.unscaledDeltaTime * 1.4f);
            if (Mathf.Abs(displayedProgress - lastProgress) < 0.002f) return;
            lastProgress = displayedProgress;
            UiBuilder.SetProgress01(progressFill, displayedProgress);
        }

        private void RotateHint()
        {
            if (activeHints.Length <= 1) return;
            hintTimer += Time.unscaledDeltaTime;
            if (hintTimer < HintRotateSeconds) return;
            hintTimer = 0f;
            hintIndex = (hintIndex + 1) % activeHints.Length;
            loadingHint.text = activeHints[hintIndex];
        }

        private void SetOverlayAlpha(float alpha)
        {
            overlayGroup.alpha = Mathf.Clamp01(alpha);
        }

        private void SetLoadingVisible(bool visible)
        {
            loadingText.gameObject.SetActive(visible);
            loadingHint.gameObject.SetActive(visible);
            progressFill.transform.parent.gameObject.SetActive(visible);
            mascot.gameObject.SetActive(visible);
        }

        private void CompleteTransition()
        {
            state = State.Idle;
            overlayGroup.alpha = 0f;
            overlayGroup.blocksRaycasts = false;
            overlayGroup.interactable = false;
            overlayCanvas.SetActive(false);
            onReady?.Invoke();
            onReady = null;
            asyncLoad = null;
        }

        private static void Place(RectTransform rect, Vector2 min, Vector2 max)
        {
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }
    }
}
