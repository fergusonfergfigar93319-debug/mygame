using UnityEngine;

namespace PenguinRun
{
    internal sealed class PlayerMotor
    {
        // 角色的脚底位于 SurfaceY，身体中心在脚底上方，所以视觉偏移就是身体中心到脚底的距离
        private const float PlayerVisualCenterOffset = 0.78f;

        private readonly RunnerMovementTuning tuning;
        private readonly RunnerPowerUpTuning powerUpTuning;
        private readonly RunnerAudio audio;
        private readonly Transform player;

        public PlayerMotor(Transform player, RunnerAudio audio, RunnerMovementTuning tuning, RunnerPowerUpTuning powerUpTuning)
        {
            this.player = player;
            this.audio = audio;
            this.tuning = tuning;
            this.powerUpTuning = powerUpTuning;
            Lane = RunnerLane.Center;
            PlayerY = tuning.groundY;
            Grounded = true;
            coyoteTimer = tuning.coyoteTime;
        }

        public RunnerLane Lane { get; private set; }
        public float PlayerY { get; private set; }
        public bool Grounded { get; private set; }
        public float SlideTimer { get; private set; }
        public float LaneLean01 { get; private set; }

        /// <summary>本局已使用的二段跳次数（CloudWalk 期间允许 1 次）。</summary>
        public bool DoubleJumpUsed { get; private set; }

        /// <summary>最近一次跳跃后的剩余 perfect-dodge 检测窗口（秒）。在 0.25s 内闪过障碍则记完美闪避。</summary>
        public float PerfectActionWindow { get; private set; }

        /// <summary>最近一次执行的关键动作类型：用于判定 perfect dodge。</summary>
        public RunnerAction LastCriticalAction { get; private set; }

        private float velocityY;
        private float coyoteTimer;
        private float jumpBufferTimer;

        public void ResetToStart()
        {
            Lane = RunnerLane.Center;
            PlayerY = tuning.groundY;
            velocityY = 0f;
            SlideTimer = 0f;
            coyoteTimer = tuning.coyoteTime;
            jumpBufferTimer = 0f;
            Grounded = true;
            DoubleJumpUsed = false;
            PerfectActionWindow = 0f;
            LastCriticalAction = RunnerAction.None;
            if (player != null)
            {
                player.position = new Vector3(0f, PlayerY + PlayerVisualCenterOffset, 0f);
            }
        }

        public void ApplyAction(RunnerAction action, ref string feedbackText, ref float feedbackTimer, float glideTimerSeconds, float groundY)
        {
            switch (action)
            {
                case RunnerAction.Jump:
                    SlideTimer = 0f;
                    TryJump(ref feedbackText, ref feedbackTimer, glideTimerSeconds);
                    break;
                case RunnerAction.Slide:
                    if (PlayerY > groundY + 0.35f)
                    {
                        velocityY = -tuning.jumpVelocity * 1.5f;
                        feedbackText = "快速下落";
                        feedbackTimer = 0.45f;
                        LastCriticalAction = RunnerAction.Slide;
                        PerfectActionWindow = 0.28f;
                        audio.PlaySlide();
                    }
                    else if (Grounded)
                    {
                        SlideTimer = tuning.slideDuration;
                        feedbackText = "滑铲";
                        feedbackTimer = 0.45f;
                        LastCriticalAction = RunnerAction.Slide;
                        PerfectActionWindow = 0.32f;
                        audio.PlaySlide();
                    }
                    break;
                case RunnerAction.Left:
                    Lane = Lane.MoveLeft();
                    feedbackText = "左移";
                    feedbackTimer = 0.35f;
                    break;
                case RunnerAction.Right:
                    Lane = Lane.MoveRight();
                    feedbackText = "右移";
                    feedbackTimer = 0.35f;
                    break;
            }
        }

        public void Tick(float dt, float laneWidth, float laneLerp, ref string feedbackText, ref float feedbackTimer, float glideTimerSeconds, float groundY)
        {
            var wasGrounded = Grounded;

            velocityY -= tuning.gravity * (glideTimerSeconds > 0f && !Grounded ? powerUpTuning.glideGravityMultiplier : 1f) * dt;
            PlayerY += velocityY * dt;

            Grounded = PlayerY <= groundY + tuning.groundEpsilon;
            if (Grounded)
            {
                PlayerY = groundY;
                velocityY = 0f;
                coyoteTimer = tuning.coyoteTime;
                if (!wasGrounded)
                {
                    audio.PlayLand();
                    DoubleJumpUsed = false;
                }
            }
            else
            {
                coyoteTimer = Mathf.Max(0f, coyoteTimer - dt);
            }

            PerfectActionWindow = Mathf.Max(0f, PerfectActionWindow - dt);

            if (jumpBufferTimer > 0f)
            {
                jumpBufferTimer -= dt;
                if (Grounded)
                {
                    velocityY = tuning.jumpVelocity;
                    coyoteTimer = 0f;
                    jumpBufferTimer = 0f;
                }
            }

            SlideTimer = Mathf.Max(0f, SlideTimer - dt);

            var targetX = Lane.ToX(laneWidth);
            var pos = player.position;
            LaneLean01 = Mathf.Clamp((targetX - pos.x) / laneWidth, -1f, 1f);
            pos.x = Mathf.Lerp(pos.x, targetX, Mathf.Clamp01(laneLerp * dt));
            // 脚底在 PlayerY，身体中心在脚底上方；滑铲时身体降低
            pos.y = PlayerY + PlayerVisualCenterOffset - (SlideTimer > 0f ? 0.35f : 0f);
            player.position = pos;
        }

        private void TryJump(ref string feedbackText, ref float feedbackTimer, float glideTimerSeconds)
        {
            if (Grounded || coyoteTimer > 0f)
            {
                velocityY = tuning.jumpVelocity * (glideTimerSeconds > 0f ? powerUpTuning.glideJumpMultiplier : 1f);
                coyoteTimer = 0f;
                jumpBufferTimer = 0f;
                SlideTimer = 0f;
                feedbackText = "跳跃";
                feedbackTimer = 0.45f;
                LastCriticalAction = RunnerAction.Jump;
                PerfectActionWindow = 0.32f;
                audio.PlayJump();
            }
            else if (glideTimerSeconds > 0f && !DoubleJumpUsed)
            {
                // CloudWalk / WindRider 期间可空中二段跳
                velocityY = tuning.jumpVelocity * 0.85f;
                DoubleJumpUsed = true;
                feedbackText = "二段跳";
                feedbackTimer = 0.5f;
                LastCriticalAction = RunnerAction.Jump;
                PerfectActionWindow = 0.28f;
                audio.PlayJump();
            }
            else
            {
                jumpBufferTimer = tuning.jumpBufferTime;
                feedbackText = "预输入";
                feedbackTimer = 0.45f;
            }
        }
    }
}

