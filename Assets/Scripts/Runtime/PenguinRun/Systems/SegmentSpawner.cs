using System.Collections.Generic;
using PenguinRun.Core;
using PenguinRun.Game;
using UnityEngine;

namespace PenguinRun
{
    internal sealed partial class SegmentSpawner
    {
        // 所有物体的显示高度都基于 TrackTerrain.SurfaceY (地面表面) 计算
        private const float CoinHoverHeight = 0.58f;      // 金币悬浮：略高于路面，避免贴雪又不过高
        private const float PowerUpHoverHeight = 0.55f;   // 道具悬浮高度
        private const float OpenObstacleVisualScaleBoost = 1.42f;
        private const float OpenWideWallVisualScaleBoost = 1.32f;
        private const float OpenEnemyVisualScaleBoost = 1.38f;

        private sealed class RollingState
        {
            public GameObject Go;
            public float LaneCenterX;
            public float Amplitude;
            public float Frequency;
            public float Phase;
            /// <summary>绕 Y 轴自转速度（度/秒），风刃等；0 表示不额外旋转。</summary>
            public float SpinYawDegreesPerSec;
        }

        /// <summary>移动敌人 NPC 的运动状态。</summary>
        private sealed class EnemyNpcState
        {
            public GameObject Go;
            public float Phase;
            public bool IsPatrol;     // true = 左右巡逻；false = 斜向扑入
            public float Frequency;   // 巡逻：振荡频率 Hz
            public float Amplitude;   // 巡逻：X 半幅；扑入：X 方向速度
            public float BaseX;       // 巡逻：中心 X
            public float Dy;          // 相对地面的 Y 偏移
        }

        private readonly RunnerWorldTuning worldTuning;
        private readonly RunnerPowerUpTuning powerUpTuning;
        private readonly float laneWidth;
        private readonly RunnerSessionConfig config;
        private readonly SegmentWaveDirector waveDirector;
        private readonly float baseGroundY;

        private float nextSpawnZ;
        private readonly List<RollingState> rollingObstacles = new();
        private readonly List<EnemyNpcState> enemyNpcs = new();
        private readonly Dictionary<GameObject, float> obstacleYDelta = new();
        private readonly Dictionary<GameObject, float> coinYDelta = new();
        private readonly Dictionary<string, GameObject> modelPrefabCache = new();
        private readonly RunnerVisualFactory visualFactory = new();
        private bool slideGateTutorialPending;
        private GameObject routeEventRoot;
        private readonly List<GameObject> routeOutcomeRoots = new();
        private readonly List<GameObject> mechanicObstacleRoots = new();
        private readonly List<GameObject> bossRewardEntities = new();

        private readonly GameObjectPool coinPool;
        private readonly GameObjectPool powerUpPool;

        public SegmentSpawner(
            RunnerWorldTuning worldTuning,
            RunnerPowerUpTuning powerUpTuning,
            float laneWidth,
            RunnerSessionConfig config,
            SegmentWaveDirector waveDirector,
            float baseGroundY)
        {
            this.worldTuning = worldTuning;
            this.powerUpTuning = powerUpTuning;
            this.laneWidth = laneWidth;
            this.config = config;
            routeTheme = config.MapTheme;
            this.waveDirector = waveDirector;
            this.baseGroundY = baseGroundY;
            nextSpawnZ = worldTuning.initialSpawnZ;

            coinPool = new GameObjectPool(() => CoinPickupVisual.Build(visualFactory), null, 60, 200, recycleOnFull: true);
            powerUpPool = new GameObjectPool(BuildPooledPowerUpRoot, null, 24, 80, recycleOnFull: true);
        }

        public readonly List<GameObject> obstacles = new();

        /// <summary>
        /// <see cref="obstacles"/> 的成员镜像，与其在 Add/RemoveAt/Clear 三处同步维护。
        /// 滚球与移动敌人每帧都要判断「自己是否还在场上」，用 List.Contains 是 O(n) 线性扫描，
        /// 障碍密集时整体退化成 O(n²)。
        /// </summary>
        private readonly HashSet<GameObject> obstacleSet = new();
        public readonly Dictionary<GameObject, ObstacleColliderSpec> obstacleSpecs = new();
        public readonly List<ObstacleBehavior> obstacleBehaviors = new();
        public readonly List<GameObject> coins = new();
        public readonly Dictionary<GameObject, PowerUpKind> powerUps = new();
        private readonly List<GameObject> powerKeyScratch = new();
        public readonly List<GameObject> speedGates = new();

        public void Reset()
        {
            ClearSpawnedEntities();
            nextSpawnZ = worldTuning.initialSpawnZ;
            pendingSegmentBanner = false;
            pendingOnboardingPatternHint = false;
            pendingDashHint = false;
            dashHintConsumed = false;
            waveDirector.SetTheme(config.MapTheme);
            routeTheme = config.MapTheme;
            waveDirector.ResetForRun();
        }

        public void ApplyTheme(RunnerMapTheme theme, bool clearSpawnedEntities = true)
        {
            CancelRouteEncounter();
            waveDirector.SetTheme(theme);
            routeTheme = theme;
            if (!clearSpawnedEntities) return;

            ClearSpawnedEntities();
            // 换图后尽快接上新主题障碍，但避免立刻顶脸生成。
            nextSpawnZ = worldTuning.spawnDistance + 12f;
        }


        public void SeedInitialObstacles(float runDistance)
        {
            // 使用当前波次导演的调色板，确保开局障碍匹配地图主题
            var palette = waveDirector.CurrentPalette;
            var count = config.IsOnboarding ? 2 : 5;
            for (var i = 0; i < count; i++)
            {
                SpawnStandardObstacle(26f + i * 18f, (RunnerLane)Random.Range(-1, 2), Random.value > 0.72f, palette, runDistance);
            }

            // 开局金币放在首批障碍（约 26m 起）之前的净空带，避免叠在障碍上。
            SpawnCoinLine(10f, RunnerLane.Center, 6, runDistance);
            SpawnCoinLine(14f, RunnerLane.Left, 4, runDistance);
            SpawnCoinLine(18f, RunnerLane.Right, 4, runDistance);
            if (config.IsOnboarding)
                SpawnPowerUp(32f, runDistance, palette, PowerUpKind.Dash, RunnerLane.Left);
        }

        /// <summary>
        /// 清掉 [zMin,zMax] 内的障碍（及叠在障碍上的金币），给 Boss 前奏/登场留净空跑道。
        /// </summary>
        public void ClearTrackHazardsBetween(float zMin, float zMax)
        {
            // 必须先回收叠在障碍上的金币再销毁障碍：反过来时障碍已不在，
            // 重叠检测恒为 false，这些金币会残留在 Boss 净空带里。
            for (var i = coins.Count - 1; i >= 0; i--)
            {
                var c = coins[i];
                if (c == null)
                {
                    coins.RemoveAt(i);
                    continue;
                }

                var z = c.transform.position.z;
                if (z < zMin || z > zMax) continue;
                if (!IsCoinOverlappingObstacle(c.transform.position)) continue;
                routeFish.Remove(c);
                coinYDelta.Remove(c);
                coinPool.Return(c);
                coins.RemoveAt(i);
            }

            for (var i = obstacles.Count - 1; i >= 0; i--)
            {
                var o = obstacles[i];
                if (o == null)
                {
                    DestroyObstacle(o, i);
                    continue;
                }

                var z = o.transform.position.z;
                if (z >= zMin && z <= zMax)
                    DestroyObstacle(o, i);
            }
        }

        /// <summary>当 boss 战激活时由外部设为 true，暂停障碍生成。</summary>
        public bool SpawnPaused { get; set; }

        private bool pendingSegmentBanner;
        private SegmentPatternKind pendingSegmentBannerKind;
        private bool pendingOnboardingPatternHint;
        private SegmentPatternKind pendingOnboardingPatternKind;
        private bool pendingDashHint;
        private bool dashHintConsumed;

        public bool TryConsumeSegmentBanner(out SegmentPatternKind kind)
        {
            if (!pendingSegmentBanner)
            {
                kind = default;
                return false;
            }

            kind = pendingSegmentBannerKind;
            pendingSegmentBanner = false;
            return true;
        }

        /// <summary>
        /// First wave of an onboarding rotation segment. Used only for competence hints.
        /// </summary>
        public bool TryConsumeOnboardingPatternHint(out SegmentPatternKind kind)
        {
            if (!pendingOnboardingPatternHint)
            {
                kind = default;
                return false;
            }

            kind = pendingOnboardingPatternKind;
            pendingOnboardingPatternHint = false;
            return true;
        }

        public bool TryConsumeDashHint(out string message)
        {
            message = "";
            if (!pendingDashHint) return false;
            pendingDashHint = false;
            dashHintConsumed = true;
            message = "裂纹墙：双击屏幕或按 Shift 冲刺打碎";
            return true;
        }


        public void Tick(float dt, float effectiveSpeed, float runDistance)
        {
            if (dt <= 0f) return;
            nextSpawnZ -= effectiveSpeed * dt;
            if (!SpawnPaused && !RouteEncounterActive && nextSpawnZ < worldTuning.spawnDistance)
            {
                var modifiers = waveDirector.ConsumeWave();
                if (waveDirector.LastConsumeWasFirstWave &&
                    (modifiers.PatternKind == SegmentPatternKind.HazardRhythm ||
                     modifiers.PatternKind == SegmentPatternKind.RewardRun ||
                     modifiers.PatternKind == SegmentPatternKind.EnemyAmbush ||
                     modifiers.PatternKind == SegmentPatternKind.PowerUpTrial))
                {
                    pendingSegmentBanner = true;
                    pendingSegmentBannerKind = modifiers.PatternKind;
                }
                if (config.IsOnboarding
                    && waveDirector.LastConsumeWasFirstWave
                    && (modifiers.PatternKind == SegmentPatternKind.LaneWeave
                        || modifiers.PatternKind == SegmentPatternKind.JumpArc
                        || modifiers.PatternKind == SegmentPatternKind.SlideTunnel
                        || modifiers.PatternKind == SegmentPatternKind.RewardRun))
                {
                    pendingOnboardingPatternHint = true;
                    pendingOnboardingPatternKind = modifiers.PatternKind;
                }
                var lead = Random.Range(worldTuning.spawnLeadMin, worldTuning.spawnLeadMax);
                SpawnPattern(nextSpawnZ + worldTuning.spawnDistance + lead, modifiers, runDistance);
                nextSpawnZ += Random.Range(worldTuning.spawnSpacingMin, worldTuning.spawnSpacingMax);
            }
            else if (SpawnPaused || RouteEncounterActive)
            {
                // boss 战期间，不堆积新障碍生成时机
                nextSpawnZ = Mathf.Max(nextSpawnZ, worldTuning.spawnDistance + 4f);
            }

            TickRouteMarkers(dt, effectiveSpeed, runDistance);

            for (var i = obstacles.Count - 1; i >= 0; i--)
            {
                var obstacle = obstacles[i];
                obstacle.transform.Translate(Vector3.back * (effectiveSpeed * dt), Space.World);
                if (obstacle.transform.position.z < worldTuning.despawnZ)
                {
                    DestroyObstacle(obstacle, i);
                }
            }

            ApplyRollingMotion(dt, runDistance);
            TickEnemies(dt, runDistance);
            TickObstacleBehaviors(dt, runDistance);
            SyncObstacleHeights(runDistance);

            // ── 回收被 PickupSystem 设为非活跃的金币 ──────────────
            for (var i = coins.Count - 1; i >= 0; i--)
            {
                var c = coins[i];
                if (c == null || !c.activeSelf)
                {
                    routeFish.Remove(c);
                    if (c != null) coinPool.Return(c);
                    coinYDelta.Remove(c);
                    coins.RemoveAt(i);
                }
            }

            for (var i = coins.Count - 1; i >= 0; i--)
            {
                var c = coins[i];
                if (c == null)
                {
                    if (!ReferenceEquals(c, null))
                    {
                        coinYDelta.Remove(c);
                    }
                    coins.RemoveAt(i);
                    continue;
                }

                c.transform.Translate(Vector3.back * (effectiveSpeed * dt), Space.World);
                SyncCoinHeight(c, runDistance);
                if (c.transform.position.z < worldTuning.despawnZ)
                {
                    routeFish.Remove(c);
                    coinYDelta.Remove(c);
                    coinPool.Return(c);
                    coins.RemoveAt(i);
                }
            }

            // ── 回收被 PickupSystem 设为非活跃的道具 ──────────────
            // 复用 scratch 列表：仅为了能在遍历中安全 Remove，内容每帧大多不变，
            // 没必要每帧新建两个 List。
            powerKeyScratch.Clear();
            powerKeyScratch.AddRange(powerUps.Keys);
            foreach (var power in powerKeyScratch)
            {
                if (power == null || !power.activeSelf)
                {
                    if (power != null) ReturnPowerUp(power);
                    powerUps.Remove(power);
                }
            }

            powerKeyScratch.Clear();
            powerKeyScratch.AddRange(powerUps.Keys);
            foreach (var power in powerKeyScratch)
            {
                if (power == null)
                {
                    powerUps.Remove(power);
                    continue;
                }

                power.transform.Translate(Vector3.back * (effectiveSpeed * dt), Space.World);
                power.transform.Rotate(Vector3.up, 130f * dt, Space.World);
                SyncPowerUpHeight(power, runDistance);
                if (power.transform.position.z < worldTuning.despawnZ)
                {
                    ReturnPowerUp(power);
                    powerUps.Remove(power);
                }
            }

            // 速度门移动
            for (var i = speedGates.Count - 1; i >= 0; i--)
            {
                var gate = speedGates[i];
                if (gate == null) { speedGates.RemoveAt(i); continue; }
                gate.transform.Translate(Vector3.back * (effectiveSpeed * dt), Space.World);
                var gz = gate.transform.position.z;
                gate.transform.position = new Vector3(gate.transform.position.x,
                    TrackTerrain.SurfaceY(gz, runDistance, baseGroundY), gz);
                if (gz < worldTuning.despawnZ)
                {
                    Object.Destroy(gate);
                    speedGates.RemoveAt(i);
                }
            }
        }

