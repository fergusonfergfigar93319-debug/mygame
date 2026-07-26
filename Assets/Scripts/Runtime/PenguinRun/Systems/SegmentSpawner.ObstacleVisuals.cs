using System.Collections.Generic;
using UnityEngine;

namespace PenguinRun
{
    /// <summary>
    /// SegmentSpawner 的障碍外观构建部分：程序化 Mesh、开源模型挂载、调色板染色与配饰。
    ///
    /// 这些方法全部是**非权威**表现层——碰撞规格由 ObstacleColliderSpec 决定，与这里生成
    /// 的网格无关。拆到独立文件是为了让这条既有的权威边界在文件树上可见：改「障碍长什么样」
    /// 不该碰到「障碍放在哪、怎么判定」那一半。
    ///
    /// 用 partial 而不是抽成独立类：成员、可见性与调用点全部不变，因此这次拆分不可能改变行为。
    /// 后续若要真正下沉为独立协作者，再从这里出发。
    /// </summary>
    internal sealed partial class SegmentSpawner
    {
        /// <summary>尝试用程序化 Mesh 构建障碍外观。返回 false 表示无匹配形状，应回退到 Primitive。</summary>
        private static bool TryBuildProceduralObstacle(Transform root, bool lowBarrier, SegmentObstaclePalette palette)
        {
            var mesh = lowBarrier
                ? ObstacleMeshBuilder.BuildSlideGate()
                : ObstacleMeshBuilder.BuildCrystalPillar();
            return mesh != null && AttachProceduralMesh(root, mesh, GetObstaclePaletteTint(palette));
        }

        private static Color GetObstaclePaletteTint(SegmentObstaclePalette palette) => palette switch
        {
            SegmentObstaclePalette.OceanCoral => new Color(0.95f, 0.5f, 0.55f),
            SegmentObstaclePalette.CedarWood => new Color(0.48f, 0.34f, 0.22f),
            SegmentObstaclePalette.AuroraGlow => new Color(0.42f, 0.36f, 0.65f),
            SegmentObstaclePalette.MistFog => new Color(0.62f, 0.7f, 0.78f),
            SegmentObstaclePalette.SkyCloud => new Color(0.72f, 0.82f, 1f),
            _ => new Color(0.22f, 0.52f, 0.72f),
        };

        private static bool AttachProceduralMesh(Transform root, Mesh mesh, Color tint, string objectName = "ProceduralMesh", Vector3 localPosition = default)
        {
            if (mesh == null) return false;

            var go = new GameObject(objectName);
            go.transform.SetParent(root, false);
            go.transform.localPosition = localPosition;
            go.transform.localRotation = Quaternion.identity;
            var mf = go.AddComponent<MeshFilter>();
            mf.sharedMesh = mesh;
            var mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = RunnerVisuals.CreateMaterial(tint, false);
            return true;
        }

