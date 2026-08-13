using UnityEngine;

namespace PenguinRun
{
    /// <summary>
    /// In-run currency (鱼干) presentation: prefer Kenney coin FBX, else a compact
    /// layered gold disc. Sized for lane readability without drowning the penguin.
    /// </summary>
    internal static class CoinPickupVisual
    {
        private const float VendorCoinScale = 1.15f;
        private const float DiscRadius = 0.42f;

        public static GameObject Build(RunnerVisualFactory visualFactory)
        {
            var root = new GameObject("Fish Coin");
            var pivot = new GameObject("CoinPivot");
            pivot.transform.SetParent(root.transform, false);

            var hasVendor = TryAttachVendorCoin(pivot.transform, visualFactory);
            if (!hasVendor)
                BuildLayeredDisc(pivot.transform);

            AttachFloorGlow(root.transform);
            root.AddComponent<CoinSpinBob>().Bind(pivot.transform);
            return root;
        }

        private static bool TryAttachVendorCoin(Transform pivot, RunnerVisualFactory visualFactory)
        {
            // Prefer raw FBX — avoids stacking the 1.85× pickup prefab LOD sculpture on top of another disc.
            var prefab = Resources.Load<GameObject>("PenguinRun/Models/coin_gold");
            if (prefab == null && visualFactory != null)
            {
                // Fallback: sculpture prefab, then strip oversize LOD wrappers later.
                visualFactory.TryAttachPickup(pivot, PickupVisualId.FishSnack);
            }
            else if (prefab != null)
            {
                var instance = Object.Instantiate(prefab, pivot, false);
                instance.name = "coin_gold";
                instance.transform.localPosition = Vector3.zero;
                instance.transform.localRotation = Quaternion.identity;
                instance.transform.localScale = Vector3.one * VendorCoinScale;
                // 与编辑器路径（RunnerSculptureBuilder）一致：FBX 自带的 Collider 与拾取判定无关，剥掉防止物理查询误伤。
                foreach (var collider in instance.GetComponentsInChildren<Collider>(true))
                    Object.Destroy(collider);
            }
            else
            {
                return false;
            }

            var gold = CreateGoldMaterial(new Color(1f, 0.82f, 0.28f), 0.22f);
            var accent = CreateGoldMaterial(new Color(0.92f, 0.58f, 0.16f), 0.12f);
            var found = false;
            foreach (var renderer in pivot.GetComponentsInChildren<Renderer>(true))
            {
                if (renderer == null) continue;
                found = true;
                renderer.sharedMaterial = renderer.name.IndexOf("rim", System.StringComparison.OrdinalIgnoreCase) >= 0
                    ? accent
                    : gold;
                renderer.allowOcclusionWhenDynamic = false;
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }

            // Keep vendor mesh; hide huge procedural leftovers from old prefab bake if any.
            foreach (var t in pivot.GetComponentsInChildren<Transform>(true))
            {
                if (t.name is "CoinHalo" or "CoinDisc" or "CoinRim" or "PickupGlow")
                    t.gameObject.SetActive(false);
            }

            // Clamp runaway scale from old fish_snack CustomVisual (2.6 × 1.85).
            foreach (var t in pivot.GetComponentsInChildren<Transform>(true))
            {
                if (t == null || !t.name.StartsWith("coin_", System.StringComparison.Ordinal)) continue;
                t.localRotation = Quaternion.identity;
                t.localScale = Vector3.one * VendorCoinScale;
                t.localPosition = Vector3.zero;
                found = true;
            }

            return found;
        }

        private static void BuildLayeredDisc(Transform pivot)
        {
            var body = Part(pivot, "CoinBody", PrimitiveType.Cylinder,
                Vector3.zero, new Vector3(DiscRadius, 0.045f, DiscRadius),
                new Color(1f, 0.84f, 0.28f), 0.2f);
            body.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);

            var rim = Part(pivot, "CoinEdge", PrimitiveType.Cylinder,
                Vector3.zero, new Vector3(DiscRadius * 1.08f, 0.028f, DiscRadius * 1.08f),
                new Color(0.95f, 0.55f, 0.14f), 0.1f);
            rim.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);

            var inset = Part(pivot, "CoinInset", PrimitiveType.Cylinder,
                new Vector3(0f, 0f, -0.02f), new Vector3(DiscRadius * 0.62f, 0.02f, DiscRadius * 0.62f),
                new Color(1f, 0.72f, 0.22f), 0.28f);
            inset.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        }

        private static void AttachFloorGlow(Transform root)
        {
            var glow = RunnerVisuals.CreatePrimitive(
                "PickupGlow",
                PrimitiveType.Cylinder,
                new Vector3(0f, -0.48f, 0f),
                new Vector3(0.55f, 0.01f, 0.55f),
                new Color(1f, 0.85f, 0.35f, 0.16f),
                true);
            glow.transform.SetParent(root, false);
        }

        private static GameObject Part(
            Transform parent,
            string name,
            PrimitiveType type,
            Vector3 localPos,
            Vector3 localScale,
            Color color,
            float emission)
        {
            var go = RunnerVisuals.CreatePrimitive(name, type, localPos, localScale, color);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localScale = localScale;
            var renderer = go.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.sharedMaterial = CreateGoldMaterial(color, emission);
                renderer.allowOcclusionWhenDynamic = false;
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }
            return go;
        }

        private static Material CreateGoldMaterial(Color color, float emission)
        {
            // Prefer Standard so metallic gold reads; fall back via RunnerVisuals.
            var shader = Shader.Find("Standard") ?? Shader.Find("Universal Render Pipeline/Lit");
            var mat = shader != null
                ? new Material(shader) { color = color }
                : RunnerVisuals.CreateMaterial(color);
            if (mat == null) return null;
            if (mat.HasProperty("_Color")) mat.SetColor("_Color", color);
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
            if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", 0.72f);
            if (mat.HasProperty("_Glossiness")) mat.SetFloat("_Glossiness", 0.72f);
            if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic", 0.55f);
            if (mat.HasProperty("_Cull")) mat.SetInt("_Cull", 0);
            if (emission > 0.001f && mat.HasProperty("_EmissionColor"))
            {
                mat.EnableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor", new Color(color.r, color.g, color.b) * emission);
            }
            return mat;
        }

        private sealed class CoinSpinBob : MonoBehaviour
        {
            private Transform pivot;
            private float phase;

            public void Bind(Transform coinPivot) => pivot = coinPivot;

            private void OnEnable() => phase = Random.Range(0f, Mathf.PI * 2f);

            private void LateUpdate()
            {
                if (pivot == null) return;
                // 只动 pivot：root 留给 SegmentSpawner 滚道位移。
                var t = Time.time * 2.8f + phase;
                pivot.localPosition = new Vector3(0f, Mathf.Sin(t) * 0.06f, 0f);
                // Kenney / layered coin face ±Z；绕 Y 旋转是经典金币读法。
                pivot.localRotation = Quaternion.Euler(0f, (t * 95f) % 360f, 0f);
            }
        }
    }
}
