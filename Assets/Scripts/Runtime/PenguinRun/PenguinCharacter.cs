using UnityEngine;

namespace PenguinRun
{
    /// <summary>
    /// 玩家角色：单层程序化网格企鹅（CutePenguinMesh）+ 少量配饰与反馈特效。
    /// 动画全部相对绑定基线偏移，跑动朝向 +Z（镜头看到背影），面部朝向由固定 180° 基础偏航实现。
    /// </summary>
    internal sealed class PenguinCharacter
    {
        public Transform Root { get; }

        // ── 网格部件（来自 CutePenguinMesh） ──
        private readonly Transform body;
        private readonly Transform head;
        private readonly Transform leftWing;
        private readonly Transform rightWing;
        private readonly Transform leftFoot;
        private readonly Transform rightFoot;
        private readonly Transform leftPupil;
        private readonly Transform rightPupil;
        private readonly Transform leftCheek;
        private readonly Transform rightCheek;
        private readonly Renderer bodyRenderer;

        // ── 配饰（程序化 primitive，附着于网格部件） ──
        private readonly Transform scarfTail;
        private readonly Transform gogglesBridge;
        private readonly Transform leftLens;
        private readonly Transform rightLens;
        private readonly Transform hatPom;

        // ── 反馈特效 ──
        private readonly Transform shieldAura;
        private readonly Transform dashTrail;
        private readonly Transform magnetHalo;
        private readonly Renderer shieldRenderer;
        private readonly Renderer dashRenderer;
        private readonly Renderer magnetRenderer;
        private readonly Transform invulnerableRing;
        private readonly Renderer invulnerableRenderer;
        private readonly Transform jumpRing;
        private readonly Renderer jumpRingRenderer;
        private readonly Transform slideTrail;
        private readonly Renderer slideTrailRenderer;
        private readonly Transform powerAura;
        private readonly Renderer powerAuraRenderer;
        private readonly Transform[] dizzyStars = new Transform[3];
        private readonly Transform[] buffMotes = new Transform[3];
        private readonly Renderer[] buffMoteRenderers = new Renderer[3];
        private readonly System.Collections.Generic.List<Color> activeAuxColors = new(12);

        // ── 绑定基线 ──
        private readonly Vector3 bodyBasePos;
        private readonly Vector3 headBasePos;
        private readonly Vector3 pupilBaseScale;
        private readonly Vector3 cheekBaseScale;

        /// <summary>面部朝 -Z 建模；固定 180° 偏航让企鹅面向跑动方向（+Z），镜头看到背影。</summary>
        private static readonly Quaternion FacingBase = Quaternion.Euler(0f, 180f, 0f);

        private float bob;
        private float landingPulse;
        private float jumpRingTimer;
        private bool wasGroundedLast = true;
        private bool wasDashActiveLast;
        private float dashBurstTimer;
        private float hurtFlashTimer;
        private float perfectDodgeBurstTimer;
        private float celebrateTimer;
        private static readonly Color DefaultBodyColor = new(0.04f, 0.09f, 0.18f);
        private static readonly Color DashBodyColor = new(0.04f, 0.18f, 0.24f);

