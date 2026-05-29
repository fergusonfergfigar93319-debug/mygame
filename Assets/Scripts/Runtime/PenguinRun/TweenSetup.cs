#if DOTWEEN
using DG.Tweening;
#endif
using UnityEngine;

namespace PenguinRun
{
    /// <summary>
    /// DOTween 初始化和全局配置。挂到任意场景的 GameObject 上即可。
    /// 在 PenguinRunner 和 MainMenu 场景中都会自动创建。
    /// </summary>
    public sealed class TweenSetup : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void AutoInit()
        {
#if DOTWEEN
            // DOTween 从 OpenUPM 安装后自动初始化，此处设置全局参数
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
