// Standalone tests only. No Unity installation or real PlayerPrefs is accessed.
// Catalog, shop transactions and save rules are compiled from production sources.
using System;
using System.Collections.Generic;

namespace UnityEngine
{
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
        public static int Min(int a, int b) => Math.Min(a, b);
        public static int Clamp(int value, int min, int max) => Math.Clamp(value, min, max);
        public static float Clamp01(float value) => Math.Clamp(value, 0f, 1f);
        public static int RoundToInt(float value) => (int)Math.Round(value, MidpointRounding.ToEven);
    }

    public static class PlayerPrefs
    {
        private static Dictionary<string, object> values = new();
        private static Dictionary<string, object> saved = new();
        public static int Writes { get; private set; }
        public static int Saves { get; private set; }
        public static int GetInt(string key, int fallback = 0) => values.TryGetValue(key, out var v) ? (int)v : fallback;
        public static float GetFloat(string key, float fallback = 0f) => values.TryGetValue(key, out var v) ? (float)v : fallback;
        public static string GetString(string key, string fallback = "") => values.TryGetValue(key, out var v) ? (string)v : fallback;
        public static void SetInt(string key, int value) { values[key] = value; Writes++; }
        public static void SetFloat(string key, float value) { values[key] = value; Writes++; }
        public static void SetString(string key, string value) { values[key] = value; Writes++; }
        public static Dictionary<string, object> Snapshot() => new(values);
        public static void Save() { saved = new(values); Saves++; }
        public static void Reload() { values = new(saved); }
        public static void Reset() { values.Clear(); saved.Clear(); Writes = 0; Saves = 0; }
    }
}

namespace PenguinRun.Game.Save
{
    public static class RunRecords
    {
        public struct Stats { public int TotalRuns; }
        public static Stats GetStats() => throw new InvalidOperationException("Shop tests must not read run history.");
    }
}
