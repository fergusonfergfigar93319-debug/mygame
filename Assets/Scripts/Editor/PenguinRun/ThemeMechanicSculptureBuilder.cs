#if UNITY_EDITOR
using UnityEngine;

namespace PenguinRun.Editor
{
    internal static class ThemeMechanicSculptureBuilder
    {
        public static void Build(Transform root, string path, SculpturePalette p, bool low)
        {
            switch (path)
            {
                case "ice/cracked_plate": BuildCrackedPlate(root, p, low); break;
                case "ice/crystal_rebound_gate": BuildCrystalGate(root, p, low); break;
                case "ice/floe_bridge": BuildFloeBridge(root, p, low); break;
                case "cedar/swing_log": BuildSwingLog(root, p, low); break;
                case "cedar/root_gallery": BuildRootGallery(root, p, low); break;
                case "cedar/ruin_escape": BuildRuinEscape(root, p, low); break;
                case "aurora/polarity_gate": BuildPolarityGate(root, p, low); break;
                case "aurora/prism_beam": BuildPrismBeam(root, p, low); break;
                case "aurora/magnetic_rebuild": BuildMagneticRebuild(root, p, low); break;
                case "mist/decoy_shadow": BuildDecoy(root, p, low); break;
                case "mist/rotating_beacon": BuildBeacon(root, p, low); break;
                case "mist/floodgate_chase": BuildFloodgate(root, p, low); break;
                case "ocean/coral_lift": BuildCoral(root, p, low); break;
                case "ocean/bubble_vault": BuildBubbleVault(root, p, low); break;
                case "ocean/wave_tunnel": BuildWaveTunnel(root, p, low); break;
                case "sky/wind_gate": BuildWindGate(root, p, low); break;
                case "sky/cloud_platform": BuildCloudPlatform(root, p, low); break;
                case "sky/island_glide_chain": BuildIslandChain(root, p, low); break;
                default: throw new System.ArgumentOutOfRangeException(nameof(path), path,
                    "No theme mechanic sculpture recipe exists for this apparatus path.");
            }
        }

        private static void BuildCrackedPlate(Transform r, SculpturePalette p, bool low)
        {
            var count = low ? 3 : 6;
            for (var i = 0; i < count; i++)
            {
                var a = i * Mathf.PI * 2f / count;
                P(r, $"IceSlab_{i}", PrimitiveType.Cube,
                    new Vector3(Mathf.Cos(a) * .45f, .05f, Mathf.Sin(a) * .45f),
                    new Vector3(.72f, .10f, .58f), i % 2 == 0 ? p.Primary : p.Secondary,
                    new Vector3(0f, -i * 17f, (i % 3 - 1) * 3f));
            }
            var cracks = low ? 2 : 5;
            for (var i = 0; i < cracks; i++)
                B(r, $"DarkCrack_{i}", Vector3.up * .12f,
                    new Vector3(Mathf.Sin(i * 2.1f) * .85f, .13f, Mathf.Cos(i * 1.7f) * .75f),
                    .018f, p.Dark);
            if (!low) for (var i = 0; i < 3; i++)
                P(r, $"SnowChip_{i}", PrimitiveType.Sphere, new Vector3(-.6f + i * .55f, .18f, .72f - i * .31f),
                    Vector3.one * .14f, p.Snow);
        }

        private static void BuildCrystalGate(Transform r, SculpturePalette p, bool low)
        {
            Gate(r, p, low, true);
            P(r, "GlowCore", PrimitiveType.Sphere, new Vector3(0f, 1.25f, 0f), Vector3.one * .28f, p.Glow);
            if (!low) for (var i = 0; i < 4; i++)
                B(r, $"CrystalBrace_{i}", new Vector3(i < 2 ? -1.05f : 1.05f, .2f, 0f),
                    new Vector3(i < 2 ? -.55f : .55f, 1.65f, (i % 2 == 0 ? -.25f : .25f)), .045f, p.Accent);
        }

