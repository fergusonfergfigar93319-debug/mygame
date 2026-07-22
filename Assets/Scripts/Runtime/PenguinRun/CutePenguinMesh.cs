using UnityEngine;

namespace PenguinRun
{
    /// <summary>
    /// 可爱卡通企鹅的程序化 3D 网格生成器。
    /// 使用球形变形、顶点混合和平滑法线来创建圆润可爱的外观，
    /// 替代原来由 40+ 个 Primitive 几何体拼接的角色。
    ///
    /// 设计原则：
    /// - 大头（~40%身高）→ 婴儿比例 = 可爱
    /// - 大眼睛（~25%头宽）→ 萌系特征
    /// - 梨形体态（底宽顶窄）→ 企鹅辨识度
    /// - 圆润所有边缘 → 柔软无害感
    /// </summary>
    public static class CutePenguinMesh
    {
        // ── 比例参数 ──────────────────────────────────────────
        private const float TotalHeight = 1.8f;       // 从脚底到帽顶的总高度
        private const float BodyHeight = 1.1f;         // 身体高度
        private const float BodyWidth = 0.65f;         // 身体最大宽度
        private const float BodyDepth = 0.55f;         // 身体深度
        private const float HeadRadius = 0.42f;        // 头部半径
        private const float EyeRadius = 0.095f;        // 眼睛（白色部分）半径
        private const float IrisRadius = 0.058f;       // 虹膜半径
        private const float PupilRadius = 0.028f;      // 瞳孔半径
        private const float BeakWidth = 0.16f;         // 喙宽度
        private const float WingLength = 0.65f;        // 翅膀长度
        private const float FootLength = 0.30f;        // 脚长度
        private const float HatHeight = 0.22f;         // 帽子高度

        // ── 颜色 ──────────────────────────────────────────────
        private static readonly Color BodyBlue = new(0.04f, 0.09f, 0.18f);
        private static readonly Color BellyWhite = new(0.92f, 0.96f, 0.99f);
        private static readonly Color BeakOrange = new(1f, 0.62f, 0.22f);
        private static readonly Color FootOrange = new(0.95f, 0.64f, 0.25f);
        private static readonly Color EyeWhite = Color.white;
        private static readonly Color IrisBlue = new(0.08f, 0.56f, 0.95f);
        private static readonly Color PupilBlack = new(0.02f, 0.03f, 0.08f);
        private static readonly Color HatBlue = new(0.04f, 0.34f, 0.78f);
        private static readonly Color ScarfRed = new(0.85f, 0.15f, 0.2f);
        private static readonly Color CheekPink = new(1f, 0.55f, 0.6f);

        /// <summary>在 parent 下创建完整的企鹅 GameObject 层级。</summary>
        public static GameObject Create(Transform parent, out PenguinCharacterParts parts)
        {
            var root = new GameObject("Cute Penguin");
            root.transform.SetParent(parent, false);

            parts = new PenguinCharacterParts();

            // 1. 身体（梨形椭球）
            parts.Body = CreateBodyMesh(root.transform);

            // 2. 头部（正球体 + 面部白斑）
            parts.Head = CreateHeadMesh(root.transform);

            // 3. 眼睛（大球体 + 虹膜 + 瞳孔 + 高光）
            CreateEyeMeshes(parts.Head.transform, out parts.LeftEye, out parts.RightEye,
                out parts.LeftPupil, out parts.RightPupil);

            // 4. 喙
            parts.Beak = CreateBeakMesh(parts.Head.transform);

            // 5. 脸颊红晕
            parts.LeftCheek = CreateCheekMesh(parts.Head.transform, true);
            parts.RightCheek = CreateCheekMesh(parts.Head.transform, false);

            // 6. 翅膀
            parts.LeftWing = CreateWingMesh(root.transform, true);
            parts.RightWing = CreateWingMesh(root.transform, false);

            // 7. 脚
            parts.LeftFoot = CreateFootMesh(root.transform, true);
            parts.RightFoot = CreateFootMesh(root.transform, false);

            // 8. 帽子
            parts.Hat = CreateHatMesh(parts.Head.transform);

            // 9. 围巾
            parts.Scarf = CreateScarfMesh(root.transform);

            // 10. 腹部白斑（贴在身体前方）
            parts.Belly = CreateBellyMesh(root.transform);

            return root;
        }

