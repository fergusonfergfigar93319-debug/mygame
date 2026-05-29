using UnityEngine;

namespace PenguinRun
{
    /// <summary>
    /// 程序化障碍物/道具 Mesh 生成器。
    /// 替代 SegmentSpawner 中的 Primitive 拼接，每种障碍有独特且匹配碰撞体的外形。
    ///
    /// 设计原则：
    /// - 视觉尺寸严格匹配 ObstacleColliderSpec 的 HalfDx/HalfDz
    /// - 每种障碍有辨识度高的轮廓（方便玩家快速判断规避方式）
    /// - 移动端友好的面数（30-200 三角面）
    /// </summary>
    public static class ObstacleMeshBuilder
    {
        // ── 障碍物 ──────────────────────────────────────────────

        /// <summary>滑铲门：矮横梁 + 两侧支柱。低矮障碍，滑铲可通过。</summary>
        public static Mesh BuildSlideGate()
        {
            // 横梁: 宽 1.0m × 高 0.25m × 深 0.7m → HalfDx≈0.50, HalfDz≈0.35
            var beam = BuildRoundedBox(new Vector3(1.0f, 0.25f, 0.7f), 0.06f, 8, 4, 4);

            // 两侧短柱
            var pillars = new CombineInstance[3];
            pillars[0].mesh = beam;
            pillars[0].transform = Matrix4x4.identity;

            var pillarL = BuildRoundedBox(new Vector3(0.14f, 0.36f, 0.16f), 0.03f, 6, 4, 4);
            pillars[1].mesh = pillarL;
            pillars[1].transform = Matrix4x4.TRS(new Vector3(-0.48f, 0f, 0f), Quaternion.identity, Vector3.one);

            var pillarR = BuildRoundedBox(new Vector3(0.14f, 0.36f, 0.16f), 0.03f, 6, 4, 4);
            pillars[2].mesh = pillarR;
            pillars[2].transform = Matrix4x4.TRS(new Vector3(0.48f, 0f, 0f), Quaternion.identity, Vector3.one);

            return Combine("SlideGate", pillars);
        }

        /// <summary>跳跃障碍：六角冰柱。中等高度，需跳跃通过。</summary>
        public static Mesh BuildCrystalPillar()
        {
            return BuildHexPillar(0.28f, 0.9f, 0.06f);
        }

        /// <summary>宽墙：矮宽屏障。须换道通过。</summary>
        public static Mesh BuildWideWall()
        {
            return BuildRoundedBox(new Vector3(2.4f, 0.85f, 0.65f), 0.08f, 12, 6, 6);
        }

        /// <summary>宽滑铲门：更宽的横梁。滑铲或走第三道可通过。</summary>
        public static Mesh BuildWideSlideGate()
        {
            var beam = BuildRoundedBox(new Vector3(2.2f, 0.25f, 0.7f), 0.06f, 10, 4, 4);
            return Combine("WideSlideGate", new[] {
                new CombineInstance { mesh = beam, transform = Matrix4x4.identity }
            });
        }

        /// <summary>雪球：滚动的球体，绕 X 轴横向移动。</summary>
        public static Mesh BuildSnowball()
        {
            return BuildIcoSphere(0.5f, 1);
        }

        /// <summary>刺针障碍：高瘦尖刺。须换道通过。</summary>
        public static Mesh BuildSpike()
        {
            return BuildCone(0.16f, 0.22f, 1.1f, 8);
        }

        /// <summary>中等栏：可跳可铲的通用障碍。</summary>
        public static Mesh BuildMediumBarrier()
        {
            return BuildRoundedBox(new Vector3(1.05f, 0.55f, 0.55f), 0.05f, 8, 6, 6);
        }

        /// <summary>飞行障碍：悬浮球体带光环。须跳跃通过（脚底需高于 FeetClearance）。</summary>
        public static Mesh BuildFlyingOrb()
        {
            var core = BuildIcoSphere(0.35f, 1);
            var ring = BuildTorus(0.48f, 0.04f, 16);
            return Combine("FlyingOrb", new[] {
                new CombineInstance { mesh = core, transform = Matrix4x4.identity },
                new CombineInstance { mesh = ring, transform = Matrix4x4.TRS(Vector3.zero, Quaternion.Euler(90f, 0f, 0f), Vector3.one) }
            });
        }

