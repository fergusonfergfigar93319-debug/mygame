#if DOTWEEN
using DG.Tweening;
#endif
using UnityEngine;

namespace PenguinRun.Game.UI
{
    /// <summary>
    /// 统一 Tween 入口：DOTween 可用时使用高性能补间，否则回退到简单的手动动画。
    /// 安装 DOTween 后（免费 .unitypackage 或 Asset Store），DOTween Setup 会自动添加 DOTWEEN 编译符号。
    /// </summary>
    public static class TweenExtensions
    {
        /// <summary>Transform 弹性缩放弹跳（按钮点击反馈）。</summary>
        public static void PlayClickBounce(this Transform t, float scale = 0.92f, float duration = 0.3f)
        {
#if DOTWEEN
            t.DOPunchScale(Vector3.one * 0.1f, duration, 4, 0.5f);
#else
            if (t.TryGetComponent<ClickScaleFallback>(out var fb))
                fb.Trigger(duration);
#endif
        }

        /// <summary>相机震动（位置 + 旋转）。</summary>
        public static void PlayCameraShake(this Camera cam, float duration, float intensity, int vibrato = 30)
        {
#if DOTWEEN
            cam.DOShakePosition(duration, intensity, vibrato, 90f, true, ShakeRandomnessMode.Full);
#else
            // 回退到手动震动（PenguinRunnerGame 中原有逻辑）
            Debug.LogWarning($"CameraShake fallback: {duration}s @ {intensity}");
#endif
        }

        /// <summary>UI 元素脉冲缩放（循环呼吸效果）。</summary>
        public static void PlayPulse(this Transform t, float amplitude = 0.03f, float period = 1.2f)
        {
#if DOTWEEN
            t.DOScale(1f + amplitude, period * 0.5f)
                .SetLoops(-1, LoopType.Yoyo)
                .SetEase(Ease.InOutSine);
#endif
        }

        /// <summary>UI 面板弹性弹出：缩放 + 淡入，暂停时也照常播放。</summary>
        public static void PlayPanelPopIn(this RectTransform rt, float duration = 0.3f)
        {
#if DOTWEEN
            rt.localScale = Vector3.one * 0.92f;
            rt.DOScale(1f, duration).SetEase(Ease.OutBack, 1.2f).SetUpdate(true);
            var group = EnsureCanvasGroup(rt);
            group.alpha = 0f;
            group.DOFade(1f, duration * 0.7f).SetUpdate(true);
#endif
        }

        /// <summary>
        /// UI 面板退场：淡出 + 轻微收缩后回调（通常用于 Destroy/SetActive(false)）。
        /// 播放期间关闭交互，避免退场半途还能点到按钮。DOTween 不可用时立即回调。
        /// </summary>
        public static void PlayPanelPopOut(this RectTransform rt, System.Action onComplete, float duration = 0.16f)
        {
#if DOTWEEN
            if (rt == null)
            {
                onComplete?.Invoke();
                return;
            }
            var group = EnsureCanvasGroup(rt);
            group.interactable = false;
            group.blocksRaycasts = false;
            rt.DOScale(0.95f, duration).SetEase(Ease.InCubic).SetUpdate(true);
            group.DOFade(0f, duration).SetUpdate(true)
                .OnComplete(() => onComplete?.Invoke());
#else
            onComplete?.Invoke();
#endif
        }

        /// <summary>
        /// 列表行入场：前若干行按序号错峰淡入 + 轻缩放（一次性）。
        /// 只做 scale/alpha，不动位置——行位置归 LayoutGroup 管。
        /// </summary>
        public static void PlayListItemEnter(this RectTransform rt, int index, int maxStaggered = 10)
        {
#if DOTWEEN
            if (rt == null || index >= maxStaggered) return;
            var group = EnsureCanvasGroup(rt);
            group.alpha = 0f;
            rt.localScale = Vector3.one * 0.97f;
            var delay = index * 0.045f;
            group.DOFade(1f, 0.22f).SetDelay(delay).SetUpdate(true);
            rt.DOScale(1f, 0.26f).SetDelay(delay).SetEase(Ease.OutCubic).SetUpdate(true);
#endif
        }