        // ═══════════════════════════════════════════════════════
        //  身体网格：纵向略扁的椭球体，底部比顶部略宽（梨形）
        // ═══════════════════════════════════════════════════════
        private static GameObject CreateBodyMesh(Transform parent)
        {
            var go = CreateMeshObject("Body", parent, BodyBlue,
                new Vector3(0f, BodyHeight * 0.55f, 0f));

            var mf = go.GetComponent<MeshFilter>();
            mf.sharedMesh = BuildRoundedShape(24, 16, (v) =>
            {
                // 梨形变形：底部加宽 15%，顶部收窄 5%
                var pearFactor = 1f + (v.y - 0.5f) * 0.3f;
                var rx = BodyWidth * 0.5f * pearFactor;
                var ry = BodyHeight * 0.5f;
                var rz = BodyDepth * 0.5f * pearFactor;
                return new Vector3(v.x * rx, v.y * ry, v.z * rz);
            });

            return go;
        }

        // ═══════════════════════════════════════════════════════
        //  头部网格：正球 + 面部白斑
        // ═══════════════════════════════════════════════════════
        private static GameObject CreateHeadMesh(Transform parent)
        {
            var go = CreateMeshObject("Head", parent, BodyBlue,
                new Vector3(0f, BodyHeight * 0.55f + HeadRadius * 0.85f, 0f));

            var mf = go.GetComponent<MeshFilter>();
            mf.sharedMesh = BuildRoundedShape(20, 14, (v) =>
            {
                // 轻微椭球形（上下略扁）
                return new Vector3(v.x * HeadRadius, v.y * HeadRadius * 0.92f, v.z * HeadRadius * 0.88f);
            });

            return go;
        }

        // ═══════════════════════════════════════════════════════
        //  腹部白斑
        // ═══════════════════════════════════════════════════════
        private static GameObject CreateBellyMesh(Transform parent)
        {
            var go = CreateMeshObject("Belly", parent, BellyWhite,
                new Vector3(0f, BodyHeight * 0.52f, -BodyDepth * 0.28f));

            var mf = go.GetComponent<MeshFilter>();
            mf.sharedMesh = BuildRoundedShape(16, 10, (v) =>
            {
                var rx = BodyWidth * 0.38f;
                var ry = BodyHeight * 0.42f;
                var rz = BodyDepth * 0.06f;
                return new Vector3(v.x * rx, v.y * ry, v.z * rz);
            });

            return go;
        }

