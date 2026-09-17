using PenguinRun.Game.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PenguinRun
{
    /// <summary>
    /// 跑酷全屏表现层（保留式 uGUI）：闪屏、Boss 边缘预警、死亡红幕、开局倒计时与主题切换幕布。
    /// 取代了原先 <c>OnGUI</c> 里的四段 IMGUI 绘制——IMGUI 每帧至少跑两遍（Layout + Repaint），
    /// 闪屏计时器在绘制里递减导致衰减速度随事件次数漂移，且字体/缩放与其余 uGUI 不一致。
    /// 所有写入都按值门控：颜色量化到 1/255、缩放/透明度量化到 1/100，不变不写。
    /// 不拥有任何玩法状态。
    /// </summary>
    internal sealed class RunnerScreenOverlay
    {
        private const int SortingOrder = 200; // HUD 170 之上，死亡卡 240 / 札记 260 之下

        private readonly GameObject canvasObject;
        private readonly Image flash;
        private readonly Image hold;
        private readonly Image warnTop;
        private readonly Image warnBottom;
        private readonly GameObject countdownRoot;
        private readonly TextMeshProUGUI countdownText;
        private readonly TextMeshProUGUI goText;
        private readonly GameObject transitionRoot;
        private readonly Image transitionShade;
        private readonly TextMeshProUGUI transitionLabel;

        private Color flashColor = Color.clear;
        private float flashTimer;
        private float flashDuration;
        private Color32 lastFlash;
        private Color32 lastHold;
        private bool warnPending;
        private float warnIntensity;
        private Color32 lastWarn;
        private int lastCountdownNumber = int.MinValue;
        private int lastCountdownScaleCents = -1;
        private int lastCountdownAlphaCents = -1;
        private bool goVisible;
        private int lastGoScaleCents = -1;
        private int lastGoAlphaCents = -1;
        private int lastTransitionAlphaCents = -1;
        private int lastTransitionLabelAlphaCents = -1;
        private string lastTransitionLabel;

        private RunnerScreenOverlay(GameObject canvasObject, Image flash, Image hold, Image warnTop, Image warnBottom,
            GameObject countdownRoot, TextMeshProUGUI countdownText, TextMeshProUGUI goText,
            GameObject transitionRoot, Image transitionShade, TextMeshProUGUI transitionLabel)
        {
            this.canvasObject = canvasObject;
            this.flash = flash;
            this.hold = hold;
            this.warnTop = warnTop;
            this.warnBottom = warnBottom;
            this.countdownRoot = countdownRoot;
            this.countdownText = countdownText;
            this.goText = goText;
            this.transitionRoot = transitionRoot;
            this.transitionShade = transitionShade;
            this.transitionLabel = transitionLabel;
        }

        public static RunnerScreenOverlay Create()
        {
            var canvasObject = UiBuilder.CreateCanvas("RunnerScreenOverlayCanvas", null, SortingOrder);
            // 整层不接收点击：闪屏盖在 HUD 之上，绝不能吞掉暂停按钮的触摸。
            if (canvasObject.TryGetComponent<GraphicRaycaster>(out var raycaster))
                Object.Destroy(raycaster);

            var root = UiBuilder.CreateRect("Overlay", canvasObject.transform, Vector2.zero, Vector2.one,
                Vector2.zero, Vector2.zero);

            var hold = FullScreenImage("DeathHold", root);
            var flash = FullScreenImage("Flash", root);
            var warnTop = FullScreenImage("BossWarnTop", root);
            warnTop.rectTransform.anchorMin = new Vector2(0f, 0.88f);
            var warnBottom = FullScreenImage("BossWarnBottom", root);
            warnBottom.rectTransform.anchorMax = new Vector2(1f, 0.12f);

            var countdownRoot = UiBuilder.CreateRect("Countdown", root, new Vector2(0.5f, 0.6f), new Vector2(0.5f, 0.6f),
                new Vector2(600f, 260f), Vector2.zero);
            var countdownText = UiBuilder.CreateTmpText("Number", countdownRoot, "3", 168, FontStyle.Bold,
                TextAnchor.MiddleCenter, Color.white);
            Stretch(countdownText.rectTransform);
            UiBuilder.AddShadow(countdownText.gameObject, new Color(0f, 0.05f, 0.12f, 0.6f), new Vector2(0f, -6f));
            var goText = UiBuilder.CreateTmpText("Go", countdownRoot, "出发！", 112, FontStyle.Bold,
                TextAnchor.MiddleCenter, new Color(0.34f, 1f, 0.82f, 1f));
            Stretch(goText.rectTransform);
            UiBuilder.AddShadow(goText.gameObject, new Color(0f, 0.05f, 0.12f, 0.6f), new Vector2(0f, -6f));
            goText.gameObject.SetActive(false);
            countdownRoot.gameObject.SetActive(false);

            var transitionRoot = UiBuilder.CreateRect("ThemeTransition", root, Vector2.zero, Vector2.one,
                Vector2.zero, Vector2.zero);
            var transitionShade = FullScreenImage("Shade", transitionRoot);
            var transitionLabel = UiBuilder.CreateTmpText("Label", transitionRoot, string.Empty, 42, FontStyle.Bold,
                TextAnchor.MiddleCenter, new Color(0.78f, 0.97f, 1f, 0f));
            transitionLabel.rectTransform.anchorMin = new Vector2(0f, 0.5f);
            transitionLabel.rectTransform.anchorMax = new Vector2(1f, 0.5f);
            transitionLabel.rectTransform.sizeDelta = new Vector2(0f, 80f);
            transitionLabel.rectTransform.anchoredPosition = Vector2.zero;
            transitionRoot.gameObject.SetActive(false);

            return new RunnerScreenOverlay(canvasObject, flash, hold, warnTop, warnBottom,
                countdownRoot.gameObject, countdownText, goText,
                transitionRoot.gameObject, transitionShade, transitionLabel);
        }

        /// <summary>一次性闪屏：alpha 按 ease-out 二次曲线从给定值衰减到 0。</summary>
        public void Flash(Color color, float seconds)
        {
            if (seconds <= 0f || color.a <= 0f) return;
            // 同帧多次触发时保留更醒目的那一次，避免弱提示盖掉受伤红闪。
            if (flashTimer > 0f && flashColor.a * (flashTimer / Mathf.Max(0.01f, flashDuration)) > color.a) return;
            flashColor = color;
            flashDuration = seconds;
            flashTimer = seconds;
        }

        /// <summary>持续幕布（死亡红幕）：传 <see cref="Color.clear"/> 释放。</summary>
        public void SetHold(Color color) => Apply(hold, color, ref lastHold);

        public void SetBossWarning(bool pending, float intensity)
        {
            warnPending = pending;
            warnIntensity = intensity;
            if (!pending) Apply(warnTop, warnBottom, Color.clear, ref lastWarn);
        }

        /// <summary>倒计时数字；<paramref name="tInStep"/> 为该数字已展示的归一化时长（0→1）。</summary>
        public void ShowCountdown(int number, float tInStep)
        {
            if (!countdownRoot.activeSelf) countdownRoot.SetActive(true);
            if (goVisible) { goText.gameObject.SetActive(false); goVisible = false; }
            if (!countdownText.gameObject.activeSelf) countdownText.gameObject.SetActive(true);
            if (number != lastCountdownNumber)
            {
                lastCountdownNumber = number;
                countdownText.text = CountdownLabel(number);
            }
            var t = Mathf.Clamp01(tInStep);
            // 入场 0.18 段回弹放大，其余时间缓慢收缩淡出——数字「落下来」而不是凭空出现。
            var pop = t < 0.18f ? Mathf.Lerp(1.35f, 1.05f, t / 0.18f) : Mathf.Lerp(1.05f, 0.92f, (t - 0.18f) / 0.82f);
            ApplyScale(countdownText.rectTransform, pop, ref lastCountdownScaleCents);
            ApplyAlpha(countdownText, 1f - t * 0.55f, ref lastCountdownAlphaCents);
        }

        public void ShowGo(float t01)
        {
            if (!countdownRoot.activeSelf) countdownRoot.SetActive(true);
            if (countdownText.gameObject.activeSelf) countdownText.gameObject.SetActive(false);
            if (!goVisible) { goText.gameObject.SetActive(true); goVisible = true; }
            var t = Mathf.Clamp01(t01);
            ApplyScale(goText.rectTransform, 1f + t * 0.35f, ref lastGoScaleCents);
            ApplyAlpha(goText, 1f - t * t, ref lastGoAlphaCents);
        }

        public void HideCountdown()
        {
            if (countdownRoot.activeSelf) countdownRoot.SetActive(false);
            lastCountdownNumber = int.MinValue;
        }

        /// <summary>主题切换幕布：<paramref name="alpha01"/> 为遮罩强度（0 = 隐藏）。</summary>
        public void SetThemeTransition(float alpha01, string label)
        {
            if (alpha01 <= 0f)
            {
                if (transitionRoot.activeSelf) transitionRoot.SetActive(false);
                lastTransitionAlphaCents = -1;
                lastTransitionLabelAlphaCents = -1;
                return;
            }
            if (!transitionRoot.activeSelf) transitionRoot.SetActive(true);
            var cents = Mathf.RoundToInt(Mathf.Clamp01(alpha01) * 100f);
            if (cents != lastTransitionAlphaCents)
            {
                lastTransitionAlphaCents = cents;
                transitionShade.color = new Color(0.02f, 0.06f, 0.12f, cents / 100f);
            }
            if (!ReferenceEquals(label, lastTransitionLabel) && label != lastTransitionLabel)
            {
                lastTransitionLabel = label;
                transitionLabel.text = label ?? string.Empty;
            }
            ApplyAlpha(transitionLabel, Mathf.Clamp01(alpha01 / 0.82f) * 0.92f, ref lastTransitionLabelAlphaCents);
        }

        /// <summary>每帧推进闪屏与预警脉冲。传真实时间，暂停/顿帧时表现层照常衰减。</summary>
        public void Tick(float unscaledDt)
        {
            if (flashTimer > 0f)
            {
                flashTimer = Mathf.Max(0f, flashTimer - unscaledDt);
                var t = flashTimer / Mathf.Max(0.01f, flashDuration);
                var alpha = flashColor.a * t * t; // ease-out quad
                Apply(flash, new Color(flashColor.r, flashColor.g, flashColor.b, alpha), ref lastFlash);
            }
            else if (lastFlash.a != 0)
            {
                Apply(flash, Color.clear, ref lastFlash);
            }

            if (warnPending)
            {
                var pulse = Mathf.Sin(Time.unscaledTime * 6f) * 0.5f + 0.5f;
                Apply(warnTop, warnBottom, new Color(0.8f, 0.15f, 0.08f, warnIntensity * pulse * 0.22f), ref lastWarn);
            }
        }

        public void Reset()
        {
            flashTimer = 0f;
            flashDuration = 0f;
            flashColor = Color.clear;
            Apply(flash, Color.clear, ref lastFlash);
            SetHold(Color.clear);
            SetBossWarning(false, 0f);
            HideCountdown();
            SetThemeTransition(0f, null);
        }

        public void Dispose()
        {
            if (canvasObject != null) Object.Destroy(canvasObject);
        }

        private static Image FullScreenImage(string name, Transform parent)
        {
            var rect = UiBuilder.CreateRect(name, parent, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, Color.clear);
            var image = rect.GetComponent<Image>();
            image.raycastTarget = false;
            return image;
        }

        private static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.sizeDelta = Vector2.zero;
            rt.anchoredPosition = Vector2.zero;
        }

        private static void Apply(Image image, Color color, ref Color32 last)
        {
            Color32 quantized = color;
            if (quantized.r == last.r && quantized.g == last.g && quantized.b == last.b && quantized.a == last.a) return;
            last = quantized;
            image.color = color;
        }

        private static void Apply(Image a, Image b, Color color, ref Color32 last)
        {
            Color32 quantized = color;
            if (quantized.r == last.r && quantized.g == last.g && quantized.b == last.b && quantized.a == last.a) return;
            last = quantized;
            a.color = color;
            b.color = color;
        }

        private static void ApplyScale(RectTransform rt, float scale, ref int lastCents)
        {
            var cents = Mathf.RoundToInt(scale * 100f);
            if (cents == lastCents) return;
            lastCents = cents;
            rt.localScale = Vector3.one * (cents / 100f);
        }

        private static void ApplyAlpha(TextMeshProUGUI text, float alpha, ref int lastCents)
        {
            var cents = Mathf.RoundToInt(Mathf.Clamp01(alpha) * 100f);
            if (cents == lastCents) return;
            lastCents = cents;
            var c = text.color;
            text.color = new Color(c.r, c.g, c.b, cents / 100f);
        }

        private static string CountdownLabel(int n) => n switch
        {
            1 => "1",
            2 => "2",
            3 => "3",
            _ => n.ToString(),
        };
    }
}
