package com.example.mygame.game.boss

import com.example.mygame.game.BOSS_DAMAGE_FLASH_MAX_S
import com.example.mygame.game.BossArenaSpec
import com.example.mygame.game.BossEntity
import com.example.mygame.game.BossState
import com.example.mygame.game.EnemyKind
import com.example.mygame.game.ImpactWave
import com.example.mygame.game.StompFeel
import kotlin.math.max
import kotlin.math.sin

sealed class BossTickEvent {
    /** 以 [ImpactWave.centerX] 为轴心、半宽外扩的地面冲击。 */
    data class GroundImpact(val wave: ImpactWave) : BossTickEvent()

    /**
     * 顿帧 + 震幅 + 震屏轴偏好；[playBossSlamSfx] 为真时主循环发 `playBossLand`。
     */
    data class HeavyStompFeel(
        val hitStop: Float = StompFeel.HIT_STOP_S,
        val shake: Float = StompFeel.SHAKE_MAX_PX * 0.8f,
        val shakeBias: StompFeel.ShakeBias = StompFeel.ShakeBias.None,
        val playBossSlamSfx: Boolean = false,
    ) : BossTickEvent()

    data class RequestSpawnMinion(
        val x: Float,
        val y: Float,
        val width: Float,
        val height: Float,
        val kind: EnemyKind = EnemyKind.Seal,
    ) : BossTickEvent()

    class MarkPlatformsFragile(val enabled: Boolean) : BossTickEvent()

    data object BossDefeated : BossTickEvent()
}

/**
 * 高松鹅三阶段试炼：P1 跳跃+冲击波 → P2 冰盾+小怪（破盾后虚弱）→ P3 全平台变脆弱+更快砸地。
 * 与 [com.example.mygame.game.GuguGagaGame] 的碰撞/踩踏分支协同。
 */
class TakamatsuBossController {

    lateinit var entity: BossEntity
        private set
    var running: Boolean = false
        private set
    var enrageApplied: Boolean = false
        private set

    val isEntityReady: Boolean get() = running && this::entity.isInitialized

    private var groundY: Float = 0f
    private var hero: Float = 0f
    /**
     * P1/狂暴跳跃：单次周期内已流逝时间。θ = π·(jumpT/T)，Y 偏移 = -JumpHeight·sin(θ)；
     * [jumpT] 归 0 于地面帧并与冲击波/顿帧/音效同发。
     */
    private var jumpT: Float = 0f
    private var spawnAcc: Float = 0f
    private var minionsSpawned: Int = 0
    private var pendingDefeated: Boolean = false
    private var centerX: Float = 0f

    fun reset() {
        running = false
        pendingDefeated = false
        enrageApplied = false
        minionsSpawned = 0
        jumpT = 0f
        spawnAcc = 0f
    }

    fun start(arena: BossArenaSpec, groundY: Float, hero: Float) {
        reset()
        this.groundY = groundY
        this.hero = hero
        this.centerX = arena.arenaCenterWorldX
        val w = hero * 1.35f
        val h = hero * 1.15f
        entity = BossEntity(
            x = centerX - w * 0.5f,
            y = groundY - h,
            width = w,
            height = h,
        )
        entity.state = BossState.INTRO
        entity.stateTime = 0f
        running = true
    }

    fun notifyShieldBroken() {
        if (!running) return
        if (entity.state != BossState.SHIELDED || !entity.hasShield) return
        entity.hasShield = false
        entity.state = BossState.STUNNED
        entity.stateTime = 0f
        entity.damageFlashTimer = BOSS_DAMAGE_FLASH_MAX_S
    }

    /** P1 起跳、双脚离地：不允许正常踩头，用冰盾格挡感弹开。 */
    fun isBossAirborneForStompIgnore(groundY: Float): Boolean =
        running && entity.state == BossState.JUMPING && (entity.y + entity.height) < groundY - hero * 0.2f

