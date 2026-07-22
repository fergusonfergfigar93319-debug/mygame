using UnityEngine;

namespace PenguinRun
{
    internal sealed class RunnerExpressionDriver : MonoBehaviour
    {
        private Transform leftEye;
        private Transform rightEye;
        private Transform leftEyelid;
        private Transform rightEyelid;
        private Quaternion leftEyeBind;
        private Quaternion rightEyeBind;
        private Vector3 leftEyelidBindScale;
        private Vector3 rightEyelidBindScale;
        private float blinkClock;
        private float nextBlink = 2.2f;

        public void Bind(Transform modelRoot)
        {
            leftEye = FindDeep(modelRoot, "Eye.L");
            rightEye = FindDeep(modelRoot, "Eye.R");
            leftEyelid = FindDeep(modelRoot, "Eyelid.L");
            rightEyelid = FindDeep(modelRoot, "Eyelid.R");
            if (leftEye != null) leftEyeBind = leftEye.localRotation;
            if (rightEye != null) rightEyeBind = rightEye.localRotation;
            if (leftEyelid != null) leftEyelidBindScale = leftEyelid.localScale;
            if (rightEyelid != null) rightEyelidBindScale = rightEyelid.localScale;
        }

        public void Tick(float deltaTime, in RunnerAvatarState state)
        {
            blinkClock += deltaTime;
            var blinkPhase = blinkClock - nextBlink;
            var eyelidY = blinkPhase >= 0f && blinkPhase < 0.18f
                ? Mathf.Lerp(1f, 0.08f, 1f - Mathf.Abs(blinkPhase / 0.09f - 1f))
                : 1f;
            if (blinkPhase >= 0.18f)
            {
                blinkClock = 0f;
                nextBlink = 2.1f + Mathf.Repeat(Time.unscaledTime * 0.37f, 1.7f);
            }

            if (leftEyelid != null) leftEyelid.localScale = Vector3.Scale(leftEyelidBindScale, new Vector3(1f, 1f, eyelidY));
            if (rightEyelid != null) rightEyelid.localScale = Vector3.Scale(rightEyelidBindScale, new Vector3(1f, 1f, eyelidY));
            var look = Mathf.Clamp(state.LaneLean, -1f, 1f) * 6f;
            if (leftEye != null) leftEye.localRotation = leftEyeBind * Quaternion.Euler(0f, 0f, -look);
            if (rightEye != null) rightEye.localRotation = rightEyeBind * Quaternion.Euler(0f, 0f, -look);
        }

        internal static Transform FindDeep(Transform root, string name)
        {
            if (root == null) return null;
            if (root.name == name || root.name.EndsWith(name, System.StringComparison.Ordinal)) return root;
            for (var i = 0; i < root.childCount; i++)
            {
                var found = FindDeep(root.GetChild(i), name);
                if (found != null) return found;
            }
            return null;
        }
    }
}
