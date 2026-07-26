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

    /// <summary>
    /// 布景道具的逐帧动画所需的常量属性，在注册时按名字算一次并缓存。
    /// <para>
    /// 这些判断此前每帧从 <c>GameObject.name</c> 现算——而 <c>.name</c> 的 getter 每次调用都会
    /// 从原生层 marshal 出一个新 string。场景里有 80–100 个道具、每个道具每帧 3 次名字判断
    /// 外加 2 次 <c>GetComponent</c>，是跑酷主循环里最大的一处 GC 压力来源。名字在注册后
    /// 永不改变，因此全部可以前移到注册期。
    /// </para>
    /// </summary>
    internal sealed class SceneryPropInfo
    {
        public GameObject Go;
        public Transform Tr;
        public SceneryLateralOffset Lateral;
        public bool IsAuroraRibbon;
        public bool IsCheckpointGate;
        public bool SkipCorridorFade;
        public bool ForceHideInCorridor;
        public bool SnapToGround;
    }
}
