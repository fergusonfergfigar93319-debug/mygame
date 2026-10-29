using System;

namespace PenguinRun
{
    // One session authority and one pending rotation; no wall-clock catch-up debt.
    internal sealed class RunThemeSessionState
    {
        internal const float SwitchIntervalSeconds = 52f;
        public RunThemeSessionState(RunnerMapTheme openingTheme, bool daily)
        {
            if (!IsValid(openingTheme)) throw new ArgumentOutOfRangeException(nameof(openingTheme));
            OpeningTheme = CurrentTheme = openingTheme;
            IsDaily = daily;
        }
        public RunnerMapTheme OpeningTheme { get; }
        public RunnerMapTheme CurrentTheme { get; private set; }
        public bool IsDaily { get; }
        public bool SwitchPending { get; private set; }
        public float ElapsedSeconds { get; private set; }

        public void Tick(float deltaTime, bool canAdvance)
        {
            if (!canAdvance || IsDaily || SwitchPending || deltaTime <= 0f
                || float.IsNaN(deltaTime) || float.IsInfinity(deltaTime)) return;
            ElapsedSeconds = Math.Min(SwitchIntervalSeconds, ElapsedSeconds + deltaTime);
            if (ElapsedSeconds >= SwitchIntervalSeconds) RequestSwitch();
        }
        public bool RequestSwitch()
        {
            if (IsDaily || SwitchPending) return false;
            SwitchPending = true;
            return true;
        }
        public bool ApplyTheme(RunnerMapTheme theme)
        {
            if (IsDaily || !IsValid(theme) || theme == CurrentTheme) return false;
            CurrentTheme = theme;
            CancelPending();
            return true;
        }
        public RunnerMapTheme ConsumeSwitch()
        {
            if (SwitchPending) ApplyTheme(NextTheme(CurrentTheme));
            return CurrentTheme;
        }
        public void CancelPending() { SwitchPending = false; ElapsedSeconds = 0f; }
        public void Reset() { CurrentTheme = OpeningTheme; CancelPending(); }
        public static bool IsValid(RunnerMapTheme theme) => (int)theme >= 0 && (int)theme <= 5;
        public static RunnerMapTheme NextTheme(RunnerMapTheme theme) =>
            IsValid(theme) ? (RunnerMapTheme)(((int)theme + 1) % 6) : RunnerMapTheme.IceLakeEcho;
    }
}