        private void SyncObstacleHeights(float runDistance)
        {
            for (var i = 0; i < obstacles.Count; i++)
            {
                var o = obstacles[i];
                if (!obstacleYDelta.TryGetValue(o, out var dy)) continue;
                var p = o.transform.position;
                // dy 是障碍中心相对于地面表面的高度偏移
                p.y = TrackTerrain.SurfaceY(p.z, runDistance, baseGroundY) + dy;
                o.transform.position = p;
            }
        }

        private void SyncCoinHeight(GameObject c, float runDistance)
        {
            var p = c.transform.position;
            coinYDelta.TryGetValue(c, out var bonusY);
            p.y = TrackTerrain.SurfaceY(p.z, runDistance, baseGroundY) + CoinHoverHeight + bonusY;
            c.transform.position = p;
        }

        private void SyncPowerUpHeight(GameObject power, float runDistance)
        {
            var p = power.transform.position;
            p.y = TrackTerrain.SurfaceY(p.z, runDistance, baseGroundY) + PowerUpHoverHeight;
            power.transform.position = p;
        }

        private void DestroyObstacle(GameObject obstacle, int index)
        {
            routeObstacles.Remove(obstacle);
            obstacleYDelta.Remove(obstacle);
            // 清理关联行为
            for (var b = obstacleBehaviors.Count - 1; b >= 0; b--)
            {
                if (obstacleBehaviors[b] != null && obstacleBehaviors[b].gameObject == obstacle)
                {
                    Object.Destroy(obstacleBehaviors[b]);
                    obstacleBehaviors.RemoveAt(b);
                }
            }
            Object.Destroy(obstacle);
            obstacleSpecs.Remove(obstacle);
            obstacles.RemoveAt(index);
            obstacleSet.Remove(obstacle);
            for (var r = rollingObstacles.Count - 1; r >= 0; r--)
            {
                if (rollingObstacles[r].Go == obstacle)
                {
                    rollingObstacles.RemoveAt(r);
                }
            }
            for (var e = enemyNpcs.Count - 1; e >= 0; e--)
            {
                if (enemyNpcs[e].Go == obstacle)
                {
                    enemyNpcs.RemoveAt(e);
                }
            }
        }

        private void TickObstacleBehaviors(float dt, float runDistance)
        {
            for (var i = obstacleBehaviors.Count - 1; i >= 0; i--)
            {
                var behavior = obstacleBehaviors[i];
                if (behavior == null)
                {
                    obstacleBehaviors.RemoveAt(i);
                    continue;
                }
                behavior.Tick(dt, runDistance, 0f);
            }
        }

        private void ClearSpawnedEntities()
        {
            CancelRouteEncounter();
            foreach (var obstacle in obstacles) Object.Destroy(obstacle);
            obstacles.Clear();
            obstacleSet.Clear();
            obstacleSpecs.Clear();
            obstacleBehaviors.Clear();
            rollingObstacles.Clear();
            enemyNpcs.Clear();

            foreach (var c in coins) coinPool.Return(c);
            coins.Clear();

            foreach (var power in powerUps.Keys) ReturnPowerUp(power);
            powerUps.Clear();

            foreach (var gate in speedGates) Object.Destroy(gate);
            speedGates.Clear();

            if (routeEventRoot != null) Object.Destroy(routeEventRoot);
            routeEventRoot = null;
            foreach (var root in routeOutcomeRoots) Object.Destroy(root);
            routeOutcomeRoots.Clear();

            obstacleYDelta.Clear();
            coinYDelta.Clear();
            mechanicObstacleRoots.Clear();
            bossRewardEntities.Clear();
        }


        /// <summary>护盾/受击消除障碍时调用，同步滚动雪球列表。</summary>
        public void TryRemoveObstacle(GameObject obstacle)
        {
            if (obstacle == null) return;
            var idx = obstacles.IndexOf(obstacle);
            if (idx < 0) return;
            DestroyObstacle(obstacle, idx);
        }


        private void ApplyRollingMotion(float dt, float runDistance)
        {
            for (var i = rollingObstacles.Count - 1; i >= 0; i--)
            {
                var state = rollingObstacles[i];
                if (state.Go == null || !obstacleSet.Contains(state.Go))
                {
                    rollingObstacles.RemoveAt(i);
                    continue;
                }

                var dyFallback = 0.53f;
                if (!obstacleYDelta.TryGetValue(state.Go, out var dy))
                {
                    dy = dyFallback;
                }

                state.Phase += dt * state.Frequency * Mathf.PI * 2f;
                var pos = state.Go.transform.position;
                pos.x = state.LaneCenterX + Mathf.Sin(state.Phase) * state.Amplitude;
                pos.y = TrackTerrain.SurfaceY(pos.z, runDistance, baseGroundY) + dy;
                state.Go.transform.position = pos;

                if (state.SpinYawDegreesPerSec != 0f)
                {
                    state.Go.transform.Rotate(Vector3.up, state.SpinYawDegreesPerSec * dt, Space.World);
                }
            }
        }

        private void SpawnPattern(float z, SegmentSpawnModifiers m, float runDistance)
        {
            // 随跑动距离逐步引入移动敌人 NPC（距离 >200m 开始出现，>500m 概率提升）。
            // 新手期完全抑制，避免首局信息过载。
            if (!config.IsOnboarding)
            {
                var enemyChance = runDistance > 500f ? 0.24f : runDistance > 200f ? 0.13f : 0f;
                if (Random.value < enemyChance)
                {
                    var spawnZ = z + Random.Range(10f, 20f);
                    if (Random.value > 0.45f)
                        SpawnPatrolEnemy(spawnZ, runDistance);
                    else
                        SpawnSwoopEnemy(spawnZ, runDistance);
                }
            }

            if (TrySpawnDirectedPattern(z, m, runDistance))
            {
                return;
            }

            var powerChance = ShiftProb(
                powerUpTuning.basePowerUpChance + config.PolarLevel * powerUpTuning.powerUpChancePerPolarLevel,
                m.PowerChanceDelta);
            var coinChance = ShiftProb(powerUpTuning.coinLineSpawnChance, m.CoinLineChanceDelta);
            var singleChance = ShiftProb(powerUpTuning.singleObstacleSpawnChance, m.SingleObstacleChanceDelta);

            var roll = Random.value;
            var thresholdCoins = powerChance + coinChance;
            var thresholdSingle = thresholdCoins + singleChance;

            if (roll < powerChance)
            {
                SpawnPowerUp(z + 1.8f, runDistance, m.Palette);
                SpawnCoinLine(z + 5.2f, (RunnerLane)Random.Range(-1, 2), 3, runDistance);
                return;
            }

            if (roll < thresholdCoins)
            {
                SpawnCoinLine(z, (RunnerLane)Random.Range(-1, 2), Random.Range(4, 7), runDistance);
                return;
            }

            if (roll < thresholdSingle)
            {
                var blockedLane = (RunnerLane)Random.Range(-1, 2);
                TrySpawnThreatObstacle(z, blockedLane, m, runDistance);
                SpawnCoinLine(z + 6f, PickDifferentLane(blockedLane), 3, runDistance);
                return;
            }

            var safeLane = (RunnerLane)Random.Range(-1, 2);
            for (var laneValue = -1; laneValue <= 1; laneValue++)
            {
                var obstacleLane = (RunnerLane)laneValue;
                if (obstacleLane == safeLane) continue;
                var zz = z + Random.Range(-1.6f, 1.6f) + (int)obstacleLane * 0.35f;
                if (Random.value < Mathf.Clamp01(m.WideWallChance))
                {
                    TrySpawnWideThreat(zz, obstacleLane, m.Palette, runDistance);
                }
                else
                {
                    SpawnStandardObstacle(zz, obstacleLane, LowRoll(m), m.Palette, runDistance);
                }
            }

            SpawnCoinLine(z + 8.5f, safeLane, 4, runDistance);
        }

        private bool TrySpawnDirectedPattern(float z, SegmentSpawnModifiers m, float runDistance)
        {
            return m.PatternKind switch
            {
                SegmentPatternKind.LaneWeave when Random.value < 0.58f => SpawnLaneWeave(z, m, runDistance),
                SegmentPatternKind.JumpArc when Random.value < 0.55f => SpawnJumpArc(z, m, runDistance),
                SegmentPatternKind.SlideTunnel when Random.value < 0.52f => SpawnSlideTunnel(z, m, runDistance),
                SegmentPatternKind.RewardRun when Random.value < 0.68f => SpawnRewardRun(z, m, runDistance),
                SegmentPatternKind.SplitGate when Random.value < 0.58f => SpawnSplitGate(z, m, runDistance),
                SegmentPatternKind.HazardRhythm when Random.value < 0.65f => SpawnHazardRhythm(z, m, runDistance),
                SegmentPatternKind.EnemyAmbush when Random.value < 0.60f => SpawnEnemyAmbush(z, m, runDistance),
                SegmentPatternKind.PowerUpTrial when Random.value < 0.72f => SpawnPowerUpTrial(z, m, runDistance),
                _ => false,
            };
        }

        /// <summary>危险节奏段：高密度连续障碍，考验反应和节奏感。</summary>
        private bool SpawnHazardRhythm(float z, SegmentSpawnModifiers m, float runDistance)
        {
            var safeLane = (RunnerLane)Random.Range(-1, 2);
            var alternateOffset = 0f;
            for (var i = 0; i < 6; i++)
            {
                var blockZ = z + i * 2.1f + alternateOffset;
                // 交替封堵不同车道，保留一条安全路线
                var dangerLane = (RunnerLane)Mathf.Clamp((int)safeLane + (i % 2 == 0 ? 1 : -1), -1, 1);
                if (i % 3 == 2)
                {
                    TrySpawnWideThreat(blockZ, dangerLane, m.Palette, runDistance);
                }
                else
                {
                    SpawnStandardObstacle(blockZ, dangerLane, i % 2 == 0, m.Palette, runDistance);
                }

                if (i % 2 == 1)
                {
                    SpawnCoin(blockZ - 0.8f, safeLane, runDistance);
                }
            }

            SpawnCoinLine(z + 13f, safeLane, 3, runDistance);
            return true;
        }

        /// <summary>敌人伏击段：移动敌人集中出现，考验动态判断。</summary>
        private bool SpawnEnemyAmbush(float z, SegmentSpawnModifiers m, float runDistance)
        {
            var count = Random.Range(2, 4);
            for (var i = 0; i < count; i++)
            {
                var spawnZ = z + i * 5.5f + Random.Range(-0.5f, 1.5f);
                // 交替巡逻和扑入
                if (i % 2 == 0)
                    SpawnPatrolEnemy(spawnZ, runDistance);
                else
                    SpawnSwoopEnemy(spawnZ, runDistance);
            }

            // 在敌人间隙放金币线作为引导
            var coinLane = (RunnerLane)Random.Range(-1, 2);
            SpawnCoinLine(z + 2.5f, coinLane, 4, runDistance);

            // 敌人段后给一个道具奖励
            if (Random.value < 0.55f)
            {
                SpawnPowerUp(z + count * 5.5f + 2f, runDistance, m.Palette);
            }

            return true;
        }

        /// <summary>道具展示段：道具密集出现，让玩家体验组合效果。</summary>
        private bool SpawnPowerUpTrial(float z, SegmentSpawnModifiers m, float runDistance)
        {
            // 2-3 个道具排布在不同车道
            var lanes = new[] { RunnerLane.Left, RunnerLane.Center, RunnerLane.Right };
            Shuffle(lanes);
            var pickupCount = Random.Range(2, 4);
            for (var i = 0; i < pickupCount; i++)
            {
                SpawnPowerUp(z + i * 5.5f, runDistance, m.Palette);
            }

            // 各个道具之间铺金币引导
            SpawnCoinLine(z + 1.2f, lanes[0], 3, runDistance);
            SpawnCoinLine(z + 7.0f, lanes[1], 3, runDistance);

            // 末尾放一个轻量障碍，测试玩家能否带着道具效果通过
            if (pickupCount >= 2)
            {
                SpawnStandardObstacle(z + pickupCount * 5.5f + 2f, lanes[2], false, m.Palette, runDistance);
            }

            // 道具展示段末尾放速度门让玩家爽一下
            if (Random.value < 0.4f)
            {
                SpawnSpeedGate(z + pickupCount * 5.5f + 4f, (RunnerLane)Random.Range(-1, 2), runDistance, m.Palette);
            }

            return true;
        }

        private static void Shuffle<T>(T[] arr)
        {
            for (var i = arr.Length - 1; i > 0; i--)
            {
                var j = Random.Range(0, i + 1);
                (arr[i], arr[j]) = (arr[j], arr[i]);
            }
        }

        private bool SpawnLaneWeave(float z, SegmentSpawnModifiers m, float runDistance)
        {
            var start = (RunnerLane)Random.Range(-1, 2);
            var dir = Random.value > 0.5f ? 1 : -1;
            for (var i = 0; i < 5; i++)
            {
                var laneValue = Mathf.Clamp((int)start + (i % 3 - 1) * dir, -1, 1);
                var lane = (RunnerLane)laneValue;
                SpawnCoin(z + i * 2.05f, lane, runDistance);

                if (i == 1 || i == 3)
                {
                    var blocker = (RunnerLane)Mathf.Clamp(laneValue - dir, -1, 1);
                    if (blocker != lane)
                    {
                        TrySpawnThreatObstacle(z + i * 2.05f + 0.85f, blocker, m, runDistance);
                    }
                }
            }

            return true;
        }

        private bool SpawnJumpArc(float z, SegmentSpawnModifiers m, float runDistance)
        {
            var lane = (RunnerLane)Random.Range(-1, 2);
            SpawnStandardObstacle(z + 1.2f, lane, true, m.Palette, runDistance);
            for (var i = 0; i < 5; i++)
            {
                var t = i / 4f;
                var arcY = Mathf.Sin(t * Mathf.PI) * 1.15f;
                SpawnCoin(z + 0.4f + i * 1.15f, lane, runDistance, arcY);
            }

            SpawnFlyingObstacle(z + 7.2f, lane, m.Palette, runDistance);
            SpawnCoinLine(z + 7f, PickDifferentLane(lane), 3, runDistance);
            return true;
        }

