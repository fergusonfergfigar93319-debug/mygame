package com.example.mygame.engine

import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.flow.asStateFlow
enum class RunnerPlayerState {
    Running, Jumping, Sliding, LaneChanging, Dead
}

enum class Lane(val index: Int, val xOffset: Float) {
    Left(0, -3f),
    Center(1, 0f),
    Right(2, 3f);

    fun moveLeft(): Lane = when (this) {
        Left -> Left
        Center -> Left
        Right -> Center
    }

    fun moveRight(): Lane = when (this) {
        Left -> Center
        Center -> Right
        Right -> Right
    }
}

data class RunnerSnapshot(
    val distance: Float = 0f,
    val score: Int = 0,
    val coins: Int = 0,
    val fishSnacks: Int = 0,
    val beacons: Int = 0,
    val lorePages: Int = 0,
    val multiplier: Float = 1f,
    val survivalSeconds: Float = 0f,
    val distanceScore: Int = 0,
    val collectionScore: Int = 0,
    val actionScore: Int = 0,
    val comboCount: Int = 0,
    val nearMissCount: Int = 0,
    val runSpeed: Float = 8f,
    val speedPhase: Int = 0,        // 0=热身 … 6=极速
    val isRunning: Boolean = false,
    val isPaused: Boolean = false,
    val isGameOver: Boolean = false,
    val playerState: RunnerPlayerState = RunnerPlayerState.Running,
    val currentLane: Lane = Lane.Center,
    val fishDashActive: Boolean = false,
    val hasBubbleScarf: Boolean = false,
    val snowShieldActive: Boolean = false,
    val gustBootsActive: Boolean = false,
    val magnetActive: Boolean = false,
    val slideFlowActive: Boolean = false,
    val assistReady: Boolean = false,
    val assistTimer: Float = 0f,
)

class RunnerGameState {
    private val _snapshot = MutableStateFlow(RunnerSnapshot())
    val snapshot: StateFlow<RunnerSnapshot> = _snapshot.asStateFlow()

    private val _gameOverEvent = MutableStateFlow(0)
    val gameOverEvent: StateFlow<Int> = _gameOverEvent.asStateFlow()

    private val _powerUpEvent = MutableStateFlow<Pair<Int, PowerUpType?>>(0 to null)
    val powerUpEvent: StateFlow<Pair<Int, PowerUpType?>> = _powerUpEvent.asStateFlow()

    // milestone: (version, distance in meters)
    private val _milestoneEvent = MutableStateFlow<Pair<Int, Int>>(0 to 0)
    val milestoneEvent: StateFlow<Pair<Int, Int>> = _milestoneEvent.asStateFlow()

    // speedPhaseEvent: (version, phaseIndex) — fires when player enters a new speed phase
    private val _speedPhaseEvent = MutableStateFlow<Pair<Int, Int>>(0 to 0)
    val speedPhaseEvent: StateFlow<Pair<Int, Int>> = _speedPhaseEvent.asStateFlow()

    fun update(block: RunnerSnapshot.() -> RunnerSnapshot) {
        _snapshot.value = _snapshot.value.block()
    }

    fun emitGameOver() { _gameOverEvent.value++ }

    fun emitPowerUp(type: PowerUpType) {
        _powerUpEvent.value = (_powerUpEvent.value.first + 1) to type
    }

    fun emitMilestone(meters: Int) {
        _milestoneEvent.value = (_milestoneEvent.value.first + 1) to meters
    }

    fun emitSpeedPhase(phase: Int) {
        _speedPhaseEvent.value = (_speedPhaseEvent.value.first + 1) to phase
    }

    fun reset() {
        _snapshot.value = RunnerSnapshot()
        _powerUpEvent.value = 0 to null
        _milestoneEvent.value = 0 to 0
        _speedPhaseEvent.value = 0 to 0
    }
}
