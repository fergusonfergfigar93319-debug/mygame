package com.example.mygame.game

data class Platform(
    val x: Float,
    val y: Float,
    val width: Float,
    val height: Float,
    /**
     * 地表摩擦系数：1 = 普通地面；数值越小越滑（冰面常用约 0.18–0.3）。
     * 仅作用于脚底站稳、无水平输入时的速度衰减。
     */
    val surfaceFriction: Float = 1f,
)

/** 结合地面 [surfaceFriction] 得到本帧水平阻尼系数（无输入时 v 乘该值）。 */
fun horizontalGroundDampening(baseFriction: Float, surfaceFriction: Float): Float {
    val s = surfaceFriction.coerceIn(0.05f, 1f)
    return 1f - (1f - baseFriction) * s
}

/**
 * 根据当前脚底接触面取摩擦系数；多面重叠时取最小值（最滑者生效）。
 */
fun standingSurfaceFriction(
    onGround: Boolean,
    playerX: Float,
    playerY: Float,
    playerSize: Float,
    groundY: Float,
    overPit: Boolean,
    platforms: List<Platform>,
    blocks: List<Block>,
): Float {
    if (!onGround) return 1f
    val bottom = playerY + playerSize
    val left = playerX
    val right = playerX + playerSize
    val eps = 8f
    val matches = ArrayList<Float>(4)
    if (!overPit && bottom >= groundY - eps && bottom <= groundY + eps * 2f) {
        matches += 1f
    }
    for (p in platforms) {
        if (right > p.x && left < p.x + p.width && bottom >= p.y - eps && bottom <= p.y + eps * 2f) {
            matches += p.surfaceFriction
        }
    }
    for (b in blocks) {
        val top = b.y - b.bounceOffset
        if (right > b.x && left < b.x + b.size && bottom >= top - eps && bottom <= top + eps * 2f) {
            matches += 1f
        }
    }
    return matches.minOrNull() ?: 1f
}

data class Pit(
    val startX: Float,
    val endX: Float
)

data class Enemy(
    val x: Float,
    val y: Float,
    val width: Float,
    val height: Float,
    val patrolStart: Float,
    val patrolEnd: Float,
    val speed: Float,
    val kind: EnemyKind = EnemyKind.Seal,
    val direction: Float = 1f
)

enum class EnemyKind {
    Seal,
    Bird,
    SpikedSeal,
    Owl,
}

fun EnemyKind.canBeStomped(): Boolean = this != EnemyKind.SpikedSeal

enum class CoinKind {
    Normal,
    Beacon,
    LorePage,
}

data class Coin(
    val x: Float,
    val y: Float,
    val size: Float,
    val kind: CoinKind = CoinKind.Normal,
)

enum class BlockType {
    Brick,
    Question
}

enum class BlockReward {
    Coin,
    Fish,
    Scarf,
    Shield,
    Boots,
}

data class Block(
    val x: Float,
    val y: Float,
    val size: Float,
    val type: BlockType,
    val reward: BlockReward = BlockReward.Coin,
    val used: Boolean = false,
    val bounceOffset: Float = 0f
)

data class FloatingCoin(
    val x: Float,
    val y: Float,
    val size: Float,
    val velocityY: Float,
    val life: Float
)

data class FishSnack(
    val x: Float,
    val y: Float,
    val size: Float,
    val velocityX: Float,
    val velocityY: Float,
    val emerging: Boolean = true,
    val progress: Float = 0f
)

data class FriendGoal(
    val x: Float,
    val groundY: Float,
    val height: Float
)
