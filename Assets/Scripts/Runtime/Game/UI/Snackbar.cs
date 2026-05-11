using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace PenguinRun.Game.UI
{
    /// <summary>
    /// 简易 Snackbar：出现 N 秒后自动淡出；后续调用会顶替前一条。
    /// 重构：圆角卡片 + 青色边框，与 UI 设计系统一致；带轻量上滑入场动画。
    /// </summary>
    public sealed class Snackbar : MonoBehaviour
    {
        private static Snackbar instance;
        public static Snackbar Instance => instance;

        private CanvasGroup group;
        private Text label;
        private RectTransform rectTransform;
        private Vector2 baseAnchoredPos;
        private Coroutine running;

        public static Snackbar EnsureUnder(Canvas canvas)
        {
            if (instance != null) return instance;

            var go = new GameObject("Snackbar",
                typeof(RectTransform), typeof(CanvasGroup), typeof(Image));
            go.transform.SetParent(canvas.transform, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = new Vector2(0.5f, 0f);
            rt.anchorMax = new Vector2(0.5f, 0f);
            rt.pivot = new Vector2(0.5f, 0f);
            rt.sizeDelta = new Vector2(720f, 96f);
            rt.anchoredPosition = new Vector2(0f, 130f);

            var img = go.GetComponent<Image>();
            img.color = new Color(0.04f, 0.09f, 0.16f, 0.96f);
            img.sprite = UiBuilder.RoundedSprite;
            img.type = Image.Type.Sliced;
            UiBuilder.AddOutline(go, UiBuilder.BorderAccent, new Vector2(1.5f, -1.5f));
            UiBuilder.AddShadow(go, new Color(0f, 0f, 0f, 0.55f), new Vector2(0f, -6f));

            // 顶部强调条（青色发光）
            var topAccent = UiBuilder.CreateRect("TopAccent", go.transform,
                new Vector2(0f, 1f), new Vector2(1f, 1f),
                new Vector2(0f, 3f), new Vector2(0f, -1.5f),
                UiBuilder.AccentCyan, rounded: true);
            topAccent.GetComponent<Image>().raycastTarget = false;
            topAccent.offsetMin = new Vector2(20f, topAccent.offsetMin.y);
            topAccent.offsetMax = new Vector2(-20f, topAccent.offsetMax.y);

            // 左侧图标（信息圆点）
            var iconBadge = UiBuilder.CreateRect("Icon", go.transform,
                new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
                new Vector2(40f, 40f), new Vector2(40f, 0f),
                new Color(UiBuilder.AccentCyan.r, UiBuilder.AccentCyan.g, UiBuilder.AccentCyan.b, 0.22f),
                circle: true);
            UiBuilder.AddOutline(iconBadge.gameObject, UiBuilder.AccentCyan, new Vector2(1f, -1f));
            UiBuilder.CreateText("IconText", iconBadge.transform, "\u2139", 22, FontStyle.Bold,
                TextAnchor.MiddleCenter, UiBuilder.AccentCyan);

            var text = UiBuilder.CreateText(
                "Label", go.transform, string.Empty,
                26,
                FontStyle.Bold,
                TextAnchor.MiddleLeft,
                Color.white);
            var trt = (RectTransform)text.transform;
            trt.anchorMin = Vector2.zero;
            trt.anchorMax = Vector2.one;
            trt.sizeDelta = Vector2.zero;
            trt.offsetMin = new Vector2(80f, 0f);
            trt.offsetMax = new Vector2(-30f, 0f);

            instance = go.AddComponent<Snackbar>();
            instance.group = go.GetComponent<CanvasGroup>();
            instance.group.alpha = 0f;
            instance.label = text;
            instance.rectTransform = rt;
            instance.baseAnchoredPos = rt.anchoredPosition;
            return instance;
        }

        public void Show(string message, float seconds = 1.8f)
        {
            label.text = message;
            if (running != null) StopCoroutine(running);
            running = StartCoroutine(FadeRoutine(seconds));
        }

        private IEnumerator FadeRoutine(float seconds)
        {
            const float fadeIn = 0.22f;
            const float fadeOut = 0.4f;
            const float slideAmount = 36f;
            var t = 0f;

            // 入场：淡入 + 轻微上滑
            while (t < fadeIn)
            {
                t += Time.unscaledDeltaTime;
                var ratio = Mathf.Clamp01(t / fadeIn);
                var eased = 1f - Mathf.Pow(1f - ratio, 3f);
                group.alpha = eased;
                if (rectTransform != null)
                    rectTransform.anchoredPosition = baseAnchoredPos + new Vector2(0f, -slideAmount * (1f - eased));
                yield return null;
            }
            group.alpha = 1f;
            if (rectTransform != null)
                rectTransform.anchoredPosition = baseAnchoredPos;

            yield return new WaitForSecondsRealtime(Mathf.Max(0.2f, seconds));

            // 退场：淡出
            t = 0f;
            while (t < fadeOut)
            {
                t += Time.unscaledDeltaTime;
                group.alpha = 1f - Mathf.Clamp01(t / fadeOut);
                yield return null;
            }
            group.alpha = 0f;
            running = null;
        }
    }
}
