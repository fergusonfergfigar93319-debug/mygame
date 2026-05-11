using System.Collections.Generic;
using UnityEngine;

namespace PenguinRun
{
    /// <summary>
    /// 单次 boss 遭遇的运行时实例：负责自身 GameObject、攻击模式时间线、外观更新。
    /// </summary>
    internal sealed class BossEncounter
    {
        public BossDefinition Definition { get; }
        public GameObject Root { get; }
        public BossPhase Phase { get; set; }
        public BossPattern Pattern { get; private set; }
        public int HitsRemaining { get; set; } = BossSystem.BossMaxHits;
        public float HitFlashTimer { get; set; }
        public float RetreatTimer { get; set; }
        public float SafeLaneX { get; private set; }
        public List<float> SalvoLaneX { get; } = new();
        public bool IsInAttackInterval => Phase == BossPhase.Active && Pattern == BossPattern.None && patternIntervalTimer > 0f;
        public float AttackIntervalTimeLeft => Mathf.Max(0f, patternIntervalTimer);
        public float AttackIntervalDuration => Mathf.Max(0.01f, patternIntervalDuration);

        public float WorldX => Root != null ? Root.transform.position.x : 0f;
        public float WorldZ => Root != null ? Root.transform.position.z : 0f;

        /// <summary>当前攻击是否处于"危险窗口"（命中判定有效）。</summary>
        public bool IsPatternDangerous => patternTimer > 0f && (patternDuration - patternTimer) >= patternDangerDelay;

        // 模式时间控制
        private float patternTimer;
        private float patternDuration;
        private float patternDangerDelay;
        private float patternIntervalTimer;
        private float patternIntervalDuration;
        private float vulnerableTimer;
        private float spawnTimer;
        private float laneWidth;
        private float baseGroundY;
        private float bobPhase;
        private float patternMinSeconds;
        private float patternMaxSeconds;
        private float dangerWindowRatio;
        private float telegraphLifetimeRatio;
        private float vulnerableChance;
        private float patternIntervalMin = 0.45f;
        private float patternIntervalMax = 0.9f;
        private float hitReactionTimer;
        private float breakReactionTimer;
        private float reactionWingFlap;
        private float reactionArmRecoil;
        private bool quickRecoverAfterHit;
        private BossPattern previousPattern = BossPattern.None;
        private float attackAnimPunch;
        private const float HitReactionDuration = 0.42f;
        private const float BreakReactionDuration = 0.62f;

        // 视觉子节点引用
        private readonly Transform body;
        private readonly Renderer bodyRenderer;
        private readonly List<Renderer> tintRenderers = new();
        // 记录每个 tint 渲染器的"原色"，受击/破绽期间在原色基础上叠加效果，避免颜色累积漂移
        private readonly List<Color> tintBaseColors = new();
        private readonly Transform leftArm;
        private readonly Transform rightArm;
        private readonly Transform[] healthCrystals = new Transform[BossSystem.BossMaxHits];
        private readonly List<GameObject> projectileObjects = new();
        private readonly List<TelegraphMarker> telegraphs = new();
        private readonly List<GameObject> attackFxObjects = new();
        private static readonly Dictionary<string, Texture2D> textureCache = new();
        private float attackFxTimer;

        private struct TelegraphMarker
        {
            public GameObject Go;
            public float Lifetime;
            public float MaxLifetime;
            public Vector3 BaseScale;
        }

        public BossEncounter(BossDefinition def, float baseGroundY, float laneWidth, float runDistance,
            float patternMinSeconds = 1.6f, float patternMaxSeconds = 2.4f,
            float dangerWindowRatio = 0.55f, float telegraphLifetimeRatio = 0.6f, float vulnerableChance = 0.5f)
        {
            Definition = def;
            this.baseGroundY = baseGroundY;
            this.laneWidth = laneWidth;
            this.patternMinSeconds = patternMinSeconds;
            this.patternMaxSeconds = patternMaxSeconds;
            this.dangerWindowRatio = Mathf.Clamp(dangerWindowRatio, 0.3f, 0.82f);
            this.telegraphLifetimeRatio = Mathf.Clamp(telegraphLifetimeRatio, 0.35f, 0.9f);
            this.vulnerableChance = Mathf.Clamp01(vulnerableChance);
            ConfigurePatternIntervalByStyle();
            Phase = BossPhase.Spawning;
            spawnTimer = 0.85f;

            Root = new GameObject($"Boss_{def.DisplayName}");
            var startSurface = TrackTerrain.SurfaceY(BossSystem.BossSpawnAheadZ, runDistance, baseGroundY);
            Root.transform.position = new Vector3(0f, startSurface, BossSystem.BossSpawnAheadZ);

            body = BuildSilhouette(def, out bodyRenderer, out leftArm, out rightArm);
            ApplyOpenTextures(def);

            // 缓存所有 tint 渲染器的初始颜色，作为 flash/破绽叠加的基准
            tintBaseColors.Clear();
            foreach (var r in tintRenderers)
            {
                tintBaseColors.Add(r != null ? RunnerVisuals.GetColor(r.material) : Color.white);
            }

            // 健康水晶（在 boss 上方），击中后变暗
            for (var i = 0; i < BossSystem.BossMaxHits; i++)
            {
                var crystal = AddPrim($"HP_{i}", PrimitiveType.Cube,
                    new Vector3((i - 1) * 0.5f, 4.4f, 0f),
                    new Vector3(0.32f, 0.42f, 0.32f),
                    def.GlowColor, true);
                crystal.transform.SetParent(Root.transform, false);
                crystal.transform.localPosition = new Vector3((i - 1) * 0.5f, 4.4f, 0f);
                crystal.transform.localRotation = Quaternion.Euler(0f, 45f, 0f);
                healthCrystals[i] = crystal.transform;
                tintRenderers.Add(crystal.GetComponent<Renderer>());
            }

            // 底部威吓光环
            var ring = AddPrim("BossRing", PrimitiveType.Cylinder,
                new Vector3(0f, 0.05f, 0f),
                new Vector3(3.4f, 0.06f, 3.4f),
                new Color(def.GlowColor.r, def.GlowColor.g, def.GlowColor.b, 0.45f), true);
            ring.transform.SetParent(Root.transform, false);

            // 出场震波
            CreateTelegraph(0f, 0.7f, 4f, def.GlowColor);
        }

        public void Dispose()
        {
            if (Root != null) Object.Destroy(Root);
            foreach (var p in projectileObjects)
                if (p != null) Object.Destroy(p);
            projectileObjects.Clear();
            foreach (var t in telegraphs)
                if (t.Go != null) Object.Destroy(t.Go);
            telegraphs.Clear();
            ClearAttackFx();
        }

