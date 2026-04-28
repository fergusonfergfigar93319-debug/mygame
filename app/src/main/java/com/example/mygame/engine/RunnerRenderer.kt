package com.example.mygame.engine

import android.opengl.GLES20
import android.opengl.GLSurfaceView
import android.opengl.Matrix
import javax.microedition.khronos.egl.EGLConfig
import javax.microedition.khronos.opengles.GL10
import kotlin.math.min

class RunnerRenderer(
    private val gameState: RunnerGameState,
    private val onSoundEvent: ((SoundEvent) -> Unit)? = null,
) : GLSurfaceView.Renderer {

    enum class SoundEvent {
        Jump, Land, Slide, LaneSwitch, Die, CoinPickup, PowerUp, Stomp, NearMiss
    }

    private lateinit var shader: ShaderProgram
    private lateinit var camera: CameraController
    private lateinit var track: TrackRenderer
    private lateinit var player: PlayerController
    private lateinit var obstacleManager: ObstacleManager
    private lateinit var environment: EnvironmentRenderer
    private lateinit var background: BackgroundRenderer
    private lateinit var particles: ParticleSystem
    private val powerUpManager = PowerUpManager()

    private var runSpeed = 8f
    private val baseSpeed = 8f
    private val maxSpeed = 42f      // 极限速度，理论上可以无限接近但不超过

    // 速度分段：(持续秒数, 目标速度, 过渡曲线指数)
    // 指数 < 1 = 先快后慢；指数 > 1 = 先慢后快
    private data class SpeedPhase(val duration: Float, val targetSpeed: Float, val curve: Float)
    private val speedPhases = listOf(
        SpeedPhase(30f,  12f, 0.6f),   // 0–30s：热身，缓慢爬升到 12
        SpeedPhase(30f,  16f, 1.0f),   // 30–60s：正常加速到 16
        SpeedPhase(30f,  20f, 1.2f),   // 60–90s：开始发力到 20
        SpeedPhase(30f,  25f, 1.5f),   // 90–120s：明显加速到 25
        SpeedPhase(30f,  30f, 1.8f),   // 120–150s：高速冲刺到 30
        SpeedPhase(30f,  36f, 2.0f),   // 150–180s：极速到 36
        SpeedPhase(Float.MAX_VALUE, maxSpeed, 2.5f), // 180s+：趋近极限
    )

    private var runElapsed = 0f
    var isRunning = false; private set
    var isPaused = false; private set
    var isGameOver = false; private set

    private var lastFrameTime = 0L
    private var viewportW = 1; private var viewportH = 1

    // combo system
    private var comboCount = 0
    private var comboTimer = 0f
    private val comboTimeout = 3f

    // milestone tracking
    private var lastMilestone = 0
    private var lastSpeedPhase = -1

    // dust spawn timer
    private var dustTimer = 0f

    private val pendingActions = java.util.concurrent.ConcurrentLinkedQueue<Action>()

    enum class Action { Jump, Slide, MoveLeft, MoveRight, Start, Pause, Resume }

    fun enqueueAction(action: Action) = pendingActions.add(action)

    override fun onSurfaceCreated(gl: GL10?, config: EGLConfig?) {
        GLES20.glClearColor(0.06f, 0.08f, 0.18f, 1f)
        GLES20.glEnable(GLES20.GL_DEPTH_TEST)
        GLES20.glEnable(GLES20.GL_CULL_FACE)
        GLES20.glCullFace(GLES20.GL_BACK)

        shader = ShaderProgram(ShaderProgram.SCENE_VERTEX, ShaderProgram.SCENE_FRAGMENT)
        camera = CameraController()
        track = TrackRenderer(); track.create()
        player = PlayerController(); player.create()
        obstacleManager = ObstacleManager(); obstacleManager.create()
        environment = EnvironmentRenderer(); environment.create()
        background = BackgroundRenderer(); background.create()
        particles = ParticleSystem(); particles.create()
        lastFrameTime = System.nanoTime()
    }

    override fun onSurfaceChanged(gl: GL10?, width: Int, height: Int) {
        GLES20.glViewport(0, 0, width, height)
        viewportW = width; viewportH = height
        camera.setProjection(width, height)
        camera.viewportWidth = width; camera.viewportHeight = height
    }

    override fun onDrawFrame(gl: GL10?) {
        val now = System.nanoTime()
        val dt = min((now - lastFrameTime) / 1_000_000_000f, 0.05f)
        lastFrameTime = now

        processActions()

        // dynamic sky color based on speed
        val speedT = ((runSpeed - baseSpeed) / (maxSpeed - baseSpeed)).coerceIn(0f, 1f)
        val skyR = 0.06f + speedT * 0.04f
        val skyG = 0.08f - speedT * 0.02f
        val skyB = 0.18f + speedT * 0.08f
        GLES20.glClearColor(skyR, skyG, skyB, 1f)
        GLES20.glClear(GLES20.GL_COLOR_BUFFER_BIT or GLES20.GL_DEPTH_BUFFER_BIT)

        if (isRunning && !isPaused && !isGameOver) {
            runElapsed += dt
            runSpeed = calcSpeed(runElapsed)

            val animEvents = player.update(dt, runSpeed)
            track.update(player.z)
            obstacleManager.update(player.z, runElapsed)
            environment.update(player.z)
            background.update(player.z)
            powerUpManager.update(dt)

            // camera with lane lean and speed FOV
            camera.rebuildProjection(viewportW, viewportH)
            camera.update(player.x, player.y, player.z, dt, runSpeed, player.lastLaneDir)

            // particles: run dust
            dustTimer -= dt
            if (dustTimer <= 0f && player.onGround) {
                particles.spawnRunDust(player.x, player.getVisualY(), player.z)
                dustTimer = 0.08f
            }
            if (animEvents.landed) {
                particles.spawnLandDust(player.x, player.getVisualY(), player.z)
                onSoundEvent?.invoke(SoundEvent.Land)
                camera.shake(0.08f)
            }
            if (powerUpManager.isFishDashActive) {
                particles.spawnDashTrail(player.x, player.getVisualY(), player.z)
            }
            particles.update(dt)

            // collision
            if (!powerUpManager.isFishDashActive) {
                val col = obstacleManager.checkCollision(
                    player.x, player.y, player.z,
                    player.state == RunnerPlayerState.Sliding,
                    player.state == RunnerPlayerState.Jumping,
                )
                if (col.hit) {
                    if (powerUpManager.consumeShield()) {
                        camera.shake(0.2f); onSoundEvent?.invoke(SoundEvent.Stomp)
                    } else {
                        particles.spawnDeathBurst(player.x, player.y, player.z)
                        camera.shake(0.4f); endRun()
                    }
                }
            }

            // near miss
            if (obstacleManager.checkNearMiss(player.x, player.z)) {
                addCombo("near_miss")
                onSoundEvent?.invoke(SoundEvent.NearMiss)
            }

            // coins
            val coinsGot = obstacleManager.collectCoins(
                player.x, player.z, powerUpManager.isMagnetActive,
            )
            if (coinsGot > 0) {
                particles.spawnCoinSparkle(player.x, player.getVisualY(), player.z)
                onSoundEvent?.invoke(SoundEvent.CoinPickup)
                addCombo("coin")
            }

            // power-ups
            val pu = obstacleManager.collectPowerUps(player.x, player.z)
            if (pu != null) {
                powerUpManager.activate(pu); onSoundEvent?.invoke(SoundEvent.PowerUp)
                gameState.emitPowerUp(pu)
            }

            // combo decay
            comboTimer -= dt
            if (comboTimer <= 0f && comboCount > 0) comboCount = 0

            // milestone every 100m
            val dist = (-player.z).toInt()
            val milestone = dist / 100
            if (milestone > lastMilestone) {
                lastMilestone = milestone
                gameState.emitMilestone(milestone * 100)
            }

            // speed phase change notification
            val currentPhase = speedPhaseIndex(runElapsed)
            if (currentPhase != lastSpeedPhase && lastSpeedPhase >= 0) {
                gameState.emitSpeedPhase(currentPhase)
                camera.shake(0.12f)
            }
            lastSpeedPhase = currentPhase

            // score
            val distScore = ((-player.z) * 0.5f).toInt()
            val coinScore = obstacleManager.coinsCollected * 10
            val nearMissBonus = obstacleManager.nearMissCount * 50
            val comboBonus = comboCount * 5
            val multiplier = (1f + runElapsed / 60f + comboCount * 0.05f).coerceAtMost(5f)
            val totalScore = ((distScore + coinScore + nearMissBonus + comboBonus) * multiplier).toInt()

            gameState.update {
                copy(
                    distance = -player.z,
                    score = totalScore,
                    coins = obstacleManager.coinsCollected,
                    multiplier = multiplier,
                    distanceScore = distScore,
                    collectionScore = coinScore,
                    survivalSeconds = runElapsed,
                    runSpeed = this@RunnerRenderer.runSpeed,
                    speedPhase = speedPhaseIndex(runElapsed),
                    playerState = player.state,
                    currentLane = player.currentLane,
                    fishDashActive = powerUpManager.isFishDashActive,
                    hasBubbleScarf = powerUpManager.hasBubbleScarf,
                    snowShieldActive = powerUpManager.hasSnowShield,
                    gustBootsActive = powerUpManager.hasGustBoots,
                    magnetActive = powerUpManager.isMagnetActive,
                    comboCount = comboCount,
                    nearMissCount = obstacleManager.nearMissCount,
                    isRunning = true,
                )
            }

            if (player.y < -5f) endRun()
        } else {
            camera.update(player.x, player.y, player.z, dt)
        }

        // set shader uniforms: fog color, fog range (camera-relative, fixed visual distance)
        shader.use()
        val speedT2 = ((runSpeed - baseSpeed) / (maxSpeed - baseSpeed)).coerceIn(0f, 1f)
        GLES20.glUniform3f(
            shader.getUniformLocation("uFogColor"),
            0.06f + speedT2 * 0.04f, 0.08f, 0.18f + speedT2 * 0.08f,
        )
        // fog near/far are eye-space distances — constant regardless of world position
        GLES20.glUniform1f(shader.getUniformLocation("uFogNear"), 40f)
        GLES20.glUniform1f(shader.getUniformLocation("uFogFar"), 130f)
        GLES20.glUniform3f(shader.getUniformLocation("uSkyTint"), 1f, 1f, 1f)

        background.render(
            shader, camera.vpMatrix, speedT2,
            player.x, player.z,
            camera.eyeWorldX,
        )

        track.render(shader, camera.vpMatrix)
        environment.render(shader, camera.vpMatrix)
        if (isRunning || isGameOver) {
            player.render(shader, camera.vpMatrix)
            obstacleManager.render(shader, camera.vpMatrix, runElapsed)
        }
        particles.render(camera.vpMatrix)
    }

    /** Non-linear speed curve: piecewise phases with configurable easing. */
    private fun calcSpeed(elapsed: Float): Float {
        var t = elapsed
        var fromSpeed = baseSpeed
        for (phase in speedPhases) {
            if (t <= phase.duration) {
                val progress = (t / phase.duration).coerceIn(0f, 1f)
                val eased = Math.pow(progress.toDouble(), phase.curve.toDouble()).toFloat()
                return fromSpeed + (phase.targetSpeed - fromSpeed) * eased
            }
            t -= phase.duration
            fromSpeed = phase.targetSpeed
        }
        return maxSpeed
    }

    /** 0-based phase index玩家当前处于哪个速度段 */
    private fun speedPhaseIndex(elapsed: Float): Int {
        var t = elapsed
        for ((i, phase) in speedPhases.withIndex()) {
            if (t <= phase.duration) return i
            t -= phase.duration
        }
        return speedPhases.size - 1
    }

    private fun addCombo(source: String) {
        comboCount++
        comboTimer = comboTimeout
    }

    private fun processActions() {
        while (true) {
            val action = pendingActions.poll() ?: break
            when (action) {
                Action.Jump -> if (isRunning && !isPaused) {
                    player.jump(highJump = powerUpManager.hasGustBoots)
                    particles.spawnJumpTrail(player.x, player.getVisualY(), player.z)
                    onSoundEvent?.invoke(SoundEvent.Jump)
                }
                Action.Slide -> if (isRunning && !isPaused) {
                    player.slide(); onSoundEvent?.invoke(SoundEvent.Slide)
                }
                Action.MoveLeft -> if (isRunning && !isPaused) {
                    player.moveLeft(); onSoundEvent?.invoke(SoundEvent.LaneSwitch)
                }
                Action.MoveRight -> if (isRunning && !isPaused) {
                    player.moveRight(); onSoundEvent?.invoke(SoundEvent.LaneSwitch)
                }
                Action.Start -> if (!isRunning || isGameOver) startRun()
                Action.Pause -> { isPaused = true; gameState.update { copy(isPaused = true) } }
                Action.Resume -> { isPaused = false; gameState.update { copy(isPaused = false) } }
            }
        }
    }

    private fun startRun() {
        player.reset(); obstacleManager.reset(); environment.reset(); background.reset()
        track.reset()
        powerUpManager.reset(); particles.clear(); camera.reset(0f)
        runSpeed = baseSpeed; runElapsed = 0f; comboCount = 0; comboTimer = 0f
        lastMilestone = 0; lastSpeedPhase = -1; dustTimer = 0f
        isRunning = true; isPaused = false; isGameOver = false
        gameState.update { RunnerSnapshot(isRunning = true) }
    }

    private fun endRun() {
        isRunning = false; isGameOver = true
        player.die(); onSoundEvent?.invoke(SoundEvent.Die)
        gameState.update { copy(isRunning = false, isGameOver = true) }
        gameState.emitGameOver()
    }
}
