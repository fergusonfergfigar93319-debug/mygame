using UnityEngine;

namespace PenguinRun
{
    /// <summary>
    /// 远景辉光 Billboard 工具：程序生成柔边贴图（月亮/光晕/极光带），
    /// 替代 Sphere/Cube 拼装的远景天体。纯表现件，不参与判定。
    /// </summary>
    internal static class SceneryBillboards
    {
        private static Texture2D moonTexture;
        private static Texture2D haloTexture;
        private static Texture2D auroraTexture;
        private static Mesh quadMesh;
        private static Shader spriteShader;

        public static GameObject CreateGlowQuad(string name, Vector3 position, Vector2 size, Color tint, Texture2D texture, bool billboard = true)
        {
            var go = new GameObject(name);
            go.transform.position = position;
            go.transform.localScale = new Vector3(size.x, size.y, 1f);

            var mf = go.AddComponent<MeshFilter>();
            mf.sharedMesh = GetQuadMesh();
            var mr = go.AddComponent<MeshRenderer>();
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            mr.receiveShadows = false;

            spriteShader ??= Shader.Find("Sprites/Default");
            var material = new Material(spriteShader) { name = $"Glow {name}" };
            material.mainTexture = texture;
            material.color = tint;
            material.renderQueue = 3000;
            mr.sharedMaterial = material;

            if (billboard)
            {
                go.AddComponent<FaceCameraBillboard>();
            }

            return go;
        }

        /// <summary>月面：亮核 + 柔边 + 两个暗斑。</summary>
        public static Texture2D MoonTexture()
        {
            if (moonTexture != null) return moonTexture;
            moonTexture = BuildRadial(64, r =>
            {
                var alpha = Mathf.Clamp01(1.06f - Mathf.Pow(r, 6f) * 1.4f);
                return r > 0.82f ? Mathf.Clamp01((1f - r) / 0.18f) : alpha;
            });
            // 暗斑（环形山暗示）
            StampDim(moonTexture, 22, 26, 6, 0.82f);
            StampDim(moonTexture, 40, 40, 4, 0.86f);
            StampDim(moonTexture, 30, 44, 3, 0.88f);
            moonTexture.Apply();
            return moonTexture;
        }

        /// <summary>柔和圆形光晕。</summary>
        public static Texture2D HaloTexture()
        {
            if (haloTexture != null) return haloTexture;
            haloTexture = BuildRadial(64, r => Mathf.Pow(Mathf.Clamp01(1f - r), 2.2f));
            haloTexture.Apply();
            return haloTexture;
        }

        /// <summary>极光竖向柔边光带。</summary>
        public static Texture2D AuroraBandTexture()
        {
            if (auroraTexture != null) return auroraTexture;
            const int w = 32;
            const int h = 64;
            auroraTexture = NewTexture(w, h);
            for (var y = 0; y < h; y++)
            {
                var v = y / (h - 1f);
                var vertical = Mathf.Sin(v * Mathf.PI);
                for (var x = 0; x < w; x++)
                {
                    var u = x / (w - 1f);
                    var horizontal = Mathf.Pow(Mathf.Sin(u * Mathf.PI), 1.6f);
                    var streak = 0.78f + Mathf.PerlinNoise(u * 3.2f, v * 6.5f) * 0.35f;
                    var a = Mathf.Clamp01(horizontal * vertical * streak);
                    auroraTexture.SetPixel(x, y, new Color(1f, 1f, 1f, a));
                }
            }
            auroraTexture.Apply();
            return auroraTexture;
        }

        private static Texture2D BuildRadial(int sizePx, System.Func<float, float> alphaByRadius)
        {
            var tex = NewTexture(sizePx, sizePx);
            var half = (sizePx - 1) * 0.5f;
            for (var y = 0; y < sizePx; y++)
            {
                for (var x = 0; x < sizePx; x++)
                {
                    var r = Mathf.Sqrt((x - half) * (x - half) + (y - half) * (y - half)) / half;
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, Mathf.Clamp01(alphaByRadius(Mathf.Clamp01(r)))));
                }
            }
            return tex;
        }

        private static void StampDim(Texture2D tex, int cx, int cy, int radius, float factor)
        {
            for (var y = cy - radius; y <= cy + radius; y++)
            {
                for (var x = cx - radius; x <= cx + radius; x++)
                {
                    if (x < 0 || y < 0 || x >= tex.width || y >= tex.height) continue;
                    var d = Mathf.Sqrt((x - cx) * (x - cx) + (y - cy) * (y - cy)) / radius;
                    if (d > 1f) continue;
                    var c = tex.GetPixel(x, y);
                    var dim = Mathf.Lerp(factor, 1f, d);
                    tex.SetPixel(x, y, new Color(c.r * dim, c.g * dim, c.b * dim, c.a));
                }
            }
        }

        private static Texture2D NewTexture(int w, int h) =>
            new(w, h, TextureFormat.RGBA32, false)
            {
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
                hideFlags = HideFlags.HideAndDontSave,
            };

        private static Mesh GetQuadMesh()
        {
            if (quadMesh != null) return quadMesh;
            quadMesh = new Mesh { name = "Scenery Glow Quad" };
            quadMesh.vertices = new[]
            {
                new Vector3(-0.5f, -0.5f, 0f), new Vector3(0.5f, -0.5f, 0f),
                new Vector3(0.5f, 0.5f, 0f), new Vector3(-0.5f, 0.5f, 0f),
            };
            quadMesh.uv = new[] { new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(0f, 1f) };
            quadMesh.triangles = new[] { 0, 2, 1, 0, 3, 2 };
            quadMesh.RecalculateNormals();
            quadMesh.RecalculateBounds();
            return quadMesh;
        }

        /// <summary>朝向主相机的 Billboard 组件。</summary>
        internal sealed class FaceCameraBillboard : MonoBehaviour
        {
            private Transform cam;

            private void LateUpdate()
            {
                if (cam == null)
                {
                    var main = Camera.main;
                    if (main == null) return;
                    cam = main.transform;
                }

                transform.rotation = Quaternion.LookRotation(transform.position - cam.position, Vector3.up);
            }
        }
    }
}
