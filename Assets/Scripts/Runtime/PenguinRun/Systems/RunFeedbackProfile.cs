using UnityEngine;

namespace PenguinRun
{
    /// <summary>
    /// 一次局内反馈事件在各表现通道上的配方：闪屏、震屏、视野冲击、命中顿帧与振动。
    /// 纯数据。判定层只产出 <see cref="RunFeedbackKind"/>，由 <see cref="PenguinRunnerGame"/>
    /// 按本档案分发到相机 / HUD / 振动——这样「受伤到底有多重」只在一处定义。
    /// </summary>
    internal readonly struct RunFeedbackProfile
    {
        public readonly Color FlashColor;
        public readonly float FlashSeconds;
        public readonly float ShakeAmplitude;
        public readonly float ShakeSeconds;
        /// <summary>视野瞬时偏移（度）：负值收窄（撞击），正值外扩（加速 / 完美）。</summary>
        public readonly float FovKick;
        /// <summary>命中顿帧：模拟时间在该秒数内以 <see cref="HitStopTimeScale"/> 运行。</summary>
        public readonly float HitStopSeconds;
        public readonly bool Haptic;

        public const float HitStopTimeScale = 0.3f;

        public RunFeedbackProfile(Color flashColor, float flashSeconds, float shakeAmplitude = 0f,
            float shakeSeconds = 0f, float fovKick = 0f, float hitStopSeconds = 0f, bool haptic = false)
        {
            FlashColor = flashColor;
            FlashSeconds = flashSeconds;
            ShakeAmplitude = shakeAmplitude;
            ShakeSeconds = shakeSeconds;
            FovKick = fovKick;
            HitStopSeconds = hitStopSeconds;
            Haptic = haptic;
        }

        public static RunFeedbackProfile For(RunFeedbackKind kind) => kind switch
        {
            RunFeedbackKind.PerfectDodge => new RunFeedbackProfile(
                new Color(1f, 0.85f, 0.2f, 0.22f), 0.25f, fovKick: 3.5f),
            RunFeedbackKind.NearMiss => new RunFeedbackProfile(
                new Color(0.55f, 0.95f, 1f, 0.12f), 0.18f, fovKick: 2f),
            RunFeedbackKind.ObstacleDamage => new RunFeedbackProfile(
                new Color(1f, 0.35f, 0.2f, 0.4f), 0.28f, 0.28f, 0.22f, -7f, 0.14f, haptic: true),
            RunFeedbackKind.ObstacleDeath => new RunFeedbackProfile(
                new Color(1f, 0.1f, 0.05f, 0.65f), 0.45f, 0.42f, 0.3f, -10f, haptic: true),
            RunFeedbackKind.BossWarning => new RunFeedbackProfile(
                new Color(0.8f, 0.45f, 0.08f, 0.15f), 0.4f),
            RunFeedbackKind.BossHit => new RunFeedbackProfile(
                new Color(1f, 0.2f, 0.15f, 0.55f), 0.35f, 0.34f, 0.24f, -8f, 0.14f, haptic: true),
            RunFeedbackKind.BossDeath => new RunFeedbackProfile(
                new Color(1f, 0.05f, 0.02f, 0.7f), 0.6f, 0.46f, 0.32f, -10f, haptic: true),
            RunFeedbackKind.DefenseBlock => new RunFeedbackProfile(
                new Color(0.4f, 0.85f, 1f, 0.28f), 0.22f, 0.16f, 0.14f, -3f, 0.06f, haptic: true),
            RunFeedbackKind.SegmentHazard => new RunFeedbackProfile(
                new Color(1f, 0.55f, 0.35f, 0.12f), 0.35f),
            RunFeedbackKind.SegmentReward => new RunFeedbackProfile(
                new Color(1f, 0.85f, 0.4f, 0.12f), 0.35f),
            RunFeedbackKind.DashStart => new RunFeedbackProfile(
                new Color(0.7f, 0.95f, 1f, 0.08f), 0.2f, fovKick: 5f),
            _ => new RunFeedbackProfile(Color.clear, 0f),
        };
    }
}
