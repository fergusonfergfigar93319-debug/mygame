package com.example.mygame.engine

import android.opengl.GLES20
import android.opengl.Matrix
import com.example.mygame.engine.models.LowPolyFactory
import kotlin.math.abs
import kotlin.math.sin
import kotlin.random.Random

class ObstacleManager {

    companion object {
        const val SPAWN_AHEAD = 120f
        const val DESPAWN_BEHIND = 20f
        const val NEAR_MISS_DIST = 1.2f  // within this Z distance counts as near-miss
    }

    // Predefined rhythm patterns: each entry is a list of (lane, type, relativeZ)
    private data class ObstacleSpec(val lane: Lane, val type: ObstacleType, val relZ: Float)
    private data class Pattern(val specs: List<ObstacleSpec>, val length: Float, val coins: List<Pair<Lane, Float>>)

    private val easyPatterns = buildEasyPatterns()
    private val mediumPatterns = buildMediumPatterns()
    private val hardPatterns = buildHardPatterns()

    private lateinit var iceWallMesh: Mesh
    private lateinit var lowArchMesh: Mesh
    private lateinit var fenceMesh: Mesh
    private lateinit var sealMesh: Mesh
    private lateinit var coinMesh: Mesh
    private lateinit var questionBlockMesh: Mesh

    private val obstacles = mutableListOf<Obstacle>()
    private val coins = mutableListOf<CoinEntity>()
    private val powerUpBoxes = mutableListOf<PowerUpBox>()

    private var nextSpawnZ = -20f
    private var difficulty = 0
    private var rng = Random(System.currentTimeMillis())

    private val modelMatrix = FloatArray(16)

    var coinsCollected = 0; private set
    var nearMissCount = 0; private set
    private var lastNearMissZ = Float.MAX_VALUE

    fun create() {
        iceWallMesh = LowPolyFactory.iceWall()
        lowArchMesh = LowPolyFactory.lowArch()
        fenceMesh = LowPolyFactory.fence()
        sealMesh = LowPolyFactory.seal()
        coinMesh = LowPolyFactory.coin()
        questionBlockMesh = LowPolyFactory.questionBlock()
    }

    fun reset() {
        obstacles.clear(); coins.clear(); powerUpBoxes.clear()
        nextSpawnZ = -20f; coinsCollected = 0; nearMissCount = 0
        lastNearMissZ = Float.MAX_VALUE
        rng = Random(System.currentTimeMillis())
    }

    fun update(playerZ: Float, elapsed: Float) {
        difficulty = when {
            elapsed < 20f -> 0
            elapsed < 60f -> 1
            elapsed < 120f -> 2
            else -> 3
        }
        while (nextSpawnZ > playerZ - SPAWN_AHEAD) {
            spawnPattern(nextSpawnZ)
        }
        obstacles.removeAll { it.z > playerZ + DESPAWN_BEHIND }
        coins.removeAll { it.collected || it.z > playerZ + DESPAWN_BEHIND }
        powerUpBoxes.removeAll { it.collected || it.z > playerZ + DESPAWN_BEHIND }
    }

    private fun spawnPattern(startZ: Float) {
        val pool = when (difficulty) {
            0 -> easyPatterns
            1 -> easyPatterns + mediumPatterns
            2 -> mediumPatterns + hardPatterns
            else -> hardPatterns
        }
        val pattern = pool.random(rng)

        for (spec in pattern.specs) {
            obstacles.add(Obstacle(type = spec.type, lane = spec.lane, z = startZ + spec.relZ))
        }
        for ((lane, relZ) in pattern.coins) {
            coins.add(CoinEntity(lane = lane, z = startZ + relZ))
        }

        // power-up box (rare, every ~8 patterns on average)
        if (rng.nextFloat() < 0.12f) {
            val lane = Lane.entries.random(rng)
            val pz = startZ - pattern.length * 0.5f
            powerUpBoxes.add(PowerUpBox(lane = lane, z = pz, type = PowerUpType.entries.random(rng)))
        }

        nextSpawnZ = startZ - pattern.length - rng.nextFloat() * 4f - 2f
    }

