#if UNITY_EDITOR
using System;
using System.Collections.Generic;
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
    /// Only processes known itch pack zip names (KayKit / Quaternius), never Kenney archives.
    /// </summary>
    internal static class VendorCc0PackImporter
    {
        private const string VendorPacks = "_vendor/packs";
        private const string VendorExtract = "_vendor/extracted";
        private const string ModelsOut = "Assets/Resources/PenguinRun/Models";

        private static readonly string[] KayKitAllow = {
            "Tree_1_A_Color1", "Tree_2_A_Color1", "Tree_3_A_Color1", "Tree_4_A_Color1",
            "Tree_Bare_1_A_Color1", "Tree_Bare_2_A_Color1",
            "Bush_1_A_Color1", "Bush_2_A_Color1", "Bush_3_A_Color1", "Bush_4_A_Color1",
            "Rock_1_A_Color1", "Rock_1_G_Color1", "Rock_2_A_Color1", "Rock_2_E_Color1",
            "Rock_3_A_Color1", "Rock_3_H_Color1", "Grass_1_A_Color1", "Grass_2_A_Color1"
        };

        private static readonly string[] UltimateAllow = {
            "PineTree_Snow_1", "BirchTree_Snow_1", "Rock_Snow_1", "Bush_Snow_1",
            "PineTree_1", "CommonTree_1", "WoodLog_Moss", "TreeStump_Moss",
            "BirchTree_Autumn_1", "Willow_Autumn_1", "CommonTree_Autumn_1",
            "Rock_Moss_1", "CommonTree_Dead_1", "BushBerries_1", "Willow_Dead_1",
            "PalmTree_1", "PalmTree_2", "Lilypad", "Rock_1", "Flowers"
        };

        private static readonly string[] StylizedAllow = {
            "Pine_1", "Pine_3", "CommonTree_1", "TwistedTree_1", "DeadTree_1",
            "Bush_Common", "Bush_Common_Flowers", "Mushroom_Common", "Mushroom_Laetiporus",
            "Rock_Medium_1", "Fern_1", "Grass_Common_Tall", "Flower_3_Group", "Plant_1_Big"
        };

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
                if (name.Contains("kenney"))
                {
                    Debug.Log($"[VendorCc0PackImporter] skip Kenney archive: {Path.GetFileName(zip)}");
                    continue;
                }

                var kind = ResolvePackKind(name);
                if (kind == PackKind.Unknown)
                {
                    Debug.LogWarning($"[VendorCc0PackImporter] skip unrecognized pack: {Path.GetFileName(zip)}");
                    continue;
                }

                var dest = Path.Combine(extractDir, name);
                if (Directory.Exists(dest)) Directory.Delete(dest, true);
                ZipFile.ExtractToDirectory(zip, dest);
                imported += CherryPick(dest, kind);
            }

            AssetDatabase.Refresh();
            return $"processed packs; copied {imported} model files into {ModelsOut}";
        }

        private enum PackKind
        {
            Unknown,
            KayKitForest,
            UltimateNature,
            StylizedNature
        }

        private static PackKind ResolvePackKind(string packKey)
        {
            if (packKey.Contains("kaykit") || (packKey.Contains("forest") && packKey.Contains("nature")))
                return PackKind.KayKitForest;
            if (packKey.Contains("ultimate"))
                return PackKind.UltimateNature;
            if (packKey.Contains("stylized") || packKey.Contains("megakit"))
                return PackKind.StylizedNature;
            if (packKey.Contains("quaternius") && packKey.Contains("nature"))
                return PackKind.UltimateNature;
            return PackKind.Unknown;
        }

        private static int CherryPick(string extractRoot, PackKind kind)
        {
            return kind switch
            {
                PackKind.KayKitForest => CopyAllowList(extractRoot, KayKitAllow, "vendor_", ModelsOut, preferUnityFbx: true)
                                         + CopyNamedTexture(extractRoot, "forest_texture.png", Path.Combine(ModelsOut, "kaykit_forest_texture.png")),
                PackKind.UltimateNature => CopyAllowList(extractRoot, UltimateAllow, "vendor_un_", ModelsOut, preferUnityFbx: false),
                PackKind.StylizedNature => CopyStylized(extractRoot),
                _ => 0
            };
        }

        private static int CopyStylized(string extractRoot)
        {
            var outDir = Path.Combine(ModelsOut, "vendor_sn");
            Directory.CreateDirectory(outDir);
            var count = CopyAllowList(extractRoot, StylizedAllow, "", outDir, preferUnityFbx: true);
            var texDir = Directory.GetDirectories(extractRoot, "Textures", SearchOption.AllDirectories).FirstOrDefault();
            if (texDir != null)
            {
                foreach (var png in Directory.GetFiles(texDir, "*.png"))
                    File.Copy(png, Path.Combine(outDir, Path.GetFileName(png)), true);
            }

            return count;
        }

        private static int CopyNamedTexture(string extractRoot, string fileName, string destPath)
        {
            var tex = Directory.GetFiles(extractRoot, fileName, SearchOption.AllDirectories).FirstOrDefault();
            if (tex == null) return 0;
            File.Copy(tex, destPath, true);
            return 0;
        }

        private static int CopyAllowList(string extractRoot, IReadOnlyList<string> allow, string prefix,
            string outDir, bool preferUnityFbx)
        {
            Directory.CreateDirectory(outDir);
            var searchRoots = new List<string>();
            if (preferUnityFbx)
            {
                searchRoots.AddRange(Directory.GetDirectories(extractRoot, "*fbx*", SearchOption.AllDirectories)
                    .Where(d => d.IndexOf("unity", StringComparison.OrdinalIgnoreCase) >= 0));
            }

            if (searchRoots.Count == 0)
                searchRoots.Add(extractRoot);

            var index = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var root in searchRoots)
            {
                foreach (var fbx in Directory.GetFiles(root, "*.fbx", SearchOption.AllDirectories))
                {
                    var stem = Path.GetFileNameWithoutExtension(fbx);
                    if (!index.ContainsKey(stem))
                        index[stem] = fbx;
                }
            }

            // Fallback scan whole extract if unity folder missed stems.
            if (preferUnityFbx)
            {
                foreach (var fbx in Directory.GetFiles(extractRoot, "*.fbx", SearchOption.AllDirectories))
                {
                    var stem = Path.GetFileNameWithoutExtension(fbx);
                    if (!index.ContainsKey(stem))
                        index[stem] = fbx;
                }
            }

            var count = 0;
            foreach (var stem in allow)
            {
                if (!index.TryGetValue(stem, out var src)) continue;
                var destName = prefix + stem + ".fbx";
                File.Copy(src, Path.Combine(outDir, destName), true);
                count++;
            }

            return count;
        }
    }
}
#endif
