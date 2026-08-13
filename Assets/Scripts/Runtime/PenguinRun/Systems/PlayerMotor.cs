using PenguinRun.Core;
using UnityEngine;

namespace PenguinRun
{
    internal sealed class PlayerMotor
    {
        // 角色的脚底位于 SurfaceY，身体中心在脚底上方，所以视觉偏移就是身体中心到脚底的距离
        private const float PlayerVisualCenterOffset = 0.78f;

        // 蹲姿判定盒：站立/滑铲身高与滑铲结束后的起身速度（起身期间仍可从低障碍下方通过，形成宽容窗口）
        private const float StandingBodyHeight = 1.56f;
        private const float SlidingBodyHeight = 0.86f;
        private const float BodyRiseSpeed = 1.2f;
        private const float AirDiveHeight = 0.35f;

        private readonly RunnerMovementTuning tuning;
        private readonly RunnerPowerUpTuning powerUpTuning;
        private readonly RunnerCombatTuning combatTuning;
        private readonly RunnerAudio audio;
        private readonly Transform player;

        public PlayerMotor(Transform player, RunnerAudio audio, RunnerMovementTuning tuning, RunnerPowerUpTuning powerUpTuning, RunnerCombatTuning combatTuning)
        {
            this.player = player;
            this.audio = audio;
            this.tuning = tuning;
            this.powerUpTuning = powerUpTuning;
            this.combatTuning = combatTuning;
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
        public float VerticalVelocity => velocityY;

        /// <summary>当前判定身高（蹲姿盒）：滑铲时立即压低，滑铲结束后按 BodyRiseSpeed 起身。</summary>
        public float BodyHeight { get; private set; } = StandingBodyHeight;

        /// <summary>
        /// 是否处于低姿态（含滑铲后起身宽容窗）。Boss 低空招式与障碍 IsLow 共用同一蹲姿盒语义，
        /// 阈值对齐 <see cref="ObstacleColliderSpec.DefaultPassUnderMaxHeight"/>。
        /// </summary>
        public bool IsLowPosture => SlideTimer > 0f
            || BodyHeight <= ObstacleColliderSpec.DefaultPassUnderMaxHeight;

        /// <summary>本局已使用的二段跳次数（CloudWalk 期间允许 1 次）。</summary>
        public bool DoubleJumpUsed { get; private set; }

        /// <summary>最近一次跳跃/滑铲后的 perfect-dodge 检测窗口（秒）。在窗口内闪过障碍则记完美闪避。</summary>
        public float PerfectActionWindow { get; private set; }

        /// <summary>最近一次执行的关键动作类型：用于判定 perfect dodge。</summary>
        public RunnerAction LastCriticalAction { get; private set; }

        private float velocityY;
        private float coyoteTimer;
        private float jumpBufferTimer;
        private float slideBufferTimer;

        public void ResetToStart()
        {
            Lane = RunnerLane.Center;
            PlayerY = tuning.groundY;
            velocityY = 0f;
            SlideTimer = 0f;
            coyoteTimer = tuning.coyoteTime;
            jumpBufferTimer = 0f;
            slideBufferTimer = 0f;
            Grounded = true;
            DoubleJumpUsed = false;
            PerfectActionWindow = 0f;
            LastCriticalAction = RunnerAction.None;
            BodyHeight = StandingBodyHeight;
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
                    slideBufferTimer = 0f;
                    TryJump(ref feedbackText, ref feedbackTimer, glideTimerSeconds);
                    break;
                case RunnerAction.Slide:
                    if (PlayerY > groundY + AirDiveHeight)
                    {
                        // 高空下滑：急坠 + 落地滑铲缓冲（足够落到地面）。
                        // PlayerSlid 事件留给落地 BeginSlide 发出，避免同一次操作连响两次滑铲音。
                        velocityY = -tuning.jumpVelocity * 1.5f;
                        slideBufferTimer = Mathf.Max(slideBufferTimer, 0.42f);
                        feedbackText = "快速下落";
                        feedbackTimer = 0.45f;
                        LastCriticalAction = RunnerAction.Slide;
                        PerfectActionWindow = combatTuning.perfectWindowAirSlideSeconds;
                    }
                    else if (Grounded)
                    {
                        BeginSlide(ref feedbackText, ref feedbackTimer);
                    }
                    else
                    {
                        // 贴地腾空 / 提前按滑铲：与跳跃对称的预输入窗口
                        slideBufferTimer = tuning.slideBufferTime;
                        feedbackText = "预输入滑铲";
                        feedbackTimer = 0.4f;
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
            Tick(dt, laneWidth, laneLerp, ref feedbackText, ref feedbackTimer,
                glideTimerSeconds, groundY, ThemeMechanicModifiers.Identity);
        }

        public void Tick(float dt, float laneWidth, float laneLerp, ref string feedbackText,
            ref float feedbackTimer, float glideTimerSeconds, float groundY,
            in ThemeMechanicModifiers mechanic)
        {
            var wasGrounded = Grounded;

            velocityY -= tuning.gravity
                * (glideTimerSeconds > 0f && !Grounded ? powerUpTuning.glideGravityMultiplier : 1f)
                * mechanic.GravityMultiplier
                * dt;
            PlayerY += velocityY * dt;

            Grounded = PlayerY <= groundY + tuning.groundEpsilon;
            var consumedLandSlide = false;
            if (Grounded)
            {
                PlayerY = groundY;
                velocityY = 0f;
                coyoteTimer = tuning.coyoteTime;
                if (!wasGrounded)
                {
                    EventBus.Emit(GameEvents.PlayerLanded);
                    DoubleJumpUsed = false;
                }

                // 落地 / 贴地预输入：滑铲优先于过期跳跃缓冲，避免「空中下滑却被旧跳跃顶起飞」钻不过门。
                if (slideBufferTimer > 0f && SlideTimer <= 0f)
                {
                    BeginSlide(ref feedbackText, ref feedbackTimer);
                    jumpBufferTimer = 0f;
                    slideBufferTimer = 0f;
                    consumedLandSlide = true;
                }
            }
            else
            {
                coyoteTimer = Mathf.Max(0f, coyoteTimer - dt);
            }

            PerfectActionWindow = Mathf.Max(0f, PerfectActionWindow - dt);
            slideBufferTimer = Mathf.Max(0f, slideBufferTimer - dt);

            if (!consumedLandSlide && jumpBufferTimer > 0f && Grounded)
            {
                // 预输入跳跃兑现：与直接起跳同权，补完美窗与音效。
                // 先兑现再扣时——落地帧恰逢缓冲耗尽时不应吞掉这次输入。
                velocityY = tuning.jumpVelocity * (glideTimerSeconds > 0f ? powerUpTuning.glideJumpMultiplier : 1f);
                coyoteTimer = 0f;
                jumpBufferTimer = 0f;
                SlideTimer = 0f;
                slideBufferTimer = 0f;
                LastCriticalAction = RunnerAction.Jump;
                PerfectActionWindow = combatTuning.perfectWindowJumpSeconds;
                feedbackText = "跳跃";
                feedbackTimer = 0.45f;
                audio.PlayJump();
            }
            else if (jumpBufferTimer > 0f)
            {
                jumpBufferTimer = Mathf.Max(0f, jumpBufferTimer - dt);
            }

            SlideTimer = Mathf.Max(0f, SlideTimer - dt);

            // 蹲姿判定盒：滑铲立即压低；结束后逐渐起身（起身期间保留低姿态宽容）
            BodyHeight = SlideTimer > 0f
                ? SlidingBodyHeight
                : Mathf.Min(StandingBodyHeight, BodyHeight + BodyRiseSpeed * dt);

            var targetX = Lane.ToX(laneWidth);
            var pos = player.position;
            LaneLean01 = Mathf.Clamp((targetX - pos.x) / laneWidth, -1f, 1f);
            var response = laneLerp * mechanic.LaneResponseMultiplier;
            pos.x = Mathf.Lerp(pos.x, targetX, Mathf.Clamp01(response * dt));
            pos.x += mechanic.LaneDriftLanesPerSecond * laneWidth * dt;
            pos.x = Mathf.Clamp(pos.x, -laneWidth, laneWidth);
            // 脚底在 PlayerY，身体中心在脚底上方；滑铲时身体降低
            pos.y = PlayerY + PlayerVisualCenterOffset - (SlideTimer > 0f ? 0.35f : 0f);
            player.position = pos;
        }

        private void BeginSlide(ref string feedbackText, ref float feedbackTimer)
        {
            SlideTimer = tuning.slideDuration;
            feedbackText = "滑铲";
            feedbackTimer = 0.45f;
            LastCriticalAction = RunnerAction.Slide;
            PerfectActionWindow = combatTuning.perfectWindowSlideSeconds;
            EventBus.Emit(GameEvents.PlayerSlid);
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
                PerfectActionWindow = combatTuning.perfectWindowJumpSeconds;
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
                PerfectActionWindow = combatTuning.perfectWindowDoubleJumpSeconds;
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