    // ── Pattern builders ──────────────────────────────────────────────────────

    private fun buildEasyPatterns(): List<Pattern> = listOf(
        // Single wall, coins on safe lanes
        Pattern(
            specs = listOf(ObstacleSpec(Lane.Center, ObstacleType.IceWall, -5f)),
            length = 18f,
            coins = coinLine(Lane.Left, -3f, 6) + coinLine(Lane.Right, -3f, 6),
        ),
        // Low arch in center, must slide
        Pattern(
            specs = listOf(ObstacleSpec(Lane.Center, ObstacleType.LowArch, -5f)),
            length = 18f,
            coins = coinLine(Lane.Center, -3f, 5),
        ),
        // Fence across all lanes, must jump
        Pattern(
            specs = listOf(
                ObstacleSpec(Lane.Left, ObstacleType.Fence, -5f),
                ObstacleSpec(Lane.Center, ObstacleType.Fence, -5f),
                ObstacleSpec(Lane.Right, ObstacleType.Fence, -5f),
            ),
            length = 20f,
            coins = coinArc(Lane.Center, -4f, 5),
        ),
        // Seal on left, coins on right
        Pattern(
            specs = listOf(ObstacleSpec(Lane.Left, ObstacleType.Seal, -6f)),
            length = 16f,
            coins = coinLine(Lane.Right, -4f, 6) + coinLine(Lane.Center, -4f, 4),
        ),
        // Two walls, one gap
        Pattern(
            specs = listOf(
                ObstacleSpec(Lane.Left, ObstacleType.IceWall, -5f),
                ObstacleSpec(Lane.Right, ObstacleType.IceWall, -5f),
            ),
            length = 18f,
            coins = coinLine(Lane.Center, -3f, 7),
        ),
    )

    private fun buildMediumPatterns(): List<Pattern> = listOf(
        // Staggered walls
        Pattern(
            specs = listOf(
                ObstacleSpec(Lane.Left, ObstacleType.IceWall, -4f),
                ObstacleSpec(Lane.Right, ObstacleType.IceWall, -10f),
            ),
            length = 22f,
            coins = coinLine(Lane.Center, -3f, 5) + coinLine(Lane.Right, -3f, 3),
        ),
        // Arch + wall combo
        Pattern(
            specs = listOf(
                ObstacleSpec(Lane.Center, ObstacleType.LowArch, -4f),
                ObstacleSpec(Lane.Left, ObstacleType.IceWall, -12f),
            ),
            length = 24f,
            coins = coinLine(Lane.Center, -2f, 4) + coinLine(Lane.Right, -10f, 4),
        ),
        // Seal patrol + fence
        Pattern(
            specs = listOf(
                ObstacleSpec(Lane.Center, ObstacleType.Seal, -5f),
                ObstacleSpec(Lane.Left, ObstacleType.Fence, -5f),
                ObstacleSpec(Lane.Right, ObstacleType.Fence, -5f),
            ),
            length = 22f,
            coins = coinArc(Lane.Center, -3f, 6),
        ),
        // Double arch
        Pattern(
            specs = listOf(
                ObstacleSpec(Lane.Left, ObstacleType.LowArch, -4f),
                ObstacleSpec(Lane.Right, ObstacleType.LowArch, -4f),
            ),
            length = 20f,
            coins = coinLine(Lane.Center, -2f, 8),
        ),
        // Zigzag walls
        Pattern(
            specs = listOf(
                ObstacleSpec(Lane.Left, ObstacleType.IceWall, -4f),
                ObstacleSpec(Lane.Center, ObstacleType.IceWall, -9f),
                ObstacleSpec(Lane.Right, ObstacleType.IceWall, -14f),
            ),
            length = 26f,
            coins = coinLine(Lane.Right, -2f, 3) + coinLine(Lane.Left, -7f, 3) + coinLine(Lane.Center, -12f, 3),
        ),
    )

