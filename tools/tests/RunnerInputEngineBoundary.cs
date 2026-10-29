// Only Unity's sampling/math boundary is replaced. Gesture logic is production code.
using System;
using System.Collections.Generic;
namespace UnityEngine
{
    public struct Vector2
    {
        public float x, y;
        public Vector2(float x, float y) { this.x = x; this.y = y; }
        public static Vector2 zero => new Vector2(0, 0);
        public static Vector2 operator -(Vector2 a, Vector2 b) => new Vector2(a.x-b.x, a.y-b.y);
        public static float Distance(Vector2 a, Vector2 b) { var d=a-b; return (float)Math.Sqrt(d.x*d.x+d.y*d.y); }
    }
    public static class Mathf
    {
        public static float Abs(float a) => Math.Abs(a);
        public static float Min(float a, float b) => Math.Min(a,b);
        public static float Max(float a, float b) => Math.Max(a,b);
        public static int Max(int a, int b) => Math.Max(a,b);
        public static float Clamp(float v, float a, float b) => Math.Max(a,Math.Min(v,b));
    }
    public enum TouchPhase { Began, Moved, Stationary, Ended, Canceled }
    public struct Touch { public int fingerId; public Vector2 position; public TouchPhase phase; }
    public enum KeyCode { LeftShift, RightShift, F, Space, W, UpArrow, S, DownArrow, LeftArrow, A, RightArrow, D, P, Escape }
    public static class Screen { public static int width=1920, height=1080; }
    public static class Time { public static float unscaledTime; public static int frameCount; }
    public static class Input
    {
        public static Touch[] contacts=Array.Empty<Touch>();
        public static bool mouseDown, mouseHeld, mouseUp;
        public static Vector2 mousePosition;
        public static HashSet<KeyCode> keys=new HashSet<KeyCode>();
        public static int touchCount => contacts.Length;
        public static Touch GetTouch(int i) => contacts[i];
        public static bool GetKeyDown(KeyCode key) => keys.Contains(key);
        public static bool GetMouseButtonDown(int b) => mouseDown;
        public static bool GetMouseButton(int b) => mouseHeld;
        public static bool GetMouseButtonUp(int b) => mouseUp;
        public static void Frame(params Touch[] touches)
        {
            contacts=touches; mouseDown=mouseHeld=mouseUp=false; keys.Clear();
            Time.unscaledTime+=0.05f; Time.frameCount++;
        }
    }
}
