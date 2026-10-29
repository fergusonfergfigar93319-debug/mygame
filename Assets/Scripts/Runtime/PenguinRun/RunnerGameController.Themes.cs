using PenguinRun.Core;
using UnityEngine;

namespace PenguinRun
{
    internal sealed partial class RunnerGameController
    {
        public RunnerMapTheme CurrentTheme => themeSession.CurrentTheme;

        public bool ThemeSwitchPending => themeSession.SwitchPending;

        public float ThemeSwitchElapsedSeconds => themeSession.ElapsedSeconds;

        public bool CanApplyPendingThemeSwitch(float bossDistanceGuard = 180f)
        {
            if (!themeSession.SwitchPending || config.Daily || IsThemeTransition
                || stateMachine.Current != GameStateMachine.State.Running
                || world.Paused || waitingForGuidedReaction || upgradeOfferPending)
                return false;
            if (bossSystem.BossActive || bossSystem.HasSpawnedPrelude
                || bossSystem.IsBossApproaching(world.Distance, bossDistanceGuard))
                return false;
            if (routeEventDirector.AwaitingChoice || spawner.RouteEncounterActive)
                return false;
            if (mechanicRuntime.Snapshot.Phase != ThemeMechanicPhase.Idle
                && mechanicRuntime.Snapshot.Phase != ThemeMechanicPhase.Cancelled)
                return false;
            return !spawner.HasOwnedBossRewardEntities;
        }

        private void TickThemeSwitchClock(float deltaTime)
        {
            themeSession.Tick(deltaTime, stateMachine.Current == GameStateMachine.State.Running
                && !world.Paused && !waitingForGuidedReaction && !upgradeOfferPending && !bossSystem.BossActive
                && !bossSystem.HasSpawnedPrelude && !IsThemeTransition);
        }

        public bool TryApplyCurrentTheme(RunnerMapTheme nextTheme)
        {
            // Commit only under the transition mask after every owner released its content.
            if (!IsThemeTransition || !themeSession.SwitchPending || config.Daily
                || !RunThemeSessionState.IsValid(nextTheme) || nextTheme == CurrentTheme
                || !bossSystem.SetTheme(nextTheme)) return false;
            if (!themeSession.ApplyTheme(nextTheme)) return false;
            routeEventDirector.SetTheme(nextTheme, world.Distance);
            masterySession.SetTheme(nextTheme, world.Distance);
            mechanicRuntime.Cancel(ThemeMechanicCancelReason.ThemeTransition);
            mechanicRuntime.Tick(0f);
            mechanicSpawner.Clear();
            // Spawn heights must use the newly committed terrain profile.
            TrackTerrain.SetThemeProfile(nextTheme);
            spawner.ApplyTheme(nextTheme, clearSpawnedEntities: true);
            segmentTrackingActive = false;
            return true;
        }

        private void CancelMechanicForWorldAuthority()
        {
            var activeBeforeCancel = mechanicRuntime.Snapshot.Phase != ThemeMechanicPhase.Idle
                && mechanicRuntime.Snapshot.Phase != ThemeMechanicPhase.Cancelled;
            if (bossSystem.BossActive || bossSystem.HasSpawnedPrelude
                || bossSystem.IsBossApproaching(world.Distance, 120f))
            {
                mechanicRuntime.Cancel(ThemeMechanicCancelReason.Boss);
            }
            else if (RouteOwnsTrack)
            {
                mechanicRuntime.Cancel(ThemeMechanicCancelReason.RouteEvent);
            }
            else if (IsThemeTransition)
            {
                mechanicRuntime.Cancel(ThemeMechanicCancelReason.ThemeTransition);
            }

            if (activeBeforeCancel && mechanicRuntime.Snapshot.Phase == ThemeMechanicPhase.Cancelled)
            {
                themeMastery.CancelEvent();
                mechanicSpawner.Clear();
                spawner.ClearMechanicVisuals();
                mechanicKeypointIndex = 0;
            }
        }

        private void TryStartThemeMechanic()
        {
            if (RouteOwnsTrack || bossSystem.HasSpawnedPrelude || bossSystem.IsBossApproaching(world.Distance, 120f)
                || themeSession.SwitchPending
                || mechanicRuntime.Snapshot.Phase != ThemeMechanicPhase.Idle)
            {
                return;
            }

            var context = new ThemeMechanicContext(
                CurrentTheme,
                config.ThemeMechanicMode,
                world.Distance,
                world.Lives,
                secondsSinceDamage,
                bossSystem.BossActive,
                RouteOwnsTrack,
                IsThemeTransition,
                Mathf.Clamp01((int)config.Difficulty / 3f));
            if (!mechanicDirector.TrySchedule(context, out var request))
            {
                return;
            }

            var pattern = ThemeMechanicPatternCatalog.For(request.Definition.Id, request.Signature);
            if (pattern.Length == 0 || !mechanicRuntime.Begin(request))
            {
                return;
            }

            mechanicKeypointIndex = 0;
            mechanicSpawner.Begin(pattern, world.Distance);
            themeMastery.BeginEvent(pattern.Length, request.Signature);
            for (var index = 0; index < pattern.Length; index++)
            {
                var beat = pattern[index];
                spawner.SpawnMechanicObstacle(
                    beat.ApparatusPath,
                    beat.Lane,
                    30f + beat.Seconds * 6f,
                    world.Distance);
            }
        }

        private void EvaluateMechanicKeypoints()
        {
            var request = mechanicRuntime.CurrentRequest;
            var pattern = ThemeMechanicPatternCatalog.For(
                request.Definition.Id, request.Signature);
            while (mechanicKeypointIndex < pattern.Length
                && pattern[mechanicKeypointIndex].Seconds <= mechanicRuntime.PhaseElapsedSeconds)
            {
                var beat = pattern[mechanicKeypointIndex];
                var laneCorrect = motor.Lane == beat.Lane;
                var actionCorrect = (!beat.RequiresJump || !motor.Grounded || lastRunnerAction == RunnerAction.Jump)
                    && (!beat.RequiresSlide || motor.SlideTimer > 0f || lastRunnerAction == RunnerAction.Slide);
                var perfect = laneCorrect && actionCorrect;
                mechanicRuntime.RecordKeypoint(perfect);
                themeMastery.RecordKeypoint(perfect);
                if (perfect && beat.Rewarding)
                {
                    AwardSkill(SkillAction.NoHitSegment, 90);
                }
                mechanicKeypointIndex++;
            }
        }

        private void ResolveThemeMechanic()
        {
            var grade = themeMastery.ResolveEvent(mechanicRuntime.CurrentRequest.Signature);
            mechanicSpawner.Clear();
            spawner.ClearMechanicVisuals();
            mechanicKeypointIndex = 0;
            if (grade != ThemeMechanicGrade.None)
            {
                feedbackText = grade switch
                {
                    ThemeMechanicGrade.Flawless => "环境应对 · 无伤",
                    ThemeMechanicGrade.Perfect => "环境应对 · 完美",
                    _ => "环境应对 · 完成",
                } + themeMastery.ConsumeResonanceBanner();
                feedbackTimer = 1.2f;
            }
        }

        private void RecordThemeDamage()
        {
            secondsSinceDamage = 0f;
            themeMastery.RecordDamage();
        }

    }
}