        /// <summary>巡逻敌人：球体 + 眼睛 + 光环（移动端简单的低面敌人）。</summary>
        public static Mesh BuildEnemyBody()
        {
            var body = BuildIcoSphere(0.45f, 1);
            var ring = BuildTorus(0.62f, 0.05f, 12);
            return Combine("EnemyBody", new[] {
                new CombineInstance { mesh = body, transform = Matrix4x4.identity },
                new CombineInstance { mesh = ring, transform = Matrix4x4.TRS(Vector3.zero, Quaternion.Euler(90f, 0f, 0f), Vector3.one) }
            });
        }

        // ── 道具 ────────────────────────────────────────────────

        /// <summary>通用道具：旋转星形。</summary>
        public static Mesh BuildPowerUpStar()
        {
            return BuildStarShape(0.35f, 0.15f, 6, 0.35f);
        }

        /// <summary>鱼干收集物：流线鱼身 + 尾鳍 + 背鳍 + 腹鳍（面向 -Z）。</summary>
        public static Mesh BuildFishSnack()
        {
            var body = BuildIcoSphere(1f, 1);
            var bodyTx = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(0.38f, 0.28f, 0.58f));

            var tail = BuildBox(new Vector3(0.34f, 0.24f, 0.07f));
            var tailTx = Matrix4x4.TRS(new Vector3(0f, 0f, 0.48f), Quaternion.Euler(10f, 0f, 0f), Vector3.one);

            var tailForkL = BuildBox(new Vector3(0.16f, 0.1f, 0.05f));
            var tailForkLTx = Matrix4x4.TRS(new Vector3(-0.1f, 0f, 0.54f), Quaternion.Euler(8f, -18f, 0f), Vector3.one);

            var tailForkR = BuildBox(new Vector3(0.16f, 0.1f, 0.05f));
            var tailForkRTx = Matrix4x4.TRS(new Vector3(0.1f, 0f, 0.54f), Quaternion.Euler(8f, 18f, 0f), Vector3.one);

            var dorsal = BuildBox(new Vector3(0.07f, 0.16f, 0.24f));
            var dorsalTx = Matrix4x4.TRS(new Vector3(0f, 0.26f, 0.06f), Quaternion.Euler(-18f, 0f, 0f), Vector3.one);

            var bellyFin = BuildBox(new Vector3(0.12f, 0.05f, 0.16f));
            var bellyTx = Matrix4x4.TRS(new Vector3(0f, -0.2f, 0.1f), Quaternion.Euler(22f, 0f, 0f), Vector3.one);

            return Combine("FishSnack", new[]
            {
                new CombineInstance { mesh = body, transform = bodyTx },
                new CombineInstance { mesh = tail, transform = tailTx },
                new CombineInstance { mesh = tailForkL, transform = tailForkLTx },
                new CombineInstance { mesh = tailForkR, transform = tailForkRTx },
                new CombineInstance { mesh = dorsal, transform = dorsalTx },
                new CombineInstance { mesh = bellyFin, transform = bellyTx },
            });
        }

        /// <summary>金币：厚圆盘 + 边缘环，备用拾取或 UI 装饰。</summary>
        public static Mesh BuildCoin()
        {
            var disc = BuildCylinder(0.24f, 0.07f, 16);
            var rim = BuildTorus(0.23f, 0.028f, 14);
            return Combine("Coin", new[]
            {
                new CombineInstance { mesh = disc, transform = Matrix4x4.identity },
                new CombineInstance { mesh = rim, transform = Matrix4x4.TRS(new Vector3(0f, 0.035f, 0f), Quaternion.identity, Vector3.one) },
            });
        }

        // ═══════════════════════════════════════════════════════
        //  基础几何体构建
        // ═══════════════════════════════════════════════════════