        private bool SpawnSlideTunnel(float z, SegmentSpawnModifiers m, float runDistance)
        {
            var safeLane = (RunnerLane)Random.Range(-1, 2);
            var gateCount = Random.Range(2, 4);
            for (var laneValue = -1; laneValue <= 1; laneValue++)
            {
                var lane = (RunnerLane)laneValue;
                if (lane == safeLane) continue;
                SpawnSlideGateLine(z + 0.35f, lane, gateCount, m.Palette, runDistance);
                SpawnWideSlideGate(z + gateCount * 2.4f + 1.1f, lane, m.Palette, runDistance);
            }

            SpawnCoinLine(z + 0.7f, safeLane, 4, runDistance);
            return true;
        }

        private bool SpawnRewardRun(float z, SegmentSpawnModifiers m, float runDistance)
        {
            var lane = (RunnerLane)Random.Range(-1, 2);
            var count = Random.Range(6, 9);
            for (var i = 0; i < count; i++)
            {
                var drift = Mathf.Sin(i * 0.9f) > 0.55f ? 1 : Mathf.Sin(i * 0.9f) < -0.55f ? -1 : 0;
                var nextLane = (RunnerLane)Mathf.Clamp((int)lane + drift, -1, 1);
                SpawnCoin(z + i * 1.7f, nextLane, runDistance, i % 3 == 1 ? 0.34f : 0f);
            }

            if (Random.value < 0.35f)
            {
                SpawnPowerUp(z + count * 1.7f + 0.8f, runDistance, m.Palette);
            }

            // 有时在奖励段末尾放一个速度门
            if (Random.value < 0.25f)
            {
                var gateLane = (RunnerLane)Random.Range(-1, 2);
                SpawnSpeedGate(z + count * 1.7f + 3f, gateLane, runDistance, m.Palette);
            }

            return true;
        }

        private bool SpawnSplitGate(float z, SegmentSpawnModifiers m, float runDistance)
        {
            var safeLane = (RunnerLane)Random.Range(-1, 2);
            for (var laneValue = -1; laneValue <= 1; laneValue++)
            {
                var lane = (RunnerLane)laneValue;
                if (lane == safeLane)
                {
                    SpawnCoinLine(z + 1.5f, lane, 3, runDistance);
                    continue;
                }

                if (Random.value < 0.46f + Mathf.Clamp01(m.WideWallChance))
                {
                    TrySpawnWideThreat(z + Random.Range(-0.3f, 0.85f), lane, m.Palette, runDistance);
                }
                else
                {
                    SpawnStandardObstacle(z + Random.Range(-0.5f, 0.9f), lane, LowRoll(m), m.Palette, runDistance);
                }
            }

            return true;
        }

        private static float ShiftProb(float baseVal, float delta) => Mathf.Clamp(baseVal + delta, 0.03f, 0.82f);

        private static bool LowRoll(SegmentSpawnModifiers m)
        {
            var threshold = Mathf.Clamp(0.50f - m.LowObstacleBiasDelta, 0.15f, 0.92f);
            return Random.value > threshold;
        }

        private void TrySpawnWideThreat(float z, RunnerLane lane, SegmentObstaclePalette palette, float runDistance)
        {
            if (runDistance < 400f && Random.value < 0.25f)
            {
                SpawnWideSlideGate(z, lane, palette, runDistance);
            }
            else
            {
                SpawnWideSnowWall(z, lane, palette, runDistance);
            }
        }

        private void SpawnSlideGateLine(float z, RunnerLane lane, int count, SegmentObstaclePalette palette, float runDistance)
        {
            for (var i = 0; i < count; i++)
            {
                SpawnStandardObstacle(z + i * 2.4f, lane, true, palette, runDistance);
            }
        }

        private void TrySpawnThreatObstacle(float z, RunnerLane lane, SegmentSpawnModifiers m, float runDistance)
        {
            var roll = Random.value;
            var specialtyGate = Mathf.Clamp01(0.08f + runDistance / 5000f);
            if (roll < specialtyGate)
            {
                if (m.Palette == SegmentObstaclePalette.AuroraGlow || m.Palette == SegmentObstaclePalette.SkyCloud)
                {
                    SpawnSpinBlade(z, lane, m.Palette, runDistance);
                }
                else
                {
                    SpawnFallingCluster(z, lane, m.Palette, runDistance);
                }
                return;
            }

            // HazardRhythm 段在较长跑动后引入刺针/中等障碍
            if (m.PatternKind == SegmentPatternKind.HazardRhythm && runDistance > 300f)
            {
                var newTypeRoll = Random.value;
                if (newTypeRoll < 0.30f)
                {
                    SpawnSpikeObstacle(z, lane, m.Palette, runDistance);
                    return;
                }
                if (newTypeRoll < 0.52f)
                {
                    SpawnMediumBarrier(z, lane, m.Palette, runDistance);
                    return;
                }
            }

            // 动态机关障碍：中后程逐步引入定时门与巡移浮障（IsCollidable 动态开关 / 根节点移动判定）
            if (runDistance > 500f)
            {
                var dynamicRoll = Random.value;
                if (dynamicRoll < 0.10f)
                {
                    SpawnTimedGate(z, lane, m.Palette, runDistance);
                    return;
                }
                if (runDistance > 750f && dynamicRoll < 0.17f)
                {
                    SpawnMovingBarrier(z, lane, m.Palette, runDistance);
                    return;
                }
            }

            roll = Mathf.InverseLerp(specialtyGate, 1f, roll);
            var rollingGate = m.RollingSpawnChance;
            var wideGate = rollingGate + m.WideWallChance;
            if (roll < rollingGate)
            {
                SpawnRollingSnowball(z, lane, m.Palette, runDistance);
            }
            else if (roll < wideGate)
            {
                TrySpawnWideThreat(z, lane, m.Palette, runDistance);
            }
            else
            {
                SpawnStandardObstacle(z, lane, LowRoll(m), m.Palette, runDistance);
            }
        }

        private void SpawnSpinBlade(float z, RunnerLane lane, SegmentObstaclePalette palette, float runDistance)
        {
            var root = new GameObject("Spin Blade Hazard");
            var dy = 0.72f;
            var surface = TrackTerrain.SurfaceY(z, runDistance, baseGroundY);
            root.transform.position = new Vector3(lane.ToX(laneWidth), surface + dy, z);

            var baseTint = palette == SegmentObstaclePalette.SkyCloud
                ? new Color(0.55f, 0.58f, 0.65f)
                : new Color(0.42f, 0.38f, 0.48f);
            TryAttachObstacleModel(root.transform, "cliff_block_stone", new Vector3(0f, -0.42f, 0f),
                new Vector3(0.85f, 0.12f, 0.85f), baseTint);

            var bladeColor = palette == SegmentObstaclePalette.SkyCloud
                ? new Color(1f, 0.92f, 0.25f, 0.92f)
                : new Color(0.65f, 0.42f, 1f, 0.92f);
            AddChildPrimitive(root.transform, "Blade Hub", PrimitiveType.Sphere, Vector3.zero, Vector3.one * 0.52f, bladeColor, true);
            for (var i = 0; i < 4; i++)
            {
                var blade = AddChildPrimitive(root.transform, $"Blade {i}", PrimitiveType.Cube,
                    Vector3.zero, new Vector3(1.45f, 0.1f, 0.28f), bladeColor, true);
                blade.transform.localRotation = Quaternion.Euler(0f, i * 45f, 0f);
            }
            AddChildPrimitive(root.transform, "Warning Ring", PrimitiveType.Cylinder,
                new Vector3(0f, -0.55f, 0f), new Vector3(1.78f, 0.05f, 1.78f),
                new Color(1f, 0.28f, 0.08f, 0.55f), true);

            RegisterObstacle(root, ObstacleColliderSpec.Enemy, dy, ObstacleKind.Rolling);
            rollingObstacles.Add(new RollingState
            {
                Go = root,
                LaneCenterX = lane.ToX(laneWidth),
                Amplitude = 0.28f,
                Frequency = 2.4f,
                Phase = Random.Range(0f, Mathf.PI * 2f),
                SpinYawDegreesPerSec = 288f,
            });
        }

        private void SpawnFallingCluster(float z, RunnerLane lane, SegmentObstaclePalette palette, float runDistance)
        {
            var root = new GameObject("Falling Crystal Cluster");
            var dy = 0.78f;
            var surface = TrackTerrain.SurfaceY(z, runDistance, baseGroundY);
            root.transform.position = new Vector3(lane.ToX(laneWidth), surface + dy, z);

            var core = palette switch
            {
                SegmentObstaclePalette.OceanCoral => new Color(0.95f, 0.45f, 0.52f),
                SegmentObstaclePalette.CedarWood => new Color(0.55f, 0.36f, 0.2f),
                SegmentObstaclePalette.MistFog => new Color(0.62f, 0.7f, 0.78f),
                _ => new Color(0.58f, 0.9f, 1f),
            };
            for (var i = 0; i < 3; i++)
            {
                var x = (i - 1) * 0.28f;
                var shard = AddChildPrimitive(root.transform, $"Shard {i}", PrimitiveType.Cube,
                    new Vector3(x, 0.08f + i * 0.1f, 0f),
                    new Vector3(0.32f, 1.22f - i * 0.18f, 0.32f),
                    Color.Lerp(core, Color.white, i * 0.12f), true);
                shard.transform.localRotation = Quaternion.Euler(0f, 25f + i * 35f, 12f - i * 10f);
            }
            AddChildPrimitive(root.transform, "Impact Flash", PrimitiveType.Cylinder,
                new Vector3(0f, -0.62f, 0f), new Vector3(1.58f, 0.05f, 1.58f),
                new Color(1f, 0.42f, 0.15f, 0.48f), true);

            RegisterObstacle(root, ObstacleColliderSpec.SmallJumpable, dy, ObstacleKind.StandardHigh);
        }

        private static RunnerLane PickDifferentLane(RunnerLane lane)
        {
            return lane switch
            {
                RunnerLane.Left => RunnerLane.Center,
                RunnerLane.Center => Random.value > 0.5f ? RunnerLane.Left : RunnerLane.Right,
                RunnerLane.Right => Random.value > 0.5f ? RunnerLane.Left : RunnerLane.Center,
                _ => RunnerLane.Center,
            };
        }

        /// <summary>
        /// Consumes a pending slide-gate opportunity flag. Caller decides whether to show a hint
        /// based on competence-driven onboarding state (no longer writes PlayerPrefs).
        /// </summary>
        public bool TryConsumeSlideGateTutorial(out string message)
        {
            message = "";
            if (!slideGateTutorialPending) return false;

            slideGateTutorialPending = false;
            message = "下滑铲从横梁下穿过！";
            return true;
        }

        private void RegisterObstacle(
            GameObject obstacle,
            in ObstacleColliderSpec spec,
            float yDeltaFromFlatGround,
            ObstacleKind kind = ObstacleKind.StandardHigh)
        {
            obstacles.Add(obstacle);
            obstacleSet.Add(obstacle);
            obstacleSpecs[obstacle] = spec;
            obstacleYDelta[obstacle] = yDeltaFromFlatGround;

            var marker = obstacle.GetComponent<ObstacleKindMarker>();
            if (marker == null)
            {
                marker = obstacle.AddComponent<ObstacleKindMarker>();
            }

            marker.Kind = kind;

            if ((kind == ObstacleKind.SlideGate || kind == ObstacleKind.WideSlideGate)
                && config.IsOnboarding)
            {
                slideGateTutorialPending = true;
            }
        }

        // ── 移动敌人 NPC ───────────────────────────────────────────────────────────

        /// <summary>每帧更新所有移动敌人的横向位置，Z 方向已由主障碍循环统一推进。</summary>
        private void TickEnemies(float dt, float runDistance)
        {
            for (var i = enemyNpcs.Count - 1; i >= 0; i--)
            {
                var state = enemyNpcs[i];
                if (state.Go == null || !obstacleSet.Contains(state.Go))
                {
                    enemyNpcs.RemoveAt(i);
                    continue;
                }

                var pos = state.Go.transform.position;
                if (state.IsPatrol)
                {
                    state.Phase += dt * state.Frequency * Mathf.PI * 2f;
                    pos.x = state.BaseX + Mathf.Sin(state.Phase) * state.Amplitude;
                }
                else
                {
                    // 斜向扑入：以固定 X 速度横扫跑道
                    pos.x += state.Amplitude * dt;
                    if (Mathf.Abs(pos.x) > 7.5f)
                    {
                        var idx = obstacles.IndexOf(state.Go);
                        if (idx >= 0) DestroyObstacle(state.Go, idx);
                        // DestroyObstacle already removed from enemyNpcs
                        continue;
                    }
                }

                // 慢速自转，强化"活体威胁"感
                state.Go.transform.Rotate(Vector3.up, 65f * dt, Space.World);
                var cur = state.Go.transform.position;
                cur.x = pos.x;
                state.Go.transform.position = cur;
            }
        }

        private void SpawnPatrolEnemy(float z, float runDistance)
        {
            var enemy = new GameObject("Patrol Enemy");
            var dy = 0.6f; // 敌人中心到脚底的高度
            var surface = TrackTerrain.SurfaceY(z, runDistance, baseGroundY);
            enemy.transform.position = new Vector3(0f, surface + dy, z);

            var builtWithModel = TryBuildOpenModelEnemy(enemy.transform, true);
            if (!builtWithModel)
            {
                builtWithModel = AttachProceduralMesh(
                    enemy.transform,
                    ObstacleMeshBuilder.BuildEnemyBody(),
                    new Color(0.62f, 0.08f, 0.12f),
                    "ProcEnemy",
                    new Vector3(0f, 0.65f, 0f));
                if (builtWithModel)
                {
                    AddChildPrimitive(enemy.transform, "Eye L", PrimitiveType.Sphere,
                        new Vector3(-0.16f, 1.37f, -0.34f), new Vector3(0.17f, 0.17f, 0.17f), new Color(1f, 0.88f, 0.15f));
                    AddChildPrimitive(enemy.transform, "Eye R", PrimitiveType.Sphere,
                        new Vector3(0.16f, 1.37f, -0.34f), new Vector3(0.17f, 0.17f, 0.17f), new Color(1f, 0.88f, 0.15f));
                }
            }
            // 危险光环（放在脚底）
            AddChildPrimitive(enemy.transform, "Danger Ring", PrimitiveType.Cylinder,
                new Vector3(0f, 0.05f, 0f), new Vector3(1.72f, 0.055f, 1.72f),
                new Color(1f, 0.28f, 0.08f, 0.75f), true);

            RegisterObstacle(enemy, ObstacleColliderSpec.PatrolEnemy, dy, ObstacleKind.Enemy);
            enemyNpcs.Add(new EnemyNpcState
            {
                Go = enemy,
                IsPatrol = true,
                Phase = Random.Range(0f, Mathf.PI * 2f),
                Frequency = 0.6f,
                Amplitude = laneWidth * 1.1f,
                BaseX = 0f,
                Dy = dy,
            });
        }

