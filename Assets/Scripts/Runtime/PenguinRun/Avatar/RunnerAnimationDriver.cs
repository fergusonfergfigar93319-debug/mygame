using System.Collections.Generic;
using UnityEngine;

namespace PenguinRun
{
    internal sealed class RunnerAnimationDriver
    {
        private static readonly int Running = Animator.StringToHash("Running");
        private static readonly int Grounded = Animator.StringToHash("Grounded");
        private static readonly int Sliding = Animator.StringToHash("Sliding");
        private static readonly int GameOver = Animator.StringToHash("GameOver");
        private static readonly int Speed01 = Animator.StringToHash("Speed01");
        private static readonly int LaneLean = Animator.StringToHash("LaneLean");
        private static readonly int VerticalSpeed = Animator.StringToHash("VerticalSpeed");
        private static readonly int Hurt = Animator.StringToHash("Hurt");
        private static readonly int Celebrate = Animator.StringToHash("Celebrate");
        private static readonly int PerfectDodge = Animator.StringToHash("PerfectDodge");
        private static readonly int Interaction = Animator.StringToHash("Interaction");
        private static readonly int PlayInteraction = Animator.StringToHash("PlayInteraction");

        private readonly Animator animator;

        public RunnerAnimationDriver(Animator animator, RunnerAvatarDefinition definition)
        {
            this.animator = animator;
            animator.applyRootMotion = false;
            animator.updateMode = AnimatorUpdateMode.Normal;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;

            if (definition.BaseController == null || definition.Animations == null) return;
            var controller = new AnimatorOverrideController(definition.BaseController);
            var replacements = definition.Animations.Overrides;
            var overrides = new List<KeyValuePair<AnimationClip, AnimationClip>>();
            foreach (var clip in controller.animationClips)
            {
                if (clip != null && replacements.TryGetValue(clip.name, out var replacement) && replacement != null)
                    overrides.Add(new KeyValuePair<AnimationClip, AnimationClip>(clip, replacement));
            }
            controller.ApplyOverrides(overrides);
            animator.runtimeAnimatorController = controller;
        }

        public void Tick(float deltaTime, in RunnerAvatarState state)
        {
            animator.SetBool(Running, state.Running);
            animator.SetBool(Grounded, state.Grounded);
            animator.SetBool(Sliding, state.Sliding);
            animator.SetBool(GameOver, state.GameOver);
            animator.SetFloat(Speed01, state.Speed01, 0.08f, deltaTime);
            animator.SetFloat(LaneLean, state.LaneLean, 0.06f, deltaTime);
            animator.SetFloat(VerticalSpeed, state.VerticalVelocity);
        }

        public void TriggerHurt() => animator.SetTrigger(Hurt);
        public void TriggerCelebration() => animator.SetTrigger(Celebrate);
        public void TriggerPerfectDodge() => animator.SetTrigger(PerfectDodge);

        public void TriggerInteraction(RunnerAvatarInteraction value)
        {
            animator.SetInteger(Interaction, (int)value);
            animator.SetTrigger(PlayInteraction);
        }
    }
}