        private PenguinCharacter(Transform root, PenguinCharacterParts parts)
        {
            Root = root;
            Root.localRotation = FacingBase;

            body = parts.Body.transform;
            head = parts.Head.transform;
            leftWing = parts.LeftWing.transform;
            rightWing = parts.RightWing.transform;
            leftFoot = parts.LeftFoot.transform;
            rightFoot = parts.RightFoot.transform;
            leftPupil = parts.LeftPupil.transform;
            rightPupil = parts.RightPupil.transform;
            leftCheek = parts.LeftCheek.transform;
            rightCheek = parts.RightCheek.transform;
            bodyRenderer = body.GetComponent<Renderer>();

            bodyBasePos = body.localPosition;
            headBasePos = head.localPosition;
            pupilBaseScale = leftPupil.localScale;
            cheekBaseScale = leftCheek.localScale;

            // 风镜（推到前额发际、眼睛上方，不遮挡大眼睛）
            gogglesBridge = AddPrimitive("Goggles Bridge", PrimitiveType.Cube, head, new Vector3(0f, 0.26f, -0.3f), new Vector3(0.12f, 0.035f, 0.035f), new Color(0.72f, 0.48f, 0.22f)).transform;
            leftLens = AddPrimitive("Left Goggles Lens", PrimitiveType.Cylinder, head, new Vector3(-0.12f, 0.26f, -0.28f), new Vector3(0.11f, 0.018f, 0.11f), new Color(0.16f, 0.72f, 1f, 0.68f)).transform;
            rightLens = AddPrimitive("Right Goggles Lens", PrimitiveType.Cylinder, head, new Vector3(0.12f, 0.26f, -0.28f), new Vector3(0.11f, 0.018f, 0.11f), new Color(0.16f, 0.72f, 1f, 0.68f)).transform;
            leftLens.localRotation = Quaternion.Euler(90f, 0f, 0f);
            rightLens.localRotation = Quaternion.Euler(90f, 0f, 0f);
            AddPrimitive("Left Goggle Rim", PrimitiveType.Cylinder, leftLens, Vector3.zero, new Vector3(1.18f, 0.7f, 1.18f), new Color(0.82f, 0.58f, 0.26f, 0.64f), true);
            AddPrimitive("Right Goggle Rim", PrimitiveType.Cylinder, rightLens, Vector3.zero, new Vector3(1.18f, 0.7f, 1.18f), new Color(0.82f, 0.58f, 0.26f, 0.64f), true);

            // 帽顶毛球（挂在网格帽子上）
            hatPom = AddPrimitive("Hat Pom", PrimitiveType.Sphere, parts.Hat.transform, new Vector3(0.1f, 0.26f, 0.02f), Vector3.one * 0.16f, new Color(0.88f, 0.92f, 0.98f)).transform;

            // 围巾尾（挂在网格围巾上，向背后 +Z 侧飘动——镜头可见）
            scarfTail = AddPrimitive("Scarf Tail", PrimitiveType.Cube, parts.Scarf.transform, new Vector3(-0.18f, -0.3f, 0.26f), new Vector3(0.15f, 0.5f, 0.08f), new Color(0.82f, 0.2f, 0.24f)).transform;
            AddPrimitive("Scarf Fringe 1", PrimitiveType.Cube, scarfTail, new Vector3(-0.045f, -0.56f, 0f), new Vector3(0.035f, 0.12f, 0.08f), new Color(0.72f, 0.16f, 0.2f));
            AddPrimitive("Scarf Fringe 2", PrimitiveType.Cube, scarfTail, new Vector3(0.045f, -0.56f, 0f), new Vector3(0.035f, 0.12f, 0.08f), new Color(0.72f, 0.16f, 0.2f));

            // 反馈特效件
            shieldAura = AddPrimitive("Shield Aura", PrimitiveType.Sphere, root, new Vector3(0f, 0.9f, 0f), new Vector3(1.45f, 1.65f, 1.45f), new Color(0.42f, 0.9f, 1f, 0.28f)).transform;
            shieldRenderer = shieldAura.GetComponent<Renderer>();
            dashTrail = AddPrimitive("Dash Trail", PrimitiveType.Cube, root, new Vector3(0f, 0.62f, 0.8f), new Vector3(0.9f, 0.2f, 1.7f), new Color(0.34f, 1f, 0.82f, 0.24f)).transform;
            dashRenderer = dashTrail.GetComponent<Renderer>();
            magnetHalo = AddPrimitive("Magnet Halo", PrimitiveType.Cylinder, root, new Vector3(0f, 1.05f, 0f), new Vector3(1.18f, 0.025f, 1.18f), new Color(0.42f, 0.62f, 1f, 0.24f)).transform;
            magnetRenderer = magnetHalo.GetComponent<Renderer>();
            invulnerableRing = AddPrimitive("Invulnerable Ring", PrimitiveType.Cylinder, root, new Vector3(0f, 0.22f, 0f), new Vector3(1.34f, 0.03f, 1.34f), new Color(1f, 0.58f, 0.44f, 0.42f)).transform;
            invulnerableRenderer = invulnerableRing.GetComponent<Renderer>();
            jumpRing = AddPrimitive("Jump Ring", PrimitiveType.Cylinder, root, new Vector3(0f, 0.05f, 0f), new Vector3(1.6f, 0.04f, 1.6f), new Color(0.7f, 1f, 0.95f, 0f)).transform;
            jumpRingRenderer = jumpRing.GetComponent<Renderer>();
            slideTrail = AddPrimitive("Slide Trail", PrimitiveType.Cube, root, new Vector3(0f, 0.04f, 0.6f), new Vector3(0.85f, 0.06f, 1.4f), new Color(0.5f, 0.95f, 1f, 0f)).transform;
            slideTrailRenderer = slideTrail.GetComponent<Renderer>();
            powerAura = AddPrimitive("Power Aura", PrimitiveType.Sphere, root, new Vector3(0f, 0.85f, 0f), new Vector3(1.55f, 1.7f, 1.55f), new Color(1f, 0.78f, 0.45f, 0f)).transform;
            powerAuraRenderer = powerAura.GetComponent<Renderer>();

            // 死亡演出：头顶晕星
            for (var i = 0; i < dizzyStars.Length; i++)
            {
                dizzyStars[i] = AddPrimitive($"Dizzy Star {i}", PrimitiveType.Cube, head,
                    Vector3.up * 0.7f, Vector3.one * 0.1f, new Color(1f, 0.9f, 0.35f, 0.95f), true).transform;
                dizzyStars[i].localRotation = Quaternion.Euler(0f, 0f, 45f);
                dizzyStars[i].gameObject.SetActive(false);
            }

            // 辅助 buff 环绕光珠：滑翔/星光/沙漏/冰镜/雾灯等激活时按色环绕
            for (var i = 0; i < buffMotes.Length; i++)
            {
                var mote = AddPrimitive($"Buff Mote {i}", PrimitiveType.Sphere, root,
                    new Vector3(0f, 0.9f, 0f), Vector3.one * 0.12f, new Color(1f, 1f, 1f, 0.85f), true);
                buffMotes[i] = mote.transform;
                buffMoteRenderers[i] = mote.GetComponent<Renderer>();
                mote.SetActive(false);
            }

            SetTransparent(shieldRenderer);
            SetTransparent(dashRenderer);
            SetTransparent(magnetRenderer);
            SetTransparent(invulnerableRenderer);
            SetTransparent(jumpRingRenderer);
            SetTransparent(slideTrailRenderer);
            SetTransparent(powerAuraRenderer);
            invulnerableRing.gameObject.SetActive(false);
            jumpRing.gameObject.SetActive(false);
            slideTrail.gameObject.SetActive(false);
            powerAura.gameObject.SetActive(false);
        }

