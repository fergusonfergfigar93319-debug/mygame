using UnityEngine;

namespace PenguinRun
{
    /// <summary>
    /// 距离里程碑事件调度器：每 500m 触发特殊事件，打破跑酷单调性。
    /// 事件包括：流星雨（金币从天而降）、暴风雪（密集飘雪 + 金币翻倍）、极光爆发（道具生成率提升）。
    /// </summary>
    internal sealed class WorldEventScheduler
    {
        public enum WorldEventType { None, MeteorShower, Blizzard, AuroraBurst }

        private float nextEventDistance = 500f;
        private const float EventInterval = 500f;
        private readonly bool suppressed;

        public WorldEventScheduler(bool suppressed = false)
        {
            this.suppressed = suppressed;
        }

        public WorldEventType ActiveEvent { get; private set; }
        public float EventTimer { get; private set; }
        public float EventDuration { get; private set; }
        public bool EventActive => ActiveEvent != WorldEventType.None;

        /// <summary>金币生成倍率（暴风雪期间翻倍）。</summary>
        public float CoinMultiplier => ActiveEvent == WorldEventType.Blizzard ? 2f : 1f;

        /// <summary>道具生成概率加成（极光爆发期间 +30%）。</summary>
        public float PowerUpChanceBonus => ActiveEvent == WorldEventType.AuroraBurst ? 0.3f : 0f;

        public string EventDisplayName => ActiveEvent switch
        {
            // The bundled CJK font does not contain these emoji/symbol glyphs on
            // every Android device. Text labels remain deterministic and legible.
            WorldEventType.MeteorShower => "[流星] 流星雨",
            WorldEventType.Blizzard => "[暴雪] 暴风雪",
            WorldEventType.AuroraBurst => "[极光] 极光爆发",
            _ => "",
        };

        public Color EventAmbientColor => ActiveEvent switch
        {
            WorldEventType.MeteorShower => new Color(1f, 0.82f, 0.4f, 0.15f),
            WorldEventType.Blizzard => new Color(0.6f, 0.9f, 1f, 0.2f),
            WorldEventType.AuroraBurst => new Color(0.3f, 1f, 0.7f, 0.15f),
            _ => Color.clear,
        };

        /// <summary>每帧调用，返回是否触发了新事件。</summary>
        public bool Tick(float distance, float deltaTime, bool allowNew = true)
        {
            if (suppressed || deltaTime <= 0f || float.IsNaN(deltaTime) || float.IsInfinity(deltaTime)) return false;

            if (EventActive)
            {
                EventTimer -= deltaTime;
                if (EventTimer <= 0f)
                {
                    ActiveEvent = WorldEventType.None;
                    EventTimer = 0f;
                    EventDuration = 0f;
                }
                return false;
            }

            if (allowNew && distance >= nextEventDistance)
            {
                nextEventDistance += EventInterval;
                TriggerRandomEvent();
                return true;
            }

            return false;
        }


        private void TriggerRandomEvent()
        {
            var roll = Random.value;
            if (roll < 0.35f)
            {
                ActiveEvent = WorldEventType.MeteorShower;
                EventDuration = 5f;
            }
            else if (roll < 0.65f)
            {
                ActiveEvent = WorldEventType.Blizzard;
                EventDuration = 8f;
            }
            else
            {
                ActiveEvent = WorldEventType.AuroraBurst;
                EventDuration = 6f;
            }
            EventTimer = EventDuration;
        }

        public void Reset()
        {
            ActiveEvent = WorldEventType.None;
            EventTimer = 0f;
            EventDuration = 0f;
            nextEventDistance = 500f;
        }
    }
}