        private void SpawnSwoopEnemy(float z, float runDistance)
        {
            var fromLeft = Random.value > 0.5f;
            var startX = fromLeft ? -7f : 7f;
            var velocityX = fromLeft ? 5.2f : -5.2f;

            var enemy = new GameObject("Swoop Enemy");
            var dy = 0.5f; // 俯冲敌人中心高度（飞行状态，略高于地面）
            var surface = TrackTerrain.SurfaceY(z, runDistance, baseGroundY);
            enemy.transform.position = new Vector3(startX, surface + dy, z);

            var builtWithModel = TryBuildOpenModelEnemy(enemy.transform, false);
            if (!builtWithModel)
            {
                // 俯冲鸟形状 —— 深紫扁平机身
                AddChildPrimitive(enemy.transform, "Swoop Body", PrimitiveType.Cube,
                    new Vector3(0f, 0.25f, 0f), new Vector3(0.52f, 0.52f, 0.82f), new Color(0.22f, 0.14f, 0.38f));
                var wingSide = fromLeft ? 1f : -1f;
                AddChildPrimitive(enemy.transform, "Wing Main", PrimitiveType.Cube,
                    new Vector3(wingSide * 0.65f, 0.31f, 0f), new Vector3(1.3f, 0.1f, 0.58f),
                    new Color(0.32f, 0.18f, 0.5f));
                AddChildPrimitive(enemy.transform, "Wing Tip", PrimitiveType.Cube,
                    new Vector3(wingSide * 1.1f, 0.35f, -0.18f), new Vector3(0.48f, 0.07f, 0.38f),
                    new Color(0.82f, 0.28f, 0.6f));
            }
            // 热焰前缘发光
            AddChildPrimitive(enemy.transform, "Danger Glow", PrimitiveType.Cube,
                new Vector3(0f, 0.25f, -0.44f), new Vector3(0.44f, 0.07f, 0.1f),
                new Color(1f, 0.45f, 0.18f, 0.9f), true);

            RegisterObstacle(enemy, ObstacleColliderSpec.SwoopEnemy, dy, ObstacleKind.Enemy);
            enemyNpcs.Add(new EnemyNpcState
            {
                Go = enemy,
                IsPatrol = false,
                Phase = 0f,
                Frequency = 0f,
                Amplitude = velocityX,
                BaseX = startX,
                Dy = dy,
            });
        }

        // ── 标准障碍 ───────────────────────────────────────────────────────────────

        private void SpawnStandardObstacle(float z, RunnerLane obstacleLane, bool lowBarrier, SegmentObstaclePalette palette, float runDistance)
        {
            var obstacle = new GameObject(lowBarrier ? "Slide Gate" : "Track Obstacle");
            // dy 是障碍中心相对于地面表面的高度
            // 低障碍（滑铲障碍）：中心略高于地面，高度约0.2f
            // 高障碍：中心高度约0.55f
            var dy = lowBarrier ? 0.2f : 0.55f;
            var surface = TrackTerrain.SurfaceY(z, runDistance, baseGroundY);
            obstacle.transform.position = new Vector3(obstacleLane.ToX(laneWidth), surface + dy, z);

            // 外观优先级：开源模型 > 程序化 Mesh > Primitive 回退
            var obstacleKind = lowBarrier ? ObstacleKind.SlideGate : ObstacleKind.StandardHigh;
            var builtWithModel = visualFactory.TryAttachObstacle(obstacle.transform, routeTheme, obstacleKind);
            if (!builtWithModel)
                builtWithModel = TryBuildOpenModelObstacle(obstacle.transform, lowBarrier, palette);
            if (!builtWithModel)
                builtWithModel = TryBuildProceduralObstacle(obstacle.transform, lowBarrier, palette);

            if (lowBarrier)
            {
                if (!builtWithModel)
                    BuildLowBarrier(obstacle.transform, palette);
                RegisterObstacle(obstacle, ObstacleColliderSpec.LowSlideGate, dy, ObstacleKind.SlideGate);
            }
            else
            {
                if (!builtWithModel)
                    BuildHighObstacle(obstacle.transform, palette);
                RegisterObstacle(obstacle, ObstacleColliderSpec.SmallJumpable, dy, ObstacleKind.StandardHigh);
            }
        }

        internal bool SpawnMechanicObstacle(string apparatusPath, RunnerLane lane, float z, float runDistance)
        {
            var kind = apparatusPath switch
            {
                "ice/cracked_plate" => ObstacleKind.StandardHigh,
                "cedar/swing_log" => ObstacleKind.StandardHigh,
                "cedar/root_gallery" => ObstacleKind.SlideGate,
                "aurora/prism_beam" => ObstacleKind.SlideGate,
                "ocean/coral_lift" => ObstacleKind.StandardHigh,
                "sky/cloud_platform" => ObstacleKind.WideWall,
                _ => (ObstacleKind?)null,
            };
            return !kind.HasValue || SpawnExisting(kind.Value, lane, z, runDistance);
        }

        private bool SpawnExisting(ObstacleKind kind, RunnerLane lane, float z, float runDistance)
        {
            var countBefore = obstacles.Count;
            if (kind == ObstacleKind.WideWall)
                SpawnWideSnowWall(z, lane, waveDirector.CurrentPalette, runDistance);
            else
                SpawnStandardObstacle(z, lane, kind == ObstacleKind.SlideGate,
                    waveDirector.CurrentPalette, runDistance);
            if (obstacles.Count <= countBefore)
            {
                return false;
            }

            mechanicObstacleRoots.Add(obstacles[obstacles.Count - 1]);
            return true;
        }

        internal void ClearMechanicVisuals()
        {
            for (var index = mechanicObstacleRoots.Count - 1; index >= 0; index--)
            {
                var root = mechanicObstacleRoots[index];
                var obstacleIndex = obstacles.IndexOf(root);
                if (obstacleIndex >= 0)
                {
                    DestroyObstacle(root, obstacleIndex);
                }
            }
            mechanicObstacleRoots.Clear();
        }

        // ── 速度门 ──────────────────────────────────────────────

        /// <summary>在指定位置生成速度门（玩家穿过获得短暂冲刺）。</summary>
        public void SpawnSpeedGate(float z, RunnerLane lane, float runDistance, SegmentObstaclePalette palette)
        {
            var gate = new GameObject("Speed Gate");
            var laneX = lane.ToX(laneWidth);
            var surface = TrackTerrain.SurfaceY(z, runDistance, baseGroundY);
            gate.transform.position = new Vector3(laneX, surface, z);

            var glowColor = palette switch
            {
                SegmentObstaclePalette.AuroraGlow => new Color(0.4f, 0.6f, 1f, 0.7f),
                SegmentObstaclePalette.OceanCoral => new Color(0.2f, 0.9f, 0.95f, 0.7f),
                SegmentObstaclePalette.SkyCloud => new Color(1f, 0.9f, 0.3f, 0.7f),
                _ => new Color(0.3f, 0.9f, 1f, 0.7f),
            };

            // 左柱
            var pillarL = RunnerVisuals.CreatePrimitive("GatePillarL", PrimitiveType.Cylinder,
                Vector3.zero, new Vector3(0.18f, 2.2f, 0.18f), glowColor, true);
            pillarL.transform.SetParent(gate.transform, false);
            pillarL.transform.localPosition = new Vector3(-0.85f, 1.1f, 0f);

            // 右柱
            var pillarR = RunnerVisuals.CreatePrimitive("GatePillarR", PrimitiveType.Cylinder,
                Vector3.zero, new Vector3(0.18f, 2.2f, 0.18f), glowColor, true);
            pillarR.transform.SetParent(gate.transform, false);
            pillarR.transform.localPosition = new Vector3(0.85f, 1.1f, 0f);

            // 顶部横梁
            var beam = RunnerVisuals.CreatePrimitive("GateBeam", PrimitiveType.Cube,
                Vector3.zero, new Vector3(1.8f, 0.12f, 0.25f), glowColor, true);
            beam.transform.SetParent(gate.transform, false);
            beam.transform.localPosition = new Vector3(0f, 2.25f, 0f);

            // 光环
            var ring = RunnerVisuals.CreatePrimitive("GateRing", PrimitiveType.Cylinder,
                Vector3.zero, new Vector3(1.2f, 0.04f, 1.2f), new Color(glowColor.r, glowColor.g, glowColor.b, 0.35f), true);
            ring.transform.SetParent(gate.transform, false);
            ring.transform.localPosition = new Vector3(0f, 0.05f, 0f);

            speedGates.Add(gate);
        }

        /// <summary>检查玩家是否穿过速度门，返回 true 表示触发了冲刺。</summary>
        public bool CheckSpeedGatePass(Transform player, float playerY, out string feedback)
        {
            feedback = string.Empty;
            var px = player.position.x;
            var pz = player.position.z;
            for (var i = speedGates.Count - 1; i >= 0; i--)
            {
                var gate = speedGates[i];
                if (gate == null) { speedGates.RemoveAt(i); continue; }
                var gx = gate.transform.position.x;
                var gz = gate.transform.position.z;
                if (Mathf.Abs(gz - pz) > 0.85f) continue;
                if (Mathf.Abs(gx - px) > 1.2f) continue;
                if (Mathf.Abs(gate.transform.position.y - playerY) > 1.6f) continue;

                Object.Destroy(gate);
                speedGates.RemoveAt(i);
                feedback = "速度门 · 冲刺！";
                return true;
            }
            return false;
        }

        private static void BuildHighObstacle(Transform root, SegmentObstaclePalette palette)
        {
            // 高障碍中心在 dy=0.55f，模型从中心向上/向下构建
            switch (palette)
            {
                case SegmentObstaclePalette.CedarWood:
                case SegmentObstaclePalette.MistFog:
                    AddChildPrimitive(root, "Snow Wall", PrimitiveType.Cube, new Vector3(0f, 0.38f, 0f), new Vector3(0.95f, 0.95f, 1f), new Color(0.32f, 0.38f, 0.48f));
                    AddChildPrimitive(root, "Splinter Beam", PrimitiveType.Cube, new Vector3(0f, -0.12f, -0.48f), new Vector3(0.72f, 0.1f, 0.1f), new Color(0.38f, 0.28f, 0.2f));
                    AddChildPrimitive(root, "Hazard Cap", PrimitiveType.Cube, new Vector3(0f, 0.65f, 0f), new Vector3(0.78f, 0.12f, 0.9f), new Color(0.92f, 0.32f, 0.18f));
                    break;
                case SegmentObstaclePalette.AuroraGlow:
                    AddChildPrimitive(root, "Crystal Core", PrimitiveType.Cylinder, new Vector3(0f, 0.05f, 0f), new Vector3(1f, 0.9f, 1f), new Color(0.38f, 0.16f, 0.62f));
                    AddChildPrimitive(root, "Cyan Halo", PrimitiveType.Cylinder, new Vector3(0f, 0.05f, 0f), new Vector3(1.18f, 0.06f, 1.18f), new Color(0.28f, 0.95f, 0.88f, 0.45f), true);
                    AddChildPrimitive(root, "Cap Glow", PrimitiveType.Sphere, new Vector3(0f, 0.52f, 0f), new Vector3(0.74f, 0.14f, 0.74f), new Color(0.75f, 0.82f, 1f));
                    AddChildPrimitive(root, "Hazard Ring", PrimitiveType.Cube, new Vector3(0f, -0.28f, -0.52f), new Vector3(0.95f, 0.1f, 0.14f), new Color(0.98f, 0.35f, 0.15f));
                    break;
                case SegmentObstaclePalette.OceanCoral:
                    // 海洋主题高障碍：珊瑚柱
                    AddChildPrimitive(root, "Coral Trunk", PrimitiveType.Cylinder, new Vector3(0f, 0.02f, 0f), new Vector3(0.85f, 0.95f, 0.85f), new Color(1f, 0.45f, 0.52f));
                    AddChildPrimitive(root, "Coral Branch L", PrimitiveType.Cylinder, new Vector3(-0.35f, 0.45f, 0.1f), new Vector3(0.4f, 0.5f, 0.4f), new Color(0.95f, 0.35f, 0.45f));
                    AddChildPrimitive(root, "Coral Branch R", PrimitiveType.Cylinder, new Vector3(0.35f, 0.38f, -0.05f), new Vector3(0.42f, 0.45f, 0.42f), new Color(0.9f, 0.4f, 0.5f));
                    AddChildPrimitive(root, "Sea Glow", PrimitiveType.Sphere, new Vector3(0f, 0.55f, 0.15f), new Vector3(0.45f, 0.25f, 0.45f), new Color(0.15f, 0.9f, 0.95f, 0.5f), true);
                    AddChildPrimitive(root, "Coral Base", PrimitiveType.Cylinder, new Vector3(0f, -0.25f, 0f), new Vector3(1.05f, 0.2f, 1.05f), new Color(0.85f, 0.3f, 0.4f));
                    break;
                case SegmentObstaclePalette.SkyCloud:
                    // 天空主题高障碍：风暴柱
                    AddChildPrimitive(root, "Storm Core", PrimitiveType.Cylinder, new Vector3(0f, 0.05f, 0f), new Vector3(0.8f, 0.88f, 0.8f), new Color(0.45f, 0.55f, 0.75f));
                    AddChildPrimitive(root, "Storm Swirl", PrimitiveType.Cylinder, new Vector3(0f, 0.35f, 0f), new Vector3(1.15f, 0.2f, 1.15f), new Color(0.6f, 0.7f, 0.9f, 0.4f), true);
                    AddChildPrimitive(root, "Lightning Glow", PrimitiveType.Cube, new Vector3(0f, 0.55f, 0.25f), new Vector3(0.25f, 0.15f, 0.08f), new Color(1f, 0.95f, 0.3f, 0.8f), true);
                    AddChildPrimitive(root, "Cloud Base", PrimitiveType.Sphere, new Vector3(0f, -0.28f, 0f), new Vector3(1.1f, 0.35f, 1.1f), new Color(0.92f, 0.92f, 0.98f, 0.7f), true);
                    break;
                default:
                    // 默认主题：更精致的冰柱障碍 - 加冰晶尖刺、内部发光、底座
                    AddChildPrimitive(root, "Pillar Core", PrimitiveType.Cylinder, new Vector3(0f, 0.05f, 0f), new Vector3(1f, 0.9f, 1f), new Color(0.14f, 0.26f, 0.42f));
                    AddChildPrimitive(root, "Pillar Inner Glow", PrimitiveType.Cylinder, new Vector3(0f, 0.05f, 0f), new Vector3(0.45f, 0.85f, 0.45f), new Color(0.4f, 0.85f, 1f, 0.45f), true);
                    AddChildPrimitive(root, "Snow Cap", PrimitiveType.Sphere, new Vector3(0f, 0.52f, 0f), new Vector3(0.76f, 0.16f, 0.76f), new Color(0.9f, 0.96f, 1f));
                    // 顶部冰晶尖刺
                    var spike1 = AddChildPrimitive(root, "Ice Spike 1", PrimitiveType.Cube, new Vector3(-0.18f, 0.7f, 0.1f), new Vector3(0.14f, 0.32f, 0.14f), new Color(0.65f, 0.92f, 1f, 0.85f), true);
                    spike1.transform.localRotation = Quaternion.Euler(8f, 0f, 12f);
                    var spike2 = AddChildPrimitive(root, "Ice Spike 2", PrimitiveType.Cube, new Vector3(0.18f, 0.72f, -0.05f), new Vector3(0.16f, 0.36f, 0.16f), new Color(0.65f, 0.92f, 1f, 0.85f), true);
                    spike2.transform.localRotation = Quaternion.Euler(-6f, 0f, -10f);
                    AddChildPrimitive(root, "Warning Stripe", PrimitiveType.Cube, new Vector3(0f, -0.15f, -0.54f), new Vector3(0.88f, 0.12f, 0.1f), new Color(1f, 0.38f, 0.12f));
                    AddChildPrimitive(root, "Warning Stripe Hi", PrimitiveType.Cube, new Vector3(0f, 0.15f, -0.54f), new Vector3(0.88f, 0.06f, 0.1f), new Color(1f, 0.62f, 0.22f));
                    AddChildPrimitive(root, "Side Hazard L", PrimitiveType.Cube, new Vector3(-0.52f, 0.28f, 0.1f), new Vector3(0.12f, 0.55f, 0.75f), new Color(0.92f, 0.28f, 0.14f));
                    AddChildPrimitive(root, "Side Hazard R", PrimitiveType.Cube, new Vector3(0.52f, 0.28f, 0.1f), new Vector3(0.12f, 0.55f, 0.75f), new Color(0.92f, 0.28f, 0.14f));
                    // 底座霜花
                    AddChildPrimitive(root, "Frost Base", PrimitiveType.Cylinder, new Vector3(0f, -0.42f, 0f), new Vector3(1.25f, 0.08f, 1.25f), new Color(0.78f, 0.92f, 1f, 0.55f), true);
                    break;
            }
        }

