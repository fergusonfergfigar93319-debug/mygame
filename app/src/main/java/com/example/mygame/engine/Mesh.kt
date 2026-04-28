package com.example.mygame.engine

import android.opengl.GLES20
import java.nio.ByteBuffer
import java.nio.ByteOrder
import java.nio.FloatBuffer
import java.nio.ShortBuffer

class Mesh(
    private val vertices: FloatArray,
    private val indices: ShortArray,
    private val hasColor: Boolean = true,
) {
    private val vertexBuffer: FloatBuffer
    private val indexBuffer: ShortBuffer
    val stride = if (hasColor) 7 * 4 else 3 * 4 // position(3) + color(4) or just position(3)

    init {
        vertexBuffer = ByteBuffer.allocateDirect(vertices.size * 4)
            .order(ByteOrder.nativeOrder())
            .asFloatBuffer()
            .put(vertices)
        vertexBuffer.position(0)

        indexBuffer = ByteBuffer.allocateDirect(indices.size * 2)
            .order(ByteOrder.nativeOrder())
            .asShortBuffer()
            .put(indices)
        indexBuffer.position(0)
    }

    fun draw(shader: ShaderProgram) {
        val posLoc = shader.getAttribLocation("aPosition")
        GLES20.glEnableVertexAttribArray(posLoc)
        vertexBuffer.position(0)
        GLES20.glVertexAttribPointer(posLoc, 3, GLES20.GL_FLOAT, false, stride, vertexBuffer)

        if (hasColor) {
            val colLoc = shader.getAttribLocation("aColor")
            GLES20.glEnableVertexAttribArray(colLoc)
            vertexBuffer.position(3)
            GLES20.glVertexAttribPointer(colLoc, 4, GLES20.GL_FLOAT, false, stride, vertexBuffer)
        }

        GLES20.glDrawElements(GLES20.GL_TRIANGLES, indices.size, GLES20.GL_UNSIGNED_SHORT, indexBuffer)

        GLES20.glDisableVertexAttribArray(posLoc)
        if (hasColor) {
            val colLoc = shader.getAttribLocation("aColor")
            GLES20.glDisableVertexAttribArray(colLoc)
        }
    }

    companion object {
        fun box(
            w: Float, h: Float, d: Float,
            r: Float, g: Float, b: Float, a: Float = 1f,
        ): Mesh {
            val hw = w / 2f; val hh = h / 2f; val hd = d / 2f
            val v = floatArrayOf(
                // front
                -hw, -hh, hd, r, g, b, a,
                 hw, -hh, hd, r, g, b, a,
                 hw,  hh, hd, r, g, b, a,
                -hw,  hh, hd, r, g, b, a,
                // back
                -hw, -hh, -hd, r*0.8f, g*0.8f, b*0.8f, a,
                 hw, -hh, -hd, r*0.8f, g*0.8f, b*0.8f, a,
                 hw,  hh, -hd, r*0.8f, g*0.8f, b*0.8f, a,
                -hw,  hh, -hd, r*0.8f, g*0.8f, b*0.8f, a,
                // top
                -hw, hh, -hd, r*0.95f, g*0.95f, b*0.95f, a,
                 hw, hh, -hd, r*0.95f, g*0.95f, b*0.95f, a,
                 hw, hh,  hd, r*0.95f, g*0.95f, b*0.95f, a,
                -hw, hh,  hd, r*0.95f, g*0.95f, b*0.95f, a,
                // bottom
                -hw, -hh, -hd, r*0.6f, g*0.6f, b*0.6f, a,
                 hw, -hh, -hd, r*0.6f, g*0.6f, b*0.6f, a,
                 hw, -hh,  hd, r*0.6f, g*0.6f, b*0.6f, a,
                -hw, -hh,  hd, r*0.6f, g*0.6f, b*0.6f, a,
                // right
                 hw, -hh, -hd, r*0.85f, g*0.85f, b*0.85f, a,
                 hw,  hh, -hd, r*0.85f, g*0.85f, b*0.85f, a,
                 hw,  hh,  hd, r*0.85f, g*0.85f, b*0.85f, a,
                 hw, -hh,  hd, r*0.85f, g*0.85f, b*0.85f, a,
                // left
                -hw, -hh, -hd, r*0.75f, g*0.75f, b*0.75f, a,
                -hw,  hh, -hd, r*0.75f, g*0.75f, b*0.75f, a,
                -hw,  hh,  hd, r*0.75f, g*0.75f, b*0.75f, a,
                -hw, -hh,  hd, r*0.75f, g*0.75f, b*0.75f, a,
            )
            val idx = shortArrayOf(
                0,1,2, 0,2,3,
                4,6,5, 4,7,6,
                8,9,10, 8,10,11,
                12,14,13, 12,15,14,
                16,17,18, 16,18,19,
                20,22,21, 20,23,22,
            )
            return Mesh(v, idx)
        }

        fun cylinder(radius: Float, height: Float, segments: Int, r: Float, g: Float, b: Float): Mesh {
            val verts = mutableListOf<Float>()
            val inds = mutableListOf<Short>()
            val hh = height / 2f
            for (i in 0..segments) {
                val angle = (i.toFloat() / segments) * Math.PI.toFloat() * 2f
                val x = kotlin.math.cos(angle) * radius
                val z = kotlin.math.sin(angle) * radius
                // bottom vertex
                verts.addAll(listOf(x, -hh, z, r*0.8f, g*0.8f, b*0.8f, 1f))
                // top vertex
                verts.addAll(listOf(x, hh, z, r, g, b, 1f))
            }
            // center bottom
            val cb = ((segments + 1) * 2).toShort()
            verts.addAll(listOf(0f, -hh, 0f, r*0.6f, g*0.6f, b*0.6f, 1f))
            // center top
            val ct = (cb + 1).toShort()
            verts.addAll(listOf(0f, hh, 0f, r, g, b, 1f))

            for (i in 0 until segments) {
                val b0 = (i * 2).toShort()
                val t0 = (i * 2 + 1).toShort()
                val b1 = ((i + 1) * 2).toShort()
                val t1 = ((i + 1) * 2 + 1).toShort()
                // side
                inds.addAll(listOf(b0, b1, t1, b0, t1, t0))
                // bottom cap
                inds.addAll(listOf(cb, b1, b0))
                // top cap
                inds.addAll(listOf(ct, t0, t1))
            }
            return Mesh(verts.toFloatArray(), inds.map { it }.toShortArray())
        }
    }
}
