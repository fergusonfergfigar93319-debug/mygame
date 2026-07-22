using UnityEngine;

namespace PenguinRun
{
    /// <summary>
    /// 动态障碍物行为基类。挂载到障碍物 GameObject 上，每帧由 SegmentSpawner.Tick 驱动。
    /// </summary>
    internal abstract class ObstacleBehavior : MonoBehaviour
    {
        /// <summary>每帧由 SegmentSpawner 调用。</summary>
        public abstract void Tick(float dt, float runDistance, float playerZ);

        /// <summary>此障碍物当前是否可以碰撞到玩家（用于动态碰撞开关）。</summary>
        public virtual bool IsCollidable => true;
    }

    /// <summary>定时开合的门：周期性开关，需要把握时机通过。</summary>
    internal sealed class TimedGate : ObstacleBehavior
    {
        private float timer;
        private float openDuration = 1.2f;
        private float closedDuration = 2.0f;
        private bool isOpen;
        private Transform visual;
        private ObstacleColliderSpec spec;
        private Vector3 closedScale;
        private float laneX;
        private float closedHeight = 2.0f;
        private float openHeight = 0.15f;

        public void Setup(Transform gateVisual, ObstacleColliderSpec colliderSpec, float laneCenterX)
        {
            visual = gateVisual;
            spec = colliderSpec;
            laneX = laneCenterX;
            timer = Random.value * (openDuration + closedDuration); // 随机偏移
            closedScale = gateVisual.localScale;
        }

        public override void Tick(float dt, float runDistance, float playerZ)
        {
            timer += dt;
            var cycle = openDuration + closedDuration;
            var tInCycle = timer % cycle;
            var wasOpen = isOpen;
            isOpen = tInCycle < openDuration;

            if (isOpen != wasOpen)
            {
                if (isOpen)
                {
                    // 门打开：缩小到地面高度
                    visual.localScale = new Vector3(closedScale.x, openHeight, closedScale.z);
                    visual.localPosition = new Vector3(0f, openHeight * 0.5f, 0f);
                }
                else
                {
                    // 门关闭：恢复完整高度
                    visual.localScale = closedScale;
                    visual.localPosition = new Vector3(0f, closedHeight * 0.5f, 0f);
                }
            }
        }

        public override bool IsCollidable => !isOpen;
    }

    /// <summary>左右移动的障碍物：在车道间横向移动，需要看准时机通行。</summary>
    internal sealed class MovingBarrier : ObstacleBehavior
    {
        // 移动的是障碍根节点（碰撞判定读根节点世界坐标），保证视觉与判定一致
        private Transform root;
        private float amplitude;
        private float frequency;
        private float baseX;
        private float phase;

        public void Setup(Transform obstacleRoot, float moveAmplitude, float moveFrequency, float laneCenterX)
        {
            root = obstacleRoot;
            amplitude = moveAmplitude;
            frequency = moveFrequency;
            baseX = laneCenterX;
            phase = Random.value * Mathf.PI * 2f;
        }

        public override void Tick(float dt, float runDistance, float playerZ)
        {
            phase += dt * frequency * Mathf.PI * 2f;
            var pos = root.position;
            pos.x = baseX + Mathf.Sin(phase) * amplitude;
            root.position = pos;
        }

        public override bool IsCollidable => true;
    }

    /// <summary>可击碎墙：冲刺状态下可撞碎，否则阻挡。</summary>
    internal sealed class ShatterWall : ObstacleBehavior
    {
        private Transform visual;
        private bool shattered;
        private float shatterTimer;
        private float shatterDuration = 0.5f;

        public void Setup(Transform wallVisual)
        {
            visual = wallVisual;
        }

        public bool TryShatter()
        {
            if (shattered) return false;
            shattered = true;
            shatterTimer = shatterDuration;
            return true;
        }

        public override void Tick(float dt, float runDistance, float playerZ)
        {
            if (!shattered) return;

            shatterTimer -= dt;
            var t = 1f - shatterTimer / shatterDuration;
            // 碎裂动画：缩小并淡出
            var scale = Mathf.Lerp(1f, 0.01f, t);
            visual.localScale = Vector3.one * scale;

            if (shatterTimer <= 0f)
            {
                visual.gameObject.SetActive(false);
            }
        }

        public override bool IsCollidable => !shattered;
    }
}
