using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using PenguinRun.Game.Save;
using UnityEngine;

namespace PenguinRun.Tests
{
    public static class CampUpgradePreviewRegression
    {
        private static readonly List<string> Failures = new();
        public static int CaseCount { get; private set; }

        private static void Case(string name, Action test)
        {
            CaseCount++;
            PlayerPrefs.Reset();
            try { test(); }
            catch (Exception e) { Failures.Add(name + ": " + e.GetBaseException().Message); }
        }

        private static void Check(bool condition, string message)
        { if (!condition) throw new Exception(message); }

        private static void Near(float actual, float expected, string message)
        { Check(Math.Abs(actual - expected) < 0.006f, $"{message}: expected {expected}, got {actual}"); }

        private static float Number(string text, string label = "")
        {
            var match = Regex.Match(text, Regex.Escape(label) + @"\s*(?<n>\d+(?:\.\d+)?)");
            Check(match.Success, $"Missing numeric effect '{label}' in '{text}'");
            return float.Parse(match.Groups["n"].Value, CultureInfo.InvariantCulture);
        }

        public static string[] Run()
        {
            var previousCulture = CultureInfo.CurrentCulture;
            CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
            try
            {
                Failures.Clear(); CaseCount = 0;
                var config = RunnerTuningConfig.Default();
                var runtime = RunnerTuning.FromConfig(config).powerUp;
                for (var level = 0; level <= PlayerSave.CampMaxLevel; level++)
                {
                    var lv = level;
                    Case($"dash preview level {lv} follows gameplay defaults", () =>
                        Near(Number(CampUpgrades.FormatEffectAt(CampUpgradeKind.Dash, lv)),
                            runtime.dashBaseSeconds + lv * runtime.dashPerLevelSeconds, "dash"));
                    Case($"magnet preview level {lv} follows gameplay defaults", () =>
                        Near(Number(CampUpgrades.FormatEffectAt(CampUpgradeKind.Magnet, lv)),
                            runtime.magnetBaseSeconds + lv * runtime.magnetPerLevelSeconds, "magnet"));
                    Case($"Tuan shield preview level {lv} follows gameplay defaults", () =>
                        Near(Number(CampUpgrades.FormatEffectAt(CampUpgradeKind.Tuan, lv), "护盾"),
                            runtime.shieldBaseSeconds + lv * runtime.shieldPerLevelSeconds, "shield"));
                    Case($"Tuan glide preview level {lv} follows gameplay defaults", () =>
                        Near(Number(CampUpgrades.FormatEffectAt(CampUpgradeKind.Tuan, lv), "滑翔"),
                            runtime.glideBaseSeconds + lv * runtime.glidePerLevelSeconds, "glide"));
                    Case($"Polar score-star preview level {lv} follows gameplay defaults", () =>
                        Near(Number(CampUpgrades.FormatEffectAt(CampUpgradeKind.Polar, lv), "星光"),
                            runtime.scoreStarBaseSeconds + lv * runtime.scoreStarPerLevelSeconds, "score star"));
                    Case($"Polar double-fish preview level {lv} follows gameplay defaults", () =>
                        Near(Number(CampUpgrades.FormatEffectAt(CampUpgradeKind.Polar, lv), "双倍"),
                            runtime.doubleFishBaseSeconds + lv * runtime.doubleFishPerPolarLevelSeconds, "double fish"));
                    Case($"Polar hourglass preview level {lv} follows gameplay defaults", () =>
                        Near(Number(CampUpgrades.FormatEffectAt(CampUpgradeKind.Polar, lv), "沙漏"),
                            runtime.hourglassBaseSeconds + lv * runtime.hourglassPerPolarLevelSeconds, "hourglass"));
                    Case($"Polar probability preview level {lv} retains its additive points", () =>
                        Near(Number(CampUpgrades.FormatEffectAt(CampUpgradeKind.Polar, lv)),
                            lv * runtime.powerUpChancePerPolarLevel * 100f, "probability points"));
                    Case($"duration getters level {lv} match gameplay defaults", () =>
                    {
                        PlayerSave.SetCampLevel(PlayerSave.Keys.CampDash, lv);
                        PlayerSave.SetCampLevel(PlayerSave.Keys.CampTuan, lv);
                        PlayerSave.SetCampLevel(PlayerSave.Keys.CampMagnet, lv);
                        Near(CampUpgrades.GetFishDashDurationSeconds(), runtime.dashBaseSeconds + lv * runtime.dashPerLevelSeconds, "dash getter");
                        Near(CampUpgrades.GetTuanAssistDurationSeconds(), runtime.shieldBaseSeconds + lv * runtime.shieldPerLevelSeconds, "shield getter");
                        Near(CampUpgrades.GetMagnetDurationSeconds(), runtime.magnetBaseSeconds + lv * runtime.magnetPerLevelSeconds, "magnet getter");
                    });
                    Case($"reward previews level {lv} remain unchanged", () =>
                    {
                        Check(CampUpgrades.FormatEffectAt(CampUpgradeKind.FishGain, lv) == $"结算鱼干 ×{1f + lv * 0.06f:0.00}", "fish multiplier changed");
                        Check(CampUpgrades.FormatEffectAt(CampUpgradeKind.ScoreBonus, lv) == $"结算得分 ×{1f + lv * 0.04f:0.00}", "score multiplier changed");
                    });
                }
                foreach (var kind in CampUpgrades.AllKinds)
                {
                    var upgrade = kind;
                    Case($"{upgrade} preview clamps invalid levels", () =>
                    {
                        Check(CampUpgrades.FormatEffectAt(upgrade, int.MinValue) == CampUpgrades.FormatEffectAt(upgrade, 0), "negative level not clamped");
                        Check(CampUpgrades.FormatEffectAt(upgrade, int.MaxValue) == CampUpgrades.FormatEffectAt(upgrade, PlayerSave.CampMaxLevel), "excess level not clamped");
                    });
                }
                foreach (var kind in new[] { CampUpgradeKind.Dash, CampUpgradeKind.Tuan, CampUpgradeKind.Polar, CampUpgradeKind.Magnet })
                {
                    var upgrade = kind;
                    Case($"{upgrade} explicitly labels base effects", () =>
                        Check(CampUpgrades.FormatEffectAt(upgrade, 5).Contains("基础"), "missing base-duration qualifier"));
                }
                Case("Tuan description names shield and glide without promising multiple blocks", () =>
                {
                    var text = CampUpgrades.GetDescription(CampUpgradeKind.Tuan);
                    Check(text.Contains("护盾") && text.Contains("滑翔"), "missing affected power-ups");
                    Check(text.Contains("受击") && !text.Contains("挡更多"), "shield consumption not explained");
                });
                Case("Polar description names all three timed effects", () =>
                {
                    var text = CampUpgrades.GetDescription(CampUpgradeKind.Polar);
                    Check(text.Contains("星光") && text.Contains("双倍") && text.Contains("沙漏"), "missing timed effects");
                });
                Case("preview reads never write save data or flush", () =>
                {
                    foreach (var kind in CampUpgrades.AllKinds)
                        for (var lv = 0; lv <= PlayerSave.CampMaxLevel; lv++)
                        { CampUpgrades.FormatEffectAt(kind, lv); CampUpgrades.GetDescription(kind); }
                    CampUpgrades.GetFishDashDurationSeconds();
                    CampUpgrades.GetTuanAssistDurationSeconds();
                    CampUpgrades.GetMagnetDurationSeconds();
                    Check(PlayerPrefs.Writes == 0 && PlayerPrefs.Saves == 0, "preview mutated the save");
                });
                Case("purchases retain cost, levels and full-upgrade cap", () =>
                {
                    PlayerSave.AddFishSnacks(1000);
                    Check(CampUpgrades.TryPurchase(CampUpgradeKind.Dash), "first upgrade unavailable");
                    Check(PlayerSave.TotalFishSnacks == 960 && PlayerSave.CampDashLevel == 1, "first upgrade cost or level changed");
                    Check(CampUpgrades.GetUpgradeCost(CampUpgradeKind.Dash, 4) == 540, "last upgrade cost changed");
                    PlayerSave.SetCampLevel(PlayerSave.Keys.CampDash, PlayerSave.CampMaxLevel);
                    Check(!CampUpgrades.TryPurchase(CampUpgradeKind.Dash), "purchased above maximum level");
                });
                return Failures.ToArray();
            }
            finally { CultureInfo.CurrentCulture = previousCulture; PlayerPrefs.Reset(); }
        }
    }
}
