package com.example.mygame.engine

import android.opengl.GLES20
import android.opengl.Matrix
import java.nio.ByteBuffer
import java.nio.ByteOrder
import java.nio.FloatBuffer
import kotlin.math.cos
import kotlin.math.sin
import kotlin.random.Random

private data class Particle(
    var x: Float, var y: Float, var z: Float,
    var vx: Float, var vy: Float, var vz: Float,
    var life: Float, var maxLife: Float,
    var r: Float, var g: Float, var b: Float,
    var size: Float,
)

class ParticleSystem {

    companion object {
        private const val MAX_PARTICLES = 512
        private const val FLOATS_PER_PARTICLE = 7 // x,y,z,r,g,b,a
    }

    private val particles = ArrayList<Particle>(MAX_PARTICLES)
    private val rng = Random(System.currentTimeMillis())

    private lateinit var shader: ShaderProgram
    private val vertexBuffer: FloatBuffer
    private val vertexData = FloatArray(MAX_PARTICLES * FLOATS_PER_PARTICLE)
    private val identityMvp = FloatArray(16)

    init {
        vertexBuffer = ByteBuffer.allocateDirect(MAX_PARTICLES * FLOATS_PER_PARTICLE * 4)
            .order(ByteOrder.nativeOrder())
            .asFloatBuffer()
        Matrix.setIdentityM(identityMvp, 0)
    }

    fun create() {
        shader = ShaderProgram(ShaderProgram.PARTICLE_VERTEX, ShaderProgram.PARTICLE_FRAGMENT)
    }

    // Running snow dust from feet
    fun spawnRunDust(x: Float, y: Float, z: Float) {
        if (particles.size >= MAX_PARTICLES) return
        repeat(2) {
            particles.add(Particle(
                x = x + rng.nextFloat() * 0.6f - 0.3f,
                y = y - 0.5f,
                z = z + rng.nextFloat() * 0.3f,
                vx = rng.nextFloat() * 0.8f - 0.4f,
                vy = rng.nextFloat() * 0.5f + 0.2f,
                vz = rng.nextFloat() * 0.4f + 0.2f,
                life = 0.4f + rng.nextFloat() * 0.3f,
                maxLife = 0.7f,
                r = 0.85f, g = 0.92f, b = 0.98f,
                size = 0.08f + rng.nextFloat() * 0.06f,
            ))
        }
    }

    // Jump trail (blue-white streaks)
    fun spawnJumpTrail(x: Float, y: Float, z: Float) {
        if (particles.size >= MAX_PARTICLES) return
        repeat(6) {
            val angle = rng.nextFloat() * Math.PI.toFloat() * 2f
            particles.add(Particle(
                x = x + cos(angle) * 0.3f,
                y = y,
                z = z + sin(angle) * 0.3f,
                vx = cos(angle) * 1.5f,
                vy = rng.nextFloat() * 2f,
                vz = sin(angle) * 1.5f + 0.5f,
                life = 0.3f + rng.nextFloat() * 0.2f,
                maxLife = 0.5f,
                r = 0.5f, g = 0.8f, b = 1f,
                size = 0.06f,
            ))
        }
    }

    // Landing impact dust
    fun spawnLandDust(x: Float, y: Float, z: Float) {
        if (particles.size >= MAX_PARTICLES) return
        repeat(10) {
            val angle = rng.nextFloat() * Math.PI.toFloat() * 2f
            val speed = rng.nextFloat() * 2.5f + 0.5f
            particles.add(Particle(
                x = x + cos(angle) * 0.2f,
                y = y - 0.5f,
                z = z + sin(angle) * 0.2f,
                vx = cos(angle) * speed,
                vy = rng.nextFloat() * 0.8f,
                vz = sin(angle) * speed,
                life = 0.35f + rng.nextFloat() * 0.25f,
                maxLife = 0.6f,
                r = 0.75f, g = 0.88f, b = 0.95f,
                size = 0.07f + rng.nextFloat() * 0.05f,
            ))
        }
    }

    // Coin sparkle
    fun spawnCoinSparkle(x: Float, y: Float, z: Float) {
        if (particles.size >= MAX_PARTICLES) return
        repeat(5) {
            val angle = rng.nextFloat() * Math.PI.toFloat() * 2f
            particles.add(Particle(
                x = x, y = y, z = z,
                vx = cos(angle) * 1.2f,
                vy = rng.nextFloat() * 2f + 0.5f,
                vz = sin(angle) * 1.2f,
                life = 0.25f + rng.nextFloat() * 0.2f,
                maxLife = 0.45f,
                r = 1f, g = 0.9f, b = 0.2f,
                size = 0.05f,
            ))
        }
    }