        private bool TryBuildOpenModelObstacle(Transform root, bool lowBarrier, SegmentObstaclePalette palette)
        {
            string modelName;
            Vector3 localScale;
            Vector3 localPos;
            Color tint;

            // 每个调色板使用专属模型（消除 Cedar/Mist、IceLake/Aurora/Ocean 跨主题复用；只换外观不动碰撞规格）
            if (lowBarrier)
            {
                switch (palette)
                {
                    case SegmentObstaclePalette.OceanCoral:
                        modelName = "mushroom_redGroup"; // 珊瑚簇剪影
                        localScale = new Vector3(0.54f, 0.54f, 0.54f);
                        localPos = new Vector3(0f, -0.12f, 0f);
                        tint = new Color(0.96f, 0.55f, 0.6f);
                        break;
                    case SegmentObstaclePalette.SkyCloud:
                        modelName = "stone_2";
                        localScale = new Vector3(0.7f, 0.7f, 0.7f);
                        localPos = new Vector3(0f, -0.05f, 0f);
                        tint = new Color(0.84f, 0.9f, 0.98f);
                        break;
                    case SegmentObstaclePalette.CedarWood:
                        modelName = "log_stackLarge"; // 断木堆
                        localScale = new Vector3(0.5f, 0.5f, 0.5f);
                        localPos = new Vector3(0f, -0.1f, 0f);
                        tint = new Color(0.55f, 0.4f, 0.26f);
                        break;
                    case SegmentObstaclePalette.MistFog:
                        modelName = "stone_1"; // 湿石
                        localScale = new Vector3(0.74f, 0.74f, 0.74f);
                        localPos = new Vector3(0f, -0.06f, 0f);
                        tint = new Color(0.55f, 0.62f, 0.68f);
                        break;
                    case SegmentObstaclePalette.AuroraGlow:
                        modelName = "rock_smallA"; // 磁石碎块
                        localScale = new Vector3(0.8f, 0.8f, 0.8f);
                        localPos = new Vector3(0f, -0.06f, 0f);
                        tint = new Color(0.52f, 0.44f, 0.8f);
                        break;
                    default:
                        modelName = "stone_1";
                        localScale = new Vector3(0.76f, 0.76f, 0.76f);
                        localPos = new Vector3(0f, -0.06f, 0f);
                        tint = new Color(0.72f, 0.84f, 0.94f);
                        break;
                }
            }
            else
            {
                switch (palette)
                {
                    case SegmentObstaclePalette.SkyCloud:
                        modelName = "leaftree";
                        localScale = new Vector3(0.42f, 0.42f, 0.42f);
                        localPos = new Vector3(0f, -0.5f, 0f);
                        tint = new Color(0.78f, 0.9f, 1f);
                        break;
                    case SegmentObstaclePalette.CedarWood:
                        modelName = "log_stackLarge"; // 雪松残木
                        localScale = new Vector3(0.62f, 0.62f, 0.62f);
                        localPos = new Vector3(0f, -0.4f, 0f);
                        tint = new Color(0.52f, 0.38f, 0.24f);
                        break;
                    case SegmentObstaclePalette.MistFog:
                        modelName = "cliff_block_stone"; // 雾堤残垣石
                        localScale = new Vector3(0.6f, 0.6f, 0.6f);
                        localPos = new Vector3(0f, -0.42f, 0f);
                        tint = new Color(0.56f, 0.64f, 0.7f);
                        break;
                    case SegmentObstaclePalette.AuroraGlow:
                        modelName = "cliff_block_rock"; // 磁场棱岩
                        localScale = new Vector3(0.58f, 0.58f, 0.58f);
                        localPos = new Vector3(0f, -0.42f, 0f);
                        tint = new Color(0.58f, 0.48f, 0.95f);
                        break;
                    case SegmentObstaclePalette.OceanCoral:
                        modelName = "mushroom_redGroup"; // 大珊瑚簇
                        localScale = new Vector3(0.82f, 0.82f, 0.82f);
                        localPos = new Vector3(0f, -0.5f, 0f);
                        tint = new Color(0.96f, 0.5f, 0.58f);
                        break;
                    default:
                        modelName = "rock_largeA"; // 冰湖巨岩（仅冰湖使用）
                        localScale = new Vector3(0.82f, 0.82f, 0.82f);
                        localPos = new Vector3(0f, -0.22f, 0f);
                        tint = new Color(0.68f, 0.85f, 0.98f);
                        break;
                }
            }

            var prefab = LoadModelPrefab(modelName);
            if (prefab == null)
            {
                return false;
            }

            var inst = Object.Instantiate(prefab, root, false);
            inst.name = $"ObstacleModel_{modelName}";
            inst.transform.localPosition = lowBarrier ? localPos + new Vector3(0f, -0.04f, 0f) : localPos;
            inst.transform.localRotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
            var scaleBoost = lowBarrier ? OpenObstacleVisualScaleBoost * 1.08f : OpenObstacleVisualScaleBoost;
            inst.transform.localScale = localScale * scaleBoost;
            TintModel(inst, tint);
            AddPaletteAccent(root, palette, lowBarrier);
            if (lowBarrier)
            {
                AddSlideGateGroundStrip(root);
            }

            return true;
        }

        /// <summary>主题读感强化件：每个调色板叠一小组专属发光/氛围装饰（纯表现，不参与判定）。</summary>
        private static void AddPaletteAccent(Transform root, SegmentObstaclePalette palette, bool lowBarrier)
        {
            var topY = lowBarrier ? 0.35f : 1.05f;
            switch (palette)
            {
                case SegmentObstaclePalette.AuroraGlow:
                    // 发光水晶尖
                    var crystal = AddChildPrimitive(root, "Accent Crystal", PrimitiveType.Cube,
                        new Vector3(0.22f, topY, 0f), new Vector3(0.2f, 0.55f, 0.2f),
                        new Color(0.65f, 0.9f, 1f, 0.85f), true);
                    crystal.transform.localRotation = Quaternion.Euler(8f, 30f, 42f);
                    break;
                case SegmentObstaclePalette.OceanCoral:
                    // 上浮气泡
                    AddChildPrimitive(root, "Accent Bubble A", PrimitiveType.Sphere,
                        new Vector3(-0.3f, topY + 0.25f, 0.1f), Vector3.one * 0.14f,
                        new Color(0.7f, 0.95f, 1f, 0.5f), true);
                    AddChildPrimitive(root, "Accent Bubble B", PrimitiveType.Sphere,
                        new Vector3(0.24f, topY + 0.55f, -0.08f), Vector3.one * 0.1f,
                        new Color(0.75f, 0.98f, 1f, 0.42f), true);
                    break;
                case SegmentObstaclePalette.MistFog:
                    // 贴地雾盘
                    AddChildPrimitive(root, "Accent Fog Disc", PrimitiveType.Cylinder,
                        new Vector3(0f, lowBarrier ? -0.16f : -0.5f, 0f), new Vector3(1.9f, 0.05f, 1.9f),
                        new Color(0.82f, 0.88f, 0.92f, 0.28f), true);
                    break;
                case SegmentObstaclePalette.CedarWood:
                    // 顶部积雪
                    AddChildPrimitive(root, "Accent Snow Cap", PrimitiveType.Sphere,
                        new Vector3(0f, topY, 0f), new Vector3(0.72f, 0.16f, 0.6f),
                        new Color(0.93f, 0.96f, 0.99f));
                    break;
                case SegmentObstaclePalette.SkyCloud:
                    // 侧向风痕
                    AddChildPrimitive(root, "Accent Wind Streak", PrimitiveType.Cube,
                        new Vector3(-0.55f, topY * 0.7f, 0f), new Vector3(0.75f, 0.05f, 0.05f),
                        new Color(1f, 1f, 1f, 0.5f), true);
                    break;
                default:
                    // 冰湖：冰晶反光条
                    AddChildPrimitive(root, "Accent Ice Gleam", PrimitiveType.Cube,
                        new Vector3(0.12f, topY * 0.75f, -0.3f), new Vector3(0.08f, 0.62f, 0.08f),
                        new Color(0.8f, 0.98f, 1f, 0.75f), true);
                    break;
            }
        }