        /// <summary>圆角长方体（带倒角边的 Box）。</summary>
        private static Mesh BuildRoundedBox(Vector3 size, float bevel, int segsX, int segsY, int segsZ)
        {
            // 简化版：使用普通 Box + Scale（倒角对低面风格不是必须的）
            return BuildBox(size);
        }

        private static Mesh BuildBox(Vector3 size)
        {
            var hx = size.x * 0.5f; var hy = size.y * 0.5f; var hz = size.z * 0.5f;
            var verts = new Vector3[] {
                new(-hx,-hy,-hz), new( hx,-hy,-hz), new( hx, hy,-hz), new(-hx, hy,-hz),
                new(-hx,-hy, hz), new( hx,-hy, hz), new( hx, hy, hz), new(-hx, hy, hz),
            };
            var tris = new int[] {
                0,2,1,0,3,2, 4,5,6,4,6,7,
                0,1,5,0,5,4, 2,3,7,2,7,6,
                0,4,7,0,7,3, 1,2,6,1,6,5,
            };
            var mesh = new Mesh { name = "Box", vertices = verts, triangles = tris };
            mesh.RecalculateNormals(); mesh.RecalculateBounds();
            return mesh;
        }

        /// <summary>六角柱（水晶/冰柱）。</summary>
        private static Mesh BuildHexPillar(float radius, float height, float bevel)
        {
            const int segs = 6;
            var hh = height * 0.5f;
            var verts = new Vector3[segs * 2 + 2];
            var tris = new int[segs * 12];
            verts[segs * 2] = new Vector3(0f, hh, 0f);
            verts[segs * 2 + 1] = new Vector3(0f, -hh, 0f);

            for (var i = 0; i < segs; i++)
            {
                var a = i * Mathf.PI * 2f / segs;
                verts[i * 2] = new Vector3(Mathf.Cos(a) * radius, hh, Mathf.Sin(a) * radius);
                verts[i * 2 + 1] = new Vector3(Mathf.Cos(a) * radius, -hh, Mathf.Sin(a) * radius);
            }

            var ti = 0;
            for (var i = 0; i < segs; i++)
            {
                var n = (i + 1) % segs;
                var t = i * 2; var b = t + 1; var nt = n * 2; var nb = nt + 1;
                tris[ti++] = t; tris[ti++] = b; tris[ti++] = nt;
                tris[ti++] = nt; tris[ti++] = b; tris[ti++] = nb;
                tris[ti++] = segs * 2; tris[ti++] = nt; tris[ti++] = t;
                tris[ti++] = segs * 2 + 1; tris[ti++] = b; tris[ti++] = nb;
            }

            var mesh = new Mesh { name = "HexPillar", vertices = verts, triangles = tris };
            mesh.RecalculateNormals(); mesh.RecalculateBounds();
            return mesh;
        }

        /// <summary>锥体（尖刺）。</summary>
        private static Mesh BuildCone(float topR, float bottomR, float height, int segs)
        {
            var hh = height * 0.5f;
            var verts = new Vector3[segs * 2 + 2];
            verts[segs * 2] = new Vector3(0f, hh, 0f);
            verts[segs * 2 + 1] = new Vector3(0f, -hh, 0f);

            for (var i = 0; i < segs; i++)
            {
                var a = i * Mathf.PI * 2f / segs;
                verts[i * 2] = new Vector3(Mathf.Cos(a) * topR, hh, Mathf.Sin(a) * topR);
                verts[i * 2 + 1] = new Vector3(Mathf.Cos(a) * bottomR, -hh, Mathf.Sin(a) * bottomR);
            }

            var ti = 0;
            var tris = new int[segs * 12];
            for (var i = 0; i < segs; i++)
            {
                var n = (i + 1) % segs;
                var t = i * 2; var b = t + 1; var nt = n * 2; var nb = nt + 1;
                tris[ti++] = t; tris[ti++] = b; tris[ti++] = nt;
                tris[ti++] = nt; tris[ti++] = b; tris[ti++] = nb;
                tris[ti++] = segs * 2; tris[ti++] = nt; tris[ti++] = t;
                tris[ti++] = segs * 2 + 1; tris[ti++] = b; tris[ti++] = nb;
            }

            var mesh = new Mesh { name = "Cone", vertices = verts, triangles = tris };
            mesh.RecalculateNormals(); mesh.RecalculateBounds();
            return mesh;
        }