    // Death burst
    fun spawnDeathBurst(x: Float, y: Float, z: Float) {
        if (particles.size >= MAX_PARTICLES) return
        repeat(20) {
            val angle = rng.nextFloat() * Math.PI.toFloat() * 2f
            val pitch = rng.nextFloat() * Math.PI.toFloat() - Math.PI.toFloat() / 2f
            val speed = rng.nextFloat() * 4f + 1f
            particles.add(Particle(
                x = x, y = y, z = z,
                vx = cos(angle) * cos(pitch) * speed,
                vy = sin(pitch) * speed,
                vz = sin(angle) * cos(pitch) * speed,
                life = 0.5f + rng.nextFloat() * 0.5f,
                maxLife = 1f,
                r = 0.9f, g = 0.3f, b = 0.15f,
                size = 0.1f + rng.nextFloat() * 0.08f,
            ))
        }
    }

    // Fish dash trail
    fun spawnDashTrail(x: Float, y: Float, z: Float) {
        if (particles.size >= MAX_PARTICLES) return
        repeat(3) {
            particles.add(Particle(
                x = x + rng.nextFloat() * 0.4f - 0.2f,
                y = y + rng.nextFloat() * 0.6f - 0.1f,
                z = z + rng.nextFloat() * 0.3f,
                vx = rng.nextFloat() * 0.3f - 0.15f,
                vy = rng.nextFloat() * 0.2f,
                vz = rng.nextFloat() * 0.5f + 0.3f,
                life = 0.2f + rng.nextFloat() * 0.15f,
                maxLife = 0.35f,
                r = 1f, g = 0.55f, b = 0.1f,
                size = 0.09f,
            ))
        }
    }

    fun update(dt: Float) {
        val iter = particles.iterator()
        while (iter.hasNext()) {
            val p = iter.next()
            p.life -= dt
            if (p.life <= 0f) { iter.remove(); continue }
            p.x += p.vx * dt
            p.y += p.vy * dt
            p.z += p.vz * dt
            p.vy -= 4f * dt // gravity
            p.vx *= (1f - 3f * dt)
            p.vz *= (1f - 3f * dt)
        }
    }

    fun render(vpMatrix: FloatArray) {
        if (particles.isEmpty()) return

        GLES20.glEnable(GLES20.GL_BLEND)
        GLES20.glBlendFunc(GLES20.GL_SRC_ALPHA, GLES20.GL_ONE)

        shader.use()
        val mvpLoc = shader.getUniformLocation("uMVP")
        GLES20.glUniformMatrix4fv(mvpLoc, 1, false, vpMatrix, 0)

        var idx = 0
        val count = minOf(particles.size, MAX_PARTICLES)
        for (i in 0 until count) {
            val p = particles[i]
            val alpha = (p.life / p.maxLife).coerceIn(0f, 1f)
            vertexData[idx++] = p.x
            vertexData[idx++] = p.y
            vertexData[idx++] = p.z
            vertexData[idx++] = p.r
            vertexData[idx++] = p.g
            vertexData[idx++] = p.b
            vertexData[idx++] = alpha
        }

        vertexBuffer.position(0)
        vertexBuffer.put(vertexData, 0, count * FLOATS_PER_PARTICLE)
        vertexBuffer.position(0)

        val posLoc = shader.getAttribLocation("aPosition")
        val colLoc = shader.getAttribLocation("aColor")
        val stride = FLOATS_PER_PARTICLE * 4

        GLES20.glEnableVertexAttribArray(posLoc)
        GLES20.glVertexAttribPointer(posLoc, 3, GLES20.GL_FLOAT, false, stride, vertexBuffer)

        vertexBuffer.position(3)
        GLES20.glEnableVertexAttribArray(colLoc)
        GLES20.glVertexAttribPointer(colLoc, 4, GLES20.GL_FLOAT, false, stride, vertexBuffer)

        GLES20.glDrawArrays(GLES20.GL_POINTS, 0, count)

        GLES20.glDisableVertexAttribArray(posLoc)
        GLES20.glDisableVertexAttribArray(colLoc)
        GLES20.glDisable(GLES20.GL_BLEND)
    }

    fun clear() = particles.clear()
}