        private static void BuildFloeBridge(Transform r, SculpturePalette p, bool low)
        {
            var count = low ? 4 : 7;
            for (var i = 0; i < count; i++)
                P(r, $"FloePlate_{i}", PrimitiveType.Cube,
                    new Vector3((i % 2 == 0 ? -.12f : .12f), .05f + (i % 3) * .025f, -1.8f + i * .62f),
                    new Vector3(1.45f, .11f, .52f), i % 2 == 0 ? p.Primary : p.Secondary,
                    new Vector3(0f, (i % 3 - 1) * 7f, 0f));
            if (!low) for (var i = 0; i < 4; i++)
                B(r, $"BridgeCrack_{i}", new Vector3(-.5f, .13f, -.9f + i * .58f),
                    new Vector3(.52f, .13f, -.65f + i * .58f), .015f, p.Dark);
            P(r, "EdgeCrystalL", PrimitiveType.Cylinder, new Vector3(-.8f, .35f, -1.65f), new Vector3(.12f,.35f,.12f), p.Glow, new Vector3(0f,0f,-12f));
            P(r, "EdgeCrystalR", PrimitiveType.Cylinder, new Vector3(.8f, .35f, 1.65f), new Vector3(.12f,.35f,.12f), p.Glow, new Vector3(0f,0f,12f));
        }

        private static void BuildSwingLog(Transform r, SculpturePalette p, bool low)
        {
            P(r, "SuspendedLog", PrimitiveType.Cylinder, new Vector3(0f, .72f, 0f), new Vector3(.42f, 1.35f, .42f), p.Primary, new Vector3(0f,0f,90f));
            B(r, "RopeL", new Vector3(-.82f,.78f,0f), new Vector3(-1.15f,2.25f,0f), .035f, p.Dark);
            B(r, "RopeR", new Vector3(.82f,.78f,0f), new Vector3(1.15f,2.25f,0f), .035f, p.Dark);
            P(r, "CollarL", PrimitiveType.Cylinder, new Vector3(-.82f,.72f,0f), new Vector3(.47f,.08f,.47f), p.Accent, new Vector3(0f,0f,90f));
            P(r, "CollarR", PrimitiveType.Cylinder, new Vector3(.82f,.72f,0f), new Vector3(.47f,.08f,.47f), p.Accent, new Vector3(0f,0f,90f));
            if (!low) for (var i=0;i<6;i++) P(r,$"BarkRib_{i}",PrimitiveType.Cube,new Vector3(-.65f+i*.26f,.72f,-.39f),new Vector3(.035f,.36f,.035f),p.Secondary);
        }

        private static void BuildRootGallery(Transform r, SculpturePalette p, bool low)
        {
            var count=low?2:4;
            for(var i=0;i<count;i++)
            {
                var z=-1.2f+i*.8f; B(r,$"RootL_{i}",new Vector3(-1f,0f,z),new Vector3(-.35f,1.55f,z),.11f,p.Primary);
                B(r,$"RootR_{i}",new Vector3(1f,0f,z),new Vector3(.35f,1.55f,z),.11f,p.Primary);
                B(r,$"RootCrown_{i}",new Vector3(-.35f,1.55f,z),new Vector3(.35f,1.55f,z),.11f,p.Secondary);
            }
            if(!low) for(var i=0;i<3;i++) B(r,$"Brace_{i}",new Vector3(-.8f,.5f,-1f+i),new Vector3(.8f,.75f,-1f+i),.045f,p.Accent);
        }

