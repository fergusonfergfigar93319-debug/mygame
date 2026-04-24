package com.example.mygame.game

import androidx.compose.ui.graphics.Color
import kotlin.math.cos
import kotlin.math.sin
import kotlin.random.Random

enum class ParticleStyle {
    /** 落地尘：小重力、空气阻力。 */
    Dust,
    /** 吃鱼干径向爆开。 */
    Burst,
    /** 鱼干冲刺尾迹。 */
    Trail,
    /** 踩怪击杀爆发（与 [Burst] 同物理，便于区分配色）。 */
    Stomp,
}

data class Particle(
    var x: Float,
    var y: Float,
    var vx: Float,
    var vy: Float,
    var life: Float,
    val maxLife: Float,
    val color: Color,
    val baseRadius: Float,
    val style: ParticleStyle,
)

private const val MAX_PARTICLES = 256

object ParticleSpawners {

    fun landingDust(
        out: MutableList<Particle>,
        worldX: Float,
        footY: Float,
        playerWidth: Float,
    ) {
        if (out.size >= MAX_PARTICLES) return
        val r = Random.Default
        repeat(5) {
            if (out.size >= MAX_PARTICLES) return
            val life = 0.35f + r.nextFloat() * 0.2f
            out += Particle(
                x = worldX + playerWidth * 0.5f + (r.nextFloat() - 0.5f) * playerWidth * 0.45f,
                y = footY,
                vx = (r.nextFloat() - 0.5f) * 200f,
                vy = -90f - r.nextFloat() * 240f,
                life = life,
                maxLife = life,
                color = Color(0xFFECEFF1).copy(alpha = 0.75f + r.nextFloat() * 0.2f),
                baseRadius = 2.2f + r.nextFloat() * 2.2f,
                style = ParticleStyle.Dust,
            )
        }
    }

    fun fishSnackBurst(
        out: MutableList<Particle>,
        centerX: Float,
        centerY: Float,
    ) {
        val r = Random.Default
        val n = 12
        for (i in 0 until n) {
            if (out.size >= MAX_PARTICLES) return
            val ang = (i / n.toFloat()) * 6.2832f + r.nextFloat() * 0.4f
            val sp = 120f + r.nextFloat() * 200f
            val lifeB = 0.28f + r.nextFloat() * 0.12f
            out += Particle(
                x = centerX,
                y = centerY,
                vx = cos(ang) * sp,
                vy = sin(ang) * sp * 0.75f,
                life = lifeB,
                maxLife = lifeB,
                color = when (i % 3) {
                    0 -> Color(0xFFFFB74D)
                    1 -> Color(0xFFFFE082)
                    else -> Color(0xFFFF8A65)
                },
                baseRadius = 2.4f + r.nextFloat() * 2.3f,
                style = ParticleStyle.Burst,
            )
        }
    }

    /** 踩踏敌人：冷色向四周射出，与鱼干爆区分。 */
    fun stompKillBurst(
        out: MutableList<Particle>,
        centerX: Float,
        centerY: Float,
    ) {
        val r = Random.Default
        val n = 10
        for (i in 0 until n) {
            if (out.size >= MAX_PARTICLES) return
            val ang = (i / n.toFloat()) * 6.2832f + r.nextFloat() * 0.55f
            val sp = 160f + r.nextFloat() * 220f
            val lifeB = 0.22f + r.nextFloat() * 0.1f
            out += Particle(
                x = centerX,
                y = centerY,
                vx = cos(ang) * sp,
                vy = sin(ang) * sp * 0.72f,
                life = lifeB,
                maxLife = lifeB,
                color = when (i % 4) {
                    0 -> Color(0xFFE1F5FE)
                    1 -> Color(0xFF81D4FA)
                    2 -> Color(0xFFB3E5FC)
                    else -> Color(0xFFFFFFFF).copy(alpha = 0.85f)
                },
                baseRadius = 2.0f + r.nextFloat() * 2.5f,
                style = ParticleStyle.Stomp,
            )
        }
    }

    fun fishDashTrail(
        out: MutableList<Particle>,
        worldX: Float,
        worldY: Float,
        size: Float,
        facingRight: Boolean,
    ) {
        if (out.size >= MAX_PARTICLES) return
        val r = Random.Default
        val back = if (facingRight) -1f else 1f
        val lifeT = 0.1f + r.nextFloat() * 0.06f
        out += Particle(
            x = worldX + size * 0.5f + back * (size * 0.2f) + (r.nextFloat() - 0.5f) * size * 0.1f,
            y = worldY + size * 0.45f,
            vx = 0f,
            vy = 0f,
            life = lifeT,
            maxLife = lifeT,
            color = if (r.nextBoolean()) {
                Color(0xFF81D4FA).copy(alpha = 0.6f)
            } else {
                Color.White.copy(alpha = 0.5f)
            },
            baseRadius = 1.5f + r.nextFloat() * 1.5f,
            style = ParticleStyle.Trail,
        )
    }
}

fun updateParticlesInPlace(
    particles: MutableList<Particle>,
    frameSeconds: Float,
) {
    if (particles.isEmpty()) return
    val dt = frameSeconds
    for (i in particles.indices.reversed()) {
        val p = particles[i]
        when (p.style) {
            ParticleStyle.Dust -> {
                p.x += p.vx * dt
                p.y += p.vy * dt
                p.vy += 420f * dt
                p.vx *= 0.95f
                p.life -= dt
            }
            ParticleStyle.Burst -> {
                p.x += p.vx * dt
                p.y += p.vy * dt
                p.vy += 200f * dt
                p.vx *= 0.97f
                p.life -= dt
            }
            ParticleStyle.Trail -> {
                p.life -= dt
            }
            ParticleStyle.Stomp -> {
                p.x += p.vx * dt
                p.y += p.vy * dt
                p.vy += 240f * dt
                p.vx *= 0.96f
                p.life -= dt
            }
        }
        if (p.life <= 0f) {
            particles.removeAt(i)
        }
    }
}
