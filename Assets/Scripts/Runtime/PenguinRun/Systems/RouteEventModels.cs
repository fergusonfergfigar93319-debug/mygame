using UnityEngine;

namespace PenguinRun
{
    internal enum RouteEventKind
    {
        RiskFork,
        CompanionRescue,
        RelicCache,
        WeatherTrial,
        BossChase,
        SafeExit,
    }

    internal enum RouteSegmentPattern
    {
        CalmLane,
        Slalom,
        JumpArc,
        SlideTunnel,
        FishRibbon,
        ChaseLine,
    }

    internal readonly struct RouteEventOutcome
    {
        public readonly string Label;
        public readonly int ScoreReward;
        public readonly int FishReward;
        public readonly RouteSegmentPattern SegmentPattern;

        public RouteEventOutcome(
            string label,
            int scoreReward,
            int fishReward,
            RouteSegmentPattern segmentPattern)
        {
            Label = label ?? string.Empty;
            ScoreReward = Mathf.Max(0, scoreReward);
            FishReward = Mathf.Max(0, fishReward);
            SegmentPattern = segmentPattern;
        }
    }

    internal readonly struct RouteEventDefinition
    {
        public readonly string Id;
        public readonly RouteEventKind Kind;
        public readonly string Title;
        public readonly string SafeLabel;
        public readonly string RiskLabel;
        public readonly RouteEventOutcome SafeOutcome;
        public readonly RouteEventOutcome RiskOutcome;

        public RouteEventDefinition(
            string id,
            RouteEventKind kind,
            string title,
            string safeLabel,
            string riskLabel,
            RouteEventOutcome safeOutcome,
            RouteEventOutcome riskOutcome)
        {
            Id = id ?? string.Empty;
            Kind = kind;
            Title = title ?? string.Empty;
            SafeLabel = safeLabel ?? string.Empty;
            RiskLabel = riskLabel ?? string.Empty;
            SafeOutcome = safeOutcome;
            RiskOutcome = riskOutcome;
        }
    }

    internal readonly struct RouteEventSnapshot
    {
        public readonly string Id;
        public readonly RouteEventKind Kind;
        public readonly string Title;
        public readonly string SafeLabel;
        public readonly string RiskLabel;
        public readonly float Progress01;
        public readonly bool AwaitingChoice;

        public RouteEventSnapshot(
            string id,
            RouteEventKind kind,
            string title,
            string safeLabel,
            string riskLabel,
            float progress01,
            bool awaitingChoice)
        {
            Id = id ?? string.Empty;
            Kind = kind;
            Title = title ?? string.Empty;
            SafeLabel = safeLabel ?? string.Empty;
            RiskLabel = riskLabel ?? string.Empty;
            Progress01 = Mathf.Clamp01(progress01);
            AwaitingChoice = awaitingChoice;
        }

        public static RouteEventSnapshot Empty => new(
            string.Empty,
            RouteEventKind.SafeExit,
            string.Empty,
            string.Empty,
            string.Empty,
            0f,
            false);
    }

    internal readonly struct RouteEventResolution
    {
        public readonly bool IsValid;
        public readonly string EventId;
        public readonly RouteEventKind Kind;
        public readonly bool Risky;
        public readonly RouteEventOutcome Outcome;

        public RouteEventResolution(
            bool isValid,
            string eventId,
            RouteEventKind kind,
            bool risky,
            RouteEventOutcome outcome)
        {
            IsValid = isValid;
            EventId = eventId ?? string.Empty;
            Kind = kind;
            Risky = risky;
            Outcome = outcome;
        }

        public static RouteEventResolution None => new(
            false,
            string.Empty,
            RouteEventKind.SafeExit,
            false,
            default);
    }
}
