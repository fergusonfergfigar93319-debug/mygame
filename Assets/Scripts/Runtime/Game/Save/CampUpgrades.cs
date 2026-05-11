using System;
using UnityEngine;

namespace PenguinRun.Game.Save
{
    /// <summary>
    /// 营地强化的种类。前四类直接驱动跑酷内的拾取与时长（PickupSystem 读取
    /// <see cref="RunnerSessionConfig"/> 中的等级），后两类在 <see cref="RunOutcomeRouter"/> 结算时
    /// 作为鱼干 / 得分的乘数生效。
    /// </summary>
    public enum CampUpgradeKind
    {
        Dash,
        Tuan,
        Polar,
        Magnet,
        FishGain,
        ScoreBonus,
    }

    /// <summary>养成的逻辑分类，用于 UI Tab 过滤与分组展示。</summary>
    public enum CampUpgradeCategory
    {
        Action,    // 跑酷类：冲刺、直觉
        Survival,  // 防守类：团团掩护
        Reward,    // 收益类：磁吸、鱼干、得分
    }

    /// <summary>
    /// 营地局外强化的等级、价格、效果换算与综合倍率统计。
    /// 重构后：6 项强化 / 5 级上限 / 阶梯价格 / 分类 / 效果预览 / 综合加成。
    /// </summary>
    public static class CampUpgrades
    {
        public static readonly CampUpgradeKind[] AllKinds = (CampUpgradeKind[])Enum.GetValues(typeof(CampUpgradeKind));

        /// <summary>等级 0 → 1 / 1 → 2 / … / 4 → 5 的基础鱼干价格曲线。</summary>
        private static readonly int[] BaseUpgradeCostByLevel = { 40, 90, 180, 320, 540 };

        public static int GetLevel(CampUpgradeKind kind) =>
            kind switch
            {
                CampUpgradeKind.Dash       => PlayerSave.CampDashLevel,
                CampUpgradeKind.Tuan       => PlayerSave.CampTuanLevel,
                CampUpgradeKind.Polar      => PlayerSave.CampPolarIntuitionLevel,
                CampUpgradeKind.Magnet     => PlayerSave.CampMagnetLevel,
                CampUpgradeKind.FishGain   => PlayerSave.CampFishGainLevel,
                CampUpgradeKind.ScoreBonus => PlayerSave.CampScoreBonusLevel,
                _ => 0,
            };

        public static CampUpgradeCategory GetCategory(CampUpgradeKind kind) =>
            kind switch
            {
                CampUpgradeKind.Dash       => CampUpgradeCategory.Action,
                CampUpgradeKind.Polar      => CampUpgradeCategory.Action,
                CampUpgradeKind.Tuan       => CampUpgradeCategory.Survival,
                CampUpgradeKind.Magnet     => CampUpgradeCategory.Reward,
                CampUpgradeKind.FishGain   => CampUpgradeCategory.Reward,
                CampUpgradeKind.ScoreBonus => CampUpgradeCategory.Reward,
                _ => CampUpgradeCategory.Action,
            };

        public static string GetTitle(CampUpgradeKind kind) =>
            kind switch
            {
                CampUpgradeKind.Dash       => "鱼干冲刺时长",
                CampUpgradeKind.Tuan       => "团团雪球掩护",
                CampUpgradeKind.Polar      => "极地直觉",
                CampUpgradeKind.Magnet     => "磁针吸附时长",
                CampUpgradeKind.FishGain   => "鱼干收益",
                CampUpgradeKind.ScoreBonus => "终局得分加成",
                _ => "未知强化",
            };

        public static string GetDescription(CampUpgradeKind kind) =>
            kind switch
            {
                CampUpgradeKind.Dash       => "拾取鱼干冲刺后获得更长加速。",
                CampUpgradeKind.Tuan       => "团团雪球掩护持续更久，挡更多障碍。",
                CampUpgradeKind.Polar      => "更敏锐的极地直觉，幸运道具的掉率与时长提升。",
                CampUpgradeKind.Magnet     => "极光磁针吸附鱼干的时间更长，吸附半径更大。",
                CampUpgradeKind.FishGain   => "每局结算时获得的鱼干额外增加。",
                CampUpgradeKind.ScoreBonus => "每局最终得分获得百分比加成，更易冲刷新纪录。",
                _ => string.Empty,
            };

        public static string GetCategoryLabel(CampUpgradeCategory category) =>
            category switch
            {
                CampUpgradeCategory.Action   => "跑酷",
                CampUpgradeCategory.Survival => "防守",
                CampUpgradeCategory.Reward   => "收益",
                _ => "其他",
            };

        public static Color GetCategoryColor(CampUpgradeCategory category) =>
            category switch
            {
                CampUpgradeCategory.Action   => new Color(1f, 0.62f, 0.32f, 1f),
                CampUpgradeCategory.Survival => new Color(0.55f, 0.85f, 1f, 1f),
                CampUpgradeCategory.Reward   => new Color(1f, 0.85f, 0.4f, 1f),
                _ => Color.white,
            };

