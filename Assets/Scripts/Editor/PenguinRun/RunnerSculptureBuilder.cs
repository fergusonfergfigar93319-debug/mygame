#if UNITY_EDITOR
using UnityEngine;

namespace PenguinRun.Editor
{
    internal readonly struct SculpturePalette
    {
        public readonly Material Primary;
        public readonly Material Secondary;
        public readonly Material Accent;
        public readonly Material Dark;
        public readonly Material Snow;
        public readonly Material Glow;

        public SculpturePalette(Material primary, Material secondary, Material accent,
            Material dark, Material snow, Material glow)
        {
            Primary = primary;
            Secondary = secondary;
            Accent = accent;
            Dark = dark;
            Snow = snow;
            Glow = glow;
        }
    }

    /// <summary>Code-authored story-sculpture models used when a DCC pipeline is unavailable.</summary>
    internal static class RunnerSculptureBuilder
    {
        public static void BuildObstacle(Transform root, RunnerMapTheme theme, ObstacleKind kind,
            SculpturePalette palette, bool lowDetail)
        {
            switch (kind)
            {
                case ObstacleKind.SlideGate:
                    BuildSlideGate(root, theme, palette, lowDetail);
                    break;
                case ObstacleKind.WideWall:
                    BuildWideWall(root, theme, palette, lowDetail);
                    break;
                default:
                    BuildStandardHigh(root, theme, palette, lowDetail);
                    break;
            }
        }

        public static void BuildPickup(Transform root, PickupVisualId id, SculpturePalette palette,
            bool lowDetail)
        {
            switch (id)
            {
                case PickupVisualId.FishSnack:
                    Part(root, "FishBody", PrimitiveType.Sphere, new Vector3(0f, 0.02f, 0f),
                        new Vector3(.38f, .23f, .56f), palette.Accent);
                    Part(root, "FishTail", PrimitiveType.Cube, new Vector3(0f, 0f, .48f),
                        new Vector3(.34f, .25f, .08f), palette.Primary, new Vector3(0f, 0f, 45f));
                    Part(root, "Binding", PrimitiveType.Cylinder, new Vector3(0f, .02f, .15f),
                        new Vector3(.26f, .05f, .26f), palette.Dark, new Vector3(90f, 0f, 0f));
                    if (!lowDetail) AddFishScales(root, palette);
                    break;
                case PickupVisualId.Dash:
                    Part(root, "SledHull", PrimitiveType.Capsule, Vector3.zero,
                        new Vector3(.40f, .36f, .72f), palette.Primary, new Vector3(90f, 0f, 0f));
                    for (var i = -1; i <= 1; i++)
                        Part(root, $"Chevron_{i+2}", PrimitiveType.Cube, new Vector3(0f, .05f, i*.23f),
                            new Vector3(.48f-i*.04f, .08f, .11f), palette.Accent, new Vector3(0f, 0f, 45f));
                    Beam(root, "RunnerL", new Vector3(-.31f, -.23f, -.55f), new Vector3(-.31f, -.23f, .55f), .035f, palette.Dark);
                    Beam(root, "RunnerR", new Vector3(.31f, -.23f, -.55f), new Vector3(.31f, -.23f, .55f), .035f, palette.Dark);
                    break;
                case PickupVisualId.Magnet:
                    Part(root, "MagnetBridge", PrimitiveType.Cube, new Vector3(0f, .34f, 0f),
                        new Vector3(.62f, .18f, .20f), palette.Dark);
                    Part(root, "PoleL", PrimitiveType.Capsule, new Vector3(-.24f, 0f, 0f),
                        new Vector3(.18f, .62f, .18f), palette.Accent);
                    Part(root, "PoleR", PrimitiveType.Capsule, new Vector3(.24f, 0f, 0f),
                        new Vector3(.18f, .62f, .18f), palette.Primary);
                    Part(root, "Compass", PrimitiveType.Cylinder, new Vector3(0f, .05f, -.16f),
                        new Vector3(.18f, .05f, .18f), palette.Secondary, new Vector3(90f, 0f, 0f));
                    break;
                case PickupVisualId.Shield:
                    Part(root, "Buckler", PrimitiveType.Cylinder, Vector3.zero,
                        new Vector3(.58f, .10f, .58f), palette.Primary, new Vector3(90f, 0f, 0f));
                    Part(root, "ShieldBoss", PrimitiveType.Sphere, new Vector3(0f, 0f, -.10f),
                        Vector3.one*.22f, palette.Accent);
                    for (var i = 0; i < (lowDetail ? 4 : 8); i++)
                    {
                        var a = i*Mathf.PI*2f/(lowDetail ? 4 : 8);
                        Part(root, $"Rivet_{i}", PrimitiveType.Sphere,
                            new Vector3(Mathf.Cos(a)*.43f, Mathf.Sin(a)*.43f, -.12f), Vector3.one*.055f, palette.Accent);
                    }
                    Beam(root, "WingL", new Vector3(-.06f, .05f, -.17f), new Vector3(-.34f, .20f, -.17f), .035f, palette.Snow);
                    Beam(root, "WingR", new Vector3(.06f, .05f, -.17f), new Vector3(.34f, .20f, -.17f), .035f, palette.Snow);
                    break;
                case PickupVisualId.ScoreStar:
                    for (var i = 0; i < 5; i++)
                    {
                        var a = i*72f;
                        Part(root, $"StarRay_{i}", PrimitiveType.Cube,
                            Quaternion.Euler(0f, 0f, a)*new Vector3(0f, .28f, 0f),
                            new Vector3(.18f, .52f, .16f), palette.Accent, new Vector3(0f, 0f, a+45f));
                    }
                    Part(root, "CompassRose", PrimitiveType.Cylinder, new Vector3(0f, 0f, -.12f),
                        new Vector3(.24f, .06f, .24f), palette.Secondary, new Vector3(90f, 0f, 0f));
                    break;
                default:
                    Beam(root, "FeatherShaft", new Vector3(0f, -.52f, 0f), new Vector3(0f, .56f, 0f), .035f, palette.Accent);
                    for (var i = 0; i < (lowDetail ? 3 : 6); i++)
                    {
                        var y = -.32f+i*.15f;
                        Beam(root, $"VaneL_{i}", new Vector3(-.03f, y, 0f), new Vector3(-.36f+i*.025f, y+.18f, 0f), .055f, palette.Snow);
                        Beam(root, $"VaneR_{i}", new Vector3(.03f, y+.04f, 0f), new Vector3(.34f-i*.02f, y+.21f, 0f), .055f, palette.Primary);
                    }
                    Part(root, "BindingRing", PrimitiveType.Cylinder, new Vector3(0f, -.45f, 0f),
                        new Vector3(.11f, .05f, .11f), palette.Dark, new Vector3(90f, 0f, 0f));
                    break;
            }
        }

