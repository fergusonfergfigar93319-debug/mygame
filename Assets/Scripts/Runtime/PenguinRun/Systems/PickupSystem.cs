using System.Collections.Generic;
using PenguinRun.Game;
using UnityEngine;

namespace PenguinRun
{
    internal sealed class PickupSystem
    {
        private readonly RunnerPickupTuning pickupTuning;
        private readonly RunnerPowerUpTuning powerUpTuning;
        private readonly RunnerAudio audio;
        private readonly RunnerSessionConfig config;

        public PickupSystem(RunnerPickupTuning pickupTuning, RunnerPowerUpTuning powerUpTuning, RunnerAudio audio, RunnerSessionConfig config)
        {
            this.pickupTuning = pickupTuning;
            this.powerUpTuning = powerUpTuning;
            this.audio = audio;
            this.config = config;
        }

        public void CollectCoins(Transform player, float playerY, WorldDirector world, List<GameObject> coinObjs, ref string feedbackText, ref float feedbackTimer)
        {
            var px = player.position.x;
            var pz = player.position.z;
            var magnetActive = world.MagnetTimer > 0f;
            var collectX =
                magnetActive
                    ? pickupTuning.magnetCollectXBase + config.MagnetLevel * pickupTuning.magnetCollectXPerLevel
                    : pickupTuning.coinCollectX;
            var collectZ = magnetActive ? pickupTuning.magnetCollectZ : pickupTuning.coinCollectZ;

            for (var i = coinObjs.Count - 1; i >= 0; i--)
            {
                var c = coinObjs[i];
                var p = c.transform.position;
                if (Mathf.Abs(p.z - pz) > collectZ) continue;
                if (Mathf.Abs(p.x - px) > collectX) continue;
                if (Mathf.Abs(p.y - playerY) > pickupTuning.coinCollectY) continue;

                var nextCombo = world.CoinCombo + 1;
                var comboBonus = Mathf.Min(pickupTuning.comboBonusMax, nextCombo * pickupTuning.comboBonusPerCoin);
                world.AddCoin(1, comboBonus, pickupTuning.comboWindowSeconds);

                feedbackText = nextCombo >= 3 ? $"金币连击 x{nextCombo}" : "+1 金币";
                feedbackTimer = 0.55f;
                audio.PlayCoin(nextCombo);
                Object.Destroy(c);
                coinObjs.RemoveAt(i);
            }
        }

        public void CollectPowerUps(Transform player, WorldDirector world, Dictionary<GameObject, PowerUpKind> powerUps, ref string feedbackText, ref float feedbackTimer)
        {
            var px = player.position.x;
            var pz = player.position.z;
            var py = player.position.y;

            var magnetActive = world.MagnetTimer > 0f;
            var collectX = magnetActive
                ? Mathf.Min(
                    pickupTuning.magnetCollectXBase + config.MagnetLevel * pickupTuning.magnetCollectXPerLevel,
                    4.2f)
                : 1.38f;
            var collectZ = magnetActive ? Mathf.Min(pickupTuning.magnetCollectZ, 2.15f) : 1.32f;
            var halfY = 1.9f;

            var powerKeys = new List<GameObject>(powerUps.Keys);
            foreach (var power in powerKeys)
            {
                if (power == null)
                {
                    powerUps.Remove(power);
                    continue;
                }

                var p = power.transform.position;
                if (Mathf.Abs(p.z - pz) > collectZ) continue;
                if (Mathf.Abs(p.x - px) > collectX) continue;
                if (Mathf.Abs(p.y - py) > halfY) continue;

                ActivatePowerUp(powerUps[power], world, ref feedbackText);
                feedbackTimer = 0.85f;
                audio.PlayPowerUp(powerUps[power]);

                Object.Destroy(power);
                powerUps.Remove(power);
            }
        }