        /// <summary>ICO 球体（比 UV 球更均匀的三角分布，低面也圆润）。</summary>
        private static Mesh BuildIcoSphere(float radius, int subdiv)
        {
            // Icosahedron base
            var t = (1f + Mathf.Sqrt(5f)) * 0.5f;
            var verts = new System.Collections.Generic.List<Vector3> {
                new(-1f, t, 0f), new(1f, t, 0f), new(-1f, -t, 0f), new(1f, -t, 0f),
                new(0f, -1f, t), new(0f, 1f, t), new(0f, -1f, -t), new(0f, 1f, -t),
                new(t, 0f, -1f), new(t, 0f, 1f), new(-t, 0f, -1f), new(-t, 0f, 1f),
            };
            for (var i = 0; i < verts.Count; i++)
                verts[i] = verts[i].normalized * radius;

            var tris = new System.Collections.Generic.List<int> {
                0,11,5, 0,5,1, 0,1,7, 0,7,10, 0,10,11,
                1,5,9, 5,11,4, 11,10,2, 10,7,6, 7,1,8,
                3,9,4, 3,4,2, 3,2,6, 3,6,8, 3,8,9,
                4,9,5, 2,4,11, 6,2,10, 8,6,7, 9,8,1,
            };

            // Subdivide for smoother result
            for (var s = 0; s < subdiv; s++)
            {
                var newTris = new System.Collections.Generic.List<int>();
                var midCache = new System.Collections.Generic.Dictionary<long, int>();
                for (var i = 0; i < tris.Count; i += 3)
                {
                    var a = tris[i]; var b = tris[i + 1]; var c = tris[i + 2];
                    var ab = GetMidPoint(a, b, verts, midCache);
                    var bc = GetMidPoint(b, c, verts, midCache);
                    var ca = GetMidPoint(c, a, verts, midCache);
                    newTris.AddRange(new[] { a, ab, ca, ab, b, bc, ab, bc, ca, ca, bc, c });
                }
                tris = newTris;
            }

            // Normalize all to sphere
            for (var i = 0; i < verts.Count; i++)
                verts[i] = verts[i].normalized * radius;

            var mesh = new Mesh { name = "IcoSphere", vertices = verts.ToArray(), triangles = tris.ToArray() };
            mesh.RecalculateNormals(); mesh.RecalculateBounds();
            return mesh;
        }

        private static int GetMidPoint(int i1, int i2, System.Collections.Generic.List<Vector3> verts,
            System.Collections.Generic.Dictionary<long, int> cache)
        {
            var min = Mathf.Min(i1, i2); var max = Mathf.Max(i1, i2);
            var key = (long)min << 32 | (uint)max;
            if (cache.TryGetValue(key, out var idx)) return idx;
            var mid = (verts[i1] + verts[i2]) * 0.5f;
            idx = verts.Count;
            verts.Add(mid);
            cache[key] = idx;
            return idx;
        }

        /// <summary>圆柱体。</summary>
        private static Mesh BuildCylinder(float radius, float height, int segs)
        {
            var hh = height * 0.5f;
            var verts = new Vector3[segs * 2 + 2];
            var tris = new int[segs * 12];
            verts[segs * 2] = new Vector3(0f, hh, 0f);
            verts[segs * 2 + 1] = new Vector3(0f, -hh, 0f);

            for (var i = 0; i < segs; i++)
            {
                var a = i * Mathf.PI * 2f / segs;
                var x = Mathf.Cos(a) * radius; var z = Mathf.Sin(a) * radius;
                verts[i * 2] = new Vector3(x, hh, z);
                verts[i * 2 + 1] = new Vector3(x, -hh, z);
            }

            var ti = 0;
            for (var i = 0; i < segs; i++)
            {
                var n = (i + 1) % segs;
                var t = i * 2; var b = t + 1; var nt = n * 2; var nb = nt + 1;
                tris[ti++] = t; tris[ti++] = b; tris[ti++] = nt;
                tris[ti++] = nt; tris[ti++] = b; tris[ti++] = nb;
                tris[ti++] = segs * 2; tris[ti++] = nt; tris[ti++] = t;
                tris[ti++] = segs * 2 + 1; tris[ti++] = b; tris[ti++] = nb;
            }

            var mesh = new Mesh { name = "Cylinder", vertices = verts, triangles = tris };
            mesh.RecalculateNormals(); mesh.RecalculateBounds();
            return mesh;
        }

