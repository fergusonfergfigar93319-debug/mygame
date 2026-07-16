using UnityEngine;

namespace PenguinRun
{
    internal readonly struct RunnerGestureThresholds
    {
        public RunnerGestureThresholds(float swipePixels, float tapPixels, float doubleTapPixels)
        {
            SwipePixels = swipePixels;
            TapPixels = tapPixels;
            DoubleTapPixels = doubleTapPixels;
        }

        public float SwipePixels { get; }
        public float TapPixels { get; }
        public float DoubleTapPixels { get; }

        public static RunnerGestureThresholds ForViewport(int width, int height)
        {
            var basis = Mathf.Max(1f, Mathf.Min(width, height));
            return new RunnerGestureThresholds(
                Mathf.Clamp(basis * 0.054f, 40f, 88f),
                Mathf.Clamp(basis * 0.020f, 16f, 32f),
                Mathf.Clamp(basis * 0.048f, 36f, 78f));
        }
    }

    internal static class RunnerGestureClassifier
    {
        private const float DominantAxisRatio = 1.12f;
        private const float DoubleTapMaxGapSeconds = 0.3f;

        public static RunnerAction ClassifySwipe(Vector2 delta, RunnerGestureThresholds thresholds)
        {
            var absX = Mathf.Abs(delta.x);
            var absY = Mathf.Abs(delta.y);
            if (absX >= thresholds.SwipePixels && absX > absY * DominantAxisRatio)
                return delta.x > 0f ? RunnerAction.Right : RunnerAction.Left;
            if (absY >= thresholds.SwipePixels && absY > absX * DominantAxisRatio)
                return delta.y > 0f ? RunnerAction.Jump : RunnerAction.Slide;
            return RunnerAction.None;
        }

        public static bool IsTap(Vector2 delta, RunnerGestureThresholds thresholds)
        {
            return Mathf.Abs(delta.x) < thresholds.TapPixels &&
                   Mathf.Abs(delta.y) < thresholds.TapPixels;
        }

        public static bool IsDoubleTap(float distancePixels, float elapsedSeconds,
            RunnerGestureThresholds thresholds)
        {
            return elapsedSeconds >= 0f && elapsedSeconds <= DoubleTapMaxGapSeconds &&
                   distancePixels <= thresholds.DoubleTapPixels;
        }

        public static bool IsTwoFingerPause(int touchCount, bool anyTouchBegan)
        {
            return touchCount >= 2 && anyTouchBegan;
        }
    }
}