        private void SpawnWideSnowWall(float z, RunnerLane obstacleLane, SegmentObstaclePalette palette, float runDistance)
        {
            var obstacle = new GameObject("Wide Snow Wall");
            var dy = 0.48f; // 宽墙中心高度
            var surface = TrackTerrain.SurfaceY(z, runDistance, baseGroundY);
            obstacle.transform.position = new Vector3(obstacleLane.ToX(laneWidth), surface + dy, z);

            var builtWithModel = visualFactory.TryAttachObstacle(obstacle.transform, routeTheme, ObstacleKind.WideWall);
            if (!builtWithModel)
                builtWithModel = TryBuildOpenModelWideWall(obstacle.transform, palette);
            if (!builtWithModel)
            {
                var wallTint = palette switch
                {
                    SegmentObstaclePalette.OceanCoral => new Color(0.88f, 0.52f, 0.42f),
                    SegmentObstaclePalette.SkyCloud => new Color(0.72f, 0.78f, 0.9f),
                    SegmentObstaclePalette.AuroraGlow => new Color(0.26f, 0.22f, 0.55f),
                    SegmentObstaclePalette.CedarWood => new Color(0.34f, 0.38f, 0.44f),
                    _ => new Color(0.16f, 0.28f, 0.42f),
                };
                builtWithModel = AttachProceduralMesh(obstacle.transform, ObstacleMeshBuilder.BuildWideWall(), wallTint, "ProcWall");
            }

            if (!builtWithModel)
            {
                switch (palette)
                {
                    case SegmentObstaclePalette.OceanCoral:
                        // 海洋宽障碍：巨型珊瑚礁
                        AddChildPrimitive(obstacle.transform, "Reef Base", PrimitiveType.Cube, new Vector3(0f, 0.02f, 0f), new Vector3(2.45f, 0.85f, 1.1f), new Color(0.88f, 0.52f, 0.42f));
                        AddChildPrimitive(obstacle.transform, "Reef Top", PrimitiveType.Cube, new Vector3(0f, 0.48f, -0.05f), new Vector3(2.2f, 0.18f, 0.95f), new Color(0.95f, 0.65f, 0.55f));
                        AddChildPrimitive(obstacle.transform, "Reef Glow", PrimitiveType.Cube, new Vector3(0f, 0.22f, 0.52f), new Vector3(2.35f, 0.08f, 0.12f), new Color(0.15f, 0.85f, 0.95f, 0.55f), true);
                        AddChildPrimitive(obstacle.transform, "Coral Tip L", PrimitiveType.Sphere, new Vector3(-0.85f, 0.62f, 0.1f), new Vector3(0.35f, 0.25f, 0.3f), new Color(0.75f, 0.4f, 0.5f));
                        AddChildPrimitive(obstacle.transform, "Coral Tip R", PrimitiveType.Sphere, new Vector3(0.85f, 0.58f, -0.05f), new Vector3(0.38f, 0.28f, 0.32f), new Color(0.7f, 0.45f, 0.55f));
                        break;
                    case SegmentObstaclePalette.SkyCloud:
                        // 天空宽障碍：雷云墙
                        AddChildPrimitive(obstacle.transform, "Cloud Wall", PrimitiveType.Cube, new Vector3(0f, 0.04f, 0f), new Vector3(2.4f, 0.8f, 1.05f), new Color(0.72f, 0.78f, 0.9f, 0.75f), true);
                        AddChildPrimitive(obstacle.transform, "Storm Rim", PrimitiveType.Cube, new Vector3(0f, 0.48f, -0.04f), new Vector3(2.25f, 0.12f, 0.9f), new Color(0.85f, 0.88f, 0.95f, 0.6f), true);
                        AddChildPrimitive(obstacle.transform, "Lightning Flash", PrimitiveType.Cube, new Vector3(0f, 0.22f, 0.55f), new Vector3(2.3f, 0.06f, 0.1f), new Color(1f, 0.9f, 0.25f, 0.7f), true);
                        break;
                    default:
                        var coreColor =
                            palette switch
                            {
                                SegmentObstaclePalette.AuroraGlow => new Color(0.26f, 0.22f, 0.55f),
                                SegmentObstaclePalette.CedarWood => new Color(0.34f, 0.38f, 0.44f),
                                SegmentObstaclePalette.MistFog => new Color(0.36f, 0.42f, 0.48f),
                                _ => new Color(0.16f, 0.28f, 0.42f),
                            };
                        AddChildPrimitive(obstacle.transform, "Wall Body", PrimitiveType.Cube, new Vector3(0f, 0.03f, 0f), new Vector3(2.35f, 0.9f, 1.05f), coreColor);
                        AddChildPrimitive(obstacle.transform, "Crest Snow", PrimitiveType.Cube, new Vector3(0f, 0.52f, -0.06f), new Vector3(2.2f, 0.2f, 0.95f), new Color(0.72f, 0.78f, 0.86f));
                        AddChildPrimitive(obstacle.transform, "Hazard Front", PrimitiveType.Cube, new Vector3(0f, 0.12f, 0.58f), new Vector3(2.28f, 0.1f, 0.14f), new Color(0.96f, 0.3f, 0.12f));
                        break;
                }
            }
            RegisterObstacle(obstacle, ObstacleColliderSpec.WideWall, dy, ObstacleKind.WideWall);

            // 冲刺可碎墙：中后程宽墙有概率升级为 ShatterWall，冲刺态撞上直接粉碎。
            // 裂纹光带作为“可冲刺击碎”的视觉语言（与冲刺道具同色系）。
            if (runDistance > 350f && Random.value < 0.30f)
            {
                var crackA = AddChildPrimitive(obstacle.transform, "Shatter Crack A", PrimitiveType.Cube,
                    new Vector3(-0.2f, 0.1f, 0.56f), new Vector3(1.9f, 0.08f, 0.05f),
                    new Color(0.45f, 0.95f, 1f, 0.9f), true);
                crackA.transform.localRotation = Quaternion.Euler(0f, 0f, 16f);
                var crackB = AddChildPrimitive(obstacle.transform, "Shatter Crack B", PrimitiveType.Cube,
                    new Vector3(0.3f, 0.05f, 0.56f), new Vector3(1.3f, 0.06f, 0.05f),
                    new Color(0.45f, 0.95f, 1f, 0.75f), true);
                crackB.transform.localRotation = Quaternion.Euler(0f, 0f, -24f);

                var shatter = obstacle.AddComponent<ShatterWall>();
                shatter.Setup(obstacle.transform);
                obstacleBehaviors.Add(shatter);
                if (!dashHintConsumed)
                    pendingDashHint = true;
            }
        }

        /// <summary>定时开合能量门：周期开/关，看准开门窗口通过，或换道绕行。</summary>
        private void SpawnTimedGate(float z, RunnerLane lane, SegmentObstaclePalette palette, float runDistance)
        {
            var obstacle = new GameObject("Timed Gate");
            var dy = 0f;
            var surface = TrackTerrain.SurfaceY(z, runDistance, baseGroundY);
            obstacle.transform.position = new Vector3(lane.ToX(laneWidth), surface + dy, z);

            var frameColor = GetObstaclePaletteTint(palette);
            var energyColor = palette switch
            {
                SegmentObstaclePalette.AuroraGlow => new Color(0.62f, 0.4f, 1f, 0.62f),
                SegmentObstaclePalette.OceanCoral => new Color(0.2f, 0.85f, 0.9f, 0.62f),
                SegmentObstaclePalette.CedarWood => new Color(1f, 0.62f, 0.25f, 0.62f),
                SegmentObstaclePalette.MistFog => new Color(0.72f, 0.82f, 0.95f, 0.62f),
                SegmentObstaclePalette.SkyCloud => new Color(1f, 0.9f, 0.35f, 0.62f),
                _ => new Color(0.35f, 0.85f, 1f, 0.62f),
            };

            AddChildPrimitive(obstacle.transform, "Gate Pillar L", PrimitiveType.Cylinder,
                new Vector3(-0.92f, 1.05f, 0f), new Vector3(0.18f, 1.05f, 0.18f), frameColor);
            AddChildPrimitive(obstacle.transform, "Gate Pillar R", PrimitiveType.Cylinder,
                new Vector3(0.92f, 1.05f, 0f), new Vector3(0.18f, 1.05f, 0.18f), frameColor);
            AddChildPrimitive(obstacle.transform, "Gate Crown", PrimitiveType.Cube,
                new Vector3(0f, 2.16f, 0f), new Vector3(2.05f, 0.16f, 0.24f), frameColor);

            var panel = AddChildPrimitive(obstacle.transform, "Gate Energy Panel", PrimitiveType.Cube,
                new Vector3(0f, 1.0f, 0f), new Vector3(1.62f, 2.0f, 0.16f), energyColor, true);

            var spec = new ObstacleColliderSpec(false, 0.62f, 0.30f, 999f);
            RegisterObstacle(obstacle, spec, dy, ObstacleKind.StandardHigh);

            var gate = obstacle.AddComponent<TimedGate>();
            gate.Setup(panel.transform, spec, lane.ToX(laneWidth));
            obstacleBehaviors.Add(gate);
        }

        /// <summary>横向巡移浮障：在车道间往返滑动，看准空档换道或直接跳过。</summary>
        private void SpawnMovingBarrier(float z, RunnerLane lane, SegmentObstaclePalette palette, float runDistance)
        {
            var obstacle = new GameObject("Moving Barrier");
            var dy = 0.5f;
            var surface = TrackTerrain.SurfaceY(z, runDistance, baseGroundY);
            var laneX = lane.ToX(laneWidth);
            obstacle.transform.position = new Vector3(laneX, surface + dy, z);

            var bodyColor = GetObstaclePaletteTint(palette);
            AddChildPrimitive(obstacle.transform, "Barrier Body", PrimitiveType.Cube,
                new Vector3(0f, 0.05f, 0f), new Vector3(1.05f, 0.85f, 0.55f), bodyColor);
            AddChildPrimitive(obstacle.transform, "Barrier Glow Rim", PrimitiveType.Cube,
                new Vector3(0f, 0.05f, 0.3f), new Vector3(1.12f, 0.2f, 0.06f),
                new Color(1f, 0.45f, 0.2f, 0.85f), true);
            AddChildPrimitive(obstacle.transform, "Hover Disc", PrimitiveType.Cylinder,
                new Vector3(0f, -0.52f, 0f), new Vector3(1.15f, 0.04f, 0.7f),
                new Color(1f, 0.6f, 0.25f, 0.4f), true);

            RegisterObstacle(obstacle, new ObstacleColliderSpec(false, 0.55f, 0.34f, 0.95f), dy, ObstacleKind.Enemy);

            var barrier = obstacle.AddComponent<MovingBarrier>();
            barrier.Setup(obstacle.transform, laneWidth * 0.6f, 0.33f, laneX);
            obstacleBehaviors.Add(barrier);
        }

