// Only Unity metadata/math/storage and unrelated history are replaced.
// Config, runtime mapping, upgrade rules and PlayerSave are production code.
using System;
using System.Collections.Generic;

namespace UnityEngine
{
    public sealed class HeaderAttribute : Attribute { public HeaderAttribute(string text) { } }
    public sealed class TooltipAttribute : Attribute { public TooltipAttribute(string text) { } }
    public sealed class RangeAttribute : Attribute { public RangeAttribute(float min, float max) { } }
    public struct Color
    {
        public float r, g, b, a;
        public Color(float r, float g, float b, float a = 1f)
        { this.r = r; this.g = g; this.b = b; this.a = a; }
        public static Color white => new Color(1f, 1f, 1f);
    }
    public static class Mathf
    {
        public static int Max(int a, int b) => Math.Max(a, b);
        public static int Clamp(int value, int min, int max) => Math.Clamp(value, min, max);
        public static float Clamp01(float value) => Math.Clamp(value, 0f, 1f);
        public static int RoundToInt(float value) => (int)Math.Round(value, MidpointRounding.ToEven);
    }
    public static class PlayerPrefs
    {
        private static readonly Dictionary<string, object> Values = new();
        public static int Writes { get; private set; }
        public static int Saves { get; private set; }
        public static int GetInt(string key, int fallback = 0) => Values.TryGetValue(key, out var v) ? (int)v : fallback;
        public static float GetFloat(string key, float fallback = 0f) => Values.TryGetValue(key, out var v) ? (float)v : fallback;
        public static string GetString(string key, string fallback = "") => Values.TryGetValue(key, out var v) ? (string)v : fallback;
        public static void SetInt(string key, int value) { Values[key] = value; Writes++; }
        public static void SetFloat(string key, float value) { Values[key] = value; Writes++; }
        public static void SetString(string key, string value) { Values[key] = value; Writes++; }
        public static void Save() { Saves++; }
        public static void Reset() { Values.Clear(); Writes = 0; Saves = 0; }
    }
}

namespace PenguinRun.Game.Save
{
    public static class RunRecords
    {
        public struct Stats { public int TotalRuns; }
        public static Stats GetStats() => throw new InvalidOperationException("Preview must not read run history.");
    }
}
