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

        private readonly Animator[] animators;

        public RunnerAnimationDriver(Animator[] animators, RunnerAvatarDefinition definition)
        {
            this.animators = animators;

            RuntimeAnimatorController runtimeController = definition.BaseController;
            if (definition.BaseController != null && definition.Animations != null)
            {
                var controller = new AnimatorOverrideController(definition.BaseController);
                var replacements = definition.Animations.Overrides;
                var overrides = new List<KeyValuePair<AnimationClip, AnimationClip>>();
                foreach (var clip in controller.animationClips)
                {
                    if (clip == null) continue;
                    if (TryResolveReplacement(clip.name, replacements, out var replacement))
                        overrides.Add(new KeyValuePair<AnimationClip, AnimationClip>(clip, replacement));
                }
                controller.ApplyOverrides(overrides);
                runtimeController = controller;
            }

            foreach (var animator in animators)
            {
                animator.applyRootMotion = false;
                animator.updateMode = AnimatorUpdateMode.Normal;
                animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                animator.runtimeAnimatorController = runtimeController;
                // Rebind after swapping controllers so editor/batch capture and runtime
                // both start from a valid graph before the first Update.
                animator.Rebind();
                animator.Update(0f);
            }
        }

        public void Tick(float deltaTime, in RunnerAvatarState state)
        {
            foreach (var animator in animators)
            {
                animator.SetBool(Running, state.Running);
                animator.SetBool(Grounded, state.Grounded);
                animator.SetBool(Sliding, state.Sliding);
                animator.SetBool(GameOver, state.GameOver);
                animator.SetFloat(Speed01, state.Speed01, 0.08f, deltaTime);
                animator.SetFloat(LaneLean, state.LaneLean, 0.06f, deltaTime);
                animator.SetFloat(VerticalSpeed, state.VerticalVelocity);
            }
        }

        public void TriggerHurt()
        {
            foreach (var animator in animators) animator.SetTrigger(Hurt);
        }

        public void TriggerCelebration()
        {
            foreach (var animator in animators) animator.SetTrigger(Celebrate);
        }

        public void TriggerPerfectDodge()
        {
            foreach (var animator in animators) animator.SetTrigger(PerfectDodge);
        }

        public void TriggerInteraction(RunnerAvatarInteraction value)
        {
            foreach (var animator in animators)
            {
                animator.SetInteger(Interaction, (int)value);
                animator.SetTrigger(PlayInteraction);
            }
        }

        private static bool TryResolveReplacement(
            string clipName,
            IReadOnlyDictionary<string, AnimationClip> replacements,
            out AnimationClip replacement)
        {
            if (replacements.TryGetValue(clipName, out replacement) && replacement != null)
                return true;

            // Imported takes often arrive as "Armature|Idle"; canonical keys are bare names.
            var separator = clipName.LastIndexOf('|');
            if (separator >= 0 && separator + 1 < clipName.Length)
            {
                var bare = clipName.Substring(separator + 1);
                if (replacements.TryGetValue(bare, out replacement) && replacement != null)
                    return true;
            }

            replacement = null;
            return false;
        }
    }
}
