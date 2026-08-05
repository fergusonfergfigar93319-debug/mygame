#if UNITY_EDITOR
using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace PenguinRun.Editor
{
    /// <summary>
    /// Imports CC0 packs dropped into _vendor/packs (itch manual downloads).
    /// Cherry-picks a curated FBX set into Resources/PenguinRun/Models.
    /// </summary>
    internal static class VendorCc0PackImporter
    {
        private const string VendorPacks = "_vendor/packs";
        private const string VendorExtract = "_vendor/extracted";
        private const string ModelsOut = "Assets/Resources/PenguinRun/Models";

        [MenuItem("PenguinRun/Assets/Import Vendor CC0 Packs")]
        public static void ImportFromMenu()
        {
            var report = Import();
            Debug.Log($"[VendorCc0PackImporter] {report}");
            EditorUtility.DisplayDialog("Vendor CC0 Import", report, "OK");
        }

        public static void ImportFromCommandLine()
        {
            try
            {
                Debug.Log($"[VendorCc0PackImporter] {Import()}");
                EditorApplication.Exit(0);
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
                EditorApplication.Exit(1);
            }
        }

        private static string Import()
        {
            var root = Directory.GetParent(Application.dataPath)!.FullName;
            var packsDir = Path.Combine(root, VendorPacks);
            var extractDir = Path.Combine(root, VendorExtract);
            Directory.CreateDirectory(packsDir);
            Directory.CreateDirectory(extractDir);

            var imported = 0;
            foreach (var zip in Directory.GetFiles(packsDir, "*.zip"))
            {
                var len = new FileInfo(zip).Length;
                if (len < 200_000)
                {
                    Debug.LogWarning($"[VendorCc0PackImporter] skip tiny/non-zip: {Path.GetFileName(zip)} ({len} bytes)");
                    continue;
                }

                var name = Path.GetFileNameWithoutExtension(zip).ToLowerInvariant();
                var dest = Path.Combine(extractDir, name);
                if (Directory.Exists(dest)) Directory.Delete(dest, true);
                ZipFile.ExtractToDirectory(zip, dest);
                imported += CherryPick(dest, name);
            }

            AssetDatabase.Refresh();
            return $"processed packs; copied {imported} model files into {ModelsOut}";
        }

        private static int CherryPick(string extractRoot, string packKey)
        {
            var count = 0;
            var fbxFiles = Directory.GetFiles(extractRoot, "*.fbx", SearchOption.AllDirectories);
            string[] wanted;
            if (packKey.Contains("kaykit") || packKey.Contains("forest"))
            {
                wanted = new[] { "tree", "rock", "bush", "grass", "cliff", "terrain" };
            }
            else if (packKey.Contains("stylized") || packKey.Contains("nature"))
            {
                wanted = new[] { "tree", "rock", "bush", "plant", "grass", "flower", "pine", "birch" };
            }
            else return 0;

            foreach (var src in fbxFiles)
            {
                var file = Path.GetFileName(src);
                var lower = file.ToLowerInvariant();
                if (!wanted.Any(w => lower.Contains(w))) continue;
                // Cap per pack to avoid dumping hundreds of files.
                if (count >= 40) break;
                var destName = "vendor_" + Path.GetFileNameWithoutExtension(file)
                    .Replace(' ', '_')
                    .Replace('-', '_') + ".fbx";
                var dest = Path.Combine(ModelsOut, destName);
                File.Copy(src, dest, true);
                count++;
            }

            return count;
        }
    }
}
#endif
