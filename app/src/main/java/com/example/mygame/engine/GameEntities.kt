package com.example.mygame.engine

enum class ObstacleType(val needsJump: Boolean, val needsSlide: Boolean) {
    IceWall(false, false),       // blocks lane, must switch
    LowArch(false, true),       // must slide under
    Fence(true, false),          // must jump over
    Seal(false, false),          // blocks lane, can jump or switch
    Pit(true, false),            // gap in ground, must jump
}

data class Obstacle(
    val type: ObstacleType,
    val lane: Lane,
    val z: Float,
    val width: Float = 1.8f,
    val height: Float = 2.2f,
    val depth: Float = 1.0f,
    var active: Boolean = true,
)

data class CoinEntity(
    val lane: Lane,
    val z: Float,
    val y: Float = 0.8f,
    var collected: Boolean = false,
)

data class PowerUpBox(
    val lane: Lane,
    val z: Float,
    val type: PowerUpType,
    var collected: Boolean = false,
)

data class LaneSegment(
    val obstacles: List<Obstacle>,
    val coins: List<CoinEntity>,
    val startZ: Float,
    val length: Float,
)