        private static void BuildRuinEscape(Transform r, SculpturePalette p, bool low)
        {
            var count=low?2:3;
            for(var i=0;i<count;i++) P(r,$"LeaningTower_{i}",PrimitiveType.Cube,new Vector3(-1.2f+i*1.2f,.85f,(i%2)*.45f),new Vector3(.48f,1.7f,.5f),i%2==0?p.Primary:p.Secondary,new Vector3(0f,0f,-12f+i*12f));
            var beams=low?3:6; for(var i=0;i<beams;i++) B(r,$"FallingBeam_{i}",new Vector3(-1.4f+i*.5f,1.7f,-.6f),new Vector3(-.8f+i*.5f,.25f,.7f),.055f,p.Dark);
            if(!low) for(var i=0;i<4;i++) P(r,$"DustMarker_{i}",PrimitiveType.Sphere,new Vector3(-.9f+i*.6f,.12f,.75f),Vector3.one*.18f,p.Snow);
        }

        private static void BuildPolarityGate(Transform r, SculpturePalette p, bool low)
        {
            P(r,"BaseBlue",PrimitiveType.Cylinder,new Vector3(-.82f,.10f,0f),new Vector3(.30f,.10f,.30f),p.Dark);
            P(r,"BaseRed",PrimitiveType.Cylinder,new Vector3(.82f,.10f,0f),new Vector3(.30f,.10f,.30f),p.Dark);
            P(r,"PoleBlue",PrimitiveType.Cylinder,new Vector3(-.82f,1f,0f),new Vector3(.12f,1f,.12f),p.Primary);
            P(r,"PoleRed",PrimitiveType.Cylinder,new Vector3(.82f,1f,0f),new Vector3(.12f,1f,.12f),p.Accent);
            var rings=low?1:3; for(var i=0;i<rings;i++) P(r,$"CopperRing_{i}",PrimitiveType.Cylinder,new Vector3(0f,.65f+i*.45f,0f),new Vector3(.55f,.025f,.55f),p.Accent,new Vector3(90f,0f,0f));
            P(r,"CompassCore",PrimitiveType.Sphere,new Vector3(0f,1.15f,0f),Vector3.one*.25f,p.Glow);
            P(r,"EmitterBlue",PrimitiveType.Sphere,new Vector3(-.82f,2f,0f),Vector3.one*.19f,p.Glow);
            P(r,"EmitterRed",PrimitiveType.Sphere,new Vector3(.82f,2f,0f),Vector3.one*.19f,p.Glow);
            B(r,"FluxBridgeTop",new Vector3(-.72f,1.88f,0f),new Vector3(.72f,1.88f,0f),.025f,p.Glow);
            if(!low){B(r,"FluxCableL",new Vector3(-.82f,.25f,.15f),new Vector3(-.18f,1.05f,.15f),.025f,p.Secondary);B(r,"FluxCableR",new Vector3(.82f,.25f,-.15f),new Vector3(.18f,1.05f,-.15f),.025f,p.Secondary);}
        }

        private static void BuildPrismBeam(Transform r, SculpturePalette p, bool low)
        {
            Gate(r,p,low,false); B(r,"EmissiveBeam",new Vector3(-.9f,1.05f,0f),new Vector3(.9f,1.05f,0f),.055f,p.Glow);
            var count=low?1:3; for(var i=0;i<count;i++) P(r,$"Prism_{i}",PrimitiveType.Cylinder,new Vector3(-.45f+i*.45f,1.42f,.1f),new Vector3(.12f,.25f,.12f),p.Accent,new Vector3(0f,0f,18f+i*12f));
        }

        private static void BuildMagneticRebuild(Transform r, SculpturePalette p, bool low)
        {
            var blocks=low?3:5; for(var i=0;i<blocks;i++){var a=i*Mathf.PI*2f/blocks;P(r,$"FloatingBlock_{i}",PrimitiveType.Cube,new Vector3(Mathf.Cos(a)*.85f,.65f+i*.22f,Mathf.Sin(a)*.55f),new Vector3(.42f,.32f,.38f),i%2==0?p.Primary:p.Secondary,new Vector3(i*11f,i*23f,0f));}
            var arcs=low?2:4; for(var i=0;i<arcs;i++) B(r,$"CopperArc_{i}",new Vector3(-.85f+i*.55f,.35f,-.4f),new Vector3(-.55f+i*.55f,1.65f,.4f),.035f,p.Accent);
            P(r,"MagneticCore",PrimitiveType.Sphere,new Vector3(0f,1.05f,0f),Vector3.one*.22f,p.Glow);
        }

