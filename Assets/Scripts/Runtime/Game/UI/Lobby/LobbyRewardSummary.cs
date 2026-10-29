using System;
using System.Collections.Generic;
using PenguinRun.Game.Mission;

namespace PenguinRun.Game.UI.Lobby
{
    /// <summary>Read-only reward copy from one mission-state snapshot; never grants rewards.</summary>
    public readonly struct LobbyRewardSummary
    {
        public const string JourneyRewardLabel = "沿途收集鱼干 · 旅程结束后结算";
        public const string DailyChallengeHint = "今日挑战尚未开始 · 挑战当日记录";

        private LobbyRewardSummary(int claimableCount, long fishTotal)
        {
            ClaimableCount = claimableCount;
            FishTotal = fishTotal;
        }

        public int ClaimableCount { get; }
        public long FishTotal { get; }
        public string JournalLabel => ClaimableCount > 0
            ? $"可领取 {ClaimableCount} 份任务奖励 · 共 {FishTotal} 鱼干"
            : "完成任务后可领取鱼干";

        public static LobbyRewardSummary FromMissions(IEnumerable<MissionUiState> missions)
        {
            var count = 0;
            long fish = 0;
            if (missions != null)
            {
                foreach (var state in missions)
                {
                    if (state == null || !state.Claimable || state.Definition == null) continue;
                    count++;
                    fish += Math.Max(0, state.Definition.RewardFishSnacks);
                }
            }
            return new LobbyRewardSummary(count, fish);
        }
    }
}
