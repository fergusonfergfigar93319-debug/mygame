package com.example.mygame.engine

import android.opengl.Matrix
import com.example.mygame.engine.models.LowPolyFactory

class TrackRenderer {

    companion object {
        const val LANE_WIDTH = 3f
        const val CHUNK_LENGTH = 40f
        const val VISIBLE_CHUNKS = 6
    }

    private lateinit var chunkMesh: Mesh
    private val chunkPositions = FloatArray(VISIBLE_CHUNKS)
    private val modelMatrix = FloatArray(16)

    fun create() {
        chunkMesh = LowPolyFactory.trackChunk(CHUNK_LENGTH, LANE_WIDTH)
        reset()
    }

    /** 与 [create] 一致；每局开始须调用，否则会延续上一局的 −Z 履带位置导致玩家踩在地图外 */
    fun reset() {
        for (i in 0 until VISIBLE_CHUNKS) chunkPositions[i] = -i * CHUNK_LENGTH
    }

    fun update(playerZ: Float) {
        for (i in 0 until VISIBLE_CHUNKS) {
            if (chunkPositions[i] > playerZ + CHUNK_LENGTH * 2) {
                var minZ = Float.MAX_VALUE
                for (j in 0 until VISIBLE_CHUNKS) if (chunkPositions[j] < minZ) minZ = chunkPositions[j]
                chunkPositions[i] = minZ - CHUNK_LENGTH
            }
        }
    }

    fun render(shader: ShaderProgram, vpMatrix: FloatArray) {
        for (i in 0 until VISIBLE_CHUNKS) {
            Matrix.setIdentityM(modelMatrix, 0)
            Matrix.translateM(modelMatrix, 0, 0f, 0f, chunkPositions[i])
            shader.setTransform(vpMatrix, modelMatrix, 0f)
            chunkMesh.draw(shader)
        }
    }
}
