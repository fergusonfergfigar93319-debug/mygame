using UnityEngine;

namespace PenguinRun
{
    [CreateAssetMenu(menuName = "PenguinRun/Avatar/Avatar Definition", fileName = "RunnerAvatarDefinition")]
    public sealed class RunnerAvatarDefinition : ScriptableObject
    {
        [SerializeField] private string avatarId = "gugugaga";
        [SerializeField] private GameObject modelPrefab;
        [SerializeField] private RuntimeAnimatorController baseController;
        [SerializeField] private RunnerAnimationSet animations;
        [SerializeField] private RunnerAppearanceSet appearance;
        [SerializeField] private RunnerAccessorySocketSet sockets;
        [SerializeField] private Vector3 localPosition = Vector3.zero;
        [SerializeField] private Vector3 localEulerAngles = new(0f, 180f, 0f);
        [SerializeField] private Vector3 localScale = Vector3.one;
        [SerializeField] private bool fallbackToProcedural = true;

        public string AvatarId => avatarId;
        public GameObject ModelPrefab => modelPrefab;
        public RuntimeAnimatorController BaseController => baseController;
        public RunnerAnimationSet Animations => animations;
        public RunnerAppearanceSet Appearance => appearance;
        public RunnerAccessorySocketSet Sockets => sockets;
        public Vector3 LocalPosition => localPosition;
        public Vector3 LocalEulerAngles => localEulerAngles;
        public Vector3 LocalScale => localScale;
        public bool FallbackToProcedural => fallbackToProcedural;
    }
}
