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

        /// <summary>UI 面板弹性弹出。</summary>
        public static void PlayPanelPopIn(this RectTransform rt, float duration = 0.35f)
        {
#if DOTWEEN
            rt.localScale = Vector3.one * 0.7f;
            rt.DOScale(1f, duration).SetEase(Ease.OutBack);
#endif
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
