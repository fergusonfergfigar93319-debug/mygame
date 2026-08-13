using UnityEngine;

namespace PenguinRun
{
    /// <summary>
    /// Kenney Cube Pets penguin (CC0) as the in-run presentation layer.
    /// Pack has no runner clips, so run/jump/slide are driven by procedural motion.
    /// </summary>
    internal sealed class CubePetPenguinCharacter : IPlayerCharacterPresentation
    {
        public const string ResourcePath = "PenguinRun/Characters/CubePetPenguin/animal-penguin";

        private static readonly Quaternion FacingBase = Quaternion.Euler(0f, 180f, 0f);
        private const float TargetStandingHeight = 1.56f;

        private readonly Transform visual;
        private readonly Vector3 visualBaseScale;
        private readonly Renderer[] renderers;

        private float stridePhase;
        private float hurtFlashTimer;
        private float celebrateTimer;
        private float perfectDodgeBurstTimer;
        private float landSquashTimer;
        private bool wasGrounded = true;
        private Color[] baseColors;

        private CubePetPenguinCharacter(Transform root, Transform visualRoot)
        {
            Root = root;
            Root.localRotation = FacingBase;
            visual = visualRoot;
            visualBaseScale = visual.localScale;
            renderers = visual.GetComponentsInChildren<Renderer>(true);
            CaptureBaseColors();
            ApplyWarmPaletteBoost();
        }

        public Transform Root { get; }

        public static IPlayerCharacterPresentation TryCreate(Transform parent)
        {
            var prefab = Resources.Load<GameObject>(ResourcePath);
            if (prefab == null) return null;

            try
            {
                var root = new GameObject("CubePet Penguin").transform;
                root.SetParent(parent, false);

                var instance = Object.Instantiate(prefab, root, false);
                instance.name = "animal-penguin";
                StripCollidersAndAudio(instance);
                FitStandingHeight(instance.transform);

                return new CubePetPenguinCharacter(root, instance.transform);
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"CubePet penguin failed to create; falling back. {ex.Message}");
                return null;
            }
        }

        public void Tick(float deltaTime, in RunnerAvatarState state)
        {
            hurtFlashTimer = Mathf.Max(0f, hurtFlashTimer - deltaTime);
            celebrateTimer = Mathf.Max(0f, celebrateTimer - deltaTime);
            perfectDodgeBurstTimer = Mathf.Max(0f, perfectDodgeBurstTimer - deltaTime);
            landSquashTimer = Mathf.Max(0f, landSquashTimer - deltaTime);

            if (state.GameOver)
            {
                // 表现层实例跨重开复用：局末把姿态基线归位，避免上局空中死亡导致重开首帧误触发落地挤压。
                wasGrounded = true;
                landSquashTimer = 0f;
            }

            if (!wasGrounded && state.Grounded)
                landSquashTimer = 0.22f;
            wasGrounded = state.Grounded;

            var cadence = Mathf.Lerp(7.5f, 14f, state.Speed01);
            if (state.Running && state.Grounded && !state.Sliding)
                stridePhase += deltaTime * cadence;
            else if (!state.Grounded)
                stridePhase += deltaTime * 3.2f;
            else
                stridePhase += deltaTime * 2.2f;

            var stride = Mathf.Sin(stridePhase);
            var strideAbs = Mathf.Abs(stride);
            var step = Mathf.Sin(stridePhase * 2f);

            // Vertical hop: readable run bounce (was ~5cm, now ~14cm at full speed).
            var bounce = 0f;
            if (state.Sliding)
                bounce = -0.06f;
            else if (!state.Grounded)
                bounce = Mathf.Clamp(state.VerticalVelocity * 0.035f, -0.08f, 0.22f);
            else if (state.Running)
                bounce = strideAbs * Mathf.Lerp(0.06f, 0.14f, state.Speed01);
            else
                bounce = Mathf.Sin(Time.time * 2.6f) * 0.025f;

            if (celebrateTimer > 0f)
                bounce += Mathf.Abs(Mathf.Sin(Time.time * 11f)) * 0.12f;

            // Lateral sway + stride rock so the silhouette clearly "steps".
            var swayX = state.Running && state.Grounded && !state.Sliding
                ? stride * Mathf.Lerp(0.04f, 0.09f, state.Speed01)
                : state.LaneLean * 0.03f;
            var rollZ = state.Running && state.Grounded && !state.Sliding
                ? -stride * Mathf.Lerp(6f, 14f, state.Speed01)
                : -state.LaneLean * 8f;

            Vector3 squash;
            if (state.Sliding)
            {
                squash = new Vector3(1.35f, 0.42f, 1.4f);
            }
            else if (!state.Grounded)
            {
                var stretch = Mathf.Clamp01(Mathf.Abs(state.VerticalVelocity) / 10f);
                squash = Vector3.Lerp(new Vector3(0.9f, 1.18f, 0.9f), new Vector3(0.82f, 1.32f, 0.82f), stretch);
            }
            else if (landSquashTimer > 0f)
            {
                var land = landSquashTimer / 0.22f;
                squash = Vector3.Lerp(Vector3.one, new Vector3(1.28f, 0.72f, 1.22f), land);
            }
            else if (state.Running)
            {
                // Alternating squash on foot plant.
                squash = new Vector3(
                    1f + step * 0.08f,
                    1f - strideAbs * 0.12f,
                    1f + strideAbs * 0.06f);
            }
            else
            {
                squash = Vector3.one;
            }

            visual.localPosition = new Vector3(swayX, bounce, 0f);
            visual.localScale = Vector3.Lerp(
                visual.localScale,
                Vector3.Scale(visualBaseScale, squash),
                Mathf.Clamp01(deltaTime * 14f));

            var dashLean = state.Has(RunnerAvatarBuffFlags.Dash) ? 18f : 0f;
            var forwardLean = (state.Sliding ? 28f : state.Running ? 8f + state.Speed01 * 6f : 0f) + dashLean;
            if (!state.Grounded)
                forwardLean += Mathf.Clamp(-state.VerticalVelocity * 1.2f, -12f, 18f);
            var defeatPitch = state.GameOver ? 55f : 0f;
            var yaw = state.LaneLean * 18f + (state.Running && state.Grounded ? stride * 4f : 0f);

            Root.localRotation = Quaternion.Lerp(
                Root.localRotation,
                FacingBase * Quaternion.Euler(-forwardLean + defeatPitch, yaw, rollZ),
                Mathf.Clamp01(deltaTime * 12f));

            ApplyHurtFlash();
            if (perfectDodgeBurstTimer > 0f)
                PulseRenderers(1f + 0.1f * (perfectDodgeBurstTimer / 0.55f));
        }

