using System.Collections.Generic;
using UnityEngine;

namespace PenguinRun
{
    public sealed class PlayerAvatarPresenter : IPlayerCharacterPresentation
    {
        private readonly RunnerAnimationDriver animationDriver;
        private readonly RunnerExpressionDriver expressionDriver;
        private readonly RunnerSecondaryMotion secondaryMotion;
        private readonly RunnerCharacterFx characterFx;
        private readonly Dictionary<string, Transform> sockets = new();

        private PlayerAvatarPresenter(Transform parent, RunnerAvatarDefinition definition)
        {
            var instance = Object.Instantiate(definition.ModelPrefab, parent, false);
            instance.name = $"Avatar {definition.AvatarId}";
            Root = instance.transform;
            Root.localPosition = definition.LocalPosition;
            Root.localRotation = Quaternion.Euler(definition.LocalEulerAngles);
            Root.localScale = definition.LocalScale;

            var animators = instance.GetComponentsInChildren<Animator>(true);
            if (animators.Length == 0) throw new System.InvalidOperationException("Avatar prefab has no Animator.");
            animationDriver = new RunnerAnimationDriver(animators, definition);

            expressionDriver = instance.GetComponent<RunnerExpressionDriver>() ?? instance.AddComponent<RunnerExpressionDriver>();
            expressionDriver.Bind(instance.transform);
            secondaryMotion = instance.GetComponent<RunnerSecondaryMotion>() ?? instance.AddComponent<RunnerSecondaryMotion>();
            secondaryMotion.Bind(instance.transform);
            characterFx = new RunnerCharacterFx(parent);
            ApplyAppearance(instance.transform, definition.Appearance);
            ResolveSockets(instance.transform, definition.Sockets);
        }

        public Transform Root { get; }

        public static IPlayerCharacterPresentation Create(Transform parent, RunnerAvatarDefinition definition)
        {
            if (definition == null || definition.ModelPrefab == null)
                return PenguinCharacter.CreateCute(parent);

            try
            {
                return new PlayerAvatarPresenter(parent, definition);
            }
            catch (System.Exception ex)
            {
                if (!definition.FallbackToProcedural) throw;
                Debug.LogWarning($"Avatar '{definition.AvatarId}' failed; using procedural fallback. {ex.Message}");
                return PenguinCharacter.CreateCute(parent);
            }
        }

        public void Tick(float deltaTime, in RunnerAvatarState state)
        {
            animationDriver.Tick(deltaTime, state);
            expressionDriver.Tick(deltaTime, state);
            secondaryMotion.Tick(deltaTime, state);
            characterFx.Tick(deltaTime, state);
        }

        public void TriggerHurt()
        {
            animationDriver.TriggerHurt();
            characterFx.TriggerHurt();
        }

        public void TriggerPerfectDodge()
        {
            animationDriver.TriggerPerfectDodge();
            characterFx.TriggerPerfectDodge();
        }

        public void TriggerCelebration()
        {
            animationDriver.TriggerCelebration();
            characterFx.TriggerCelebration();
        }

        public void TriggerInteraction(RunnerAvatarInteraction interaction)
        {
            animationDriver.TriggerInteraction(interaction);
            characterFx.TriggerInteraction(interaction);
        }

        public bool TryGetSocket(string id, out Transform socket) => sockets.TryGetValue(id, out socket);

        private static void ApplyAppearance(Transform modelRoot, RunnerAppearanceSet appearance)
        {
            if (appearance == null) return;
            foreach (var entry in appearance.Overrides)
            {
                if (entry == null || entry.Material == null || string.IsNullOrWhiteSpace(entry.RendererPath)) continue;
                var target = modelRoot.Find(entry.RendererPath);
                if (target != null && target.TryGetComponent<Renderer>(out var renderer)) renderer.sharedMaterial = entry.Material;
            }
        }

        private void ResolveSockets(Transform modelRoot, RunnerAccessorySocketSet socketSet)
        {
            if (socketSet == null) return;
            foreach (var entry in socketSet.Sockets)
            {
                if (entry == null || string.IsNullOrWhiteSpace(entry.Id)) continue;
                var socket = modelRoot.Find(entry.RelativePath) ?? RunnerExpressionDriver.FindDeep(modelRoot, entry.RelativePath);
                if (socket != null) sockets[entry.Id] = socket;
            }
        }
    }
}