        // ═══════════════════════════════════════════════════════
        //  眼睛网格（白色眼球 + 嵌入虹膜和瞳孔）
        // ═══════════════════════════════════════════════════════
        private static void CreateEyeMeshes(Transform headParent,
            out GameObject leftEye, out GameObject rightEye,
            out GameObject leftPupil, out GameObject rightPupil)
        {
            // 父节点即头部：所有面部件相对头部原点定位（修复此前叠加 head.localPosition 的双重偏移）
            var eyeY = HeadRadius * 0.08f;
            var eyeZ = -HeadRadius * 0.62f;
            var eyeX = HeadRadius * 0.32f;

            // 左眼白
            leftEye = CreateMeshObject("LeftEye", headParent, EyeWhite,
                new Vector3(-eyeX, eyeY, eyeZ));
            leftEye.GetComponent<MeshFilter>().sharedMesh = BuildSphere(EyeRadius, 16, 12);

            // 右眼白
            rightEye = CreateMeshObject("RightEye", headParent, EyeWhite,
                new Vector3(eyeX, eyeY, eyeZ));
            rightEye.GetComponent<MeshFilter>().sharedMesh = BuildSphere(EyeRadius, 16, 12);

            // 左虹膜
            var irisGo = CreateMeshObject("LeftIris", leftEye.transform, IrisBlue,
                new Vector3(0f, 0f, -EyeRadius * 0.52f));
            irisGo.GetComponent<MeshFilter>().sharedMesh = BuildSphere(IrisRadius, 12, 8);

            // 右虹膜
            var irisGoR = CreateMeshObject("RightIris", rightEye.transform, IrisBlue,
                new Vector3(0f, 0f, -EyeRadius * 0.52f));
            irisGoR.GetComponent<MeshFilter>().sharedMesh = BuildSphere(IrisRadius, 12, 8);

            // 左瞳孔
            leftPupil = CreateMeshObject("LeftPupil", irisGo.transform, PupilBlack,
                new Vector3(0f, 0f, -IrisRadius * 0.55f));
            leftPupil.GetComponent<MeshFilter>().sharedMesh = BuildSphere(PupilRadius, 8, 6);

            // 右瞳孔
            rightPupil = CreateMeshObject("RightPupil", irisGoR.transform, PupilBlack,
                new Vector3(0f, 0f, -IrisRadius * 0.55f));
            rightPupil.GetComponent<MeshFilter>().sharedMesh = BuildSphere(PupilRadius, 8, 6);

            // 高光点
            var sparkL = CreateMeshObject("LeftSpark", leftEye.transform, Color.white,
                new Vector3(-EyeRadius * 0.25f, EyeRadius * 0.35f, -EyeRadius * 0.75f));
            sparkL.GetComponent<MeshFilter>().sharedMesh = BuildSphere(EyeRadius * 0.16f, 6, 4);

            var sparkR = CreateMeshObject("RightSpark", rightEye.transform, Color.white,
                new Vector3(-EyeRadius * 0.25f, EyeRadius * 0.35f, -EyeRadius * 0.75f));
            sparkR.GetComponent<MeshFilter>().sharedMesh = BuildSphere(EyeRadius * 0.16f, 6, 4);
        }

        // ═══════════════════════════════════════════════════════
        //  喙：圆润的三角锥
        // ═══════════════════════════════════════════════════════
        private static GameObject CreateBeakMesh(Transform headParent)
        {
            var go = CreateMeshObject("Beak", headParent, BeakOrange,
                new Vector3(0f, -HeadRadius * 0.18f, -HeadRadius * 0.85f));

            var mf = go.GetComponent<MeshFilter>();
            mf.sharedMesh = BuildBeakShape();

            return go;
        }

