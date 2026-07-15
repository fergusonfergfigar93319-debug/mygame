using System;
using UnityEngine;

namespace PenguinRun
{
    public enum JourneyContractKind
    {
        PerfectStreak,
        FlawlessEvents,
        ResonanceFish,
        SignatureComplete,
        SkillBeforeDistance,
    }

    internal readonly struct JourneyContractDefinition
    {
        public JourneyContractDefinition(
            JourneyContractKind kind,
            string title,
            string description,
            int target)
        {
            Kind = kind;
            Title = title ?? string.Empty;
            Description = description ?? string.Empty;
            Target = Math.Max(1, target);
        }

        public JourneyContractKind Kind { get; }
        public string Title { get; }
        public string Description { get; }
        public int Target { get; }
    }

    internal readonly struct JourneyContractSnapshot
    {
        public JourneyContractSnapshot(JourneyContractDefinition definition, int progress)
        {
            Kind = definition.Kind;
            Title = definition.Title;
            Description = definition.Description;
            Target = definition.Target;
            Progress = Mathf.Clamp(progress, 0, Target);
        }

        public JourneyContractKind Kind { get; }
        public string Title { get; }
        public string Description { get; }
        public int Progress { get; }
        public int Target { get; }
        public bool Completed => Progress >= Target;
    }

    public readonly struct ThemeMasteryRunSnapshot
    {
        public ThemeMasteryRunSnapshot(
            RunnerMapTheme theme,
            int eventsCompleted,
            int perfectCount,
            int flawlessCount,
            int bestPerfectStreak,
            bool signatureCompleted,
            bool flawlessSignatureCompleted,
            bool contractCompleted,
            JourneyContractKind contractKind,
            int earnedMasteryStars,
            bool tutorialSeen)
        {
            Theme = theme;
            EventsCompleted = Math.Max(0, eventsCompleted);
            PerfectCount = Math.Max(0, perfectCount);
            FlawlessCount = Math.Max(0, flawlessCount);
            BestPerfectStreak = Math.Max(0, bestPerfectStreak);
            SignatureCompleted = signatureCompleted;
            FlawlessSignatureCompleted = flawlessSignatureCompleted;
            ContractCompleted = contractCompleted;
            ContractKind = contractKind;
            EarnedMasteryStars = Mathf.Clamp(earnedMasteryStars, 0, 3);
            TutorialSeen = tutorialSeen;
        }

        public RunnerMapTheme Theme { get; }
        public int EventsCompleted { get; }
        public int PerfectCount { get; }
        public int FlawlessCount { get; }
        public int BestPerfectStreak { get; }
        public bool SignatureCompleted { get; }
        public bool FlawlessSignatureCompleted { get; }
        public bool ContractCompleted { get; }
        public JourneyContractKind ContractKind { get; }
        public int EarnedMasteryStars { get; }
        public bool TutorialSeen { get; }
    }

    internal readonly struct ThemeMasteryHudSnapshot
    {
        public ThemeMasteryHudSnapshot(
            float resonance01,
            float resonanceSecondsRemaining,
            JourneyContractSnapshot contract,
            bool showTutorial)
        {
            Resonance01 = Mathf.Clamp01(resonance01);
            ResonanceSecondsRemaining = Mathf.Max(0f, resonanceSecondsRemaining);
            Contract = contract;
            ShowTutorial = showTutorial;
        }

        public float Resonance01 { get; }
        public float ResonanceSecondsRemaining { get; }
        public JourneyContractSnapshot Contract { get; }
        public bool ShowTutorial { get; }
        public bool IsResonating => ResonanceSecondsRemaining > 0f;
    }
}
