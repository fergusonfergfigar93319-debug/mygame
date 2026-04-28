package com.example.mygame.engine

import android.opengl.Matrix
import kotlin.random.Random

class EnvironmentRenderer {

    companion object {
        const val DECOR_SPACING = 12f
        const val DECOR_SIDE_OFFSET = 8f
        const val VISIBLE_AHEAD = 100f
        const val VISIBLE_BEHIND = 15f
    }

    private lateinit var icebergMesh: Mesh
    private lateinit var snowTreeMesh: Mesh
    private lateinit var icePillarMesh: Mesh

    private data class Decor(val x: Float, val z: Float, val type: Int, val scale: Float, val rotY: Float)

    private val decors = mutableListOf<Decor>()
    private var nextDecorZ = 0f
    private val rng = Random(42)

    private val modelMatrix = FloatArray(16)
    private val mvpMatrix = FloatArray(16)

    fun create() {
        icebergMesh = Mesh.box(2f, 2.5f, 2f, 0.55f, 0.72f, 0.82f)
        snowTreeMesh = createSnowTree()
        icePillarMesh = Mesh.box(0.5f, 4f, 0.5f, 0.6f, 0.8f, 0.9f)
    }

    private fun createSnowTree(): Mesh {
        val verts = mutableListOf<Float>()
        val inds = mutableListOf<Short>()
        var offset: Short = 0
        // trunk
        offset = addBox(verts, inds, offset, 0f, 0.6f, 0f, 0.2f, 1.2f, 0.2f, 0.35f, 0.25f, 0.18f)
        // foliage layers
        offset = addBox(verts, inds, offset, 0f, 1.6f, 0f, 1.2f, 0.6f, 1.2f, 0.2f, 0.45f, 0.25f)
        offset = addBox(verts, inds, offset, 0f, 2.2f, 0f, 0.9f, 0.5f, 0.9f, 0.22f, 0.5f, 0.28f)
        addBox(verts, inds, offset, 0f, 2.7f, 0f, 0.5f, 0.4f, 0.5f, 0.25f, 0.55f, 0.3f)
        // snow caps
        return Mesh(verts.toFloatArray(), inds.map { it }.toShortArray())
    }

    fun update(playerZ: Float) {
        while (nextDecorZ > playerZ - VISIBLE_AHEAD) {
            val side = if (rng.nextBoolean()) DECOR_SIDE_OFFSET else -DECOR_SIDE_OFFSET
            val jitter = (rng.nextFloat() - 0.5f) * 3f
            decors.add(
                Decor(
                    x = side + jitter,
                    z = nextDecorZ,
                    type = rng.nextInt(3),
                    scale = 0.7f + rng.nextFloat() * 0.8f,
                    rotY = rng.nextFloat() * 360f,
                ),
            )
            // sometimes add on both sides
            if (rng.nextFloat() < 0.4f) {
                decors.add(
                    Decor(
                        x = -side + (rng.nextFloat() - 0.5f) * 2f,
                        z = nextDecorZ - rng.nextFloat() * 4f,
                        type = rng.nextInt(3),
                        scale = 0.6f + rng.nextFloat() * 0.6f,
                        rotY = rng.nextFloat() * 360f,
                    ),
                )
            }
            nextDecorZ -= DECOR_SPACING
        }
        decors.removeAll { it.z > playerZ + VISIBLE_BEHIND }
    }

    fun render(shader: ShaderProgram, vpMatrix: FloatArray) {
        for (d in decors) {
            Matrix.setIdentityM(modelMatrix, 0)
            Matrix.translateM(modelMatrix, 0, d.x, 0f, d.z)
            Matrix.rotateM(modelMatrix, 0, d.rotY, 0f, 1f, 0f)
            Matrix.scaleM(modelMatrix, 0, d.scale, d.scale, d.scale)
            shader.setTransform(vpMatrix, modelMatrix, 0f)
            when (d.type) {
                0 -> icebergMesh.draw(shader)
                1 -> snowTreeMesh.draw(shader)
                2 -> icePillarMesh.draw(shader)
            }
        }
    }

    fun reset() {
        decors.clear()
        nextDecorZ = 0f
    }

    private fun addBox(
        verts: MutableList<Float>, inds: MutableList<Short>,
        baseIdx: Short, cx: Float, cy: Float, cz: Float,
        w: Float, h: Float, d: Float,
        r: Float, g: Float, b: Float,
    ): Short {
        val hw = w / 2f; val hh = h / 2f; val hd = d / 2f
        val shades = floatArrayOf(1f, 0.8f, 0.95f, 0.6f, 0.85f, 0.75f)
        val corners = arrayOf(
            arrayOf(fv(-hw,-hh,hd), fv(hw,-hh,hd), fv(hw,hh,hd), fv(-hw,hh,hd)),
            arrayOf(fv(hw,-hh,-hd), fv(-hw,-hh,-hd), fv(-hw,hh,-hd), fv(hw,hh,-hd)),
            arrayOf(fv(-hw,hh,-hd), fv(-hw,hh,hd), fv(hw,hh,hd), fv(hw,hh,-hd)),
            arrayOf(fv(-hw,-hh,hd), fv(-hw,-hh,-hd), fv(hw,-hh,-hd), fv(hw,-hh,hd)),
            arrayOf(fv(hw,-hh,hd), fv(hw,-hh,-hd), fv(hw,hh,-hd), fv(hw,hh,hd)),
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