        private static Mesh BuildBeakShape()
        {
            // 圆润三角锥形喙
            const int segs = 8;
            var verts = new Vector3[segs * 2 + 1];
            var tris = new int[segs * 9];
            var norms = new Vector3[verts.Length];

            var tip = new Vector3(0f, 0f, -0.22f); // 喙尖
            verts[0] = tip;

            for (var i = 0; i < segs; i++)
            {
                var a = i * Mathf.PI * 2f / segs;
                var rx = BeakWidth * 0.5f;
                var ry = BeakWidth * 0.38f;
                verts[1 + i] = new Vector3(Mathf.Cos(a) * rx, Mathf.Sin(a) * ry, 0.04f);
                verts[1 + segs + i] = new Vector3(Mathf.Cos(a) * rx * 1.3f, Mathf.Sin(a) * ry * 1.3f, 0.06f);
            }

            var ti = 0;
            for (var i = 0; i < segs; i++)
            {
                var next = (i + 1) % segs;
                tris[ti++] = 0; tris[ti++] = 1 + next; tris[ti++] = 1 + i;

                var a = 1 + i;
                var b = 1 + next;
                var c = 1 + segs + i;
                var d = 1 + segs + next;
                tris[ti++] = a; tris[ti++] = b; tris[ti++] = d;
                tris[ti++] = a; tris[ti++] = d; tris[ti++] = c;
            }

            var mesh = new Mesh { name = "Beak", vertices = verts, triangles = tris };
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        // ═══════════════════════════════════════════════════════
        //  脸颊红晕
        // ═══════════════════════════════════════════════════════
        private static GameObject CreateCheekMesh(Transform headParent, bool left)
        {
            var x = left ? -HeadRadius * 0.58f : HeadRadius * 0.58f;
            var y = -HeadRadius * 0.22f;
            var z = -HeadRadius * 0.55f;

            var go = CreateMeshObject(left ? "LeftCheek" : "RightCheek", headParent, CheekPink,
                new Vector3(x, y, z));
            go.GetComponent<MeshFilter>().sharedMesh = BuildEllipsoid(
                new Vector3(0.1f, 0.05f, 0.03f), 10, 6);
            return go;
        }

        // ═══════════════════════════════════════════════════════
        //  翅膀：扁平的鳍状
        // ═══════════════════════════════════════════════════════
        private static GameObject CreateWingMesh(Transform parent, bool left)
        {
            var x = left ? -BodyWidth * 0.62f : BodyWidth * 0.62f;
            var y = BodyHeight * 0.58f;
            var go = CreateMeshObject(left ? "LeftWing" : "RightWing", parent, BodyBlue,
                new Vector3(x, y, 0f));

            var mf = go.GetComponent<MeshFilter>();
            mf.sharedMesh = BuildWingShape(left);
            return go;
        }

        private static Mesh BuildWingShape(bool left)
        {
            // 扁椭球形状的鳍
            var mesh = BuildEllipsoid(
                new Vector3(0.2f, WingLength * 0.5f, 0.1f), 10, 8);
            var verts = mesh.vertices;
            var dir = left ? -1f : 1f;

            // 下垂效果
            for (var i = 0; i < verts.Length; i++)
            {
                var p = verts[i];
                p.y -= Mathf.Abs(p.x) * 0.5f;
                p.x += dir * 0.12f;
                verts[i] = p;
            }

            mesh.vertices = verts;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        // ═══════════════════════════════════════════════════════
        //  脚：扁平圆润
        // ═══════════════════════════════════════════════════════
        private static GameObject CreateFootMesh(Transform parent, bool left)
        {
            var x = left ? -BodyWidth * 0.2f : BodyWidth * 0.2f;
            var go = CreateMeshObject(left ? "LeftFoot" : "RightFoot", parent, FootOrange,
                new Vector3(x, 0.04f, -BodyDepth * 0.05f));

            var mf = go.GetComponent<MeshFilter>();
            mf.sharedMesh = BuildFootShape();
            return go;
        }

        private static Mesh BuildFootShape()
        {
            // 扁平椭球 + 前伸脚趾
            var mesh = BuildEllipsoid(
                new Vector3(FootLength * 0.5f, 0.06f, FootLength * 0.7f), 12, 6);
            var verts = mesh.vertices;

            // 压扁 + 前伸
            for (var i = 0; i < verts.Length; i++)
            {
                verts[i].y *= 0.35f;
                if (verts[i].z < 0f)
                    verts[i].z *= 1.3f;
            }

            mesh.vertices = verts;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        // ═══════════════════════════════════════════════════════
        //  帽子
        // ═══════════════════════════════════════════════════════
        private static GameObject CreateHatMesh(Transform headParent)
        {
            var go = CreateMeshObject("Hat", headParent, HatBlue,
                new Vector3(0f, HeadRadius * 0.65f, -HeadRadius * 0.05f));

            var mf = go.GetComponent<MeshFilter>();
            mf.sharedMesh = BuildHatShape();
            return go;
        }

        private static Mesh BuildHatShape()
        {
            // 半球形帽身 + 小球顶
            var verts = new System.Collections.Generic.List<Vector3>();
            var tris = new System.Collections.Generic.List<int>();

            // 帽身（下半球体）
            BuildHalfSphere(verts, tris, 0, HeadRadius * 0.95f, HatHeight, 16, 6, true);

            // 帽檐（扁平环）
            var brimBase = verts.Count;
            const int brimSegs = 16;
            for (var i = 0; i < brimSegs; i++)
            {
                var a = i * Mathf.PI * 2f / brimSegs;
                var r = HeadRadius * 1.08f;
                verts.Add(new Vector3(Mathf.Cos(a) * r, -HatHeight * 0.5f, Mathf.Sin(a) * r));
                verts.Add(new Vector3(Mathf.Cos(a) * r * 0.82f, -HatHeight * 0.35f, Mathf.Sin(a) * r * 0.82f));
            }
            for (var i = 0; i < brimSegs; i++)
            {
                var next = (i + 1) % brimSegs;
                var a = brimBase + i * 2;
                var b = brimBase + next * 2;
                tris.Add(a); tris.Add(a + 1); tris.Add(b);
                tris.Add(a + 1); tris.Add(b + 1); tris.Add(b);
            }

            var mesh = new Mesh { name = "Hat", vertices = verts.ToArray(), triangles = tris.ToArray() };
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        // ═══════════════════════════════════════════════════════
        //  围巾
        // ═══════════════════════════════════════════════════════
        private static GameObject CreateScarfMesh(Transform parent)
        {
            // 围巾在颈部（身体与头的衔接处），而不是腹部中段
            var go = CreateMeshObject("Scarf", parent, ScarfRed,
                new Vector3(0f, BodyHeight * 0.82f, -BodyDepth * 0.08f));

            var mf = go.GetComponent<MeshFilter>();
            mf.sharedMesh = BuildScarfShape();
            return go;
        }

        private static Mesh BuildScarfShape()
        {
            // 环形围巾（筒状环）
            const int segs = 16;
            var verts = new Vector3[segs * 2];
            var tris = new int[segs * 6];

            var ringR = BodyWidth * 0.52f;
            for (var i = 0; i < segs; i++)
            {
                var a = i * Mathf.PI * 2f / segs;
                var x = Mathf.Cos(a) * ringR;
                var z = Mathf.Sin(a) * ringR * 0.65f;
                verts[i * 2] = new Vector3(x, 0.08f, z);
                verts[i * 2 + 1] = new Vector3(x, -0.08f, z);
            }

            var ti = 0;
            for (var i = 0; i < segs; i++)
            {
                var next = (i + 1) % segs;
                var a = i * 2; var b = next * 2;
                tris[ti++] = a; tris[ti++] = b; tris[ti++] = a + 1;
                tris[ti++] = a + 1; tris[ti++] = b; tris[ti++] = b + 1;
            }

            var mesh = new Mesh { name = "Scarf", vertices = verts, triangles = tris };
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        // ═══════════════════════════════════════════════════════
        //  通用网格构建工具
        // ═══════════════════════════════════════════════════════

        /// <summary>UV 球体（极点收敛、无接缝）。</summary>
        private static Mesh BuildSphere(float radius, int lonSegs, int latSegs)
        {
            var vc = (latSegs + 1) * (lonSegs + 1);
            var verts = new Vector3[vc];
            var tris = new int[latSegs * lonSegs * 6];
            var v = 0;
            for (var lat = 0; lat <= latSegs; lat++)
            {
                var theta = Mathf.PI * lat / latSegs;
                var y = Mathf.Cos(theta) * radius;
                var r = Mathf.Sin(theta) * radius;
                for (var lon = 0; lon <= lonSegs; lon++)
                {
                    var phi = 2f * Mathf.PI * lon / lonSegs;
                    verts[v++] = new Vector3(Mathf.Cos(phi) * r, y, Mathf.Sin(phi) * r);
                }
            }
            var t = 0;
            for (var lat = 0; lat < latSegs; lat++)
            {
                for (var lon = 0; lon < lonSegs; lon++)
                {
                    // 外向绕向（此前内外翻转导致从背面可透视看到前侧部件）
                    var cur = lat * (lonSegs + 1) + lon;
                    var nxt = cur + lonSegs + 1;
                    tris[t++] = cur; tris[t++] = cur + 1; tris[t++] = nxt;
                    tris[t++] = cur + 1; tris[t++] = nxt + 1; tris[t++] = nxt;
                }
            }
            var mesh = new Mesh { name = $"Sphere_{radius}", vertices = verts, triangles = tris };
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        /// <summary>椭球体（X/Y/Z 不同半径）。</summary>
        private static Mesh BuildEllipsoid(Vector3 radii, int lonSegs, int latSegs)
        {
            return BuildRoundedShape(lonSegs, latSegs, v =>
                new Vector3(v.x * radii.x, v.y * radii.y, v.z * radii.z));
        }

        /// <summary>UV 球体 + 自定义变形函数。</summary>
        private static Mesh BuildRoundedShape(int lonSegs, int latSegs, System.Func<Vector3, Vector3> deform)
        {
            var vc = (latSegs + 1) * (lonSegs + 1);
            var verts = new Vector3[vc];
            var tris = new int[latSegs * lonSegs * 6];
            var v = 0;
            for (var lat = 0; lat <= latSegs; lat++)
            {
                var theta = Mathf.PI * lat / latSegs;
                var y = Mathf.Cos(theta);
                var r = Mathf.Sin(theta);
                for (var lon = 0; lon <= lonSegs; lon++)
                {
                    var phi = 2f * Mathf.PI * lon / lonSegs;
                    var unit = new Vector3(Mathf.Cos(phi) * r, y, Mathf.Sin(phi) * r);
                    verts[v++] = deform(unit);
                }
            }
            var t = 0;
            for (var lat = 0; lat < latSegs; lat++)
            {
                for (var lon = 0; lon < lonSegs; lon++)
                {
                    // 外向绕向（与 BuildSphere 同步修正）
                    var cur = lat * (lonSegs + 1) + lon;
                    var nxt = cur + lonSegs + 1;
                    tris[t++] = cur; tris[t++] = cur + 1; tris[t++] = nxt;
                    tris[t++] = cur + 1; tris[t++] = nxt + 1; tris[t++] = nxt;
                }
            }
            var mesh = new Mesh { name = "RoundedShape", vertices = verts, triangles = tris };
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        /// <summary>半球（帽顶等）。</summary>
        private static void BuildHalfSphere(System.Collections.Generic.List<Vector3> verts,
            System.Collections.Generic.List<int> tris, int baseIdx,
            float radius, float height, int lonSegs, int latSegs, bool top)
        {
            var halfLat = latSegs / 2;
            for (var lat = 0; lat <= halfLat; lat++)
            {
                var theta = Mathf.PI * 0.5f * lat / halfLat;
                var y = top ? Mathf.Sin(theta) * height : -Mathf.Sin(theta) * height;
                var r = Mathf.Cos(theta) * radius;
                for (var lon = 0; lon <= lonSegs; lon++)
                {
                    var phi = 2f * Mathf.PI * lon / lonSegs;
                    verts.Add(new Vector3(Mathf.Cos(phi) * r, y, Mathf.Sin(phi) * r));
                }
            }
            for (var lat = 0; lat < halfLat; lat++)
            {
                for (var lon = 0; lon < lonSegs; lon++)
                {
                    // 外向绕向（与 BuildRoundedShape 同步修正）
                    var cur = baseIdx + lat * (lonSegs + 1) + lon;
                    var nxt = cur + lonSegs + 1;
                    tris.Add(cur); tris.Add(cur + 1); tris.Add(nxt);
                    tris.Add(cur + 1); tris.Add(nxt + 1); tris.Add(nxt);
                }
            }
        }

        // ═══════════════════════════════════════════════════════
        //  辅助函数
        // ═══════════════════════════════════════════════════════
        private static GameObject CreateMeshObject(string name, Transform parent, Color color, Vector3 localPos)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;

            var mf = go.AddComponent<MeshFilter>();
            var mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = CreateMaterial(color);
            return go;
        }

        private static Material CreateMaterial(Color color)
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit")
                      ?? Shader.Find("Standard");
            var mat = new Material(shader);
            if (mat.HasProperty("_BaseColor"))
                mat.SetColor("_BaseColor", color);
            if (mat.HasProperty("_Color"))
                mat.SetColor("_Color", color);
            // 卡通风格：降低光滑度
            if (mat.HasProperty("_Smoothness"))
                mat.SetFloat("_Smoothness", 0.15f);
            return mat;
        }
    }

    /// <summary>企鹅角色的子部件 Transform 引用（供动画系统使用）。</summary>
    public sealed class PenguinCharacterParts
    {
        public GameObject Body;
        public GameObject Head;
        public GameObject Belly;
        public GameObject LeftEye;
        public GameObject RightEye;
        public GameObject LeftPupil;
        public GameObject RightPupil;
        public GameObject LeftCheek;
        public GameObject RightCheek;
        public GameObject Beak;
        public GameObject LeftWing;
        public GameObject RightWing;
        public GameObject LeftFoot;
        public GameObject RightFoot;
        public GameObject Hat;
        public GameObject Scarf;
    }
}