    /**
     * 可踩踏的 Boss 体（P2 带盾、P3 无盾的地面阶段、虚弱）。
     */
    fun canReceiveStomp(): Boolean {
        if (!running) return false
        return when (entity.state) {
            BossState.SHIELDED, BossState.STUNNED, BossState.ENRAGED -> true
            BossState.JUMPING -> (entity.y + entity.height) >= groundY - hero * 0.2f
            else -> false
        }
    }

    fun tick(dt: Float): List<BossTickEvent> {
        if (!running) return emptyList()
        val e = entity
        e.damageFlashTimer = kotlin.math.max(0f, e.damageFlashTimer - dt)
        e.stateTime += dt
        val out = ArrayList<BossTickEvent>()

        when (e.state) {
            BossState.INTRO -> {
                if (e.stateTime >= 1.05f) {
                    e.state = BossState.JUMPING
                    e.stateTime = 0f
                    jumpT = 0f
                }
            }

            BossState.JUMPING, BossState.ENRAGED -> {
                val enraged = e.state == BossState.ENRAGED
                val T = if (enraged) 1.05f else 1.5f
                val baseY = groundY - e.height
                val jumpHeight = hero * 1.1f
                val nextT = jumpT + dt
                if (nextT < T) {
                    jumpT = nextT
                    e.y = baseY - jumpHeight * sin(kotlin.math.PI * jumpT / T).toFloat()
                } else {
                    e.y = baseY
                    jumpT = 0f
                    out.addAll(makeBossSlamAtLanding())
                }
                if (!enraged && e.stateTime >= 6.8f) {
                    e.state = BossState.SHIELDED
                    e.stateTime = 0f
                    e.y = baseY
                    jumpT = 0f
                    e.hasShield = true
                    spawnAcc = 0f
                    minionsSpawned = 0
                }
            }

            BossState.SHIELDED -> {
                e.y = groundY - e.height
                spawnAcc += dt
                if (spawnAcc >= 2.1f && minionsSpawned < 2) {
                    spawnAcc = 0f
                    minionsSpawned += 1
                    val ex = e.x - hero * 0.8f * if (minionsSpawned == 1) 1f else -0.3f
                    out += BossTickEvent.RequestSpawnMinion(
                        ex.coerceIn(e.x - hero * 2.5f, e.x + e.width * 0.2f),
                        groundY - hero * 0.72f,
                        hero * 0.7f,
                        hero * 0.58f,
                    )
                }
            }

            BossState.STUNNED -> {
                e.y = groundY - e.height
                if (e.stateTime >= 2.1f) {
                    e.hp = max(0f, e.hp - 28f)
                    e.damageFlashTimer = BOSS_DAMAGE_FLASH_MAX_S
                    e.stateTime = 0f
                    if (e.hp <= 0f) {
                        e.state = BossState.DYING
                    } else if (!enrageApplied && e.hp < 64f) {
                        enrageApplied = true
                        e.state = BossState.ENRAGED
                        e.hasShield = false
                        jumpT = 0f
                        out += BossTickEvent.MarkPlatformsFragile(true)
                    } else {
                        e.state = BossState.JUMPING
                        jumpT = 0f
                    }
                }
            }

            BossState.DYING -> {
                e.y = groundY - e.height
                if (e.stateTime >= 1.25f) {
                    if (!pendingDefeated) {
                        pendingDefeated = true
                        out += BossTickEvent.BossDefeated
                    }
                }
            }
        }
        return out
    }

    private fun makeBossSlamAtLanding(): List<BossTickEvent> {
        val e = entity
        val life = 0.48f
        val wave = ImpactWave(
            centerX = e.x + e.width * 0.5f,
            halfWidth = hero * 0.38f,
            surfaceY = groundY,
            life = life,
            maxLife = life,
        )
        return listOf(
            BossTickEvent.GroundImpact(wave),
            BossTickEvent.HeavyStompFeel(
                hitStop = StompFeel.BOSS_LAND_HIT_STOP_S,
                shake = StompFeel.SHAKE_BOSS_LANDING_PX,
                shakeBias = StompFeel.ShakeBias.Vertical,
                playBossSlamSfx = true,
            ),
        )
    }
}
