// Math-only Unity boundary; all planning, reservation and director logic is production.
using System;
namespace UnityEngine
{
    public static class Mathf
    {
        public static float Max(float a, float b) => Math.Max(a, b);
        public static float Min(float a, float b) => Math.Min(a, b);
        public static int Max(int a, int b) => Math.Max(a, b);
        public static float Clamp01(float value) => Math.Max(0f, Math.Min(1f, value));
        public static float InverseLerp(float a, float b, float v) => a == b ? 0f : Clamp01((v-a)/(b-a));
    }
}
