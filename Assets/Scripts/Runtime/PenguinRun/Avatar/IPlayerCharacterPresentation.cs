using UnityEngine;

namespace PenguinRun
{
    internal interface IPlayerCharacterPresentation
    {
        Transform Root { get; }
        void Tick(float deltaTime, in RunnerAvatarState state);
        void TriggerHurt();
        void TriggerPerfectDodge();
        void TriggerCelebration();
        void TriggerInteraction(RunnerAvatarInteraction interaction);
    }
}