        public void Tick(float dt, float runDistance, float surfaceBaseY)
        {
            if (Root == null) return;

            HitFlashTimer = Mathf.Max(0f, HitFlashTimer - dt);
            hitReactionTimer = Mathf.Max(0f, hitReactionTimer - dt);
            breakReactionTimer = Mathf.Max(0f, breakReactionTimer - dt);
            bobPhase += dt;
            attackAnimPunch = Mathf.Max(0f, attackAnimPunch - dt * 2.6f);
            if (Phase == BossPhase.Active && IsPatternDangerous)
            {
                attackAnimPunch = Mathf.Max(attackAnimPunch, 0.34f);
            }

            // boss 跟随路面前进（保持在玩家前方），落地高度跟随地形
            var holdZ = Phase == BossPhase.Retreating ? BossSystem.BossActiveZ + 4f : BossSystem.BossActiveZ;
            var pos = Root.transform.position;
            var targetZ = Mathf.Lerp(pos.z, holdZ, Mathf.Clamp01(dt * 3.2f));
            pos.z = targetZ;
            pos.y = TrackTerrain.SurfaceY(pos.z, runDistance, surfaceBaseY);
            Root.transform.position = pos;

            // 浮动 + 朝向玩家
            if (body != null)
            {
                var float01 = Mathf.Sin(bobPhase * 1.6f) * 0.12f;
                var attackForward = attackAnimPunch * 0.55f;
                var basePos = new Vector3(0f, 1.2f + float01, -attackForward);
                var baseRot = new Vector3(
                    attackAnimPunch * 10f,
                    180f + Mathf.Sin(bobPhase * 1.4f) * 6f,
                    Mathf.Sin(bobPhase * 1.1f) * 4f + attackAnimPunch * 6f);
                EvaluateReactionOffsets(out var reactionPos, out var reactionRot);
                body.localPosition = basePos + reactionPos;
                body.localRotation = Quaternion.Euler(baseRot + reactionRot);
            }

            UpdateAttackPose();

            UpdateTelegraphs(dt);
            UpdateProjectiles(dt);
            UpdateAttackFx(dt);
            UpdateHealthCrystals();
            UpdateBodyTint(dt);

            switch (Phase)
            {
                case BossPhase.Spawning:
                    spawnTimer -= dt;
                    if (spawnTimer <= 0f)
                    {
                        Phase = BossPhase.Active;
                        BeginAttackInterval();
                    }
                    break;

                case BossPhase.Active:
                    if (Pattern == BossPattern.None)
                    {
                        patternIntervalTimer -= dt;
                        if (patternIntervalTimer <= 0f)
                        {
                            StartNextPattern();
                        }
                        break;
                    }

                    patternTimer -= dt;
                    if (patternTimer <= 0f)
                    {
                        EndPattern();
                        // 一定概率进入破绽期
                        if (Random.value < vulnerableChance)
                        {
                            EnterVulnerable();
                        }
                        else
                        {
                            BeginAttackInterval();
                        }
                    }
                    break;

                case BossPhase.Vulnerable:
                    vulnerableTimer -= dt;
                    if (vulnerableTimer <= 0f)
                    {
                        Phase = BossPhase.Active;
                        BeginAttackInterval(quickRecoverAfterHit);
                        quickRecoverAfterHit = false;
                    }
                    break;

                case BossPhase.Retreating:
                    RetreatTimer -= dt;
                    if (Root != null)
                    {
                        var p = Root.transform.position;
                        p.y += dt * 4.2f; // 向上飞走
                        Root.transform.position = p;
                        Root.transform.Rotate(Vector3.up, 240f * dt, Space.World);
                    }
                    break;
            }
        }

        public void EnterAfterHit()
        {
            HitFlashTimer = BossSystem.HitInvulnerabilitySeconds;
            hitReactionTimer = HitReactionDuration;
            EndPattern();
            quickRecoverAfterHit = true;
            patternTimer = 0f;
            vulnerableTimer = 0f;
        }

        private void BeginAttackInterval(bool afterBreak = false)
        {
            EndPattern();
            var lo = afterBreak ? patternIntervalMin * 0.78f : patternIntervalMin;
            var hi = afterBreak ? patternIntervalMax * 0.86f : patternIntervalMax;
            patternIntervalDuration = Random.Range(lo, Mathf.Max(lo + 0.08f, hi));
            patternIntervalTimer = patternIntervalDuration;
        }

        private void StartNextPattern()
        {
            ClearTelegraphs();
            ClearAttackFx();
            Pattern = Definition.PatternPool[Random.Range(0, Definition.PatternPool.Length)];
            if (Definition.PatternPool.Length > 1 && Pattern == previousPattern)
            {
                var retry = 0;
                while (Pattern == previousPattern && retry < 4)
                {
                    Pattern = Definition.PatternPool[Random.Range(0, Definition.PatternPool.Length)];
                    retry++;
                }
            }
            previousPattern = Pattern;
            patternDuration = Random.Range(patternMinSeconds, patternMaxSeconds);
            patternTimer = patternDuration;
            attackFxTimer = patternDuration;
            patternDangerDelay = ComputePatternDangerDelay(patternDuration);

            switch (Pattern)
            {
                case BossPattern.SweepLow:
                {
                    CreateSweepSlashFx();
                    // 选一个安全车道（其余车道是危险）
                    var safeIdx = Random.Range(-1, 2);
                    SafeLaneX = safeIdx * laneWidth;
                    for (var i = -1; i <= 1; i++)
                    {
                        if (i == safeIdx) continue;
                        CreateTelegraph(i * laneWidth, PatternTelegraphLifetime(), 1.4f, new Color(1f, 0.4f, 0.2f, 0.7f));
                    }
                    break;
                }
                case BossPattern.DiveHigh:
                {
                    CreateDiveBurstFx();
                    // boss 选一个车道俯冲
                    var laneIdx = Random.Range(-1, 2);
                    var bp = Root.transform.position;
                    bp.x = laneIdx * laneWidth;
                    Root.transform.position = bp;
                    CreateTelegraph(laneIdx * laneWidth, PatternTelegraphLifetime(), 1.6f, new Color(1f, 0.55f, 0.3f, 0.8f));
                    break;
                }
                case BossPattern.ChargeAcross:
                {
                    CreateChargeTrailFx();
                    // 全场扫荡 → 三车道全部标记
                    for (var i = -1; i <= 1; i++)
                    {
                        CreateTelegraph(i * laneWidth, PatternTelegraphLifetime(), 1.4f, new Color(1f, 0.35f, 0.3f, 0.75f));
                    }
                    break;
                }
                case BossPattern.RangedSalvo:
                {
                    CreateRangedGlyphFx();
                    // 抛 2~3 枚投射物，命中随机车道
                    SalvoLaneX.Clear();
                    var n = Random.Range(2, 4);
                    var lanes = new[] { -1, 0, 1 };
                    Shuffle(lanes);
                    for (var k = 0; k < n; k++)
                    {
                        var lx = lanes[k] * laneWidth;
                        SalvoLaneX.Add(lx);
                        SpawnProjectile(lx, patternDuration);
                        CreateTelegraph(lx, PatternTelegraphLifetime(0.04f), 1.1f, new Color(1f, 0.7f, 0.3f, 0.7f));
                    }
                    break;
                }
                case BossPattern.CenterBeam:
                {
                    CreateCenterBeamFx();
                    // 中央光束：中心车道危险
                    CreateTelegraph(0f, PatternTelegraphLifetime(), 1.45f, new Color(1f, 0.3f, 0.65f, 0.8f));
                    break;
                }
                case BossPattern.QuakePulse:
                {
                    CreateQuakePulseFx();
                    // 震地脉冲：三车道全危险，但高度较低，鼓励滑铲
                    for (var i = -1; i <= 1; i++)
                    {
                        CreateTelegraph(i * laneWidth, PatternTelegraphLifetime(), 1.55f, new Color(1f, 0.78f, 0.2f, 0.72f));
                    }
                    break;
                }
            }
            attackAnimPunch = 1.15f;
        }

        private float PatternTelegraphLifetime(float bonus = 0f)
        {
            var byRatio = patternDuration * Mathf.Clamp01(telegraphLifetimeRatio + bonus);
            var byDanger = Mathf.Clamp(patternDangerDelay + 0.1f + bonus * 0.4f, 0.18f, patternDuration * 0.9f);
            return Mathf.Min(byRatio, byDanger);
        }

        private float ComputePatternDangerDelay(float duration)
        {
            var baseDelay = duration * (1f - Mathf.Clamp(dangerWindowRatio, 0.32f, 0.86f));
            var styleMul = 1f;
            switch (Definition.Silhouette)
            {
                case BossSilhouette.StormEagle:
                    styleMul = 0.76f;
                    break;
                case BossSilhouette.AuroraSerpent:
                    styleMul = 0.82f;
                    break;
                case BossSilhouette.SnowKing:
                    styleMul = 0.9f;
                    break;
                case BossSilhouette.CedarSentinel:
                    styleMul = 1.06f;
                    break;
            }

            return Mathf.Clamp(baseDelay * styleMul, 0.2f, 0.72f);
        }