        private void SpawnWideSlideGate(float z, RunnerLane obstacleLane, SegmentObstaclePalette palette, float runDistance)
        {
            var obstacle = new GameObject("Wide Slide Gate");
            var dy = 0.22f;
            var surface = TrackTerrain.SurfaceY(z, runDistance, baseGroundY);
            obstacle.transform.position = new Vector3(obstacleLane.ToX(laneWidth), surface + dy, z);

            var builtWithModel = TryBuildOpenModelObstacle(obstacle.transform, true, palette);
            if (!builtWithModel)
                builtWithModel = AttachProceduralMesh(obstacle.transform, ObstacleMeshBuilder.BuildWideSlideGate(), GetObstaclePaletteTint(palette), "ProcWideSlideGate");
            if (!builtWithModel)
            {
                BuildLowBarrier(obstacle.transform, palette);

                var beam = obstacle.transform.Find("Gate Beam");
                if (beam != null)
                {
                    var scale = beam.localScale;
                    beam.localScale = new Vector3(scale.x * 2.2f, scale.y, scale.z);
                }
                else
                {
                    AddChildPrimitive(obstacle.transform, "Wide Gate Beam", PrimitiveType.Cube,
                        new Vector3(0f, 0.03f, 0f), new Vector3(3.18f, 0.28f, 1.25f), new Color(0.88f, 0.38f, 0.12f));
                    AddChildPrimitive(obstacle.transform, "Wide Gate Stripe", PrimitiveType.Cube,
                        new Vector3(0f, 0.04f, 0f), new Vector3(3.08f, 0.08f, 0.18f), new Color(0.18f, 0.1f, 0.06f));
                }
            }

            AddSlideGateGroundStrip(obstacle.transform);
            RegisterObstacle(obstacle, ObstacleColliderSpec.WideSlideOnly, dy, ObstacleKind.WideSlideGate);
        }

        private void SpawnFlyingObstacle(float z, RunnerLane lane, SegmentObstaclePalette palette, float runDistance)
        {
            var obstacle = new GameObject("Flying Hazard");
            var dy = 1.1f;
            var surface = TrackTerrain.SurfaceY(z, runDistance, baseGroundY);
            obstacle.transform.position = new Vector3(lane.ToX(laneWidth), surface + dy, z);

            var tint = palette switch
            {
                SegmentObstaclePalette.SkyCloud => new Color(0.72f, 0.82f, 1f, 0.88f),
                SegmentObstaclePalette.OceanCoral => new Color(0.95f, 0.55f, 0.62f, 0.9f),
                SegmentObstaclePalette.AuroraGlow => new Color(0.72f, 0.55f, 0.95f, 0.9f),
                _ => new Color(0.55f, 0.78f, 0.95f, 0.9f),
            };
            AttachProceduralMesh(obstacle.transform, ObstacleMeshBuilder.BuildFlyingOrb(), tint, "ProcFlyingOrb");

            RegisterObstacle(obstacle, ObstacleColliderSpec.SmallFlying, dy, ObstacleKind.Flying);
        }

        private bool TryBuildOpenModelWideWall(Transform root, SegmentObstaclePalette palette)
        {
            var modelName = palette switch
            {
                SegmentObstaclePalette.CedarWood => "log_stackLarge",
                SegmentObstaclePalette.MistFog => "cliff_block_stone",
                SegmentObstaclePalette.AuroraGlow => "cliff_block_rock",
                SegmentObstaclePalette.OceanCoral => "cliff_block_stone",
                SegmentObstaclePalette.SkyCloud => "cliff_block_rock",
                _ => "cliff_block_stone",
            };
            var prefab = LoadModelPrefab(modelName);
            if (prefab == null) return false;

            var inst = Object.Instantiate(prefab, root, false);
            inst.name = $"WideWall_{modelName}";
            inst.transform.localPosition = new Vector3(0f, -0.08f, 0f);
            inst.transform.localRotation = Quaternion.Euler(0f, 90f, 0f);
            var scale = modelName == "log_stackLarge"
                ? new Vector3(1.22f, 1.05f, 1.58f)
                : new Vector3(2.42f, 1.15f, 1.48f);
            inst.transform.localScale = scale * OpenWideWallVisualScaleBoost;

            var tint = palette switch
            {
                SegmentObstaclePalette.OceanCoral => new Color(0.62f, 0.86f, 0.88f),
                SegmentObstaclePalette.SkyCloud => new Color(0.82f, 0.9f, 1f),
                SegmentObstaclePalette.AuroraGlow => new Color(0.7f, 0.62f, 0.92f),
                SegmentObstaclePalette.CedarWood => new Color(0.66f, 0.54f, 0.4f),
                _ => new Color(0.72f, 0.82f, 0.9f),
            };
            TintModel(inst, tint);
            AddChildPrimitive(root, "WideWall Hazard Front", PrimitiveType.Cube,
                new Vector3(0f, 0.12f, 0.58f), new Vector3(2.28f, 0.1f, 0.14f), new Color(0.96f, 0.3f, 0.12f));
            return true;
        }

        private bool TryBuildOpenModelEnemy(Transform root, bool patrolEnemy)
        {
            // 巡逻：高大植物/岩石模型；扑入：棕榈/树模型体现动态感
            var modelName = patrolEnemy ? "cactus_tall" : "tree_palmShort";
            var prefab = LoadModelPrefab(modelName);
            if (prefab == null) return false;
            var inst = Object.Instantiate(prefab, root, false);
            inst.name = patrolEnemy ? "PatrolEnemyModel" : "SwoopEnemyModel";
            inst.transform.localPosition = patrolEnemy ? new Vector3(0f, -0.22f, 0f) : new Vector3(0f, -0.18f, 0f);
            inst.transform.localRotation = Quaternion.Euler(0f, patrolEnemy ? 0f : 90f, patrolEnemy ? 0f : 12f);
            var scale = patrolEnemy ? new Vector3(0.95f, 0.95f, 0.95f) : new Vector3(0.84f, 0.84f, 0.84f);
            inst.transform.localScale = scale * OpenEnemyVisualScaleBoost;
            TintModel(inst, patrolEnemy ? new Color(0.74f, 0.2f, 0.22f) : new Color(0.58f, 0.36f, 0.82f));

            // 给敌人添加警示标志：红色危险环
            AddChildPrimitive(root, "EnemyWarnRing", PrimitiveType.Cylinder, new Vector3(0f, -0.3f, 0f),
                new Vector3(1.18f, 0.06f, 1.18f), new Color(1f, 0.25f, 0.12f, 0.6f), true);
            return true;
        }

        /// <summary>生成主题化刺针/尖柱障碍（TallSpike 规格，需换道）。</summary>
        private void SpawnSpikeObstacle(float z, RunnerLane lane, SegmentObstaclePalette palette, float runDistance)
        {
            var obstacle = new GameObject("SpikeObstacle");
            var dy = 0.62f;
            var surface = TrackTerrain.SurfaceY(z, runDistance, baseGroundY);
            obstacle.transform.position = new Vector3(lane.ToX(laneWidth), surface + dy, z);

            var tint = palette switch
            {
                SegmentObstaclePalette.AuroraGlow => new Color(0.52f, 0.32f, 0.85f),
                SegmentObstaclePalette.MistFog => new Color(0.55f, 0.62f, 0.68f),
                SegmentObstaclePalette.OceanCoral => new Color(0.88f, 0.35f, 0.48f),
                SegmentObstaclePalette.SkyCloud => new Color(0.82f, 0.92f, 1f),
                SegmentObstaclePalette.CedarWood => new Color(0.42f, 0.28f, 0.16f),
                _ => new Color(0.62f, 0.78f, 0.9f),
            };

            // 尝试用开源模型；失败时用程序化几何
            var modelName = palette switch
            {
                SegmentObstaclePalette.CedarWood => "tree_pineSmallA",
                SegmentObstaclePalette.MistFog => "cliff_block_stone",
                _ => "rock_largeA",
            };
            var prefab = LoadModelPrefab(modelName);
            if (prefab != null)
            {
                var inst = Object.Instantiate(prefab, obstacle.transform, false);
                inst.transform.localPosition = new Vector3(0f, -0.25f, 0f);
                inst.transform.localScale = new Vector3(0.68f, 1.15f, 0.68f);
                TintModel(inst, tint);
            }
            else
            {
                AttachProceduralMesh(obstacle.transform, ObstacleMeshBuilder.BuildSpike(), tint, "ProcSpike");
            }

            // 危险标志
            AddChildPrimitive(obstacle.transform, "DangerRing", PrimitiveType.Cylinder,
                new Vector3(0f, -0.28f, 0f), new Vector3(0.98f, 0.06f, 0.98f),
                new Color(1f, 0.28f, 0.1f, 0.55f), true);

            RegisterObstacle(obstacle, ObstacleColliderSpec.TallSpike, dy, ObstacleKind.TallSpike);
        }

        /// <summary>生成中等高度障碍（MediumBarrier 规格，可跳或可滑）。</summary>
        private void SpawnMediumBarrier(float z, RunnerLane lane, SegmentObstaclePalette palette, float runDistance)
        {
            var obstacle = new GameObject("MediumBarrier");
            var dy = 0.45f;
            var surface = TrackTerrain.SurfaceY(z, runDistance, baseGroundY);
            obstacle.transform.position = new Vector3(lane.ToX(laneWidth), surface + dy, z);

            var builtWithModel = TryBuildOpenModelObstacle(obstacle.transform, true, palette);
            if (!builtWithModel)
                builtWithModel = AttachProceduralMesh(obstacle.transform, ObstacleMeshBuilder.BuildMediumBarrier(), GetObstaclePaletteTint(palette), "ProcMediumBarrier");
            if (!builtWithModel)
            {
                AddChildPrimitive(obstacle.transform, "Barrier Slab", PrimitiveType.Cube,
                    Vector3.zero, new Vector3(0.85f, 0.9f, 0.36f), new Color(0.65f, 0.75f, 0.85f));
                AddChildPrimitive(obstacle.transform, "Barrier Stripe", PrimitiveType.Cube,
                    new Vector3(0f, 0.35f, -0.2f), new Vector3(0.88f, 0.12f, 0.04f), new Color(1f, 0.82f, 0.1f));
            }

            RegisterObstacle(obstacle, ObstacleColliderSpec.MediumBarrier, dy, ObstacleKind.MediumBarrier);
        }

        private void SpawnRollingSnowball(float z, RunnerLane obstacleLane, SegmentObstaclePalette palette, float runDistance)
        {
            var obstacle = new GameObject("Rolling Snowball");
            var tint = palette switch
            {
                SegmentObstaclePalette.AuroraGlow => new Color(0.48f, 0.42f, 0.62f),
                SegmentObstaclePalette.MistFog => new Color(0.45f, 0.5f, 0.56f),
                _ => new Color(0.42f, 0.48f, 0.56f),
            };
            if (!TryAttachObstacleModel(obstacle.transform, "rock_smallA", Vector3.zero, Vector3.one * 1.05f, tint))
                AttachProceduralMesh(obstacle.transform, ObstacleMeshBuilder.BuildSnowball(), tint, "ProcSnowball");
            var dy = 0.53f;
            var surface = TrackTerrain.SurfaceY(z, runDistance, baseGroundY);
            obstacle.transform.position = new Vector3(obstacleLane.ToX(laneWidth), surface + dy, z);
            AddChildPrimitive(obstacle.transform, "Hazard Belt", PrimitiveType.Cube, new Vector3(0f, 0f, 0f), new Vector3(1.18f, 0.12f, 1.18f), new Color(0.95f, 0.34f, 0.12f));
            AddChildPrimitive(obstacle.transform, "Reflect Spot", PrimitiveType.Sphere, new Vector3(0.22f, 0.35f, 0.2f), new Vector3(0.28f, 0.28f, 0.28f), new Color(0.92f, 0.96f, 1f));
            // 雪屑碎片缠绕
            for (var i = 0; i < 4; i++)
            {
                var ang = i * 90f * Mathf.Deg2Rad;
                AddChildPrimitive(obstacle.transform, $"Snow Chunk {i}", PrimitiveType.Cube,
                    new Vector3(Mathf.Cos(ang) * 0.5f, Mathf.Sin(ang) * 0.4f - 0.15f, Mathf.Sin(ang * 1.3f) * 0.3f),
                    Vector3.one * 0.18f,
                    new Color(0.92f, 0.96f, 1f, 0.85f), true);
            }
            // 后方雪雾尾迹
            AddChildPrimitive(obstacle.transform, "Snow Mist", PrimitiveType.Sphere,
                new Vector3(0f, -0.1f, 0.55f), new Vector3(0.9f, 0.4f, 0.7f),
                new Color(0.95f, 0.98f, 1f, 0.45f), true);
            RegisterObstacle(obstacle, ObstacleColliderSpec.Rolling, dy, ObstacleKind.Rolling);
            rollingObstacles.Add(
                new RollingState
                {
                    Go = obstacle,
                    LaneCenterX = obstacleLane.ToX(laneWidth),
                    Amplitude = 0.82f,
                    Frequency = 1.65f,
                    Phase = Random.Range(0f, Mathf.PI * 2f),
                    SpinYawDegreesPerSec = 0f,
                });
        }

        private void SpawnCoinLine(float z, RunnerLane coinLane, int count, float runDistance)
        {
            for (var i = 0; i < count; i++)
            {
                SpawnCoin(z + i * 1.85f, coinLane, runDistance);
            }
        }

        private GameObject SpawnCoin(float z, RunnerLane coinLane, float runDistance, float bonusY = 0f,
            bool ownedBossReward = false)
        {
            if (!TryPickClearCoinLane(z, ref coinLane))
                return null;

            var coin = coinPool.Rent();
            if (coin == null) return null;
            // Rent may recycle a pooled object. Ownership belongs to this spawn, not its reference.
            bossRewardEntities.Remove(coin);
            routeFish.Remove(coin);
            coins.Remove(coin);
            if (ownedBossReward) RegisterBossReward(coin);
            var surface = TrackTerrain.SurfaceY(z, runDistance, baseGroundY);
            coin.transform.position = new Vector3(coinLane.ToX(laneWidth), surface + CoinHoverHeight + bonusY, z);
            coins.Add(coin);
            if (bonusY > 0f)
            {
                coinYDelta[coin] = bonusY;
            }
            else
            {
                coinYDelta.Remove(coin);
            }
            return coin;
        }

        /// <summary>优先原车道；被障碍占用则改旁道；三道都被挡则跳过该枚金币。</summary>
        private bool TryPickClearCoinLane(float z, ref RunnerLane lane)
        {
            if (!IsLaneBlockedByObstacle(z, lane)) return true;

            var order = new[]
            {
                lane,
                lane == RunnerLane.Center ? RunnerLane.Left : RunnerLane.Center,
                lane == RunnerLane.Right ? RunnerLane.Left : RunnerLane.Right,
            };
            for (var i = 0; i < order.Length; i++)
            {
                if (IsLaneBlockedByObstacle(z, order[i])) continue;
                lane = order[i];
                return true;
            }

            return false;
        }

