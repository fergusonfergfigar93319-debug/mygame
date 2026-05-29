using UnityEngine;

namespace PenguinRun
{
    /// <summary>
    /// 布景相对跑道中心线的横向偏移；在 <see cref="PenguinRunnerGame"/> 滚动时随弯道更新世界 X。
    /// </summary>
    internal sealed class SceneryLateralOffset : MonoBehaviour
    {
        public float LateralOffset;
        public bool SnapToGround = true;
    }
}
