using UnityEngine;

namespace PenguinRun
{
    /// <summary>
    /// Curated CC0 roadside model ids + pack-relative scale multipliers.
    /// Keeps <see cref="PenguinRunnerGame"/> from growing past the file-size ratchet.
    /// </summary>
    public static class NaturePropCatalog
    {
        public static float ScaleFor(string modelName)
        {
            if (string.IsNullOrEmpty(modelName)) return 1f;
            // Bounds audit (VendorModelImportRepair): mushrooms/flowers are sub-meter and need a boost.
            if (modelName.IndexOf("Mushroom", System.StringComparison.OrdinalIgnoreCase) >= 0) return 2.4f;
            if (modelName.IndexOf("Flower", System.StringComparison.OrdinalIgnoreCase) >= 0) return 1.6f;
            // Stylized MegaKit trees ~7–9m; roadside budget prefers ~5m.
            if (modelName.StartsWith("vendor_sn/", System.StringComparison.Ordinal)) return 0.72f;
            // Ultimate Nature trees ~2.5–4.5m; nudge up toward Kenney roadside height.
            if (modelName.StartsWith("vendor_un_", System.StringComparison.Ordinal)) return 1.2f;
            // KayKit Color1 trees/rocks sit near Kenney unit scale.
            if (modelName.StartsWith("vendor_", System.StringComparison.Ordinal)) return 1.05f;
            return 1f;
        }

        public static Vector3 ApplyScale(string modelName, Vector3 scale) =>
            scale * ScaleFor(modelName);

        public static string IceLakeTree(int index) =>
            index % 2 == 0 ? "vendor_un_PineTree_Snow_1" : "vendor_un_BirchTree_Snow_1";

        public static string IceLakeAccent(int index) =>
            index % 2 == 0 ? "vendor_un_Rock_Snow_1" : "CubePets/cubepet_polar";

        public static string CedarTree() => "vendor_sn/Pine_1";

        public static string CedarAccent(int index) =>
            index % 3 == 0 ? "CubePets/cubepet_deer"
            : index % 2 == 0 ? "vendor_Bush_2_A_Color1" : "vendor_sn/Bush_Common";

        public static string AuroraTree(int index) =>
            index % 2 == 0 ? "vendor_un_BirchTree_Autumn_1" : "vendor_Tree_Bare_1_A_Color1";

        public static string AuroraAccent(int index) =>
            index % 2 == 0 ? "vendor_sn/Flower_3_Group" : "CubePets/cubepet_chick";

        public static string MistAccentA(int index) =>
            index % 2 == 0 ? "vendor_sn/Mushroom_Common" : "vendor_Rock_2_A_Color1";

        public static string MistAccentB(int index) =>
            index % 2 == 0 ? "CubePets/cubepet_fox" : "vendor_sn/DeadTree_1";

        public static string OceanTree() => "vendor_un_PalmTree_1";

        public static string SkyAccent(int index) =>
            index % 2 == 0 ? "crate_strong" : "vendor_un_Rock_1";

        public static string SnowPineLandmark() => "vendor_un_PineTree_1";

        public static string FogStoneLandmark() => "vendor_un_Rock_Moss_1";
    }
}