        public static void BuildEnvironment(Transform root, RunnerMapTheme theme, EnvironmentPropId id,
            SculpturePalette palette, bool lowDetail)
        {
            switch (id)
            {
                case EnvironmentPropId.Landmark:
                    BuildLandmark(root, theme, palette, lowDetail);
                    break;
                case EnvironmentPropId.Signpost:
                    Beam(root, "Post", new Vector3(0f, 0f, 0f), new Vector3(0f, 1.75f, 0f), .10f, palette.Dark);
                    Beam(root, "ArrowL", new Vector3(0f, 1.45f, 0f), new Vector3(-.72f, 1.58f, 0f), .13f, palette.Primary);
                    Beam(root, "ArrowR", new Vector3(0f, 1.08f, 0f), new Vector3(.62f, 1.20f, 0f), .12f, palette.Secondary);
                    if (!lowDetail) AddThemeMotif(root, theme, palette, 1.2f, 1.8f, false);
                    break;
                case EnvironmentPropId.Light:
                    Beam(root, "Handle", new Vector3(-.22f, 1.10f, 0f), new Vector3(.22f, 1.10f, 0f), .045f, palette.Dark);
                    Beam(root, "HandleL", new Vector3(-.22f, 1.10f, 0f), new Vector3(-.30f, .82f, 0f), .045f, palette.Dark);
                    Beam(root, "HandleR", new Vector3(.22f, 1.10f, 0f), new Vector3(.30f, .82f, 0f), .045f, palette.Dark);
                    Part(root, "LanternCage", PrimitiveType.Cylinder, new Vector3(0f, .45f, 0f),
                        new Vector3(.42f, .60f, .42f), palette.Primary);
                    Part(root, "WarmCore", PrimitiveType.Sphere, new Vector3(0f, .45f, 0f), Vector3.one*.42f, palette.Glow);
                    Part(root, "Cap", PrimitiveType.Cylinder, new Vector3(0f, .83f, 0f), new Vector3(.50f, .10f, .50f), palette.Dark);
                    if (!lowDetail) AddCageBars(root, palette.Dark);
                    break;
                default:
                    Part(root, "SupplyCrate", PrimitiveType.Cube, new Vector3(-.28f, .25f, 0f),
                        new Vector3(.62f, .50f, .55f), palette.Primary);
                    Part(root, "BrokenAnchor", PrimitiveType.Cylinder, new Vector3(.38f, .24f, .08f),
                        new Vector3(.16f, .46f, .16f), palette.Dark, new Vector3(0f, 0f, 62f));
                    Part(root, "SnowClump", PrimitiveType.Sphere, new Vector3(-.26f, .50f, 0f),
                        new Vector3(.58f, .16f, .50f), palette.Snow);
                    if (!lowDetail) AddThemeMotif(root, theme, palette, 1.25f, .75f, false);
                    break;
            }
        }

