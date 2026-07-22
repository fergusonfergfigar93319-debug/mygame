using UnityEngine;
using UnityEngine.Rendering.PostProcessing;

namespace PenguinRun
{
    /// <summary>
    /// 运行时后处理装配资源载体：由编辑器工具烘焙进 Resources 预制体，
    /// 保证 PostProcessResources / Profile 在真机构建中被打包引用。
    /// </summary>
    public sealed class PostFxRigAsset : MonoBehaviour
    {
        public PostProcessResources Resources;
        public PostProcessProfile Profile;
    }

    /// <summary>
    /// 跑酷后处理：Bloom（高阈值轻强度）+ Vignette（死亡加深）+ 轻量 Color Grading。
    /// 质量分档：Android 且非最高画质档时关闭 Bloom 只留 Vignette。
    /// 缺少 rig 资产时优雅跳过（不影响玩法）。
    /// </summary>
    internal sealed class RunnerPostFx
    {
        private const string RigResourcePath = "PenguinRun/PostFx/postfx_rig";
        private const int VolumeLayer = 1; // TransparentFX：专用于后处理体积

        private PostProcessVolume volume;
        private Vignette vignette;
        private Bloom bloom;
        private float baseVignetteIntensity;

        public static RunnerPostFx TryAttach(Camera camera)
        {
            if (camera == null) return null;
            var rigPrefab = Resources.Load<GameObject>(RigResourcePath);
            var rig = rigPrefab != null ? rigPrefab.GetComponent<PostFxRigAsset>() : null;
            if (rig == null || rig.Resources == null || rig.Profile == null)
            {
                return null;
            }

            var fx = new RunnerPostFx();

            var layer = camera.GetComponent<PostProcessLayer>();
            if (layer == null)
            {
                layer = camera.gameObject.AddComponent<PostProcessLayer>();
            }
            layer.Init(rig.Resources);
            layer.volumeTrigger = camera.transform;
            layer.volumeLayer = 1 << VolumeLayer;
            layer.antialiasingMode = PostProcessLayer.Antialiasing.None;

            var volumeGo = new GameObject("Runner PostFx Volume") { layer = VolumeLayer };
            fx.volume = volumeGo.AddComponent<PostProcessVolume>();
            fx.volume.isGlobal = true;
            fx.volume.priority = 10f;
            // 实例化 profile，避免运行时调参污染资产
            fx.volume.profile = Object.Instantiate(rig.Profile);

            fx.volume.profile.TryGetSettings(out fx.bloom);
            fx.volume.profile.TryGetSettings(out fx.vignette);
            fx.baseVignetteIntensity = fx.vignette != null ? fx.vignette.intensity.value : 0.2f;

            ApplyQualityTier(fx);
            return fx;
        }

        /// <summary>质量分档：Android 中低档默认关 Bloom（性能预算优先）。</summary>
        private static void ApplyQualityTier(RunnerPostFx fx)
        {
            if (fx.bloom == null) return;
#if UNITY_ANDROID && !UNITY_EDITOR
            var tier = QualitySettings.GetQualityLevel();
            var maxTier = QualitySettings.names.Length - 1;
            if (tier < maxTier)
            {
                fx.bloom.active = false;
            }
            else
            {
                fx.bloom.fastMode.Override(true);
            }
#endif
        }

        /// <summary>死亡瞬间暗角加深（0=常态，1=死亡定格）。</summary>
        public void SetDeathEmphasis(float amount01)
        {
            if (vignette == null) return;
            vignette.intensity.Override(Mathf.Lerp(baseVignetteIntensity, 0.44f, Mathf.Clamp01(amount01)));
        }

        public void Dispose()
        {
            if (volume != null)
            {
                if (volume.profile != null) Object.Destroy(volume.profile);
                Object.Destroy(volume.gameObject);
                volume = null;
            }
        }
    }
}
