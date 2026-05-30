#if DOTWEEN
using DG.Tweening;
#endif
using UnityEngine;

namespace PenguinRun
{
    /// <summary>
    /// DOTween 初始化和全局配置。挂到任意场景的 GameObject 上即可。
    /// 安装：从 https://dotween.demigiant.com/download.php 导入 .unitypackage 后执行 Tools → Demigiant → DOTween Utility Panel → Setup（会自动添加 DOTWEEN 编译符号）。
    /// </summary>
    public sealed class TweenSetup : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void AutoInit()
        {
#if DOTWEEN
            // DOTween Setup 完成后自动初始化，此处设置全局参数
            DOTween.Init(true, true, LogBehaviour.Default);
            DOTween.defaultAutoPlay = AutoPlay.None;
            DOTween.defaultAutoKill = true;
            DOTween.defaultTimeScaleIndependent = false;
            Debug.Log("[TweenSetup] DOTween initialized with project defaults.");
#endif
        }

#if DOTWEEN
        private void Awake()
        {
            // 场景级设置：确保 DOTween 使用 unscaled time（暂停时也能动画）
            DOTween.SetTweensCapacity(500, 100);
        }
#endif
    }
}