        /// <summary>
        /// 数字滚动（count-up）：从 0 滚到 target，只在整数值变化帧回调（调用方直接写 text 即满足门控）。
        /// DOTween 不可用时立即回调最终值。
        /// </summary>
        public static void PlayCountUp(int target, float duration, System.Action<int> onValue)
        {
#if DOTWEEN
            if (onValue == null) return;
            var last = int.MinValue;
            var current = 0f;
            DOTween.To(() => current, v =>
                {
                    current = v;
                    var rounded = Mathf.RoundToInt(v);
                    if (rounded == last) return;
                    last = rounded;
                    onValue(rounded);
                }, target, duration)
                .SetEase(Ease.OutCubic)
                .SetUpdate(true);
#else
            onValue?.Invoke(target);
#endif
        }

        private static CanvasGroup EnsureCanvasGroup(Component c)
        {
            if (!c.TryGetComponent<CanvasGroup>(out var group))
                group = c.gameObject.AddComponent<CanvasGroup>();
            return group;
        }

        /// <summary>文字淡入弹出。</summary>
        public static void PlayFeedbackPopup(this Transform t, float duration = 0.5f)
        {
#if DOTWEEN
            t.localScale = Vector3.one * 0.5f;
            t.DOScale(1f, duration).SetEase(Ease.OutBack);
#endif
        }

        /// <summary>漂浮动画（持续上下浮动）。</summary>
        public static void PlayFloat(this Transform t, float amplitude = 8f, float period = 2.5f)
        {
#if DOTWEEN
            t.DOLocalMoveY(t.localPosition.y + amplitude, period * 0.5f)
                .SetLoops(-1, LoopType.Yoyo)
                .SetEase(Ease.InOutSine);
#endif
        }
    }

    /// <summary>
    /// 按压即时反馈：PointerDown 压到 0.96、PointerUp 回弹。点击弹跳发生在 onClick 之后，
    /// 手指按下的第一帧没有任何反馈——这个组件补上那 100ms 的"手感"。
    /// </summary>
    internal sealed class PressScaleFeedback : MonoBehaviour,
        UnityEngine.EventSystems.IPointerDownHandler, UnityEngine.EventSystems.IPointerUpHandler
    {
        private const float PressedScale = 0.96f;

        public void OnPointerDown(UnityEngine.EventSystems.PointerEventData eventData)
        {
#if DOTWEEN
            transform.DOKill();
            transform.DOScale(PressedScale, 0.06f).SetUpdate(true);
#else
            transform.localScale = Vector3.one * PressedScale;
#endif
        }

        public void OnPointerUp(UnityEngine.EventSystems.PointerEventData eventData)
        {
#if DOTWEEN
            transform.DOKill();
            transform.DOScale(1f, 0.14f).SetEase(Ease.OutBack).SetUpdate(true);
#else
            transform.localScale = Vector3.one;
#endif
        }
    }

    // ── DOTween 不可用时的轻量回退 ────────────────────────
    internal class ClickScaleFallback : MonoBehaviour
    {
        private Vector3 baseScale;
        private float timer;
        private float duration;
        private bool active;

        private void Start() => baseScale = transform.localScale;

        public void Trigger(float dur = 0.3f)
        {
            duration = dur;
            timer = duration;
            active = true;
            baseScale = transform.localScale;
        }

        private void Update()
        {
            if (!active) return;
            timer -= Time.deltaTime;
            var t = Mathf.Clamp01(1f - timer / duration);
            var s = Mathf.Sin(t * Mathf.PI);
            transform.localScale = baseScale * (1f - s * 0.1f);
            if (timer <= 0f) { transform.localScale = baseScale; active = false; }
        }
    }
}
