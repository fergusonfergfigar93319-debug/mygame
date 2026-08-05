using UnityEngine;

namespace PenguinRun
{
    /// <summary>
    /// Kenney Cube Pets penguin (CC0) as the in-run presentation layer.
    /// Uses procedural bob/squash because the pack ships static meshes without a runner clip set.
    /// </summary>
    internal sealed class CubePetPenguinCharacter : IPlayerCharacterPresentation
    {
        public const string ResourcePath = "PenguinRun/Characters/CubePetPenguin/animal-penguin";

        private static readonly Quaternion FacingBase = Quaternion.Euler(0f, 180f, 0f);
        private const float TargetStandingHeight = 1.56f;

        private readonly Transform visual;
        private readonly Vector3 visualBaseScale;
        private readonly Renderer[] renderers;

        private float bob;
        private float hurtFlashTimer;
        private float celebrateTimer;
        private float perfectDodgeBurstTimer;
        private Color[] baseColors;

        private CubePetPenguinCharacter(Transform root, Transform visualRoot)
        {
            Root = root;
            Root.localRotation = FacingBase;
            visual = visualRoot;
            visualBaseScale = visual.localScale;
            renderers = visual.GetComponentsInChildren<Renderer>(true);
            CaptureBaseColors();
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

            bob += deltaTime * Mathf.Lerp(4.2f, 11.5f, state.Speed01);
            var runWave = Mathf.Sin(bob);
            var idleWave = Mathf.Sin(Time.time * 2.4f);
            var bounce = state.Running && state.Grounded
                ? Mathf.Abs(runWave) * 0.05f
                : idleWave * 0.02f;
            if (celebrateTimer > 0f)
                bounce += Mathf.Abs(Mathf.Sin(Time.time * 9f)) * 0.1f;

            var squash = state.Sliding
                ? new Vector3(1.18f, 0.55f, 1.22f)
                : Vector3.one;
            if (!state.Grounded)
                squash = Vector3.Lerp(squash, new Vector3(0.92f, 1.12f, 0.92f), 0.65f);

            visual.localPosition = new Vector3(0f, bounce, 0f);
            visual.localScale = Vector3.Lerp(
                visual.localScale,
                Vector3.Scale(visualBaseScale, squash),
                Mathf.Clamp01(deltaTime * 12f));

            var dashLean = state.Has(RunnerAvatarBuffFlags.Dash) ? 14f : 0f;
            var forwardLean = (state.Sliding ? 10f : 0f) + dashLean;
            var defeatPitch = state.GameOver ? 55f : 0f;
            Root.localRotation = Quaternion.Lerp(
                Root.localRotation,
                FacingBase * Quaternion.Euler(-forwardLean + defeatPitch, state.LaneLean * 12f, 0f),
                Mathf.Clamp01(deltaTime * 10f));

            ApplyHurtFlash();
            if (perfectDodgeBurstTimer > 0f)
                PulseRenderers(1f + 0.08f * (perfectDodgeBurstTimer / 0.55f));
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
            // Keep feet near y=0 after uniform scale.
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