        /// <summary>下一级价格；满级返回 null。各类 kind 的成本在基础曲线上做小幅度倍率调整。</summary>
        public static int? GetUpgradeCost(CampUpgradeKind kind, int fromLevel)
        {
            if (fromLevel < 0 || fromLevel >= PlayerSave.CampMaxLevel) return null;
            if (fromLevel >= BaseUpgradeCostByLevel.Length) return null;
            var baseCost = BaseUpgradeCostByLevel[fromLevel];
            var multiplier = kind switch
            {
                CampUpgradeKind.Magnet     => 1.20f,
                CampUpgradeKind.FishGain   => 1.35f,
                CampUpgradeKind.ScoreBonus => 1.50f,
                _ => 1.0f,
            };
            // 5 整数取整，便于商店 / 营地的展示。
            var raw = baseCost * multiplier;
            return Mathf.Max(5, Mathf.RoundToInt(raw / 5f) * 5);
        }

        /// <summary>支付鱼干并升级一级；已满级或鱼干不足返回 false。</summary>
        public static bool TryPurchase(CampUpgradeKind kind)
        {
            var current = GetLevel(kind);
            if (current >= PlayerSave.CampMaxLevel) return false;
            var cost = GetUpgradeCost(kind, current);
            if (!cost.HasValue) return false;
            if (!PlayerSave.TrySpendFishSnacks(cost.Value)) return false;
            var key = KeyOf(kind);
            PlayerSave.SetCampLevel(key, current + 1);
            PlayerSave.AddCampInvested(cost.Value);
            PlayerSave.Flush();
            return true;
        }

        // ── 等级 → 数值效果 ────────────────────────────────────────────────────

        public static float GetFishDashDurationSeconds() => 7f + GetLevel(CampUpgradeKind.Dash) * 1f;
        public static float GetTuanAssistDurationSeconds() => 4.5f + GetLevel(CampUpgradeKind.Tuan) * 0.55f;
        public static float GetMagnetDurationSeconds() => 10f + GetLevel(CampUpgradeKind.Magnet) * 1.5f;

        /// <summary>0~5 → 0~30%（每级 +6%）。</summary>
        public static float GetFishGainBonus() => GetLevel(CampUpgradeKind.FishGain) * 0.06f;

        /// <summary>0~5 → 0~20%（每级 +4%）。</summary>
        public static float GetScoreBonus() => GetLevel(CampUpgradeKind.ScoreBonus) * 0.04f;

        public static float GetFishGainMultiplier() => 1f + GetFishGainBonus();
        public static float GetScoreMultiplier() => 1f + GetScoreBonus();

        /// <summary>
        /// 当前等级（含 0）下该项强化对应的可读效果值。供 UI 展示的「当前」与「下一级」共用。
        /// </summary>
        public static string FormatEffectAt(CampUpgradeKind kind, int level)
        {
            level = Mathf.Clamp(level, 0, PlayerSave.CampMaxLevel);
            return kind switch
            {
                CampUpgradeKind.Dash       => $"持续 {7f + level * 1f:0.0} 秒",
                CampUpgradeKind.Tuan       => $"持续 {4.5f + level * 0.55f:0.00} 秒",
                CampUpgradeKind.Polar      => $"幸运道具掉率 +{level * 2.5f:0.#}%",
                CampUpgradeKind.Magnet     => $"持续 {10f + level * 1.5f:0.0} 秒",
                CampUpgradeKind.FishGain   => $"结算鱼干 ×{1f + level * 0.06f:0.00}",
                CampUpgradeKind.ScoreBonus => $"结算得分 ×{1f + level * 0.04f:0.00}",
                _ => "—",
            };
        }

        // ── 总览统计（CampPanel 顶部「养成总览」展示）──────────────────────────

        public static int TotalLevelsCount()
        {
            var sum = 0;
            foreach (var k in AllKinds) sum += GetLevel(k);
            return sum;
        }

        public static int MaxedKindCount()
        {
            var n = 0;
            foreach (var k in AllKinds)
                if (GetLevel(k) >= PlayerSave.CampMaxLevel) n++;
            return n;
        }

        public static int MaxTotalLevels => AllKinds.Length * PlayerSave.CampMaxLevel;

        private static string KeyOf(CampUpgradeKind kind) =>
            kind switch
            {
                CampUpgradeKind.Dash       => PlayerSave.Keys.CampDash,
                CampUpgradeKind.Tuan       => PlayerSave.Keys.CampTuan,
                CampUpgradeKind.Polar      => PlayerSave.Keys.CampPolar,
                CampUpgradeKind.Magnet     => PlayerSave.Keys.CampMagnet,
                CampUpgradeKind.FishGain   => PlayerSave.Keys.CampFishGain,
                CampUpgradeKind.ScoreBonus => PlayerSave.Keys.CampScoreBonus,
                _ => throw new ArgumentOutOfRangeException(nameof(kind)),
            };
    }
}