        private GameObject LoadModelPrefab(string modelName)
        {
            if (string.IsNullOrEmpty(modelName))
            {
                return null;
            }

            if (modelPrefabCache.TryGetValue(modelName, out var cached))
            {
                return cached;
            }

            var prefab = Resources.Load<GameObject>($"PenguinRun/Models/{modelName}");
            modelPrefabCache[modelName] = prefab;
            return prefab;
        }

        private bool TryAttachObstacleModel(Transform root, string modelName, Vector3 localPos, Vector3 localScale, Color tint)
        {
            var prefab = LoadModelPrefab(modelName);
            if (prefab == null) return false;
            var inst = Object.Instantiate(prefab, root, false);
            inst.name = $"ObstacleModel_{modelName}";
            inst.transform.localPosition = localPos;
            inst.transform.localRotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
            inst.transform.localScale = localScale * OpenObstacleVisualScaleBoost;
            TintModel(inst, tint);
            return true;
        }

        private static readonly MaterialPropertyBlock TintBlock = new();

        /// <summary>模型主题染色：走 MaterialPropertyBlock，不实例化材质（保住合批与 GPU instancing）。</summary>
        private static void TintModel(GameObject root, Color tint)
        {
            if (root == null) return;
            foreach (var renderer in root.GetComponentsInChildren<Renderer>(true))
            {
                if (renderer == null) continue;
                var mat = renderer.sharedMaterial;
                if (mat == null) continue;
                var baseColor = mat.HasProperty("_BaseColor") ? mat.GetColor("_BaseColor")
                    : mat.HasProperty("_Color") ? mat.GetColor("_Color")
                    : Color.white;
                var tinted = Color.Lerp(baseColor, tint, 0.52f);

                renderer.GetPropertyBlock(TintBlock);
                if (mat.HasProperty("_BaseColor")) TintBlock.SetColor("_BaseColor", tinted);
                if (mat.HasProperty("_Color")) TintBlock.SetColor("_Color", tinted);
                renderer.SetPropertyBlock(TintBlock);
            }
        }