        private static void BuildStandardHigh(Transform root, RunnerMapTheme theme, SculpturePalette p, bool low)
        {
            switch (theme)
            {
                case RunnerMapTheme.IceLakeEcho:
                    for (var i = 0; i < (low ? 3 : 6); i++)
                        Part(root, $"IceShard_{i}", PrimitiveType.Cube,
                            new Vector3((i-(low?1:2.5f))*.16f, .38f+i%2*.16f, (i%3-1)*.08f),
                            new Vector3(.28f, .78f+i%2*.24f, .30f), i%2==0?p.Primary:p.Secondary,
                            new Vector3(0f, i*19f, 45f+i%2*8f));
                    break;
                case RunnerMapTheme.CedarRuins:
                    for (var i = -1; i <= 1; i++)
                        Beam(root, $"CedarLog_{i}", new Vector3(-.48f, .25f+i*.17f, i*.12f),
                            new Vector3(.48f, .33f+i*.17f, i*.12f), .19f, p.Primary);
                    break;
                case RunnerMapTheme.AuroraField:
                    Part(root, "MagnetStone", PrimitiveType.Sphere, new Vector3(0f, .55f, 0f),
                        new Vector3(.72f, .95f, .60f), p.Dark);
                    for (var i = 0; i < (low?2:4); i++)
                        Part(root, $"CopperRing_{i}", PrimitiveType.Cylinder, new Vector3(0f, .25f+i*.20f, 0f),
                            new Vector3(.78f-i*.06f, .045f, .78f-i*.06f), p.Accent);
                    break;
                case RunnerMapTheme.MistDike:
                    for (var y = 0; y < (low?2:3); y++)
                        for (var x = -1; x <= 1; x++)
                            Part(root, $"WetStone_{y}_{x}", PrimitiveType.Sphere,
                                new Vector3(x*.31f+(y%2)*.12f, .18f+y*.28f, 0f),
                                new Vector3(.40f, .28f, .46f), y%2==0?p.Primary:p.Secondary);
                    break;
                case RunnerMapTheme.OceanReef:
                    for (var i = -2; i <= 2; i++)
                    {
                        Beam(root, $"Coral_{i}", new Vector3(i*.16f, .05f, 0f),
                            new Vector3(i*.22f, .72f-Mathf.Abs(i)*.07f, (i%2)*.16f), .12f, i%2==0?p.Accent:p.Primary);
                        if (!low) Part(root, $"Shell_{i}", PrimitiveType.Sphere, new Vector3(i*.20f, .26f, -.18f),
                            new Vector3(.18f,.11f,.06f), p.Snow);
                    }
                    break;
                default:
                    Part(root, "WreckCore", PrimitiveType.Cube, new Vector3(0f, .40f, 0f),
                        new Vector3(.92f, .58f, .54f), p.Primary, new Vector3(8f, 0f, -9f));
                    Beam(root, "RopeL", new Vector3(-.42f, .15f, -.24f), new Vector3(.38f, .70f, .24f), .035f, p.Dark);
                    Beam(root, "RopeR", new Vector3(.42f, .15f, -.24f), new Vector3(-.38f, .70f, .24f), .035f, p.Dark);
                    Part(root, "CloudCrystal", PrimitiveType.Cube, new Vector3(0f, .82f, 0f), Vector3.one*.28f, p.Glow, new Vector3(0f,0f,45f));
                    break;
            }
            AddThemeMotif(root, theme, p, 1.15f, 1.1f, low);
        }

