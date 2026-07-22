using UnityEngine;

namespace PenguinRun
{
    internal sealed class RunnerSecondaryMotion : MonoBehaviour
    {
        private readonly Transform[] scarfBones = new Transform[3];
        private readonly Quaternion[] scarfBindRotations = new Quaternion[3];
        private Transform goggles;
        private Quaternion gogglesBindRotation;
        private bool enabledForQuality = true;
        private float clock;

        public void Bind(Transform modelRoot)
        {
            for (var i = 0; i < scarfBones.Length; i++)
            {
                scarfBones[i] = RunnerExpressionDriver.FindDeep(modelRoot, $"Scarf.0{i + 1}");
                if (scarfBones[i] != null) scarfBindRotations[i] = scarfBones[i].localRotation;
            }
            goggles = RunnerExpressionDriver.FindDeep(modelRoot, "Goggles");
            if (goggles != null) gogglesBindRotation = goggles.localRotation;
        }

        public void SetQuality(bool secondaryMotionEnabled)
        {
            enabledForQuality = secondaryMotionEnabled;
            if (enabledForQuality) return;
            for (var i = 0; i < scarfBones.Length; i++)
                if (scarfBones[i] != null) scarfBones[i].localRotation = scarfBindRotations[i];
            if (goggles != null) goggles.localRotation = gogglesBindRotation;
        }

        public void Tick(float deltaTime, in RunnerAvatarState state)
        {
            if (!enabledForQuality) return;
            clock += deltaTime;
            for (var i = 0; i < scarfBones.Length; i++)
            {
                if (scarfBones[i] == null) continue;
                var phase = clock * (5.2f + state.Speed01 * 4f) - i * 0.55f;
                var pitch = -state.Speed01 * (8f + i * 5f) + Mathf.Sin(phase) * (2.5f + i * 1.5f);
                var yaw = -state.LaneLean * (5f + i * 3f);
                var target = scarfBindRotations[i] * Quaternion.Euler(pitch, yaw, 0f);
                scarfBones[i].localRotation = Quaternion.Slerp(scarfBones[i].localRotation, target, Mathf.Clamp01(deltaTime * 12f));
            }
            if (goggles != null)
            {
                var vibration = state.Running ? Mathf.Sin(clock * 27f) * state.Speed01 * 0.7f : 0f;
                goggles.localRotation = gogglesBindRotation * Quaternion.Euler(vibration, 0f, -state.LaneLean * 1.2f);
            }
        }
    }
}