    private fun buildHardPatterns(): List<Pattern> = listOf(
        // Triple threat: wall + arch + fence
        Pattern(
            specs = listOf(
                ObstacleSpec(Lane.Left, ObstacleType.IceWall, -4f),
                ObstacleSpec(Lane.Center, ObstacleType.LowArch, -4f),
                ObstacleSpec(Lane.Right, ObstacleType.Fence, -10f),
                ObstacleSpec(Lane.Left, ObstacleType.Fence, -16f),
            ),
            length = 28f,
            coins = coinLine(Lane.Right, -2f, 3) + coinLine(Lane.Center, -8f, 3),
        ),
        // Rapid stagger
        Pattern(
            specs = listOf(
                ObstacleSpec(Lane.Left, ObstacleType.IceWall, -3f),
                ObstacleSpec(Lane.Right, ObstacleType.IceWall, -7f),
                ObstacleSpec(Lane.Left, ObstacleType.IceWall, -11f),
                ObstacleSpec(Lane.Right, ObstacleType.IceWall, -15f),
            ),
            length = 26f,
            coins = coinLine(Lane.Center, -2f, 10),
        ),
        // Seal gauntlet
        Pattern(
            specs = listOf(
                ObstacleSpec(Lane.Left, ObstacleType.Seal, -4f),
                ObstacleSpec(Lane.Right, ObstacleType.Seal, -8f),
                ObstacleSpec(Lane.Center, ObstacleType.Seal, -12f),
                ObstacleSpec(Lane.Left, ObstacleType.IceWall, -16f),
            ),
            length = 28f,
            coins = coinLine(Lane.Center, -2f, 4) + coinLine(Lane.Right, -6f, 3),
        ),
        // Arch gauntlet
        Pattern(
            specs = listOf(
                ObstacleSpec(Lane.Left, ObstacleType.LowArch, -4f),
                ObstacleSpec(Lane.Center, ObstacleType.LowArch, -9f),
                ObstacleSpec(Lane.Right, ObstacleType.LowArch, -14f),
                ObstacleSpec(Lane.Left, ObstacleType.LowArch, -19f),
            ),
            length = 30f,
            coins = coinLine(Lane.Right, -2f, 3) + coinLine(Lane.Left, -7f, 3) + coinLine(Lane.Center, -12f, 3),
        ),
    )

    private fun coinLine(lane: Lane, startRelZ: Float, count: Int): List<Pair<Lane, Float>> =
        (0 until count).map { lane to startRelZ - it * 1.4f }

    private fun coinArc(lane: Lane, startRelZ: Float, count: Int): List<Pair<Lane, Float>> =
        (0 until count).map { i ->
            val yOffset = sin(i.toFloat() / count * Math.PI.toFloat()) * 1.2f
            lane to startRelZ - i * 1.4f
        }

    // ── Collision & collection ────────────────────────────────────────────────

    data class CollisionResult(val hit: Boolean, val type: ObstacleType? = null)

    fun checkCollision(
        playerX: Float, playerY: Float, playerZ: Float,
        isSliding: Boolean, isJumping: Boolean,
    ): CollisionResult {
        val playerHalfW = 0.4f
        val playerH = if (isSliding) 0.6f else 1.4f
        val playerBottom = playerY - 0.7f

        for (obs in obstacles) {
            if (!obs.active) continue
            val dz = abs(playerZ - obs.z)
            if (dz > obs.depth * 0.5f + 0.35f) continue
            val dx = abs(playerX - obs.lane.xOffset)
            if (dx > obs.width * 0.5f + playerHalfW) continue

            when (obs.type) {
                ObstacleType.Fence -> if (playerBottom < obs.height * 0.5f) return CollisionResult(true, obs.type)
                ObstacleType.LowArch -> if (!isSliding && playerBottom < 1f) return CollisionResult(true, obs.type)
                ObstacleType.Pit -> if (playerBottom <= 0.01f) return CollisionResult(true, obs.type)
                else -> return CollisionResult(true, obs.type)
            }
        }
        return CollisionResult(false)
    }

