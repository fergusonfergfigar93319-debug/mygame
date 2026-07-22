using UnityEngine;

namespace PenguinRun
{
    /// <summary>
    /// 主题机关的运行时视觉动画（摆动/旋转/浮动/脉冲/微颤）。
    /// 只作用于 CustomVisual 表现件的局部变换，绝不参与碰撞或机关判定。
    /// </summary>
    public sealed class ThemeMechanicMotion : MonoBehaviour
    {
        [System.Flags]
        private enum Motion
        {
            None = 0,
            SwingZ = 1 << 0,   // 钟摆（吊木）
            SpinY = 1 << 1,    // 匀速自旋（灯塔/棱镜）
            Bob = 1 << 2,      // 上下浮动（浮台/气泡）
            Pulse = 1 << 3,    // 呼吸缩放（水晶/气泡）
            Shiver = 1 << 4,   // 细微颤动（裂板/残骸）
            RollZ = 1 << 5,    // 波浪侧滚（波浪隧道）
        }

        private Motion motion;
        private float amplitude = 1f;
        private float frequency = 1f;
        private float phase;
        private Vector3 basePos;
        private Quaternion baseRot;
        private Vector3 baseScale;

        /// <summary>按机关路径配置动画风格。</summary>
        public static void Configure(GameObject visual, string apparatusPath)
        {
            if (visual == null || string.IsNullOrEmpty(apparatusPath)) return;
            var m = visual.GetComponent<ThemeMechanicMotion>();
            if (m == null) m = visual.AddComponent<ThemeMechanicMotion>();

            (m.motion, m.amplitude, m.frequency) = apparatusPath switch
            {
                "ice/cracked_plate" => (Motion.Shiver, 0.35f, 1f),
                "ice/crystal_rebound_gate" => (Motion.Pulse, 0.05f, 1.6f),
                "ice/floe_bridge" => (Motion.Bob | Motion.RollZ, 0.35f, 0.7f),
                "cedar/swing_log" => (Motion.SwingZ, 1f, 0.55f),
                "cedar/root_gallery" => (Motion.RollZ, 0.35f, 0.5f),
                "cedar/ruin_escape" => (Motion.Shiver, 0.6f, 1.4f),
                "aurora/polarity_gate" => (Motion.Pulse | Motion.SpinY, 0.4f, 0.35f),
                "aurora/prism_beam" => (Motion.SpinY, 1f, 0.4f),
                "aurora/magnetic_rebuild" => (Motion.SpinY | Motion.Bob, 0.45f, 0.5f),
                "mist/decoy_shadow" => (Motion.Pulse | Motion.Bob, 0.5f, 0.8f),
                "mist/rotating_beacon" => (Motion.SpinY, 1.6f, 0.5f),
                "mist/floodgate_chase" => (Motion.Bob, 0.4f, 1.1f),
                "ocean/coral_lift" => (Motion.Bob, 1f, 0.55f),
                "ocean/bubble_vault" => (Motion.Bob | Motion.Pulse, 0.7f, 0.9f),
                "ocean/wave_tunnel" => (Motion.RollZ, 0.7f, 0.65f),
                "sky/wind_gate" => (Motion.SpinY | Motion.Shiver, 0.5f, 0.9f),
                "sky/cloud_platform" => (Motion.Bob, 0.8f, 0.45f),
                "sky/island_glide_chain" => (Motion.Bob | Motion.RollZ, 0.55f, 0.5f),
                _ => (Motion.Pulse, 0.3f, 0.8f),
            };
            m.CaptureBaseline();
        }

        private void CaptureBaseline()
        {
            basePos = transform.localPosition;
            baseRot = transform.localRotation;
            baseScale = transform.localScale;
            phase = Random.value * Mathf.PI * 2f;
        }

        private void Update()
        {
            if (motion == Motion.None) return;
            var t = Time.time * frequency * Mathf.PI * 2f + phase;

            var pos = basePos;
            var rot = baseRot;
            var scale = baseScale;

            if ((motion & Motion.SwingZ) != 0)
            {
                rot *= Quaternion.Euler(0f, 0f, Mathf.Sin(t) * 26f * amplitude);
            }

            if ((motion & Motion.SpinY) != 0)
            {
                rot *= Quaternion.Euler(0f, Time.time * 42f * amplitude, 0f);
            }

            if ((motion & Motion.RollZ) != 0)
            {
                rot *= Quaternion.Euler(Mathf.Sin(t * 0.8f) * 4f * amplitude, 0f, Mathf.Sin(t) * 7f * amplitude);
            }

            if ((motion & Motion.Bob) != 0)
            {
                pos += Vector3.up * (Mathf.Sin(t) * 0.16f * amplitude);
            }

            if ((motion & Motion.Shiver) != 0)
            {
                pos += new Vector3(
                    (Mathf.PerlinNoise(Time.time * 9f, phase) - 0.5f) * 0.05f * amplitude,
                    0f,
                    (Mathf.PerlinNoise(phase, Time.time * 9f) - 0.5f) * 0.05f * amplitude);
            }

            if ((motion & Motion.Pulse) != 0)
            {
                scale = baseScale * (1f + Mathf.Sin(t) * 0.06f * (0.5f + amplitude));
            }

            transform.localPosition = pos;
            transform.localRotation = rot;
            transform.localScale = scale;
        }
    }
}
