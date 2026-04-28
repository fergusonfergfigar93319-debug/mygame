package com.example.mygame.engine.models

import com.example.mygame.engine.Mesh
import kotlin.math.PI
import kotlin.math.cos
import kotlin.math.sin

object LowPolyFactory {

    fun penguin(): Mesh {
        // simplified penguin: body box + belly box + beak
        val verts = mutableListOf<Float>()
        val inds = mutableListOf<Short>()
        var offset: Short = 0

        // body (dark)
        offset = addBox(verts, inds, offset, 0f, 0.7f, 0f, 0.6f, 1.4f, 0.5f, 0.12f, 0.12f, 0.14f)
        // belly (white, slightly forward)
        offset = addBox(verts, inds, offset, 0f, 0.65f, 0.12f, 0.42f, 1.1f, 0.3f, 0.92f, 0.94f, 0.96f)
        // beak (orange)
        offset = addBox(verts, inds, offset, 0f, 1.05f, 0.3f, 0.18f, 0.1f, 0.15f, 1f, 0.65f, 0.15f)
        // left eye (white)
        offset = addBox(verts, inds, offset, -0.12f, 1.15f, 0.25f, 0.07f, 0.07f, 0.05f, 1f, 1f, 1f)
        // right eye (white)
        addBox(verts, inds, offset, 0.12f, 1.15f, 0.25f, 0.07f, 0.07f, 0.05f, 1f, 1f, 1f)

        return Mesh(verts.toFloatArray(), inds.map { it }.toShortArray())
    }

    fun coin(): Mesh = Mesh.cylinder(0.2f, 0.06f, 8, 1f, 0.84f, 0f)

    fun iceWall(): Mesh = Mesh.box(1.8f, 2.2f, 1.0f, 0.6f, 0.85f, 0.95f)

    fun lowArch(): Mesh {
        val verts = mutableListOf<Float>()
        val inds = mutableListOf<Short>()
        var offset: Short = 0
        // left pillar
        offset = addBox(verts, inds, offset, -0.7f, 0.5f, 0f, 0.2f, 1f, 0.8f, 0.7f, 0.9f, 0.98f)
        // right pillar
        offset = addBox(verts, inds, offset, 0.7f, 0.5f, 0f, 0.2f, 1f, 0.8f, 0.7f, 0.9f, 0.98f)
        // top bar
        addBox(verts, inds, offset, 0f, 1.05f, 0f, 1.8f, 0.15f, 0.8f, 0.75f, 0.92f, 1f)
        return Mesh(verts.toFloatArray(), inds.map { it }.toShortArray())
    }

    fun fence(): Mesh = Mesh.box(8f, 0.6f, 0.15f, 0.55f, 0.65f, 0.7f)

    fun seal(): Mesh {
        val verts = mutableListOf<Float>()
        val inds = mutableListOf<Short>()
        var offset: Short = 0
        // body
        offset = addBox(verts, inds, offset, 0f, 0.35f, 0f, 0.8f, 0.7f, 0.5f, 0.38f, 0.49f, 0.55f)
        // head
        addBox(verts, inds, offset, 0f, 0.6f, 0.25f, 0.4f, 0.35f, 0.35f, 0.42f, 0.53f, 0.6f)
        return Mesh(verts.toFloatArray(), inds.map { it }.toShortArray())
    }

    fun questionBlock(): Mesh = Mesh.box(0.8f, 0.8f, 0.8f, 1f, 0.79f, 0.16f)

