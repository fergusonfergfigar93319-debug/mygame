using System.Collections.Generic;
using UnityEngine;

namespace PenguinRun
{
    internal sealed class BossFxPoolTag : MonoBehaviour
    {
        public PrimitiveType Primitive;
    }

    /// <summary>
    /// 复用 Boss 攻击 FX 几何体，降低每招 Destroy 带来的 GC。
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

            if (!active.Contains(go))
            {
                active.Add(go);
            }

            return go;
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
