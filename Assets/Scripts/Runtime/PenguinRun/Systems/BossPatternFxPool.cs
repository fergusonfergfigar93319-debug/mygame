using System.Collections.Generic;
using UnityEngine;

namespace PenguinRun
{
    internal sealed class BossFxPoolTag : MonoBehaviour
    {
        public PrimitiveType Primitive;
        public ParticleSystem Sparkle;
    }

    /// <summary>
    /// 复用 Boss 攻击 FX 对象（发光几何体 + 附着粒子），降低每招 Destroy 带来的 GC。
    /// 池容量上限 48；End()/ReleaseAll 只归还不销毁（领域规则）。
    /// </summary>
    internal sealed class BossPatternFxPool
    {
        private const int MaxPoolSize = 48;
        private readonly Stack<GameObject> available = new();
        private readonly List<GameObject> active = new();

        public GameObject Rent(PrimitiveType type, string name, Vector3 worldPos, Vector3 scale, Color color, bool transparent, Transform parent)
        {
            var go = TryPopMatching(type);
            if (go == null)
            {
                if (active.Count >= MaxPoolSize) return null;
                go = RunnerVisuals.CreatePrimitive(name, type, worldPos, scale, color, transparent);
                var tag = go.AddComponent<BossFxPoolTag>();
                tag.Primitive = type;
                tag.Sparkle = CreateSparkleSystem(go.transform);
            }
            else
            {
                go.name = name;
                go.SetActive(true);
            }

            go.transform.SetParent(parent, false);
            go.transform.position = worldPos;
            go.transform.localScale = scale;
            go.transform.localRotation = Quaternion.identity;
            var rend = go.GetComponent<Renderer>();
            if (rend != null)
            {
                RunnerVisuals.ApplyColor(rend, color, transparent);
            }

            EmitSparkles(go, color);

            if (!active.Contains(go))
            {
                active.Add(go);
            }

            return go;
        }

        /// <summary>租出瞬间的粒子迸发：几何体 + 星尘组合（粒子随对象缩放反向补偿，保持均匀颗粒感）。</summary>
        private static void EmitSparkles(GameObject go, Color color)
        {
            var tag = go.GetComponent<BossFxPoolTag>();
            if (tag == null || tag.Sparkle == null) return;

            var ps = tag.Sparkle;
            var scale = go.transform.localScale;
            var maxAxis = Mathf.Max(0.05f, Mathf.Max(scale.x, Mathf.Max(scale.y, scale.z)));
            var emit = new ParticleSystem.EmitParams
            {
                startColor = new Color(color.r, color.g, color.b, Mathf.Clamp01(color.a + 0.3f)),
                startSize = Mathf.Clamp(0.09f * maxAxis, 0.06f, 0.32f) / maxAxis,
                startLifetime = Random.Range(0.35f, 0.6f),
            };

            var count = Mathf.Clamp(Mathf.RoundToInt(4f + maxAxis * 2f), 4, 10);
            for (var i = 0; i < count; i++)
            {
                var dir = Random.onUnitSphere;
                dir.y = Mathf.Abs(dir.y) * 0.7f + 0.3f;
                emit.velocity = dir * Random.Range(0.8f, 2.2f);
                emit.position = Vector3.zero;
                ps.Emit(emit, 1);
            }
        }

        private static ParticleSystem CreateSparkleSystem(Transform parent)
        {
            var go = new GameObject("Pool Sparkle");
            go.transform.SetParent(parent, false);
            var ps = go.AddComponent<ParticleSystem>();

            var main = ps.main;
            main.playOnAwake = false;
            main.loop = false;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.startSpeed = 0f;
            main.maxParticles = 24;
            main.gravityModifier = 0.35f;

            var emission = ps.emission;
            emission.enabled = false;

            var shape = ps.shape;
            shape.enabled = false;

            var renderer = ps.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.material = RunnerVisuals.CreateMaterial(new Color(1f, 1f, 1f, 0.9f), true);
            return ps;
        }

        private GameObject TryPopMatching(PrimitiveType type)
        {
            while (available.Count > 0)
            {
                var candidate = available.Pop();
                if (candidate == null) continue;
                var tag = candidate.GetComponent<BossFxPoolTag>();
                if (tag != null && tag.Primitive != type)
                {
                    Object.Destroy(candidate);
                    continue;
                }

                return candidate;
            }

            return null;
        }

        public void ReleaseAll()
        {
            for (var i = active.Count - 1; i >= 0; i--)
            {
                var go = active[i];
                if (go == null)
                {
                    active.RemoveAt(i);
                    continue;
                }

                go.SetActive(false);
                go.transform.SetParent(null, false);
                if (available.Count < MaxPoolSize)
                {
                    available.Push(go);
                }
                else
                {
                    Object.Destroy(go);
                }
            }

            active.Clear();
        }
    }
}
