using System;

namespace PenguinRun.Game.Growth
{
    public enum CompanionId { TuanTuan, Dror }

    public readonly struct CompanionBondSnapshot
    {
        public CompanionBondSnapshot(CompanionId id, bool unlocked, int xp, int level,
            int levelStartXp, int nextLevelXp, string helperLabel)
        {
            Id = id;
            Unlocked = unlocked;
            Xp = Math.Max(0, xp);
            Level = Math.Max(0, level);
            LevelStartXp = Math.Max(0, levelStartXp);
            NextLevelXp = Math.Max(LevelStartXp, nextLevelXp);
            HelperLabel = helperLabel ?? string.Empty;
        }
        public CompanionId Id { get; }
        public bool Unlocked { get; }
        public int Xp { get; }
        public int Level { get; }
        public int LevelStartXp { get; }
        public int NextLevelXp { get; }
        public string HelperLabel { get; }
        public float Progress01 => !Unlocked ? 0f : NextLevelXp <= LevelStartXp ? 1f :
            Math.Min(1f, (Xp - LevelStartXp) / (float)(NextLevelXp - LevelStartXp));
    }

    public readonly struct CompanionBondAward
    {
        public CompanionBondAward(int tuanTuanXp, int drorXp, bool tuanTuanLevelUp, bool drorLevelUp)
        {
            TuanTuanXp = Math.Max(0, tuanTuanXp);
            DrorXp = Math.Max(0, drorXp);
            TuanTuanLevelUp = tuanTuanLevelUp;
            DrorLevelUp = drorLevelUp;
        }
        public int TuanTuanXp { get; }
        public int DrorXp { get; }
        public bool TuanTuanLevelUp { get; }
        public bool DrorLevelUp { get; }
        public int TotalXp => TuanTuanXp + DrorXp;
    }
}
