package com.example.mygame.game.level

import com.example.mygame.game.Block
import com.example.mygame.game.BlockReward
import com.example.mygame.game.BlockType
import com.example.mygame.game.Coin
import com.example.mygame.game.CoinKind
import com.example.mygame.game.Enemy
import com.example.mygame.game.EnemyKind
import com.example.mygame.game.Pit
import com.example.mygame.game.Platform
import com.example.mygame.game.modes.EndlessBalanceConfig
import kotlin.math.min
import kotlin.random.Random

/**
 * 片段池 + 难度曲线 + 安全奖励段插入。
 */
object EndlessSegmentPool {

    fun roll(
        worldWidth: Float,
        worldHeight: Float,
        runSeconds: Float,
        random: Random,
        playerX: Float,
        lastRewardEndX: Float,
        /** 数值越小，越容易插入 [EndlessSegmentKind.RewardSafe]（补给休整）段。 */
        rewardSpacingWidthMultiplier: Float = EndlessBalanceConfig.rewardSpacingWorldWidthMultiplier,
    ): SegmentGeometry {
        val tier = when {
            runSeconds < EndlessBalanceConfig.tier0EndsAtSeconds -> 0
            runSeconds < EndlessBalanceConfig.tier1EndsAtSeconds -> 1
            else -> 2
        }
        val needReward = playerX - lastRewardEndX > worldWidth * rewardSpacingWidthMultiplier
        if (needReward) {
            return buildRewardSafe(worldWidth, worldHeight, random)
        }
        val pick = random.nextInt(100)
        val t0 = EndlessBalanceConfig.Tier0Roll
        val t1 = EndlessBalanceConfig.Tier1Roll
        val t2 = EndlessBalanceConfig.Tier2Roll
        return when (tier) {
            0 -> when {
                pick < t0.flatChaseWideUntil -> buildFlatChase(worldWidth, worldHeight, random, wide = true)
                pick < t0.pitJumpNarrowUntil -> buildPitJump(worldWidth, worldHeight, narrow = true)
                else -> buildFlatChase(worldWidth, worldHeight, random, wide = false)
            }
            1 -> when {
                pick < t1.pitJumpUntil -> buildPitJump(worldWidth, worldHeight, narrow = false)
                pick < t1.thinIceUntil -> buildThinIce(worldWidth, worldHeight)
                pick < t1.blizzardUntil -> buildBlizzard(worldWidth, worldHeight, random, dense = false)
                pick < t1.dangerUntil -> buildDanger(worldWidth, worldHeight, random, hard = false)
                else -> buildFlatChase(worldWidth, worldHeight, random, wide = true)
            }
            else -> when {
                pick < t2.pitJumpUntil -> buildPitJump(worldWidth, worldHeight, narrow = false)
                pick < t2.branchUntil -> buildBranchChoice(worldWidth, worldHeight, random)
                pick < t2.blizzardUntil -> buildBlizzard(worldWidth, worldHeight, random, dense = true)
                pick < t2.dangerUntil -> buildDanger(worldWidth, worldHeight, random, hard = true)
                pick < t2.thinIceUntil -> buildThinIce(worldWidth, worldHeight)
                else -> buildRewardSafe(worldWidth, worldHeight, random)
            }
        }
    }

    private fun groundY(h: Float) = h * 0.82f
    private fun tile(w: Float) = w * 0.12f
    private fun hero(w: Float, h: Float) = min(w, h) * 0.1f

    private fun buildFlatChase(w: Float, h: Float, r: Random, wide: Boolean): SegmentGeometry {
        val gy = groundY(h)
        val t = tile(w)
        val he = hero(w, h)
        val width = w * if (wide) 1.05f else 0.88f
        val coins = List(2 + r.nextInt(2)) { i ->
            Coin(
                x = t * (1.2f + i * 1.1f),
                y = gy - h * 0.14f,
                size = he * 0.4f,
                kind = CoinKind.Normal,
            )
        }
        return SegmentGeometry(
            width = width,
            kind = EndlessSegmentKind.FlatChase,
            pits = emptyList(),
            platforms = emptyList(),
            enemies = emptyList(),
            coins = coins,
            blocks = emptyList(),
            speedMultiplier = 1f,
            blizzardIntensity = 0f,
        )
    }

