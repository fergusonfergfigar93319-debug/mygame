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

        public void CollectPowerUps(Transform player, WorldDirector world, Dictionary<GameObject, PowerUpKind> powerUps, ref string feedbackText, ref float feedbackTimer, System.Action pulseHaptic)
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
                pulseHaptic?.Invoke();

                Object.Destroy(power);
                powerUps.Remove(power);
            }
        }

        private void ActivatePowerUp(PowerUpKind kind, WorldDirector world, ref string feedbackText)
        {
            switch (kind)
            {
                case PowerUpKind.Dash:
                    world.DashTimer = Mathf.Max(world.DashTimer, powerUpTuning.dashBaseSeconds + config.DashLevel * powerUpTuning.dashPerLevelSeconds);
                    feedbackText = "鱼干冲刺";
                    break;
                case PowerUpKind.Magnet:
                    world.MagnetTimer = Mathf.Max(world.MagnetTimer, powerUpTuning.magnetBaseSeconds + config.MagnetLevel * powerUpTuning.magnetPerLevelSeconds);
                    feedbackText = "极光磁针";
                    break;
                case PowerUpKind.Shield:
                    world.ShieldTimer = Mathf.Max(world.ShieldTimer, powerUpTuning.shieldBaseSeconds + config.TuanLevel * powerUpTuning.shieldPerLevelSeconds);
                    feedbackText = "团团护盾";
                    break;
                case PowerUpKind.ScoreStar:
                    world.ScoreBoostTimer = Mathf.Max(world.ScoreBoostTimer, powerUpTuning.scoreStarBaseSeconds + config.PolarLevel * powerUpTuning.scoreStarPerLevelSeconds);
                    feedbackText = "星光加分";
                    break;
                case PowerUpKind.GlideFeather:
                    world.GlideTimer = Mathf.Max(world.GlideTimer, powerUpTuning.glideBaseSeconds + config.TuanLevel * powerUpTuning.glidePerLevelSeconds);
                    feedbackText = "极地滑翔";
                    break;
                case PowerUpKind.DoubleFishSnack:
                    world.ExtendDoubleFish(powerUpTuning.doubleFishBaseSeconds + config.PolarLevel * powerUpTuning.doubleFishPerPolarLevelSeconds);
                    feedbackText = "双倍鱼干";
                    break;
                case PowerUpKind.TimeHourglass:
                    world.ExtendSlowMo(powerUpTuning.hourglassBaseSeconds + config.PolarLevel * powerUpTuning.hourglassPerPolarLevelSeconds);
                    feedbackText = "时间沙漏";
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
            }
        }
    }
}

