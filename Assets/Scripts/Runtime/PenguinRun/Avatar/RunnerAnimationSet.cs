using System.Collections.Generic;
using UnityEngine;

namespace PenguinRun
{
    [CreateAssetMenu(menuName = "PenguinRun/Avatar/Animation Set", fileName = "RunnerAnimationSet")]
    public sealed class RunnerAnimationSet : ScriptableObject
    {
        [SerializeField] private AnimationClip idle;
        [SerializeField] private AnimationClip run;
        [SerializeField] private AnimationClip laneLeft;
        [SerializeField] private AnimationClip laneRight;
        [SerializeField] private AnimationClip jump;
        [SerializeField] private AnimationClip fall;
        [SerializeField] private AnimationClip land;
        [SerializeField] private AnimationClip slide;
        [SerializeField] private AnimationClip hurt;
        [SerializeField] private AnimationClip defeat;
        [SerializeField] private AnimationClip celebrate;
        [SerializeField] private AnimationClip fishPickup;
        [SerializeField] private AnimationClip shieldActivate;
        [SerializeField] private AnimationClip dashActivate;
        [SerializeField] private AnimationClip magnetActivate;
        [SerializeField] private AnimationClip bossCounter;

        public IReadOnlyDictionary<string, AnimationClip> Overrides => new Dictionary<string, AnimationClip>
        {
            ["Idle"] = idle,
            ["Run"] = run,
            ["LaneLeft"] = laneLeft,
            ["LaneRight"] = laneRight,
            ["Jump"] = jump,
            ["Fall"] = fall,
            ["Land"] = land,
            ["Slide"] = slide,
            ["Hurt"] = hurt,
            ["Defeat"] = defeat,
            ["Celebrate"] = celebrate,
            ["FishPickup"] = fishPickup,
            ["ShieldActivate"] = shieldActivate,
            ["DashActivate"] = dashActivate,
            ["MagnetActivate"] = magnetActivate,
            ["BossCounter"] = bossCounter,
        };

        public IEnumerable<KeyValuePair<string, AnimationClip>> RequiredClips => Overrides;
    }
}