    private fun buildPitJump(w: Float, h: Float, narrow: Boolean): SegmentGeometry {
        val gy = groundY(h)
        val t = tile(w)
        val he = hero(w, h)
        val width = w * if (narrow) 0.92f else 1.08f
        val pitStart = t * (if (narrow) 4.2f else 3.8f)
        val pitEnd = pitStart + t * (if (narrow) 0.65f else 0.95f)
        val pits = listOf(Pit(startX = pitStart, endX = pitEnd))
        val platforms = listOf(
            Platform(x = t * 2.1f, y = gy - h * 0.2f, width = t * 1.2f, height = 24f, isFragile = true),
            Platform(x = pitEnd + t * 0.35f, y = gy - h * 0.24f, width = t * 1.35f, height = 24f, isFragile = true),
        )
        val coins = listOf(
            Coin(x = t * 1.4f, y = gy - h * 0.35f, size = he * 0.4f),
            Coin(x = pitEnd + t * 0.9f, y = gy - h * 0.32f, size = he * 0.4f),
        )
        return SegmentGeometry(
            width = width,
            kind = EndlessSegmentKind.PitJump,
            pits = pits,
            platforms = platforms,
            enemies = emptyList(),
            coins = coins,
            blocks = emptyList(),
        )
    }

    private fun buildThinIce(w: Float, h: Float): SegmentGeometry {
        val gy = groundY(h)
        val t = tile(w)
        val he = hero(w, h)
        val width = w * 1.02f
        val shieldSeal =
            Enemy(
                x = t * 4.6f,
                y = gy - he * 0.72f,
                width = he * 0.78f,
                height = he * 0.72f,
                patrolStart = t * 3.9f,
                patrolEnd = t * 6.8f,
                speed = 108f,
                kind = EnemyKind.Seal,
                hasIceShield = true,
            )
        return SegmentGeometry(
            width = width,
            kind = EndlessSegmentKind.ThinIceGlide,
            pits = emptyList(),
            platforms = listOf(
                Platform(x = t * 3.5f, y = gy - h * 0.18f, width = t * 1.4f, height = 22f, isFragile = true),
                Platform(x = t * 6.2f, y = gy - h * 0.28f, width = t * 1.2f, height = 22f, isFragile = true),
            ),
            enemies = listOf(shieldSeal),
            coins = listOf(
                Coin(x = t * 2f, y = gy - h * 0.12f, size = he * 0.4f),
                Coin(x = t * 5f, y = gy - h * 0.38f, size = he * 0.4f, kind = CoinKind.Beacon),
            ),
            blocks = emptyList(),
            speedMultiplier = EndlessBalanceConfig.thinIceSpeedMultiplier,
        )
    }

    private fun buildBlizzard(w: Float, h: Float, r: Random, dense: Boolean): SegmentGeometry {
        val gy = groundY(h)
        val t = tile(w)
        val he = hero(w, h)
        val width = w * (if (dense) 1.12f else 0.98f)
        val seal = Enemy(
            x = t * 4f,
            y = gy - he * 0.72f,
            width = he * 0.78f,
            height = he * 0.72f,
            patrolStart = t * 3f,
            patrolEnd = t * (if (dense) 7.5f else 6.2f),
            speed = if (dense) 128f else 108f,
            kind = EnemyKind.Seal,
        )
        val bird = if (dense && r.nextBoolean()) Enemy(
            x = t * 6f,
            y = gy - he * 1.3f,
            width = he * 0.68f,
            height = he * 0.52f,
            patrolStart = t * 5f,
            patrolEnd = t * 8f,
            speed = 120f,
            kind = EnemyKind.Bird,
        ) else null
        return SegmentGeometry(
            width = width,
            kind = EndlessSegmentKind.BlizzardLowVis,
            pits = emptyList(),
            platforms = listOf(Platform(x = t * 2.4f, y = gy - h * 0.22f, width = t * 1.1f, height = 24f)),
            enemies = listOfNotNull(seal, bird),
            coins = listOf(Coin(x = t * 1.5f, y = gy - h * 0.15f, size = he * 0.38f)),
            blocks = emptyList(),
            blizzardIntensity = if (dense) {
                EndlessBalanceConfig.blizzardIntensityDense
            } else {
                EndlessBalanceConfig.blizzardIntensityLight
            },
        )
    }

    private fun buildDanger(w: Float, h: Float, r: Random, hard: Boolean): SegmentGeometry {
        val gy = groundY(h)
        val t = tile(w)
        val he = hero(w, h)
        val width = w * (if (hard) 1.15f else 1f)
        val pitW = if (hard) 0.55f else 0.42f
        val pitStart = t * 5f
        val pitEnd = pitStart + t * pitW
        val enemies = mutableListOf(
            Enemy(
                x = t * 3.2f,
                y = gy - he * 0.72f,
                width = he * 0.78f,
                height = he * 0.72f,
                patrolStart = t * 2.4f,
                patrolEnd = t * 4.6f,
                speed = if (hard) 125f else 112f,
                kind = EnemyKind.Seal,
                hasIceShield = hard && r.nextBoolean(),
            ),
            Enemy(
                x = t * 7f,
                y = gy - he * 1.25f,
                width = he * 0.7f,
                height = he * 0.54f,
                patrolStart = pitEnd + t * 0.2f,
                patrolEnd = pitEnd + t * 3.2f,
                speed = 130f,
                kind = EnemyKind.Bird,
            ),
        )
        if (hard && r.nextBoolean()) {
            enemies += Enemy(
                x = pitEnd + t * 1.8f,
                y = gy - he * 0.72f,
                width = he * 0.78f,
                height = he * 0.72f,
                patrolStart = pitEnd + t * 0.5f,
                patrolEnd = pitEnd + t * 3.5f,
                speed = 118f,
                kind = EnemyKind.Seal,
            )
        }
        return SegmentGeometry(
            width = width,
            kind = EndlessSegmentKind.DangerMixed,
            pits = listOf(Pit(startX = pitStart, endX = pitEnd)),
            platforms = listOf(
                Platform(x = t * 1.8f, y = gy - h * 0.2f, width = t * 1.1f, height = 24f),
                Platform(x = pitEnd + t * 0.15f, y = gy - h * 0.26f, width = t * 1.25f, height = 24f),
            ),
            enemies = enemies,
            coins = listOf(
                Coin(x = t * 1.2f, y = gy - h * 0.12f, size = he * 0.4f),
                Coin(x = pitEnd + t * 1.2f, y = gy - h * 0.34f, size = he * 0.36f, kind = CoinKind.LorePage),
            ),
            blocks = emptyList(),
        )
    }

