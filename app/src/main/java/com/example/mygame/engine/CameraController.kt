package com.example.mygame.engine

import android.opengl.GLES20
import android.opengl.Matrix
import kotlin.math.max

class CameraController {

    val viewMatrix = FloatArray(16)
    val projMatrix = FloatArray(16)
    val vpMatrix = FloatArray(16)

    private val eyePos = floatArrayOf(0f, 4.5f, 7f)

    val eyeWorldX get() = eyePos[0]
    val eyeWorldY get() = eyePos[1]
    val eyeWorldZ get() = eyePos[2]
    private val lookTarget = floatArrayOf(0f, 1.2f, -6f)
    private val baseOffset = floatArrayOf(0f, 4.5f, 7f)
    private val lookAhead = floatArrayOf(0f, 1.2f, -6f)

    private var shakeAmplitude = 0f
    private val shakeDecay = 0.88f

    // lean: camera tilts slightly when player switches lanes
    private var leanX = 0f
    private var targetLeanX = 0f

    // speed FOV: widens slightly at high speed
    private var currentFov = 67f
    private var targetFov = 67f

    fun setProjection(width: Int, height: Int, fov: Float = 67f) {
        val aspect = width.toFloat() / height.toFloat()
        Matrix.perspectiveM(projMatrix, 0, fov, aspect, 0.1f, 200f)
    }

    fun update(playerX: Float, playerY: Float, playerZ: Float, dt: Float, speed: Float = 8f, laneDir: Float = 0f) {
        // speed-based FOV
        targetFov = 67f + (speed - 8f) * 0.35f
        currentFov += (targetFov - currentFov) * (3f * dt).coerceIn(0f, 1f)

        // lane lean
        targetLeanX = laneDir * 0.6f
        leanX += (targetLeanX - leanX) * (10f * dt).coerceIn(0f, 1f)

        val targetEyeX = playerX + baseOffset[0] + leanX
        val targetEyeY = playerY + baseOffset[1]
        val targetEyeZ = playerZ + baseOffset[2]

        val lerpFactor = (8f * dt).coerceIn(0f, 1f)
        eyePos[0] += (targetEyeX - eyePos[0]) * lerpFactor
        eyePos[1] += (targetEyeY - eyePos[1]) * lerpFactor
        eyePos[2] += (targetEyeZ - eyePos[2]) * lerpFactor

        lookTarget[0] = playerX + lookAhead[0]
        lookTarget[1] = playerY + lookAhead[1]
        lookTarget[2] = playerZ + lookAhead[2]

        var ex = eyePos[0]; var ey = eyePos[1]
        if (shakeAmplitude > 0.01f) {
            ex += (Math.random().toFloat() - 0.5f) * 2f * shakeAmplitude
            ey += (Math.random().toFloat() - 0.5f) * shakeAmplitude
            shakeAmplitude *= shakeDecay
        } else {
            shakeAmplitude = 0f
        }

        Matrix.setLookAtM(viewMatrix, 0,
            ex, ey, eyePos[2],
            lookTarget[0], lookTarget[1], lookTarget[2],
            0f, 1f, 0f)
        Matrix.multiplyMM(vpMatrix, 0, projMatrix, 0, viewMatrix, 0)
    }

    fun shake(amplitude: Float) { shakeAmplitude = max(shakeAmplitude, amplitude) }

    fun setLane(dir: Float) { targetLeanX = dir }

    fun reset(playerZ: Float) {
        eyePos[0] = 0f; eyePos[1] = 4.5f; eyePos[2] = playerZ + 7f
        leanX = 0f; targetLeanX = 0f; currentFov = 67f
    }

    fun rebuildProjection(width: Int, height: Int) {
        val aspect = width.toFloat() / height.toFloat()
        Matrix.perspectiveM(projMatrix, 0, currentFov, aspect, 0.1f, 200f)
    }

    var viewportWidth = 1; var viewportHeight = 1
}
