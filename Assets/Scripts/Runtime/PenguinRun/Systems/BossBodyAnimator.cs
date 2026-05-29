using UnityEngine;

namespace PenguinRun
{
    /// <summary>
    /// Boss 躯干/头部程序化动画，按招式阶段采样。
    /// </summary>
    internal sealed class BossBodyAnimator
    {
        private Vector3 headBaseLocalPos = Vector3.zero;
        private Quaternion headBaseLocalRot = Quaternion.identity;
        private bool headInitialized;

        public void CaptureHeadBaseline(Transform head)
        {
            if (head == null) return;
            headBaseLocalPos = head.localPosition;
            headBaseLocalRot = head.localRotation;
            headInitialized = true;
        }

        public void Apply(
            Transform body,
            Transform head,
            BossDefinition def,
            BossPattern pattern,
            BossAttackPhase phase,
            BossPatternVfxProfile profile,
            float windup01,
            float strike01,
            float recovery01,
            ref Vector3 bodyPosOffset,
            ref Vector3 bodyRotOffset)
        {
            if (body == null || profile == null) return;

            var themeMul = def.VfxShakeMultiplier;
            var leanMul = def.AttackStrikePunch;

            var phaseWindup = phase == BossAttackPhase.Windup ? windup01
                : phase == BossAttackPhase.Telegraph ? 1f
                : phase == BossAttackPhase.Strike ? 1f
                : phase == BossAttackPhase.Recovery ? Mathf.Lerp(1f, 0f, recovery01)
                : 0f;

            var phaseStrike = phase == BossAttackPhase.Strike ? strike01
                : phase == BossAttackPhase.Recovery ? Mathf.Lerp(strike01, 0f, recovery01)
                : 0f;

            // 前摇压缩：身体下沉+前倾，模拟蓄力
            var squashStretch = phase == BossAttackPhase.Windup
                ? Mathf.Lerp(1f, 0.88f, windup01)
                : phase == BossAttackPhase.Strike
                ? Mathf.Lerp(1f, 1.08f, strike01)
                : 1f;

            var lean = profile.WindupBodyLean * leanMul * phaseWindup * themeMul;
            var lungeZ = profile.StrikeBodyLungeZ * leanMul * phaseStrike * themeMul;
            var torsoPitch = profile.StrikeTorsoPitch * phaseStrike;

            // 蓄力下沉：Windup 阶段身体重心降低
            if (phase == BossAttackPhase.Windup)
            {
                bodyPosOffset += new Vector3(0f, -0.1f * windup01, 0f);
            }

            // 击发后坐力：Strike 阶段身体被向后推
            if (phase == BossAttackPhase.Strike)
            {
                bodyPosOffset += new Vector3(0f, 0f, -0.15f * strike01);
            }

            switch (pattern)
            {
                case BossPattern.SweepLow:
                    bodyPosOffset += new Vector3(0f, -0.12f * phaseWindup, -lungeZ * 0.35f);
                    bodyRotOffset += new Vector3(lean + torsoPitch, 0f, Mathf.Sin(Time.time * 8f) * phaseWindup * 2f);
                    break;
                case BossPattern.DiveHigh:
                    bodyPosOffset += new Vector3(0f, 0.18f * phaseWindup + 0.08f * phaseStrike, -lungeZ);
                    bodyRotOffset += new Vector3(-lean * 0.5f + torsoPitch, 0f, 0f);
                    break;
                case BossPattern.ChargeAcross:
                    bodyPosOffset += new Vector3(0f, -0.16f * phaseWindup, -lungeZ);
                    bodyRotOffset += new Vector3(lean + torsoPitch, 0f, 0f);
                    break;
                case BossPattern.RangedSalvo:
                    bodyPosOffset += new Vector3(0f, 0.06f * phaseWindup, -lungeZ * 0.5f);
                    bodyRotOffset += new Vector3(lean * 0.6f + torsoPitch * 0.5f, Mathf.Sin(Time.time * 12f) * phaseWindup * 4f, 0f);
                    break;
                case BossPattern.CenterBeam:
                    bodyPosOffset += new Vector3(0f, 0.04f * phaseWindup, -lungeZ * 0.65f);
                    bodyRotOffset += new Vector3(lean * 0.7f + torsoPitch, 0f, 0f);
                    break;
                case BossPattern.QuakePulse:
                    bodyPosOffset += new Vector3(0f, -0.2f * phaseWindup + 0.06f * Mathf.Sin(Time.time * 32f) * phaseStrike, -lungeZ * 0.25f);
                    bodyRotOffset += new Vector3(lean + torsoPitch, 0f, 0f);
                    break;
            }

            if (head != null && headInitialized)
            {
                var headPitch = profile.StrikeHeadPitch * phaseStrike;
                var headBob = phase == BossAttackPhase.Windup ? -0.06f * phaseWindup : 0.04f * phaseStrike;
                head.localPosition = headBaseLocalPos + new Vector3(0f, headBob, 0f);
                head.localRotation = headBaseLocalRot * Quaternion.Euler(headPitch, 0f, 0f);
            }
        }
    }
}
