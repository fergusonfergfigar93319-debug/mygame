using UnityEngine;

namespace PenguinRun
{
    internal sealed class RunnerCharacterFx
    {
        private readonly Transform root;
        private readonly Transform shieldAura;
        private readonly Transform dashTrail;
        private readonly Transform magnetHalo;
        private readonly Transform invulnerableRing;
        private readonly Transform jumpRing;
        private readonly Transform slideTrail;
        private readonly Transform interactionBurst;
        private float jumpRingTimer;
        private float interactionTimer;
        private float hurtTimer;
        private bool wasGrounded = true;

        public RunnerCharacterFx(Transform parent)
        {
            root = new GameObject("Character FX").transform;
            root.SetParent(parent, false);
            shieldAura = Add("Shield Aura", PrimitiveType.Sphere, new Vector3(0f, 0.9f, 0f), new Vector3(1.45f, 1.65f, 1.45f), new Color(0.42f, 0.9f, 1f, 0.22f));
            dashTrail = Add("Dash Trail", PrimitiveType.Cube, new Vector3(0f, 0.62f, 0.85f), new Vector3(0.9f, 0.18f, 1.8f), new Color(0.34f, 1f, 0.82f, 0.22f));
            magnetHalo = Add("Magnet Halo", PrimitiveType.Cylinder, new Vector3(0f, 1.02f, 0f), new Vector3(1.2f, 0.025f, 1.2f), new Color(0.42f, 0.62f, 1f, 0.24f));
            invulnerableRing = Add("Invulnerable Ring", PrimitiveType.Cylinder, new Vector3(0f, 0.18f, 0f), new Vector3(1.3f, 0.03f, 1.3f), new Color(1f, 0.58f, 0.44f, 0.38f));
            jumpRing = Add("Jump Ring", PrimitiveType.Cylinder, new Vector3(0f, 0.04f, 0f), new Vector3(1.5f, 0.035f, 1.5f), new Color(0.7f, 1f, 0.95f, 0.5f));
            slideTrail = Add("Slide Trail", PrimitiveType.Cube, new Vector3(0f, 0.03f, 0.7f), new Vector3(0.8f, 0.05f, 1.5f), new Color(0.5f, 0.95f, 1f, 0.45f));
            interactionBurst = Add("Interaction Burst", PrimitiveType.Cylinder,
                new Vector3(0f, 0.12f, 0f), new Vector3(1.3f, 0.03f, 1.3f),
                new Color(1f, 0.78f, 0.3f, 0.18f));
            SetAllInactive();
        }

        public void Tick(float deltaTime, in RunnerAvatarState state)
        {
            jumpRingTimer = Mathf.Max(0f, jumpRingTimer - deltaTime);
            interactionTimer = Mathf.Max(0f, interactionTimer - deltaTime);
            hurtTimer = Mathf.Max(0f, hurtTimer - deltaTime);
            if (wasGrounded && !state.Grounded) jumpRingTimer = Mathf.Max(jumpRingTimer, 0.38f);

            shieldAura.gameObject.SetActive(state.Has(RunnerAvatarBuffFlags.Shield));
            dashTrail.gameObject.SetActive(state.Has(RunnerAvatarBuffFlags.Dash));
            magnetHalo.gameObject.SetActive(state.Has(RunnerAvatarBuffFlags.Magnet));
            invulnerableRing.gameObject.SetActive(state.Has(RunnerAvatarBuffFlags.Invulnerable));
            jumpRing.gameObject.SetActive(jumpRingTimer > 0f);
            slideTrail.gameObject.SetActive(state.Sliding);
            interactionBurst.gameObject.SetActive(interactionTimer > 0f || hurtTimer > 0f);

            var pulse = 1f + Mathf.Sin(Time.time * 9f) * 0.05f;
            shieldAura.localScale = new Vector3(1.45f, 1.65f, 1.45f) * pulse;
            magnetHalo.localRotation = Quaternion.Euler(0f, Time.time * 160f, 0f);
            invulnerableRing.localRotation = Quaternion.Euler(0f, Time.time * 260f, 0f);
            if (jumpRingTimer > 0f)
            {
                var t = 1f - jumpRingTimer / 0.38f;
                var radius = Mathf.Lerp(1.4f, 2.7f, t);
                jumpRing.localScale = new Vector3(radius, 0.035f, radius);
            }
            if (interactionTimer > 0f || hurtTimer > 0f)
            {
                var remaining = Mathf.Max(interactionTimer, hurtTimer);
                var radius = Mathf.Lerp(1.6f, 0.8f, remaining / 0.35f);
                interactionBurst.localScale = new Vector3(radius, 0.03f, radius);
            }
            wasGrounded = state.Grounded;
        }

        public void TriggerHurt() => hurtTimer = 0.24f;
        public void TriggerPerfectDodge() => jumpRingTimer = 0.55f;
        public void TriggerCelebration() => interactionTimer = 0.55f;
        public void TriggerInteraction(RunnerAvatarInteraction interaction) => interactionTimer = 0.35f;

        private Transform Add(string name, PrimitiveType type, Vector3 position, Vector3 scale, Color color)
        {
            var go = RunnerVisuals.CreatePrimitive(name, type, Vector3.zero, scale, color, true);
            go.transform.SetParent(root, false);
            go.transform.localPosition = position;
            go.transform.localScale = scale;
            return go.transform;
        }

        private void SetAllInactive()
        {
            for (var i = 0; i < root.childCount; i++) root.GetChild(i).gameObject.SetActive(false);
        }
    }
}
