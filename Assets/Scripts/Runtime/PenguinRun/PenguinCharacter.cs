using UnityEngine;

namespace PenguinRun
{
    internal sealed class PenguinCharacter
    {
        public Transform Root { get; }

        private readonly Transform body;
        private readonly Transform head;
        private readonly Transform leftWing;
        private readonly Transform rightWing;
        private readonly Transform leftFoot;
        private readonly Transform rightFoot;
        private readonly Transform scarfTail;
        private readonly Transform gogglesBridge;
        private readonly Transform leftLens;
        private readonly Transform rightLens;
        private readonly Transform hatPom;
        private readonly Transform leftPupil;
        private readonly Transform rightPupil;
        private readonly Transform leftCheek;
        private readonly Transform rightCheek;
        private readonly Transform shieldAura;
        private readonly Transform dashTrail;
        private readonly Transform magnetHalo;
        private readonly Renderer bodyRenderer;
        private readonly Renderer shieldRenderer;
        private readonly Renderer dashRenderer;
        private readonly Renderer magnetRenderer;
        private readonly Transform invulnerableRing;
        private readonly Renderer invulnerableRenderer;

        // 新增：呼吸动画 / 着陆冲击 / 跳跃环 / 滑铲尾迹
        private readonly Transform jumpRing;
        private readonly Renderer jumpRingRenderer;
        private readonly Transform slideTrail;
        private readonly Renderer slideTrailRenderer;
        private readonly Transform powerAura;
        private readonly Renderer powerAuraRenderer;

        private float bob;
        // 着陆冲击：刚落地时短暂触发，让脚部有"啪"的反馈
        private float landingPulse;
        // 跳跃环：跳起瞬间生成的环波
        private float jumpRingTimer;
        // 用于检测着陆 / 起跳瞬间的状态切换
        private bool wasGroundedLast = true;
        private bool wasDashActiveLast;
        private float dashBurstTimer;

