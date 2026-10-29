using System;
using System.Collections;
using System.Collections.Generic;
using PenguinRun.Game.Mission;
using PenguinRun.Game.UI.Lobby;

namespace PenguinRun.Tests
{
    public static class LobbyRewardSummaryRegression
    {
        private static readonly List<string> Failures = new();
        public static int CaseCount { get; private set; }

        private static void Case(string name, Action test)
        {
            CaseCount++;
            try { test(); }
            catch (Exception e) { Failures.Add(name + ": " + e.GetBaseException().Message); }
        }

        private static void Check(bool condition, string message)
        { if (!condition) throw new Exception(message); }

        private static MissionUiState State(MissionDefinition definition, bool completed = true, bool claimed = false)
            => new()
            {
                Definition = definition,
                Progress = completed ? definition.Target : 0,
                Completed = completed,
                Claimed = claimed,
                Claimable = completed && !claimed,
            };

        private static void Expect(LobbyRewardSummary summary, int count, long fish)
        {
            Check(summary.ClaimableCount == count, $"expected {count} claimable, got {summary.ClaimableCount}");
            Check(summary.FishTotal == fish, $"expected {fish} fish, got {summary.FishTotal}");
            var expectedLabel = count > 0
                ? $"可领取 {count} 份任务奖励 · 共 {fish} 鱼干"
                : "完成任务后可领取鱼干";
            Check(summary.JournalLabel == expectedLabel, "label does not match the actual claimable amount");
        }