        private static void BuildDecoy(Transform r, SculpturePalette p, bool low)
        {
            P(r,"DecoyPlinth",PrimitiveType.Cylinder,new Vector3(0f,.10f,0f),new Vector3(.58f,.10f,.58f),p.Primary);
            var fins=low?2:3; for(var i=0;i<fins;i++) P(r,$"ShadowFin_{i}",PrimitiveType.Cube,new Vector3(-.45f+i*.45f,.75f,i*.12f),new Vector3(.12f,1.4f,.65f),p.Dark,new Vector3(0f,-25f+i*25f,0f));
            P(r,"FalseLamp",PrimitiveType.Sphere,new Vector3(0f,1.35f,-.18f),Vector3.one*.18f,p.Accent);
            P(r,"LensHalo",PrimitiveType.Cylinder,new Vector3(0f,1.35f,-.18f),new Vector3(.31f,.035f,.31f),p.Glow,new Vector3(90f,0f,0f));
            B(r,"FinCrown",new Vector3(-.62f,1.55f,.05f),new Vector3(.62f,1.55f,.05f),.035f,p.Secondary);
            if(!low){for(var i=0;i<4;i++) B(r,$"MistAntenna_{i}",new Vector3((i<2?-.5f:.5f),.25f,(i%2==0?-.3f:.3f)),new Vector3((i<2?-.78f:.78f),1.75f,(i%2==0?-.45f:.45f)),.018f,p.Glow);}
        }

        private static void BuildBeacon(Transform r, SculpturePalette p, bool low)
        {
            P(r,"Foundation",PrimitiveType.Cylinder,new Vector3(0f,.07f,0f),new Vector3(.82f,.07f,.82f),p.Primary);
            P(r,"StoneBase",PrimitiveType.Cylinder,new Vector3(0f,.18f,0f),new Vector3(.65f,.18f,.65f),p.Dark);
            P(r,"BeaconMast",PrimitiveType.Cylinder,new Vector3(0f,1f,0f),new Vector3(.12f,.85f,.12f),p.Primary);
            var fins=low?2:4; for(var i=0;i<fins;i++) P(r,$"LensFin_{i}",PrimitiveType.Cube,new Vector3(0f,1.65f,0f),new Vector3(.62f,.08f,.16f),p.Accent,new Vector3(0f,i*90f,0f));
            P(r,"BrightCore",PrimitiveType.Sphere,new Vector3(0f,1.65f,0f),Vector3.one*.24f,p.Glow);
            P(r,"MastCollar",PrimitiveType.Cylinder,new Vector3(0f,1.32f,0f),new Vector3(.23f,.07f,.23f),p.Secondary);
            P(r,"LensCap",PrimitiveType.Cylinder,new Vector3(0f,1.92f,0f),new Vector3(.30f,.08f,.30f),p.Dark);
            if(!low){for(var i=0;i<4;i++){var a=i*Mathf.PI*.5f;B(r,$"LensCage_{i}",new Vector3(Mathf.Cos(a)*.27f,1.38f,Mathf.Sin(a)*.27f),new Vector3(Mathf.Cos(a)*.27f,1.91f,Mathf.Sin(a)*.27f),.018f,p.Glow);}}
        }

        private static void BuildFloodgate(Transform r, SculpturePalette p, bool low)
        {
            Gate(r,p,low,false); P(r,"LightCarriage",PrimitiveType.Cube,new Vector3(0f,1.35f,0f),new Vector3(.65f,.22f,.35f),p.Glow);
            var beams=low?3:6; for(var i=0;i<beams;i++) B(r,$"WaterRope_{i}",new Vector3(-1.1f+i*.44f,.1f,-.45f),new Vector3(-.85f+i*.34f,1.65f,.45f),.03f,i%2==0?p.Primary:p.Accent);
        }

