package com.example.mygame.engine

import android.opengl.GLES20
import android.opengl.Matrix
import com.example.mygame.engine.models.LowPolyFactory
import kotlin.math.abs
import kotlin.math.max
import kotlin.math.sin

class PlayerController {

    companion object {
        const val GRAVITY = 22f
        const val JUMP_VELOCITY = 12f
        const val HIGH_JUMP_VELOCITY = 14f
        const val LANE_SWITCH_SPEED = 18f
        const val SLIDE_DURATION = 0.5f
        const val COYOTE_TIME = 0.12f
        const val JUMP_BUFFER_TIME = 0.14f
        const val GROUND_Y = 0.7f
    }

    var x = 0f; var y = GROUND_Y; var z = 0f
    var velocityY = 0f; private set
    var currentLane = Lane.Center; private set
    var state = RunnerPlayerState.Running; private set
    var onGround = true; private set

    // animation state
    private var runTime = 0f
    private var bobPhase = 0f
    private var tiltZ = 0f          // roll tilt when switching lanes
    private var targetTiltZ = 0f
    private var squashY = 1f        // squash/stretch on jump/land
    private var targetSquashY = 1f
    private var landBounce = 0f     // extra upward bob on landing
    private var prevOnGround = true

    // lane switch tracking for camera lean
    var lastLaneDir = 0f; private set  // -1 left, +1 right, 0 none

    private var slideTimer = 0f
    private var coyoteTimer = COYOTE_TIME
    private var jumpBufferTimer = 0f

    private lateinit var penguinMesh: Mesh
    private val modelMatrix = FloatArray(16)
    private val mvpMatrix = FloatArray(16)

    fun create() {
        penguinMesh = LowPolyFactory.penguin()
        reset()
    }

    fun jump(highJump: Boolean = false) {
        if (state == RunnerPlayerState.Dead) return
        if (state == RunnerPlayerState.Sliding) {
            slideTimer = 0f; state = RunnerPlayerState.Running
        }
        if (onGround || coyoteTimer > 0f) {
            velocityY = if (highJump) HIGH_JUMP_VELOCITY else JUMP_VELOCITY
            onGround = false; coyoteTimer = 0f; jumpBufferTimer = 0f
            state = RunnerPlayerState.Jumping
            squashY = 1.35f; targetSquashY = 0.75f  // stretch on takeoff
        } else {
            jumpBufferTimer = JUMP_BUFFER_TIME
        }
    }

    fun slide() {
        if (state == RunnerPlayerState.Dead) return
        if (state == RunnerPlayerState.Jumping) { velocityY = -JUMP_VELOCITY * 1.5f; return }
        if (onGround) {
            state = RunnerPlayerState.Sliding; slideTimer = SLIDE_DURATION
        }
    }

    fun moveLeft() {
        if (state == RunnerPlayerState.Dead) return
        val next = currentLane.moveLeft()
        if (next != currentLane) {
            currentLane = next; targetTiltZ = 12f; lastLaneDir = -1f
        }
    }

    fun moveRight() {
        if (state == RunnerPlayerState.Dead) return
        val next = currentLane.moveRight()
        if (next != currentLane) {
            currentLane = next; targetTiltZ = -12f; lastLaneDir = 1f
        }
    }

    fun die() { state = RunnerPlayerState.Dead; velocityY = 0f }

    fun reset() {
        currentLane = Lane.Center; x = Lane.Center.xOffset; y = GROUND_Y; z = 0f
        velocityY = 0f; onGround = true; state = RunnerPlayerState.Running
        slideTimer = 0f; coyoteTimer = COYOTE_TIME; jumpBufferTimer = 0f
        runTime = 0f; bobPhase = 0f; tiltZ = 0f; targetTiltZ = 0f
        squashY = 1f; targetSquashY = 1f; landBounce = 0f; lastLaneDir = 0f
    }

    fun update(dt: Float, runSpeed: Float): PlayerAnimEvents {
        val events = PlayerAnimEvents()
        if (state == RunnerPlayerState.Dead) return events

        z -= runSpeed * dt

        // lane transition
        val targetX = currentLane.xOffset
        x += (targetX - x) * (LANE_SWITCH_SPEED * dt).coerceIn(0f, 1f)
        if (abs(x - targetX) < 0.05f) lastLaneDir = 0f

        // tilt animation
        tiltZ += (targetTiltZ - tiltZ) * (14f * dt).coerceIn(0f, 1f)
        targetTiltZ *= (1f - 8f * dt).coerceAtLeast(0f)

        // gravity
        velocityY -= GRAVITY * dt
        y += velocityY * dt

        prevOnGround = onGround
        if (y <= GROUND_Y) {
            y = GROUND_Y; velocityY = 0f; onGround = true; coyoteTimer = COYOTE_TIME
            if (!prevOnGround) {
                // landing
                squashY = 0.65f; targetSquashY = 1f; landBounce = 0.18f
                events.landed = true
            }
            if (state == RunnerPlayerState.Jumping) state = RunnerPlayerState.Running
        } else {
            onGround = false; coyoteTimer = max(0f, coyoteTimer - dt)
        }

        if (jumpBufferTimer > 0f) { jumpBufferTimer -= dt; if (onGround) jump() }

        if (state == RunnerPlayerState.Sliding) {
            slideTimer -= dt; if (slideTimer <= 0f) state = RunnerPlayerState.Running
        }

        // squash/stretch spring
        squashY += (targetSquashY - squashY) * (12f * dt).coerceIn(0f, 1f)

        // landing bounce decay
        landBounce *= (1f - 10f * dt).coerceAtLeast(0f)

        // run bob (only when on ground and running)
        if (onGround && state == RunnerPlayerState.Running) {
            runTime += dt
            bobPhase += runSpeed * dt * 1.8f
            events.footStep = (bobPhase % (Math.PI.toFloat() * 2f)) < (runSpeed * dt * 1.8f)
        }

        return events
    }

    fun getVisualY(): Float {
        val bob = if (onGround && state == RunnerPlayerState.Running)
            sin(bobPhase) * 0.06f else 0f
        return y + bob + landBounce
    }

    fun render(shader: ShaderProgram, vpMatrix: FloatArray) {
        val scaleY = when (state) {
            RunnerPlayerState.Sliding -> 0.5f
            else -> squashY
        }
        val scaleX = if (state != RunnerPlayerState.Sliding) (2f - squashY).coerceIn(0.85f, 1.15f) else 1f

        Matrix.setIdentityM(modelMatrix, 0)
        Matrix.translateM(modelMatrix, 0, x, getVisualY(), z)
        Matrix.rotateM(modelMatrix, 0, 180f, 0f, 1f, 0f)
        Matrix.rotateM(modelMatrix, 0, tiltZ, 0f, 0f, 1f)
        Matrix.scaleM(modelMatrix, 0, scaleX, scaleY, scaleX)

        Matrix.multiplyMM(mvpMatrix, 0, vpMatrix, 0, modelMatrix, 0)

        val mvpLoc = shader.getUniformLocation("uMVP")
        val modelLoc = shader.getUniformLocation("uModel")
        val emissiveLoc = shader.getUniformLocation("uEmissive")
        GLES20.glUniformMatrix4fv(mvpLoc, 1, false, mvpMatrix, 0)
        GLES20.glUniformMatrix4fv(modelLoc, 1, false, modelMatrix, 0)
        GLES20.glUniform1f(emissiveLoc, 0f)
        penguinMesh.draw(shader)
    }
}

data class PlayerAnimEvents(
    var landed: Boolean = false,
    var footStep: Boolean = false,
)
