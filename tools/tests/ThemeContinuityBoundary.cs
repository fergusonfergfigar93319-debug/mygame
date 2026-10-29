// Only Unity mathematics are replaced; all session/director logic is production.
using System;
namespace UnityEngine
{
    public readonly struct Color
    {
        public Color(float r, float g, float b, float a) { }
        public static Color clear => default;
    }
    public static class Random { public static float value => 0.1f; }
    public static class Debug
    {
        public static void LogWarning(object message) { }
    }
    public static class Mathf
    {
        public static float Max(float a, float b) => Math.Max(a, b);
        public static int Max(int a, int b) => Math.Max(a, b);
        public static float Min(float a, float b) => Math.Min(a, b);
        public static float Clamp(float v, float a, float b) => Math.Clamp(v, a, b);
        public static int Clamp(int v, int a, int b) => Math.Clamp(v, a, b);
        public static float Clamp01(float v) => Math.Clamp(v, 0f, 1f);
        public static float InverseLerp(float a, float b, float v) => a == b ? 0f : Clamp01((v-a)/(b-a));
        public static int RoundToInt(float v) => (int)Math.Round(v);
    }
}
