using UnityEngine;

namespace PenguinRun
{
    internal static class RunnerVisuals
    {
        private static Mesh cubeMesh;
        private static Mesh sphereMesh;
        private static Mesh cylinderMesh;
        private static Mesh capsuleMesh;

        public static GameObject CreatePrimitive(string name, PrimitiveType type, Vector3 position, Vector3 scale, Color color, bool transparent = false)
        {
            var go = new GameObject(name);
            go.name = name;
            go.transform.position = position;
            go.transform.localScale = scale;
            var meshFilter = go.AddComponent<MeshFilter>();
            meshFilter.sharedMesh = GetMesh(type);
            var renderer = go.AddComponent<MeshRenderer>();
            ApplyColor(renderer, color, transparent);
            return go;
        }

        public static void ApplyColor(Renderer renderer, Color color, bool transparent = false)
        {
            renderer.sharedMaterial = CreateMaterial(color, transparent);
        }

        public static Material CreateMaterial(Color color, bool transparent = false)
        {
            var shader =
                Shader.Find("Universal Render Pipeline/Unlit") ??
                Shader.Find("Universal Render Pipeline/Lit") ??
                Shader.Find("Unlit/Color") ??
                Shader.Find("Sprites/Default") ??
                Shader.Find("Standard");

            var material = new Material(shader);
            SetColor(material, color);
            if (transparent || color.a < 0.99f)
            {
                MakeTransparent(material);
            }

            return material;
        }

        public static void SetColor(Material material, Color color)
        {
            if (material.HasProperty("_BaseColor"))
            {
                material.SetColor("_BaseColor", color);
            }
            if (material.HasProperty("_Color"))
            {
                material.SetColor("_Color", color);
            }
        }

        public static Color GetColor(Material material)
        {
            if (material.HasProperty("_BaseColor"))
            {
                return material.GetColor("_BaseColor");
            }
            return material.HasProperty("_Color") ? material.GetColor("_Color") : Color.white;
        }

        private static void MakeTransparent(Material material)
        {
            if (material.HasProperty("_Surface"))
            {
                material.SetFloat("_Surface", 1f);
            }
            if (material.HasProperty("_Blend"))
            {
                material.SetFloat("_Blend", 0f);
            }
            material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            material.SetInt("_ZWrite", 0);
            material.DisableKeyword("_ALPHATEST_ON");
            material.EnableKeyword("_ALPHABLEND_ON");
            material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            material.renderQueue = 3000;
        }

        private static Mesh GetMesh(PrimitiveType type)
        {
            switch (type)
            {
                case PrimitiveType.Sphere:
                    return sphereMesh ??= LoadBuiltinMesh("Sphere.fbx") ?? CreateSphereMesh();
                case PrimitiveType.Capsule:
                    return capsuleMesh ??= LoadBuiltinMesh("Capsule.fbx") ?? LoadBuiltinMesh("Sphere.fbx") ?? CreateSphereMesh();
                case PrimitiveType.Cylinder:
                    return cylinderMesh ??= LoadBuiltinMesh("Cylinder.fbx") ?? CreateCylinderMesh();
                case PrimitiveType.Cube:
                case PrimitiveType.Plane:
                case PrimitiveType.Quad:
                default:
                    return cubeMesh ??= LoadBuiltinMesh("Cube.fbx") ?? CreateCubeMesh();
            }
        }

        private static Mesh LoadBuiltinMesh(string name)
        {
            return Resources.GetBuiltinResource<Mesh>(name);
        }

        private static Mesh CreateCubeMesh()
        {
            var mesh = new Mesh { name = "Runtime Cube Mesh" };
            mesh.vertices =
                new[]
                {
                    new Vector3(-0.5f, -0.5f, -0.5f), new Vector3(0.5f, -0.5f, -0.5f), new Vector3(0.5f, 0.5f, -0.5f), new Vector3(-0.5f, 0.5f, -0.5f),
                    new Vector3(-0.5f, -0.5f, 0.5f), new Vector3(0.5f, -0.5f, 0.5f), new Vector3(0.5f, 0.5f, 0.5f), new Vector3(-0.5f, 0.5f, 0.5f),
                };
            mesh.triangles =
                new[]
                {
                    0, 2, 1, 0, 3, 2,
                    4, 5, 6, 4, 6, 7,
                    0, 1, 5, 0, 5, 4,
                    2, 3, 7, 2, 7, 6,
                    0, 4, 7, 0, 7, 3,
                    1, 2, 6, 1, 6, 5,
                };
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        private static Mesh CreateSphereMesh()
        {
            const int latSegments = 10;
            const int lonSegments = 16;
            var vertices = new Vector3[(latSegments + 1) * (lonSegments + 1)];
            var triangles = new int[latSegments * lonSegments * 6];
            var v = 0;
            for (var lat = 0; lat <= latSegments; lat++)
            {
                var theta = Mathf.PI * lat / latSegments;
                var y = Mathf.Cos(theta) * 0.5f;
                var radius = Mathf.Sin(theta) * 0.5f;
                for (var lon = 0; lon <= lonSegments; lon++)
                {
                    var phi = 2f * Mathf.PI * lon / lonSegments;
                    vertices[v++] = new Vector3(Mathf.Cos(phi) * radius, y, Mathf.Sin(phi) * radius);
                }
            }

            var t = 0;
            for (var lat = 0; lat < latSegments; lat++)
            {
                for (var lon = 0; lon < lonSegments; lon++)
                {
                    var current = lat * (lonSegments + 1) + lon;
                    var next = current + lonSegments + 1;
                    triangles[t++] = current;
                    triangles[t++] = next;
                    triangles[t++] = current + 1;
                    triangles[t++] = current + 1;
                    triangles[t++] = next;
                    triangles[t++] = next + 1;
                }
            }

            var mesh = new Mesh { name = "Runtime Sphere Mesh", vertices = vertices, triangles = triangles };
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        private static Mesh CreateCylinderMesh()
        {
            const int segments = 18;
            var vertices = new Vector3[segments * 2 + 2];
            var triangles = new int[segments * 12];
            vertices[segments * 2] = new Vector3(0f, 0.5f, 0f);
            vertices[segments * 2 + 1] = new Vector3(0f, -0.5f, 0f);
            for (var i = 0; i < segments; i++)
            {
                var a = 2f * Mathf.PI * i / segments;
                var x = Mathf.Cos(a) * 0.5f;
                var z = Mathf.Sin(a) * 0.5f;
                vertices[i * 2] = new Vector3(x, 0.5f, z);
                vertices[i * 2 + 1] = new Vector3(x, -0.5f, z);
            }

            var topCenter = segments * 2;
            var bottomCenter = segments * 2 + 1;
            var t = 0;
            for (var i = 0; i < segments; i++)
            {
                var next = (i + 1) % segments;
                var top = i * 2;
                var bottom = top + 1;
                var nextTop = next * 2;
                var nextBottom = nextTop + 1;

                triangles[t++] = top;
                triangles[t++] = bottom;
                triangles[t++] = nextTop;
                triangles[t++] = nextTop;
                triangles[t++] = bottom;
                triangles[t++] = nextBottom;

                triangles[t++] = topCenter;
                triangles[t++] = nextTop;
                triangles[t++] = top;

                triangles[t++] = bottomCenter;
                triangles[t++] = bottom;
                triangles[t++] = nextBottom;
            }

            var mesh = new Mesh { name = "Runtime Cylinder Mesh", vertices = vertices, triangles = triangles };
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }
    }
}
