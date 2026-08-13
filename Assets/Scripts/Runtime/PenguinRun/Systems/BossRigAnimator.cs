using UnityEngine;

namespace PenguinRun
{
    /// <summary>
    /// rigged Boss 的骨骼动画播放器：包装 Legacy Animation 片段并按招式阶段程序化驱动。
    /// 不引入 Animator 状态机（见 docs/adr/0002-cc0-rigged-boss-models.md）；
    /// 攻击片段通过播放速度缩放对齐击发时刻（危险窗起点），命中判定不读动画。
    /// </summary>
    internal sealed class BossRigAnimator
    {
        private Animation anim;
        private string idleClip;
        private string meleeClip;
        private string castClip;
        private string slamClip;
        private string hitClip;
        private string deathClip;
        private string moveClip;
        private string dazedClip;
        private float oneShotTimer;
        private bool deathPlaying;

        public bool IsValid => anim != null && idleClip != null;

        public void Bind(GameObject modelRoot)
        {
            anim = modelRoot.GetComponentInChildren<Animation>();
            if (anim == null) return;

            anim.playAutomatically = false;
            anim.cullingType = AnimationCullingType.AlwaysAnimate;

            foreach (AnimationState state in anim)
            {
                var key = state.name.ToLowerInvariant();
                var suffix = key;
                var bar = key.LastIndexOf('|');
                if (bar >= 0 && bar < key.Length - 1) suffix = key.Substring(bar + 1);

                switch (suffix)
                {
                    case "flying_idle": idleClip = state.name; break;
                    case "idle": idleClip ??= state.name; break;
                    case "headbutt": meleeClip = state.name; break;
                    case "punch":
                        meleeClip ??= state.name;
                        castClip = state.name;
                        break;
                    case "weapon": slamClip = state.name; break;
                    case "hitreact": hitClip = state.name; break;
                    case "death": deathClip = state.name; break;
                    case "fast_flying": moveClip = state.name; break;
                    case "run": moveClip ??= state.name; break;
                    case "walk": if (moveClip == null) moveClip = state.name; break;
                    case "no": dazedClip = state.name; break;
                }
            }

            castClip ??= meleeClip;
            slamClip ??= meleeClip;
            dazedClip ??= hitClip;

            SetWrap(idleClip, WrapMode.Loop);
            SetWrap(moveClip, WrapMode.Loop);
            SetWrap(dazedClip, WrapMode.Loop);
            SetWrap(meleeClip, WrapMode.ClampForever);
            SetWrap(castClip, WrapMode.ClampForever);
            SetWrap(slamClip, WrapMode.ClampForever);
            SetWrap(hitClip, WrapMode.ClampForever);
            SetWrap(deathClip, WrapMode.ClampForever);

            PlayIdle();
        }

        private void SetWrap(string clip, WrapMode mode)
        {
            if (clip == null) return;
            var state = anim[clip];
            if (state != null) state.wrapMode = mode;
        }

        /// <summary>每帧推进：一次性片段播完后自动回到待机循环。</summary>
        public void Tick(float dt)
        {
            if (!IsValid || deathPlaying) return;
            if (oneShotTimer > 0f)
            {
                oneShotTimer -= dt;
                if (oneShotTimer <= 0f) PlayIdle(0.22f);
            }
        }

        public void PlayIdle(float fade = 0.15f)
        {
            if (!IsValid || deathPlaying) return;
            ResetSpeed(idleClip);
            anim.CrossFade(idleClip, fade);
            oneShotTimer = 0f;
        }

        /// <summary>入场/追击移动循环。</summary>
        public void PlayMove(float fade = 0.2f)
        {
            if (!IsValid || deathPlaying || moveClip == null) { PlayIdle(fade); return; }
            ResetSpeed(moveClip);
            anim.CrossFade(moveClip, fade);
            oneShotTimer = 0f;
        }

        /// <summary>
        /// 招式攻击：按招式选片段，并缩放播放速度，让片段收势落在
        /// <paramref name="secondsUntilStrikeImpact"/>（危险窗起点 / 判定生效瞬间）。
        /// </summary>
        public void PlayAttack(BossPattern pattern, float secondsUntilStrikeImpact)
        {
            if (!IsValid || deathPlaying) return;
            var clip = pattern switch
            {
                BossPattern.RangedSalvo => castClip,
                BossPattern.CenterBeam => castClip,
                BossPattern.QuakePulse => slamClip,
                _ => meleeClip,
            };
            if (clip == null) return;

            var state = anim[clip];
            if (state == null) return;
            var duration = Mathf.Max(0.3f, secondsUntilStrikeImpact);
            state.speed = state.length / duration;
            state.time = 0f;
            anim.CrossFade(clip, 0.1f);
            oneShotTimer = duration + 0.05f;
        }

        /// <summary>被玩家反击命中。</summary>
        public void PlayHit()
        {
            if (!IsValid || deathPlaying || hitClip == null) return;
            ResetSpeed(hitClip);
            anim[hitClip].time = 0f;
            anim.CrossFade(hitClip, 0.06f);
            oneShotTimer = anim[hitClip].length + 0.05f;
        }

        /// <summary>破绽期眩晕循环（摇头/踉跄）。</summary>
        public void PlayDazed(float fade = 0.15f)
        {
            if (!IsValid || deathPlaying || dazedClip == null) { PlayIdle(fade); return; }
            ResetSpeed(dazedClip);
            anim.CrossFade(dazedClip, fade);
            oneShotTimer = 0f;
        }

        /// <summary>击败倒地：保持在最后一帧，不再切回待机。</summary>
        public void PlayDeath()
        {
            if (!IsValid || deathPlaying || deathClip == null) return;
            deathPlaying = true;
            ResetSpeed(deathClip);
            anim[deathClip].time = 0f;
            anim.CrossFade(deathClip, 0.08f);
        }

        private void ResetSpeed(string clip)
        {
            if (clip == null) return;
            var state = anim[clip];
            if (state != null) state.speed = 1f;
        }
    }
}