        private void ActivatePowerUp(PowerUpKind kind, WorldDirector world, ref string feedbackText)
        {
            var comboMult = world.RegisterPowerUpPickup(kind);
            var comboLabel = world.PowerUpComboLabel;

            switch (kind)
            {
                case PowerUpKind.Dash:
                    world.DashTimer = Mathf.Max(world.DashTimer, (powerUpTuning.dashBaseSeconds + config.DashLevel * powerUpTuning.dashPerLevelSeconds) * comboMult);
                    feedbackText = string.IsNullOrEmpty(comboLabel) ? "鱼干冲刺" : $"鱼干冲刺 · {comboLabel}";
                    break;
                case PowerUpKind.Magnet:
                    world.MagnetTimer = Mathf.Max(world.MagnetTimer, (powerUpTuning.magnetBaseSeconds + config.MagnetLevel * powerUpTuning.magnetPerLevelSeconds) * comboMult);
                    feedbackText = string.IsNullOrEmpty(comboLabel) ? "极光磁针" : $"极光磁针 · {comboLabel}";
                    break;
                case PowerUpKind.Shield:
                    world.ShieldTimer = Mathf.Max(world.ShieldTimer, (powerUpTuning.shieldBaseSeconds + config.TuanLevel * powerUpTuning.shieldPerLevelSeconds) * comboMult);
                    feedbackText = string.IsNullOrEmpty(comboLabel) ? "团团护盾" : $"团团护盾 · {comboLabel}";
                    break;
                case PowerUpKind.ScoreStar:
                    world.ScoreBoostTimer = Mathf.Max(world.ScoreBoostTimer, (powerUpTuning.scoreStarBaseSeconds + config.PolarLevel * powerUpTuning.scoreStarPerLevelSeconds) * comboMult);
                    feedbackText = string.IsNullOrEmpty(comboLabel) ? "星光加分" : $"星光加分 · {comboLabel}";
                    break;
                case PowerUpKind.GlideFeather:
                    world.GlideTimer = Mathf.Max(world.GlideTimer, (powerUpTuning.glideBaseSeconds + config.TuanLevel * powerUpTuning.glidePerLevelSeconds) * comboMult);
                    feedbackText = string.IsNullOrEmpty(comboLabel) ? "极地滑翔" : $"极地滑翔 · {comboLabel}";
                    break;
                case PowerUpKind.DoubleFishSnack:
                    world.ExtendDoubleFish((powerUpTuning.doubleFishBaseSeconds + config.PolarLevel * powerUpTuning.doubleFishPerPolarLevelSeconds) * comboMult);
                    feedbackText = string.IsNullOrEmpty(comboLabel) ? "双倍鱼干" : $"双倍鱼干 · {comboLabel}";
                    break;
                case PowerUpKind.TimeHourglass:
                    world.ExtendSlowMo((powerUpTuning.hourglassBaseSeconds + config.PolarLevel * powerUpTuning.hourglassPerPolarLevelSeconds) * comboMult);
                    feedbackText = string.IsNullOrEmpty(comboLabel) ? "时间沙漏" : $"时间沙漏 · {comboLabel}";
                    break;
                case PowerUpKind.BubbleShield:
                    world.ShieldTimer = Mathf.Max(world.ShieldTimer, (powerUpTuning.shieldBaseSeconds + 1.2f) + config.TuanLevel * powerUpTuning.shieldPerLevelSeconds);
                    feedbackText = "泡泡护盾";
                    break;
                case PowerUpKind.SeahorseBoost:
                    world.DashTimer = Mathf.Max(world.DashTimer, (powerUpTuning.dashBaseSeconds + 1.5f) + config.DashLevel * powerUpTuning.dashPerLevelSeconds);
                    feedbackText = "海马加速";
                    break;
                case PowerUpKind.CloudWalk:
                    world.GlideTimer = Mathf.Max(world.GlideTimer, (powerUpTuning.glideBaseSeconds + 1.0f) + config.TuanLevel * powerUpTuning.glidePerLevelSeconds);
                    feedbackText = "踏云而行";
                    break;
                case PowerUpKind.WindRider:
                    world.GlideTimer = Mathf.Max(world.GlideTimer, (powerUpTuning.glideBaseSeconds + 2.0f) + config.TuanLevel * powerUpTuning.glidePerLevelSeconds);
                    feedbackText = "乘风滑翔";
                    break;
                case PowerUpKind.FishBomb:
                    world.AddFishBomb();
                    feedbackText = $"鱼弹×{world.FishBombs}";
                    break;
                case PowerUpKind.SecondHeart:
                    world.AddExtraLife();
                    feedbackText = $"\u2665 +1 心 ({world.Lives}/{world.MaxLives})";
                    break;

                // ── 新道具 ────────────────────────────────────
                case PowerUpKind.IceMirror:
                    world.IceMirrorTimer = Mathf.Max(world.IceMirrorTimer, 5.0f);
                    feedbackText = "冰镜反射";
                    break;
                case PowerUpKind.AuroraChain:
                    world.AuroraChainTimer = Mathf.Max(world.AuroraChainTimer, 8.0f);
                    feedbackText = "极光连锁";
                    break;
                case PowerUpKind.FogLantern:
                    world.FogLanternTimer = Mathf.Max(world.FogLanternTimer, 6.0f);
                    world.MagnetTimer = Mathf.Max(world.MagnetTimer, 6.0f);
                    world.ExtendSlowMo(3.5f);
                    feedbackText = "雾灯照路";
                    break;
                case PowerUpKind.TreantArmor:
                    world.AddTreantArmorHits(2);
                    feedbackText = $"树人护甲×{world.TreantArmorHits}";
                    break;
                case PowerUpKind.CoralBounce:
                    world.SetCoralBounce();
                    world.ShieldTimer = Mathf.Max(world.ShieldTimer, 3.5f);
                    feedbackText = "珊瑚回弹";
                    break;
                case PowerUpKind.ThunderFeather:
                    world.ThunderFeatherTimer = Mathf.Max(world.ThunderFeatherTimer, 4.5f);
                    world.DashTimer = Mathf.Max(world.DashTimer, 4.5f);
                    world.MagnetTimer = Mathf.Max(world.MagnetTimer, 4.5f);
                    feedbackText = "雷羽激荡";
                    break;
            }
        }
    }
}