        private PenguinCharacter(Transform root)
        {
            Root = root;
            body = AddPrimitive("Body", PrimitiveType.Capsule, root, new Vector3(0f, 0.78f, 0f), new Vector3(0.9f, 1.08f, 0.84f), new Color(0.035f, 0.095f, 0.17f)).transform;
            bodyRenderer = body.GetComponent<Renderer>();
            AddPrimitive("Body Blue Rim", PrimitiveType.Capsule, body, new Vector3(0f, 0.02f, -0.48f), new Vector3(0.78f, 0.92f, 0.18f), new Color(0.055f, 0.22f, 0.36f));
            head = AddPrimitive("Head", PrimitiveType.Sphere, root, new Vector3(0f, 1.55f, 0.02f), new Vector3(0.8f, 0.7f, 0.72f), new Color(0.035f, 0.085f, 0.16f)).transform;
            AddPrimitive("Face Patch", PrimitiveType.Sphere, head, new Vector3(0f, 0.01f, -0.56f), new Vector3(0.66f, 0.56f, 0.18f), new Color(0.92f, 0.97f, 0.99f));
            AddPrimitive("Belly", PrimitiveType.Sphere, body, new Vector3(0f, -0.04f, -0.58f), new Vector3(0.72f, 0.94f, 0.18f), new Color(0.92f, 0.97f, 0.99f));

            AddPrimitive("Left Eye", PrimitiveType.Sphere, head, new Vector3(-0.2f, 0.1f, -0.66f), Vector3.one * 0.12f, Color.white);
            AddPrimitive("Right Eye", PrimitiveType.Sphere, head, new Vector3(0.2f, 0.1f, -0.66f), Vector3.one * 0.12f, Color.white);
            AddPrimitive("Left Iris", PrimitiveType.Sphere, head, new Vector3(-0.2f, 0.1f, -0.74f), Vector3.one * 0.078f, new Color(0.08f, 0.56f, 0.95f));
            AddPrimitive("Right Iris", PrimitiveType.Sphere, head, new Vector3(0.2f, 0.1f, -0.74f), Vector3.one * 0.078f, new Color(0.08f, 0.56f, 0.95f));
            leftPupil = AddPrimitive("Left Pupil", PrimitiveType.Sphere, head, new Vector3(-0.2f, 0.095f, -0.8f), Vector3.one * 0.038f, new Color(0.02f, 0.04f, 0.08f)).transform;
            rightPupil = AddPrimitive("Right Pupil", PrimitiveType.Sphere, head, new Vector3(0.2f, 0.095f, -0.8f), Vector3.one * 0.038f, new Color(0.02f, 0.04f, 0.08f)).transform;
            AddPrimitive("Left Eye Spark", PrimitiveType.Sphere, head, new Vector3(-0.23f, 0.14f, -0.84f), Vector3.one * 0.026f, Color.white);
            AddPrimitive("Right Eye Spark", PrimitiveType.Sphere, head, new Vector3(0.17f, 0.14f, -0.84f), Vector3.one * 0.026f, Color.white);
            leftCheek = AddPrimitive("Left Cheek", PrimitiveType.Sphere, head, new Vector3(-0.32f, -0.1f, -0.7f), new Vector3(0.13f, 0.065f, 0.034f), new Color(1f, 0.52f, 0.48f)).transform;
            rightCheek = AddPrimitive("Right Cheek", PrimitiveType.Sphere, head, new Vector3(0.32f, -0.1f, -0.7f), new Vector3(0.13f, 0.065f, 0.034f), new Color(1f, 0.52f, 0.48f)).transform;
            AddPrimitive("Beak Upper", PrimitiveType.Cube, head, new Vector3(0f, -0.04f, -0.84f), new Vector3(0.28f, 0.09f, 0.2f), new Color(1f, 0.62f, 0.2f));
            AddPrimitive("Beak Smile", PrimitiveType.Cube, head, new Vector3(0f, -0.115f, -0.86f), new Vector3(0.18f, 0.04f, 0.14f), new Color(0.72f, 0.26f, 0.16f));

            var hat = AddPrimitive("Blue Knit Hat", PrimitiveType.Sphere, head, new Vector3(0f, 0.35f, 0.02f), new Vector3(0.82f, 0.34f, 0.74f), new Color(0.04f, 0.34f, 0.78f));
            AddPrimitive("Hat Rib Left", PrimitiveType.Cube, hat.transform, new Vector3(-0.22f, -0.02f, -0.36f), new Vector3(0.045f, 0.24f, 0.05f), new Color(0.07f, 0.44f, 0.88f));
            AddPrimitive("Hat Rib Center", PrimitiveType.Cube, hat.transform, new Vector3(0f, -0.02f, -0.38f), new Vector3(0.045f, 0.26f, 0.05f), new Color(0.07f, 0.44f, 0.88f));
            AddPrimitive("Hat Rib Right", PrimitiveType.Cube, hat.transform, new Vector3(0.22f, -0.02f, -0.36f), new Vector3(0.045f, 0.24f, 0.05f), new Color(0.07f, 0.44f, 0.88f));
            AddPrimitive("Hat Brim", PrimitiveType.Cube, head, new Vector3(0f, 0.23f, -0.66f), new Vector3(0.82f, 0.11f, 0.14f), new Color(0.03f, 0.24f, 0.62f));
            hatPom = AddPrimitive("Hat Pom", PrimitiveType.Sphere, head, new Vector3(0.22f, 0.62f, 0.03f), Vector3.one * 0.18f, new Color(0.88f, 0.92f, 0.98f)).transform;

            gogglesBridge = AddPrimitive("Goggles Bridge", PrimitiveType.Cube, head, new Vector3(0f, 0.31f, -0.76f), new Vector3(0.18f, 0.055f, 0.06f), new Color(0.72f, 0.48f, 0.22f)).transform;
            leftLens = AddPrimitive("Left Goggles Lens", PrimitiveType.Cylinder, head, new Vector3(-0.25f, 0.31f, -0.75f), new Vector3(0.32f, 0.04f, 0.32f), new Color(0.16f, 0.72f, 1f, 0.68f)).transform;
            rightLens = AddPrimitive("Right Goggles Lens", PrimitiveType.Cylinder, head, new Vector3(0.25f, 0.31f, -0.75f), new Vector3(0.32f, 0.04f, 0.32f), new Color(0.16f, 0.72f, 1f, 0.68f)).transform;
            leftLens.localRotation = Quaternion.Euler(90f, 0f, 0f);
            rightLens.localRotation = Quaternion.Euler(90f, 0f, 0f);
            AddPrimitive("Left Goggle Rim", PrimitiveType.Cylinder, leftLens, Vector3.zero, new Vector3(1.18f, 0.7f, 1.18f), new Color(0.82f, 0.58f, 0.26f, 0.64f), true);
            AddPrimitive("Right Goggle Rim", PrimitiveType.Cylinder, rightLens, Vector3.zero, new Vector3(1.18f, 0.7f, 1.18f), new Color(0.82f, 0.58f, 0.26f, 0.64f), true);

            var scarf = AddPrimitive("Scarf Wrap", PrimitiveType.Cube, root, new Vector3(0f, 1.17f, -0.62f), new Vector3(1.02f, 0.17f, 0.18f), new Color(0.08f, 0.5f, 0.9f));
            AddPrimitive("Scarf Snowflake Pin", PrimitiveType.Sphere, scarf.transform, new Vector3(0.22f, 0.02f, -0.58f), Vector3.one * 0.05f, new Color(0.86f, 0.98f, 1f));
            scarfTail = AddPrimitive("Scarf Tail", PrimitiveType.Cube, scarf.transform, new Vector3(-0.5f, -0.12f, -0.12f), new Vector3(0.18f, 0.58f, 0.1f), new Color(0.1f, 0.56f, 0.95f)).transform;
            AddPrimitive("Scarf Fringe 1", PrimitiveType.Cube, scarfTail, new Vector3(-0.045f, -0.56f, 0f), new Vector3(0.035f, 0.12f, 0.08f), new Color(0.09f, 0.46f, 0.82f));
            AddPrimitive("Scarf Fringe 2", PrimitiveType.Cube, scarfTail, new Vector3(0.045f, -0.56f, 0f), new Vector3(0.035f, 0.12f, 0.08f), new Color(0.09f, 0.46f, 0.82f));

            leftWing = AddPrimitive("Left Wing", PrimitiveType.Capsule, root, new Vector3(-0.58f, 0.84f, -0.02f), new Vector3(0.23f, 0.78f, 0.18f), new Color(0.03f, 0.055f, 0.1f)).transform;
            rightWing = AddPrimitive("Right Wing", PrimitiveType.Capsule, root, new Vector3(0.58f, 0.84f, -0.02f), new Vector3(0.23f, 0.78f, 0.18f), new Color(0.03f, 0.055f, 0.1f)).transform;
            AddPrimitive("Left Wing Highlight", PrimitiveType.Capsule, leftWing, new Vector3(0f, -0.06f, -0.36f), new Vector3(0.42f, 0.82f, 0.28f), new Color(0.055f, 0.09f, 0.15f));
            AddPrimitive("Right Wing Highlight", PrimitiveType.Capsule, rightWing, new Vector3(0f, -0.06f, -0.36f), new Vector3(0.42f, 0.82f, 0.28f), new Color(0.055f, 0.09f, 0.15f));
            leftFoot = AddPrimitive("Left Foot", PrimitiveType.Cube, root, new Vector3(-0.24f, 0.08f, -0.14f), new Vector3(0.34f, 0.1f, 0.56f), new Color(0.95f, 0.64f, 0.25f)).transform;
            rightFoot = AddPrimitive("Right Foot", PrimitiveType.Cube, root, new Vector3(0.24f, 0.08f, -0.14f), new Vector3(0.34f, 0.1f, 0.56f), new Color(0.95f, 0.64f, 0.25f)).transform;
            AddPrimitive("Left Toe Tip", PrimitiveType.Cube, leftFoot, new Vector3(0f, -0.02f, -0.46f), new Vector3(0.82f, 0.8f, 0.32f), new Color(1f, 0.72f, 0.32f));
            AddPrimitive("Right Toe Tip", PrimitiveType.Cube, rightFoot, new Vector3(0f, -0.02f, -0.46f), new Vector3(0.82f, 0.8f, 0.32f), new Color(1f, 0.72f, 0.32f));

            shieldAura = AddPrimitive("Shield Aura", PrimitiveType.Sphere, root, new Vector3(0f, 0.9f, 0f), new Vector3(1.45f, 1.65f, 1.45f), new Color(0.42f, 0.9f, 1f, 0.28f)).transform;
            shieldRenderer = shieldAura.GetComponent<Renderer>();
            dashTrail = AddPrimitive("Dash Trail", PrimitiveType.Cube, root, new Vector3(0f, 0.62f, 0.8f), new Vector3(0.9f, 0.2f, 1.7f), new Color(0.34f, 1f, 0.82f, 0.24f)).transform;
            dashRenderer = dashTrail.GetComponent<Renderer>();
            magnetHalo = AddPrimitive("Magnet Halo", PrimitiveType.Cylinder, root, new Vector3(0f, 1.05f, 0f), new Vector3(1.18f, 0.025f, 1.18f), new Color(0.42f, 0.62f, 1f, 0.24f)).transform;
            magnetRenderer = magnetHalo.GetComponent<Renderer>();
            invulnerableRing = AddPrimitive("Invulnerable Ring", PrimitiveType.Cylinder, root, new Vector3(0f, 0.22f, 0f), new Vector3(1.34f, 0.03f, 1.34f), new Color(1f, 0.58f, 0.44f, 0.42f)).transform;
            invulnerableRenderer = invulnerableRing.GetComponent<Renderer>();

            // 跳跃冲击波环
            jumpRing = AddPrimitive("Jump Ring", PrimitiveType.Cylinder, root, new Vector3(0f, 0.05f, 0f), new Vector3(1.6f, 0.04f, 1.6f), new Color(0.7f, 1f, 0.95f, 0f)).transform;
            jumpRingRenderer = jumpRing.GetComponent<Renderer>();
            // 滑铲尾迹（地面拖曳）
            slideTrail = AddPrimitive("Slide Trail", PrimitiveType.Cube, root, new Vector3(0f, 0.04f, 0.6f), new Vector3(0.85f, 0.06f, 1.4f), new Color(0.5f, 0.95f, 1f, 0f)).transform;
            slideTrailRenderer = slideTrail.GetComponent<Renderer>();
            // 任何 buff 激活时显示的暖色光环
            powerAura = AddPrimitive("Power Aura", PrimitiveType.Sphere, root, new Vector3(0f, 0.85f, 0f), new Vector3(1.55f, 1.7f, 1.55f), new Color(1f, 0.78f, 0.45f, 0f)).transform;
            powerAuraRenderer = powerAura.GetComponent<Renderer>();

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

        public static PenguinCharacter Create(Transform parent)
        {
            return new PenguinCharacter(parent);
        }

        public void Animate(float dt, bool running, bool grounded, bool sliding, bool gameOver, float speed01, float laneLean, float dashTimer, float magnetTimer, float shieldTimer, float invulnerabilityTimer)
        {
            var dashActive = dashTimer > 0f;
            if (dashActive && !wasDashActiveLast)
            {
                // 冲刺起手爆发：短促前冲感
                dashBurstTimer = 0.22f;
            }
            dashBurstTimer = Mathf.Max(0f, dashBurstTimer - dt);

            bob += dt * Mathf.Lerp(4.2f, 11.5f, speed01);
            var runWave = Mathf.Sin(bob);
            var idleWave = Mathf.Sin(Time.time * 2.4f);
            var bounce = running && grounded ? Mathf.Abs(runWave) * 0.06f : idleWave * 0.025f;
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
            Root.localRotation = Quaternion.Lerp(
                Root.localRotation,
                Quaternion.Euler((sliding ? 8f : 0f) + dashLean, laneLean * 5f, -laneLean * 11f + invulnWobble),
                Mathf.Clamp01(dt * 10f));
            body.localPosition = Vector3.Lerp(body.localPosition, new Vector3(0f, 0.78f + bounce, 0f), Mathf.Clamp01(dt * 10f));
            head.localPosition = Vector3.Lerp(head.localPosition, new Vector3(0f, 1.55f + bounce * 0.8f, 0.02f), Mathf.Clamp01(dt * 10f));

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
            if (gameOver)
            {
                wingSwing = -20f;
            }

            leftWing.localRotation = Quaternion.Lerp(leftWing.localRotation, Quaternion.Euler(0f, 0f, 18f + wingSwing), Mathf.Clamp01(dt * 14f));
            rightWing.localRotation = Quaternion.Lerp(rightWing.localRotation, Quaternion.Euler(0f, 0f, -18f - wingSwing), Mathf.Clamp01(dt * 14f));
            leftFoot.localRotation = Quaternion.Euler(running ? runWave * 18f : 0f, 0f, 0f);
            rightFoot.localRotation = Quaternion.Euler(running ? -runWave * 18f : 0f, 0f, 0f);
            scarfTail.localRotation = Quaternion.Euler(0f, 0f, 10f + Mathf.Sin(Time.time * 5f) * 10f + speed01 * 16f);
            gogglesBridge.localRotation = Quaternion.Euler(0f, Mathf.Sin(Time.time * 1.7f) * 3f, 0f);
            leftLens.localRotation = Quaternion.Euler(90f, Mathf.Sin(Time.time * 1.7f) * 3f, 0f);
            rightLens.localRotation = Quaternion.Euler(90f, Mathf.Sin(Time.time * 1.7f) * 3f, 0f);
            hatPom.localScale = Vector3.one * (0.18f + Mathf.Sin(Time.time * 3.5f) * 0.015f);
            leftPupil.localScale = new Vector3(0.038f, 0.038f * blink, 0.038f);
            rightPupil.localScale = new Vector3(0.038f, 0.038f * blink, 0.038f);
            var cheekPulse = 1f + Mathf.Sin(Time.time * 4.5f) * 0.06f;
            leftCheek.localScale = new Vector3(0.13f, 0.065f, 0.034f) * cheekPulse;
            rightCheek.localScale = new Vector3(0.13f, 0.065f, 0.034f) * cheekPulse;

            var baseBodyColor = dashTimer > 0f ? new Color(0.04f, 0.18f, 0.24f) : new Color(0.035f, 0.095f, 0.17f);
            var invulnMix = invulnerable ? (0.5f + 0.5f * Mathf.Sin(Time.time * 24f)) : 0f;
            var targetBodyColor = Color.Lerp(baseBodyColor, new Color(1f, 0.55f, 0.48f), invulnMix * 0.65f);
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
            // 起跳瞬间生成一圈环波
            if (wasGroundedLast && !grounded)
            {
                jumpRingTimer = 0.45f;
            }
            // 落地瞬间触发着陆冲击
            if (!wasGroundedLast && grounded)
            {
                landingPulse = 0.35f;
            }

            jumpRingTimer = Mathf.Max(0f, jumpRingTimer - dt);
            landingPulse = Mathf.Max(0f, landingPulse - dt);

            jumpRing.gameObject.SetActive(jumpRingTimer > 0f);
            if (jumpRingTimer > 0f)
            {
                var t = 1f - jumpRingTimer / 0.45f;
                jumpRing.localScale = new Vector3(1.6f + t * 1.4f, 0.04f, 1.6f + t * 1.4f);
                var rc = RunnerVisuals.GetColor(jumpRingRenderer.material);
                rc.a = 0.6f * (1f - t);
                RunnerVisuals.SetColor(jumpRingRenderer.material, rc);
            }

            // 着陆时身体短暂下压（Y方向缩放）
            if (landingPulse > 0f)
            {
                var t = landingPulse / 0.35f;
                var squish = 1f - t * 0.12f;
                var current = Root.localScale;
                Root.localScale = new Vector3(current.x * 1.04f, current.y * squish, current.z * 1.04f);
            }

            // 滑铲尾迹
            slideTrail.gameObject.SetActive(sliding);
            if (sliding)
            {
                var c = RunnerVisuals.GetColor(slideTrailRenderer.material);
                c.a = Mathf.Lerp(c.a, 0.65f, Mathf.Clamp01(dt * 8f));
                RunnerVisuals.SetColor(slideTrailRenderer.material, c);
                slideTrail.localScale = new Vector3(0.85f + Mathf.Sin(Time.time * 18f) * 0.06f, 0.06f, 1.4f + speed01 * 0.6f);
            }

            // 通用 buff 光环：dash / magnet / score boost 都触发
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
