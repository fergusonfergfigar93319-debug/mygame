using UnityEngine;

namespace PenguinRun
{
    /// <summary>挂在障碍根节点，用于教学提示与调试识别。</summary>
    public enum ObstacleKind
    {
        StandardHigh,
        SlideGate,
        WideSlideGate,
        WideWall,
        Enemy,
        Rolling,
        MediumBarrier,
        TallSpike,
        Flying,
    }

    internal sealed class ObstacleKindMarker : MonoBehaviour
    {
        public ObstacleKind Kind;
    }
}