        public void TriggerHurt() => hurtFlashTimer = 0.18f;

        public void TriggerPerfectDodge() => perfectDodgeBurstTimer = 0.55f;

        public void TriggerCelebration() => celebrateTimer = 1.4f;

        public void TriggerInteraction(RunnerAvatarInteraction interaction)
        {
        }

        private void CaptureBaseColors()
        {
            baseColors = new Color[renderers.Length];
            for (var i = 0; i < renderers.Length; i++)
            {
                var mat = renderers[i] != null ? renderers[i].material : null;
                if (mat == null)
                {
                    baseColors[i] = Color.white;
                    continue;
                }

                if (mat.HasProperty("_BaseColor")) baseColors[i] = mat.GetColor("_BaseColor");
                else if (mat.HasProperty("_Color")) baseColors[i] = mat.GetColor("_Color");
                else baseColors[i] = Color.white;
            }
        }

        /// <summary>Pull near-white Cube Pet materials toward cream/amber so the hero reads on snow.</summary>
        private void ApplyWarmPaletteBoost()
        {
            for (var i = 0; i < baseColors.Length; i++)
            {
                var c = baseColors[i];
                var luma = 0.2126f * c.r + 0.7152f * c.g + 0.0722f * c.b;
                if (luma > 0.78f)
                    c = Color.Lerp(c, new Color(1f, 0.9f, 0.72f), 0.42f);
                else if (c.r > c.b + 0.08f)
                    c = Color.Lerp(c, new Color(1f, 0.55f, 0.28f), 0.28f);
                else
                    c = Color.Lerp(c, new Color(0.55f, 0.78f, 1f), 0.18f);
                baseColors[i] = c;

                var renderer = renderers[i];
                if (renderer == null) continue;
                var mat = renderer.material;
                if (mat == null) continue;
                if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", c);
                if (mat.HasProperty("_Color")) mat.SetColor("_Color", c);
            }
        }

        private void ApplyHurtFlash()
        {
            if (renderers == null) return;
            var flashing = hurtFlashTimer > 0f;
            for (var i = 0; i < renderers.Length; i++)
            {
                var renderer = renderers[i];
                if (renderer == null) continue;
                var mat = renderer.material;
                if (mat == null) continue;
                var color = flashing
                    ? Color.Lerp(baseColors[i], new Color(1f, 0.35f, 0.35f), 0.65f)
                    : baseColors[i];
                if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
                if (mat.HasProperty("_Color")) mat.SetColor("_Color", color);
            }
        }

        private void PulseRenderers(float mul)
        {
            for (var i = 0; i < renderers.Length; i++)
            {
                var renderer = renderers[i];
                if (renderer == null) continue;
                var mat = renderer.material;
                if (mat == null) continue;
                var color = baseColors[i] * mul;
                color.a = baseColors[i].a;
                if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
                if (mat.HasProperty("_Color")) mat.SetColor("_Color", color);
            }
        }

        private static void StripCollidersAndAudio(GameObject instance)
        {
            foreach (var collider in instance.GetComponentsInChildren<Collider>(true))
                Object.Destroy(collider);
            foreach (var audio in instance.GetComponentsInChildren<AudioSource>(true))
                Object.Destroy(audio);
            foreach (var anim in instance.GetComponentsInChildren<Animator>(true))
                Object.Destroy(anim);
            foreach (var legacy in instance.GetComponentsInChildren<Animation>(true))
                Object.Destroy(legacy);
        }

        private static void FitStandingHeight(Transform modelRoot)
        {
            var bounds = new Bounds(modelRoot.position, Vector3.zero);
            var hasRenderer = false;
            foreach (var renderer in modelRoot.GetComponentsInChildren<Renderer>(true))
            {
                if (!hasRenderer)
                {
                    bounds = renderer.bounds;
                    hasRenderer = true;
                }
                else bounds.Encapsulate(renderer.bounds);
            }

            if (!hasRenderer || bounds.size.y < 0.01f)
            {
                modelRoot.localScale = Vector3.one * 1.4f;
                return;
            }

            var scale = TargetStandingHeight / bounds.size.y;
            modelRoot.localScale = Vector3.one * scale;
            var newBounds = new Bounds(modelRoot.position, Vector3.zero);
            var first = true;
            foreach (var renderer in modelRoot.GetComponentsInChildren<Renderer>(true))
            {
                if (first)
                {
                    newBounds = renderer.bounds;
                    first = false;
                }
                else newBounds.Encapsulate(renderer.bounds);
            }

            var feetOffset = newBounds.min.y - modelRoot.parent.position.y;
            modelRoot.localPosition = new Vector3(0f, -feetOffset, 0f);
        }
    }
}