        public static string[] Run()
        {
            Failures.Clear();
            CaseCount = 0;
            Case("null input has no promised payout", () =>
                Expect(LobbyRewardSummary.FromMissions(null), 0, 0));
            Case("empty snapshot has no promised payout", () =>
                Expect(LobbyRewardSummary.FromMissions(Array.Empty<MissionUiState>()), 0, 0));
            Case("default summary retains the neutral empty label", () =>
                Expect(default, 0, 0));
            Case("incomplete mission is not claimable", () =>
                Expect(LobbyRewardSummary.FromMissions(new[] { State(MissionCatalog.All[0], false) }), 0, 0));
            Case("already claimed mission is excluded", () =>
                Expect(LobbyRewardSummary.FromMissions(new[] { State(MissionCatalog.All[0], true, true) }), 0, 0));
            Case("claimability follows the supplied store snapshot", () =>
            {
                var state = State(MissionCatalog.All[0]);
                state.Claimable = false;
                Expect(LobbyRewardSummary.FromMissions(new[] { state }), 0, 0);
            });
            Case("missing entries and definitions do not invent rewards", () =>
                Expect(LobbyRewardSummary.FromMissions(new MissionUiState[]
                    { null, new() { Claimable = true } }), 0, 0));

            foreach (var definition in MissionCatalog.All)
            {
                var mission = definition;
                Case("real catalog reward: " + mission.Id, () =>
                    Expect(LobbyRewardSummary.FromMissions(new[] { State(mission) }), 1, mission.RewardFishSnacks));
            }

            Case("all real catalog rewards sum across both tracks", () =>
            {
                var states = new List<MissionUiState>();
                long expected = 0;
                foreach (var definition in MissionCatalog.All)
                {
                    states.Add(State(definition));
                    expected += definition.RewardFishSnacks;
                }
                Expect(LobbyRewardSummary.FromMissions(states), states.Count, expected);
            });
            Case("daily and achievement rewards share the same claimable snapshot", () =>
            {
                var daily = new List<MissionDefinition>(MissionCatalog.DefinitionsFor(MissionTrack.Daily));
                var achievements = new List<MissionDefinition>(MissionCatalog.DefinitionsFor(MissionTrack.Achievement));
                var states = new[]
                {
                    State(daily[0]), State(daily[1], false),
                    State(achievements[0]), State(achievements[1], true, true),
                };
                Expect(LobbyRewardSummary.FromMissions(states), 2,
                    (long)daily[0].RewardFishSnacks + achievements[0].RewardFishSnacks);
            });
            Case("real daily catalog is not replaced by a fixed reward", () =>
            {
                var states = new List<MissionUiState>();
                long expected = 0;
                foreach (var definition in MissionCatalog.DefinitionsFor(MissionTrack.Daily))
                {
                    states.Add(State(definition));
                    expected += definition.RewardFishSnacks;
                }
                Expect(LobbyRewardSummary.FromMissions(states), states.Count, expected);
            });
            Case("a changed synthetic reward is read rather than copied from a constant", () =>
            {
                var definition = new MissionDefinition { Id = "synthetic-reward", RewardFishSnacks = 73 };
                Expect(LobbyRewardSummary.FromMissions(new[] { State(definition) }), 1, 73);
            });
            Case("snapshot values stay immutable when source data changes", () =>
            {
                var definition = new MissionDefinition { Id = "synthetic-refresh", RewardFishSnacks = 17 };
                var state = State(definition);
                var before = LobbyRewardSummary.FromMissions(new[] { state });
                definition.RewardFishSnacks = 71;
                Expect(before, 1, 17);
                Expect(LobbyRewardSummary.FromMissions(new[] { state }), 1, 71);
            });
            Case("refresh after a claim removes its amount", () =>
            {
                var state = State(MissionCatalog.All[0]);
                var before = LobbyRewardSummary.FromMissions(new[] { state });
                state.Claimed = true;
                state.Claimable = false;
                Expect(before, 1, state.Definition.RewardFishSnacks);
                Expect(LobbyRewardSummary.FromMissions(new[] { state }), 0, 0);
            });
            Case("reading summaries never changes the mission state or catalog", () =>
            {
                var definition = MissionCatalog.All[0];
                var state = State(definition);
                var originalReward = definition.RewardFishSnacks;
                var originalProgress = state.Progress;
                var states = new[] { state };
                Expect(LobbyRewardSummary.FromMissions(states), 1, originalReward);
                Expect(LobbyRewardSummary.FromMissions(states), 1, originalReward);
                Check(ReferenceEquals(state.Definition, definition), "definition reference changed");
                Check(definition.RewardFishSnacks == originalReward, "real catalog reward mutated");
                Check(state.Completed && state.Claimable && !state.Claimed, "claim flags mutated");
                Check(state.Progress == originalProgress && ReferenceEquals(states[0], state), "input snapshot mutated");
            });
            Case("zero reward is counted but does not invent fish", () =>
                Expect(LobbyRewardSummary.FromMissions(new[]
                    { State(new MissionDefinition { Id = "synthetic-zero", RewardFishSnacks = 0 }) }), 1, 0));
            Case("negative reward cannot reduce the displayed sum", () =>
            {
                var negative = new MissionDefinition { Id = "synthetic-negative", RewardFishSnacks = -12 };
                Expect(LobbyRewardSummary.FromMissions(new[] { State(negative), State(MissionCatalog.All[0]) }),
                    2, MissionCatalog.All[0].RewardFishSnacks);
                Check(negative.RewardFishSnacks == -12, "aggregation rewrote the input definition");
            });
            Case("sum above Int32 maximum does not overflow", () =>
            {
                var definition = new MissionDefinition { Id = "synthetic-large", RewardFishSnacks = int.MaxValue };
                Expect(LobbyRewardSummary.FromMissions(new[] { State(definition), State(definition) }),
                    2, (long)int.MaxValue * 2);
            });
            Case("count and fish use one enumeration", () =>
            {
                var input = new SinglePassMissions(new[] { State(MissionCatalog.All[0]), State(MissionCatalog.All[1]) });
                var summary = LobbyRewardSummary.FromMissions(input);
                Expect(summary, 2, (long)MissionCatalog.All[0].RewardFishSnacks + MissionCatalog.All[1].RewardFishSnacks);
                Check(input.Enumerations == 1, "snapshot enumerated more than once");
            });
            Case("journey label promises only existing collection and settlement", () =>
                Check(LobbyRewardSummary.JourneyRewardLabel == "沿途收集鱼干 · 旅程结束后结算", "unsupported journey promise"));
            Case("daily hint promises a challenge rather than a payout", () =>
                Check(LobbyRewardSummary.DailyChallengeHint == "今日挑战尚未开始 · 挑战当日记录", "unsupported daily promise"));
            return Failures.ToArray();
        }

        private sealed class SinglePassMissions : IEnumerable<MissionUiState>
        {
            private readonly IEnumerable<MissionUiState> states;
            public int Enumerations { get; private set; }
            public SinglePassMissions(IEnumerable<MissionUiState> states) { this.states = states; }
            public IEnumerator<MissionUiState> GetEnumerator()
            {
                if (++Enumerations > 1) throw new Exception("mission snapshot enumerated twice");
                return states.GetEnumerator();
            }
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }
    }
}
