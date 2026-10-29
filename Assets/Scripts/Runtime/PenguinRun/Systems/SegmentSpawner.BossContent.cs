using UnityEngine;

namespace PenguinRun
{
    internal sealed partial class SegmentSpawner
    {
        public bool HasOwnedBossRewardEntities
        {
            get
            {
                for (var i = bossRewardEntities.Count - 1; i >= 0; i--)
                {
                    var entity = bossRewardEntities[i];
                    if (entity != null && entity.activeSelf && entity.transform.position.z >= worldTuning.despawnZ)
                        continue;
                    bossRewardEntities.RemoveAt(i);
                }
                return bossRewardEntities.Count > 0;
            }
        }

        // ── Boss 战特殊片段 ──────────────────────────────────

        /// <summary>生成 Boss 前奏片段：预警、补给、教学</summary>
        public void SpawnBossPrelude(float runDistance, BossDefinition bossDef, float preludeLengthMeters = 100f)
        {
            // 在 Boss 出现前 preludeLengthMeters 处开始生成特殊片段
            var startZ = nextSpawnZ + worldTuning.spawnDistance;

            // 1. 主题化预兆视觉（如冰裂、风道等）- 通过降低障碍密度和放置特色元素实现
            // 2. 放置推荐反制道具
            if (bossDef.PreferredPowerUps != null && bossDef.PreferredPowerUps.Length > 0)
            {
                var powerUp = bossDef.PreferredPowerUps[Random.Range(0, bossDef.PreferredPowerUps.Length)];
                SpawnSpecificPowerUp(startZ + 20f, runDistance, powerUp);
            }

            // 3. 教学金币线：展示正确动作路线
            var lane = (RunnerLane)Random.Range(-1, 2);
            SpawnTutorialCoinLine(startZ + 40f, lane, 6, runDistance, bossDef.Archetype);

            // 4. 降低普通障碍密度（通过调整 nextSpawnZ 实现更大的间隔）
            nextSpawnZ += worldTuning.spawnSpacingMax * 1.5f;
        }

        /// <summary>生成 Boss 战后奖励段</summary>
        public void SpawnBossReward(float runDistance, BossDefinition bossDef)
        {
            var startZ = nextSpawnZ + worldTuning.spawnDistance;

            // 1. 鱼干大串
            var rewardLane = (RunnerLane)Random.Range(-1, 2);
            for (var i = 0; i < 12; i++)
                SpawnCoin(startZ + 10f + i * 1.85f, rewardLane, runDistance, ownedBossReward: true);

            // 2. 短暂安全冲刺道具
            RegisterBossReward(SpawnSpecificPowerUp(startZ + 35f, runDistance, PowerUpKind.Dash));

            // 3. 主题特色道具
            if (bossDef.PreferredPowerUps != null && bossDef.PreferredPowerUps.Length > 0)
            {
                var powerUp = bossDef.PreferredPowerUps[Random.Range(0, bossDef.PreferredPowerUps.Length)];
                RegisterBossReward(SpawnSpecificPowerUp(startZ + 50f, runDistance, powerUp));
            }

            // 4. 安全区间：再次降低障碍密度
            nextSpawnZ += worldTuning.spawnSpacingMax * 2f;
        }

        private void RegisterBossReward(GameObject entity)
        {
            if (entity != null) bossRewardEntities.Add(entity);
        }

        private GameObject SpawnSpecificPowerUp(float z, float runDistance, PowerUpKind kind)
        {
            var powerLane = (RunnerLane)Random.Range(-1, 2);
            var (color, secondaryColor) = GetPowerUpColors(kind);
            var power = CreatePowerUpRoot($"PowerUp {kind}", color, kind, secondaryColor);
            if (power == null) return null;
            var laneX = powerLane.ToX(laneWidth);
            var surface = TrackTerrain.SurfaceY(z, runDistance, baseGroundY);
            power.transform.position = new Vector3(laneX, surface + PowerUpHoverHeight, z);
            powerUps[power] = kind;
            return power;
        }

        private void SpawnTutorialCoinLine(float z, RunnerLane lane, int count, float runDistance, BossArchetype archetype)
        {
            // 根据 Boss 类型，金币线展示不同的动作路线
            for (var i = 0; i < count; i++)
            {
                var bonusY = archetype switch
                {
                    BossArchetype.Aerial => i % 2 == 0 ? 0.8f : 0f, // 跳跃路线
                    BossArchetype.Grounded => i % 2 == 0 ? -0.2f : 0.4f, // 滑铲/地面路线
                    _ => (i % 3 == 1) ? 0.5f : 0f, // 混合路线
                };
                SpawnCoin(z + i * 1.85f, lane, runDistance, bonusY);
            }
        }

    }
}
