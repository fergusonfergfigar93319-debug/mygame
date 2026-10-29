using PenguinRun.Core;
using UnityEngine;

namespace PenguinRun
{
    internal sealed partial class RunnerGameController
    {
        private bool RouteOwnsTrack => routeEventDirector.AwaitingChoice || spawner.RouteEncounterActive;

        private float RouteBeatSpacing(RouteSegmentPattern pattern)
        {
            var maxSpeed = tuning.world.maxSpeed * Mathf.Max(1f, tuning.world.dashSpeedMultiplier);
            var recovery = tuning.movement.slideDuration;
            if (pattern == RouteSegmentPattern.JumpArc)
            {
                recovery = 2f * tuning.movement.jumpVelocity / tuning.movement.gravity;
                // A feather already ahead can be collected before the first route obstacle.
                if (world.GlideTimer > 0f || spawner.HasPendingGlidePickup)
                    recovery *= tuning.powerUp.glideJumpMultiplier / tuning.powerUp.glideGravityMultiplier;
            }
            return Mathf.Max(tuning.world.spawnSpacingMax, maxSpeed * (recovery + 0.2f));
        }

        private void TickRouteScheduling()
        {
            if (bossSystem.BossActive || bossSystem.HasSpawnedPrelude || IsThemeTransition
                || bossSystem.IsBossApproaching(world.Distance, 120f))
            {
                CancelRoutes();
                return;
            }
            if (themeSession.SwitchPending && !routeEventDirector.AwaitingChoice
                && !spawner.RouteEncounterActive)
            {
                return;
            }
            if (routeEventDirector.AwaitingChoice)
            {
                routeEventDirector.Tick(world.Distance);
                return;
            }
            if (spawner.RouteEncounterActive || stateMachine.Current != GameStateMachine.State.Running
                || bossSystem.IsBossApproaching(world.Distance, 180f)) return;
            if (routeEventDirector.Tick(world.Distance)
                && !spawner.SpawnRouteEvent(routeEventDirector.Snapshot, world.Distance))
                routeEventDirector.Cancel(world.Distance);
        }

        private void TickRouteChoice()
        {
            if (!routeEventDirector.AwaitingChoice) return;
            if (spawner.TryConsumeRouteChoice(motor.Lane, out var choseRisk))
                ApplyRouteEventOutcome(routeEventDirector.Choose(choseRisk, world.Distance));
            else if (!spawner.HasRouteChoiceMarker)
                ApplyRouteEventOutcome(routeEventDirector.Expire(world.Distance));
        }

        private void CancelRoutes()
        {
            routeEventDirector.Cancel(world.Distance);
            spawner.CancelRouteEncounter();
        }

        private void CancelRoutesForState(GameStateMachine.State state)
        {
            if (state is GameStateMachine.State.ThemeTransition or GameStateMachine.State.DeathSequence
                or GameStateMachine.State.GameOver or GameStateMachine.State.Finished or GameStateMachine.State.Idle)
                CancelRoutes();
        }

        private void ApplyRouteEventOutcome(RouteEventResolution resolution)
        {
            if (!resolution.IsValid) return;

            var companionMul = resolution.Kind == RouteEventKind.CompanionRescue
                ? runUpgradeState.CompanionRewardMultiplier
                : 1f;
            var fishReward = Mathf.RoundToInt(
                resolution.Outcome.FishReward * runUpgradeState.FishRewardMultiplier * companionMul);
            var scoreReward = Mathf.RoundToInt(
                resolution.Outcome.ScoreReward * runUpgradeState.RouteRewardMultiplier * companionMul);
            world.AddRouteEventReward(fishReward, scoreReward);
            spawner.SpawnRouteEventOutcome(resolution, world.Distance, RouteBeatSpacing(resolution.Outcome.SegmentPattern));
            if (resolution.Risky)
            {
                AwardSkill(SkillAction.RiskRoute, 140);
            }
            feedbackText = resolution.Risky
                ? $"冒险路线 · {resolution.Outcome.Label}  +{scoreReward}分"
                : $"安心路线 · {resolution.Outcome.Label}  +{fishReward}鱼干";
            feedbackTimer = 1.5f;
            runnerAudio.PlayCheckpoint();

            if (runUpgradeState.CanSelect &&
                (routeEventDirector.CompletedCount == 1 ||
                 (routeEventDirector.CompletedCount - 1) % 2 == 0))
            {
                upgradeOfferPending = true;
            }
        }
    }
}
