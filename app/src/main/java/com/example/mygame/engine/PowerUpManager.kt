package com.example.mygame.engine

import kotlin.math.max

enum class PowerUpType {
    FishDash,      // invincible dash through obstacles
    BubbleScarf,   // slow fall / glide
    SnowShield,    // survive one hit
    GustBoots,     // higher jump
    AuroraMagnet,  // attract coins
}

data class ActivePowerUp(
    val type: PowerUpType,
    var remaining: Float,
)

class PowerUpManager {

    private val active = mutableListOf<ActivePowerUp>()

    val isFishDashActive get() = active.any { it.type == PowerUpType.FishDash }
    val hasBubbleScarf get() = active.any { it.type == PowerUpType.BubbleScarf }
    val hasSnowShield get() = active.any { it.type == PowerUpType.SnowShield }
    val hasGustBoots get() = active.any { it.type == PowerUpType.GustBoots }
    val isMagnetActive get() = active.any { it.type == PowerUpType.AuroraMagnet }

    fun activate(type: PowerUpType) {
        val duration = when (type) {
            PowerUpType.FishDash -> 3f
            PowerUpType.BubbleScarf -> 5f
            PowerUpType.SnowShield -> 8f
            PowerUpType.GustBoots -> 6f
            PowerUpType.AuroraMagnet -> 5f
        }
        val existing = active.find { it.type == type }
        if (existing != null) {
            existing.remaining = max(existing.remaining, duration)
        } else {
            active.add(ActivePowerUp(type, duration))
        }
    }

    fun consumeShield(): Boolean {
        val shield = active.find { it.type == PowerUpType.SnowShield }
        if (shield != null) {
            active.remove(shield)
            return true
        }
        return false
    }

    fun update(dt: Float) {
        val iter = active.iterator()
        while (iter.hasNext()) {
            val p = iter.next()
            p.remaining -= dt
            if (p.remaining <= 0f) iter.remove()
        }
    }

    fun reset() = active.clear()
}
