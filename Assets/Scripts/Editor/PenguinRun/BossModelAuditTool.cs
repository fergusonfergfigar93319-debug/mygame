using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace PenguinRun.EditorTools
{
    /// <summary>
    /// 批处理审计：列出 Bosses 目录每个模型的动画片段、层级锚点与三角数，
    /// 输出到 _tmp/boss-model-report.txt 供接线核对。
    /// </summary>
    internal static class BossModelAuditTool
    {
        public static void Run()
        {
            var sb = new StringBuilder();
            var guids = AssetDatabase.FindAssets("t:Model", new[] { "Assets/Resources/PenguinRun/Models/Bosses" });
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                sb.AppendLine($"=== {path}");

                var go = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (go == null)
                {
                    sb.AppendLine("  LOAD FAILED");
                    continue;
                }

                var tris = 0;
                foreach (var mf in go.GetComponentsInChildren<MeshFilter>(true))
                    if (mf.sharedMesh != null) tris += mf.sharedMesh.triangles.Length / 3;
                foreach (var smr in go.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                    if (smr.sharedMesh != null) tris += smr.sharedMesh.triangles.Length / 3;
                sb.AppendLine($"  Triangles: {tris}");

                var bounds = new Bounds(Vector3.zero, Vector3.zero);
                var hasBounds = false;
                foreach (var r in go.GetComponentsInChildren<Renderer>(true))
                {
                    if (!hasBounds) { bounds = r.bounds; hasBounds = true; }
                    else bounds.Encapsulate(r.bounds);
                }
                sb.AppendLine($"  Bounds: center={bounds.center} size={bounds.size}");

                foreach (var asset in AssetDatabase.LoadAllAssetsAtPath(path))
                {
                    if (asset is AnimationClip clip && (clip.hideFlags & HideFlags.HideInHierarchy) == 0)
                        sb.AppendLine($"  Clip: {clip.name}  len={clip.length:0.00}s legacy={clip.legacy}");
                }

                sb.AppendLine("  Hierarchy:");
                DumpHierarchy(go.transform, sb, 4, 2);
            }

            Directory.CreateDirectory("_tmp");
            File.WriteAllText("_tmp/boss-model-report.txt", sb.ToString());
            Debug.Log($"BossModelAuditTool report written ({guids.Length} models)");
        }

        private static void DumpHierarchy(Transform t, StringBuilder sb, int indent, int depthLeft)
        {
            sb.Append(' ', indent).AppendLine(t.name);
            if (depthLeft <= 0) return;
            for (var i = 0; i < t.childCount; i++)
                DumpHierarchy(t.GetChild(i), sb, indent + 2, depthLeft - 1);
        }
    }
}
