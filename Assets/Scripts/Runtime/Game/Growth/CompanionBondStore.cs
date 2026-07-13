using PenguinRun.Game.Save;
using UnityEngine;

namespace PenguinRun.Game.Growth
{
    public static class CompanionBondStore
    {
        public const int MaxLevel = 5;
        private static readonly int[] LevelThresholds = { 0, 60, 150, 300, 520 };

        public static CompanionBondSnapshot Snapshot(CompanionId id)
        {
            var unlocked = IsUnlocked(id);
            var xp = unlocked ? Mathf.Max(0, PlayerSave.ReadGrowthInt(Key(id), 0)) : 0;
            var level = unlocked ? LevelForXp(xp) : 0;
            var start = level <= 1 ? 0 : LevelThresholds[level - 1];
            var next = level >= MaxLevel ? LevelThresholds[MaxLevel - 1] : LevelThresholds[level];
            return new CompanionBondSnapshot(id, unlocked, xp, level, start, next, HelperLabel(id, level));
        }

        public static CompanionBondAward AwardAfterRun(int routeEventsCompleted, int riskyRoutesCompleted,
            int skillScore, bool dailyMode, bool discoveredThemedRelic)
        {
            routeEventsCompleted = Mathf.Max(0, routeEventsCompleted);
            riskyRoutesCompleted = Mathf.Max(0, riskyRoutesCompleted);
            skillScore = Mathf.Max(0, skillScore);
            var shared = 8 + routeEventsCompleted * 3 + riskyRoutesCompleted * 5 +
                Mathf.Min(12, skillScore / 450) + (dailyMode ? 5 : 0) + (discoveredThemedRelic ? 8 : 0);
            var tuanXp = IsUnlocked(CompanionId.TuanTuan) ? shared : 0;
            var drorXp = IsUnlocked(CompanionId.Dror) ? shared + riskyRoutesCompleted * 2 + (dailyMode ? 3 : 0) : 0;
            var tuanLevelUp = Award(CompanionId.TuanTuan, tuanXp);
            var drorLevelUp = Award(CompanionId.Dror, drorXp);
            return new CompanionBondAward(tuanXp, drorXp, tuanLevelUp, drorLevelUp);
        }

        public static float GetPassiveHelperBonus(CompanionId id)
        {
            var level = Snapshot(id).Level;
            return Mathf.Min(0.08f, Mathf.Max(0, level - 1) * 0.02f);
        }

        public static float GetConditionalHelperChance(CompanionId id)
        {
            var level = Snapshot(id).Level;
            return Mathf.Min(0.10f, Mathf.Max(0, level - 1) * 0.025f);
        }

        private static bool Award(CompanionId id, int amount)
        {
            if (amount <= 0 || !IsUnlocked(id)) return false;
            var before = Snapshot(id).Level;
            var nextXp = Mathf.Min(LevelThresholds[MaxLevel - 1], PlayerSave.ReadGrowthInt(Key(id), 0) + amount);
            PlayerSave.WriteGrowthInt(Key(id), nextXp);
            return LevelForXp(nextXp) > before;
        }

        private static int LevelForXp(int xp)
        {
            var level = 1;
            for (var i = 1; i < LevelThresholds.Length; i++)
                if (xp >= LevelThresholds[i]) level = i + 1;
            return Mathf.Clamp(level, 1, MaxLevel);
        }

        private static bool IsUnlocked(CompanionId id) => id switch
        {
            CompanionId.TuanTuan => PlayerSave.RescuedTuanTuan,
            CompanionId.Dror => PlayerSave.CompanionDrorUnlocked,
            _ => false,
        };

        private static string HelperLabel(CompanionId id, int level)
        {
            if (level <= 0) return "尚未相遇";
            return id switch
            {
                CompanionId.TuanTuan when level >= 5 => "暖意回响 · 收藏帮助 8%",
                CompanionId.TuanTuan when level >= 3 => "鱼干嗅觉 · 收藏帮助提升",
                CompanionId.TuanTuan => "营火问候 · 解锁同行对话",
                CompanionId.Dror when level >= 5 => "极光补给 · 冒险援助 10%",
                CompanionId.Dror when level >= 3 => "风雪直觉 · 冒险援助提升",
                _ => "冰湖来信 · 解锁同行对话",
            };
        }

        private static string Key(CompanionId id) => "bond_" + id.ToString().ToLowerInvariant() + "_xp";
    }
}
