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

        /// <summary>当前攻击是否处于"击发窗口"（命中判定有效）。预警可更久，但伤害窗口应短而明确。</summary>
        public bool IsPatternDangerous
        {
            get
            {
                if (patternTimer <= 0f) return false;
                var elapsed = patternDuration - patternTimer;
                return elapsed >= patternDangerDelay && elapsed <= patternDangerDelay + patternActiveWindow;
            }
        }

        /// <summary>获取当前攻击判定进度（0-1，用于调试显示）</summary>
        public float GetAttackProgress01()
        {
            if (patternDuration <= 0f) return 0f;
            return 1f - Mathf.Clamp01(patternTimer / patternDuration);
        }

        /// <summary>获取危险窗口开始和结束时间（相对于攻击开始）</summary>
        public (float start, float end, float duration) GetDangerWindow()
        {
            return (patternDangerDelay, patternDangerDelay + patternActiveWindow, patternActiveWindow);
        }

        /// <summary>获取预警剩余时间</summary>
        public float GetTelegraphTimeRemaining()
        {
            foreach (var t in telegraphs)
            {
                if (t.Go != null && t.Lifetime > 0 && !t.IsDecoy)
                    return t.Lifetime;
            }
            return 0f;
        }

        public bool UsesDeceptiveTelegraphs => Definition.Archetype == BossArchetype.Deceptive;

        /// <summary>危险窗口剩余秒数（非危险窗为 0）。</summary>
        public float DangerWindowTimeRemaining
        {
            get
            {
                if (!IsPatternDangerous) return 0f;
                var elapsed = GetPatternElapsed();
                return Mathf.Max(0f, patternDangerDelay + patternActiveWindow - elapsed);
            }
        }

        /// <summary>当前招式前摇进度 0→1（至危险窗开始）。</summary>
        public float WindupProgress01
        {
            get
            {
                if (patternTimer <= 0f || Pattern == BossPattern.None) return 0f;
                return patternDangerDelay > 0.01f
                    ? Mathf.Clamp01(GetPatternElapsed() / patternDangerDelay)
                    : 0f;
            }
        }

        /// <summary>危险窗口内进度 0→1。</summary>
        public float DangerWindowProgress01
        {
            get
            {
                if (!IsPatternDangerous || patternActiveWindow <= 0.01f) return 0f;
                var elapsed = GetPatternElapsed();
                return Mathf.Clamp01((elapsed - patternDangerDelay) / patternActiveWindow);
            }
        }

        /// <summary>当前招式视觉阶段（前摇/预警/击发/收招）。</summary>
        public BossAttackPhase AttackPhase => patternVfxPlayer.Phase;

        public float PatternStrikeCameraShakeAmp { get; private set; }
        public float PatternStrikeCameraShakeDuration { get; private set; }

        // 模式时间控制
        private float patternTimer;
        private float patternDuration;
        private float patternDangerDelay;
        private float patternActiveWindow;
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
        private float minPatternDangerDelay = 0.55f;
        private Vector3 patternWindupScale = Vector3.one;
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
        private float spawnDuration;
        private Vector3 spawnStartPos;
        private Vector3 spawnStartBodyScale = Vector3.one;
        private Vector3 spawnStartEuler;

        // 视觉子节点引用
        private readonly Transform body;
        private readonly Renderer bodyRenderer;
        private readonly List<Renderer> tintRenderers = new();
        // 记录每个 tint 渲染器的"原色"，受击/破绽期间在原色基础上叠加效果，避免颜色累积漂移
        private readonly List<Color> tintBaseColors = new();
        private readonly Transform leftArm;
        private readonly Transform rightArm;
        private Transform head;
        private readonly Transform[] healthCrystals = new Transform[BossSystem.BossMaxHits];
        private readonly Renderer[] healthCrystalRenderers = new Renderer[BossSystem.BossMaxHits];
        private readonly List<GameObject> projectileObjects = new();
        private readonly List<TelegraphMarker> telegraphs = new();
        private readonly BossPatternVfxPlayer patternVfxPlayer = new();
        private readonly BossBodyAnimator bodyAnimator = new();
        private readonly BossRigAnimator rigAnimator = new();
        private bool usesRiggedModel;
        private BossPhase lastTickPhase = BossPhase.Spawning;
        private BossThemeTrait themeTrait;
        private static readonly Dictionary<string, Texture2D> textureCache = new();
        private int patternDiveLaneIdx;
        private int patternSweepSafeLaneIdx = int.MinValue;
        private System.Action<BossPattern, BossPatternVfxProfile> strikeCallback;
        private float lastRunDistance;

        internal float LastRunDistance => lastRunDistance;

        internal float TelegraphSurfaceY(float worldZ) =>
            TrackTerrain.SurfaceY(worldZ, lastRunDistance, baseGroundY);

        internal float PatternDangerDelay => patternDangerDelay;
        internal float PatternDuration => patternDuration;
        internal float PatternTimer => patternTimer;
        internal float LaneWidthInternal => laneWidth;
        internal float BaseGroundYInternal => baseGroundY;
        internal Transform BodyTransform => body;
        internal Transform FxSceneParentInternal => FxSceneParent;
        internal float GetPatternElapsed() =>
            patternDuration - Mathf.Clamp(patternTimer, 0f, patternDuration);

        public void SetStrikeCallback(System.Action<BossPattern, BossPatternVfxProfile> callback) =>
            strikeCallback = callback;

        public void SetThemeTrait(BossThemeTrait trait) => themeTrait = trait;

        private struct TelegraphMarker
        {
            public GameObject Go;
            public Renderer Rend;
            public float Lifetime;
            public float MaxLifetime;
            public Vector3 BaseScale;
            /// <summary>预警期配色；危险窗口切换为高亮红。</summary>
            public Color WarnColor;
            /// <summary>雾堤假预警：不参与命中，危险窗前淡出。</summary>
            public bool IsDecoy;
        }

        private Transform FxSceneParent =>
            Root != null && Root.transform.parent != null ? Root.transform.parent : Root.transform;

        public BossEncounter(BossDefinition def, float baseGroundY, float laneWidth, float runDistance,
            float patternMinSeconds = 1.6f, float patternMaxSeconds = 2.4f,
            float dangerWindowRatio = 0.55f, float telegraphLifetimeRatio = 0.6f, float vulnerableChance = 0.5f,
            float minDangerDelay = 0.55f)
        {
            Definition = def;
            this.baseGroundY = baseGroundY;
            this.laneWidth = laneWidth;
            this.patternMinSeconds = patternMinSeconds;
            this.patternMaxSeconds = patternMaxSeconds;
            this.dangerWindowRatio = Mathf.Clamp(dangerWindowRatio, 0.3f, 0.82f);
            this.telegraphLifetimeRatio = Mathf.Clamp(telegraphLifetimeRatio, 0.35f, 0.9f);
            this.vulnerableChance = Mathf.Clamp01(vulnerableChance);
            minPatternDangerDelay = Mathf.Max(0.35f, minDangerDelay);
            ConfigurePatternIntervalByStyle();
            Phase = BossPhase.Spawning;
            spawnTimer = 1.8f;
            spawnDuration = spawnTimer;

            Root = new GameObject($"Boss_{def.DisplayName}");
            var startSurface = TrackTerrain.SurfaceY(BossSystem.BossSpawnAheadZ, runDistance, baseGroundY);
            Root.transform.position = new Vector3(0f, startSurface, BossSystem.BossSpawnAheadZ);

            body = BuildSilhouette(def, out bodyRenderer, out leftArm, out rightArm);
            // rigged 模型的头由骨骼动画驱动，不做程序化 head 覆盖（会与动画采样互相覆盖）
            head = usesRiggedModel ? null : body.Find("Head") ?? body.Find("EagleHead");
            bodyAnimator.CaptureHeadBaseline(head);
            patternVfxPlayer.SetStrikeCallback(OnPatternStrikeMoment);
            ApplyOpenTextures(def);

            // 缓存所有 tint 渲染器的初始颜色，作为 flash/破绽叠加的基准
            tintBaseColors.Clear();
            foreach (var r in tintRenderers)
            {
                tintBaseColors.Add(r != null && r.sharedMaterial != null
                    ? RunnerVisuals.GetColor(r.sharedMaterial)
                    : Color.white);
            }

            // 健康水晶（在 boss 上方），击中后变暗。颜色仅由 UpdateHealthCrystals 驱动，
            // 不加入 tintRenderers，避免与 UpdateBodyTint 每帧互相覆盖。
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
                healthCrystalRenderers[i] = crystal.GetComponent<Renderer>();
            }

            // 底部威吓光环
            var ring = AddPrim("BossRing", PrimitiveType.Cylinder,
                new Vector3(0f, 0.05f, 0f),
                new Vector3(3.4f, 0.06f, 3.4f),
                new Color(def.GlowColor.r, def.GlowColor.g, def.GlowColor.b, 0.45f), true);
            ring.transform.SetParent(Root.transform, false);

            // 出场震波
            CreateTelegraph(0f, 0.7f, 4f, def.GlowColor);
            ConfigureSpawnIntroPose();
            CreateSpawnSignatureFx();
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
            patternVfxPlayer.End();
        }

        // ── 阶段节奏控制 ──────────────────────────────────────
        private int patternsSinceLastVulnerable = 0;
        private const int MaxPatternsBeforeVulnerable = 4; // 最多连续4招后必出破绽
        private float elapsedInPhase = 0f;
        private int currentPhaseCycle = 0; // 当前阶段循环计数

        public void Tick(float dt, float runDistance, float surfaceBaseY)
        {
            if (Root == null) return;

            lastRunDistance = runDistance;
            HitFlashTimer = Mathf.Max(0f, HitFlashTimer - dt);
            hitReactionTimer = Mathf.Max(0f, hitReactionTimer - dt);
            breakReactionTimer = Mathf.Max(0f, breakReactionTimer - dt);
            bobPhase += dt;
            elapsedInPhase += dt;
            attackAnimPunch = ComputeAttackAnimPunch();

            if (Phase != lastTickPhase)
            {
                OnPhaseChangedForRig(lastTickPhase, Phase);
                lastTickPhase = Phase;
            }
            rigAnimator.Tick(dt);

            // boss 跟随路面前进（保持在玩家前方），落地高度跟随地形
            var holdZ = Phase == BossPhase.Retreating ? BossSystem.BossActiveZ + 4f : BossSystem.BossActiveZ;
            var pos = Root.transform.position;
            if (Phase == BossPhase.Spawning)
            {
                UpdateSpawnTransform(dt, runDistance, surfaceBaseY, holdZ, ref pos);
            }
            else
            {
                var targetZ = Mathf.Lerp(pos.z, holdZ, Mathf.Clamp01(dt * 3.2f));
                pos.x = Mathf.Lerp(pos.x, 0f, Mathf.Clamp01(dt * 4f));
                pos.z = targetZ;
                pos.y = TrackTerrain.SurfaceY(pos.z, runDistance, surfaceBaseY);
                Root.transform.rotation = Quaternion.Slerp(
                    Root.transform.rotation,
                    Quaternion.identity,
                    Mathf.Clamp01(dt * 5.2f));
            }
            Root.transform.position = pos;

            patternVfxPlayer.Tick(dt);

            var vfxProfile = BossPatternVfxProfile.Get(Pattern);
            var strike01 = IsPatternDangerous ? DangerWindowProgress01 : 0f;
            var recovery01 = Pattern != BossPattern.None && !IsPatternDangerous && patternTimer > 0f
                ? Mathf.Clamp01((GetPatternElapsed() - patternDangerDelay - patternActiveWindow) / Mathf.Max(0.2f, patternDuration - patternDangerDelay - patternActiveWindow))
                : 0f;
            var bodyAnimPos = Vector3.zero;
            var bodyAnimRot = Vector3.zero;
            bodyAnimator.Apply(body, head, Definition, Pattern, AttackPhase, vfxProfile,
                WindupProgress01, strike01, recovery01, ref bodyAnimPos, ref bodyAnimRot);

            // 浮动 + 朝向玩家（每个 Boss 的前倾角度由 Definition.AttackWindupLean 决定）
            if (body != null)
            {
                var float01 = Mathf.Sin(bobPhase * 1.6f) * 0.12f;
                var attackForward = attackAnimPunch * 0.55f;
                var basePos = new Vector3(0f, 1.2f + float01, -attackForward);
                var windupLeanX = attackAnimPunch * Definition.AttackWindupLean * 0.72f;
                var baseRot = new Vector3(
                    windupLeanX,
                    180f + Mathf.Sin(bobPhase * 1.4f) * 6f,
                    Mathf.Sin(bobPhase * 1.1f) * 4f + attackAnimPunch * 6f);
                EvaluateReactionOffsets(out var reactionPos, out var reactionRot);
                body.localPosition = basePos + bodyAnimPos + reactionPos;
                body.localRotation = Quaternion.Euler(baseRot + bodyAnimRot + reactionRot);
                var targetScale = ComputeBodyTargetScale();
                body.localScale = Vector3.Lerp(body.localScale, targetScale, dt * (IsPatternDangerous ? 14f : 6f));
            }

            UpdateAttackPose();

            UpdateTelegraphs(dt);
            UpdateProjectiles(dt);
            UpdateHealthCrystals();
            UpdateBodyTint(dt);

            switch (Phase)
            {
                case BossPhase.Spawning:
                    spawnTimer -= dt;
                    if (spawnTimer <= 0f)
                    {
                        Phase = BossPhase.Active;
                        elapsedInPhase = 0f;
                        patternsSinceLastVulnerable = 0;
                        currentPhaseCycle = 0;
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
                            patternsSinceLastVulnerable++;
                        }
                        break;
                    }

                    patternTimer -= dt;
                    if (patternTimer <= 0f)
                    {
                        EndPattern();
                        // 阶段化节奏：连续攻击后强制破绽，或概率进入破绽
                        var forceVulnerable = patternsSinceLastVulnerable >= MaxPatternsBeforeVulnerable;
                        if (forceVulnerable || Random.value < vulnerableChance)
                        {
                            EnterVulnerable();
                            patternsSinceLastVulnerable = 0;
                            currentPhaseCycle++;
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
                        elapsedInPhase = 0f;
                        BeginAttackInterval(quickRecoverAfterHit);
                        quickRecoverAfterHit = false;
                    }
                    break;

                case BossPhase.Retreating:
                    RetreatTimer -= dt;
                    if (Root != null)
                    {
                        if (usesRiggedModel)
                        {
                            // 击败演出：倒地片段（PlayDeath 已触发）+ 后仰下沉 + 收缩离场
                            var defeat01 = 1f - Mathf.Clamp01(RetreatTimer / BossSystem.DefeatRetreatSeconds);
                            var p = Root.transform.position;
                            p.z += dt * 2.6f;
                            p.y -= dt * Mathf.Lerp(0f, 2.2f, Mathf.Max(0f, defeat01 - 0.45f) / 0.55f);
                            Root.transform.position = p;
                            if (body != null && defeat01 > 0.55f)
                            {
                                var shrink = 1f - (defeat01 - 0.55f) / 0.45f * 0.85f;
                                body.localScale = Vector3.one * Mathf.Max(0.15f, shrink);
                            }
                        }
                        else
                        {
                            var p = Root.transform.position;
                            p.y += dt * 4.2f; // 向上飞走
                            Root.transform.position = p;
                            Root.transform.Rotate(Vector3.up, 240f * dt, Space.World);
                        }
                    }
                    break;
            }
        }

        /// <summary>rigged 模型的阶段切换动画：入场移动 → 待机；破绽眩晕；击败倒地。</summary>
        private void OnPhaseChangedForRig(BossPhase from, BossPhase to)
        {
            if (!usesRiggedModel) return;
            switch (to)
            {
                case BossPhase.Active:
                    rigAnimator.PlayIdle(from == BossPhase.Spawning ? 0.3f : 0.18f);
                    break;
                case BossPhase.Vulnerable:
                    rigAnimator.PlayDazed();
                    break;
                case BossPhase.Retreating:
                case BossPhase.Defeated:
                    rigAnimator.PlayDeath();
                    break;
            }
        }

        private void ConfigureSpawnIntroPose()
        {
            if (Root == null || body == null) return;

            var startOffset = Vector3.zero;
            spawnStartBodyScale = Vector3.one;
            spawnStartEuler = Vector3.zero;

            switch (Definition.Silhouette)
            {
                case BossSilhouette.SnowKing:
                    startOffset = new Vector3(0f, -2.4f, 1.6f);
                    spawnStartBodyScale = new Vector3(1.28f, 0.52f, 1.28f);
                    break;
                case BossSilhouette.CedarSentinel:
                    startOffset = new Vector3(0f, 5f, 2.8f);
                    spawnStartBodyScale = new Vector3(0.78f, 1.28f, 0.78f);
                    spawnStartEuler = new Vector3(0f, 118f, 12f);
                    break;
                case BossSilhouette.AuroraSerpent:
                    startOffset = new Vector3(-laneWidth * 1.2f, 1.9f, 3.6f);
                    spawnStartBodyScale = new Vector3(0.56f, 1.52f, 0.56f);
                    spawnStartEuler = new Vector3(0f, -142f, -16f);
                    break;
                case BossSilhouette.MistGuardian:
                    startOffset = new Vector3(0f, 2.1f, 2.1f);
                    spawnStartBodyScale = new Vector3(0.3f, 0.3f, 0.3f);
                    break;
                case BossSilhouette.CoralKraken:
                    startOffset = new Vector3(laneWidth * 1.05f, -1.4f, 3.1f);
                    spawnStartBodyScale = new Vector3(1.46f, 0.48f, 1.46f);
                    spawnStartEuler = new Vector3(0f, 72f, -9f);
                    break;
                case BossSilhouette.StormEagle:
                    startOffset = new Vector3(0f, 8.6f, 8f);
                    spawnStartBodyScale = new Vector3(0.66f, 1.34f, 0.66f);
                    spawnStartEuler = new Vector3(-44f, 180f, 24f);
                    break;
            }

            spawnStartPos = Root.transform.position + startOffset;
            Root.transform.position = spawnStartPos;
            Root.transform.rotation = Quaternion.Euler(spawnStartEuler);
            body.localScale = spawnStartBodyScale;
        }

        private void UpdateSpawnTransform(float dt, float runDistance, float surfaceBaseY, float holdZ, ref Vector3 pos)
        {
            var targetZ = Mathf.Lerp(pos.z, holdZ, Mathf.Clamp01(dt * 2.8f));
            var targetSurface = TrackTerrain.SurfaceY(targetZ, runDistance, surfaceBaseY);
            var progress = Mathf.Clamp01(1f - (spawnTimer / Mathf.Max(0.01f, spawnDuration)));
            var eased = Mathf.SmoothStep(0f, 1f, progress);
            var wobble = Mathf.Sin(progress * Mathf.PI * 4f) * (1f - progress);

            var targetPos = new Vector3(0f, targetSurface, holdZ);
            pos = Vector3.Lerp(spawnStartPos, targetPos, eased);

            var targetEuler = Vector3.zero;
            switch (Definition.Silhouette)
            {
                case BossSilhouette.SnowKing:
                    pos.y += Mathf.Sin(progress * Mathf.PI) * 0.48f;
                    break;
                case BossSilhouette.CedarSentinel:
                    targetEuler.z += wobble * 14f;
                    targetEuler.y += wobble * 8f;
                    break;
                case BossSilhouette.AuroraSerpent:
                    pos.x += Mathf.Sin(progress * Mathf.PI * 2.5f) * laneWidth * 0.36f;
                    targetEuler.y += wobble * 20f;
                    break;
                case BossSilhouette.MistGuardian:
                    pos.y += Mathf.Sin(progress * Mathf.PI * 1.8f) * 0.25f;
                    targetEuler.y += wobble * 6f;
                    break;
                case BossSilhouette.CoralKraken:
                    pos.x += Mathf.Sin(progress * Mathf.PI * 1.6f) * laneWidth * 0.22f;
                    targetEuler.z += wobble * 10f;
                    break;
                case BossSilhouette.StormEagle:
                    pos.z -= Mathf.Sin(progress * Mathf.PI) * 1.2f;
                    targetEuler.x -= Mathf.Sin(progress * Mathf.PI) * 12f;
                    targetEuler.y += wobble * 16f;
                    break;
            }

            var spawnEuler = Vector3.Lerp(spawnStartEuler, targetEuler, eased);
            Root.transform.rotation = Quaternion.Euler(spawnEuler);

            if (body != null)
            {
                body.localScale = Vector3.Lerp(spawnStartBodyScale, Vector3.one, eased);
            }
        }

        private void CreateSpawnSignatureFx()
        {
            switch (Definition.Silhouette)
            {
                case BossSilhouette.SnowKing:
                    CreateTelegraph(0f, 0.95f, 4.8f, new Color(0.72f, 0.95f, 1f, 0.65f));
                    break;
                case BossSilhouette.CedarSentinel:
                    CreateTelegraph(0f, 0.85f, 4.2f, new Color(0.95f, 0.62f, 0.28f, 0.62f));
                    break;
                case BossSilhouette.AuroraSerpent:
                    CreateTelegraph(-laneWidth * 0.45f, 0.9f, 4.5f, new Color(0.72f, 0.48f, 1f, 0.62f));
                    CreateTelegraph(laneWidth * 0.45f, 0.9f, 4.5f, new Color(0.42f, 1f, 0.85f, 0.55f));
                    break;
                case BossSilhouette.MistGuardian:
                    CreateTelegraph(0f, 1.1f, 5.1f, new Color(0.82f, 0.9f, 0.95f, 0.42f));
                    break;
                case BossSilhouette.CoralKraken:
                    CreateTelegraph(0f, 0.92f, 4.7f, new Color(1f, 0.72f, 0.45f, 0.58f));
                    break;
                case BossSilhouette.StormEagle:
                    CreateTelegraph(0f, 0.75f, 4.1f, new Color(1f, 0.95f, 0.46f, 0.7f));
                    break;
            }
        }

        /// <summary>当前 Boss 战已进入第几个攻击-破绽循环</summary>
        public int CurrentPhaseCycle => currentPhaseCycle;

        /// <summary>距离下次破绽还有几招</summary>
        public int PatternsUntilVulnerable => Mathf.Max(0, MaxPatternsBeforeVulnerable - patternsSinceLastVulnerable);

        /// <summary>当前阶段已持续时间</summary>
        public float ElapsedInCurrentPhase => elapsedInPhase;

        public void EnterAfterHit()
        {
            HitFlashTimer = BossSystem.HitInvulnerabilitySeconds;
            hitReactionTimer = HitReactionDuration;
            EndPattern();
            quickRecoverAfterHit = true;
            patternTimer = 0f;
            vulnerableTimer = 0f;
            patternVfxPlayer.PlayThemePhaseCue(this, themeTrait, BossPhase.Active);
            rigAnimator.PlayHit();
        }

        private void BeginAttackInterval(bool afterBreak = false)
        {
            EndPattern();
            // afterBreak：玩家刚打破 boss 护盾，给一段较短的喘息（仍保持节奏）
            var lo = afterBreak ? patternIntervalMin * 0.82f : patternIntervalMin;
            var hi = afterBreak ? patternIntervalMax * 0.90f : patternIntervalMax;
            var intervalMultiplier = themeTrait.AttackIntervalMultiplier > 0f
                ? themeTrait.AttackIntervalMultiplier
                : 1f;
            intervalMultiplier *= HealthAggressionMultiplier();
            lo *= intervalMultiplier;
            hi *= intervalMultiplier;
            patternIntervalDuration = Random.Range(lo, Mathf.Max(lo + 0.25f, hi));
            patternIntervalTimer = patternIntervalDuration;
        }

        /// <summary>
        /// 残血压迫：Boss 每掉一管血，招式之间的喘息就缩短一档（满血 1.0 / 二段 0.88 / 残血 0.76）。
        /// 只压缩「攻击之间的间隔」，不动前摇与预警窗口——那两项是玩家的反应预算，
        /// 缩短它们会把难度变成不公平而不是更有压迫感。
        /// </summary>
        private float HealthAggressionMultiplier()
        {
            var lost = Mathf.Clamp(BossSystem.BossMaxHits - HitsRemaining, 0, BossSystem.BossMaxHits - 1);
            return 1f - 0.12f * lost;
        }

        private void StartNextPattern()
        {
            ClearTelegraphs();
            patternVfxPlayer.End();
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
            patternDangerDelay = ComputePatternDangerDelay(patternDuration);
            patternActiveWindow = ComputePatternActiveWindow(patternDuration);
            patternDiveLaneIdx = 0;
            patternSweepSafeLaneIdx = int.MinValue;

            switch (Pattern)
            {
                case BossPattern.SweepLow:
                {
                    var safeIdx = Random.Range(-1, 2);
                    patternSweepSafeLaneIdx = safeIdx;
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
                    var laneIdx = Random.Range(-1, 2);
                    patternDiveLaneIdx = laneIdx;
                    var bp = Root.transform.position;
                    bp.x = laneIdx * laneWidth;
                    Root.transform.position = bp;
                    CreateTelegraph(laneIdx * laneWidth, PatternTelegraphLifetime(), 1.6f, new Color(1f, 0.55f, 0.3f, 0.8f));
                    break;
                }
                case BossPattern.ChargeAcross:
                {
                    var safeIdx = Random.Range(-1, 2);
                    SafeLaneX = safeIdx * laneWidth;
                    for (var i = -1; i <= 1; i++)
                    {
                        if (i == safeIdx) continue;
                        CreateTelegraph(i * laneWidth, PatternTelegraphLifetime(), 1.4f, new Color(1f, 0.35f, 0.3f, 0.88f));
                    }
                    break;
                }
                case BossPattern.RangedSalvo:
                {
                    SalvoLaneX.Clear();
                    var n = Random.Range(2, 4);
                    var lanes = new[] { -1, 0, 1 };
                    Shuffle(lanes);
                    for (var k = 0; k < n; k++)
                    {
                        var lx = lanes[k] * laneWidth;
                        SalvoLaneX.Add(lx);
                        var impactTime = patternDangerDelay + patternActiveWindow * 0.5f;
                        SpawnProjectile(lx, impactTime);
                        CreateTelegraph(lx, PatternTelegraphLifetime(0.04f), 1.1f, new Color(1f, 0.7f, 0.3f, 0.88f));
                    }
                    break;
                }
                case BossPattern.CenterBeam:
                    CreateTelegraph(0f, PatternTelegraphLifetime(), 1.45f, new Color(1f, 0.3f, 0.65f, 0.8f));
                    break;
                case BossPattern.QuakePulse:
                    for (var i = -1; i <= 1; i++)
                    {
                        CreateTelegraph(i * laneWidth, PatternTelegraphLifetime(), 2.35f, new Color(1f, 0.78f, 0.2f, 0.88f), flatRing: true);
                    }
                    break;
            }

            patternVfxPlayer.Begin(this, Pattern, patternDuration, patternDiveLaneIdx, patternSweepSafeLaneIdx);
            SpawnDeceptiveDecoyTelegraphsIfNeeded();
            ApplySignatureWindup();

            // 骨骼攻击动画：片段收势对齐危险窗结束（速度缩放，判定不读动画）
            rigAnimator.PlayAttack(Pattern, patternDangerDelay + patternActiveWindow);
        }

        private void OnPatternStrikeMoment(BossPattern p, BossPatternVfxProfile prof)
        {
            PatternStrikeCameraShakeAmp = prof.CameraShakeAmp * Definition.VfxShakeMultiplier;
            PatternStrikeCameraShakeDuration = prof.CameraShakeDuration;
            patternVfxPlayer.PlayThemeStrikeCue(themeTrait);
            strikeCallback?.Invoke(p, prof);
        }

        /// <summary>在招式起手瞬间激活每个 Boss 专属的视觉/缩放前摇。</summary>
        private void ApplySignatureWindup()
        {
            if (body == null) return;
            patternWindupScale = Definition.Silhouette switch
            {
                BossSilhouette.SnowKing => new Vector3(1.12f, 1.12f, 1.12f),
                BossSilhouette.CedarSentinel => new Vector3(1.18f, 0.90f, 1.10f),
                BossSilhouette.AuroraSerpent => new Vector3(0.92f, 1.22f, 0.92f),
                BossSilhouette.MistGuardian => new Vector3(1.05f, 1.08f, 1.05f),
                BossSilhouette.CoralKraken => new Vector3(1.22f, 0.95f, 1.05f),
                BossSilhouette.StormEagle => new Vector3(0.82f, 1.18f, 0.82f),
                _ => Vector3.one * 1.06f,
            };
            // rigged 模型自带攻击动画，程序化 squash 减半避免过度扭曲角色
            if (usesRiggedModel)
            {
                patternWindupScale = Vector3.Lerp(Vector3.one, patternWindupScale, 0.45f);
            }
            body.localScale = patternWindupScale;
        }

        private float ComputeAttackAnimPunch()
        {
            if (Phase != BossPhase.Active || Pattern == BossPattern.None || patternTimer <= 0f)
                return Mathf.Sin(bobPhase * 2.4f) * 0.08f * Definition.AttackStrikePunch;

            var elapsed = GetPatternElapsed();
            var windup01 = patternDangerDelay > 0.01f
                ? Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / patternDangerDelay))
                : 0f;
            if (IsPatternDangerous)
                return Definition.AttackStrikePunch * (0.55f + 0.45f * DangerWindowProgress01);
            if (elapsed < patternDangerDelay)
                return Definition.AttackStrikePunch * windup01 * 0.85f;
            return Definition.AttackStrikePunch * 0.2f;
        }

        private Vector3 ComputeBodyTargetScale()
        {
            if (Phase != BossPhase.Active || Pattern == BossPattern.None || patternTimer <= 0f)
                return Vector3.one;

            if (IsPatternDangerous)
            {
                var strike = 1.06f + Definition.AttackStrikePunch * 0.05f;
                return patternWindupScale * strike;
            }

            var elapsed = GetPatternElapsed();
            if (elapsed < patternDangerDelay)
                return patternWindupScale;
            return Vector3.Lerp(patternWindupScale, Vector3.one, 0.35f);
        }

        private void SpawnDeceptiveDecoyTelegraphsIfNeeded()
        {
            if (Definition.Archetype != BossArchetype.Deceptive) return;
            if (Pattern == BossPattern.Vulnerable) return;

            var decoyCount = Random.Range(1, 3);
            var lanes = new[] { -1, 0, 1 };
            Shuffle(lanes);
            for (var k = 0; k < decoyCount; k++)
            {
                var lx = lanes[k] * laneWidth;
                var alreadyReal = false;
                foreach (var t in telegraphs)
                {
                    if (t.IsDecoy || t.Go == null) continue;
                    if (Mathf.Abs(t.Go.transform.position.x - lx) < 0.2f)
                    {
                        alreadyReal = true;
                        break;
                    }
                }
                if (alreadyReal) continue;
                CreateTelegraph(lx, PatternTelegraphLifetime(0.08f), 1.25f,
                    new Color(0.55f, 0.62f, 0.7f, 0.38f), isDecoy: true);
            }
        }

        // ── 攻击判定时间优化 ───────────────────────────────────
        // 原则：预警时间充足，伤害窗口短而明确

        private float PatternTelegraphLifetime(float bonus = 0f)
        {
            var minTelegraphTime = 0.6f;
            var byRatio = patternDuration * Mathf.Clamp01(telegraphLifetimeRatio + bonus);
            var throughDanger = patternDangerDelay + patternActiveWindow + 0.25f;
            return Mathf.Max(minTelegraphTime, throughDanger, byRatio * 0.95f);
        }

        private float ComputePatternDangerDelay(float duration)
        {
            // 改进：根据 Boss 定位调整预警时间
            // 新手友好型 Boss 给更多预警，高难度 Boss 预警更短
            var archetypeDelayMul = Definition.Archetype switch
            {
                BossArchetype.Balanced => 1.15f,    // 冰霜雪王：更长的预警时间
                BossArchetype.Grounded => 1.0f,     // 雪松哨兵：标准
                BossArchetype.Evasive => 0.88f,     // 极光长蛇：稍短（配合颜色轨迹）
                BossArchetype.Deceptive => 1.0f,    // 雾堤守卫：标准（依靠真假预警机制）
                BossArchetype.Defensive => 1.0f,    // 珊瑚海怪：标准
                BossArchetype.Aerial => 0.82f,      // 雷云苍鹰：较短（高速 Boss）
                _ => 1.0f,
            };

            // 基础延迟：危险窗口开始前的时间
            // 使用更小的 dangerWindowRatio 意味着更长的准备时间
            var adjustedRatio = Mathf.Clamp(dangerWindowRatio * 0.85f, 0.28f, 0.72f);
            var baseDelay = duration * (1f - adjustedRatio) * archetypeDelayMul;

            return Mathf.Clamp(baseDelay, minPatternDangerDelay, duration * 0.75f);
        }

        private float ComputePatternActiveWindow(float duration)
        {
            // 改进：更短的实际伤害窗口，让躲避判定更清晰
            // 不同招式有不同的危险窗口（单位：秒）
            var baseWindow = Pattern switch
            {
                BossPattern.SweepLow => 0.35f,      // 横扫：短窗口，需要快速反应
                BossPattern.DiveHigh => 0.42f,       // 俯冲：稍长，因为需要跳跃时机
                BossPattern.ChargeAcross => 0.38f,     // 冲锋：中等
                BossPattern.RangedSalvo => 0.32f,     // 齐射：分散投射物，短窗口
                BossPattern.CenterBeam => 0.45f,      // 光束：持续型，稍长
                BossPattern.QuakePulse => 0.35f,      // 震波：短窗口
                _ => 0.38f,
            };

            // 根据 Boss 强度调整
            var intensityMul = Definition.PhaseIntensity switch
            {
                <= 0.85f => 1.1f,  // 低强度：更宽容
                <= 1.0f => 1.0f,   // 中等：标准
                _ => 0.92f,        // 高强度：更严格
            };

            return Mathf.Clamp(baseWindow * intensityMul, 0.28f, 0.65f);
        }

        private void ConfigurePatternIntervalByStyle()
        {
            // 攻击间隔：让玩家在连续攻击之间有足够的喘息和反应时间
            switch (Definition.Silhouette)
            {
                case BossSilhouette.StormEagle:
                    patternIntervalMin = 1.1f;
                    patternIntervalMax = 1.9f;
                    break;
                case BossSilhouette.AuroraSerpent:
                    patternIntervalMin = 1.2f;
                    patternIntervalMax = 2.0f;
                    break;
                case BossSilhouette.SnowKing:
                    patternIntervalMin = 1.4f;
                    patternIntervalMax = 2.2f;
                    break;
                case BossSilhouette.CedarSentinel:
                    patternIntervalMin = 1.5f;
                    patternIntervalMax = 2.4f;
                    break;
                default:
                    patternIntervalMin = 1.3f;
                    patternIntervalMax = 2.1f;
                    break;
            }
        }

        private void ApplyOpenTextures(BossDefinition def)
        {
            // rigged 模型自带 Atlas 贴图材质，不再叠加表面贴图
            if (usesRiggedModel) return;

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
            var elapsed = GetPatternElapsed();
            var windupSpan = Mathf.Max(0.12f, patternDangerDelay);
            var windup = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / windupSpan));
            var strike = AttackPhase == BossAttackPhase.Strike
                ? 0.55f + DangerWindowProgress01 * 0.45f
                : AttackPhase == BossAttackPhase.Recovery ? 0.25f : 0f;
            var dangerSnap = strike * (0.5f + Mathf.Sin(Time.time * 26f * styleTempo) * 0.5f);

            float attackL = 0f;
            float attackR = 0f;
            switch (Pattern)
            {
                case BossPattern.SweepLow:
                    attackL = Mathf.Lerp(-38f, 82f, windup) + strike * 22f * stylePower + dangerSnap * 8f;
                    attackR = Mathf.Lerp(38f, -82f, windup) - strike * 22f * stylePower - dangerSnap * 8f;
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
                    // 破绽期：抖动频率由 VulnerableShakeFreq 控制，越高越颤抖
                    var vulnFreq = 8f * styleTempo * Definition.VulnerableShakeFreq;
                    attackL = -40f + Mathf.Sin(Time.time * vulnFreq) * 6f;
                    attackR = 40f - Mathf.Sin(Time.time * vulnFreq * 1.3f) * 6f;
                    break;
            }

            var targetL = Mathf.Lerp(idleL, attackL, windup) + styleBiasL;
            var targetR = Mathf.Lerp(idleR, attackR, windup) + styleBiasR;

            // 额外欧拉角（俯仰/偏航），强化招式可读性
            var eulerXL = 0f;
            var eulerYL = 0f;
            var eulerXR = 0f;
            var eulerYR = 0f;
            switch (Pattern)
            {
                case BossPattern.SweepLow:
                    eulerYL = Mathf.Lerp(14f, -28f, windup) + strike * -18f;
                    eulerYR = Mathf.Lerp(-14f, 28f, windup) + strike * 18f;
                    break;
                case BossPattern.DiveHigh:
                    eulerXL = eulerXR = Mathf.Lerp(6f, 52f, windup) + strike * 24f;
                    eulerYL = Mathf.Lerp(0f, -16f, windup);
                    eulerYR = Mathf.Lerp(0f, 16f, windup);
                    break;
                case BossPattern.ChargeAcross:
                    eulerXL = eulerXR = Mathf.Lerp(4f, 64f, windup) + strike * 18f;
                    break;
                case BossPattern.CenterBeam:
                    eulerYL = Mathf.Lerp(28f, -48f, windup);
                    eulerYR = Mathf.Lerp(-28f, 48f, windup);
                    eulerXL = eulerXR = strike * 26f + windup * 12f;
                    break;
                case BossPattern.QuakePulse:
                    eulerXL = eulerXR = Mathf.Lerp(-22f, 58f, windup) * (0.4f + strike * 0.6f);
                    break;
                case BossPattern.RangedSalvo:
                    var aimLeft = Mathf.FloorToInt(Time.time * 3.1f) % 2 == 0;
                    if (aimLeft)
                    {
                        eulerXL = Mathf.Lerp(2f, 36f, windup);
                        eulerYR = Mathf.Lerp(0f, -26f, windup);
                    }
                    else
                    {
                        eulerXR = Mathf.Lerp(2f, 36f, windup);
                        eulerYL = Mathf.Lerp(0f, 26f, windup);
                    }
                    break;
            }

            var flap = Mathf.Sin(Time.time * 40f) * reactionWingFlap;
            targetL += flap + reactionArmRecoil;
            targetR -= flap - reactionArmRecoil;
            var armSpeed = Time.deltaTime * (14f + styleTempo * 4f);
            leftArm.localRotation = Quaternion.Slerp(leftArm.localRotation, Quaternion.Euler(eulerXL, eulerYL, targetL), armSpeed);
            rightArm.localRotation = Quaternion.Slerp(rightArm.localRotation, Quaternion.Euler(eulerXR, eulerYR, targetR), armSpeed);
        }

        private void EndPattern()
        {
            Pattern = BossPattern.None;
            SalvoLaneX.Clear();
            ClearTelegraphs();
            patternVfxPlayer.End();
            PatternStrikeCameraShakeAmp = 0f;
            PatternStrikeCameraShakeDuration = 0f;
        }

        private void EnterVulnerable()
        {
            Phase = BossPhase.Vulnerable;
            Pattern = BossPattern.Vulnerable;
            var windowMultiplier = themeTrait.VulnerableWindowMultiplier > 0f
                ? themeTrait.VulnerableWindowMultiplier
                : 1f;
            vulnerableTimer = BossSystem.VulnerableSeconds * windowMultiplier;
            breakReactionTimer = BreakReactionDuration;
            patternVfxPlayer.End();
            patternVfxPlayer.PlayThemePhaseCue(this, themeTrait, BossPhase.Vulnerable);
            CreateTelegraph(WorldX, vulnerableTimer, 2.4f, new Color(0.4f, 1f, 0.55f, 0.9f));
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

            // 优先使用 CC0 rigged 模型（Quaternius Ultimate Monsters）；缺失时回退程序化剪影
            if (TryBuildRiggedModel(def, bodyGo.transform, out mainRenderer))
            {
                usesRiggedModel = true;
                armL = null;
                armR = null;
                return bodyGo.transform;
            }

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

        internal static string RiggedModelName(BossSilhouette silhouette) => silhouette switch
        {
            BossSilhouette.SnowKing => "boss_snow_king",
            BossSilhouette.CedarSentinel => "boss_cedar_sentinel",
            BossSilhouette.AuroraSerpent => "boss_aurora_serpent",
            BossSilhouette.MistGuardian => "boss_mist_guardian",
            BossSilhouette.CoralKraken => "boss_coral_kraken",
            BossSilhouette.StormEagle => "boss_storm_eagle",
            _ => null,
        };

        private static float RiggedTargetHeight(BossSilhouette silhouette) => silhouette switch
        {
            BossSilhouette.SnowKing => 3.9f,
            BossSilhouette.CedarSentinel => 3.6f,
            BossSilhouette.AuroraSerpent => 3.4f,
            BossSilhouette.MistGuardian => 3.3f,
            BossSilhouette.CoralKraken => 3.4f,
            BossSilhouette.StormEagle => 3.2f,
            _ => 3.5f,
        };

        private bool TryBuildRiggedModel(BossDefinition def, Transform parent, out Renderer mainRenderer)
        {
            mainRenderer = null;
            var modelName = RiggedModelName(def.Silhouette);
            if (string.IsNullOrEmpty(modelName)) return false;

            var prefab = Resources.Load<GameObject>($"PenguinRun/Models/Bosses/{modelName}");
            if (prefab == null) return false;

            var model = Object.Instantiate(prefab, parent, false);
            model.name = "RiggedModel";
            model.transform.localPosition = Vector3.zero;
            // Quaternius 模型出厂面朝 +Z；body 容器基础偏航 180°，模型局部保持 0° 时面部正对玩家（已用预览图核对）
            model.transform.localRotation = Quaternion.identity;

            var renderers = model.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0)
            {
                Object.Destroy(model);
                return false;
            }

            // 体型归一：按网格绑定姿态包围盒把模型高度缩放到目标身高，脚底对齐跑道地面（body 原点离地 1.2）。
            // 不能用 Renderer.bounds：实例化当帧蒙皮包围盒尚未更新，会得到错误尺寸。
            if (!TryComputeMeshBounds(model.transform, parent, out var localBounds))
            {
                Object.Destroy(model);
                return false;
            }

            var scale = RiggedTargetHeight(def.Silhouette) / Mathf.Max(0.01f, localBounds.size.y);
            model.transform.localScale = Vector3.one * scale;
            model.transform.localPosition = new Vector3(0f, -1.2f - localBounds.min.y * scale, 0f);

            foreach (var r in renderers)
            {
                if (r is SkinnedMeshRenderer smr)
                {
                    smr.updateWhenOffscreen = true;
                    if (mainRenderer == null) mainRenderer = r;
                }
                tintRenderers.Add(r);
            }
            mainRenderer ??= renderers[0];

            rigAnimator.Bind(model);
            rigAnimator.PlayMove();
            return true;
        }

        /// <summary>在 parent 局部空间（模型未缩放前）用网格数据求绑定姿态包围盒。</summary>
        private static bool TryComputeMeshBounds(Transform model, Transform parent, out Bounds localBounds)
        {
            var bounds = default(Bounds);
            var hasAny = false;
            var toParent = parent.worldToLocalMatrix;

            foreach (var smr in model.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                EncapsulateMesh(smr.sharedMesh, smr.transform, toParent, ref bounds, ref hasAny);
            foreach (var mf in model.GetComponentsInChildren<MeshFilter>(true))
                EncapsulateMesh(mf.sharedMesh, mf.transform, toParent, ref bounds, ref hasAny);

            localBounds = bounds;
            return hasAny && localBounds.size.y > 0.001f;
        }

        private static void EncapsulateMesh(Mesh mesh, Transform owner, Matrix4x4 toParent, ref Bounds bounds, ref bool hasAny)
        {
            if (mesh == null) return;
            var b = mesh.bounds;
            var l2p = toParent * owner.localToWorldMatrix;
            for (var i = 0; i < 8; i++)
            {
                var corner = new Vector3(
                    (i & 1) == 0 ? b.min.x : b.max.x,
                    (i & 2) == 0 ? b.min.y : b.max.y,
                    (i & 4) == 0 ? b.min.z : b.max.z);
                var p = l2p.MultiplyPoint3x4(corner);
                if (!hasAny)
                {
                    bounds = new Bounds(p, Vector3.zero);
                    hasAny = true;
                }
                else
                {
                    bounds.Encapsulate(p);
                }
            }
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

            // 冰核和肩甲建立远距离可辨的冰王剪影；仅为表现件，不参与命中判定。
            var iceCore = AddPrimChild(root, "SnowKingIceCore", PrimitiveType.Sphere,
                new Vector3(0f, 0.38f, -0.96f), Vector3.one * 0.42f, new Color(0.3f, 0.9f, 1f, 0.72f), true);
            var coreRenderer = iceCore.GetComponent<Renderer>();
            if (coreRenderer != null) tintRenderers.Add(coreRenderer);
            for (var side = -1; side <= 1; side += 2)
            {
                var shoulder = AddPrimChild(root, $"SnowKingShoulderIce{side}", PrimitiveType.Cube,
                    new Vector3(side * 1.18f, 1.08f, 0.12f), new Vector3(0.48f, 0.82f, 0.62f),
                    new Color(0.5f, 0.86f, 1f, 0.82f), true);
                shoulder.transform.localRotation = Quaternion.Euler(0f, 0f, -side * 22f);
            }

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
            var heartCore = AddPrimChild(root, "CedarHeartCore", PrimitiveType.Sphere,
                new Vector3(0f, 0.48f, -0.93f), Vector3.one * 0.34f, new Color(0.95f, 0.7f, 0.28f, 0.8f), true);
            var heartRenderer = heartCore.GetComponent<Renderer>();
            if (heartRenderer != null) tintRenderers.Add(heartRenderer);
            for (var side = -1; side <= 1; side += 2)
            {
                var rootShoulder = AddPrimChild(root, $"CedarRootShoulder{side}", PrimitiveType.Capsule,
                    new Vector3(side * 1.18f, 1.16f, 0.12f), new Vector3(0.36f, 0.78f, 0.36f),
                    new Color(0.32f, 0.22f, 0.12f));
                rootShoulder.transform.localRotation = Quaternion.Euler(0f, 0f, side * 28f);
            }
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
            AddPrimChild(root, "AuroraMagnetCore", PrimitiveType.Sphere, new Vector3(0f, 0.75f, -0.94f),
                Vector3.one * 0.42f, new Color(0.3f, 1f, 0.82f, 0.72f), true);
            for (var side = -1; side <= 1; side += 2)
            {
                var fin = AddPrimChild(root, $"AuroraFin{side}", PrimitiveType.Cube,
                    new Vector3(side * 1.02f, 1.26f, -0.1f), new Vector3(0.18f, 0.72f, 0.68f),
                    new Color(0.58f, 0.36f, 1f, 0.68f), true);
                fin.transform.localRotation = Quaternion.Euler(0f, 0f, -side * 34f);
            }

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
            var headGo = new GameObject("Head");
            headGo.transform.SetParent(root, false);
            headGo.transform.localPosition = new Vector3(0f, 1.05f, -0.15f);
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
            for (var side = -1; side <= 1; side += 2)
            {
                var horn = AddPrimChild(root, $"MistCrownHorn{side}", PrimitiveType.Cylinder,
                    new Vector3(side * 0.72f, 2.08f, 0.08f), new Vector3(0.32f, 0.72f, 0.32f),
                    new Color(0.5f, 0.62f, 0.7f, 0.72f), true);
                horn.transform.localRotation = Quaternion.Euler(0f, 0f, -side * 18f);
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
            AddPrimChild(root, "CoralHeartCore", PrimitiveType.Sphere, new Vector3(0f, 0.72f, -1.08f),
                Vector3.one * 0.46f, new Color(1f, 0.42f, 0.62f, 0.75f), true);
            for (var side = -1; side <= 1; side += 2)
            {
                var crest = AddPrimChild(root, $"CoralCrest{side}", PrimitiveType.Cylinder,
                    new Vector3(side * 1.05f, 1.8f, -0.1f), new Vector3(0.18f, 0.62f, 0.18f),
                    new Color(1f, 0.64f, 0.34f, 0.82f), true);
                crest.transform.localRotation = Quaternion.Euler(0f, 0f, -side * 26f);
            }

            var headGo = new GameObject("Head");
            headGo.transform.SetParent(root, false);
            headGo.transform.localPosition = new Vector3(0f, 1.55f, -0.85f);

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
            AddPrimChild(root, "StormCore", PrimitiveType.Sphere, new Vector3(0f, 0.72f, -0.84f),
                Vector3.one * 0.34f, new Color(1f, 0.94f, 0.3f, 0.78f), true);
            for (var side = -1; side <= 1; side += 2)
            {
                var tail = AddPrimChild(root, $"StormTailFeather{side}", PrimitiveType.Cube,
                    new Vector3(side * 0.56f, 0.52f, 1.38f), new Vector3(0.22f, 0.18f, 1.05f),
                    new Color(0.65f, 0.84f, 1f, 0.76f), true);
                tail.transform.localRotation = Quaternion.Euler(0f, side * 16f, side * 12f);
            }

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

        private void SpawnProjectile(float worldX, float flightTime)
        {
            var bossPos = Root.transform.position;
            var go = AddPrim("Projectile", PrimitiveType.Sphere,
                new Vector3(0f, 1.6f, 0f), Vector3.one * 0.55f,
                new Color(Definition.GlowColor.r, Definition.GlowColor.g, Definition.GlowColor.b, 0.95f));
            go.transform.SetParent(Root.transform.parent, false);
            go.transform.position = new Vector3(bossPos.x, bossPos.y + 1.6f, bossPos.z);

            var anim = go.AddComponent<BossProjectileAnim>();
            anim.Init(go.transform, worldX, flightTime, BossSystem.PlayerTelegraphZ,
                TelegraphSurfaceY(BossSystem.PlayerTelegraphZ) + 0.1f);
            anim.SetImpactCallback(lx => patternVfxPlayer.OnSalvoProjectileImpact(lx));
            projectileObjects.Add(go);
        }

        private void CreateTelegraph(float worldX, float lifetime, float radius, Color color,
            bool isDecoy = false, bool flatRing = false)
        {
            var bossPos = Root.transform.position;
            var cylHeight = flatRing ? 0.14f : 0.16f;
            var go = AddPrim(isDecoy ? "TelegraphDecoy" : "Telegraph", PrimitiveType.Cylinder,
                new Vector3(0f, cylHeight * 0.5f, 0f),
                new Vector3(radius, cylHeight, radius),
                color, true);
            go.transform.SetParent(Root.transform.parent, false);
            var trackY = TelegraphSurfaceY(BossSystem.PlayerTelegraphZ) + 0.14f;
            go.transform.position = new Vector3(worldX, trackY, BossSystem.PlayerTelegraphZ);
            telegraphs.Add(new TelegraphMarker
            {
                Go = go,
                Rend = go.GetComponent<Renderer>(),
                Lifetime = lifetime,
                MaxLifetime = lifetime,
                BaseScale = new Vector3(radius, cylHeight, radius),
                WarnColor = color,
                IsDecoy = isDecoy,
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
            var dangerPulse =
                Phase == BossPhase.Active &&
                Pattern != BossPattern.None &&
                Pattern != BossPattern.Vulnerable &&
                IsPatternDangerous;

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

                var telegraphPos = t.Go.transform.position;
                telegraphPos.y = TelegraphSurfaceY(BossSystem.PlayerTelegraphZ) + 0.14f;
                t.Go.transform.position = telegraphPos;

                // 逐渲染器 MPB 覆盖：预警圈共享缓存材质，绝不能写 sharedMaterial。
                var rend = t.Rend;
                if (t.IsDecoy)
                {
                    var fadeStart = patternDangerDelay - 0.2f;
                    var elapsed = GetPatternElapsed();
                    var alphaMul = elapsed >= fadeStart ? Mathf.Clamp01(1f - (elapsed - fadeStart) / 0.25f) : 1f;
                    var pulse = 1f + Mathf.Sin(Time.time * 18f) * 0.1f;
                    t.Go.transform.localScale = t.BaseScale * pulse;
                    var c = t.WarnColor;
                    c.a *= alphaMul * 0.65f;
                    RunnerVisuals.SetRendererColor(rend, c);
                }
                else if (dangerPulse)
                {
                    var pulse = 1f + Mathf.Sin(Time.time * 44f) * 0.32f;
                    t.Go.transform.localScale = t.BaseScale * pulse;
                    RunnerVisuals.SetRendererColor(rend, new Color(1f, 0.08f, 0.08f, 0.9f));
                }
                else
                {
                    var pulse = 1f + Mathf.Sin(Time.time * 22f) * 0.18f;
                    t.Go.transform.localScale = t.BaseScale * pulse;
                    var warn = t.WarnColor;
                    warn.a = Mathf.Max(warn.a, 0.78f);
                    RunnerVisuals.SetRendererColor(rend, warn);
                }

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

        private void UpdateHealthCrystals()
        {
            for (var i = 0; i < healthCrystals.Length; i++)
            {
                if (healthCrystals[i] == null) continue;
                var alive = i < HitsRemaining;
                var renderer = healthCrystalRenderers[i];
                if (renderer != null)
                {
                    // 三颗水晶共享同一缓存材质，必须用逐渲染器 MPB 才能各自变暗。
                    var c = alive
                        ? Definition.GlowColor
                        : new Color(0.25f, 0.28f, 0.32f, 0.55f);
                    var current = RunnerVisuals.GetRendererColor(renderer);
                    RunnerVisuals.SetRendererColor(renderer, Color.Lerp(current, c, 0.2f));
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
            var flashFull = Definition.FullBodyHitFlash;
            for (var i = 0; i < tintRenderers.Count; i++)
            {
                var rend = tintRenderers[i];
                if (rend == null) continue;
                var baseColor = i < tintBaseColors.Count ? tintBaseColors[i] : Color.white;

                Color target;
                if (HitFlashTimer > 0f)
                {
                    // FullBodyHitFlash=true：全部渲染器闪白
                    // FullBodyHitFlash=false：仅 bodyRenderer（第 0 个）闪白，其余渲染器仅轻微提亮
                    var t = HitFlashTimer / BossSystem.HitInvulnerabilitySeconds;
                    var flash = Mathf.PingPong(Time.time * 30f, 1f);
                    var intensity = flashFull ? 0.6f : (i == 0 ? 0.6f : 0.25f);
                    target = Color.Lerp(baseColor, Color.white, flash * intensity * t);
                }
                else if (Phase == BossPhase.Vulnerable)
                {
                    // 破绽期：脉动绿光，频率由 VulnerableShakeFreq 控制
                    var vuFreq = 6f * Definition.VulnerableShakeFreq;
                    var pulse = (Mathf.Sin(Time.time * vuFreq) + 1f) * 0.5f;
                    target = Color.Lerp(baseColor, new Color(0.5f, 1f, 0.7f), pulse * 0.4f);
                }
                else if (HitsRemaining == 1)
                {
                    // 残血狂暴：慢速余烬脉动。与破绽期的绿色明确区分，
                    // 让玩家不用看 HUD 也能读出 Boss 已进入最后阶段（此时攻击间隔已压缩）。
                    var ember = (Mathf.Sin(Time.time * 3.4f) + 1f) * 0.5f;
                    target = Color.Lerp(baseColor, new Color(1f, 0.42f, 0.28f), 0.14f + ember * 0.16f);
                }
                else
                {
                    target = baseColor;
                }
                RunnerVisuals.SetRendererColor(rend, target);
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

    /// <summary>挂在跑道世界空间 Boss FX 上，用于脉冲缩放动画。</summary>
    internal sealed class BossWorldFxMarker : MonoBehaviour
    {
        public Vector3 BaseScale = Vector3.one;
        public float SurfaceYOffset = 0.09f;
    }

    /// <summary>
    /// 投射物的简单抛物线动画：自身向 X 平移并向地面下落，到达地面后销毁。
    /// </summary>
    internal sealed class BossProjectileAnim : MonoBehaviour
    {
        private Transform tr;
        private float targetX;
        private float groundY;
        private float groundZ;
        private float flightTime;
        private float elapsed;
        private Vector3 startPos;
        private Renderer rend;
        private System.Action<float> onImpact;
        private bool impactFired;

        public void Init(Transform t, float worldX, float impactAfterSeconds, float trackZ, float trackSurfaceY)
        {
            tr = t;
            targetX = worldX;
            groundZ = trackZ;
            groundY = trackSurfaceY;
            flightTime = Mathf.Max(0.35f, impactAfterSeconds);
            elapsed = 0f;
            startPos = tr.position;
            rend = tr.GetComponent<Renderer>();
            impactFired = false;
        }

        public void SetImpactCallback(System.Action<float> callback) => onImpact = callback;

        private void Update()
        {
            if (tr == null) return;
            elapsed += Time.deltaTime;
            var t = Mathf.Clamp01(elapsed / flightTime);
            var end = new Vector3(targetX, groundY + 0.35f, groundZ);
            var midY = Mathf.Max(startPos.y, end.y) + 2.2f;
            var a = Vector3.Lerp(startPos, new Vector3(targetX, midY, (startPos.z + groundZ) * 0.5f), t);
            var b = Vector3.Lerp(new Vector3(targetX, midY, (startPos.z + groundZ) * 0.5f), end, t);
            tr.position = t < 0.5f ? a : b;
            tr.Rotate(Vector3.up, 240f * Time.deltaTime);
            tr.Rotate(Vector3.right, 180f * Time.deltaTime);

            if (t >= 0.98f)
            {
                if (!impactFired)
                {
                    impactFired = true;
                    onImpact?.Invoke(targetX);
                }

                if (rend != null)
                {
                    var pulse = 1f + Mathf.Sin(Time.time * 48f) * 0.25f;
                    tr.localScale = Vector3.one * 0.55f * pulse;
                }
            }

            if (elapsed >= flightTime + 0.15f)
                Destroy(gameObject);
        }
    }
}
