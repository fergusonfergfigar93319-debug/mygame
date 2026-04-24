package com.example.mygame.game

data class Platform(
    val x: Float,
    val y: Float,
    val width: Float,
    val height: Float
)

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
    Bird
}

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
    Scarf
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
