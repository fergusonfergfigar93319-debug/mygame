namespace PenguinRun
{
    /// <summary>
    /// Competence-driven first-run onboarding. Four demonstration bits + a run-count fuse.
    /// Pure C# so editor gates can instantiate it without a scene.
    /// </summary>
    public sealed class OnboardingState
    {
        public const int ForceGraduateAfterRuns = 3;

        private bool laneHintShown;
        private bool jumpHintShown;
        private bool slideHintShown;
        private bool collectHintShown;

        public OnboardingState(
            bool laneDemonstrated,
            bool jumpDemonstrated,
            bool slideDemonstrated,
            bool collectDemonstrated)
        {
            LaneDemonstrated = laneDemonstrated;
            JumpDemonstrated = jumpDemonstrated;
            SlideDemonstrated = slideDemonstrated;
            CollectDemonstrated = collectDemonstrated;
        }

        public bool LaneDemonstrated { get; private set; }
        public bool JumpDemonstrated { get; private set; }
        public bool SlideDemonstrated { get; private set; }
        public bool CollectDemonstrated { get; private set; }

        public bool AllDemonstrated =>
            LaneDemonstrated && JumpDemonstrated && SlideDemonstrated && CollectDemonstrated;

        public bool IsOnboarding(int totalRuns) =>
            totalRuns < ForceGraduateAfterRuns && !AllDemonstrated;

        public void Record(RunnerAction action, bool laneChanged)
        {
            if (laneChanged)
                LaneDemonstrated = true;

            switch (action)
            {
                case RunnerAction.Jump:
                    JumpDemonstrated = true;
                    break;
                case RunnerAction.Slide:
                    SlideDemonstrated = true;
                    break;
            }
        }

        public void RecordCollect() => CollectDemonstrated = true;

        public bool TryConsumePatternHint(SegmentPatternKind kind, out string message)
        {
            message = "";
            switch (kind)
            {
                case SegmentPatternKind.LaneWeave when !LaneDemonstrated && !laneHintShown:
                    laneHintShown = true;
                    message = "左右滑动换道！";
                    return true;
                case SegmentPatternKind.JumpArc when !JumpDemonstrated && !jumpHintShown:
                    jumpHintShown = true;
                    message = "上滑跳跃！";
                    return true;
                case SegmentPatternKind.SlideTunnel when !SlideDemonstrated && !slideHintShown:
                    slideHintShown = true;
                    message = "下滑铲从横梁下穿过！";
                    return true;
                case SegmentPatternKind.RewardRun when !CollectDemonstrated && !collectHintShown:
                    collectHintShown = true;
                    message = "收集沿途的鱼干！";
                    return true;
                default:
                    return false;
            }
        }

        public bool TryConsumeSlideOpportunityHint(out string message)
        {
            message = "";
            if (SlideDemonstrated || slideHintShown) return false;
            slideHintShown = true;
            message = "下滑铲从横梁下穿过！";
            return true;
        }

        public static OnboardingState Fresh() => new(false, false, false, false);

        public static OnboardingState LoadFromSave()
        {
            return new OnboardingState(
                Game.Save.PlayerSave.OnboardingLaneDemonstrated,
                Game.Save.PlayerSave.OnboardingJumpDemonstrated,
                Game.Save.PlayerSave.OnboardingSlideDemonstrated,
                Game.Save.PlayerSave.OnboardingCollectDemonstrated);
        }

        public void Persist()
        {
            Game.Save.PlayerSave.OnboardingLaneDemonstrated = LaneDemonstrated;
            Game.Save.PlayerSave.OnboardingJumpDemonstrated = JumpDemonstrated;
            Game.Save.PlayerSave.OnboardingSlideDemonstrated = SlideDemonstrated;
            Game.Save.PlayerSave.OnboardingCollectDemonstrated = CollectDemonstrated;
        }

        /// <summary>
        /// Marks every competence bit so a veteran save never re-enters onboarding
        /// after schema migration.
        /// </summary>
        public static void MarkAllDemonstratedInSave()
        {
            Game.Save.PlayerSave.OnboardingLaneDemonstrated = true;
            Game.Save.PlayerSave.OnboardingJumpDemonstrated = true;
            Game.Save.PlayerSave.OnboardingSlideDemonstrated = true;
            Game.Save.PlayerSave.OnboardingCollectDemonstrated = true;
        }
    }
}