    /**
     * Upward-opening sky hemisphere (y+), centered at origin. View from inside — use GL_FRONT cull.
     * Rings from pole toward horizon; apex + stacks rings × (slices+1) verts.
     */
    fun skyHemisphere(radius: Float, stacks: Int, slices: Int): Mesh {
        val verts = mutableListOf<Float>()
        val inds = mutableListOf<Short>()
        val zenith = floatArrayOf(0.34f, 0.42f, 0.68f)
        val horizon = floatArrayOf(0.06f, 0.09f, 0.20f)
        verts.addAll(listOf(0f, radius, 0f, zenith[0], zenith[1], zenith[2], 1f))
        val apex: Short = 0

        fun colorAt(theta: Float): FloatArray {
            val t = (theta / (PI.toFloat() / 2f)).coerceIn(0f, 1f)
            val tt = t * t
            return floatArrayOf(
                horizon[0] + (zenith[0] - horizon[0]) * (1f - tt),
                horizon[1] + (zenith[1] - horizon[1]) * (1f - tt),
                horizon[2] + (zenith[2] - horizon[2]) * (1f - tt),
            )
        }

        for (ring in 1..stacks) {
            val theta = ring / stacks.toFloat() * (PI.toFloat() / 2f)
            val col = colorAt(theta)
            val st = sin(theta)
            for (v in 0..slices) {
                val phi = v.toFloat() / slices * PI.toFloat() * 2f
                verts.addAll(
                    listOf(
                        radius * st * cos(phi),
                        radius * cos(theta),
                        radius * st * sin(phi),
                        col[0],
                        col[1],
                        col[2],
                        1f,
                    ),
                )
            }
        }

        fun ringIdx(ring: Int, slice: Int): Short {
            assert(ring >= 1 && ring <= stacks)
            return (1 + (ring - 1) * (slices + 1) + (slice % (slices + 1))).toShort()
        }

        for (v in 0 until slices) {
            inds.addAll(listOf(apex, ringIdx(1, v + 1), ringIdx(1, v)))
        }

        for (ring in 1 until stacks) {
            for (v in 0 until slices) {
                val i0 = ringIdx(ring, v)
                val i1 = ringIdx(ring, v + 1)
                val i2 = ringIdx(ring + 1, v + 1)
                val i3 = ringIdx(ring + 1, v)
                inds.addAll(listOf(i0, i1, i2, i0, i2, i3))
            }
        }

        return Mesh(verts.toFloatArray(), inds.map { it }.toShortArray())
    }

    /**
     * 纬度球面带：与 [skyHemisphere] 同球心；青绿→青紫渐变，赤道向缘 sin 淡出；用于极光加法叠色。
     * [thetaMinRel]/[thetaMaxRel] 为相对“天顶角”的系数（均乘 π/2），建议如 0.22〜0.40。
     */
    fun auroraRibbonBand(
        radius: Float,
        thetaMinRel: Float,
        thetaMaxRel: Float,
        slices: Int,
    ): Mesh {
        val halfPi = PI.toFloat() / 2f
        val thetaMin = thetaMinRel * halfPi
        val thetaMax = thetaMaxRel * halfPi
        val verts = mutableListOf<Float>()
        val inds = mutableListOf<Short>()

        fun pos(theta: Float, phi: Float): Triple<Float, Float, Float> {
            val st = sin(theta)
            val x = radius * st * cos(phi)
            val y = radius * cos(theta)
            val z = radius * st * sin(phi)
            return Triple(x, y, z)
        }

        fun colorAt(phi: Float, edgeBlend: Float): FloatArray {
            val twoPi = 2f * PI.toFloat()
            val u = ((phi / twoPi) % 1f + 1f) % 1f
            val c0 = floatArrayOf(0.15f, 0.92f, 0.64f)
            val c1 = floatArrayOf(0.32f, 0.74f, 0.98f)
            val c2 = floatArrayOf(0.68f, 0.42f, 0.93f)
            val t = u * 2f
            val rgb = if (t <= 1f) {
                floatArrayOf(
                    c0[0] + (c1[0] - c0[0]) * t,
                    c0[1] + (c1[1] - c0[1]) * t,
                    c0[2] + (c1[2] - c0[2]) * t,
                )
            } else {
                val tt = t - 1f
                floatArrayOf(
                    c1[0] + (c2[0] - c1[0]) * tt,
                    c1[1] + (c2[1] - c1[1]) * tt,
                    c1[2] + (c2[2] - c1[2]) * tt,
                )
            }
            val wave = 0.78f + 0.22f * sin(phi * 4.8f + 0.9f)
            val edge = edgeBlend.coerceIn(0.04f, 1f)
            val a = (0.44f * wave * edge).coerceIn(0.1f, 0.58f)
            return floatArrayOf(rgb[0], rgb[1], rgb[2], a)
        }

        var nextIdx: Short = 0
        fun pushVert(theta: Float, phi: Float): Short {
            val (x, y, z) = pos(theta, phi)
            val tNorm = (theta - thetaMin) / (thetaMax - thetaMin)
            val edgeBlend = sin(tNorm * PI.toFloat()).toFloat()
            val c = colorAt(phi, edgeBlend)
            verts.addAll(listOf(x, y, z, c[0], c[1], c[2], c[3]))
            val id = nextIdx
            nextIdx = (nextIdx + 1).toShort()
            return id
        }

        for (i in 0 until slices) {
            val phi0 = i.toFloat() / slices * 2f * PI.toFloat()
            val phi1 = (i + 1).toFloat() / slices * 2f * PI.toFloat()
            val i0 = pushVert(thetaMin, phi0)
            val i1 = pushVert(thetaMin, phi1)
            val i2 = pushVert(thetaMax, phi1)
            val i3 = pushVert(thetaMax, phi0)
            inds.addAll(listOf(i0, i1, i2, i0, i2, i3))
        }

        return Mesh(verts.toFloatArray(), inds.map { it }.toShortArray())
    }

