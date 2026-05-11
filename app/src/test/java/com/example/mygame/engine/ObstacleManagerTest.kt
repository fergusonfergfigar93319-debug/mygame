package com.example.mygame.engine

import org.junit.Assert.assertEquals
import org.junit.Assert.assertFalse
import org.junit.Assert.assertTrue
import org.junit.Test

class ObstacleManagerTest {
    @Test
    fun groundedPlayerHitsSealInSameLane() {
        val manager = ObstacleManager()
        manager.addObstacle(Obstacle(type = ObstacleType.Seal, lane = Lane.Center, z = 0f))

        val result = manager.checkCollision(
            playerX = Lane.Center.xOffset,
            playerY = 0.7f,
            playerZ = 0f,
            isSliding = false,
            isJumping = false,
        )

        assertTrue(result.hit)
        assertEquals(ObstacleType.Seal, result.type)
    }

    @Test
    fun jumpingPlayerClearsSealInSameLane() {
        val manager = ObstacleManager()
        manager.addObstacle(Obstacle(type = ObstacleType.Seal, lane = Lane.Center, z = 0f))

        val result = manager.checkCollision(
            playerX = Lane.Center.xOffset,
            playerY = 2f,
            playerZ = 0f,
            isSliding = false,
            isJumping = true,
        )

        assertFalse(result.hit)
    }

    @Suppress("UNCHECKED_CAST")
    private fun ObstacleManager.addObstacle(obstacle: Obstacle) {
        val field = ObstacleManager::class.java.getDeclaredField("obstacles")
        field.isAccessible = true
        (field.get(this) as MutableList<Obstacle>).add(obstacle)
    }
}