        private static void BuildCoral(Transform r, SculpturePalette p, bool low)
        {
            var branches=low?4:7; for(var i=0;i<branches;i++){var x=-.8f+i*(1.6f/(branches-1));B(r,$"CoralBranch_{i}",new Vector3(x*.55f,0f,0f),new Vector3(x,.65f+Mathf.Abs(x)*.55f,(i%2==0?-.25f:.25f)),.07f,i%2==0?p.Accent:p.Primary);}
            var bubbles=low?1:3; for(var i=0;i<bubbles;i++) P(r,$"Bubble_{i}",PrimitiveType.Sphere,new Vector3(-.5f+i*.5f,1.15f+i*.22f,.15f),Vector3.one*(.16f+i*.03f),p.Glow);
        }

        private static void BuildBubbleVault(Transform r, SculpturePalette p, bool low)
        {
            var bubbles=low?3:5; for(var i=0;i<bubbles;i++){var a=i*Mathf.PI/(bubbles-1);P(r,$"VaultBubble_{i}",PrimitiveType.Sphere,new Vector3(Mathf.Cos(a)*1.05f,.25f+Mathf.Sin(a)*1.35f,0f),Vector3.one*.28f,p.Glow);}
            B(r,"CoralAnchorL",new Vector3(-1.15f,0f,0f),new Vector3(-.85f,.55f,0f),.09f,p.Accent); B(r,"CoralAnchorR",new Vector3(1.15f,0f,0f),new Vector3(.85f,.55f,0f),.09f,p.Accent);
        }

        private static void BuildWaveTunnel(Transform r, SculpturePalette p, bool low)
        {
            var ribs=low?4:8; for(var i=0;i<ribs;i++){var z=-1.5f+i*(3f/(ribs-1));B(r,$"WaveRibL_{i}",new Vector3(-1f,0f,z),new Vector3(-.25f,1.55f,z),.075f,p.Primary);B(r,$"WaveRibR_{i}",new Vector3(1f,0f,z),new Vector3(.25f,1.55f,z),.075f,p.Secondary);}
            var foam=low?2:4; for(var i=0;i<foam;i++) P(r,$"Foam_{i}",PrimitiveType.Sphere,new Vector3(-.7f+i*.45f,1.45f,-1.2f+i*.7f),Vector3.one*.18f,p.Snow);
        }

        private static void BuildWindGate(Transform r, SculpturePalette p, bool low)
        {
            P(r,"WindFoot",PrimitiveType.Cylinder,new Vector3(0f,.09f,0f),new Vector3(.46f,.09f,.46f),p.Primary);
            P(r,"WindMast",PrimitiveType.Cylinder,new Vector3(0f,1f,0f),new Vector3(.1f,1f,.1f),p.Dark);
            P(r,"VaneL",PrimitiveType.Cube,new Vector3(-.58f,1.35f,0f),new Vector3(.75f,.38f,.08f),p.Primary,new Vector3(0f,0f,12f));
            P(r,"VaneR",PrimitiveType.Cube,new Vector3(.58f,1.0f,0f),new Vector3(.75f,.38f,.08f),p.Secondary,new Vector3(0f,0f,-12f));
            if(!low) for(var i=0;i<4;i++) B(r,$"Rope_{i}",new Vector3(0f,.25f,0f),new Vector3((i<2?-1f:1f),1f+(i%2)*.55f,(i%2==0?-.2f:.2f)),.025f,p.Accent);
            P(r,"CompassHub",PrimitiveType.Sphere,new Vector3(0f,1.2f,0f),Vector3.one*.18f,p.Glow);
            P(r,"MastCollarLow",PrimitiveType.Cylinder,new Vector3(0f,.48f,0f),new Vector3(.19f,.06f,.19f),p.Accent);
            P(r,"MastCollarHigh",PrimitiveType.Cylinder,new Vector3(0f,1.72f,0f),new Vector3(.19f,.06f,.19f),p.Accent);
            B(r,"DirectionNeedle",new Vector3(-.78f,1.92f,0f),new Vector3(.82f,1.92f,0f),.035f,p.Glow);
            P(r,"NeedleTip",PrimitiveType.Cube,new Vector3(.92f,1.92f,0f),new Vector3(.22f,.18f,.10f),p.Glow,new Vector3(0f,0f,45f));
        }

