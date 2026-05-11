using System.Collections.Generic;
using PenguinRun.Game;
using UnityEngine;

namespace PenguinRun
{
    internal sealed class SegmentSpawner
    {
        // 所有物体的显示高度都基于 TrackTerrain.SurfaceY (地面表面) 计算
        private const float CoinHoverHeight = 0.45f;      // 金币悬浮在表面上方
        private const float PowerUpHoverHeight = 0.55f;   // 道具悬浮高度
        private const float OpenObstacleVisualScaleBoost = 1.28f;
        private const float OpenWideWallVisualScaleBoost = 1.2f;
        private const float OpenEnemyVisualScaleBoost = 1.25f;

        private sealed class RollingState
        {
            public GameObject Go;
            public float LaneCenterX;
            public float Amplitude;
            public float Frequency;
            public float Phase;
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
            this.waveDirector = waveDirector;
            this.baseGroundY = baseGroundY;
            nextSpawnZ = worldTuning.initialSpawnZ;
        }

        public readonly List<GameObject> obstacles = new();
        public readonly Dictionary<GameObject, ObstacleColliderSpec> obstacleSpecs = new();
        public readonly List<GameObject> coins = new();
        public readonly Dictionary<GameObject, PowerUpKind> powerUps = new();

        public void Reset()
        {
            foreach (var obstacle in obstacles) Object.Destroy(obstacle);
            obstacles.Clear();
            obstacleSpecs.Clear();
            rollingObstacles.Clear();
            enemyNpcs.Clear();
            foreach (var c in coins) Object.Destroy(c);
            coins.Clear();
            foreach (var power in powerUps.Keys) Object.Destroy(power);
            powerUps.Clear();
            obstacleYDelta.Clear();
            coinYDelta.Clear();
            nextSpawnZ = worldTuning.initialSpawnZ;
            waveDirector.ResetForRun();
        }

        public void SeedInitialObstacles(float runDistance)
        {
            for (var i = 0; i < 5; i++)
            {
                SpawnStandardObstacle(26f + i * 18f, (RunnerLane)Random.Range(-1, 2), Random.value > 0.72f, SegmentObstaclePalette.IceLake, runDistance);
            }
        }

        /// <summary>当 boss 战激活时由外部设为 true，暂停障碍生成。</summary>
        public bool SpawnPaused { get; set; }

        public void Tick(float dt, float effectiveSpeed, float runDistance)
        {
            nextSpawnZ -= effectiveSpeed * dt;
            if (!SpawnPaused && nextSpawnZ < worldTuning.spawnDistance)
            {
                var modifiers = waveDirector.ConsumeWave();
                var lead = Random.Range(worldTuning.spawnLeadMin, worldTuning.spawnLeadMax);
                SpawnPattern(nextSpawnZ + worldTuning.spawnDistance + lead, modifiers, runDistance);
                nextSpawnZ += Random.Range(worldTuning.spawnSpacingMin, worldTuning.spawnSpacingMax);
            }
            else if (SpawnPaused)
            {
                // boss 战期间，不堆积新障碍生成时机
                nextSpawnZ = Mathf.Max(nextSpawnZ, worldTuning.spawnDistance + 4f);
            }

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
            SyncObstacleHeights(runDistance);

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
                    Object.Destroy(c);
                    coinYDelta.Remove(c);
                    coins.RemoveAt(i);
                }
            }

