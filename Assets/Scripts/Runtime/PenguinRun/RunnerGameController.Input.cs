using UnityEngine;

namespace PenguinRun
{
    // Input reset and manual dash requests stay separate from the world loop.
    internal sealed partial class RunnerGameController
    {
        public void ResetInput()
        {
            input.Reset();
            motor.ClearBufferedActions();
            dashBufferTimer = 0f;
            lastRunnerAction = RunnerAction.None;
        }

        private void TriggerManualDash(ref string feedbackText, ref float feedbackTimer)
        {
            if (manualDashCooldownTimer > 0f)
            {
                dashBufferTimer = 0.16f;
                return;
            }

            const float touchDashSeconds = 1.2f;
            world.DashTimer = Mathf.Max(world.DashTimer, touchDashSeconds);
            manualDashCooldownTimer = ManualDashCooldownSeconds;
            dashBufferTimer = 0f;
            feedbackText = "冲刺！";
            feedbackTimer = 0.65f;
            EmitRunFeedback(RunFeedbackKind.DashStart);
        }
    }
}
