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
            // Kenney Nature mushrooms are already ~2.5 m; the generic mushroom boost is for vendor_sn.
            if (modelName == "mushroom_redTall" || modelName == "mushroom_tanGroup"
                || modelName == "mushroom_redGroup") return 1.15f;
            // Bounds audit (VendorModelImportRepair): mushrooms/flowers are sub-meter and need a boost.
            if (modelName.IndexOf("Mushroom", System.StringComparison.OrdinalIgnoreCase) >= 0) return 2.4f;
            if (modelName.IndexOf("Flower", System.StringComparison.OrdinalIgnoreCase) >= 0) return 1.6f;
            if (modelName == "tree_pine_snow_small") return 1.85f;
            if (modelName == "block_snow_large") return 0.82f;
            if (modelName == "log_large") return 1.2f;
            if (modelName == "log_single") return 0.5f;
            if (modelName == "campfire_stones") return 0.65f;
            if (modelName == "tree_palmTall") return 0.88f;
            if (modelName == "cliff_block_rock") return 0.68f;
            if (modelName == "cliff_half_stone") return 0.38f;
            if (modelName == "cliff_block_stone") return 0.32f;
            if (modelName == "rock_largeA") return 0.4f;
            if (modelName == "rock_tallA") return 0.22f;
            if (modelName == "block_snow" || modelName == "block_snow_hexagon") return 0.9f;
            // Kenney Nature stone_tallA/B are ~9–10 m meshes; roadside crystals need ~2 m.
            if (modelName == "stone_tallA" || modelName == "stone_tallB") return 0.24f;
            if (modelName == "rock_smallA") return 1.4f;
            if (modelName == "prop_crate") return 1.15f;
            if (modelName == "sky_cloud") return 1.35f;
            if (modelName == "vendor_un_Lilypad") return 2.6f;
            if (modelName.IndexOf("Fern", System.StringComparison.OrdinalIgnoreCase) >= 0) return 1.8f;
            if (modelName.IndexOf("Grass_Common", System.StringComparison.OrdinalIgnoreCase) >= 0) return 1.55f;
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
            index % 3 == 0 ? "CubePets/cubepet_polar"
            : index % 3 == 1 ? "vendor_un_Rock_Snow_1"
            : "vendor_un_Bush_Snow_1";

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

        public static string IceLakeFar(int index) =>
            index % 2 == 0 ? "tree_pine_snow_small" : "block_snow_large";

        public static string CedarFar(int index) =>
            index % 2 == 0 ? "vendor_Tree_2_A_Color1" : "log_large";

        public static string AuroraFar(int index) =>
            index % 2 == 0 ? "vendor_un_Willow_Autumn_1" : "vendor_un_CommonTree_Autumn_1";

        public static string MistFar(int index) =>
            index % 2 == 0 ? "vendor_un_Willow_Dead_1" : "vendor_sn/Rock_Medium_1";

        public static string OceanFar(int index) =>
            index % 2 == 0 ? "tree_palmTall" : "vendor_sn/Plant_1_Big";

        public static string SkyFar(int index) =>
            index % 2 == 0 ? "vendor_Tree_3_A_Color1" : "cliff_block_rock";

        public static string IceCrystal(int index) =>
            index % 2 == 0 ? "vendor_un_Rock_Snow_1" : "stone_tallA";

        public static string SnowBank(int index) =>
            index % 2 == 0 ? "block_snow" : "block_snow_hexagon";

        public static string OceanWeed(int index) =>
            index % 3 == 0 ? "vendor_sn/Fern_1"
            : index % 3 == 1 ? "vendor_sn/Grass_Common_Tall"
            : "vendor_Grass_2_A_Color1";

        public static string OceanShell(int index) =>
            index % 2 == 0 ? "vendor_un_Lilypad" : "flower_yellowA";

        public static string CoralReef(int index) =>
            index % 3 == 0 ? "mushroom_redTall"
            : index % 3 == 1 ? "mushroom_tanGroup"
            : "vendor_sn/Mushroom_Laetiporus";

        public static string TreasureChest() => "prop_crate";

        public static string CedarRuin(int index) =>
            index % 3 == 0 ? "log_single"
            : index % 3 == 1 ? "vendor_un_WoodLog_Moss"
            : "campfire_stones";

        public static string AuroraBeacon(int index) =>
            index % 2 == 0 ? "vendor_un_BushBerries_1" : "vendor_un_Flowers";

        public static string IceArch() => "cliff_half_stone";

        public static string SkyArch(int index) =>
            index % 2 == 0 ? "cliff_block_stone" : "rock_tallA";

        public static string MistVeil(int index) =>
            index % 2 == 0 ? "vendor_sn/Bush_Common_Flowers" : "vendor_Bush_4_A_Color1";

        public static string SkyCloud() => "sky_cloud";
    }
}