        private void ConfigurePatternIntervalByStyle()
        {
            switch (Definition.Silhouette)
            {
                case BossSilhouette.StormEagle:
                    patternIntervalMin = 0.36f;
                    patternIntervalMax = 0.72f;
                    break;
                case BossSilhouette.AuroraSerpent:
                    patternIntervalMin = 0.4f;
                    patternIntervalMax = 0.78f;
                    break;
                case BossSilhouette.SnowKing:
                    patternIntervalMin = 0.52f;
                    patternIntervalMax = 0.96f;
                    break;
                case BossSilhouette.CedarSentinel:
                    patternIntervalMin = 0.58f;
                    patternIntervalMax = 1.05f;
                    break;
                default:
                    patternIntervalMin = 0.45f;
                    patternIntervalMax = 0.9f;
                    break;
            }
        }

        private void ApplyOpenTextures(BossDefinition def)
        {
            var texName = def.Silhouette switch
            {
                BossSilhouette.SnowKing => "tex_ice_surface",
                BossSilhouette.CedarSentinel => "tex_stone_surface",
                BossSilhouette.StormEagle => "tex_metal_surface",
                BossSilhouette.AuroraSerpent => "tex_fabric_surface",
                BossSilhouette.MistGuardian => "tex_fabric_surface",
                BossSilhouette.CoralKraken => "tex_stone_surface",
                _ => null,
            };
            if (string.IsNullOrEmpty(texName)) return;

            var texture = LoadTexture(texName);
            if (texture == null) return;

            ApplyTexture(bodyRenderer, texture, new Vector2(1.6f, 1.6f));
            if (leftArm != null) ApplyTexture(leftArm.GetComponent<Renderer>(), texture, new Vector2(2f, 1.2f));
            if (rightArm != null) ApplyTexture(rightArm.GetComponent<Renderer>(), texture, new Vector2(2f, 1.2f));
        }

        private static Texture2D LoadTexture(string name)
        {
            if (textureCache.TryGetValue(name, out var cached))
            {
                return cached;
            }

            var tex = Resources.Load<Texture2D>($"PenguinRun/Textures/{name}");
            textureCache[name] = tex;
            return tex;
        }

        private static void ApplyTexture(Renderer renderer, Texture2D texture, Vector2 tiling)
        {
            if (renderer == null || texture == null) return;
            var material = renderer.material;
            if (material == null) return;
            if (material.HasProperty("_BaseMap"))
            {
                material.SetTexture("_BaseMap", texture);
                material.SetTextureScale("_BaseMap", tiling);
            }
            if (material.HasProperty("_MainTex"))
            {
                material.SetTexture("_MainTex", texture);
                material.SetTextureScale("_MainTex", tiling);
            }
        }

        private void UpdateAttackPose()
        {
            if (leftArm == null || rightArm == null)
            {
                return;
            }

            // 不同Boss做动作风格分化：力度、速度、姿态各不相同
            var stylePower = 1f;
            var styleTempo = 1f;
            var styleBiasL = 0f;
            var styleBiasR = 0f;
            switch (Definition.Silhouette)
            {
                case BossSilhouette.SnowKing:
                    stylePower = 1.15f;
                    styleTempo = 0.9f;
                    break;
                case BossSilhouette.CedarSentinel:
                    stylePower = 1.22f;
                    styleTempo = 0.82f;
                    styleBiasL = -4f;
                    styleBiasR = 4f;
                    break;
                case BossSilhouette.AuroraSerpent:
                    stylePower = 0.88f;
                    styleTempo = 1.32f;
                    styleBiasL = 8f;
                    styleBiasR = -8f;
                    break;
                case BossSilhouette.MistGuardian:
                    stylePower = 0.96f;
                    styleTempo = 1.05f;
                    break;
                case BossSilhouette.CoralKraken:
                    stylePower = 1.08f;
                    styleTempo = 1.18f;
                    styleBiasL = -6f;
                    styleBiasR = 6f;
                    break;
                case BossSilhouette.StormEagle:
                    stylePower = 1.18f;
                    styleTempo = 1.42f;
                    styleBiasL = 10f;
                    styleBiasR = -10f;
                    break;
            }

            var idleL = Mathf.Sin(bobPhase * 2.2f) * 22f - 18f;
            var idleR = -Mathf.Sin(bobPhase * 2.2f) * 22f + 18f;
            var elapsed = Mathf.Max(0f, patternDuration - Mathf.Clamp(patternTimer, 0f, patternDuration));
            var windupSpan = Mathf.Max(0.12f, patternDangerDelay);
            var windup = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / windupSpan));
            var strike = IsPatternDangerous ? 1f : 0f;
            var dangerSnap = strike * (0.5f + Mathf.Sin(Time.time * 26f * styleTempo) * 0.5f);

            float attackL = 0f;
            float attackR = 0f;
            switch (Pattern)
            {
                case BossPattern.SweepLow:
                    attackL = Mathf.Lerp(-30f, 68f, windup) + strike * 18f * stylePower + dangerSnap * 6f;
                    attackR = Mathf.Lerp(30f, -68f, windup) - strike * 18f * stylePower - dangerSnap * 6f;
                    break;
                case BossPattern.DiveHigh:
                    attackL = Mathf.Lerp(-22f, 94f, windup) + strike * 10f * stylePower + dangerSnap * 9f;
                    attackR = Mathf.Lerp(22f, -94f, windup) - strike * 10f * stylePower - dangerSnap * 9f;
                    break;
                case BossPattern.ChargeAcross:
                    attackL = Mathf.Lerp(-15f, 42f, windup) + strike * 34f * stylePower + dangerSnap * 12f;
                    attackR = Mathf.Lerp(15f, -42f, windup) - strike * 34f * stylePower - dangerSnap * 12f;
                    break;
                case BossPattern.RangedSalvo:
                    attackL = Mathf.Lerp(-18f, 55f, windup) + Mathf.Sin(Time.time * 20f * styleTempo) * 8f + dangerSnap * 4f;
                    attackR = Mathf.Lerp(18f, -55f, windup) - Mathf.Sin(Time.time * 20f * styleTempo) * 8f - dangerSnap * 4f;
                    break;
                case BossPattern.CenterBeam:
                    attackL = Mathf.Lerp(-14f, 78f, windup) + strike * 22f * stylePower + dangerSnap * 8f;
                    attackR = Mathf.Lerp(14f, -78f, windup) - strike * 22f * stylePower - dangerSnap * 8f;
                    break;
                case BossPattern.QuakePulse:
                    attackL = Mathf.Lerp(-26f, 82f, windup) + strike * 16f * stylePower + dangerSnap * 7f;
                    attackR = Mathf.Lerp(26f, -82f, windup) - strike * 16f * stylePower - dangerSnap * 7f;
                    break;
                case BossPattern.Vulnerable:
                    attackL = -40f + Mathf.Sin(Time.time * 8f * styleTempo) * 5f;
                    attackR = 40f - Mathf.Sin(Time.time * 8f * styleTempo) * 5f;
                    break;
            }

