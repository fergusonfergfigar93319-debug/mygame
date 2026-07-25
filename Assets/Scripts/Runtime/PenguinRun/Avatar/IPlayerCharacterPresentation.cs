using UnityEngine;

namespace PenguinRun
{
    public interface IPlayerCharacterPresentation
    {
        Transform Root { get; }
        void Tick(float deltaTime, in RunnerAvatarState state);
        void TriggerHurt();
        void TriggerPerfectDodge();
        void TriggerCelebration();
        void TriggerInteraction(RunnerAvatarInteraction interaction);
    }
}