            var powerKeys = new List<GameObject>(powerUps.Keys);
            foreach (var power in powerKeys)
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
                    Object.Destroy(power);
                    powerUps.Remove(power);
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
            obstacleYDelta.Remove(obstacle);
            Object.Destroy(obstacle);
            obstacleSpecs.Remove(obstacle);
            obstacles.RemoveAt(index);
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
            const float rollDy = 0.53f; // 与 SpawnRollingSnowball 保持一致
            for (var i = rollingObstacles.Count - 1; i >= 0; i--)
            {
                var state = rollingObstacles[i];
                if (state.Go == null || !obstacles.Contains(state.Go))
                {
                    rollingObstacles.RemoveAt(i);
                    continue;
                }

                state.Phase += dt * state.Frequency * Mathf.PI * 2f;
                var pos = state.Go.transform.position;
                pos.x = state.LaneCenterX + Mathf.Sin(state.Phase) * state.Amplitude;
                pos.y = TrackTerrain.SurfaceY(pos.z, runDistance, baseGroundY) + rollDy;
                state.Go.transform.position = pos;
            }
        }

        private void SpawnPattern(float z, SegmentSpawnModifiers m, float runDistance)
        {
            // 随跑动距离逐步引入移动敌人 NPC（距离 >200m 开始出现，>500m 概率提升）
            var enemyChance = runDistance > 500f ? 0.24f : runDistance > 200f ? 0.13f : 0f;
            if (Random.value < enemyChance)
            {
                var spawnZ = z + Random.Range(10f, 20f);
                if (Random.value > 0.45f)
                    SpawnPatrolEnemy(spawnZ, runDistance);
                else
                    SpawnSwoopEnemy(spawnZ, runDistance);
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
                    SpawnWideSnowWall(zz, obstacleLane, m.Palette, runDistance);
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
                _ => false,
            };
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

            SpawnCoinLine(z + 7f, PickDifferentLane(lane), 3, runDistance);
            return true;
        }

        private bool SpawnSlideTunnel(float z, SegmentSpawnModifiers m, float runDistance)
        {
            var safeLane = (RunnerLane)Random.Range(-1, 2);
            for (var i = 0; i < 3; i++)
            {
                SpawnStandardObstacle(z + i * 2.55f, safeLane, false, m.Palette, runDistance);
            }

            for (var laneValue = -1; laneValue <= 1; laneValue++)
            {
                var lane = (RunnerLane)laneValue;
                if (lane == safeLane) continue;
                SpawnWideSnowWall(z + 1.8f + Mathf.Abs(laneValue) * 0.45f, lane, m.Palette, runDistance);
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
                    SpawnWideSnowWall(z + Random.Range(-0.3f, 0.85f), lane, m.Palette, runDistance);
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
            var threshold = Mathf.Clamp(0.55f - m.LowObstacleBiasDelta, 0.15f, 0.92f);
            return Random.value > threshold;
        }

        private void TrySpawnThreatObstacle(float z, RunnerLane lane, SegmentSpawnModifiers m, float runDistance)
        {
            var roll = Random.value;
            var rollingGate = m.RollingSpawnChance;
            var wideGate = rollingGate + m.WideWallChance;
            if (roll < rollingGate)
            {
                SpawnRollingSnowball(z, lane, m.Palette, runDistance);
            }
            else if (roll < wideGate)
            {
                SpawnWideSnowWall(z, lane, m.Palette, runDistance);
            }
            else
            {
                SpawnStandardObstacle(z, lane, LowRoll(m), m.Palette, runDistance);
            }
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

        private void RegisterObstacle(GameObject obstacle, in ObstacleColliderSpec spec, float yDeltaFromFlatGround)
        {
            obstacles.Add(obstacle);
            obstacleSpecs[obstacle] = spec;
            obstacleYDelta[obstacle] = yDeltaFromFlatGround;
        }

        // ── 移动敌人 NPC ───────────────────────────────────────────────────────────

        /// <summary>每帧更新所有移动敌人的横向位置，Z 方向已由主障碍循环统一推进。</summary>
        private void TickEnemies(float dt, float runDistance)
        {
            for (var i = enemyNpcs.Count - 1; i >= 0; i--)
            {
                var state = enemyNpcs[i];
                if (state.Go == null || !obstacles.Contains(state.Go))
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
                // 主体 —— 深红色企鹅轮廓（以脚底为原点构建）
                AddChildPrimitive(enemy.transform, "Enemy Body", PrimitiveType.Cube,
                    new Vector3(0f, 0.59f, 0f), new Vector3(0.88f, 1.18f, 0.7f), new Color(0.62f, 0.08f, 0.12f));
                AddChildPrimitive(enemy.transform, "Enemy Head", PrimitiveType.Cube,
                    new Vector3(0f, 1.31f, 0f), new Vector3(0.7f, 0.6f, 0.66f), new Color(0.75f, 0.12f, 0.16f));
                // 眼睛 —— 亮黄色
                AddChildPrimitive(enemy.transform, "Eye L", PrimitiveType.Sphere,
                    new Vector3(-0.16f, 1.37f, -0.34f), new Vector3(0.17f, 0.17f, 0.17f), new Color(1f, 0.88f, 0.15f));
                AddChildPrimitive(enemy.transform, "Eye R", PrimitiveType.Sphere,
                    new Vector3(0.16f, 1.37f, -0.34f), new Vector3(0.17f, 0.17f, 0.17f), new Color(1f, 0.88f, 0.15f));
            }
            // 危险光环（放在脚底）
            AddChildPrimitive(enemy.transform, "Danger Ring", PrimitiveType.Cylinder,
                new Vector3(0f, 0.05f, 0f), new Vector3(1.55f, 0.055f, 1.55f),
                new Color(1f, 0.28f, 0.08f, 0.75f), true);

            RegisterObstacle(enemy, new ObstacleColliderSpec(false, 0.52f, 0.52f), dy);
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

            RegisterObstacle(enemy, new ObstacleColliderSpec(false, 0.58f, 0.58f), dy);
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

            // 优先使用开源低模素材构建障碍外观；若加载失败则回退到原始 primitive 版本
            var builtWithModel = TryBuildOpenModelObstacle(obstacle.transform, lowBarrier, palette);
            if (lowBarrier)
            {
                if (!builtWithModel)
                {
                    BuildLowBarrier(obstacle.transform, palette);
                }
                RegisterObstacle(obstacle, ObstacleColliderSpec.DefaultLow, dy);
            }
            else
            {
                if (!builtWithModel)
                {
                    BuildHighObstacle(obstacle.transform, palette);
                }
                RegisterObstacle(obstacle, ObstacleColliderSpec.DefaultHigh, dy);
            }
        }

        private bool TryBuildOpenModelObstacle(Transform root, bool lowBarrier, SegmentObstaclePalette palette)
        {
            string modelName;
            Vector3 localScale;
            Vector3 localPos;
            Color tint;

            if (lowBarrier)
            {
                switch (palette)
                {
                    case SegmentObstaclePalette.OceanCoral:
                        modelName = "mushroom_redGroup";
                        localScale = new Vector3(0.42f, 0.42f, 0.42f);
                        localPos = new Vector3(0f, -0.12f, 0f);
                        tint = new Color(0.95f, 0.68f, 0.58f);
                        break;
                    case SegmentObstaclePalette.SkyCloud:
                        modelName = "stone_2";
                        localScale = new Vector3(0.58f, 0.58f, 0.58f);
                        localPos = new Vector3(0f, -0.05f, 0f);
                        tint = new Color(0.84f, 0.9f, 0.98f);
                        break;
                    case SegmentObstaclePalette.CedarWood:
                    case SegmentObstaclePalette.MistFog:
                        modelName = "stone_1";
                        localScale = new Vector3(0.62f, 0.62f, 0.62f);
                        localPos = new Vector3(0f, -0.06f, 0f);
                        tint = new Color(0.58f, 0.52f, 0.46f);
                        break;
                    default:
                        modelName = "stone_1";
                        localScale = new Vector3(0.64f, 0.64f, 0.64f);
                        localPos = new Vector3(0f, -0.06f, 0f);
                        tint = new Color(0.72f, 0.84f, 0.94f);
                        break;
                }
            }
            else
            {
                switch (palette)
                {
                    case SegmentObstaclePalette.SkyCloud:
                        modelName = "leaftree";
                        localScale = new Vector3(0.28f, 0.28f, 0.28f);
                        localPos = new Vector3(0f, -0.5f, 0f);
                        tint = new Color(0.78f, 0.9f, 1f);
                        break;
                    case SegmentObstaclePalette.CedarWood:
                    case SegmentObstaclePalette.MistFog:
                        modelName = "tree_simple";
                        localScale = new Vector3(0.45f, 0.45f, 0.45f);
                        localPos = new Vector3(0f, -0.48f, 0f);
                        tint = new Color(0.58f, 0.78f, 0.6f);
                        break;
                    case SegmentObstaclePalette.AuroraGlow:
                        modelName = "rock_largeA";
                        localScale = new Vector3(0.72f, 0.72f, 0.72f);
                        localPos = new Vector3(0f, -0.22f, 0f);
                        tint = new Color(0.64f, 0.58f, 0.92f);
                        break;
                    case SegmentObstaclePalette.OceanCoral:
                        modelName = "rock_largeA";
                        localScale = new Vector3(0.68f, 0.68f, 0.68f);
                        localPos = new Vector3(0f, -0.22f, 0f);
                        tint = new Color(0.62f, 0.86f, 0.88f);
                        break;
                    default:
                        modelName = "rock_largeA";
                        localScale = new Vector3(0.7f, 0.7f, 0.7f);
                        localPos = new Vector3(0f, -0.22f, 0f);
                        tint = new Color(0.72f, 0.8f, 0.9f);
                        break;
                }
            }

            var prefab = LoadModelPrefab(modelName);
            if (prefab == null)
            {
                return false;
            }

            var inst = Object.Instantiate(prefab, root, false);
            inst.name = $"ObstacleModel_{modelName}";
            inst.transform.localPosition = localPos;
            inst.transform.localRotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
            inst.transform.localScale = localScale * OpenObstacleVisualScaleBoost;
            TintModel(inst, tint);
            return true;
        }

        private GameObject LoadModelPrefab(string modelName)
        {
            if (string.IsNullOrEmpty(modelName))
            {
                return null;
            }

            if (modelPrefabCache.TryGetValue(modelName, out var cached))
            {
                return cached;
            }

            var prefab = Resources.Load<GameObject>($"PenguinRun/Models/{modelName}");
            modelPrefabCache[modelName] = prefab;
            return prefab;
        }

        private static void TintModel(GameObject root, Color tint)
        {
            if (root == null) return;
            foreach (var renderer in root.GetComponentsInChildren<Renderer>(true))
            {
                if (renderer == null) continue;
                var mat = renderer.material;
                if (mat == null) continue;
                if (mat.HasProperty("_BaseColor"))
                {
                    var baseColor = mat.GetColor("_BaseColor");
                    mat.SetColor("_BaseColor", Color.Lerp(baseColor, tint, 0.4f));
                }
                if (mat.HasProperty("_Color"))
                {
                    var baseColor = mat.GetColor("_Color");
                    mat.SetColor("_Color", Color.Lerp(baseColor, tint, 0.4f));
                }
            }
        }

        private static void BuildLowBarrier(Transform root, SegmentObstaclePalette palette)
        {
            // 低障碍中心在 dy=0.2f，模型从中心向上构建
            switch (palette)
            {
                case SegmentObstaclePalette.CedarWood:
                case SegmentObstaclePalette.MistFog:
                    AddChildPrimitive(root, "Wood Crate", PrimitiveType.Cube, new Vector3(0f, 0.05f, 0f), new Vector3(1.48f, 0.32f, 1.2f), new Color(0.45f, 0.32f, 0.2f));
                    AddChildPrimitive(root, "Wood Rim", PrimitiveType.Cube, new Vector3(0f, 0.1f, -0.02f), new Vector3(1.58f, 0.1f, 1.28f), new Color(0.62f, 0.48f, 0.34f));
                    break;
                case SegmentObstaclePalette.OceanCoral:
                    // 海洋主题低障碍：贝壳障碍
                    AddChildPrimitive(root, "Seashell Base", PrimitiveType.Cube, new Vector3(0f, 0.02f, 0f), new Vector3(1.4f, 0.24f, 1.15f), new Color(0.95f, 0.82f, 0.72f));
                    AddChildPrimitive(root, "Seashell Top", PrimitiveType.Cube, new Vector3(0f, 0.14f, 0f), new Vector3(1.2f, 0.18f, 0.95f), new Color(0.98f, 0.88f, 0.78f));
                    AddChildPrimitive(root, "Shell Glow", PrimitiveType.Cube, new Vector3(0f, 0.08f, 0.35f), new Vector3(0.6f, 0.06f, 0.08f), new Color(0.2f, 0.85f, 0.9f, 0.6f), true);
                    break;
                case SegmentObstaclePalette.SkyCloud:
                    // 天空主题低障碍：云朵块
                    AddChildPrimitive(root, "Cloud Puff", PrimitiveType.Sphere, new Vector3(-0.3f, 0.08f, 0f), new Vector3(0.55f, 0.25f, 0.5f), new Color(0.98f, 0.98f, 1f, 0.9f), true);
                    AddChildPrimitive(root, "Cloud Main", PrimitiveType.Sphere, new Vector3(0.2f, 0.05f, 0f), new Vector3(0.85f, 0.3f, 0.65f), new Color(0.95f, 0.95f, 1f, 0.85f), true);
                    AddChildPrimitive(root, "Cloud Rim", PrimitiveType.Cube, new Vector3(0f, -0.02f, 0.2f), new Vector3(0.5f, 0.06f, 0.06f), new Color(0.55f, 0.75f, 1f, 0.7f), true);
                    break;
                default:
                    // 默认主题：更精致的滑铲门设计（条纹横梁 + 警示灯）
                    AddChildPrimitive(root, "Gate Beam", PrimitiveType.Cube, new Vector3(0f, 0.03f, 0f), new Vector3(1.45f, 0.28f, 1.25f), new Color(0.88f, 0.38f, 0.12f));
                    AddChildPrimitive(root, "Gate Stripe", PrimitiveType.Cube, new Vector3(0f, 0.04f, 0f), new Vector3(1.4f, 0.08f, 0.18f), new Color(0.18f, 0.1f, 0.06f));
                    AddChildPrimitive(root, "Gate Glow", PrimitiveType.Cube, new Vector3(0f, 0.06f, -0.02f), new Vector3(1.58f, 0.06f, 1.36f), new Color(1f, 0.52f, 0.18f, 0.62f), true);
                    AddChildPrimitive(root, "Gate Rim Hazard", PrimitiveType.Cube, new Vector3(0f, 0.04f, 0.62f), new Vector3(1.55f, 0.04f, 0.12f), new Color(0.95f, 0.2f, 0.15f));
                    // 两侧支柱
                    AddChildPrimitive(root, "Gate Pillar L", PrimitiveType.Cube, new Vector3(-0.74f, 0.03f, 0f), new Vector3(0.16f, 0.46f, 0.16f), new Color(0.5f, 0.32f, 0.18f));
                    AddChildPrimitive(root, "Gate Pillar R", PrimitiveType.Cube, new Vector3(0.74f, 0.03f, 0f), new Vector3(0.16f, 0.46f, 0.16f), new Color(0.5f, 0.32f, 0.18f));
                    AddChildPrimitive(root, "Warning Lamp L", PrimitiveType.Sphere, new Vector3(-0.74f, 0.18f, 0f), Vector3.one * 0.13f, new Color(1f, 0.95f, 0.4f, 0.85f), true);
                    AddChildPrimitive(root, "Warning Lamp R", PrimitiveType.Sphere, new Vector3(0.74f, 0.18f, 0f), Vector3.one * 0.13f, new Color(1f, 0.95f, 0.4f, 0.85f), true);
                    break;
            }
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

            var builtWithModel = TryBuildOpenModelWideWall(obstacle.transform, palette);
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
            RegisterObstacle(obstacle, new ObstacleColliderSpec(false, 1.28f, 0.66f), dy);
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
                ? new Vector3(1.1f, 0.95f, 1.45f)
                : new Vector3(2.25f, 1.05f, 1.35f);
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
            var modelName = patrolEnemy ? "cactus_tall" : "tree_palmShort";
            var prefab = LoadModelPrefab(modelName);
            if (prefab == null) return false;
            var inst = Object.Instantiate(prefab, root, false);
            inst.name = patrolEnemy ? "PatrolEnemyModel" : "SwoopEnemyModel";
            inst.transform.localPosition = patrolEnemy ? new Vector3(0f, -0.22f, 0f) : new Vector3(0f, -0.18f, 0f);
            inst.transform.localRotation = Quaternion.Euler(0f, patrolEnemy ? 0f : 90f, patrolEnemy ? 0f : 12f);
            var scale = patrolEnemy ? new Vector3(0.85f, 0.85f, 0.85f) : new Vector3(0.72f, 0.72f, 0.72f);
            inst.transform.localScale = scale * OpenEnemyVisualScaleBoost;
            TintModel(inst, patrolEnemy ? new Color(0.74f, 0.2f, 0.22f) : new Color(0.58f, 0.36f, 0.82f));
            return true;
        }

        private void SpawnRollingSnowball(float z, RunnerLane obstacleLane, SegmentObstaclePalette palette, float runDistance)
        {
            var obstacle = RunnerVisuals.CreatePrimitive(
                "Rolling Snowball",
                PrimitiveType.Sphere,
                Vector3.zero,
                Vector3.one * 1.05f,
                palette switch
                {
                    SegmentObstaclePalette.AuroraGlow => new Color(0.48f, 0.42f, 0.62f),
                    SegmentObstaclePalette.MistFog => new Color(0.45f, 0.5f, 0.56f),
                    _ => new Color(0.42f, 0.48f, 0.56f),
                });
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
            RegisterObstacle(obstacle, new ObstacleColliderSpec(false, 0.68f, 0.68f), dy);
            rollingObstacles.Add(
                new RollingState
                {
                    Go = obstacle,
                    LaneCenterX = obstacleLane.ToX(laneWidth),
                    Amplitude = 0.82f,
                    Frequency = 1.65f,
                    Phase = Random.Range(0f, Mathf.PI * 2f),
                });
        }

        private void SpawnCoinLine(float z, RunnerLane coinLane, int count, float runDistance)
        {
            for (var i = 0; i < count; i++)
            {
                SpawnCoin(z + i * 1.85f, coinLane, runDistance);
            }
        }

        private void SpawnCoin(float z, RunnerLane coinLane, float runDistance, float bonusY = 0f)
        {
            var coin = RunnerVisuals.CreatePrimitive("Fish Coin", PrimitiveType.Sphere, Vector3.zero, Vector3.one * 0.42f, new Color(1f, 0.82f, 0.25f));
            var surface = TrackTerrain.SurfaceY(z, runDistance, baseGroundY);
            coin.transform.position = new Vector3(coinLane.ToX(laneWidth), surface + CoinHoverHeight + bonusY, z);
            coins.Add(coin);
            if (bonusY > 0f)
            {
                coinYDelta[coin] = bonusY;
            }
        }

        private void SpawnPowerUp(float z, float runDistance, SegmentObstaclePalette? paletteHint = null)
        {
            var powerLane = (RunnerLane)Random.Range(-1, 2);
            var r = Random.value;
            PowerUpKind kind;

            // 根据主题生成专属道具的概率
            if (paletteHint == SegmentObstaclePalette.OceanCoral && Random.value < 0.35f)
            {
                kind = Random.value < 0.6f ? PowerUpKind.BubbleShield : PowerUpKind.SeahorseBoost;
            }
            else if (paletteHint == SegmentObstaclePalette.SkyCloud && Random.value < 0.35f)
            {
                kind = Random.value < 0.6f ? PowerUpKind.CloudWalk : PowerUpKind.WindRider;
            }
            else
            {
                var tStar = powerUpTuning.powerPickScoreStarMax;
                var tDouble = tStar + powerUpTuning.powerPickDoubleFishBand;
                var tHour = tDouble + powerUpTuning.powerPickHourglassBand;
                // 在常规掉落上额外有 6% 概率掉鱼弹/心
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

            var (color, secondaryColor) = GetPowerUpColors(kind);
            var power = RunnerVisuals.CreatePrimitive($"PowerUp {kind}", PrimitiveType.Sphere, Vector3.zero, Vector3.one * 0.62f, color);
            power.name = $"PowerUp {kind}";
            var laneX = powerLane.ToX(laneWidth);
            var surface = TrackTerrain.SurfaceY(z, runDistance, baseGroundY);
            power.transform.position = new Vector3(laneX, surface + PowerUpHoverHeight, z);

            // 根据道具类型添加特效
            AddPowerUpEffects(power.transform, kind, color, secondaryColor);
            powerUps[power] = kind;
        }

        private static (Color primary, Color secondary) GetPowerUpColors(PowerUpKind kind)
        {
            return kind switch
            {
                PowerUpKind.Dash => (new Color(0.25f, 1f, 0.82f), new Color(0.15f, 0.8f, 0.65f)),
                PowerUpKind.Magnet => (new Color(0.52f, 0.68f, 1f), new Color(0.35f, 0.5f, 0.9f)),
                PowerUpKind.Shield => (new Color(1f, 0.88f, 0.35f), new Color(0.85f, 0.65f, 0.2f)),
                PowerUpKind.ScoreStar => (new Color(1f, 0.58f, 0.9f), new Color(0.9f, 0.35f, 0.75f)),
                PowerUpKind.DoubleFishSnack => (new Color(1f, 0.72f, 0.38f), new Color(0.9f, 0.55f, 0.25f)),
                PowerUpKind.TimeHourglass => (new Color(0.72f, 0.95f, 1f), new Color(0.55f, 0.8f, 0.9f)),
                PowerUpKind.GlideFeather => (new Color(0.92f, 0.96f, 1f), new Color(0.75f, 0.85f, 0.95f)),
                PowerUpKind.BubbleShield => (new Color(0.25f, 0.85f, 0.95f), new Color(0.6f, 0.95f, 1f)), // 海洋：蓝绿色泡泡
                PowerUpKind.SeahorseBoost => (new Color(1f, 0.45f, 0.65f), new Color(1f, 0.7f, 0.55f)), // 海洋：珊瑚粉
                PowerUpKind.CloudWalk => (new Color(0.95f, 0.98f, 1f), new Color(0.75f, 0.85f, 1f)),     // 天空：云白
                PowerUpKind.WindRider => (new Color(0.35f, 0.75f, 1f), new Color(0.6f, 0.9f, 1f)),       // 天空：天蓝
                PowerUpKind.FishBomb => (new Color(1f, 0.4f, 0.3f), new Color(1f, 0.85f, 0.4f)),         // 鱼弹：橙红
                PowerUpKind.SecondHeart => (new Color(1f, 0.42f, 0.55f), new Color(1f, 0.78f, 0.85f)),   // 心：粉红
                _ => (new Color(0.92f, 0.96f, 1f), new Color(0.75f, 0.85f, 0.95f)),
            };
        }

        private static void AddPowerUpEffects(Transform root, PowerUpKind kind, Color color, Color secondaryColor)
        {
            var ring = AddChildPrimitive(root, "Pickup Ring", PrimitiveType.Cylinder, Vector3.zero, new Vector3(1.42f, 0.04f, 1.42f), new Color(color.r, color.g, color.b, 0.32f), true);
            ring.transform.rotation = Quaternion.Euler(90f, 0f, 0f);

            switch (kind)
            {
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
                default:
                    AddChildPrimitive(root, "Pickup Spark", PrimitiveType.Cube, new Vector3(0f, 0.72f, 0f), new Vector3(0.12f, 0.52f, 0.12f), color, true);
                    break;
            }
        }

        private static GameObject AddChildPrimitive(Transform parent, string name, PrimitiveType type, Vector3 localPosition, Vector3 localScale, Color color, bool transparent = false)
        {
            var child = RunnerVisuals.CreatePrimitive(name, type, Vector3.zero, localScale, color, transparent);
            child.transform.SetParent(parent, false);
            child.transform.localPosition = localPosition;
            child.transform.localScale = localScale;
            return child;
        }
    }
}
