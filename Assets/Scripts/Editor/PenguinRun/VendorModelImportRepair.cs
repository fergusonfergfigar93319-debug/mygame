#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace PenguinRun.Editor
{
    /// <summary>
    /// Expands stub ModelImporter metas for Quaternius vendor FBX and audits renderer bounds.
    /// </summary>
    internal static class VendorModelImportRepair
    {
        private const string ModelsRoot = "Assets/Resources/PenguinRun/Models";

        [MenuItem("PenguinRun/Assets/Repair Vendor Model Imports")]
        public static void RepairFromMenu()
        {
            var report = Repair();
            Debug.Log($"[VendorModelImportRepair] {report}");
            EditorUtility.DisplayDialog("Vendor Model Import Repair", report, "OK");
        }

        public static void RepairFromCommandLine()
        {
            try
            {
                Debug.Log($"[VendorModelImportRepair] {Repair()}");
                EditorApplication.Exit(0);
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
                EditorApplication.Exit(1);
            }
        }

        private static string Repair()
        {
            var paths = AssetDatabase.FindAssets("t:Model", new[] { ModelsRoot })
                .Select(AssetDatabase.GUIDToAssetPath)
                .Where(p =>
                {
                    var name = Path.GetFileName(p);
                    return name.StartsWith("vendor_un_", StringComparison.OrdinalIgnoreCase)
                           || p.Replace('\\', '/').Contains("/vendor_sn/");
                })
                .ToArray();

            var repaired = 0;
            foreach (var path in paths)
            {
                var importer = AssetImporter.GetAtPath(path) as ModelImporter;
                if (importer == null) continue;
                importer.globalScale = 1f;
                importer.useFileScale = true;
                importer.importAnimation = false;
                importer.animationType = ModelImporterAnimationType.None;
                importer.materialImportMode = ModelImporterMaterialImportMode.ImportViaMaterialDescription;
                importer.materialLocation = ModelImporterMaterialLocation.InPrefab;
                importer.materialSearch = ModelImporterMaterialSearch.Local;
                importer.SaveAndReimport();
                repaired++;
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            var audit = AuditBounds(paths);
            return $"repaired {repaired} models\n{audit}";
        }

        private static string AuditBounds(string[] paths)
        {
            var sb = new StringBuilder();
            sb.AppendLine("bounds (world AABB height @ scale 1):");
            foreach (var path in paths.OrderBy(p => p, StringComparer.OrdinalIgnoreCase))
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab == null)
                {
                    sb.AppendLine($"  MISSING {path}");
                    continue;
                }

                var renderers = prefab.GetComponentsInChildren<Renderer>(true);
                if (renderers.Length == 0)
                {
                    sb.AppendLine($"  EMPTY  {Path.GetFileNameWithoutExtension(path)}");
                    continue;
                }

                var bounds = renderers[0].bounds;
                for (var i = 1; i < renderers.Length; i++)
                    bounds.Encapsulate(renderers[i].bounds);
                sb.AppendLine($"  {Path.GetFileNameWithoutExtension(path),-36} h={bounds.size.y:0.00}  xz={bounds.size.x:0.00}x{bounds.size.z:0.00}");
            }

            Debug.Log($"[VendorModelImportRepair]\n{sb}");
            return sb.ToString();
        }
    }
}
#endif