    fun checkNearMiss(playerX: Float, playerZ: Float): Boolean {
        if (abs(playerZ - lastNearMissZ) < 3f) return false
        for (obs in obstacles) {
            if (!obs.active) continue
            val dz = abs(playerZ - obs.z)
            if (dz > NEAR_MISS_DIST) continue
            val dx = abs(playerX - obs.lane.xOffset)
            // near miss: close in Z but safely past in X
            if (dx > 1.2f && dx < 3.5f) {
                nearMissCount++
                lastNearMissZ = playerZ
                return true
            }
        }
        return false
    }

    fun collectCoins(playerX: Float, playerZ: Float, magnetActive: Boolean): Int {
        var collected = 0
        val range = if (magnetActive) 4f else 1.5f
        for (coin in coins) {
            if (coin.collected) continue
            if (abs(playerZ - coin.z) > range) continue
            if (abs(playerX - coin.lane.xOffset) > range) continue
            coin.collected = true; collected++; coinsCollected++
        }
        return collected
    }

    fun collectPowerUps(playerX: Float, playerZ: Float): PowerUpType? {
        for (box in powerUpBoxes) {
            if (box.collected) continue
            if (abs(playerZ - box.z) > 1.5f) continue
            if (abs(playerX - box.lane.xOffset) > 1.5f) continue
            box.collected = true; return box.type
        }
        return null
    }

    // ── Rendering ─────────────────────────────────────────────────────────────

    fun render(shader: ShaderProgram, vpMatrix: FloatArray, elapsed: Float) {
        for (obs in obstacles) {
            if (!obs.active) continue
            Matrix.setIdentityM(modelMatrix, 0)
            Matrix.translateM(modelMatrix, 0, obs.lane.xOffset, 1f, obs.z)
            // seals bob up and down
            if (obs.type == ObstacleType.Seal) {
                val bob = sin(elapsed * 2.5f + obs.z * 0.3f) * 0.12f
                Matrix.translateM(modelMatrix, 0, 0f, bob, 0f)
            }
            val emissive = if (obs.type == ObstacleType.IceWall) 0.08f else 0f
            shader.setTransform(vpMatrix, modelMatrix, emissive)
            when (obs.type) {
                ObstacleType.IceWall -> iceWallMesh.draw(shader)
                ObstacleType.LowArch -> lowArchMesh.draw(shader)
                ObstacleType.Fence -> fenceMesh.draw(shader)
                ObstacleType.Seal -> sealMesh.draw(shader)
                ObstacleType.Pit -> {}
            }
        }

        val coinSpin = (elapsed * 180f) % 360f
        for (coin in coins) {
            if (coin.collected) continue
            Matrix.setIdentityM(modelMatrix, 0)
            Matrix.translateM(modelMatrix, 0, coin.lane.xOffset, coin.y, coin.z)
            Matrix.rotateM(modelMatrix, 0, coinSpin, 0f, 1f, 0f)
            shader.setTransform(vpMatrix, modelMatrix, 0.25f)
            coinMesh.draw(shader)
        }

        val boxBob = sin(elapsed * 2f) * 0.12f
        for (box in powerUpBoxes) {
            if (box.collected) continue
            Matrix.setIdentityM(modelMatrix, 0)
            Matrix.translateM(modelMatrix, 0, box.lane.xOffset, 0.8f + boxBob, box.z)
            Matrix.rotateM(modelMatrix, 0, elapsed * 60f % 360f, 0f, 1f, 0f)
            shader.setTransform(vpMatrix, modelMatrix, 0.4f)
            questionBlockMesh.draw(shader)
        }
    }
}