    /**
     * Uneven icy peaks spanning X along z=0 — tile along −Z as distant silhouettes.
     * [colorMul] scales vertex colors for far/mid atmospheric layers (darker blue = depth).
     */
    fun icyMountainBackdrop(
        width: Float,
        baseHeightMin: Float,
        baseHeightMax: Float,
        depth: Float,
        seed: Int = 137,
        colorMul: Triple<Float, Float, Float> = Triple(1f, 1f, 1f),
    ): Mesh {
        val verts = mutableListOf<Float>()
        val inds = mutableListOf<Short>()
        val peaks = 8
        val rng = kotlin.random.Random(seed)
        var offset: Short = 0
        val step = width / peaks
        val cr = colorMul.first
        val cg = colorMul.second
        val cb = colorMul.third
        for (i in 0 until peaks) {
            val cx = -width * 0.5f + step * i + step * 0.5f
            val hz = baseHeightMin + rng.nextFloat() * (baseHeightMax - baseHeightMin)
            val bw = step * (0.55f + rng.nextFloat() * 0.35f)
            val r = (0.18f + rng.nextFloat() * 0.12f) * cr
            val g = (0.32f + rng.nextFloat() * 0.18f) * cg
            val b = (0.48f + rng.nextFloat() * 0.16f) * cb
            offset = addBox(verts, inds, offset, cx, hz * 0.5f, 0f, bw, hz, depth, r, g, b)
        }
        return Mesh(verts.toFloatArray(), inds.map { it }.toShortArray())
    }

    /** Distant skyline with extra peaks — reads further back than eight-block strip for variety. */
    fun icyMountainSilhouette(
        width: Float,
        baseHeightMin: Float,
        baseHeightMax: Float,
        depth: Float,
        seed: Int,
        colorMul: Triple<Float, Float, Float>,
    ): Mesh {
        val verts = mutableListOf<Float>()
        val inds = mutableListOf<Short>()
        val rng = kotlin.random.Random(seed)
        val peaks = 12
        var offset: Short = 0
        val step = width / peaks
        val cr = colorMul.first
        val cg = colorMul.second
        val cb = colorMul.third
        for (i in 0 until peaks) {
            val cx = -width * 0.5f + step * i + step * 0.5f
            val jitter = (rng.nextFloat() - 0.5f) * step * 0.55f
            val hz = baseHeightMin + rng.nextFloat() * (baseHeightMax - baseHeightMin)
            val bw = step * (0.35f + rng.nextFloat() * 0.45f)
            val r = (0.12f + rng.nextFloat() * 0.1f) * cr
            val g = (0.22f + rng.nextFloat() * 0.14f) * cg
            val b = (0.38f + rng.nextFloat() * 0.14f) * cb
            offset = addBox(
                verts, inds, offset, cx + jitter, hz * 0.5f, rng.nextFloat() * 4f - 2f,
                bw, hz, depth * 0.65f,
                r, g, b,
            )
        }
        return Mesh(verts.toFloatArray(), inds.map { it }.toShortArray())
    }

