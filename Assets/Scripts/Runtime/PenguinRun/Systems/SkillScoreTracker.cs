using UnityEngine;

namespace PenguinRun
{
    internal enum SkillAction
    {
        PerfectDodge,
        LaneChain,
        AirCollection,
        NoHitSegment,
        RiskRoute,
        BossCounter,
        /// <summary>擦身而过：不跳不铲、仅靠车道位置以极小间距掠过障碍。</summary>
        NearMiss,
    }

    internal readonly struct SkillScoreSnapshot
    {
        public readonly int TotalScore;
        public readonly int Chain;
        public readonly int PeakChain;
        public readonly float Multiplier;
        public readonly float RemainingSeconds;
        public readonly string LastLabel;

        public SkillScoreSnapshot(
            int totalScore,
            int chain,
            int peakChain,
            float multiplier,
            float remainingSeconds,
            string lastLabel)
        {
            TotalScore = Mathf.Max(0, totalScore);
            Chain = Mathf.Max(0, chain);
            PeakChain = Mathf.Max(0, peakChain);
            Multiplier = Mathf.Clamp(multiplier, 1f, 3f);
            RemainingSeconds = Mathf.Max(0f, remainingSeconds);
            LastLabel = lastLabel ?? string.Empty;
        }
    }

    internal sealed class SkillScoreTracker
    {
        internal const float ChainWindowSeconds = 2.5f;
        internal const float MaxMultiplier = 3f;

        private float remainingSeconds;
        private int chain;
        private int peakChain;
        private int totalScore;
        private string lastLabel = string.Empty;

        public SkillScoreSnapshot Snapshot => new(
            totalScore,
            chain,
            peakChain,
            CurrentMultiplier,
            remainingSeconds,
            lastLabel);

        public int Record(SkillAction action, int baseScore)
        {
            if (baseScore <= 0) return 0;

            chain = remainingSeconds > 0f && chain > 0 ? chain + 1 : 1;
            peakChain = Mathf.Max(peakChain, chain);
            remainingSeconds = ChainWindowSeconds;
            lastLabel = LabelFor(action);
            var awarded = Mathf.Max(1, Mathf.RoundToInt(baseScore * CurrentMultiplier));
            totalScore += awarded;
            return awarded;
        }

        public void Tick(float dt)
        {
            remainingSeconds = Mathf.Max(0f, remainingSeconds - Mathf.Max(0f, dt));
            if (remainingSeconds <= 0f)
            {
                chain = 0;
            }
        }

        public void BreakChain()
        {
            remainingSeconds = 0f;
            chain = 0;
        }

        public void Reset()
        {
            remainingSeconds = 0f;
            chain = 0;
            peakChain = 0;
            totalScore = 0;
            lastLabel = string.Empty;
        }

        private float CurrentMultiplier => Mathf.Min(MaxMultiplier, 1f + Mathf.Max(0, chain - 1) * 0.25f);

        private static string LabelFor(SkillAction action) => action switch
        {
            SkillAction.PerfectDodge => "完美闪避",
            SkillAction.LaneChain => "连续换道",
            SkillAction.AirCollection => "空中收集",
            SkillAction.NoHitSegment => "无伤片段",
            SkillAction.RiskRoute => "冒险路线",
            SkillAction.BossCounter => "Boss反击",
            SkillAction.NearMiss => "擦身而过",
            _ => "技巧动作",
        };
    }
}
