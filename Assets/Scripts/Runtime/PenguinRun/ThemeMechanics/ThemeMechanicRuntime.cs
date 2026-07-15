using UnityEngine;

namespace PenguinRun
{
    internal sealed class ThemeMechanicRuntime
    {
        private const float ResolveSeconds = 0.75f;
        private const float CooldownSeconds = 2f;

        private ThemeMechanicRequest request;
        private float phaseTimer;
        private bool clearCancellationOnTick;

        public ThemeMechanicSnapshot Snapshot { get; private set; } = ThemeMechanicSnapshot.Empty;
        public ThemeMechanicCancelReason LastCancelReason { get; private set; }
        public int CompletedKeypoints { get; private set; }
        public int PerfectKeypoints { get; private set; }

        public bool Begin(ThemeMechanicRequest nextRequest)
        {
            if (Snapshot.Phase != ThemeMechanicPhase.Idle || !nextRequest.IsValid)
            {
                return false;
            }

            request = nextRequest;
            phaseTimer = 0f;
            clearCancellationOnTick = false;
            LastCancelReason = ThemeMechanicCancelReason.None;
            CompletedKeypoints = 0;
            PerfectKeypoints = 0;
            SetPhase(ThemeMechanicPhase.Telegraph);
            return true;
        }

        public void Tick(float deltaTime)
        {
            if (Snapshot.Phase == ThemeMechanicPhase.Cancelled)
            {
                if (clearCancellationOnTick)
                {
                    FinishToIdentity();
                }
                return;
            }

            if (Snapshot.Phase == ThemeMechanicPhase.Idle)
            {
                return;
            }

            phaseTimer += Mathf.Max(0f, deltaTime);
            for (var transition = 0; transition < 6; transition++)
            {
                var duration = DurationFor(Snapshot.Phase);
                if (duration <= 0f || phaseTimer < duration)
                {
                    break;
                }

                phaseTimer -= duration;
                AdvancePhase();
                if (Snapshot.Phase == ThemeMechanicPhase.Idle)
                {
                    return;
                }
            }

            RefreshSnapshot();
        }

        public void Cancel(ThemeMechanicCancelReason reason)
        {
            if (Snapshot.Phase == ThemeMechanicPhase.Idle
                || Snapshot.Phase == ThemeMechanicPhase.Cancelled)
            {
                return;
            }

            LastCancelReason = reason;
            phaseTimer = 0f;
            clearCancellationOnTick = true;
            Snapshot = new ThemeMechanicSnapshot(
                request.Definition.Id,
                ThemeMechanicPhase.Cancelled,
                request.Intensity,
                request.Definition.DisplayName,
                string.Empty,
                0f,
                ThemeMechanicModifiers.Identity,
                false);
        }

        public void RecordKeypoint(bool perfect)
        {
            if (Snapshot.Phase != ThemeMechanicPhase.Active)
            {
                return;
            }

            CompletedKeypoints++;
            if (perfect)
            {
                PerfectKeypoints++;
            }
        }

        private static ThemeMechanicModifiers ModifiersFor(
            ThemeMechanicId id,
            ThemeMechanicIntensity intensity)
        {
            var strength = intensity switch
            {
                ThemeMechanicIntensity.Low => 0.65f,
                ThemeMechanicIntensity.High => 1f,
                ThemeMechanicIntensity.Signature => 1f,
                _ => 0.82f,
            };

            return id switch
            {
                ThemeMechanicId.IceInertia => new ThemeMechanicModifiers(
                    1f - 0.28f * strength, 0f, 1f, 1f, 1f),
                ThemeMechanicId.CedarCollapse => ThemeMechanicModifiers.Identity,
                ThemeMechanicId.AuroraPolarity => new ThemeMechanicModifiers(
                    1f, 0.12f * strength, 1f, 1f, 1f),
                ThemeMechanicId.MistBeacon => new ThemeMechanicModifiers(
                    1f, 0f, 1f, 1f, 1f - 0.55f * strength),
                ThemeMechanicId.OceanTide => new ThemeMechanicModifiers(
                    1f, -0.10f * strength, 1.08f, 1f, 1f),
                ThemeMechanicId.SkyWind => new ThemeMechanicModifiers(
                    1f, 0.14f * strength, 0.74f, 1f + 0.55f * strength, 1f),
                _ => ThemeMechanicModifiers.Identity,
            };
        }

        private void AdvancePhase()
        {
            switch (Snapshot.Phase)
            {
                case ThemeMechanicPhase.Telegraph:
                    SetPhase(ThemeMechanicPhase.Active);
                    break;
                case ThemeMechanicPhase.Active:
                    SetPhase(ThemeMechanicPhase.Resolve);
                    break;
                case ThemeMechanicPhase.Resolve:
                    SetPhase(ThemeMechanicPhase.Reward);
                    break;
                case ThemeMechanicPhase.Reward:
                    SetPhase(ThemeMechanicPhase.Cooldown);
                    break;
                case ThemeMechanicPhase.Cooldown:
                    FinishToIdentity();
                    break;
            }
        }

        private void SetPhase(ThemeMechanicPhase phase)
        {
            phaseTimer = 0f;
            Snapshot = BuildSnapshot(phase);
        }

        private void RefreshSnapshot()
        {
            Snapshot = BuildSnapshot(Snapshot.Phase);
        }

        private ThemeMechanicSnapshot BuildSnapshot(ThemeMechanicPhase phase)
        {
            var duration = DurationFor(phase);
            var progress = duration <= 0f ? 0f : Mathf.Clamp01(phaseTimer / duration);
            var modifiers = phase == ThemeMechanicPhase.Active
                ? ModifiersFor(request.Definition.Id, request.Intensity)
                : ThemeMechanicModifiers.Identity;
            var visible = phase == ThemeMechanicPhase.Telegraph
                || phase == ThemeMechanicPhase.Active
                || phase == ThemeMechanicPhase.Reward;
            return new ThemeMechanicSnapshot(
                request.Definition.Id,
                phase,
                request.Intensity,
                request.Definition.DisplayName,
                request.Definition.ActionHint,
                progress,
                modifiers,
                visible);
        }

        private float DurationFor(ThemeMechanicPhase phase)
        {
            return phase switch
            {
                ThemeMechanicPhase.Telegraph => request.Definition.TelegraphSeconds,
                ThemeMechanicPhase.Active => request.Definition.ActiveSeconds,
                ThemeMechanicPhase.Resolve => ResolveSeconds,
                ThemeMechanicPhase.Reward => request.Definition.RewardSeconds,
                ThemeMechanicPhase.Cooldown => CooldownSeconds,
                _ => 0f,
            };
        }

        private void FinishToIdentity()
        {
            request = ThemeMechanicRequest.None;
            phaseTimer = 0f;
            clearCancellationOnTick = false;
            CompletedKeypoints = 0;
            PerfectKeypoints = 0;
            Snapshot = ThemeMechanicSnapshot.Empty;
        }
    }
}