        private bool IsLaneBlockedByObstacle(float z, RunnerLane lane)
        {
            var x = lane.ToX(laneWidth);
            const float coinHalf = 0.4f;
            const float zPad = 0.55f;
            for (var i = 0; i < obstacles.Count; i++)
            {
                var o = obstacles[i];
                if (o == null || !o.activeSelf) continue;
                if (!obstacleSpecs.TryGetValue(o, out var spec)) continue;
                var op = o.transform.position;
                if (Mathf.Abs(op.z - z) > spec.HalfDz + zPad) continue;
                // 宽墙/宽滑铲门横跨多道，整段 Z 都视为占用。
                if (spec.HalfDx >= 0.95f) return true;
                if (Mathf.Abs(op.x - x) <= spec.HalfDx + coinHalf) return true;
            }

            return false;
        }

        private bool IsCoinOverlappingObstacle(Vector3 coinPos)
        {
            const float coinHalf = 0.4f;
            const float zPad = 0.55f;
            for (var i = 0; i < obstacles.Count; i++)
            {
                var o = obstacles[i];
                if (o == null || !o.activeSelf) continue;
                if (!obstacleSpecs.TryGetValue(o, out var spec)) continue;
                var op = o.transform.position;
                if (Mathf.Abs(op.z - coinPos.z) > spec.HalfDz + zPad) continue;
                if (spec.HalfDx >= 0.95f) return true;
                if (Mathf.Abs(op.x - coinPos.x) <= spec.HalfDx + coinHalf) return true;
            }

            return false;
        }

        private static GameObject BuildFishSnackProp(Transform parent, Vector3 localPos, float scale, float _)
        {
            var prop = CoinPickupVisual.Build(null);
            prop.name = "FishSnackProp";
            prop.transform.SetParent(parent, false);
            prop.transform.localPosition = localPos;
            prop.transform.localScale = Vector3.one * scale;
            return prop;
        }

        private void SpawnPowerUp(float z, float runDistance, SegmentObstaclePalette? paletteHint = null,
            PowerUpKind? forcedKind = null, RunnerLane? forcedLane = null)
        {
            var powerLane = forcedLane ?? (RunnerLane)Random.Range(-1, 2);
            var r = Random.value;
            PowerUpKind kind;

            if (forcedKind.HasValue)
            {
                kind = forcedKind.Value;
            }
            else if (config.IsOnboarding)
            {
                kind = PowerUpKind.Dash;
            }
            else
            {
            // 根据主题生成专属道具（含新增主题道具）
            var themeRoll = Random.value;
            if (paletteHint == SegmentObstaclePalette.OceanCoral && themeRoll < 0.40f)
            {
                kind = themeRoll < 0.18f ? PowerUpKind.CoralBounce
                    : themeRoll < 0.30f ? PowerUpKind.BubbleShield
                    : PowerUpKind.SeahorseBoost;
            }
            else if (paletteHint == SegmentObstaclePalette.SkyCloud && themeRoll < 0.40f)
            {
                kind = themeRoll < 0.18f ? PowerUpKind.ThunderFeather
                    : themeRoll < 0.30f ? PowerUpKind.CloudWalk
                    : PowerUpKind.WindRider;
            }
            else if (paletteHint == SegmentObstaclePalette.IceLake && themeRoll < 0.22f)
            {
                kind = PowerUpKind.IceMirror;
            }
            else if (paletteHint == SegmentObstaclePalette.AuroraGlow && themeRoll < 0.22f)
            {
                kind = PowerUpKind.AuroraChain;
            }
            else if (paletteHint == SegmentObstaclePalette.MistFog && themeRoll < 0.22f)
            {
                kind = PowerUpKind.FogLantern;
            }
            else if (paletteHint == SegmentObstaclePalette.CedarWood && themeRoll < 0.22f)
            {
                kind = PowerUpKind.TreantArmor;
            }
            else
            {
                var tStar = powerUpTuning.powerPickScoreStarMax;
                var tDouble = tStar + powerUpTuning.powerPickDoubleFishBand;
                var tHour = tDouble + powerUpTuning.powerPickHourglassBand;
                // 额外 6% 掉鱼弹/心
                var tBomb = tHour + 0.04f;
                var tHeart = tBomb + 0.02f;

                if (r < powerUpTuning.powerPickDashMax)
                    kind = PowerUpKind.Dash;
                else if (r < powerUpTuning.powerPickMagnetMax)
                    kind = PowerUpKind.Magnet;
                else if (r < powerUpTuning.powerPickShieldMax)
                    kind = PowerUpKind.Shield;
                else if (r < tStar)
                    kind = PowerUpKind.ScoreStar;
                else if (r < tDouble)
                    kind = PowerUpKind.DoubleFishSnack;
                else if (r < tHour)
                    kind = PowerUpKind.TimeHourglass;
                else if (r < tBomb)
                    kind = PowerUpKind.FishBomb;
                else if (r < tHeart)
                    kind = PowerUpKind.SecondHeart;
                else
                    kind = PowerUpKind.GlideFeather;
            }
            }

            var (color, secondaryColor) = GetPowerUpColors(kind);
            var power = CreatePowerUpRoot($"PowerUp {kind}", color, kind, secondaryColor);
            if (power == null) return;
            var laneX = powerLane.ToX(laneWidth);
            var surface = TrackTerrain.SurfaceY(z, runDistance, baseGroundY);
            power.transform.position = new Vector3(laneX, surface + PowerUpHoverHeight, z);
            powerUps[power] = kind;
        }

        /// <summary>道具根节点 + 极小核心光球，几何由 <see cref="AddPowerUpEffects"/> 子件展示。</summary>
        private GameObject CreatePowerUpRoot(string name, Color coreColor, PowerUpKind kind, Color secondaryColor)
        {
            var root = powerUpPool.Rent();
            if (root == null) return null;
            bossRewardEntities.Remove(root);
            powerUps.Remove(root);
            root.name = name;
            var visualProfile = PowerUpVisualProfile.For(kind, routeTheme);
            coreColor = visualProfile.CoreColor;
            secondaryColor = visualProfile.HaloColor;

            // 重设核心球颜色
            for (var i = root.transform.childCount - 1; i >= 0; i--)
            {
                var child = root.transform.GetChild(i);
                if (child.name == "Pickup Core")
                {
                    RunnerVisuals.ApplyColor(child.GetComponent<Renderer>(), coreColor);
                    break;
                }
            }

            var hasCustomVisual = RunnerVisualAssetCatalog.TryGetPickupId(kind, out var visualId)
                && visualFactory.TryAttachPickup(root.transform, visualId);
            if (hasCustomVisual)
            {
                // 自定义雕塑已挂载时隐藏核心光球，否则远看仍像「模糊光点」。
                SetPickupCoreVisible(root.transform, false);
                AddPickupRecognitionRing(root.transform, coreColor);
            }
            else
            {
                SetPickupCoreVisible(root.transform, true);
                AddPowerUpEffects(root.transform, kind, coreColor, secondaryColor);
            }
            var pulse = root.GetComponent<PowerUpPulse>();
            if (pulse == null) pulse = root.AddComponent<PowerUpPulse>();
            pulse.Configure(visualProfile.PulseScale);
            return root;
        }

        /// <summary>池工厂：创建一个带核心球和基础环的空白道具根节点。</summary>
        private static GameObject BuildPooledPowerUpRoot()
        {
            var root = new GameObject("PowerUp Pooled");
            var core = RunnerVisuals.CreatePrimitive("Pickup Core", PrimitiveType.Sphere, Vector3.zero, Vector3.one * 0.08f, Color.white);
            core.transform.SetParent(root.transform, false);
            return root;
        }

        /// <summary>回收道具到池：销毁子物件、重置位姿。</summary>
        private void ReturnPowerUp(GameObject power)
        {
            visualFactory.ClearAttachment(power.transform);
            // 销毁类型专属子物件（保留核心球）
            for (var i = power.transform.childCount - 1; i >= 0; i--)
            {
                var child = power.transform.GetChild(i);
                if (child.name == "Pickup Core")
                {
                    child.gameObject.SetActive(true);
                    continue;
                }
                Object.Destroy(child.gameObject);
            }
            power.transform.localScale = Vector3.one;
            power.transform.localRotation = Quaternion.identity;
            powerUpPool.Return(power);
        }

        /// <summary>道具配色唯一来源为 <see cref="PowerUpVisualProfile"/>（primary=核心色，secondary=光晕色）。</summary>
        private (Color primary, Color secondary) GetPowerUpColors(PowerUpKind kind)
        {
            var profile = PowerUpVisualProfile.For(kind, routeTheme);
            return (profile.CoreColor, profile.HaloColor);
        }