    fun trackChunk(length: Float, laneWidth: Float): Mesh {
        val totalWidth = laneWidth * 3f + 1f
        val verts = mutableListOf<Float>()
        val inds = mutableListOf<Short>()
        var offset: Short = 0
        // ground slab
        offset = addBox(verts, inds, offset, 0f, -0.075f, 0f, totalWidth, 0.15f, length, 0.45f, 0.62f, 0.72f)
        // lane divider 1
        offset = addBox(verts, inds, offset, -laneWidth * 0.5f, 0.01f, 0f, 0.06f, 0.02f, length, 0.65f, 0.78f, 0.88f)
        // lane divider 2
        offset = addBox(verts, inds, offset, laneWidth * 0.5f, 0.01f, 0f, 0.06f, 0.02f, length, 0.65f, 0.78f, 0.88f)
        // side snow banks (slightly raised berms frame the run)
        val bankW = 1.1f
        val bankH = 0.22f
        val bankX = totalWidth * 0.5f + bankW * 0.5f - 0.25f
        offset = addBox(verts, inds, offset, -bankX, bankH * 0.5f, 0f, bankW, bankH, length, 0.72f, 0.84f, 0.92f)
        addBox(verts, inds, offset, bankX, bankH * 0.5f, 0f, bankW, bankH, length, 0.72f, 0.84f, 0.92f)
        return Mesh(verts.toFloatArray(), inds.map { it }.toShortArray())
    }

    private fun addBox(
        verts: MutableList<Float>, inds: MutableList<Short>,
        baseIdx: Short, cx: Float, cy: Float, cz: Float,
        w: Float, h: Float, d: Float,
        r: Float, g: Float, b: Float,
    ): Short {
        val hw = w / 2f; val hh = h / 2f; val hd = d / 2f
        val faces = arrayOf(
            // front, back, top, bottom, right, left
            floatArrayOf(0f, 0f, 1f), floatArrayOf(0f, 0f, -1f),
            floatArrayOf(0f, 1f, 0f), floatArrayOf(0f, -1f, 0f),
            floatArrayOf(1f, 0f, 0f), floatArrayOf(-1f, 0f, 0f),
        )
        val shades = floatArrayOf(1f, 0.8f, 0.95f, 0.6f, 0.85f, 0.75f)
        val corners = arrayOf(
            // front
            arrayOf(fv(-hw,-hh,hd), fv(hw,-hh,hd), fv(hw,hh,hd), fv(-hw,hh,hd)),
            // back
            arrayOf(fv(hw,-hh,-hd), fv(-hw,-hh,-hd), fv(-hw,hh,-hd), fv(hw,hh,-hd)),
            // top
            arrayOf(fv(-hw,hh,-hd), fv(-hw,hh,hd), fv(hw,hh,hd), fv(hw,hh,-hd)),
            // bottom
            arrayOf(fv(-hw,-hh,hd), fv(-hw,-hh,-hd), fv(hw,-hh,-hd), fv(hw,-hh,hd)),
            // right
            arrayOf(fv(hw,-hh,hd), fv(hw,-hh,-hd), fv(hw,hh,-hd), fv(hw,hh,hd)),
            // left
            arrayOf(fv(-hw,-hh,-hd), fv(-hw,-hh,hd), fv(-hw,hh,hd), fv(-hw,hh,-hd)),
        )
        var idx = baseIdx
        for (f in 0 until 6) {
            val s = shades[f]
            for (c in corners[f]) {
                verts.addAll(listOf(c[0]+cx, c[1]+cy, c[2]+cz, r*s, g*s, b*s, 1f))
            }
            inds.addAll(listOf(idx, (idx+1).toShort(), (idx+2).toShort(), idx, (idx+2).toShort(), (idx+3).toShort()))
            idx = (idx + 4).toShort()
        }
        return idx
    }

    private fun fv(x: Float, y: Float, z: Float) = floatArrayOf(x, y, z)
}