        private static void BuildSlideGate(Transform root, RunnerMapTheme theme, SculpturePalette p, bool low)
        {
            Beam(root, "SupportL", new Vector3(-.50f, -.05f, 0f), new Vector3(-.50f, .46f, 0f), .11f, p.Primary);
            Beam(root, "SupportR", new Vector3(.50f, -.05f, 0f), new Vector3(.50f, .46f, 0f), .11f, p.Primary);
            Beam(root, "Crossbar", new Vector3(-.58f, .45f, 0f), new Vector3(.58f, .45f, 0f), .14f, p.Secondary);
            if (!low)
            {
                Beam(root, "BraceL", new Vector3(-.52f, .36f, -.20f), new Vector3(-.30f, .54f, .20f), .045f, p.Dark);
                Beam(root, "BraceR", new Vector3(.52f, .36f, -.20f), new Vector3(.30f, .54f, .20f), .045f, p.Dark);
            }
            AddThemeMotif(root, theme, p, 1.25f, .75f, low);
        }

        private static void BuildWideWall(Transform root, RunnerMapTheme theme, SculpturePalette p, bool low)
        {
            var count = low ? 5 : 9;
            for (var i = 0; i < count; i++)
            {
                var x = Mathf.Lerp(-1.12f, 1.12f, count == 1 ? .5f : i/(float)(count-1));
                Part(root, $"WallSection_{i}", theme is RunnerMapTheme.MistDike or RunnerMapTheme.OceanReef
                        ? PrimitiveType.Sphere : PrimitiveType.Cube,
                    new Vector3(x, .48f+(i%3)*.035f, 0f), new Vector3(.34f, .92f, .58f),
                    i%2==0?p.Primary:p.Secondary, new Vector3(0f, (i%3-1)*5f, (i%3-1)*3f));
            }
            if (!low)
            {
                Beam(root, "DiagonalBraceL", new Vector3(-1.15f, .10f, -.33f), new Vector3(-.15f, .92f, -.33f), .07f, p.Dark);
                Beam(root, "DiagonalBraceR", new Vector3(1.15f, .10f, -.33f), new Vector3(.15f, .92f, -.33f), .07f, p.Dark);
            }
            AddThemeMotif(root, theme, p, 2.5f, 1.15f, low);
        }

        private static void BuildLandmark(Transform root, RunnerMapTheme theme, SculpturePalette p, bool low)
        {
            Part(root, "Base", PrimitiveType.Cylinder, new Vector3(0f, .22f, 0f), new Vector3(.85f,.22f,.85f), p.Dark);
            Beam(root, "Spine", new Vector3(0f,.22f,0f), new Vector3(0f,3.10f,0f), .18f, p.Primary);
            for (var i = 0; i < (low?2:5); i++)
                Part(root, $"Crown_{i}", PrimitiveType.Cube,
                    new Vector3((i-(low?.5f:2f))*.24f, 2.72f+Mathf.Abs(i-(low?.5f:2f))*.12f, 0f),
                    new Vector3(.28f,.74f,.28f), i%2==0?p.Secondary:p.Accent,
                    new Vector3(0f,i*17f,45f));
            AddThemeMotif(root, theme, p, 1.8f, 3.2f, low);
        }

