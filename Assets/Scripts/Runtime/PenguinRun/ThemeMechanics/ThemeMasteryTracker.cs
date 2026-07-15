using UnityEngine;

namespace PenguinRun
{
    internal sealed class ThemeMasteryTracker
    {
        private const float ResonanceSeconds = 8f;

        private readonly RunnerMapTheme theme;
        private readonly JourneyContractDefinition contract;

        private int eventRequired;
        private int eventCompleted;
        private int eventPerfect;
        private bool eventDamaged;
        private bool eventSignature;
        private bool tutorialSeen;
        private bool showTutorial;

        private int eventsCompleted;
        private int perfectCount;
        private int flawlessCount;
        private int currentPerfectStreak;
        private int bestPerfectStreak;
        private bool signatureCompleted;
        private bool flawlessSignatureCompleted;
        private int contractProgress;
        private float resonance01;
        private float resonanceSecondsRemaining;

        public ThemeMasteryTracker(
            RunnerMapTheme theme,
            ThemeMechanicMode mode,
            int seed,
            bool tutorialSeenBeforeRun)
        {
            this.theme = theme;
            tutorialSeen = tutorialSeenBeforeRun;
            contract = JourneyContractCatalog.Select(
                mode,
                new XorShiftRunRandom(unchecked(seed ^ ((int)theme + 1) * 486187739)));
        }

        public float Resonance01 => resonance01;
        public float ResonanceSecondsRemaining => resonanceSecondsRemaining;
        public bool IsResonating => resonanceSecondsRemaining > 0f;

        public ThemeMasteryRunSnapshot RunSnapshot => new(
            theme,
            eventsCompleted,
            perfectCount,
            flawlessCount,
            bestPerfectStreak,
            signatureCompleted,
            flawlessSignatureCompleted,
            contractProgress >= contract.Target,
            contract.Kind,
            EarnedStars(),
            tutorialSeen);

        public ThemeMasteryHudSnapshot HudSnapshot => new(
            resonance01,
            resonanceSecondsRemaining,
            new JourneyContractSnapshot(contract, contractProgress),
            showTutorial);

        public void BeginEvent(int requiredKeypoints, bool signature)
        {
            eventRequired = Mathf.Max(1, requiredKeypoints);
            eventCompleted = 0;
            eventPerfect = 0;
            eventDamaged = false;
            eventSignature = signature;
            showTutorial = !tutorialSeen;
            tutorialSeen = true;
        }

        public void RecordKeypoint(bool perfect)
        {
            if (eventRequired <= 0 || eventCompleted >= eventRequired)
            {
                return;
            }

            eventCompleted++;
            if (perfect)
            {
                eventPerfect++;
            }
        }

        public void RecordDamage()
        {
            eventDamaged = true;
            currentPerfectStreak = 0;
            resonance01 = Mathf.Max(0f, resonance01 - 0.35f);
        }

        public ThemeMechanicGrade ResolveEvent(bool signature)
        {
            var grade = GradeCurrentEvent();
            if (grade == ThemeMechanicGrade.None)
            {
                ResetEvent();
                return grade;
            }

            eventsCompleted++;
            if (grade == ThemeMechanicGrade.Perfect || grade == ThemeMechanicGrade.Flawless)
            {
                perfectCount++;
                currentPerfectStreak++;
                bestPerfectStreak = Mathf.Max(bestPerfectStreak, currentPerfectStreak);
            }
            else
            {
                currentPerfectStreak = 0;
            }

            if (grade == ThemeMechanicGrade.Flawless)
            {
                flawlessCount++;
            }

            var resolvedSignature = signature || eventSignature;
            signatureCompleted |= resolvedSignature;
            flawlessSignatureCompleted |= resolvedSignature && grade == ThemeMechanicGrade.Flawless;
            AddResonance(grade switch
            {
                ThemeMechanicGrade.Completed => 0.20f,
                ThemeMechanicGrade.Perfect => 0.32f,
                ThemeMechanicGrade.Flawless => 0.45f,
                _ => 0f,
            });
            UpdateEventContract(grade, resolvedSignature);
            ResetEvent();
            return grade;
        }

        public void RecordFish(int count)
        {
            if (contract.Kind == JourneyContractKind.ResonanceFish && IsResonating)
            {
                SetContractProgress(contractProgress + Mathf.Max(0, count));
            }
        }

        public void RecordSkill(int totalSkillScore, float distance)
        {
            if (contract.Kind == JourneyContractKind.SkillBeforeDistance && distance <= 800f)
            {
                SetContractProgress(Mathf.Max(contractProgress, Mathf.Max(0, totalSkillScore)));
            }
        }

        public void Tick(float deltaTime)
        {
            if (!IsResonating)
            {
                return;
            }

            resonanceSecondsRemaining = Mathf.Max(0f, resonanceSecondsRemaining - Mathf.Max(0f, deltaTime));
            if (resonanceSecondsRemaining <= 0f)
            {
                resonance01 = 0f;
            }
        }

        private ThemeMechanicGrade GradeCurrentEvent()
        {
            if (eventCompleted <= 0)
            {
                return ThemeMechanicGrade.None;
            }
            if (eventCompleted == eventRequired && eventPerfect == eventRequired)
            {
                return eventDamaged ? ThemeMechanicGrade.Perfect : ThemeMechanicGrade.Flawless;
            }
            return ThemeMechanicGrade.Completed;
        }

        private void AddResonance(float amount)
        {
            resonance01 = Mathf.Clamp01(resonance01 + Mathf.Max(0f, amount));
            if (resonance01 >= 0.999f)
            {
                resonance01 = 1f;
                resonanceSecondsRemaining = ResonanceSeconds;
            }
        }

        private void UpdateEventContract(ThemeMechanicGrade grade, bool signature)
        {
            switch (contract.Kind)
            {
                case JourneyContractKind.PerfectStreak:
                    SetContractProgress(bestPerfectStreak);
                    break;
                case JourneyContractKind.FlawlessEvents:
                    SetContractProgress(flawlessCount);
                    break;
                case JourneyContractKind.SignatureComplete when signature:
                    SetContractProgress(1);
                    break;
            }
        }

        private void SetContractProgress(int progress)
        {
            contractProgress = Mathf.Clamp(progress, 0, contract.Target);
        }

        private int EarnedStars()
        {
            if (flawlessSignatureCompleted)
            {
                return 3;
            }
            if (perfectCount >= 5 && contractProgress >= contract.Target)
            {
                return 2;
            }
            return eventsCompleted >= 3 ? 1 : 0;
        }

        private void ResetEvent()
        {
            eventRequired = 0;
            eventCompleted = 0;
            eventPerfect = 0;
            eventDamaged = false;
            eventSignature = false;
            showTutorial = false;
        }
    }
}
