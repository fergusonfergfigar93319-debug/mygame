package com.example.mygame.engine

import android.opengl.GLES20
import android.opengl.Matrix
import com.example.mygame.engine.models.LowPolyFactory

/**
 * 远景背景：球形天空穹顶 + 多层冰山剪影沿 −Z 平铺，
 * 与跑道 [TrackRenderer] 使用相同的世界坐标复用逻辑，避免地平线穿帮。
 */
class BackgroundRenderer {

    companion object {
        private const val SKY_RADIUS = 155f
        private const val AURORA_RADIUS = 148f
        private const val MOUNT_TILE = 105f
        private const val MOUNT_SEGMENT_COUNT = 6
        private const val EYE_SNAP_PARALLAX = 0.04f
        private const val PLAYER_SNAP_PARALLAX = 0.08f
    }

    private lateinit var skyMesh: Mesh
    private lateinit var auroraMesh: Mesh
    private lateinit var mountainMid: Mesh
    private lateinit var mountainFar: Mesh
    private val midSegmentZ = FloatArray(MOUNT_SEGMENT_COUNT)
    private val farSegmentZ = FloatArray(MOUNT_SEGMENT_COUNT)
    private val modelMatrix = FloatArray(16)

    fun create() {
        skyMesh = LowPolyFactory.skyHemisphere(SKY_RADIUS, 14, 40)
        auroraMesh = LowPolyFactory.auroraRibbonBand(
            AURORA_RADIUS,
            thetaMinRel = 0.21f,
            thetaMaxRel = 0.39f,
            slices = 56,
        )
        mountainMid = LowPolyFactory.icyMountainBackdrop(
            135f,
            11f,
            32f,
            17f,
            seed = 431,
            colorMul = Triple(0.78f, 0.84f, 0.94f),
        )
        mountainFar = LowPolyFactory.icyMountainSilhouette(
            158f,
            5f,
            21f,
            15f,
            seed = 901,
            colorMul = Triple(0.52f, 0.62f, 0.82f),
        )
        resetSegmentZs()
    }

    fun reset() {
        resetSegmentZs()
    }

    private fun resetSegmentZs() {
        val farPhase = MOUNT_TILE * 0.45f
        for (i in 0 until MOUNT_SEGMENT_COUNT) {
            midSegmentZ[i] = -i * MOUNT_TILE
            farSegmentZ[i] = -i * MOUNT_TILE - farPhase
        }
    }

    fun update(playerZ: Float) {
        recycle(midSegmentZ, playerZ)
        recycle(farSegmentZ, playerZ)
    }

    private fun recycle(positions: FloatArray, playerZ: Float) {
        val thresh = playerZ + MOUNT_TILE * 2f
        for (i in positions.indices) {
            if (positions[i] > thresh) {
                var minZ = Float.MAX_VALUE
                for (j in positions.indices) if (positions[j] < minZ) minZ = positions[j]
                positions[i] = minZ - MOUNT_TILE
            }
        }
    }

    /**
     * [speedFactor] 0〜1 で速度による天空色调偏移（与 RunnerRenderer 雾色连贯）。
     */
    fun render(
        shader: ShaderProgram,
        vpMatrix: FloatArray,
        speedFactor: Float,
        playerX: Float,
        playerZ: Float,
        eyeX: Float,
    ) {
        val parallax = eyeX * EYE_SNAP_PARALLAX + playerX * PLAYER_SNAP_PARALLAX

        // --- sky dome (inside surface, ignore depth before world geometry) ---
        val tr = (0.9f + speedFactor * 0.1f).coerceAtMost(1.05f)
        val tg = (0.95f + speedFactor * 0.05f).coerceAtMost(1.05f)
        val tb = (1f + speedFactor * 0.06f).coerceAtMost(1.08f)

        GLES20.glDepthMask(false)
        GLES20.glDisable(GLES20.GL_DEPTH_TEST)
        GLES20.glDisable(GLES20.GL_CULL_FACE)

        Matrix.setIdentityM(modelMatrix, 0)
        Matrix.translateM(modelMatrix, 0, parallax * 1.25f + playerX * 0.03f, -6f, playerZ)
        shader.setTransform(vpMatrix, modelMatrix, 0f, skyPass = true)
        GLES20.glUniform3f(shader.getUniformLocation("uSkyTint"), tr, tg, tb)
        skyMesh.draw(shader)

        // 极光加法叠在天空内侧（与天空同位姿，略偏的青紫色调随速度增强）
        GLES20.glUniform3f(shader.getUniformLocation("uSkyTint"), 1f, 1f, 1f)
        val aR = 0.82f + speedFactor * 0.14f
        val aG = 0.9f + speedFactor * 0.1f
        val aB = 1f + speedFactor * 0.1f
        val strength = 0.36f + speedFactor * 0.18f
        GLES20.glUniform1f(shader.getUniformLocation("uAuroraStrength"), strength)
        GLES20.glEnable(GLES20.GL_BLEND)
        GLES20.glBlendFunc(GLES20.GL_ONE, GLES20.GL_ONE)
        Matrix.setIdentityM(modelMatrix, 0)
        Matrix.translateM(modelMatrix, 0, parallax * 1.25f + playerX * 0.03f, -6f, playerZ)
        shader.setTransform(
            vpMatrix, modelMatrix, 0f,
            skyPass = false,
            auroraPass = true,
            auroraTintR = aR,
            auroraTintG = aG,
            auroraTintB = aB,
        )
        auroraMesh.draw(shader)
        GLES20.glDisable(GLES20.GL_BLEND)

        GLES20.glDepthMask(true)
        GLES20.glEnable(GLES20.GL_DEPTH_TEST)
        GLES20.glEnable(GLES20.GL_CULL_FACE)
        GLES20.glCullFace(GLES20.GL_BACK)

        // --- distant mountains (far layer first keeps depth coherence) ---
        for (sz in farSegmentZ) {
            Matrix.setIdentityM(modelMatrix, 0)
            Matrix.translateM(modelMatrix, 0, parallax * 1.55f - playerX * 0.02f, 0f, sz)
            shader.setTransform(vpMatrix, modelMatrix, 0f, skyPass = false)
            mountainFar.draw(shader)
        }
        for (sz in midSegmentZ) {
            Matrix.setIdentityM(modelMatrix, 0)
            Matrix.translateM(modelMatrix, 0, parallax + playerX * 0.02f, 0f, sz)
            shader.setTransform(vpMatrix, modelMatrix, 0f, skyPass = false)
            mountainMid.draw(shader)
        }
    }
}