        private static void AddThemeMotif(Transform root, RunnerMapTheme theme, SculpturePalette p,
            float width, float height, bool low)
        {
            switch (theme)
            {
                case RunnerMapTheme.IceLakeEcho:
                    for (var i = 0; i < (low?2:4); i++)
                        Part(root, $"IceDetail_{i}", PrimitiveType.Cube,
                            new Vector3(Mathf.Lerp(-width*.35f,width*.35f,i/(float)(low?1:3)), height*.62f+(i%2)*.12f, -.22f),
                            new Vector3(.15f,.34f,.15f), p.Snow, new Vector3(0f,i*21f,45f));
                    break;
                case RunnerMapTheme.CedarRuins:
                    Beam(root, "SnowRidge", new Vector3(-width*.46f,height*.82f,-.24f), new Vector3(width*.46f,height*.82f,-.24f), .08f, p.Snow);
                    if (!low) for (var i=0;i<4;i++) Part(root,$"RopeKnot_{i}",PrimitiveType.Cylinder,
                        new Vector3(Mathf.Lerp(-width*.35f,width*.35f,i/3f),height*.48f,-.28f),new Vector3(.13f,.04f,.13f),p.Accent,new Vector3(90f,0f,0f));
                    break;
                case RunnerMapTheme.AuroraField:
                    Part(root,"AuroraCore",PrimitiveType.Sphere,new Vector3(0f,height*.72f,-.30f),Vector3.one*(low?.16f:.23f),p.Glow);
                    Beam(root,"CopperFinL",new Vector3(-width*.35f,height*.35f,-.26f),new Vector3(-width*.18f,height*.78f,-.26f),.045f,p.Accent);
                    Beam(root,"CopperFinR",new Vector3(width*.35f,height*.35f,-.26f),new Vector3(width*.18f,height*.78f,-.26f),.045f,p.Accent);
                    break;
                case RunnerMapTheme.MistDike:
                    Beam(root,"RustedPipe",new Vector3(-width*.42f,height*.18f,-.30f),new Vector3(width*.42f,height*.18f,-.30f),.055f,p.Accent);
                    if(!low) Part(root,"Valve",PrimitiveType.Cylinder,new Vector3(width*.22f,height*.35f,-.34f),new Vector3(.22f,.035f,.22f),p.Dark,new Vector3(90f,0f,0f));
                    break;
                case RunnerMapTheme.OceanReef:
                    for(var i=0;i<(low?2:4);i++) Beam(root,$"CoralBranch_{i}",
                        new Vector3(Mathf.Lerp(-width*.38f,width*.38f,i/(float)(low?1:3)),height*.16f,-.25f),
                        new Vector3(Mathf.Lerp(-width*.30f,width*.30f,i/(float)(low?1:3))+(i%2==0?-.12f:.12f),height*.72f,-.25f),.055f,p.Accent);
                    break;
                default:
                    Part(root,"CanvasPatch",PrimitiveType.Cube,new Vector3(0f,height*.58f,-.31f),new Vector3(width*.68f,height*.26f,.035f),p.Snow,new Vector3(0f,0f,-4f));
                    Beam(root,"RiggingL",new Vector3(-width*.42f,height*.15f,-.35f),new Vector3(width*.30f,height*.88f,-.35f),.025f,p.Dark);
                    Beam(root,"RiggingR",new Vector3(width*.42f,height*.15f,-.35f),new Vector3(-width*.30f,height*.88f,-.35f),.025f,p.Dark);
                    break;
            }
        }

        private static void AddFishScales(Transform root, SculpturePalette p)
        {
            for (var row=0;row<3;row++) for(var col=-2;col<=2;col++)
                Part(root,$"Scale_{row}_{col}",PrimitiveType.Sphere,new Vector3(col*.07f,.10f-row*.08f,-.48f),
                    new Vector3(.055f,.04f,.018f),row%2==0?p.Snow:p.Secondary);
        }

        private static void AddCageBars(Transform root, Material material)
        {
            for(var i=0;i<6;i++)
            {
                var a=i*Mathf.PI*2f/6f;
                Beam(root,$"CageBar_{i}",new Vector3(Mathf.Cos(a)*.31f,.12f,Mathf.Sin(a)*.31f),
                    new Vector3(Mathf.Cos(a)*.31f,.82f,Mathf.Sin(a)*.31f),.025f,material);
            }
        }

        private static GameObject Part(Transform parent, string name, PrimitiveType type, Vector3 position,
            Vector3 scale, Material material, Vector3 euler = default)
        {
            var go=GameObject.CreatePrimitive(type); go.name=name; go.transform.SetParent(parent,false);
            go.transform.localPosition=position; go.transform.localEulerAngles=euler; go.transform.localScale=scale;
            var collider=go.GetComponent<Collider>(); if(collider!=null) Object.DestroyImmediate(collider);
            var renderer=go.GetComponent<Renderer>(); if(renderer!=null) renderer.sharedMaterial=material;
            return go;
        }

        private static GameObject Beam(Transform parent, string name, Vector3 start, Vector3 end,
            float radius, Material material)
        {
            var delta=end-start; var go=Part(parent,name,PrimitiveType.Cylinder,(start+end)*.5f,
                new Vector3(radius,delta.magnitude*.5f,radius),material);
            go.transform.localRotation=Quaternion.FromToRotation(Vector3.up,delta.normalized);
            return go;
        }
    }
}
#endif