        /// <summary>圆环。</summary>
        private static Mesh BuildTorus(float majorR, float minorR, int segs)
        {
            var vc = segs * segs;
            var verts = new Vector3[vc];
            var tris = new int[segs * segs * 6];
            var v = 0;
            for (var i = 0; i < segs; i++)
            {
                var phi = i * Mathf.PI * 2f / segs;
                var cx = Mathf.Cos(phi) * majorR; var cz = Mathf.Sin(phi) * majorR;
                for (var j = 0; j < segs; j++)
                {
                    var theta = j * Mathf.PI * 2f / segs;
                    var rx = Mathf.Cos(theta) * minorR;
                    var ry = Mathf.Sin(theta) * minorR;
                    verts[v++] = new Vector3(cx + rx * Mathf.Cos(phi), ry, cz + rx * Mathf.Sin(phi));
                }
            }
            var t = 0;
            for (var i = 0; i < segs; i++)
            {
                var ni = (i + 1) % segs;
                for (var j = 0; j < segs; j++)
                {
                    var nj = (j + 1) % segs;
                    var a = i * segs + j; var b = ni * segs + j;
                    var c = ni * segs + nj; var d = i * segs + nj;
                    tris[t++] = a; tris[t++] = b; tris[t++] = c;
                    tris[t++] = a; tris[t++] = c; tris[t++] = d;
                }
            }
            var mesh = new Mesh { name = "Torus", vertices = verts, triangles = tris };
            mesh.RecalculateNormals(); mesh.RecalculateBounds();
            return mesh;
        }

        /// <summary>星形（道具用）。</summary>
        private static Mesh BuildStarShape(float outerR, float innerR, int points, float thickness)
        {
            var ht = thickness * 0.5f;
            var verts = new System.Collections.Generic.List<Vector3>();
            var tris = new System.Collections.Generic.List<int>();

            // Top center
            var topC = verts.Count; verts.Add(new Vector3(0f, ht, 0f));
            var botC = verts.Count; verts.Add(new Vector3(0f, -ht, 0f));

            for (var i = 0; i < points * 2; i++)
            {
                var a = i * Mathf.PI / points - Mathf.PI * 0.5f;
                var r = i % 2 == 0 ? outerR : innerR;
                verts.Add(new Vector3(Mathf.Cos(a) * r, ht, Mathf.Sin(a) * r));
                verts.Add(new Vector3(Mathf.Cos(a) * r, -ht, Mathf.Sin(a) * r));
            }

            for (var i = 0; i < points * 2; i++)
            {
                var n = (i + 1) % (points * 2);
                var ti = 2 + i * 2; var ni = 2 + n * 2;
                tris.AddRange(new[] { topC, ni, ti });
                tris.AddRange(new[] { botC, ti + 1, ni + 1 });
                tris.AddRange(new[] { ti, ni, ni + 1 });
                tris.AddRange(new[] { ti, ni + 1, ti + 1 });
            }

            var mesh = new Mesh { name = "Star", vertices = verts.ToArray(), triangles = tris.ToArray() };
            mesh.RecalculateNormals(); mesh.RecalculateBounds();
            return mesh;
        }

        /// <summary>合并多个 Mesh 为一个。</summary>
        private static Mesh Combine(string name, CombineInstance[] instances)
        {
            var mesh = new Mesh { name = name };
            mesh.CombineMeshes(instances, true, false);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }
    }
}
