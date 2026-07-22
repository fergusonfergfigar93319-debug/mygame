using UnityEngine;
using UnityEngine.UI;

namespace PenguinRun.Game.UI
{
    /// <summary>
    /// 大厅氛围层轻量循环动效（从 MainMenuBootstrap 内嵌类迁出）。
    /// 一次性/交互动效请优先用 TweenExtensions（DOTween）；本文件只保留
    /// 需要每帧持续演化的大气循环（漂浮/闪烁/选中弹跳）。
    /// </summary>
    internal sealed class DriftTween : MonoBehaviour
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
    internal sealed class TwinkleTween : MonoBehaviour
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

    /// <summary>一次性弹跳强调（enabled=true 触发，播完自禁用）。</summary>
    internal sealed class BounceTween : MonoBehaviour
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
}