            var targetL = Mathf.Lerp(idleL, attackL, windup) + styleBiasL;
            var targetR = Mathf.Lerp(idleR, attackR, windup) + styleBiasR;
            var flap = Mathf.Sin(Time.time * 40f) * reactionWingFlap;
            targetL += flap + reactionArmRecoil;
            targetR -= flap - reactionArmRecoil;
            leftArm.localRotation = Quaternion.Slerp(leftArm.localRotation, Quaternion.Euler(0f, 0f, targetL), Time.deltaTime * (14f + styleTempo * 4f));
            rightArm.localRotation = Quaternion.Slerp(rightArm.localRotation, Quaternion.Euler(0f, 0f, targetR), Time.deltaTime * (14f + styleTempo * 4f));
        }

        private void EndPattern()
        {
            Pattern = BossPattern.None;
            SalvoLaneX.Clear();
            ClearTelegraphs();
            ClearAttackFx();
        }

        private void EnterVulnerable()
        {
            Phase = BossPhase.Vulnerable;
            Pattern = BossPattern.Vulnerable;
            vulnerableTimer = BossSystem.VulnerableSeconds;
            breakReactionTimer = BreakReactionDuration;
            ClearAttackFx();
            // 显示绿色破绽提示（在 boss 自身位置）
            CreateTelegraph(WorldX, BossSystem.VulnerableSeconds, 2.4f, new Color(0.4f, 1f, 0.55f, 0.85f));
        }

        private void EvaluateReactionOffsets(out Vector3 positionOffset, out Vector3 rotationOffset)
        {
            positionOffset = Vector3.zero;
            rotationOffset = Vector3.zero;
            reactionWingFlap = 0f;
            reactionArmRecoil = 0f;

            var hitPulse = EvaluateReactionPulse(hitReactionTimer, HitReactionDuration);
            var breakPulse = EvaluateReactionPulse(breakReactionTimer, BreakReactionDuration);
            if (hitPulse <= 0f && breakPulse <= 0f)
            {
                return;
            }

            switch (Definition.Silhouette)
            {
                // 雪王：受击后仰，破防时更明显地抬头后撤
                case BossSilhouette.SnowKing:
                    positionOffset += new Vector3(0f, 0f, hitPulse * 0.48f + breakPulse * 0.36f);
                    rotationOffset += new Vector3(-(hitPulse * 19f + breakPulse * 13f), 0f, Mathf.Sin(Time.time * 24f) * hitPulse * 3f);
                    reactionArmRecoil = hitPulse * 9f + breakPulse * 6f;
                    break;
                // 长蛇：受击扭身摆尾，破防时左右甩动更夸张
                case BossSilhouette.AuroraSerpent:
                    positionOffset += new Vector3(Mathf.Sin(Time.time * 10f) * (hitPulse * 0.16f + breakPulse * 0.2f), 0f, hitPulse * 0.1f);
                    rotationOffset += new Vector3(
                        0f,
                        Mathf.Sin(Time.time * 19f) * (hitPulse * 16f + breakPulse * 22f),
                        Mathf.Sin(Time.time * 25f) * (hitPulse * 10f + breakPulse * 15f));
                    reactionArmRecoil = hitPulse * 4f;
                    break;
                // 苍鹰：受击抖翼后退，破防时出现短促振翅
                case BossSilhouette.StormEagle:
                    positionOffset += new Vector3(0f, hitPulse * 0.06f, hitPulse * 0.28f + breakPulse * 0.48f);
                    rotationOffset += new Vector3(-(hitPulse * 9f + breakPulse * 15f), 0f, Mathf.Sin(Time.time * 28f) * hitPulse * 5f);
                    reactionWingFlap = hitPulse * 22f + breakPulse * 34f;
                    reactionArmRecoil = hitPulse * 6f + breakPulse * 4f;
                    break;
                // 其他 boss 维持较轻微的统一反馈
                default:
                    positionOffset += new Vector3(0f, 0f, hitPulse * 0.2f + breakPulse * 0.22f);
                    rotationOffset += new Vector3(-(hitPulse * 8f + breakPulse * 9f), Mathf.Sin(Time.time * 16f) * breakPulse * 6f, 0f);
                    reactionArmRecoil = hitPulse * 4f + breakPulse * 3f;
                    break;
            }
        }

        private static float EvaluateReactionPulse(float timer, float duration)
        {
            if (timer <= 0f || duration <= 0f) return 0f;
            var progress = 1f - Mathf.Clamp01(timer / duration);
            return Mathf.Sin(progress * Mathf.PI);
        }

        // ── 视觉构建 ─────────────────────────────────────────

        private Transform BuildSilhouette(BossDefinition def, out Renderer mainRenderer, out Transform armL, out Transform armR)
        {
            var bodyGo = new GameObject("BossBody");
            bodyGo.transform.SetParent(Root.transform, false);
            bodyGo.transform.localPosition = new Vector3(0f, 1.2f, 0f);

            switch (def.Silhouette)
            {
                case BossSilhouette.SnowKing:
                    BuildSnowKing(bodyGo.transform, def, out mainRenderer, out armL, out armR);
                    break;
                case BossSilhouette.CedarSentinel:
                    BuildCedarSentinel(bodyGo.transform, def, out mainRenderer, out armL, out armR);
                    break;
                case BossSilhouette.AuroraSerpent:
                    BuildAuroraSerpent(bodyGo.transform, def, out mainRenderer, out armL, out armR);
                    break;
                case BossSilhouette.MistGuardian:
                    BuildMistGuardian(bodyGo.transform, def, out mainRenderer, out armL, out armR);
                    break;
                case BossSilhouette.CoralKraken:
                    BuildCoralKraken(bodyGo.transform, def, out mainRenderer, out armL, out armR);
                    break;
                case BossSilhouette.StormEagle:
                    BuildStormEagle(bodyGo.transform, def, out mainRenderer, out armL, out armR);
                    break;
                default:
                    BuildSnowKing(bodyGo.transform, def, out mainRenderer, out armL, out armR);
                    break;
            }
            return bodyGo.transform;
        }

        private void BuildSnowKing(Transform root, BossDefinition def, out Renderer mainRenderer, out Transform armL, out Transform armR)
        {
            // 大号企鹅 boss：王冠 + 冰锤 + 厚雪披风
            var torso = AddPrim("Torso", PrimitiveType.Capsule, new Vector3(0f, 0.3f, 0f),
                new Vector3(2.2f, 1.6f, 1.8f), def.BodyColor);
            torso.transform.SetParent(root, false);
            mainRenderer = torso.GetComponent<Renderer>();
            tintRenderers.Add(mainRenderer);

            var belly = AddPrim("Belly", PrimitiveType.Sphere, new Vector3(0f, 0.05f, -0.65f),
                new Vector3(1.65f, 1.4f, 0.4f), new Color(0.92f, 0.97f, 1f));
            belly.transform.SetParent(root, false);

            var headGo = AddPrim("Head", PrimitiveType.Sphere, new Vector3(0f, 1.6f, 0f),
                new Vector3(1.6f, 1.4f, 1.5f), def.BodyColor);
            headGo.transform.SetParent(root, false);
            tintRenderers.Add(headGo.GetComponent<Renderer>());

            // 王冠
            var crown = AddPrim("Crown", PrimitiveType.Cylinder, new Vector3(0f, 2.5f, 0f),
                new Vector3(1.55f, 0.42f, 1.55f), def.TrimColor);
            crown.transform.SetParent(root, false);
            for (var i = 0; i < 5; i++)
            {
                var ang = i * 72f * Mathf.Deg2Rad;
                var tip = AddPrim($"CrownTip{i}", PrimitiveType.Cube,
                    new Vector3(Mathf.Cos(ang) * 0.78f, 2.85f, Mathf.Sin(ang) * 0.78f),
                    new Vector3(0.16f, 0.46f, 0.16f), def.GlowColor, true);
                tip.transform.SetParent(root, false);
            }

            // 眼睛（红色凶恶）
            AddPrimChild(root, "EyeL", PrimitiveType.Sphere, new Vector3(-0.4f, 1.7f, -0.78f), Vector3.one * 0.22f, new Color(1f, 0.25f, 0.2f));
            AddPrimChild(root, "EyeR", PrimitiveType.Sphere, new Vector3(0.4f, 1.7f, -0.78f), Vector3.one * 0.22f, new Color(1f, 0.25f, 0.2f));

            // 喙
            AddPrimChild(root, "Beak", PrimitiveType.Cube, new Vector3(0f, 1.45f, -0.94f), new Vector3(0.55f, 0.22f, 0.45f), new Color(1f, 0.55f, 0.18f));

            // 双臂（持冰锤）
            armL = AddPrimChild(root, "ArmL", PrimitiveType.Capsule, new Vector3(-1.4f, 0.5f, 0f), new Vector3(0.5f, 1.2f, 0.5f), def.BodyColor).transform;
            armR = AddPrimChild(root, "ArmR", PrimitiveType.Capsule, new Vector3(1.4f, 0.5f, 0f), new Vector3(0.5f, 1.2f, 0.5f), def.BodyColor).transform;
            AddPrimChild(armL, "HammerL", PrimitiveType.Cube, new Vector3(0f, -0.85f, 0f), new Vector3(0.85f, 0.55f, 0.85f), new Color(0.7f, 0.85f, 1f));
            AddPrimChild(armR, "HammerR", PrimitiveType.Cube, new Vector3(0f, -0.85f, 0f), new Vector3(0.85f, 0.55f, 0.85f), new Color(0.7f, 0.85f, 1f));
            tintRenderers.Add(armL.GetComponent<Renderer>());
            tintRenderers.Add(armR.GetComponent<Renderer>());
        }

        private void BuildCedarSentinel(Transform root, BossDefinition def, out Renderer mainRenderer, out Transform armL, out Transform armR)
        {
            var torso = AddPrimChild(root, "Torso", PrimitiveType.Cube, new Vector3(0f, 0.3f, 0f), new Vector3(2.2f, 1.8f, 1.5f), def.BodyColor);
            mainRenderer = torso.GetComponent<Renderer>();
            tintRenderers.Add(mainRenderer);
            AddPrimChild(root, "Plate", PrimitiveType.Cube, new Vector3(0f, 0.3f, -0.78f), new Vector3(1.95f, 1.65f, 0.18f), def.TrimColor);
            AddPrimChild(root, "BarkSpine", PrimitiveType.Cube, new Vector3(0f, 1.2f, 0.9f), new Vector3(0.34f, 1.45f, 0.3f), new Color(0.35f, 0.24f, 0.14f));
            var headGo = AddPrimChild(root, "Head", PrimitiveType.Cube, new Vector3(0f, 1.7f, 0f), new Vector3(1.4f, 1.0f, 1.3f), def.BodyColor);
            tintRenderers.Add(headGo.GetComponent<Renderer>());
            AddPrimChild(root, "EyeL", PrimitiveType.Sphere, new Vector3(-0.35f, 1.75f, -0.7f), Vector3.one * 0.18f, def.GlowColor);
            AddPrimChild(root, "EyeR", PrimitiveType.Sphere, new Vector3(0.35f, 1.75f, -0.7f), Vector3.one * 0.18f, def.GlowColor);
            AddPrimChild(root, "Antler", PrimitiveType.Cube, new Vector3(0f, 2.4f, 0f), new Vector3(2f, 0.18f, 0.18f), def.TrimColor);
            AddPrimChild(root, "ShoulderLogL", PrimitiveType.Cylinder, new Vector3(-1.08f, 1.2f, 0.2f), new Vector3(0.28f, 0.85f, 0.28f), new Color(0.31f, 0.22f, 0.12f));
            AddPrimChild(root, "ShoulderLogR", PrimitiveType.Cylinder, new Vector3(1.08f, 1.2f, 0.2f), new Vector3(0.28f, 0.85f, 0.28f), new Color(0.31f, 0.22f, 0.12f));

            armL = AddPrimChild(root, "ArmL", PrimitiveType.Cube, new Vector3(-1.5f, 0.4f, 0f), new Vector3(0.55f, 1.4f, 0.55f), def.BodyColor).transform;
            armR = AddPrimChild(root, "ArmR", PrimitiveType.Cube, new Vector3(1.5f, 0.4f, 0f), new Vector3(0.55f, 1.4f, 0.55f), def.BodyColor).transform;
            AddPrimChild(armL, "ClubL", PrimitiveType.Cube, new Vector3(0f, -0.95f, 0f), new Vector3(0.7f, 0.55f, 0.7f), def.TrimColor);
            AddPrimChild(armR, "ClubR", PrimitiveType.Cube, new Vector3(0f, -0.95f, 0f), new Vector3(0.7f, 0.55f, 0.7f), def.TrimColor);
            tintRenderers.Add(armL.GetComponent<Renderer>());
            tintRenderers.Add(armR.GetComponent<Renderer>());
        }

        private void BuildAuroraSerpent(Transform root, BossDefinition def, out Renderer mainRenderer, out Transform armL, out Transform armR)
        {
            // 蛇身：多节连续
            var torso = AddPrimChild(root, "Torso", PrimitiveType.Capsule, new Vector3(0f, 0.5f, 0f), new Vector3(1.6f, 2.4f, 1.6f), def.BodyColor);
            torso.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            mainRenderer = torso.GetComponent<Renderer>();
            tintRenderers.Add(mainRenderer);

            for (var i = 0; i < 5; i++)
            {
                var seg = AddPrimChild(root, $"Seg{i}", PrimitiveType.Sphere,
                    new Vector3(Mathf.Sin(i * 0.7f) * 0.4f, 0.6f - i * 0.05f, -1f - i * 0.7f),
                    new Vector3(1.2f - i * 0.13f, 1.0f - i * 0.1f, 1.2f - i * 0.13f),
                    Color.Lerp(def.BodyColor, def.GlowColor, i * 0.15f));
                tintRenderers.Add(seg.GetComponent<Renderer>());
            }

            var headGo = AddPrimChild(root, "Head", PrimitiveType.Cube, new Vector3(0f, 1.5f, 0.4f), new Vector3(1.3f, 0.9f, 1.6f), def.BodyColor);
            tintRenderers.Add(headGo.GetComponent<Renderer>());
            AddPrimChild(root, "EyeL", PrimitiveType.Sphere, new Vector3(-0.4f, 1.55f, -0.4f), Vector3.one * 0.2f, def.TrimColor);
            AddPrimChild(root, "EyeR", PrimitiveType.Sphere, new Vector3(0.4f, 1.55f, -0.4f), Vector3.one * 0.2f, def.TrimColor);

            // "翅膀"代替手臂
            armL = AddPrimChild(root, "WingL", PrimitiveType.Cube, new Vector3(-1.2f, 0.8f, 0f), new Vector3(1.4f, 0.18f, 0.85f), def.GlowColor, true).transform;
            armR = AddPrimChild(root, "WingR", PrimitiveType.Cube, new Vector3(1.2f, 0.8f, 0f), new Vector3(1.4f, 0.18f, 0.85f), def.GlowColor, true).transform;
        }

        private void BuildMistGuardian(Transform root, BossDefinition def, out Renderer mainRenderer, out Transform armL, out Transform armR)
        {
            var torso = AddPrimChild(root, "MistBody", PrimitiveType.Sphere, new Vector3(0f, 0.6f, 0f), new Vector3(2.4f, 2.2f, 2f), def.BodyColor);
            mainRenderer = torso.GetComponent<Renderer>();
            tintRenderers.Add(mainRenderer);
            AddPrimChild(root, "MistCore", PrimitiveType.Sphere, new Vector3(0f, 1.05f, -0.15f), Vector3.one * 0.65f, new Color(def.GlowColor.r, def.GlowColor.g, def.GlowColor.b, 0.55f), true);
            // 雾气环
            for (var i = 0; i < 4; i++)
            {
                var ring = AddPrimChild(root, $"Veil{i}", PrimitiveType.Cylinder,
                    new Vector3(0f, 0.3f + i * 0.5f, 0f),
                    new Vector3(2.5f + i * 0.18f, 0.06f, 2.5f + i * 0.18f),
                    new Color(def.TrimColor.r, def.TrimColor.g, def.TrimColor.b, 0.32f - i * 0.05f), true);
            }
            for (var i = 0; i < 6; i++)
            {
                var ang = i * 60f * Mathf.Deg2Rad;
                AddPrimChild(root, $"MistShard{i}", PrimitiveType.Cube,
                    new Vector3(Mathf.Cos(ang) * 1.35f, 1.2f, Mathf.Sin(ang) * 1.35f),
                    new Vector3(0.16f, 0.55f, 0.16f),
                    new Color(def.TrimColor.r, def.TrimColor.g, def.TrimColor.b, 0.42f), true);
            }
            AddPrimChild(root, "EyeL", PrimitiveType.Sphere, new Vector3(-0.55f, 1.5f, -0.85f), Vector3.one * 0.28f, def.GlowColor);
            AddPrimChild(root, "EyeR", PrimitiveType.Sphere, new Vector3(0.55f, 1.5f, -0.85f), Vector3.one * 0.28f, def.GlowColor);

            armL = AddPrimChild(root, "TendrilL", PrimitiveType.Capsule, new Vector3(-1.6f, 0.4f, 0.2f), new Vector3(0.4f, 1.4f, 0.4f), def.BodyColor).transform;
            armR = AddPrimChild(root, "TendrilR", PrimitiveType.Capsule, new Vector3(1.6f, 0.4f, 0.2f), new Vector3(0.4f, 1.4f, 0.4f), def.BodyColor).transform;
            tintRenderers.Add(armL.GetComponent<Renderer>());
            tintRenderers.Add(armR.GetComponent<Renderer>());
        }

        private void BuildCoralKraken(Transform root, BossDefinition def, out Renderer mainRenderer, out Transform armL, out Transform armR)
        {
            var torso = AddPrimChild(root, "KrakenBody", PrimitiveType.Sphere, new Vector3(0f, 0.6f, 0f), new Vector3(2.6f, 2.2f, 2.2f), def.BodyColor);
            mainRenderer = torso.GetComponent<Renderer>();
            tintRenderers.Add(mainRenderer);
            AddPrimChild(root, "CoralCrown", PrimitiveType.Cylinder, new Vector3(0f, 2.1f, 0f), new Vector3(1.7f, 0.16f, 1.7f), new Color(1f, 0.78f, 0.5f, 0.45f), true);

            // 大眼睛
            AddPrimChild(root, "BigEyeL", PrimitiveType.Sphere, new Vector3(-0.55f, 1.4f, -0.95f), Vector3.one * 0.42f, new Color(1f, 0.95f, 0.5f));
            AddPrimChild(root, "BigEyeR", PrimitiveType.Sphere, new Vector3(0.55f, 1.4f, -0.95f), Vector3.one * 0.42f, new Color(1f, 0.95f, 0.5f));
            AddPrimChild(root, "PupilL", PrimitiveType.Sphere, new Vector3(-0.55f, 1.4f, -1.1f), Vector3.one * 0.18f, Color.black);
            AddPrimChild(root, "PupilR", PrimitiveType.Sphere, new Vector3(0.55f, 1.4f, -1.1f), Vector3.one * 0.18f, Color.black);

            // 6 条触手
            for (var i = 0; i < 6; i++)
            {
                var ang = i * 60f * Mathf.Deg2Rad;
                var tx = Mathf.Cos(ang) * 1.3f;
                var tz = Mathf.Sin(ang) * 1.3f;
                var tent = AddPrimChild(root, $"Tentacle{i}", PrimitiveType.Cylinder,
                    new Vector3(tx, -0.4f, tz),
                    new Vector3(0.32f, 1.4f, 0.32f),
                    Color.Lerp(def.BodyColor, def.TrimColor, 0.4f));
                tent.transform.localRotation = Quaternion.Euler(35f, ang * Mathf.Rad2Deg, 0f);
            }
            AddPrimChild(root, "FinL", PrimitiveType.Cube, new Vector3(-1.35f, 0.9f, -0.1f), new Vector3(0.28f, 0.9f, 1.15f), def.TrimColor);
            AddPrimChild(root, "FinR", PrimitiveType.Cube, new Vector3(1.35f, 0.9f, -0.1f), new Vector3(0.28f, 0.9f, 1.15f), def.TrimColor);

            armL = AddPrimChild(root, "ArmL", PrimitiveType.Capsule, new Vector3(-1.6f, 0.6f, 0.1f), new Vector3(0.55f, 1.4f, 0.55f), def.TrimColor).transform;
            armR = AddPrimChild(root, "ArmR", PrimitiveType.Capsule, new Vector3(1.6f, 0.6f, 0.1f), new Vector3(0.55f, 1.4f, 0.55f), def.TrimColor).transform;
        }

        private void BuildStormEagle(Transform root, BossDefinition def, out Renderer mainRenderer, out Transform armL, out Transform armR)
        {
            var torso = AddPrimChild(root, "EagleBody", PrimitiveType.Capsule, new Vector3(0f, 0.4f, 0f), new Vector3(1.6f, 1.8f, 1.6f), def.BodyColor);
            mainRenderer = torso.GetComponent<Renderer>();
            tintRenderers.Add(mainRenderer);

            var headGo = AddPrimChild(root, "EagleHead", PrimitiveType.Sphere, new Vector3(0f, 1.6f, -0.4f), new Vector3(1.1f, 1.0f, 1.2f), def.BodyColor);
            tintRenderers.Add(headGo.GetComponent<Renderer>());
            AddPrimChild(root, "Beak", PrimitiveType.Cube, new Vector3(0f, 1.45f, -1.2f), new Vector3(0.5f, 0.32f, 0.6f), def.TrimColor);
            AddPrimChild(root, "EyeL", PrimitiveType.Sphere, new Vector3(-0.35f, 1.75f, -0.95f), Vector3.one * 0.16f, new Color(1f, 0.4f, 0.2f));
            AddPrimChild(root, "EyeR", PrimitiveType.Sphere, new Vector3(0.35f, 1.75f, -0.95f), Vector3.one * 0.16f, new Color(1f, 0.4f, 0.2f));

            // 大翅膀
            armL = AddPrimChild(root, "WingL", PrimitiveType.Cube, new Vector3(-1.7f, 0.7f, 0f), new Vector3(2.4f, 0.18f, 1.4f), def.BodyColor).transform;
            armR = AddPrimChild(root, "WingR", PrimitiveType.Cube, new Vector3(1.7f, 0.7f, 0f), new Vector3(2.4f, 0.18f, 1.4f), def.BodyColor).transform;
            AddPrimChild(armL, "WingTipL", PrimitiveType.Cube, new Vector3(-1f, 0f, 0f), new Vector3(0.8f, 0.16f, 0.8f), def.TrimColor);
            AddPrimChild(armR, "WingTipR", PrimitiveType.Cube, new Vector3(1f, 0f, 0f), new Vector3(0.8f, 0.16f, 0.8f), def.TrimColor);
            // 闪电纹路
            AddPrimChild(armL, "BoltL", PrimitiveType.Cube, new Vector3(0f, -0.2f, 0f), new Vector3(2.2f, 0.06f, 0.1f), new Color(1f, 0.95f, 0.4f, 0.85f), true);
            AddPrimChild(armR, "BoltR", PrimitiveType.Cube, new Vector3(0f, -0.2f, 0f), new Vector3(2.2f, 0.06f, 0.1f), new Color(1f, 0.95f, 0.4f, 0.85f), true);
        }

        // ── 投射物 / 危险区指示器 ─────────────────────────────

        private void SpawnProjectile(float worldX, float lifetime)
        {
            var bossPos = Root.transform.position;
            var go = AddPrim("Projectile", PrimitiveType.Sphere,
                new Vector3(0f, 1.6f, 0f), Vector3.one * 0.55f,
                new Color(Definition.GlowColor.r, Definition.GlowColor.g, Definition.GlowColor.b, 0.95f));
            go.transform.SetParent(Root.transform.parent, false);
            go.transform.position = new Vector3(bossPos.x, bossPos.y + 1.6f, bossPos.z);

            var anim = go.AddComponent<BossProjectileAnim>();
            anim.Init(go.transform, new Vector3(worldX, 0f, 0f), lifetime);
            projectileObjects.Add(go);
        }

        private void CreateTelegraph(float worldX, float lifetime, float radius, Color color)
        {
            var bossPos = Root.transform.position;
            var go = AddPrim("Telegraph", PrimitiveType.Cylinder,
                new Vector3(0f, 0.05f, 0f),
                new Vector3(radius, 0.05f, radius),
                color, true);
            go.transform.SetParent(Root.transform.parent, false);
            // 危险区显示在 boss 与玩家之间靠玩家一侧
            go.transform.position = new Vector3(worldX, bossPos.y + 0.06f, bossPos.z * 0.4f);
            telegraphs.Add(new TelegraphMarker
            {
                Go = go,
                Lifetime = lifetime,
                MaxLifetime = lifetime,
                BaseScale = new Vector3(radius, 0.05f, radius),
            });
        }

        private void ClearTelegraphs()
        {
            for (var i = telegraphs.Count - 1; i >= 0; i--)
            {
                if (telegraphs[i].Go != null) Object.Destroy(telegraphs[i].Go);
            }
            telegraphs.Clear();
        }

        private void UpdateTelegraphs(float dt)
        {
            for (var i = telegraphs.Count - 1; i >= 0; i--)
            {
                var t = telegraphs[i];
                if (t.Go == null)
                {
                    telegraphs.RemoveAt(i);
                    continue;
                }
                t.Lifetime -= dt;
                if (t.Lifetime <= 0f)
                {
                    Object.Destroy(t.Go);
                    telegraphs.RemoveAt(i);
                    continue;
                }
                // 闪烁脉冲
                var pulse = 1f + Mathf.Sin(Time.time * 22f) * 0.18f;
                t.Go.transform.localScale = t.BaseScale * pulse;
                telegraphs[i] = t;
            }
        }

        private void UpdateProjectiles(float dt)
        {
            for (var i = projectileObjects.Count - 1; i >= 0; i--)
            {
                if (projectileObjects[i] == null)
                {
                    projectileObjects.RemoveAt(i);
                }
            }
        }

        private void CreateSweepSlashFx()
        {
            var color = new Color(Definition.GlowColor.r, Definition.GlowColor.g, Definition.GlowColor.b, 0.46f);
            var slashL = AddPrim("SweepSlashL", PrimitiveType.Cube,
                new Vector3(-1.1f, 0.9f, -0.95f),
                new Vector3(0.28f, 0.12f, 2.4f),
                color, true);
            slashL.transform.SetParent(Root.transform, false);
            attackFxObjects.Add(slashL);

            var slashR = AddPrim("SweepSlashR", PrimitiveType.Cube,
                new Vector3(1.1f, 0.9f, -0.95f),
                new Vector3(0.28f, 0.12f, 2.4f),
                color, true);
            slashR.transform.SetParent(Root.transform, false);
            attackFxObjects.Add(slashR);
        }

        private void CreateDiveBurstFx()
        {
            var burstRing = AddPrim("DiveBurstRing", PrimitiveType.Cylinder,
                new Vector3(0f, 0.3f, -0.2f),
                new Vector3(1f, 0.04f, 1f),
                new Color(Definition.TrimColor.r, Definition.TrimColor.g, Definition.TrimColor.b, 0.42f), true);
            burstRing.transform.SetParent(Root.transform, false);
            attackFxObjects.Add(burstRing);

            var cone = AddPrim("DiveBurstCone", PrimitiveType.Capsule,
                new Vector3(0f, 1.6f, -0.6f),
                new Vector3(0.36f, 1.45f, 0.36f),
                new Color(Definition.GlowColor.r, Definition.GlowColor.g, Definition.GlowColor.b, 0.38f), true);
            cone.transform.SetParent(Root.transform, false);
            cone.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            attackFxObjects.Add(cone);
        }

        private void CreateChargeTrailFx()
        {
            for (var i = 0; i < 3; i++)
            {
                var laneX = (i - 1) * laneWidth;
                var trail = AddPrim($"ChargeTrail{i}", PrimitiveType.Cylinder,
                    new Vector3(laneX, 0.07f, -1.5f),
                    new Vector3(0.78f, 0.04f, 0.78f),
                    new Color(Definition.GlowColor.r, Definition.GlowColor.g, Definition.GlowColor.b, 0.32f), true);
                trail.transform.SetParent(Root.transform, false);
                attackFxObjects.Add(trail);
            }
        }

        private void CreateCenterBeamFx()
        {
            var beam = AddPrim("CenterBeamFx", PrimitiveType.Cylinder,
                new Vector3(0f, 2.5f, 0f),
                new Vector3(0.28f, 3f, 0.28f),
                new Color(Definition.GlowColor.r, Mathf.Clamp01(Definition.GlowColor.g * 0.8f), 1f, 0.4f), true);
            beam.transform.SetParent(Root.transform, false);
            attackFxObjects.Add(beam);

            var core = AddPrim("BeamCoreFx", PrimitiveType.Sphere,
                new Vector3(0f, 1.6f, -0.2f),
                Vector3.one * 0.45f,
                new Color(1f, 0.55f, 0.9f, 0.55f), true);
            core.transform.SetParent(Root.transform, false);
            attackFxObjects.Add(core);
        }

        private void CreateQuakePulseFx()
        {
            var ringA = AddPrim("QuakeRingA", PrimitiveType.Cylinder,
                new Vector3(0f, 0.06f, 0f),
                new Vector3(2.1f, 0.04f, 2.1f),
                new Color(1f, 0.8f, 0.28f, 0.48f), true);
            ringA.transform.SetParent(Root.transform, false);
            attackFxObjects.Add(ringA);

            var ringB = AddPrim("QuakeRingB", PrimitiveType.Cylinder,
                new Vector3(0f, 0.05f, 0f),
                new Vector3(1.3f, 0.03f, 1.3f),
                new Color(1f, 0.62f, 0.24f, 0.42f), true);
            ringB.transform.SetParent(Root.transform, false);
            attackFxObjects.Add(ringB);
        }

        private void CreateRangedGlyphFx()
        {
            var ring = AddPrim("SalvoGlyphRing", PrimitiveType.Cylinder,
                new Vector3(0f, 0.3f, 0f),
                new Vector3(1.5f, 0.03f, 1.5f),
                new Color(1f, 0.72f, 0.35f, 0.45f), true);
            ring.transform.SetParent(Root.transform, false);
            attackFxObjects.Add(ring);

            for (var i = 0; i < 3; i++)
            {
                var ang = i * 120f * Mathf.Deg2Rad;
                var orb = AddPrim($"SalvoOrb{i}", PrimitiveType.Sphere,
                    new Vector3(Mathf.Cos(ang) * 0.9f, 1.5f, Mathf.Sin(ang) * 0.9f),
                    Vector3.one * 0.24f,
                    new Color(1f, 0.78f, 0.4f, 0.72f), true);
                orb.transform.SetParent(Root.transform, false);
                attackFxObjects.Add(orb);
            }
        }

        private void UpdateAttackFx(float dt)
        {
            if (attackFxObjects.Count == 0) return;
            attackFxTimer = Mathf.Max(0f, attackFxTimer - dt);
            var life01 = patternDuration > 0.01f ? Mathf.Clamp01(attackFxTimer / patternDuration) : 0f;

            for (var i = attackFxObjects.Count - 1; i >= 0; i--)
            {
                var fx = attackFxObjects[i];
                if (fx == null)
                {
                    attackFxObjects.RemoveAt(i);
                    continue;
                }

                switch (Pattern)
                {
                    case BossPattern.SweepLow:
                    {
                        var slashSpeed = Definition.Silhouette == BossSilhouette.SnowKing ? 1.25f : 1f;
                        var dir = fx.name.Contains("L") ? 1f : -1f;
                        var sweep = Mathf.Sin(Time.time * 24f * slashSpeed + dir) * 0.42f;
                        fx.transform.localPosition = new Vector3(dir * (0.95f + sweep), 0.88f + Mathf.Sin(Time.time * 12f) * 0.05f, -1.05f);
                        fx.transform.localRotation = Quaternion.Euler(0f, 0f, dir * (35f + sweep * 30f));
                        break;
                    }
                    case BossPattern.DiveHigh:
                    {
                        if (fx.name.Contains("Ring"))
                        {
                            var expand = 1f + (1f - life01) * 2.2f;
                            fx.transform.localScale = new Vector3(1f, 0.04f, 1f) * expand;
                            fx.transform.Rotate(Vector3.up, 200f * dt, Space.Self);
                        }
                        else
                        {
                            var pitch = Mathf.Lerp(90f, 55f, 1f - life01);
                            fx.transform.localRotation = Quaternion.Euler(pitch, 0f, 0f);
                            fx.transform.localScale = new Vector3(0.36f, 1.15f + (1f - life01) * 0.8f, 0.36f);
                        }
                        break;
                    }
                    case BossPattern.ChargeAcross:
                    {
                        var pulse = 0.8f + Mathf.Sin(Time.time * 28f + i * 1.3f) * 0.2f;
                        var expand = 1f + (1f - life01) * 1.25f;
                        fx.transform.localScale = new Vector3(0.78f, 0.04f, 0.78f) * (pulse * expand);
                        fx.transform.localPosition = new Vector3(fx.transform.localPosition.x, 0.07f, -1.8f + (1f - life01) * 0.9f);
                        break;
                    }
                    case BossPattern.CenterBeam:
                    {
                        var spinMul = Definition.Silhouette == BossSilhouette.StormEagle ? 1.55f : 1f;
                        if (fx.name.Contains("CenterBeamFx"))
                        {
                            var pulse = 1f + Mathf.Sin(Time.time * 20f) * 0.15f;
                            fx.transform.localScale = new Vector3(0.28f * pulse, 2.2f + (1f - life01) * 2.8f, 0.28f * pulse);
                        }
                        else
                        {
                            fx.transform.Rotate(Vector3.up, 220f * spinMul * dt, Space.Self);
                            fx.transform.localScale = Vector3.one * (0.35f + Mathf.Sin(Time.time * 18f) * 0.1f);
                        }
                        break;
                    }
                    case BossPattern.QuakePulse:
                    {
                        var quakeMul = Definition.Silhouette == BossSilhouette.CedarSentinel ? 1.35f : 1f;
                        var expand = 1f + (1f - life01) * 1.7f;
                        fx.transform.localScale = new Vector3(fx.name.Contains("B") ? 1.3f : 2.1f, fx.transform.localScale.y, fx.name.Contains("B") ? 1.3f : 2.1f) * expand;
                        fx.transform.Rotate(Vector3.up, (fx.name.Contains("B") ? -140f : 180f) * quakeMul * dt, Space.Self);
                        break;
                    }
                    case BossPattern.RangedSalvo:
                    {
                        var orbitMul = Definition.Silhouette == BossSilhouette.AuroraSerpent ? 1.45f : 1f;
                        if (fx.name.Contains("GlyphRing"))
                        {
                            fx.transform.Rotate(Vector3.up, 180f * orbitMul * dt, Space.Self);
                            var pulse = 1f + Mathf.Sin(Time.time * 16f) * 0.1f;
                            fx.transform.localScale = new Vector3(1.5f, 0.03f, 1.5f) * pulse;
                        }
                        else
                        {
                            var idx = i;
                            var ang = Time.time * (2.6f * orbitMul) + idx * 2.1f;
                            fx.transform.localPosition = new Vector3(Mathf.Cos(ang) * 0.9f, 1.45f + Mathf.Sin(Time.time * 5f + idx) * 0.08f, Mathf.Sin(ang) * 0.9f);
                        }
                        break;
                    }
                }

                var r = fx.GetComponent<Renderer>();
                if (r != null)
                {
                    var c = RunnerVisuals.GetColor(r.material);
                    c.a = Mathf.Clamp01(c.a) * Mathf.Lerp(0.25f, 1f, life01);
                    RunnerVisuals.SetColor(r.material, c);
                }
            }
        }

        private void ClearAttackFx()
        {
            for (var i = attackFxObjects.Count - 1; i >= 0; i--)
            {
                if (attackFxObjects[i] != null) Object.Destroy(attackFxObjects[i]);
            }
            attackFxObjects.Clear();
            attackFxTimer = 0f;
        }

        private void UpdateHealthCrystals()
        {
            for (var i = 0; i < healthCrystals.Length; i++)
            {
                if (healthCrystals[i] == null) continue;
                var alive = i < HitsRemaining;
                var renderer = healthCrystals[i].GetComponent<Renderer>();
                if (renderer != null)
                {
                    var c = alive
                        ? Definition.GlowColor
                        : new Color(0.25f, 0.28f, 0.32f, 0.55f);
                    var current = RunnerVisuals.GetColor(renderer.material);
                    RunnerVisuals.SetColor(renderer.material, Color.Lerp(current, c, 0.2f));
                }
                if (alive)
                {
                    var bob = Mathf.Sin(Time.time * 3.2f + i * 0.7f) * 0.06f;
                    var p = healthCrystals[i].localPosition;
                    p.y = 4.4f + bob;
                    healthCrystals[i].localPosition = p;
                    healthCrystals[i].Rotate(Vector3.up, 60f * Time.deltaTime);
                }
            }
        }

        private void UpdateBodyTint(float dt)
        {
            for (var i = 0; i < tintRenderers.Count; i++)
            {
                var rend = tintRenderers[i];
                if (rend == null) continue;
                var baseColor = i < tintBaseColors.Count ? tintBaseColors[i] : Color.white;

                Color target;
                if (HitFlashTimer > 0f)
                {
                    // 受击闪白：在原色基础上叠加白光
                    var t = HitFlashTimer / BossSystem.HitInvulnerabilitySeconds;
                    var flash = Mathf.PingPong(Time.time * 30f, 1f);
                    target = Color.Lerp(baseColor, Color.white, flash * 0.6f * t);
                }
                else if (Phase == BossPhase.Vulnerable)
                {
                    // 破绽期：脉动绿光
                    var pulse = (Mathf.Sin(Time.time * 6f) + 1f) * 0.5f;
                    target = Color.Lerp(baseColor, new Color(0.5f, 1f, 0.7f), pulse * 0.4f);
                }
                else
                {
                    target = baseColor;
                }
                RunnerVisuals.SetColor(rend.material, target);
            }
        }

        // ── primitive helpers ──────────────────────────────────

        private static GameObject AddPrim(string name, PrimitiveType type, Vector3 position, Vector3 scale, Color color, bool transparent = false)
        {
            return RunnerVisuals.CreatePrimitive(name, type, position, scale, color, transparent);
        }

        private static GameObject AddPrimChild(Transform parent, string name, PrimitiveType type, Vector3 localPos, Vector3 scale, Color color, bool transparent = false)
        {
            var go = RunnerVisuals.CreatePrimitive(name, type, Vector3.zero, scale, color, transparent);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localScale = scale;
            return go;
        }

        private static void Shuffle<T>(T[] arr)
        {
            for (var i = arr.Length - 1; i > 0; i--)
            {
                var j = Random.Range(0, i + 1);
                (arr[i], arr[j]) = (arr[j], arr[i]);
            }
        }
    }

    /// <summary>
    /// 投射物的简单抛物线动画：自身向 X 平移并向地面下落，到达地面后销毁。
    /// </summary>
    internal sealed class BossProjectileAnim : MonoBehaviour
    {
        private Transform tr;
        private Vector3 targetXZ;
        private float life;
        private float maxLife;
        private Vector3 startPos;

        public void Init(Transform t, Vector3 target, float lifetime)
        {
            tr = t;
            targetXZ = target;
            maxLife = Mathf.Max(0.4f, lifetime);
            life = maxLife;
            startPos = tr.position;
        }

        private void Update()
        {
            if (tr == null) return;
            life -= Time.deltaTime;
            var t = 1f - Mathf.Clamp01(life / maxLife);

            // 抛物线：水平 lerp，垂直 sin 弧度
            var pos = Vector3.Lerp(startPos, new Vector3(targetXZ.x, startPos.y - 1.5f, startPos.z * (1f - t) - 4f * t), t);
            pos.y = startPos.y * (1f - t) + Mathf.Sin(t * Mathf.PI) * 1.5f;
            tr.position = pos;
            tr.Rotate(Vector3.up, 240f * Time.deltaTime);
            tr.Rotate(Vector3.right, 180f * Time.deltaTime);

            if (life <= 0f) Destroy(gameObject);
        }
    }
}