        private static void AddPowerUpEffects(Transform root, PowerUpKind kind, Color color, Color secondaryColor)
        {
            AddPickupRecognitionRing(root, color);

            switch (kind)
            {
                case PowerUpKind.Dash:
                    // 鱼干冲刺：三片 chevron 指向前方（-Z）
                    for (var i = 0; i < 3; i++)
                    {
                        var s = 0.34f - i * 0.09f;
                        var ch = AddChildPrimitive(root, $"DashChev{i}", PrimitiveType.Cube,
                            new Vector3(0f, 0f, -0.12f - i * 0.2f),
                            new Vector3(s, 0.07f, 0.06f), secondaryColor, true);
                        ch.transform.localRotation = Quaternion.Euler(0f, 0f, 45f);
                    }
                    AddChildPrimitive(root, "DashGlow", PrimitiveType.Sphere, Vector3.zero, Vector3.one * 0.55f,
                        new Color(color.r, color.g, color.b, 0.14f), true);
                    break;

                case PowerUpKind.Magnet:
                    AddChildPrimitive(root, "MagnetPoleL", PrimitiveType.Cylinder,
                        new Vector3(-0.22f, 0.02f, 0f), new Vector3(0.14f, 0.52f, 0.14f), secondaryColor);
                    AddChildPrimitive(root, "MagnetPoleR", PrimitiveType.Cylinder,
                        new Vector3(0.22f, 0.02f, 0f), new Vector3(0.14f, 0.52f, 0.14f), color);
                    AddChildPrimitive(root, "MagnetBridge", PrimitiveType.Cube,
                        new Vector3(0f, 0.32f, 0f), new Vector3(0.5f, 0.1f, 0.16f), new Color(0.35f, 0.4f, 0.5f));
                    AddChildPrimitive(root, "MagnetSouth", PrimitiveType.Sphere,
                        new Vector3(-0.22f, -0.28f, 0f), Vector3.one * 0.12f, new Color(1f, 0.35f, 0.35f));
                    AddChildPrimitive(root, "MagnetNorth", PrimitiveType.Sphere,
                        new Vector3(0.22f, -0.28f, 0f), Vector3.one * 0.12f, new Color(0.35f, 0.55f, 1f));
                    break;

                case PowerUpKind.Shield:
                    AddChildPrimitive(root, "ShieldCore", PrimitiveType.Cube, Vector3.zero, new Vector3(0.55f, 0.72f, 0.1f), color);
                    AddChildPrimitive(root, "ShieldTop", PrimitiveType.Cube, new Vector3(0f, 0.48f, 0f), new Vector3(0.52f, 0.12f, 0.1f), secondaryColor);
                    AddChildPrimitive(root, "ShieldBottom", PrimitiveType.Cube, new Vector3(0f, -0.48f, 0f), new Vector3(0.52f, 0.12f, 0.1f), secondaryColor);
                    AddChildPrimitive(root, "ShieldLeft", PrimitiveType.Cube, new Vector3(-0.34f, 0f, 0f), new Vector3(0.14f, 0.52f, 0.1f), secondaryColor);
                    AddChildPrimitive(root, "ShieldRight", PrimitiveType.Cube, new Vector3(0.34f, 0f, 0f), new Vector3(0.14f, 0.52f, 0.1f), secondaryColor);
                    AddChildPrimitive(root, "ShieldBoss", PrimitiveType.Sphere, new Vector3(0f, 0f, -0.06f), Vector3.one * 0.18f,
                        new Color(1f, 0.92f, 0.5f, 0.95f));
                    break;

                case PowerUpKind.ScoreStar:
                    AttachProceduralMesh(root, ObstacleMeshBuilder.BuildPowerUpStar(), secondaryColor, "ProcScoreStar");
                    AddChildPrimitive(root, "StarCenter", PrimitiveType.Sphere, Vector3.zero, Vector3.one * 0.12f,
                        new Color(1f, 0.95f, 1f, 0.95f));
                    break;

                case PowerUpKind.DoubleFishSnack:
                    BuildFishSnackProp(root, new Vector3(0f, 0.24f, 0f), 0.82f, 0f);
                    BuildFishSnackProp(root, new Vector3(0f, -0.24f, 0f), 0.82f, 1.7f);
                    AddChildPrimitive(root, "DoubleGlow", PrimitiveType.Sphere, Vector3.zero, Vector3.one * 0.72f,
                        new Color(color.r, color.g, color.b, 0.12f), true);
                    break;

                case PowerUpKind.TimeHourglass:
                    var top = AddChildPrimitive(root, "Hourglass Top", PrimitiveType.Capsule,
                        new Vector3(0f, 0.38f, 0f), new Vector3(0.52f, 0.28f, 0.52f), secondaryColor);
                    top.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                    var bot = AddChildPrimitive(root, "Hourglass Bottom", PrimitiveType.Capsule,
                        new Vector3(0f, -0.38f, 0f), new Vector3(0.52f, 0.28f, 0.52f), secondaryColor);
                    bot.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                    AddChildPrimitive(root, "Hourglass Waist", PrimitiveType.Cylinder,
                        Vector3.zero, new Vector3(0.14f, 0.22f, 0.14f), color);
                    AddChildPrimitive(root, "Hourglass Glow", PrimitiveType.Sphere,
                        Vector3.zero, Vector3.one * 0.28f, new Color(color.r, color.g, color.b, 0.35f), true);
                    break;

                case PowerUpKind.GlideFeather:
                    AddChildPrimitive(root, "FeatherShaft", PrimitiveType.Cylinder,
                        Vector3.zero, new Vector3(0.08f, 0.72f, 0.08f), new Color(0.95f, 0.97f, 1f));
                    var vL = AddChildPrimitive(root, "FeatherVaneL", PrimitiveType.Cube,
                        new Vector3(-0.32f, 0.08f, 0f), new Vector3(0.48f, 0.52f, 0.05f),
                        new Color(color.r, color.g, color.b, 0.55f), true);
                    vL.transform.localRotation = Quaternion.Euler(0f, 0f, -15f);
                    var vR = AddChildPrimitive(root, "FeatherVaneR", PrimitiveType.Cube,
                        new Vector3(0.32f, 0.08f, 0f), new Vector3(0.45f, 0.48f, 0.05f),
                        new Color(color.r, color.g, color.b, 0.5f), true);
                    vR.transform.localRotation = Quaternion.Euler(0f, 0f, 15f);
                    AddChildPrimitive(root, "FeatherTip", PrimitiveType.Sphere,
                        new Vector3(0f, 0.42f, 0f), Vector3.one * 0.12f, secondaryColor, true);
                    break;

                case PowerUpKind.BubbleShield:
                    // 泡泡护盾：透明气泡效果
                    AddChildPrimitive(root, "Bubble Aura", PrimitiveType.Sphere, Vector3.zero, new Vector3(1.25f, 1.25f, 1.25f), new Color(0.6f, 0.95f, 1f, 0.25f), true);
                    AddChildPrimitive(root, "Bubble Sparkles", PrimitiveType.Cube, new Vector3(0f, 0.55f, 0f), new Vector3(0.1f, 0.4f, 0.1f), new Color(1f, 1f, 1f, 0.7f), true);
                    break;
                case PowerUpKind.SeahorseBoost:
                    // 海马加速：流线型特效
                    AddChildPrimitive(root, "Seahorse Tail", PrimitiveType.Cylinder, new Vector3(0f, -0.15f, 0.25f), new Vector3(0.35f, 0.3f, 0.35f), secondaryColor);
                    AddChildPrimitive(root, "Speed Trail", PrimitiveType.Cube, new Vector3(0f, 0f, 0.35f), new Vector3(0.25f, 0.08f, 0.4f), new Color(1f, 0.8f, 0.5f, 0.6f), true);
                    break;
                case PowerUpKind.CloudWalk:
                    // 踏云而行：云朵底座
                    AddChildPrimitive(root, "Cloud Base", PrimitiveType.Sphere, new Vector3(0f, -0.25f, 0f), new Vector3(1.1f, 0.35f, 1.1f), new Color(1f, 1f, 1f, 0.5f), true);
                    AddChildPrimitive(root, "Cloud Puff L", PrimitiveType.Sphere, new Vector3(-0.3f, 0.15f, 0.1f), new Vector3(0.35f, 0.25f, 0.35f), new Color(1f, 1f, 1f, 0.6f), true);
                    AddChildPrimitive(root, "Cloud Puff R", PrimitiveType.Sphere, new Vector3(0.3f, 0.2f, -0.05f), new Vector3(0.4f, 0.3f, 0.4f), new Color(1f, 1f, 1f, 0.55f), true);
                    break;
                case PowerUpKind.WindRider:
                    // 乘风滑翔：风之漩涡
                    AddChildPrimitive(root, "Wind Ring", PrimitiveType.Cylinder, Vector3.zero, new Vector3(1.35f, 0.08f, 1.35f), new Color(color.r, color.g, color.b, 0.35f), true);
                    AddChildPrimitive(root, "Wind Spiral", PrimitiveType.Cube, new Vector3(0f, 0.4f, 0f), new Vector3(0.08f, 0.55f, 0.08f), secondaryColor, true);
                    break;
                case PowerUpKind.FishBomb:
                    // 鱼弹：鱼形 + 引信
                    AddChildPrimitive(root, "Fish Body", PrimitiveType.Capsule, new Vector3(0f, 0f, 0f), new Vector3(0.55f, 0.85f, 0.55f), color);
                    AddChildPrimitive(root, "Fish Tail", PrimitiveType.Cube, new Vector3(0f, 0f, 0.4f), new Vector3(0.4f, 0.3f, 0.18f), secondaryColor);
                    AddChildPrimitive(root, "Fish Eye", PrimitiveType.Sphere, new Vector3(0.18f, 0.1f, -0.3f), Vector3.one * 0.1f, Color.white);
                    AddChildPrimitive(root, "Fuse Spark", PrimitiveType.Sphere, new Vector3(0f, 0.5f, 0f), Vector3.one * 0.15f, new Color(1f, 0.9f, 0.4f, 0.95f), true);
                    break;
                case PowerUpKind.SecondHeart:
                    // 心形（用 2 个球 + 1 个倒立的菱形拼接）
                    AddChildPrimitive(root, "Heart Lobe L", PrimitiveType.Sphere, new Vector3(-0.18f, 0.18f, 0f), Vector3.one * 0.45f, color);
                    AddChildPrimitive(root, "Heart Lobe R", PrimitiveType.Sphere, new Vector3(0.18f, 0.18f, 0f), Vector3.one * 0.45f, color);
                    var tip = AddChildPrimitive(root, "Heart Tip", PrimitiveType.Cube, new Vector3(0f, -0.18f, 0f), new Vector3(0.55f, 0.55f, 0.55f), color);
                    tip.transform.localRotation = Quaternion.Euler(0f, 0f, 45f);
                    AddChildPrimitive(root, "Heart Glow", PrimitiveType.Sphere, new Vector3(0f, 0f, 0f), Vector3.one * 1f, new Color(1f, 0.7f, 0.85f, 0.25f), true);
                    break;

                // ── 新道具视觉特效 ─────────────────────────────
                case PowerUpKind.IceMirror:
                    // 冰镜：六边形镜面 + 冰晶碎片
                    AddChildPrimitive(root, "MirrorFace", PrimitiveType.Cube, Vector3.zero, new Vector3(1.1f, 1.1f, 0.06f), new Color(0.85f, 0.97f, 1f, 0.55f), true);
                    AddChildPrimitive(root, "IceShard L", PrimitiveType.Cube, new Vector3(-0.55f, 0.35f, 0f), new Vector3(0.12f, 0.42f, 0.08f), color, true);
                    AddChildPrimitive(root, "IceShard R", PrimitiveType.Cube, new Vector3(0.55f, -0.2f, 0f), new Vector3(0.12f, 0.38f, 0.08f), color, true);
                    AddChildPrimitive(root, "MirrorGlow", PrimitiveType.Sphere, Vector3.zero, Vector3.one * 1.2f, new Color(color.r, color.g, color.b, 0.18f), true);
                    break;
                case PowerUpKind.AuroraChain:
                    // 极光连锁：链条光环 + 极光涟漪
                    AddChildPrimitive(root, "Chain Ring 1", PrimitiveType.Cylinder, new Vector3(0f, 0.2f, 0f), new Vector3(1.3f, 0.06f, 1.3f), color, true);
                    AddChildPrimitive(root, "Chain Ring 2", PrimitiveType.Cylinder, new Vector3(0f, -0.2f, 0f), new Vector3(0.95f, 0.06f, 0.95f), secondaryColor, true);
                    AddChildPrimitive(root, "Aurora Core", PrimitiveType.Sphere, Vector3.zero, Vector3.one * 0.45f, new Color(secondaryColor.r, secondaryColor.g, secondaryColor.b, 0.8f));
                    AddChildPrimitive(root, "Aurora Glow", PrimitiveType.Sphere, Vector3.zero, Vector3.one * 1.35f, new Color(color.r, color.g, color.b, 0.2f), true);
                    break;
                case PowerUpKind.FogLantern:
                    // 雾灯：提灯外壳 + 暖光晕
                    AddChildPrimitive(root, "Lantern Body", PrimitiveType.Cylinder, Vector3.zero, new Vector3(0.55f, 0.85f, 0.55f), new Color(0.6f, 0.5f, 0.35f));
                    AddChildPrimitive(root, "Lantern Top", PrimitiveType.Cube, new Vector3(0f, 0.55f, 0f), new Vector3(0.35f, 0.18f, 0.35f), new Color(0.45f, 0.35f, 0.2f));
                    AddChildPrimitive(root, "Lantern Light", PrimitiveType.Sphere, new Vector3(0f, 0.05f, 0f), Vector3.one * 0.38f, new Color(1f, 0.95f, 0.55f, 0.9f), true);
                    AddChildPrimitive(root, "Lantern Glow", PrimitiveType.Sphere, Vector3.zero, Vector3.one * 1.55f, new Color(1f, 0.92f, 0.5f, 0.18f), true);
                    break;
                case PowerUpKind.TreantArmor:
                    // 树人护甲：树皮纹理板 + 树叶簇
                    AddChildPrimitive(root, "Bark Plate", PrimitiveType.Cube, Vector3.zero, new Vector3(1.0f, 1.15f, 0.22f), color);
                    AddChildPrimitive(root, "Leaf Tuft L", PrimitiveType.Sphere, new Vector3(-0.45f, 0.55f, 0f), new Vector3(0.5f, 0.4f, 0.5f), new Color(0.25f, 0.72f, 0.28f, 0.85f));
                    AddChildPrimitive(root, "Leaf Tuft R", PrimitiveType.Sphere, new Vector3(0.45f, 0.45f, 0f), new Vector3(0.42f, 0.35f, 0.42f), new Color(0.3f, 0.78f, 0.32f, 0.85f));
                    AddChildPrimitive(root, "Armor Glow", PrimitiveType.Sphere, Vector3.zero, Vector3.one * 1.15f, new Color(secondaryColor.r, secondaryColor.g, secondaryColor.b, 0.2f), true);
                    break;
                case PowerUpKind.CoralBounce:
                    // 珊瑚回弹：珊瑚枝 + 水花弹出特效
                    AddChildPrimitive(root, "Coral Stem", PrimitiveType.Cylinder, new Vector3(0f, -0.15f, 0f), new Vector3(0.3f, 0.7f, 0.3f), color);
                    AddChildPrimitive(root, "Coral Branch L", PrimitiveType.Cylinder, new Vector3(-0.3f, 0.22f, 0f), new Vector3(0.15f, 0.42f, 0.15f), color);
                    AddChildPrimitive(root, "Coral Branch R", PrimitiveType.Cylinder, new Vector3(0.28f, 0.18f, 0.1f), new Vector3(0.15f, 0.38f, 0.15f), color);
                    AddChildPrimitive(root, "Bounce Aura", PrimitiveType.Sphere, Vector3.zero, Vector3.one * 1.25f, new Color(secondaryColor.r, secondaryColor.g, secondaryColor.b, 0.28f), true);
                    AddChildPrimitive(root, "Splash Ring", PrimitiveType.Cylinder, new Vector3(0f, -0.3f, 0f), new Vector3(1.4f, 0.05f, 1.4f), new Color(0.4f, 1f, 0.9f, 0.4f), true);
                    break;
                case PowerUpKind.ThunderFeather:
                    // 雷羽：羽毛形 + 闪电条纹
                    AddChildPrimitive(root, "Feather Shaft", PrimitiveType.Cylinder, Vector3.zero, new Vector3(0.12f, 1.05f, 0.12f), color);
                    AddChildPrimitive(root, "Feather Vane L", PrimitiveType.Cube, new Vector3(-0.28f, 0.15f, 0f), new Vector3(0.45f, 0.72f, 0.06f), new Color(color.r, color.g, color.b, 0.8f));
                    AddChildPrimitive(root, "Feather Vane R", PrimitiveType.Cube, new Vector3(0.28f, 0.1f, 0f), new Vector3(0.42f, 0.65f, 0.06f), new Color(color.r, color.g, color.b, 0.75f));
                    AddChildPrimitive(root, "Lightning Bolt", PrimitiveType.Cube, new Vector3(0.08f, 0.3f, -0.08f), new Vector3(0.08f, 0.55f, 0.08f), new Color(1f, 1f, 0.6f, 0.95f), true);
                    AddChildPrimitive(root, "Thunder Glow", PrimitiveType.Sphere, Vector3.zero, Vector3.one * 1.3f, new Color(secondaryColor.r, secondaryColor.g, secondaryColor.b, 0.22f), true);
                    break;

                default:
                    AddChildPrimitive(root, "Pickup Spark", PrimitiveType.Cube, new Vector3(0f, 0.72f, 0f), new Vector3(0.12f, 0.52f, 0.12f), color, true);
                    break;
            }
        }

        private static void SetPickupCoreVisible(Transform root, bool visible)
        {
            for (var i = 0; i < root.childCount; i++)
            {
                var child = root.GetChild(i);
                if (child.name != "Pickup Core") continue;
                child.gameObject.SetActive(visible);
                return;
            }
        }

        private static void AddPickupRecognitionRing(Transform root, Color color)
        {
            // 贴地光环：标记位置但不遮挡模型。
            // 早期版本是 1.42m 面向镜头的半透明大圆盘，远看像“白色光罩圆顶”，把道具模型整个盖住。
            var halo = AddChildPrimitive(root, "Pickup Ring", PrimitiveType.Cylinder,
                new Vector3(0f, -0.48f, 0f), new Vector3(0.72f, 0.012f, 0.72f),
                new Color(color.r, color.g, color.b, 0.18f), true);
            halo.transform.localRotation = Quaternion.identity;
        }

        private static GameObject AddChildPrimitive(Transform parent, string name, PrimitiveType type, Vector3 localPosition, Vector3 localScale, Color color, bool transparent = false)
        {
            var child = RunnerVisuals.CreatePrimitive(name, type, Vector3.zero, localScale, color, transparent);
            child.transform.SetParent(parent, false);
            child.transform.localPosition = localPosition;
            child.transform.localScale = localScale;
            return child;
        }

        private sealed class PowerUpPulse : MonoBehaviour
        {
            private const float SpinDegreesPerSecond = 55f;

            private float scale = 1.1f;
            private float phase;
            public void Configure(float pulseScale) { scale = pulseScale; phase = Random.value * Mathf.PI * 2f; }
            private void Update()
            {
                var pulse = 1f + (scale - 1f) * (0.5f + 0.5f * Mathf.Sin(Time.time * 4f + phase));
                transform.localScale = Vector3.one * pulse;
                // 统一道具自旋：与脉冲叠加，提升场景动态辨识度
                transform.Rotate(Vector3.up, SpinDegreesPerSecond * Time.deltaTime, Space.World);
            }
        }
    }
}
