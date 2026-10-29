using UnityEngine;

namespace PenguinRun
{
    /// <summary>
    /// Frame-rate independent motion helpers. Historical runner tuning was authored
    /// against a 60fps lerp factor (<c>value * dt</c>); these conversions keep that
    /// 60fps feel while avoiding 30fps snaps and 120fps sluggishness.
    /// </summary>
    internal static class RunnerMotion
    {
        public const float ReferenceFrameRate = 60f;

        public static float Follow01(float lerpPerSecondAt60, float dt)
        {
            var lerp60 = Mathf.Clamp01(lerpPerSecondAt60 / ReferenceFrameRate);
            if (lerp60 <= 0f || dt <= 0f) return 0f;
            var rate = -Mathf.Log(Mathf.Max(1e-5f, 1f - lerp60)) * ReferenceFrameRate;
            return 1f - Mathf.Exp(-rate * dt);
        }
    }
}