        private static void BuildLowBarrier(Transform root, SegmentObstaclePalette palette)
        {
            // 低障碍中心在 dy=0.2f，模型从中心向上构建
            switch (palette)
            {
                case SegmentObstaclePalette.CedarWood:
                    AddChildPrimitive(root, "Wood Crate", PrimitiveType.Cube, new Vector3(0f, 0.05f, 0f), new Vector3(1.48f, 0.32f, 1.2f), new Color(0.45f, 0.32f, 0.2f));
                    AddChildPrimitive(root, "Wood Rim", PrimitiveType.Cube, new Vector3(0f, 0.1f, -0.02f), new Vector3(1.58f, 0.1f, 1.28f), new Color(0.62f, 0.48f, 0.34f));
                    break;
                case SegmentObstaclePalette.MistFog:
                    // 雾堤低障碍：湿石板 + 贴地雾带（与雪松木箱区分）
                    AddChildPrimitive(root, "Wet Slab", PrimitiveType.Cube, new Vector3(0f, 0.03f, 0f), new Vector3(1.5f, 0.3f, 1.18f), new Color(0.42f, 0.5f, 0.55f));
                    AddChildPrimitive(root, "Slab Moss", PrimitiveType.Cube, new Vector3(0f, 0.15f, 0f), new Vector3(1.32f, 0.07f, 1.02f), new Color(0.4f, 0.55f, 0.42f));
                    AddChildPrimitive(root, "Fog Skirt", PrimitiveType.Cylinder, new Vector3(0f, -0.1f, 0f), new Vector3(1.9f, 0.05f, 1.6f), new Color(0.82f, 0.88f, 0.92f, 0.3f), true);
                    break;
                case SegmentObstaclePalette.OceanCoral:
                    // 海洋主题低障碍：贝壳障碍
                    AddChildPrimitive(root, "Seashell Base", PrimitiveType.Cube, new Vector3(0f, 0.02f, 0f), new Vector3(1.4f, 0.24f, 1.15f), new Color(0.95f, 0.82f, 0.72f));
                    AddChildPrimitive(root, "Seashell Top", PrimitiveType.Cube, new Vector3(0f, 0.14f, 0f), new Vector3(1.2f, 0.18f, 0.95f), new Color(0.98f, 0.88f, 0.78f));
                    AddChildPrimitive(root, "Shell Glow", PrimitiveType.Cube, new Vector3(0f, 0.08f, 0.35f), new Vector3(0.6f, 0.06f, 0.08f), new Color(0.2f, 0.85f, 0.9f, 0.6f), true);
                    break;
                case SegmentObstaclePalette.SkyCloud:
                    // 天空主题低障碍：云朵块
                    AddChildPrimitive(root, "Cloud Puff", PrimitiveType.Sphere, new Vector3(-0.3f, 0.08f, 0f), new Vector3(0.55f, 0.25f, 0.5f), new Color(0.98f, 0.98f, 1f, 0.9f), true);
                    AddChildPrimitive(root, "Cloud Main", PrimitiveType.Sphere, new Vector3(0.2f, 0.05f, 0f), new Vector3(0.85f, 0.3f, 0.65f), new Color(0.95f, 0.95f, 1f, 0.85f), true);
                    AddChildPrimitive(root, "Cloud Rim", PrimitiveType.Cube, new Vector3(0f, -0.02f, 0.2f), new Vector3(0.5f, 0.06f, 0.06f), new Color(0.55f, 0.75f, 1f, 0.7f), true);
                    break;
                default:
                    // 默认主题：更精致的滑铲门设计（条纹横梁 + 警示灯）
                    AddChildPrimitive(root, "Gate Beam", PrimitiveType.Cube, new Vector3(0f, 0.03f, 0f), new Vector3(1.45f, 0.28f, 1.25f), new Color(0.88f, 0.38f, 0.12f));
                    AddChildPrimitive(root, "Gate Stripe", PrimitiveType.Cube, new Vector3(0f, 0.04f, 0f), new Vector3(1.4f, 0.08f, 0.18f), new Color(0.18f, 0.1f, 0.06f));
                    AddChildPrimitive(root, "Gate Glow", PrimitiveType.Cube, new Vector3(0f, 0.06f, -0.02f), new Vector3(1.58f, 0.06f, 1.36f), new Color(1f, 0.52f, 0.18f, 0.62f), true);
                    AddChildPrimitive(root, "Gate Rim Hazard", PrimitiveType.Cube, new Vector3(0f, 0.04f, 0.62f), new Vector3(1.55f, 0.04f, 0.12f), new Color(0.95f, 0.2f, 0.15f));
                    // 两侧支柱
                    AddChildPrimitive(root, "Gate Pillar L", PrimitiveType.Cube, new Vector3(-0.74f, 0.03f, 0f), new Vector3(0.16f, 0.46f, 0.16f), new Color(0.5f, 0.32f, 0.18f));
                    AddChildPrimitive(root, "Gate Pillar R", PrimitiveType.Cube, new Vector3(0.74f, 0.03f, 0f), new Vector3(0.16f, 0.46f, 0.16f), new Color(0.5f, 0.32f, 0.18f));
                    AddChildPrimitive(root, "Warning Lamp L", PrimitiveType.Sphere, new Vector3(-0.74f, 0.18f, 0f), Vector3.one * 0.13f, new Color(1f, 0.95f, 0.4f, 0.85f), true);
                    AddChildPrimitive(root, "Warning Lamp R", PrimitiveType.Sphere, new Vector3(0.74f, 0.18f, 0f), Vector3.one * 0.13f, new Color(1f, 0.95f, 0.4f, 0.85f), true);
                    break;
            }

            AddSlideGateGroundStrip(root);
        }

        private static void AddSlideGateGroundStrip(Transform root)
        {
            AddChildPrimitive(root, "Hazard Strip", PrimitiveType.Cube,
                new Vector3(0f, -0.14f, 0.02f), new Vector3(1.62f, 0.05f, 1.34f),
                new Color(1f, 0.48f, 0.12f, 0.42f), true);
        }
    }
}
