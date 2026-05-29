using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace PenguinRun
{
    /// <summary>
    /// 场景过渡管理器：IMGUI 渐变覆盖层 + 异步加载，替代直接 LoadScene。
    /// DontDestroyOnLoad 单例，在两个场景间共享。
    /// </summary>
    public sealed class SceneTransitionManager : MonoBehaviour
    {
        public static SceneTransitionManager Instance { get; private set; }

        private enum State { Idle, FadeOut, Loading, FadeIn }
        private State state;
        private float timer;
        private float fadeOutDuration = 0.4f;
        private float fadeInDuration = 0.4f;
        private string targetScene;
        private Action onReady;
        private AsyncOperation asyncLoad;
        private GUIStyle loadingStyle;
        private GUIStyle tipStyle;
        private bool hasInit;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        /// <summary>带回调的场景过渡。</summary>
        public void TransitionTo(string sceneName, Action readyCallback = null)
        {
            if (state != State.Idle) return;

            targetScene = sceneName;
            onReady = readyCallback;
            state = State.FadeOut;
            timer = 0f;
        }

        /// <summary>是否有过渡正在进行中。</summary>
        public bool IsTransitioning => state != State.Idle;

        private void OnGUI()
        {
            if (state == State.Idle) return;

            InitStyles();

            float alpha;
            switch (state)
            {
                case State.FadeOut:
                    timer += Time.unscaledDeltaTime;
                    alpha = Mathf.Clamp01(timer / fadeOutDuration);
                    alpha = Mathf.SmoothStep(0f, 1f, alpha);
                    DrawOverlay(alpha, null);
                    if (timer >= fadeOutDuration)
                    {
                        StartLoad();
                    }
                    break;

                case State.Loading:
                    DrawOverlay(1f, "加载中…");
                    break;

                case State.FadeIn:
                    timer += Time.unscaledDeltaTime;
                    alpha = Mathf.Clamp01(timer / fadeInDuration);
                    alpha = 1f - Mathf.SmoothStep(0f, 1f, alpha);
                    DrawOverlay(alpha, null);
                    if (timer >= fadeInDuration)
                    {
                        CompleteTransition();
                    }
                    break;
            }
        }

        private void InitStyles()
        {
            if (hasInit) return;
            hasInit = true;

            loadingStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 32,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = new Color(0.78f, 0.97f, 1f, 0.9f) },
            };

            tipStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 18,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = new Color(0.78f, 0.97f, 1f, 0.55f) },
            };
        }

        private void DrawOverlay(float alpha, string text)
        {
            GUI.color = new Color(0.02f, 0.06f, 0.12f, alpha);
            GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), Texture2D.whiteTexture);
            GUI.color = Color.white;

            if (!string.IsNullOrEmpty(text))
            {
                var labelColor = loadingStyle.normal.textColor;
                loadingStyle.normal.textColor = new Color(labelColor.r, labelColor.g, labelColor.b, alpha);
                GUI.Label(new Rect(0f, Screen.height * 0.44f, Screen.width, 48f), text, loadingStyle);

                // 旋转的点动画
                var dots = Mathf.FloorToInt(Time.unscaledTime * 1.5f) % 4;
                var dotStr = new string('.', dots);
                var tipAlpha = alpha * 0.6f;
                tipStyle.normal.textColor = new Color(0.78f, 0.97f, 1f, tipAlpha);
                GUI.Label(new Rect(0f, Screen.height * 0.52f, Screen.width, 28f), dotStr, tipStyle);
            }
        }

        private void StartLoad()
        {
            state = State.Loading;
            timer = 0f;
            asyncLoad = SceneManager.LoadSceneAsync(targetScene);
            asyncLoad.allowSceneActivation = true;
            StartCoroutine(WaitForSceneLoad());
        }

        private IEnumerator WaitForSceneLoad()
        {
            if (asyncLoad == null) yield break;

            while (!asyncLoad.isDone)
            {
                yield return null;
            }

            // 场景加载完成后等待一帧确保 Awake 执行
            yield return null;

            state = State.FadeIn;
            timer = 0f;
        }

        private void CompleteTransition()
        {
            state = State.Idle;
            onReady?.Invoke();
            onReady = null;
            asyncLoad = null;
        }
    }
}
