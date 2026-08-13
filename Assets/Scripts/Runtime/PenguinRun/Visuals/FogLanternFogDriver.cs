using UnityEngine;

namespace PenguinRun
{
    /// <summary>
    /// 雾灯（FogLantern）表现驱动：激活期间把线性雾起点/终点平滑外推，
    /// 提前照亮前方障碍（雾堤主题收益最明显）。纯表现层，不改任何判定；
    /// 只在插值仍在推进时写 RenderSettings。
    /// </summary>
    internal sealed class FogLanternFogDriver
    {
        private const float StartBonusMeters = 45f;
        private const float EndBonusMeters = 70f;

        private float themeFogStart = 55f;
        private float themeFogEnd = 180f;
        private float lantern01;

        /// <summary>主题切换/建场时设置雾距基线，并立即按当前雾灯强度写入。</summary>
        public void SetThemeBaseline(float fogStart, float fogEnd)
        {
            themeFogStart = fogStart;
            themeFogEnd = fogEnd;
            Apply();
        }

        public void Tick(float dt, bool lanternActive)
        {
            var target = lanternActive ? 1f : 0f;
            var next = Mathf.MoveTowards(lantern01, target, dt * 2.4f);
            if (Mathf.Approximately(next, lantern01)) return;
            lantern01 = next;
            Apply();
        }

        private void Apply()
        {
            RenderSettings.fogStartDistance = themeFogStart + StartBonusMeters * lantern01;
            RenderSettings.fogEndDistance = themeFogEnd + EndBonusMeters * lantern01;
        }
    }
}