    private fun buildBranchChoice(w: Float, h: Float, r: Random): SegmentGeometry {
        val gy = groundY(h)
        val t = tile(w)
        val he = hero(w, h)
        val width = w * 1.18f
        val pitStart = t * 4f
        val pitEnd = pitStart + t * 1.05f
        val highRoute = r.nextBoolean()
        val platforms = if (highRoute) listOf(
            Platform(x = t * 2f, y = gy - h * 0.38f, width = t * 1f, height = 22f),
            Platform(x = t * 3.3f, y = gy - h * 0.52f, width = t * 1.1f, height = 22f),
            Platform(x = pitEnd + t * 0.25f, y = gy - h * 0.22f, width = t * 1.4f, height = 24f),
        ) else listOf(
            Platform(x = t * 2.2f, y = gy - h * 0.2f, width = t * 1.3f, height = 24f),
            Platform(x = pitEnd + t * 0.2f, y = gy - h * 0.3f, width = t * 1.2f, height = 24f),
            Platform(x = pitEnd + t * 2.2f, y = gy - h * 0.18f, width = t * 1.5f, height = 24f),
        )
        val bonusCoin = Coin(
            x = pitEnd + t * (if (highRoute) 0.8f else 2.4f),
            y = gy - h * (if (highRoute) 0.45f else 0.28f),
            size = he * 0.45f,
            kind = CoinKind.Beacon,
        )
        return SegmentGeometry(
            width = width,
            kind = EndlessSegmentKind.BranchChoice,
            pits = listOf(Pit(startX = pitStart, endX = pitEnd)),
            platforms = platforms,
            enemies = listOf(
                Enemy(
                    x = t * 3.5f,
                    y = gy - he * 0.72f,
                    width = he * 0.78f,
                    height = he * 0.72f,
                    patrolStart = t * 2.8f,
                    patrolEnd = pitStart - t * 0.2f,
                    speed = 122f,
                    kind = EnemyKind.Seal,
                ),
            ),
            coins = listOf(
                Coin(x = t * 1.4f, y = gy - h * 0.14f, size = he * 0.38f),
                bonusCoin,
            ),
            blocks = emptyList(),
            speedMultiplier = 1f,
        )
    }

    private fun buildRewardSafe(w: Float, h: Float, r: Random): SegmentGeometry {
        val gy = groundY(h)
        val t = tile(w)
        val he = hero(w, h)
        val width = w * 0.95f
        val blockSize = t * 0.46f
        val rowY = gy - h * 0.26f
        return SegmentGeometry(
            width = width,
            kind = EndlessSegmentKind.RewardSafe,
            pits = emptyList(),
            platforms = listOf(
                Platform(x = t * 2.5f, y = gy - h * 0.2f, width = t * 1.5f, height = 24f),
                Platform(x = t * 5.5f, y = gy - h * 0.32f, width = t * 1.6f, height = 24f),
            ),
            enemies = emptyList(),
            coins = listOf(
                Coin(x = t * 1.2f, y = gy - h * 0.12f, size = he * 0.42f),
                Coin(x = t * 3.8f, y = gy - h * 0.4f, size = he * 0.4f, kind = CoinKind.Beacon),
                Coin(x = t * 6.2f, y = gy - h * 0.18f, size = he * 0.38f),
            ),
            blocks = listOf(
                Block(x = t * 4.2f, y = rowY, size = blockSize, type = BlockType.Question, reward = BlockReward.Fish),
                Block(x = t * 4.75f, y = rowY, size = blockSize, type = BlockType.Brick),
                Block(x = t * 5.3f, y = rowY, size = blockSize, type = BlockType.Question, reward = BlockReward.Magnet),
            ),
            speedMultiplier = 1f,
            blizzardIntensity = 0f,
        )
    }
}