        /// <summary>使用程序化 Mesh 生成的可爱卡通企鹅（唯一创建入口，单层网格）。</summary>
        public static PenguinCharacter CreateCute(Transform parent)
        {
            var rootGo = CutePenguinMesh.Create(parent, out var parts);
            return new PenguinCharacter(rootGo.transform, parts);
        }

        public void TriggerHurtFlash() => hurtFlashTimer = 0.15f;

        public void TriggerPerfectDodgeBurst()
        {
            perfectDodgeBurstTimer = 0.55f;
            jumpRingTimer = 0.55f;
        }

        /// <summary>庆祝演出（Boss 击败等高光时刻）：欢呼张翅 + 弹跳。</summary>
        public void TriggerCelebration() => celebrateTimer = 1.4f;

        /// <summary>
        /// 采集本帧的辅助 buff 状态（滑翔/星光/沙漏/双倍/冰镜/极光连锁/雾灯/雷羽/树甲/珊瑚/鱼弹），
        /// 供 Animate 中的环绕光珠展示；主 buff（冲刺/磁力/护盾/无敌）仍走原有专属特效。
        /// </summary>
        public void SetAuxBuffState(WorldDirector world)
        {
            activeAuxColors.Clear();
            if (world == null) return;
            if (world.GlideTimer > 0f) activeAuxColors.Add(new Color(0.85f, 0.95f, 1f));
            if (world.ScoreBoostTimer > 0f) activeAuxColors.Add(new Color(1f, 0.58f, 0.9f));
            if (world.SlowMoTimer > 0f) activeAuxColors.Add(new Color(0.72f, 0.95f, 1f));
            if (world.DoubleFishTimer > 0f) activeAuxColors.Add(new Color(1f, 0.72f, 0.38f));
            if (world.IceMirrorTimer > 0f) activeAuxColors.Add(new Color(0.75f, 0.95f, 1f));
            if (world.AuroraChainTimer > 0f) activeAuxColors.Add(new Color(0.55f, 0.25f, 1f));
            if (world.FogLanternTimer > 0f) activeAuxColors.Add(new Color(1f, 0.92f, 0.55f));
            if (world.ThunderFeatherTimer > 0f) activeAuxColors.Add(new Color(1f, 0.95f, 0.2f));
            if (world.TreantArmorHits > 0) activeAuxColors.Add(new Color(0.38f, 0.72f, 0.32f));
            if (world.CoralBounceReady) activeAuxColors.Add(new Color(1f, 0.35f, 0.65f));
            if (world.FishBombs > 0) activeAuxColors.Add(new Color(1f, 0.42f, 0.3f));
        }

