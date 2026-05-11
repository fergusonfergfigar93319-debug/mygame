package com.example.mygame.game.boss

import com.example.mygame.game.BossArenaSpec
import com.example.mygame.game.BossState
import org.junit.Assert.assertEquals
import org.junit.Assert.assertFalse
import org.junit.Assert.assertTrue
import org.junit.Test

class TakamatsuBossControllerTest {

    @Test
    fun enragedStompDamagesGroundedBoss() {
        val controller = startedEnragedBoss(hp = 44f)

        val damaged = controller.notifyEnragedStomp()

        assertTrue(damaged)
        assertEquals(22f, controller.entity.hp, 0.001f)
        assertEquals(BossState.ENRAGED, controller.entity.state)
    }

    @Test
    fun enragedStompIsIgnoredWhileBossIsAirborne() {
        val controller = startedEnragedBoss(hp = 44f)
        controller.entity.y -= HERO_SIZE

        val damaged = controller.notifyEnragedStomp()

        assertFalse(damaged)
        assertEquals(44f, controller.entity.hp, 0.001f)
        assertEquals(BossState.ENRAGED, controller.entity.state)
    }

    @Test
    fun enragedStompCanFinishBossAndEmitDefeatEvent() {
        val controller = startedEnragedBoss(hp = 20f)

        val damaged = controller.notifyEnragedStomp()
        val earlyEvents = controller.tick(1.0f)
        val defeatEvents = controller.tick(0.3f)

        assertTrue(damaged)
        assertEquals(0f, controller.entity.hp, 0.001f)
        assertEquals(BossState.DYING, controller.entity.state)
        assertTrue(earlyEvents.none { it == BossTickEvent.BossDefeated })
        assertTrue(defeatEvents.any { it == BossTickEvent.BossDefeated })
    }

    private fun startedEnragedBoss(hp: Float): TakamatsuBossController {
        val controller = TakamatsuBossController()
        controller.start(
            arena = BossArenaSpec(triggerAtPlayerX = 0f, arenaCenterWorldX = 400f),
            groundY = GROUND_Y,
            hero = HERO_SIZE,
        )
        controller.entity.state = BossState.ENRAGED
        controller.entity.stateTime = 0f
        controller.entity.hp = hp
        controller.entity.hasShield = false
        controller.entity.y = GROUND_Y - controller.entity.height
        return controller
    }

    private companion object {
        const val GROUND_Y = 300f
        const val HERO_SIZE = 48f
    }
}