        private static void BuildCloudPlatform(Transform r, SculpturePalette p, bool low)
        {
            var clouds=low?3:5; for(var i=0;i<clouds;i++) P(r,$"Cloud_{i}",PrimitiveType.Sphere,new Vector3(-.8f+i*.4f,.2f+(i%2)*.12f,(i%3-1)*.22f),new Vector3(.65f,.32f,.55f),p.Snow);
            var planks=low?2:3; for(var i=0;i<planks;i++) P(r,$"Plank_{i}",PrimitiveType.Cube,new Vector3(0f,.5f,-.42f+i*.42f),new Vector3(1.45f,.09f,.32f),p.Primary,new Vector3(0f,(i-1)*4f,0f));
            if(!low) for(var i=0;i<4;i++) B(r,$"Brace_{i}",new Vector3((i<2?-1f:1f),.42f,-.5f),new Vector3((i<2?-.55f:.55f),-.15f,.5f),.025f,p.Accent);
        }

        private static void BuildIslandChain(Transform r, SculpturePalette p, bool low)
        {
            var rocks=low?2:4; for(var i=0;i<rocks;i++) P(r,$"FloatingRock_{i}",PrimitiveType.Cube,new Vector3(-1.35f+i*.9f,.25f+i*.18f,-.65f+i*.45f),new Vector3(.7f,.32f,.62f),i%2==0?p.Primary:p.Secondary,new Vector3(i*5f,i*18f,0f));
            var clouds=low?3:6; for(var i=0;i<clouds;i++) P(r,$"ChainCloud_{i}",PrimitiveType.Sphere,new Vector3(-1.45f+i*.58f,-.05f+(i%2)*.18f,.45f),new Vector3(.45f,.22f,.4f),p.Snow);
            var guides=low?2:5; for(var i=0;i<guides;i++) B(r,$"SailGuide_{i}",new Vector3(-1.35f+i*.68f,.45f,-.5f),new Vector3(-.95f+i*.48f,1.35f,.45f),.03f,i%2==0?p.Accent:p.Glow);
        }

        private static void Gate(Transform r, SculpturePalette p, bool low, bool crystal)
        {
            P(r,"PostL",crystal?PrimitiveType.Cylinder:PrimitiveType.Cube,new Vector3(-1f,1f,0f),new Vector3(.18f,1f,.18f),p.Primary);
            P(r,"PostR",crystal?PrimitiveType.Cylinder:PrimitiveType.Cube,new Vector3(1f,1f,0f),new Vector3(.18f,1f,.18f),p.Secondary);
            B(r,"Crown",new Vector3(-1f,1.9f,0f),new Vector3(1f,1.9f,0f),.09f,p.Accent);
            if(!low){B(r,"FootBraceL",new Vector3(-1.25f,0f,0f),new Vector3(-1f,.8f,0f),.055f,p.Dark);B(r,"FootBraceR",new Vector3(1.25f,0f,0f),new Vector3(1f,.8f,0f),.055f,p.Dark);}
        }

        private static GameObject P(Transform parent,string name,PrimitiveType type,Vector3 pos,Vector3 scale,Material mat,Vector3 euler=default) => RunnerSculptureBuilder.Part(parent,name,type,pos,scale,mat,euler);
        private static GameObject B(Transform parent,string name,Vector3 start,Vector3 end,float radius,Material mat) => RunnerSculptureBuilder.Beam(parent,name,start,end,radius,mat);
    }
}
#endif
