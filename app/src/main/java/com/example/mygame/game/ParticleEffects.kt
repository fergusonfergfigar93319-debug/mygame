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
    /** 重落地：贴地两侧散开。 */
    HardLandingDust,
    /** 鱼干冲刺穿怪：以水平动量为主。 */
    DashPierce,
    /** 脆弱薄冰碎裂。 */
    FragileIce,
    /** Boss 冲击波前缘，冰青碎点。 */
    BossWave,
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

    /**
     * 重落地：沿脚底向左右扫开的雪尘，略多于 [landingDust]。
     */
    fun hardLandingDust(
        out: MutableList<Particle>,
        worldX: Float,
        footY: Float,
        playerWidth: Float,
    ) {
        if (out.size >= MAX_PARTICLES) return
        val r = Random.Default
        val n = 12
        repeat(n) {
            if (out.size >= MAX_PARTICLES) return
            val t = (it / n.toFloat() - 0.5f) * 1.1f
            val life = 0.38f + r.nextFloat() * 0.16f
            val along = t * (playerWidth * 0.55f) + (r.nextFloat() - 0.5f) * playerWidth * 0.2f
            val vx = along * 6f + (r.nextFloat() - 0.5f) * 90f
            out += Particle(
                x = worldX + playerWidth * 0.5f,
                y = footY,
                vx = vx,
                vy = -40f - r.nextFloat() * 120f,
                life = life,
                maxLife = life,
                color = Color(0xFFE1EEF7).copy(alpha = 0.78f + r.nextFloat() * 0.18f),
                baseRadius = 2.4f + r.nextFloat() * 2.8f,
                style = ParticleStyle.HardLandingDust,
            )
        }
    }

    /**
     * 鱼干冲刺贯穿敌人：以面朝方向为正向的高水平动量，少量纵向散布。
     */
    fun fragileIceShatter(
        out: MutableList<Particle>,
        centerX: Float,
        centerY: Float,
    ) {
        if (out.size >= MAX_PARTICLES) return
        val r = Random.Default
        val n = 11
        for (i in 0 until n) {
            if (out.size >= MAX_PARTICLES) return
            val ang = (i / n.toFloat()) * 6.2832f + r.nextFloat() * 0.4f
            val sp = 90f + r.nextFloat() * 200f
            val lifeB = 0.2f + r.nextFloat() * 0.12f
            out += Particle(
                x = centerX,
                y = centerY,
                vx = cos(ang) * sp,
                vy = sin(ang) * sp * 0.55f - 60f,
                life = lifeB,
                maxLife = lifeB,
                color = when (i % 3) {
                    0 -> Color(0xFFBBDEFB)
                    1 -> Color(0xFFFFFFFF).copy(alpha = 0.9f)
                    else -> Color(0xFF90CAF9)
                },
                baseRadius = 1.8f + r.nextFloat() * 2.2f,
                style = ParticleStyle.FragileIce,
            )
        }
    }

    fun dashPierceBurst(
        out: MutableList<Particle>,
        centerX: Float,
        centerY: Float,
        facingRight: Boolean,
    ) {
        if (out.size >= MAX_PARTICLES) return
        val r = Random.Default
        val forward = if (facingRight) 1f else -1f
        val n = 15
        for (i in 0 until n) {
            if (out.size >= MAX_PARTICLES) return
            val sp = 300f + r.nextFloat() * 280f
            val f = 0.45f + r.nextFloat() * 0.9f
            val lifeB = 0.2f + r.nextFloat() * 0.1f
            out += Particle(
                x = centerX,
                y = centerY,
                vx = forward * sp * f,
                vy = (r.nextFloat() - 0.5f) * 200f,
                life = lifeB,
                maxLife = lifeB,
                color = when (i % 3) {
                    0 -> Color(0xFF81D4FA).copy(alpha = 0.9f)
                    1 -> Color(0xFFE1F5FE)
                    else -> Color(0xFFFFFFFF).copy(alpha = 0.75f)
                },
                baseRadius = 1.6f + r.nextFloat() * 2.2f,
                style = ParticleStyle.DashPierce,
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

    /** 踩踏敌人：冷色碎雪向四周射出，略带上抛，与鱼干爆区分。 */
    fun stompKillBurst(
        out: MutableList<Particle>,
        centerX: Float,
        centerY: Float,
        /** Boss 终结等演出：>1 增加数量。 */
        countMultiplier: Float = 1f,
        /** <1 降低初速，配合长顿帧/慢节奏。 */
        speedScale: Float = 1f,
    ) {
        val r = Random.Default
        val baseN = 12 + r.nextInt(0, 4)
        val n = (baseN * countMultiplier).toInt().coerceIn(8, 48)
        for (i in 0 until n) {
            if (out.size >= MAX_PARTICLES) return
            val ang = (i / n.toFloat()) * 6.2832f + r.nextFloat() * 0.55f
            val sp = (160f + r.nextFloat() * 220f) * speedScale
            val lifeB = (0.22f + r.nextFloat() * 0.1f) * (1f + (1f - speedScale).coerceIn(0f, 1f) * 0.45f)
            out += Particle(
                x = centerX,
                y = centerY,
                vx = cos(ang) * sp,
                vy = sin(ang) * sp * 0.72f - 100f * speedScale,
                life = lifeB,
                maxLife = lifeB,
                color = when (i % 4) {
                    0 -> Color(0xFFE1F5FE)
                    1 -> Color(0xFF81D4FA)
                    2 -> Color(0xFFB3E5FC)
                    else -> Color(0xFFFFFFFF).copy(alpha = 0.85f)
                },
                baseRadius = 2.0f + r.nextFloat() * 2.8f,
                style = ParticleStyle.Stomp,
            )
        }
    }

    /**
     * Boss 落地冲击波在地面扫开的冰晶碎屑，沿前缘 [centerX] ± [halfWidth] 排布。
     */
    fun bossImpactWaveRim(
        out: MutableList<Particle>,
        centerX: Float,
        surfaceY: Float,
        halfWidth: Float,
    ) {
        if (out.size >= MAX_PARTICLES) return
        val r = Random.Default
        val n = 12
        for (i in 0 until n) {
            if (out.size >= MAX_PARTICLES) return
            val t = (i / (n - 1).coerceAtLeast(1).toFloat() - 0.5f) * 1.1f
            val x = centerX + t * halfWidth
            val life = 0.28f + r.nextFloat() * 0.1f
            out += Particle(
                x = x,
                y = surfaceY,
                vx = t * 120f,
                vy = -80f - r.nextFloat() * 160f,
                life = life,
                maxLife = life,
                color = when (i % 3) {
                    0 -> Color(0xFFB3E5FC)
                    1 -> Color(0xFF81D4FA)
                    else -> Color(0xFFE0F7FA)
                },
                baseRadius = 1.5f + r.nextFloat() * 2.2f,
                style = ParticleStyle.BossWave,
            )
        }
    }

    /**
     * 鱼干冲刺「残影」：由主循环按帧采样，[updateDashPhantomsInPlace] 推进寿命；
     * 绘制时用 [com.example.mygame.game.drawGuguCharacterSprite] 叠半透明。
     */
    fun trySpawnDashPhantom(
        out: MutableList<DashPhantom>,
        worldX: Float,
        worldY: Float,
        destSize: Float,
        facingRight: Boolean,
        animTick: Int,
        isMoving: Boolean,
        everyNthFrame: Int = 4,
    ) {
        if (everyNthFrame <= 0 || animTick % everyNthFrame != 0) return
        out += DashPhantom(
            worldX = worldX,
            worldY = worldY,
            destSize = destSize,
            facingRight = facingRight,
            animTick = animTick,
            isMoving = isMoving,
            life = DASH_PHANTOM_LIFE_S,
            maxLife = DASH_PHANTOM_LIFE_S,
        )
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

/** 鱼干冲刺拖尾残影寿命（秒），alpha 从约 0.5 衰减到 0。 */
const val DASH_PHANTOM_LIFE_S = 0.3f

data class DashPhantom(
    val worldX: Float,
    val worldY: Float,
    val destSize: Float,
    val facingRight: Boolean,
    val animTick: Int,
    val isMoving: Boolean,
    var life: Float,
    val maxLife: Float = DASH_PHANTOM_LIFE_S,
)

fun updateDashPhantomsInPlace(phantoms: MutableList<DashPhantom>, frameSeconds: Float) {
    if (phantoms.isEmpty()) return
    val dt = frameSeconds
    for (i in phantoms.lastIndex downTo 0) {
        phantoms[i].life -= dt
        if (phantoms[i].life <= 0f) phantoms.removeAt(i)
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
            ParticleStyle.HardLandingDust -> {
                p.x += p.vx * dt
                p.y += p.vy * dt
                p.vy += 380f * dt
                p.vx *= 0.92f
                p.life -= dt
            }
            ParticleStyle.DashPierce -> {
                p.x += p.vx * dt
                p.y += p.vy * dt
                p.vy += 100f * dt
                p.vx *= 0.985f
                p.life -= dt
            }
            ParticleStyle.FragileIce -> {
                p.x += p.vx * dt
                p.y += p.vy * dt
                p.vy += 280f * dt
                p.vx *= 0.96f
                p.life -= dt
            }
            ParticleStyle.BossWave -> {
                p.x += p.vx * dt
                p.y += p.vy * dt
                p.vy += 200f * dt
                p.vx *= 0.94f
                p.life -= dt
            }
        }
        if (p.life <= 0f) {
            particles.removeAt(i)
        }
    }
}