        public void Animate(float dt, bool running, bool grounded, bool sliding, bool gameOver, float speed01, float laneLean, float dashTimer, float magnetTimer, float shieldTimer, float invulnerabilityTimer)
        {
            var dashActive = dashTimer > 0f;
            if (dashActive && !wasDashActiveLast)
            {
                dashBurstTimer = 0.22f;
            }
            dashBurstTimer = Mathf.Max(0f, dashBurstTimer - dt);
            hurtFlashTimer = Mathf.Max(0f, hurtFlashTimer - dt);
            perfectDodgeBurstTimer = Mathf.Max(0f, perfectDodgeBurstTimer - dt);
            celebrateTimer = Mathf.Max(0f, celebrateTimer - dt);

            bob += dt * Mathf.Lerp(4.2f, 11.5f, speed01);
            var runWave = Mathf.Sin(bob);
            var idleWave = Mathf.Sin(Time.time * 2.4f);
            var bounce = running && grounded ? Mathf.Abs(runWave) * 0.06f : idleWave * 0.025f;
            var celebrating = !gameOver && celebrateTimer > 0f;
            if (celebrating)
            {
                bounce += Mathf.Abs(Mathf.Sin(Time.time * 9f)) * 0.12f;
            }
            var targetScale = sliding ? new Vector3(1.14f, 0.6f, 1.18f) : Vector3.one;
            var blink = Mathf.PingPong(Time.time * 0.42f, 1f) > 0.94f ? 0.22f : 1f;
            var invulnerable = invulnerabilityTimer > 0f;
            var invulnPulse = invulnerable ? 1f + Mathf.Sin(Time.time * 18f) * 0.035f : 1f;
            var invulnWobble = invulnerable ? Mathf.Sin(Time.time * 30f) * 4f : 0f;

            if (dashBurstTimer > 0f)
            {
                var burst = dashBurstTimer / 0.22f;
                targetScale = Vector3.Lerp(targetScale, new Vector3(1.18f, 0.9f, 1.28f), burst * 0.6f);
            }
            Root.localScale = Vector3.Lerp(Root.localScale, targetScale * invulnPulse, Mathf.Clamp01(dt * 12f));
            var dashLean = dashActive ? Mathf.Lerp(8f, 18f, Mathf.Clamp01(speed01 + 0.25f)) : 0f;
            if (dashBurstTimer > 0f)
            {
                dashLean += (dashBurstTimer / 0.22f) * 10f;
            }

            // 局部俯仰在 FacingBase(180° 偏航) 下与世界方向相反：负值=向跑动方向前倾，正值=向镜头后仰
            var forwardLean = (sliding ? 8f : 0f) + dashLean;
            var defeatPitch = gameOver ? 58f : 0f; // 仰面倒下（面向镜头）
            var celebratePitch = celebrating ? 8f + Mathf.Sin(Time.time * 9f) * 4f : 0f;

            Root.localRotation = Quaternion.Lerp(
                Root.localRotation,
                FacingBase * Quaternion.Euler(
                    -forwardLean + defeatPitch + celebratePitch,
                    -laneLean * 5f,
                    laneLean * 11f + invulnWobble),
                Mathf.Clamp01(dt * (gameOver ? 4f : 10f)));
            body.localPosition = Vector3.Lerp(body.localPosition, bodyBasePos + new Vector3(0f, bounce, 0f), Mathf.Clamp01(dt * 10f));
            head.localPosition = Vector3.Lerp(head.localPosition, headBasePos + new Vector3(0f, bounce * 0.8f, 0f), Mathf.Clamp01(dt * 10f));

            var wingSwing = running ? runWave * 36f : 28f + idleWave * 18f;
            if (!grounded)
            {
                wingSwing = 38f;
            }
            if (sliding)
            {
                wingSwing = 78f;
            }
            if (dashActive && !sliding)
            {
                wingSwing = Mathf.Lerp(wingSwing, -14f + Mathf.Sin(Time.time * 30f) * 6f, 0.72f);
            }
            if (celebrating)
            {
                wingSwing = 82f + Mathf.Sin(Time.time * 16f) * 10f;
            }
            if (gameOver)
            {
                wingSwing = -24f;
            }

            leftWing.localRotation = Quaternion.Lerp(leftWing.localRotation, Quaternion.Euler(0f, 0f, 18f + wingSwing), Mathf.Clamp01(dt * 14f));
            rightWing.localRotation = Quaternion.Lerp(rightWing.localRotation, Quaternion.Euler(0f, 0f, -18f - wingSwing), Mathf.Clamp01(dt * 14f));
            leftFoot.localRotation = Quaternion.Euler(running && !gameOver ? runWave * 18f : 0f, 0f, 0f);
            rightFoot.localRotation = Quaternion.Euler(running && !gameOver ? -runWave * 18f : 0f, 0f, 0f);
            scarfTail.localRotation = Quaternion.Euler(-14f - speed01 * 22f - Mathf.Sin(Time.time * 5f) * 8f, 0f, 8f + Mathf.Sin(Time.time * 4.2f) * 6f);
            gogglesBridge.localRotation = Quaternion.Euler(0f, Mathf.Sin(Time.time * 1.7f) * 3f, 0f);
            hatPom.localScale = Vector3.one * (0.16f + Mathf.Sin(Time.time * 3.5f) * 0.014f);
            leftPupil.localScale = Vector3.Scale(pupilBaseScale, new Vector3(1f, blink, 1f));
            rightPupil.localScale = Vector3.Scale(pupilBaseScale, new Vector3(1f, blink, 1f));
            var cheekPulse = 1f + Mathf.Sin(Time.time * 4.5f) * 0.06f;
            leftCheek.localScale = cheekBaseScale * cheekPulse;
            rightCheek.localScale = cheekBaseScale * cheekPulse;

            var baseBodyColor = dashTimer > 0f ? DashBodyColor : DefaultBodyColor;
            var invulnMix = invulnerable ? (0.5f + 0.5f * Mathf.Sin(Time.time * 24f)) : 0f;
            var hurtMix = hurtFlashTimer > 0f ? hurtFlashTimer / 0.15f : 0f;
            var targetBodyColor = Color.Lerp(baseBodyColor, new Color(1f, 0.55f, 0.48f), invulnMix * 0.65f);
            targetBodyColor = Color.Lerp(targetBodyColor, new Color(1f, 0.25f, 0.22f), hurtMix * 0.85f);
            if (gameOver)
            {
                targetBodyColor = Color.Lerp(targetBodyColor, new Color(0.16f, 0.18f, 0.26f), 0.5f);
            }
            var bodyColor = Color.Lerp(
                RunnerVisuals.GetColor(bodyRenderer.material),
                targetBodyColor,
                Mathf.Clamp01(dt * 6f));
            RunnerVisuals.SetColor(bodyRenderer.material, bodyColor);

            shieldAura.gameObject.SetActive(shieldTimer > 0f);
            if (shieldTimer > 0f)
            {
                var pulse = 1f + Mathf.Sin(Time.time * 8f) * 0.05f;
                shieldAura.localScale = new Vector3(1.45f, 1.65f, 1.45f) * pulse;
            }

            dashTrail.gameObject.SetActive(dashActive);
            if (dashActive)
            {
                var pulse = 1f + Mathf.Sin(Time.time * 34f) * 0.16f;
                dashTrail.localScale = new Vector3(0.9f + speed01 * 0.5f, 0.2f, (1.7f + speed01 * 1.1f) * pulse);
                dashTrail.localPosition = new Vector3(0f, 0.62f, 0.95f + Mathf.Sin(Time.time * 26f) * 0.08f);
                var trailColor = RunnerVisuals.GetColor(dashRenderer.material);
                trailColor.a = 0.22f + Mathf.Abs(Mathf.Sin(Time.time * 24f)) * 0.34f;
                RunnerVisuals.SetColor(dashRenderer.material, trailColor);
            }

            magnetHalo.gameObject.SetActive(magnetTimer > 0f);
            if (magnetTimer > 0f)
            {
                magnetHalo.localRotation = Quaternion.Euler(0f, Time.time * 160f, 0f);
                var halo = 1f + Mathf.Sin(Time.time * 5.5f) * 0.08f;
                magnetHalo.localScale = new Vector3(1.18f, 0.025f, 1.18f) * halo;
            }

            invulnerableRing.gameObject.SetActive(invulnerable);
            if (invulnerable)
            {
                invulnerableRing.localRotation = Quaternion.Euler(0f, Time.time * 280f, 0f);
                var ringPulse = 1f + Mathf.Sin(Time.time * 14f) * 0.18f;
                invulnerableRing.localScale = new Vector3(1.34f, 0.03f, 1.34f) * ringPulse;
                var ringColor = RunnerVisuals.GetColor(invulnerableRenderer.material);
                ringColor.a = 0.22f + Mathf.Abs(Mathf.Sin(Time.time * 16f)) * 0.35f;
                RunnerVisuals.SetColor(invulnerableRenderer.material, ringColor);
            }

            // ── 跳跃 / 着陆 / 滑铲 / 通用 buff 视觉反馈 ─────────
            if (wasGroundedLast && !grounded)
            {
                jumpRingTimer = 0.45f;
            }
            if (!wasGroundedLast && grounded)
            {
                landingPulse = 0.35f;
            }

            jumpRingTimer = Mathf.Max(0f, jumpRingTimer - dt);
            landingPulse = Mathf.Max(0f, landingPulse - dt);

            jumpRing.gameObject.SetActive(jumpRingTimer > 0f);
            if (jumpRingTimer > 0f)
            {
                var ringDuration = perfectDodgeBurstTimer > 0f ? 0.55f : 0.45f;
                var t = 1f - jumpRingTimer / ringDuration;
                var baseScale = perfectDodgeBurstTimer > 0f ? 2.2f : 1.6f;
                jumpRing.localScale = new Vector3(baseScale + t * 1.4f, 0.04f, baseScale + t * 1.4f);
                var rc = RunnerVisuals.GetColor(jumpRingRenderer.material);
                rc = perfectDodgeBurstTimer > 0f
                    ? Color.Lerp(new Color(1f, 0.85f, 0.35f, 0.85f), rc, t)
                    : rc;
                rc.a = (perfectDodgeBurstTimer > 0f ? 0.75f : 0.6f) * (1f - t);
                RunnerVisuals.SetColor(jumpRingRenderer.material, rc);
            }

            if (landingPulse > 0f)
            {
                var t = landingPulse / 0.35f;
                var squish = 1f - t * 0.12f;
                var current = Root.localScale;
                Root.localScale = new Vector3(current.x * 1.04f, current.y * squish, current.z * 1.04f);
            }

            slideTrail.gameObject.SetActive(sliding);
            if (sliding)
            {
                var c = RunnerVisuals.GetColor(slideTrailRenderer.material);
                c.a = Mathf.Lerp(c.a, 0.65f, Mathf.Clamp01(dt * 8f));
                RunnerVisuals.SetColor(slideTrailRenderer.material, c);
                slideTrail.localScale = new Vector3(0.85f + Mathf.Sin(Time.time * 18f) * 0.06f, 0.06f, 1.4f + speed01 * 0.6f);
            }

            var anyBuff = dashActive || magnetTimer > 0f;
            powerAura.gameObject.SetActive(anyBuff);
            if (anyBuff)
            {
                var aurac = RunnerVisuals.GetColor(powerAuraRenderer.material);
                Color targetCol;
                if (dashActive)
                    targetCol = new Color(0.4f, 1f, 0.85f, 0.16f + Mathf.Sin(Time.time * 8f) * 0.06f);
                else
                    targetCol = new Color(0.5f, 0.7f, 1f, 0.14f + Mathf.Sin(Time.time * 6f) * 0.05f);
                aurac = Color.Lerp(aurac, targetCol, Mathf.Clamp01(dt * 6f));
                RunnerVisuals.SetColor(powerAuraRenderer.material, aurac);
                powerAura.localRotation = Quaternion.Euler(0f, Time.time * 90f, 0f);
            }

            // 死亡演出：头顶晕星环绕
            var dizzyActive = gameOver;
            for (var i = 0; i < dizzyStars.Length; i++)
            {
                var star = dizzyStars[i];
                if (star == null) continue;
                star.gameObject.SetActive(dizzyActive);
                if (!dizzyActive) continue;
                var a = Time.time * 3.4f + i * Mathf.PI * 2f / dizzyStars.Length;
                star.localPosition = new Vector3(Mathf.Cos(a) * 0.4f, 0.62f + Mathf.Sin(Time.time * 2f + i) * 0.05f, Mathf.Sin(a) * 0.4f);
                star.localRotation = Quaternion.Euler(0f, a * Mathf.Rad2Deg, 45f);
            }

            // 辅助 buff 环绕光珠：按激活 buff 颜色环绕机体
            var moteCount = gameOver ? 0 : Mathf.Min(buffMotes.Length, activeAuxColors.Count);
            for (var i = 0; i < buffMotes.Length; i++)
            {
                var mote = buffMotes[i];
                if (mote == null) continue;
                var on = i < moteCount;
                mote.gameObject.SetActive(on);
                if (!on) continue;
                var a = Time.time * 2.6f + i * Mathf.PI * 2f / Mathf.Max(1, moteCount);
                mote.localPosition = new Vector3(Mathf.Cos(a) * 0.82f, 0.92f + Mathf.Sin(Time.time * 3.1f + i * 1.7f) * 0.16f, Mathf.Sin(a) * 0.82f);
                var col = activeAuxColors[i % activeAuxColors.Count];
                col.a = 0.6f + Mathf.Sin(Time.time * 7f + i) * 0.2f;
                RunnerVisuals.SetColor(buffMoteRenderers[i].material, col);
            }

            wasGroundedLast = grounded;
            wasDashActiveLast = dashActive;
        }

        private static GameObject AddPrimitive(string name, PrimitiveType type, Transform parent, Vector3 localPosition, Vector3 localScale, Color color, bool transparent = false)
        {
            var go = RunnerVisuals.CreatePrimitive(name, type, Vector3.zero, localScale, color, transparent || color.a < 0.99f);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPosition;
            go.transform.localScale = localScale;
            return go;
        }

        private static void SetTransparent(Renderer renderer)
        {
            var color = RunnerVisuals.GetColor(renderer.material);
            RunnerVisuals.ApplyColor(renderer, color, true);
        }
    }
}
