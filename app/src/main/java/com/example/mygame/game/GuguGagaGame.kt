package com.example.mygame.game

import androidx.compose.foundation.Canvas
import androidx.compose.foundation.background
import androidx.compose.foundation.clickable
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.width
import androidx.compose.foundation.layout.Spacer
import androidx.compose.foundation.layout.size
import androidx.compose.foundation.layout.statusBarsPadding
import androidx.compose.foundation.layout.widthIn
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.rounded.Pause
import androidx.compose.material3.Icon
import androidx.compose.material3.IconButton
import androidx.compose.material3.LinearProgressIndicator
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.runtime.LaunchedEffect
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableFloatStateOf
import androidx.compose.runtime.mutableIntStateOf
import androidx.compose.runtime.mutableStateListOf
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.rememberUpdatedState
import androidx.compose.runtime.setValue
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.geometry.CornerRadius
import androidx.compose.ui.geometry.Offset
import androidx.compose.ui.geometry.Rect
import androidx.compose.ui.graphics.Brush
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.graphics.ImageBitmap
import androidx.compose.ui.graphics.Path
import androidx.compose.ui.graphics.drawscope.Stroke
import androidx.compose.ui.graphics.drawscope.translate
import androidx.compose.ui.graphics.drawscope.withTransform
import androidx.compose.ui.platform.LocalContext
import androidx.compose.ui.res.imageResource
import androidx.compose.ui.tooling.preview.Preview
import androidx.compose.ui.unit.dp
import com.example.mygame.R
import com.example.mygame.audio.SoundManager
import com.example.mygame.data.SaveRepository
import com.example.mygame.haptic.HapticManager
import com.example.mygame.game.boss.BossTickEvent
import com.example.mygame.game.boss.TakamatsuBossController
import com.example.mygame.game.BossState
import com.example.mygame.game.CoinKind
import com.example.mygame.game.ImpactWave
import com.example.mygame.game.FRAGILE_ICE_STAND_S
import com.example.mygame.game.LevelNpc
import com.example.mygame.game.NpcKind
import com.example.mygame.game.WorldPickup
import com.example.mygame.game.WorldPickupKind
import com.example.mygame.game.level.GameLevel
import com.example.mygame.game.level.LevelCatalog
import com.example.mygame.game.level.LevelContent
import com.example.mygame.game.level.StorySceneTheme
import com.example.mygame.ui.common.GameActionHoldButton
import com.example.mygame.ui.common.GameStageControlDock
import com.example.mygame.ui.common.GameVirtualJoystick
import com.example.mygame.ui.common.GameStageTopBar
import com.example.mygame.ui.common.PauseOverlay
import com.example.mygame.ui.common.rememberDeviceTilt
import com.example.mygame.ui.theme.MyGameTheme
import kotlinx.coroutines.delay
import kotlin.math.abs
import kotlin.math.cos
import kotlin.math.exp
import kotlin.math.hypot
import kotlin.math.max
import kotlin.math.min
import kotlin.math.sin
import kotlin.random.Random

@Composable
fun GuguGagaGame(
    soundManager: SoundManager? = null,
    onExitToMenu: (() -> Unit)? = null,
) {
    val context = LocalContext.current
    val saveRepository = remember { SaveRepository(context) }
    val guguSprite = ImageBitmap.imageResource(R.drawable.gugu_sprite)
    val guguSpriteLayout = remember(guguSprite) { GuguSpriteLayout.singleImage(guguSprite) }
    val drorSprite = ImageBitmap.imageResource(R.drawable.companion_dror)

    var bestScore by remember { mutableIntStateOf(saveRepository.getBestScore()) }
    var rescuedTuanTuan by remember { mutableStateOf(saveRepository.getRescuedTuanTuan()) }
    var companionDrorUnlocked by remember { mutableStateOf(saveRepository.getCompanionDrorUnlocked()) }
    var drorWorldX by remember { mutableFloatStateOf(0f) }
    var drorWorldY by remember { mutableFloatStateOf(0f) }
    var currentLevel by remember { mutableStateOf(saveRepository.getResumeLevel()) }
    var activeLevel by remember { mutableStateOf<LevelContent?>(null) }
    var score by remember { mutableIntStateOf(0) }
    var coinsCollected by remember { mutableIntStateOf(0) }
    var started by remember { mutableStateOf(false) }
    var gameOver by remember { mutableStateOf(false) }
    var levelClear by remember { mutableStateOf(false) }
    var running by remember { mutableStateOf(false) }
    var fishDashTimer by remember { mutableFloatStateOf(0f) }
    var hasBubbleScarf by remember { mutableStateOf(false) }
    var hasSnowShield by remember { mutableStateOf(false) }
    var gustBootsTimer by remember { mutableFloatStateOf(0f) }
    var auroraMagnetTimer by remember { mutableFloatStateOf(0f) }
    var tuanTuanAssistTimer by remember { mutableFloatStateOf(0f) }
    var tuanTuanAssistReady by remember { mutableStateOf(false) }
    var fishSnacksEatenThisRun by remember { mutableIntStateOf(0) }
    var chapterPreviewVisible by remember { mutableStateOf(false) }
    var coyoteTimer by remember { mutableFloatStateOf(0f) }
    var jumpPressed by remember { mutableStateOf(false) }
    var bonusScore by remember { mutableIntStateOf(0) }
    var globalAnim by remember { mutableFloatStateOf(0f) }
    var tutorialMoved by remember { mutableStateOf(false) }
    var tutorialJumped by remember { mutableStateOf(false) }
    var tutorialCollectedPowerUp by remember { mutableStateOf(false) }
    var tutorialDefeatedEnemy by remember { mutableStateOf(false) }
    var campUnlockNoticeVisible by remember { mutableStateOf(false) }

    val deviceTilt by rememberDeviceTilt(maxOffsetPx = 80f)

    var stompHapticNonce by remember { mutableIntStateOf(0) }
    var momentumHapticNonce by remember { mutableIntStateOf(0) }
    val hapticManager = remember { HapticManager(context, saveRepository) }

    val coyoteMax = 0.12f

    var worldWidth by remember { mutableFloatStateOf(1f) }
    var worldHeight by remember { mutableFloatStateOf(1f) }
    var levelLength by remember { mutableFloatStateOf(2400f) }
    var cameraX by remember { mutableFloatStateOf(0f) }

    val takamatsuBoss = remember { TakamatsuBossController() }
    var isBossFight by remember { mutableStateOf(false) }
    /** Boss 击败后延迟结算，避免顿帧与 victory UI 抢拍。 */
    var isBossDefeatSequenceActive by remember { mutableStateOf(false) }
    var bossDefeatFinishKey by remember { mutableIntStateOf(0) }
    var isPaused by remember { mutableStateOf(false) }
    val isPausedState = rememberUpdatedState(isPaused)
    val impactWaves = remember { mutableStateListOf<ImpactWave>() }

    var playerX by remember { mutableFloatStateOf(100f) }
    var playerY by remember { mutableFloatStateOf(100f) }
    var playerVelocityX by remember { mutableFloatStateOf(0f) }
    var playerVelocityY by remember { mutableFloatStateOf(0f) }
    var onGround by remember { mutableStateOf(false) }

    /** -1f～1f，由左下角虚拟横轴摇杆驱动。 */
    var joystickX by remember { mutableFloatStateOf(0f) }
    var crouchPressed by remember { mutableStateOf(false) }
    /** 重开、进入新关卡时与摇杆 [resetKey] 同步归零。 */
    var inputSession by remember { mutableIntStateOf(0) }
    var playerFacingRight by remember { mutableStateOf(true) }
    var spriteAnimTick by remember { mutableIntStateOf(0) }

    val platforms = remember { mutableStateListOf<Platform>() }
    val pits = remember { mutableStateListOf<Pit>() }
    val enemies = remember { mutableStateListOf<Enemy>() }
    val coins = remember { mutableStateListOf<Coin>() }
    val blocks = remember { mutableStateListOf<Block>() }
    val floatingCoins = remember { mutableStateListOf<FloatingCoin>() }
    val fishSnacks = remember { mutableStateListOf<FishSnack>() }
    val levelNpcs = remember { mutableStateListOf<LevelNpc>() }
    val worldPickups = remember { mutableStateListOf<WorldPickup>() }
    var friendGoal by remember { mutableStateOf<FriendGoal?>(null) }
    var npcBubbleText by remember { mutableStateOf<String?>(null) }
    var npcBubbleTimer by remember { mutableFloatStateOf(0f) }
    var mechanicHintText by remember { mutableStateOf<String?>(null) }
    var mechanicHintTimer by remember { mutableFloatStateOf(0f) }
    val particles = remember { mutableStateListOf<Particle>() }
    val dashPhantoms = remember { mutableStateListOf<DashPhantom>() }
    var hitStopTimer by remember { mutableFloatStateOf(0f) }
    /** 当前帧震屏振幅（像素），每帧乘以 [StompFeel.SHAKE_DECAY_PER_FRAME]。 */
    var shakeAmplitude by remember { mutableFloatStateOf(0f) }
    var shakeBias by remember { mutableStateOf(StompFeel.ShakeBias.None) }
    var shakeBiasFrames by remember { mutableIntStateOf(0) }
    var renderShakeX by remember { mutableFloatStateOf(0f) }
    var renderShakeY by remember { mutableFloatStateOf(0f) }

    fun groundTop() = worldHeight * 0.82f
    fun playerSize() = min(worldWidth, worldHeight) * 0.1f

    fun intersectsPit(left: Float, right: Float): Boolean {
        return pits.any { pit -> right > pit.startX && left < pit.endX }
    }

    fun persistBestScore() {
        if (score > bestScore) {
            bestScore = score
            saveRepository.saveBestScore(bestScore)
        }
    }

    fun persistTuanTuanRescue() {
        if (!rescuedTuanTuan) {
            rescuedTuanTuan = true
            saveRepository.saveRescuedTuanTuan()
        }
    }

    fun resetPlayerPosition() {
        val size = playerSize()
        playerX = worldWidth * 0.12f
        playerY = groundTop() - size
        playerVelocityX = 0f
        playerVelocityY = 0f
        onGround = true
        cameraX = 0f
        coyoteTimer = coyoteMax
        if (companionDrorUnlocked) {
            val (ix, iy) = initialDrorPosition(playerX, playerY, playerFacingRight, size)
            drorWorldX = ix
            drorWorldY = iy
        }
    }

    fun rebuildLevel() {
        if (worldWidth <= 1f || worldHeight <= 1f) return
        val content = LevelCatalog.build(currentLevel, worldWidth, worldHeight)
        activeLevel = content
        levelLength = content.levelLength
        pits.clear()
        pits.addAll(content.pits)
        platforms.clear()
        platforms.addAll(content.platforms)
        blocks.clear()
        blocks.addAll(content.blocks)
        enemies.clear()
        enemies.addAll(content.enemies)
        coins.clear()
        coins.addAll(content.coins)
        fishSnacks.clear()
        floatingCoins.clear()
        particles.clear()
        dashPhantoms.clear()
        hitStopTimer = 0f
        shakeAmplitude = 0f
        shakeBias = StompFeel.ShakeBias.None
        shakeBiasFrames = 0
        renderShakeX = 0f
        renderShakeY = 0f
        friendGoal = content.friendGoal
        levelNpcs.clear()
        levelNpcs.addAll(content.npcs)
        worldPickups.clear()
        worldPickups.addAll(content.worldPickups)
        npcBubbleText = null
        npcBubbleTimer = 0f
        mechanicHintText = null
        mechanicHintTimer = 0f
        isBossFight = false
        isBossDefeatSequenceActive = false
        bossDefeatFinishKey = 0
        isPaused = false
        impactWaves.clear()
        takamatsuBoss.reset()
        if (currentLevel == GameLevel.IceLakeEchoValley) {
            if (!saveRepository.getCompanionDrorUnlocked()) {
                saveRepository.saveCompanionDrorUnlocked()
            }
            companionDrorUnlocked = true
        }
    }

    fun resetGame() {
        fishDashTimer = 0f
        hasBubbleScarf = false
        hasSnowShield = false
        gustBootsTimer = 0f
        auroraMagnetTimer = 0f
        tuanTuanAssistTimer = 0f
        tuanTuanAssistReady = rescuedTuanTuan
        fishSnacksEatenThisRun = 0
        chapterPreviewVisible = false
        campUnlockNoticeVisible = false
        jumpPressed = false
        crouchPressed = false
        joystickX = 0f
        inputSession++
        bonusScore = 0
        particles.clear()
        hitStopTimer = 0f
        shakeAmplitude = 0f
        shakeBias = StompFeel.ShakeBias.None
        shakeBiasFrames = 0
        renderShakeX = 0f
        renderShakeY = 0f
        rebuildLevel()
        resetPlayerPosition()
        score = 0
        coinsCollected = 0
        started = true
        gameOver = false
        levelClear = false
        running = true
    }

    fun jump() {
        if (!started || gameOver || levelClear) {
            if (levelClear && !gameOver) {
                currentLevel.storyNext()?.let { next ->
                    saveRepository.setResumeLevel(next)
                    currentLevel = next
                }
            }
            resetGame()
            return
        }
        if (onGround || coyoteTimer > 0f) {
            tutorialJumped = true
            playerVelocityY = if (gustBootsTimer > 0f) -1160f else -1040f
            onGround = false
            coyoteTimer = 0f
            soundManager?.playJump()
        }
    }

    fun endRun(win: Boolean) {
        isBossDefeatSequenceActive = false
        bossDefeatFinishKey = 0
        isPaused = false
        running = false
        if (fishSnacksEatenThisRun > 0) {
            saveRepository.addFishSnacks(fishSnacksEatenThisRun)
            fishSnacksEatenThisRun = 0
        }
        particles.clear()
        dashPhantoms.clear()
        hitStopTimer = 0f
        shakeAmplitude = 0f
        shakeBias = StompFeel.ShakeBias.None
        shakeBiasFrames = 0
        renderShakeX = 0f
        renderShakeY = 0f
        gameOver = !win
        levelClear = win
        if (win && currentLevel == GameLevel.MistDike && isBossFight) {
            saveRepository.setTakamatsuDefeated()
        }
        if (win) {
            val shouldShowCampNotice =
                currentLevel == GameLevel.CedarVillageRuins &&
                    !saveRepository.hasSeenCampUnlockNotice()
            persistTuanTuanRescue()
            tuanTuanAssistReady = true
            chapterPreviewVisible = activeLevel?.presentation?.chapterPreviewTitle != null
            if (currentLevel == GameLevel.CedarVillageRuins) {
                saveRepository.saveCompanionDrorUnlocked()
                companionDrorUnlocked = true
            }
            if (shouldShowCampNotice) {
                campUnlockNoticeVisible = true
                saveRepository.markCampUnlockNoticeSeen()
            }
        }
        persistBestScore()
    }

    fun triggerTuanTuanAssist() {
        if (!rescuedTuanTuan || !tuanTuanAssistReady || !started || gameOver || levelClear) return
        tuanTuanAssistReady = false
        tuanTuanAssistTimer = saveRepository.getTuanAssistDurationSeconds()
    }

    LaunchedEffect(worldWidth, worldHeight) {
        if (worldWidth > 1f && worldHeight > 1f && !started) {
            rebuildLevel()
            resetPlayerPosition()
        }
    }

    LaunchedEffect(stompHapticNonce) {
        if (stompHapticNonce > 0) {
            hapticManager.performStomp()
        }
    }

    LaunchedEffect(momentumHapticNonce) {
        if (momentumHapticNonce > 0) {
            hapticManager.performMomentum()
        }
    }

    LaunchedEffect(bossDefeatFinishKey) {
        if (bossDefeatFinishKey == 0) return@LaunchedEffect
        val k = bossDefeatFinishKey
        delay(1850L)
        if (bossDefeatFinishKey != k) return@LaunchedEffect
        if (!isBossDefeatSequenceActive) return@LaunchedEffect
        endRun(win = true)
    }

    LaunchedEffect(Unit) {
        while (true) {
            delay(16)
            if (!isPausedState.value && hitStopTimer <= 0f) {
                globalAnim += 0.05f
            }
        }
    }

    LaunchedEffect(running, started) {
        while (running && started) {
            delay(100)
            if (!isPausedState.value && hitStopTimer <= 0f) {
                spriteAnimTick++
            }
        }
    }

    LaunchedEffect(isPaused, started, gameOver, levelClear) {
        if (!started || gameOver || levelClear) return@LaunchedEffect
        if (isPaused) soundManager?.pauseBgm() else soundManager?.resumeBgm()
    }

    LaunchedEffect(running, worldWidth, worldHeight) {
        if (!running || worldWidth <= 1f || worldHeight <= 1f) return@LaunchedEffect

        val frameSeconds = 0.016f
        val gravity = 1980f
        val runSpeed =
            when {
                fishDashTimer > 0f -> 385f
                gustBootsTimer > 0f -> 345f
                else -> 305f
            }
        val friction = 0.82f
        val groundY = groundTop()

        while (running) {
            delay(16)
            if (isPausedState.value) continue

            if (shakeAmplitude > StompFeel.SHAKE_CUTOFF_PX) {
                val (sx, sy) =
                    when (shakeBias) {
                        StompFeel.ShakeBias.Horizontal ->
                            StompFeel.randomShakeOffsetHorizontalDominant(shakeAmplitude, Random)
                        StompFeel.ShakeBias.Vertical ->
                            StompFeel.randomShakeOffsetVerticalDominant(shakeAmplitude, Random)
                        StompFeel.ShakeBias.None ->
                            StompFeel.randomShakeOffsetPx(shakeAmplitude, Random)
                    }
                renderShakeX = sx
                renderShakeY = sy
                shakeAmplitude *= StompFeel.SHAKE_DECAY_PER_FRAME
            } else {
                shakeAmplitude = 0f
                renderShakeX = 0f
                renderShakeY = 0f
            }
            if (shakeBiasFrames > 0) {
                shakeBiasFrames--
                if (shakeBiasFrames == 0) shakeBias = StompFeel.ShakeBias.None
            }
            if (hitStopTimer > 0f) {
                hitStopTimer = max(0f, hitStopTimer - frameSeconds)
                continue
            }

            val dashActive = fishDashTimer > 0f
            fishDashTimer = max(0f, fishDashTimer - frameSeconds)
            gustBootsTimer = max(0f, gustBootsTimer - frameSeconds)
            auroraMagnetTimer = max(0f, auroraMagnetTimer - frameSeconds)
            tuanTuanAssistTimer = max(0f, tuanTuanAssistTimer - frameSeconds)

            val size = playerSize()
            val wasInAir = !onGround
            val previousY = playerY
            val previousBottom = previousY + size
            val overPitForFriction = intersectsPit(playerX, playerX + size)
            val surfaceFrictionFactor = standingSurfaceFriction(
                onGround,
                playerX,
                playerY,
                size,
                groundY,
                overPitForFriction,
                platforms,
                blocks,
            )
            val horizontalDamp = horizontalGroundDampening(friction, surfaceFrictionFactor)

            val joy = joystickX.coerceIn(-1f, 1f)
            val crouchMul = if (crouchPressed && onGround) 0.55f else 1f
            val targetV = joy * runSpeed * crouchMul
            if (abs(joy) < 0.01f) {
                playerVelocityX *= horizontalDamp
            } else {
                val alpha = 1f - exp(-14f * frameSeconds)
                playerVelocityX += (targetV - playerVelocityX) * alpha
            }
            if (abs(playerVelocityX) > 22f) {
                playerFacingRight = playerVelocityX > 0f
            }

            playerVelocityY += gravity * frameSeconds
            if (hasBubbleScarf && playerVelocityY > 180f && !onGround) {
                playerVelocityY *= 0.88f
            }
            if (!jumpPressed && playerVelocityY < -380f) {
                playerVelocityY *= 0.52f
            }
            playerX = (playerX + playerVelocityX * frameSeconds).coerceIn(0f, levelLength - size)
            if (!isBossFight && !gameOver && !levelClear && started && activeLevel?.bossArena != null) {
                val spec = activeLevel!!.bossArena!!
                if (playerX >= spec.triggerAtPlayerX) {
                    isBossFight = true
                    friendGoal = null
                    enemies.clear()
                    impactWaves.clear()
                    takamatsuBoss.start(spec, groundY, size)
                }
            }
            if (!tutorialMoved && currentLevel == GameLevel.CedarVillageRuins) {
                tutorialMoved = abs(playerX - worldWidth * 0.12f) > worldWidth * 0.08f
            }

            var nextY = playerY + playerVelocityY * frameSeconds
            var landed = false
            var landingY = Float.MAX_VALUE
            val playerLeft = playerX
            val playerRight = playerX + size
            val overPit = intersectsPit(playerLeft, playerRight)

            if (!overPit && previousBottom <= groundY && nextY + size >= groundY) {
                landed = true
                landingY = groundY - size
            }

            platforms.forEach { platform ->
                val horizontalOverlap = playerRight > platform.x && playerLeft < platform.x + platform.width
                val wasAbove = previousBottom <= platform.y + 4f
                val reachesTop = nextY + size >= platform.y
                if (horizontalOverlap && wasAbove && reachesTop) {
                    landed = true
                    landingY = min(landingY, platform.y - size)
                }
            }

            blocks.forEach { block ->
                val horizontalOverlap = playerRight > block.x && playerLeft < block.x + block.size
                val blockTop = block.y - block.bounceOffset
                val wasAbove = previousBottom <= blockTop + 4f
                val reachesTop = nextY + size >= blockTop
                if (horizontalOverlap && wasAbove && reachesTop) {
                    landed = true
                    landingY = min(landingY, blockTop - size)
                }
            }

            if (playerVelocityY < 0f) {
                for (index in blocks.indices) {
                    val block = blocks[index]
                    val horizontalOverlap = playerRight > block.x && playerLeft < block.x + block.size
                    val blockBottom = block.y - block.bounceOffset + block.size
                    val wasBelow = previousY >= blockBottom - 4f
                    val hitsBottom = nextY <= blockBottom

                    if (horizontalOverlap && wasBelow && hitsBottom) {
                        nextY = blockBottom
                        playerVelocityY = 140f
                        blocks[index] = block.copy(bounceOffset = 16f)

                        if (block.type == BlockType.Question && !block.used) {
                            when (block.reward) {
                                BlockReward.Coin -> {
                                    blocks[index] = blocks[index].copy(used = true)
                                    coinsCollected += 1
                                    soundManager?.playCoinPickup()
                                    floatingCoins += FloatingCoin(
                                        x = block.x + block.size * 0.25f,
                                        y = block.y - block.size * 0.35f,
                                        size = block.size * 0.5f,
                                        velocityY = -280f,
                                        life = 0.55f
                                    )
                                }
                                BlockReward.Fish -> {
                                    blocks[index] = blocks[index].copy(used = true)
                                    tutorialCollectedPowerUp = true
                                    fishSnacks += FishSnack(
                                        x = block.x + block.size * 0.08f,
                                        y = block.y + block.size * 0.2f,
                                        size = block.size * 0.82f,
                                        velocityX = 90f,
                                        velocityY = 0f,
                                        emerging = true,
                                        progress = 0f
                                    )
                                }
                                BlockReward.Scarf -> {
                                    blocks[index] = blocks[index].copy(used = true)
                                    tutorialCollectedPowerUp = true
                                    hasBubbleScarf = true
                                    soundManager?.playPowerUp()
                                    floatingCoins += FloatingCoin(
                                        x = block.x + block.size * 0.18f,
                                        y = block.y - block.size * 0.25f,
                                        size = block.size * 0.58f,
                                        velocityY = -220f,
                                        life = 0.7f
                                    )
                                }
                                BlockReward.Shield -> {
                                    blocks[index] = blocks[index].copy(used = true)
                                    tutorialCollectedPowerUp = true
                                    hasSnowShield = true
                                    soundManager?.playPowerUp()
                                    floatingCoins += FloatingCoin(
                                        x = block.x + block.size * 0.2f,
                                        y = block.y - block.size * 0.2f,
                                        size = block.size * 0.54f,
                                        velocityY = -240f,
                                        life = 0.65f,
                                    )
                                }
                                BlockReward.Boots -> {
                                    blocks[index] = blocks[index].copy(used = true)
                                    tutorialCollectedPowerUp = true
                                    gustBootsTimer = 8f
                                    soundManager?.playPowerUp()
                                    floatingCoins += FloatingCoin(
                                        x = block.x + block.size * 0.2f,
                                        y = block.y - block.size * 0.24f,
                                        size = block.size * 0.54f,
                                        velocityY = -240f,
                                        life = 0.65f,
                                    )
                                }
                                BlockReward.Magnet -> {
                                    blocks[index] = blocks[index].copy(used = true)
                                    tutorialCollectedPowerUp = true
                                    auroraMagnetTimer = saveRepository.getMagnetDurationSeconds()
                                    floatingCoins += FloatingCoin(
                                        x = block.x + block.size * 0.2f,
                                        y = block.y - block.size * 0.24f,
                                        size = block.size * 0.6f,
                                        velocityY = -260f,
                                        life = 0.75f,
                                    )
                                    soundManager?.playMagnetPickup()
                                }
                            }
                        }
                        break
                    }
                }
            }

            if (landed && playerVelocityY >= 0f) {
                nextY = landingY
                val springPlat = platforms.firstOrNull { p ->
                    p.bounceImpulse > 0f &&
                        playerRight > p.x && playerLeft < p.x + p.width &&
                        abs(p.y - size - landingY) < 5f
                }
                if (wasInAir && springPlat != null) {
                    playerVelocityY = -springPlat.bounceImpulse
                    onGround = false
                    mechanicHintText = "弹力苔垫会把咕咕嘎嘎向上弹起，按住摇杆可以控制落点。"
                    mechanicHintTimer = 2.2f
                    soundManager?.playJump()
                    ParticleSpawners.landingDust(
                        particles,
                        worldX = playerX,
                        footY = landingY + size,
                        playerWidth = size,
                    )
                } else {
                    if (wasInAir && playerVelocityY > 45f) {
                        if (playerVelocityY >= StompFeel.HARD_LAND_IMPACT_VY) {
                            shakeAmplitude = max(shakeAmplitude, StompFeel.SHAKE_HARD_LAND_PX)
                            shakeBias = StompFeel.ShakeBias.Vertical
                            shakeBiasFrames = StompFeel.SHAKE_MOMENTUM_BIAS_FRAMES
                            momentumHapticNonce++
                            ParticleSpawners.hardLandingDust(
                                particles,
                                worldX = playerX,
                                footY = landingY + size,
                                playerWidth = size,
                            )
                        } else {
                            ParticleSpawners.landingDust(
                                particles,
                                worldX = playerX,
                                footY = landingY + size,
                                playerWidth = size,
                            )
                        }
                        soundManager?.playLand()
                    }
                    playerVelocityY = 0f
                    onGround = true
                }
            } else {
                onGround = false
            }

            playerY = nextY
            if (onGround) {
                val pBot = playerY + size
                val epsB = 8f
                var belt = 0f
                for (p in platforms) {
                    if (p.conveyorBelt == 0f) continue
                    if (playerRight > p.x && playerLeft < p.x + p.width &&
                        pBot >= p.y - epsB && pBot <= p.y + epsB * 2f
                    ) {
                        belt = p.conveyorBelt
                        break
                    }
                }
                if (belt != 0f) {
                    playerX = (playerX + belt * frameSeconds).coerceIn(0f, levelLength - size)
                    mechanicHintText = if (belt > 0f) "顺向履带会推着你前进，蹲下可以更稳地微调。" else "逆向履带会把你往回带，提前加速或跳离它。"
                    mechanicHintTimer = max(mechanicHintTimer, 0.7f)
                }
            }

            if (playerY > worldHeight && !isBossDefeatSequenceActive) {
                endRun(win = false)
                continue
            }

            if (isBossFight && takamatsuBoss.running) {
                for (tickEvent in takamatsuBoss.tick(frameSeconds)) {
                    when (tickEvent) {
                        is BossTickEvent.GroundImpact -> {
                            impactWaves += tickEvent.wave
                            ParticleSpawners.bossImpactWaveRim(
                                particles,
                                tickEvent.wave.centerX,
                                tickEvent.wave.surfaceY,
                                tickEvent.wave.halfWidth,
                            )
                        }
                        is BossTickEvent.HeavyStompFeel -> {
                            if (tickEvent.hitStop > hitStopTimer) {
                                hitStopTimer = tickEvent.hitStop
                            }
                            shakeAmplitude = max(shakeAmplitude, tickEvent.shake)
                            if (tickEvent.shakeBias != StompFeel.ShakeBias.None) {
                                shakeBias = tickEvent.shakeBias
                                shakeBiasFrames = StompFeel.SHAKE_MOMENTUM_BIAS_FRAMES
                            }
                            if (tickEvent.playBossSlamSfx) {
                                soundManager?.playBossLand()
                                soundManager?.duckBgmOnHeavyStomp(0.22f, 220L)
                            }
                        }
                        is BossTickEvent.RequestSpawnMinion -> {
                            enemies += Enemy(
                                x = tickEvent.x,
                                y = tickEvent.y,
                                width = tickEvent.width,
                                height = tickEvent.height,
                                patrolStart = tickEvent.x - 20f,
                                patrolEnd = tickEvent.x + 200f,
                                speed = 105f,
                                kind = tickEvent.kind,
                                direction = 1f,
                            )
                        }
                        is BossTickEvent.MarkPlatformsFragile -> {
                            if (tickEvent.enabled) {
                                for (i in platforms.indices) {
                                    val p = platforms[i]
                                    if (!p.isFragile) {
                                        platforms[i] = p.copy(isFragile = true, fragileTimeLeft = FRAGILE_ICE_STAND_S)
                                    }
                                }
                            }
                        }
                        is BossTickEvent.BossDefeated -> {
                            if (isBossDefeatSequenceActive) break
                            isBossDefeatSequenceActive = true
                            bossDefeatFinishKey++
                            enemies.clear()
                            impactWaves.clear()
                            val be = takamatsuBoss.entity
                            soundManager?.playBossDefeat()
                            soundManager?.duckBgmOnHeavyStomp(0.1f, 2500L)
                            hitStopTimer = max(hitStopTimer, StompFeel.BOSS_DEATH_HIT_STOP_S)
                            shakeAmplitude = max(shakeAmplitude, StompFeel.SHAKE_MAX_PX * 1.15f)
                            shakeBias = StompFeel.ShakeBias.Vertical
                            shakeBiasFrames = StompFeel.SHAKE_MOMENTUM_BIAS_FRAMES
                            stompHapticNonce++
                            ParticleSpawners.stompKillBurst(
                                particles,
                                centerX = be.x + be.width * 0.5f,
                                centerY = be.y + be.height * 0.42f,
                                countMultiplier = 2f,
                                speedScale = 0.5f,
                            )
                        }
                    }
                }
            }
            if (!running) {
                continue
            }

            if (impactWaves.isNotEmpty()) {
                val pcx = playerX + size * 0.5f
                val pBot = playerY + size
                for (i in impactWaves.lastIndex downTo 0) {
                    val w = impactWaves[i]
                    w.halfWidth += w.growSpeed * frameSeconds
                    w.life -= frameSeconds
                    if (w.life <= 0f) {
                        impactWaves.removeAt(i)
                        continue
                    }
                    if (onGround && abs(pcx - w.centerX) < w.halfWidth &&
                        pBot >= w.surfaceY - 18f && pBot <= w.surfaceY + 22f
                    ) {
                        playerVelocityY = -480f
                        onGround = false
                        momentumHapticNonce++
                        shakeAmplitude = max(shakeAmplitude, 16f)
                        w.life = 0f
                    }
                }
                impactWaves.removeAll { it.life <= 0f }
            }

            val pBottom = playerY + size
            val pLeft = playerX
            val pRight = playerX + size
            val epsPlat = 8f
            for (pi in platforms.lastIndex downTo 0) {
                val p = platforms[pi]
                if (!p.isFragile) continue
                val standingOnFragile =
                    onGround && pRight > p.x && pLeft < p.x + p.width &&
                        pBottom >= p.y - epsPlat && pBottom <= p.y + epsPlat * 2f
                if (standingOnFragile) {
                    val t = (p.fragileTimeLeft ?: FRAGILE_ICE_STAND_S) - frameSeconds
                    if (t <= 0f) {
                        ParticleSpawners.fragileIceShatter(
                            particles,
                            centerX = p.x + p.width * 0.5f,
                            centerY = p.y + 3f,
                        )
                        platforms.removeAt(pi)
                        shakeAmplitude = max(shakeAmplitude, StompFeel.SHAKE_LIGHT_PX)
                        onGround = false
                        playerVelocityY = 50f
                        soundManager?.playIceCrack()
                    } else {
                        platforms[pi] = p.copy(fragileTimeLeft = t)
                    }
                } else {
                    if (p.fragileTimeLeft != null) {
                        platforms[pi] = p.copy(fragileTimeLeft = null)
                    }
                }
            }

            for (index in blocks.indices) {
                val block = blocks[index]
                if (block.bounceOffset > 0f) {
                    blocks[index] = block.copy(bounceOffset = max(0f, block.bounceOffset - 180f * frameSeconds))
                }
            }

            for (index in enemies.indices) {
                val enemy = enemies[index]
                val kindSpeedMul =
                    when (enemy.kind) {
                        EnemyKind.SpikedSeal -> 1.14f
                        EnemyKind.Owl -> 0.92f
                        else -> 1f
                    }
                val effectiveSpeed =
                    (if (tuanTuanAssistTimer > 0f) enemy.speed * 0.2f else enemy.speed) * kindSpeedMul
                var nextEnemyX = enemy.x + effectiveSpeed * enemy.direction * frameSeconds
                var nextDirection = enemy.direction
                if (nextEnemyX < enemy.patrolStart) {
                    nextEnemyX = enemy.patrolStart
                    nextDirection = 1f
                } else if (nextEnemyX + enemy.width > enemy.patrolEnd) {
                    nextEnemyX = enemy.patrolEnd - enemy.width
                    nextDirection = -1f
                }
                enemies[index] = enemy.copy(x = nextEnemyX, direction = nextDirection)
            }

            val nextFloatingCoins = mutableListOf<FloatingCoin>()
            floatingCoins.forEach { coin ->
                val updated = coin.copy(
                    y = coin.y + coin.velocityY * frameSeconds,
                    velocityY = coin.velocityY + 900f * frameSeconds,
                    life = coin.life - frameSeconds
                )
                if (updated.life > 0f) nextFloatingCoins += updated
            }
            floatingCoins.clear()
            floatingCoins.addAll(nextFloatingCoins)

            val nextFishSnacks = mutableListOf<FishSnack>()
            fishSnacks.forEach { fishSnack ->
                if (fishSnack.emerging) {
                    val progress = (fishSnack.progress + frameSeconds * 2f).coerceAtMost(1f)
                    val emergeY = fishSnack.y - fishSnack.size * 0.9f * frameSeconds * 2f
                    nextFishSnacks += fishSnack.copy(y = emergeY, progress = progress, emerging = progress < 1f)
                } else {
                    var fishX = fishSnack.x + fishSnack.velocityX * frameSeconds
                    var fishY = fishSnack.y + fishSnack.velocityY * frameSeconds
                    var fishVelocityX = fishSnack.velocityX
                    var fishVelocityY = fishSnack.velocityY + gravity * 0.75f * frameSeconds

                    val fishBottom = fishY + fishSnack.size
                    val fishLeft = fishX
                    val fishRight = fishX + fishSnack.size
                    var fishLanded = false
                    var fishLandingY = Float.MAX_VALUE
                    val fishOverPit = intersectsPit(fishLeft, fishRight)

                    if (!fishOverPit && fishBottom >= groundY) {
                        fishLanded = true
                        fishLandingY = groundY - fishSnack.size
                    }

                    platforms.forEach { platform ->
                        val horizontalOverlap = fishRight > platform.x && fishLeft < platform.x + platform.width
                        val reachesTop = fishBottom >= platform.y
                        val aboveTop = fishY + fishSnack.size <= platform.y + 20f
                        if (horizontalOverlap && reachesTop && aboveTop) {
                            fishLanded = true
                            fishLandingY = min(fishLandingY, platform.y - fishSnack.size)
                        }
                    }

                    blocks.forEach { block ->
                        val blockTop = block.y - block.bounceOffset
                        val horizontalOverlap = fishRight > block.x && fishLeft < block.x + block.size
                        val reachesTop = fishBottom >= blockTop
                        val aboveTop = fishY + fishSnack.size <= blockTop + 20f
                        if (horizontalOverlap && reachesTop && aboveTop) {
                            fishLanded = true
                            fishLandingY = min(fishLandingY, blockTop - fishSnack.size)
                        }
                        val verticalOverlap = fishY < blockTop + block.size && fishBottom > blockTop
                        if (verticalOverlap && horizontalOverlap) {
                            if (fishVelocityX > 0f && fishRight > block.x) {
                                fishX = block.x - fishSnack.size
                                fishVelocityX = -fishVelocityX
                            } else if (fishVelocityX < 0f && fishLeft < block.x + block.size) {
                                fishX = block.x + block.size
                                fishVelocityX = -fishVelocityX
                            }
                        }
                    }

                    if (fishLanded) {
                        fishY = fishLandingY
                        fishVelocityY = 0f
                    }
                    if (fishX <= 0f || fishX + fishSnack.size >= levelLength) {
                        fishVelocityX = -fishVelocityX
                    }
                    if (fishY <= worldHeight) {
                        nextFishSnacks += fishSnack.copy(
                            x = fishX.coerceIn(0f, levelLength - fishSnack.size),
                            y = fishY,
                            velocityX = fishVelocityX,
                            velocityY = fishVelocityY,
                            emerging = false,
                            progress = 1f
                        )
                    }
                }
            }
            fishSnacks.clear()
            fishSnacks.addAll(nextFishSnacks)

            val playerRect = Rect(playerX, playerY, playerX + size, playerY + size)
            if (auroraMagnetTimer > 0f && coins.isNotEmpty()) {
                val magnetRadius = size * 5.2f
                val pullSpeed = 680f
                val playerCenterX = playerX + size * 0.5f
                val playerCenterY = playerY + size * 0.5f
                val pulledCoins =
                    coins.map { coin ->
                        val coinCenterX = coin.x + coin.size * 0.5f
                        val coinCenterY = coin.y + coin.size * 0.5f
                        val dx = playerCenterX - coinCenterX
                        val dy = playerCenterY - coinCenterY
                        val dist = hypot(dx, dy)
                        if (dist in 1f..magnetRadius) {
                            val step = min(pullSpeed * frameSeconds, dist)
                            coin.copy(
                                x = coin.x + dx / dist * step,
                                y = coin.y + dy / dist * step,
                            )
                        } else {
                            coin
                        }
                    }
                coins.clear()
                coins.addAll(pulledCoins)
            }

            val collectedCoins = coins.filter { coin ->
                val coinRect = Rect(coin.x, coin.y, coin.x + coin.size, coin.y + coin.size)
                playerRect.overlaps(coinRect)
            }
            if (collectedCoins.isNotEmpty()) {
                coinsCollected += collectedCoins.size
                val storyCollectBonus =
                    collectedCoins.sumOf { coin ->
                        when (coin.kind) {
                            CoinKind.Normal -> 0
                            CoinKind.Beacon -> 35
                            CoinKind.LorePage -> 120
                        }
                    }
                bonusScore += storyCollectBonus
                coins.removeAll(collectedCoins.toSet())
                if (collectedCoins.any { it.kind == CoinKind.LorePage || it.kind == CoinKind.Beacon }) {
                    soundManager?.playPowerUp()
                } else {
                    soundManager?.playCoinPickup()
                }
            }

            val collectedPickups = worldPickups.filter { wp ->
                val r = Rect(wp.x, wp.y, wp.x + wp.size, wp.y + wp.size)
                playerRect.overlaps(r)
            }
            if (collectedPickups.isNotEmpty()) {
                for (wp in collectedPickups) {
                    when (wp.kind) {
                        WorldPickupKind.Snowberry -> {
                            bonusScore += 18
                            mechanicHintText = "雪浆果：获得少量奖励分。"
                            mechanicHintTimer = 1.8f
                            soundManager?.playCoinPickup()
                            ParticleSpawners.landingDust(
                                particles,
                                worldX = wp.x + wp.size * 0.2f,
                                footY = wp.y + wp.size,
                                playerWidth = wp.size,
                            )
                        }
                        WorldPickupKind.GustSeed -> {
                            gustBootsTimer = max(gustBootsTimer, 2.8f)
                            mechanicHintText = "风种：短时间延长长跳，适合接高台路线。"
                            mechanicHintTimer = 2.2f
                            soundManager?.playPowerUp()
                        }
                        WorldPickupKind.GlintFragment -> {
                            bonusScore += 32
                            mechanicHintText = "微光片：隐藏路线奖励，继续探索会有更高收益。"
                            mechanicHintTimer = 2.2f
                            soundManager?.playPowerUp()
                        }
                    }
                }
                worldPickups.removeAll(collectedPickups.toSet())
            }

            val eatenFish = fishSnacks.firstOrNull { fishSnack ->
                val fishRect = Rect(fishSnack.x, fishSnack.y, fishSnack.x + fishSnack.size, fishSnack.y + fishSnack.size)
                playerRect.overlaps(fishRect)
            }
            if (eatenFish != null) {
                val ex = eatenFish
                fishSnacks.remove(eatenFish)
                fishSnacksEatenThisRun += 1
                fishDashTimer = saveRepository.getFishDashDurationSeconds()
                tutorialCollectedPowerUp = true
                bonusScore += 50
                ParticleSpawners.fishSnackBurst(
                    particles,
                    centerX = ex.x + ex.size * 0.5f,
                    centerY = ex.y + ex.size * 0.45f,
                )
                soundManager?.playEatFish()
            }

            var bossStompHandled = false
            if (isBossFight && takamatsuBoss.isEntityReady) {
                val be = takamatsuBoss.entity
                if (be.state != BossState.DYING && be.state != BossState.INTRO) {
                    val bossRect = Rect(be.x, be.y, be.x + be.width, be.y + be.height)
                    val stompBoss =
                        playerRect.overlaps(bossRect) &&
                            playerVelocityY > 0f &&
                            playerRect.bottom <= be.y + be.height * 0.5f
                    if (stompBoss) {
                        bossStompHandled = true
                        when {
                            takamatsuBoss.isBossAirborneForStompIgnore(groundY) -> {
                                playerVelocityY = -400f
                                onGround = false
                                hitStopTimer = StompFeel.HIT_STOP_LIGHT_S
                                soundManager?.playShieldBounce()
                            }
                            be.state == BossState.SHIELDED && be.hasShield &&
                                fishDashTimer <= 0f && tuanTuanAssistTimer <= 0f -> {
                                playerVelocityY = -400f
                                onGround = false
                                hitStopTimer = StompFeel.HIT_STOP_LIGHT_S
                                soundManager?.playShieldBounce()
                            }
                            be.state == BossState.SHIELDED && be.hasShield &&
                                (fishDashTimer > 0f || tuanTuanAssistTimer > 0f) -> {
                                takamatsuBoss.notifyShieldBroken()
                                playerVelocityY = -720f
                                onGround = false
                                hitStopTimer = StompFeel.BOSS_SHIELD_BREAK_HIT_STOP_S
                                shakeAmplitude = max(shakeAmplitude, StompFeel.SHAKE_MAX_PX)
                                shakeBias = StompFeel.ShakeBias.Vertical
                                shakeBiasFrames = StompFeel.SHAKE_MOMENTUM_BIAS_FRAMES
                                stompHapticNonce++
                                ParticleSpawners.stompKillBurst(
                                    particles,
                                    centerX = be.x + be.width * 0.5f,
                                    centerY = be.y + be.height * 0.42f,
                                )
                                soundManager?.playBossShieldBreak()
                                soundManager?.duckBgmOnHeavyStomp(0.2f, 240L)
                                bonusScore += 200
                            }
                            be.state == BossState.STUNNED || be.state == BossState.ENRAGED -> {
                                playerVelocityY = -400f
                                onGround = false
                                hitStopTimer = StompFeel.HIT_STOP_LIGHT_S
                                soundManager?.playShieldBounce()
                            }
                            else -> {
                                playerVelocityY = -400f
                                onGround = false
                                hitStopTimer = StompFeel.HIT_STOP_LIGHT_S
                                soundManager?.playShieldBounce()
                            }
                        }
                    }
                }
            }

            val stompedEnemy =
                if (bossStompHandled) {
                    null
                } else {
                    enemies.firstOrNull { enemy ->
                val enemyRect = Rect(enemy.x, enemy.y, enemy.x + enemy.width, enemy.y + enemy.height)
                playerRect.overlaps(enemyRect) &&
                    enemy.kind.canBeStomped() &&
                    playerVelocityY > 0f &&
                    playerRect.bottom <= enemyRect.top + enemy.height * 0.5f
            }
                }
            if (stompedEnemy != null) {
                val e = stompedEnemy
                if (e.hasIceShield && fishDashTimer <= 0f && tuanTuanAssistTimer <= 0f) {
                    playerVelocityY = -400f
                    onGround = false
                    hitStopTimer = StompFeel.HIT_STOP_LIGHT_S
                    shakeAmplitude = max(shakeAmplitude, StompFeel.SHAKE_LIGHT_PX)
                    soundManager?.playShieldBounce()
                } else {
                enemies.remove(e)
                tutorialDefeatedEnemy = true
                playerVelocityY = -720f
                onGround = false
                bonusScore += 25
                hitStopTimer = StompFeel.HIT_STOP_S
                shakeAmplitude = max(shakeAmplitude, StompFeel.SHAKE_MAX_PX)
                shakeBias = StompFeel.ShakeBias.None
                shakeBiasFrames = 0
                stompHapticNonce++
                ParticleSpawners.stompKillBurst(
                    particles,
                    centerX = e.x + e.width * 0.5f,
                    centerY = e.y + e.height * 0.42f,
                )
                soundManager?.playStomp()
                soundManager?.duckBgmOnHeavyStomp()
                }
            } else {
                if (!bossStompHandled) {
                val hitEnemyIndex = enemies.indexOfFirst { enemy ->
                    val enemyRect = Rect(enemy.x, enemy.y, enemy.x + enemy.width, enemy.y + enemy.height)
                    playerRect.overlaps(enemyRect)
                }
                if (hitEnemyIndex >= 0) {
                    if (fishDashTimer > 0f) {
                        val e = enemies[hitEnemyIndex]
                        val ecx = e.x + e.width * 0.5f
                        val ecy = e.y + e.height * 0.42f
                        fishDashTimer = 0f
                        enemies.removeAt(hitEnemyIndex)
                        tutorialDefeatedEnemy = true
                        playerVelocityY = if (e.hasIceShield) -420f else -320f
                        if (e.hasIceShield) {
                            hitStopTimer = StompFeel.HIT_STOP_S
                            shakeAmplitude = max(shakeAmplitude, StompFeel.SHAKE_MAX_PX)
                            shakeBias = StompFeel.ShakeBias.None
                            shakeBiasFrames = 0
                            stompHapticNonce++
                            ParticleSpawners.stompKillBurst(
                                particles,
                                centerX = ecx,
                                centerY = ecy,
                            )
                            soundManager?.playStomp()
                            soundManager?.duckBgmOnHeavyStomp()
                            bonusScore += 25
                        } else {
                            shakeAmplitude = max(shakeAmplitude, StompFeel.SHAKE_DASH_PX)
                            shakeBias = StompFeel.ShakeBias.Horizontal
                            shakeBiasFrames = StompFeel.SHAKE_MOMENTUM_BIAS_FRAMES
                            momentumHapticNonce++
                            ParticleSpawners.dashPierceBurst(
                                particles,
                                centerX = ecx,
                                centerY = ecy,
                                facingRight = playerFacingRight,
                            )
                        }
                    } else if (hasSnowShield) {
                        hasSnowShield = false
                        playerVelocityY = -260f
                        playerVelocityX = if (playerFacingRight) -180f else 180f
                        hitStopTimer = StompFeel.HIT_STOP_LIGHT_S
                        shakeAmplitude = max(shakeAmplitude, StompFeel.SHAKE_LIGHT_PX)
                    } else if (!isBossDefeatSequenceActive) {
                        endRun(win = false)
                        continue
                    }
                }
                }
            }

            if (!isBossFight) {
            friendGoal?.let { flag ->
                val flagRect = Rect(flag.x - 24f, flag.groundY - flag.height, flag.x + 48f, flag.groundY)
                if (playerRect.overlaps(flagRect)) {
                    endRun(win = true)
                    continue
                }
            }
            }

            if (onGround) {
                coyoteTimer = coyoteMax
            } else {
                coyoteTimer = max(0f, coyoteTimer - frameSeconds)
            }

            if (!isBossFight && levelNpcs.isNotEmpty() && !gameOver && !levelClear) {
                val pCx = playerX + size * 0.5f
                val pBot = playerY + size
                val n = levelNpcs.find { npc ->
                    val ncx = npc.x + npc.width * 0.5f
                    abs(ncx - pCx) < size * 1.2f && pBot >= npc.y - 4f && pBot <= groundY + 8f
                }
                if (n != null) {
                    npcBubbleText = n.line
                    npcBubbleTimer = 2.6f
                } else {
                    npcBubbleTimer = max(0f, npcBubbleTimer - frameSeconds)
                    if (npcBubbleTimer <= 0f) npcBubbleText = null
                }
            }
            if (mechanicHintText != null) {
                mechanicHintTimer = max(0f, mechanicHintTimer - frameSeconds)
                if (mechanicHintTimer <= 0f) mechanicHintText = null
            }

            if (companionDrorUnlocked) {
                val (nx, ny) = lerpDrorTowardTarget(
                    drorWorldX,
                    drorWorldY,
                    playerX,
                    playerY,
                    playerFacingRight,
                    size,
                    frameSeconds,
                    playerVelocityX,
                )
                drorWorldX = nx
                drorWorldY = ny
            }

            if (dashActive) {
                ParticleSpawners.fishDashTrail(
                    particles,
                    worldX = playerX,
                    worldY = playerY,
                    size = size,
                    facingRight = playerFacingRight,
                )
                ParticleSpawners.trySpawnDashPhantom(
                    dashPhantoms,
                    worldX = playerX,
                    worldY = playerY,
                    destSize = size,
                    facingRight = playerFacingRight,
                    animTick = spriteAnimTick,
                    isMoving = guguIsMovingHorizontally(
                        playerVelocityX,
                        abs(joystickX) > 0.12f,
                    ),
                )
            }
            updateDashPhantomsInPlace(dashPhantoms, frameSeconds)
            updateParticlesInPlace(particles, frameSeconds)

            score = coinsCollected * 10 + (playerX / 24f).toInt() + if (fishDashTimer > 0f) 25 else 0 + bonusScore
            val targetCam =
                if (isBossFight && activeLevel?.bossArena != null) {
                    (activeLevel!!.bossArena!!.arenaCenterWorldX - worldWidth * 0.35f).coerceIn(
                        0f,
                        max(levelLength - worldWidth, 0f),
                    )
                } else {
                    (playerX - worldWidth * 0.35f).coerceIn(0f, max(levelLength - worldWidth, 0f))
                }
            cameraX += (targetCam - cameraX) * min(1f, 12f * frameSeconds)
        }
    }

    val sceneTheme = activeLevel?.sceneTheme ?: defaultStorySceneTheme()
    val outerGradient = sceneTheme.outerGradientColors
    val pres = activeLevel?.presentation
    val tutorialHint =
        when {
            currentLevel != GameLevel.CedarVillageRuins || tutorialDefeatedEnemy -> null
            !tutorialMoved -> "先拖动摇杆左右试探，习惯加速与回中刹停的手感。"
            !tutorialJumped -> "现在试试跳跃。轻点更利落，按住会跳得更高。"
            !tutorialCollectedPowerUp -> "去顶一下问号箱，拿到第一个道具。"
            else -> "最后试着踩掉前方敌人，或者顶着护盾安全通过。"
        }
    val storyTags =
        buildList {
            add("主线")
            if (rescuedTuanTuan) add("团团")
            if (companionDrorUnlocked) add("Dror")
            if (fishDashTimer > 0f) add("鱼干冲刺")
            else if (hasBubbleScarf) add("泡泡围巾")
            else if (hasSnowShield) add("护盾")
            else if (gustBootsTimer > 0f) add("长跳")
            else if (auroraMagnetTimer > 0f) add("磁针")
        }
    Box(Modifier.fillMaxSize().background(Brush.verticalGradient(outerGradient))) {
        Column(Modifier.fillMaxSize()) {
            Box(Modifier.weight(1f).fillMaxWidth()) {
                Box(Modifier.fillMaxSize()) {
                    Canvas(Modifier.fillMaxSize().clickable { jump() }) {
                        worldWidth = size.width
                        worldHeight = size.height

                        val groundY = groundTop()
                        val hero = playerSize()
                        val playerScreenX = playerX - cameraX
                        val pitDepth = size.height - groundY

                        val stage = activeLevel
                        val theme = stage?.sceneTheme ?: defaultStorySceneTheme()

                        translate(renderShakeX, renderShakeY) {
                        val tf = Offset(
                            deviceTilt.x * StoryParallax.TILT_LAYER_FAR,
                            deviceTilt.y * StoryParallax.TILT_LAYER_FAR,
                        )
                        val tm = Offset(
                            deviceTilt.x * StoryParallax.TILT_LAYER_MID,
                            deviceTilt.y * StoryParallax.TILT_LAYER_MID,
                        )
                        val tFore = Offset(
                            deviceTilt.x * StoryParallax.TILT_LAYER_FORE,
                            deviceTilt.y * StoryParallax.TILT_LAYER_FORE,
                        )
                        drawStorySceneBackdrop(
                            theme = theme,
                            cameraX = cameraX,
                            globalAnim = globalAnim,
                            groundY = groundY,
                            tiltParallaxFar = tf,
                            tiltParallaxMid = tm,
                            tiltParallaxFore = tFore,
                        )

                        var segmentStart = 0f
                        pits.forEach { pit ->
                            val segmentEnd = pit.startX
                            val drawStart = segmentStart - cameraX
                            val drawWidth = segmentEnd - segmentStart
                            if (drawWidth > 0f) {
                                drawRect(theme.groundTopColor, Offset(drawStart, groundY), androidx.compose.ui.geometry.Size(drawWidth, pitDepth))
                                drawRect(theme.groundBottomColor, Offset(drawStart, groundY + 18f), androidx.compose.ui.geometry.Size(drawWidth, pitDepth))
                            }
                            drawRect(
                                color = theme.waterColor,
                                topLeft = Offset(pit.startX - cameraX, groundY),
                                size = androidx.compose.ui.geometry.Size(pit.endX - pit.startX, pitDepth)
                            )
                            segmentStart = pit.endX
                        }
                        if (segmentStart < levelLength) {
                            val drawStart = segmentStart - cameraX
                            val drawWidth = levelLength - segmentStart
                            drawRect(theme.groundTopColor, Offset(drawStart, groundY), androidx.compose.ui.geometry.Size(drawWidth, pitDepth))
                            drawRect(theme.groundBottomColor, Offset(drawStart, groundY + 18f), androidx.compose.ui.geometry.Size(drawWidth, pitDepth))
                        }

                        platforms.forEach { platform ->
                            val drawX = platform.x - cameraX
                            if (drawX + platform.width < -40f || drawX > size.width + 40f) return@forEach
                            val shiverX =
                                if (platform.isFragile && platform.fragileTimeLeft != null) {
                                    val prog =
                                        1f - (platform.fragileTimeLeft!! / FRAGILE_ICE_STAND_S).coerceIn(0f, 1f)
                                    sin(globalAnim * 28f) * 5.5f * prog
                                } else {
                                    0f
                                }
                            val top =
                                if (platform.isFragile) {
                                    Color(0xFFB3E5FC)
                                } else {
                                    theme.platformTopColor
                                }
                            val bot =
                                if (platform.isFragile) {
                                    Color(0xFF64B5F6)
                                } else {
                                    theme.platformBottomColor
                                }
                            drawRoundRect(
                                top,
                                Offset(drawX + shiverX, platform.y),
                                androidx.compose.ui.geometry.Size(platform.width, platform.height),
                                CornerRadius(16f, 16f),
                            )
                            drawRoundRect(
                                bot,
                                Offset(drawX + shiverX, platform.y + platform.height * 0.55f),
                                androidx.compose.ui.geometry.Size(platform.width, platform.height * 0.45f),
                                CornerRadius(12f, 12f),
                            )
                            if (platform.bounceImpulse > 0f) {
                                val by = platform.y + 7f
                                var x0 = drawX + shiverX + 10f
                                while (x0 < drawX + shiverX + platform.width - 14f) {
                                    val pathZ = Path().apply {
                                        moveTo(x0, by)
                                        lineTo(x0 + 5f, by + 5f)
                                        lineTo(x0 + 10f, by)
                                    }
                                    drawPath(pathZ, Color(0xFF5D4037), style = Stroke(width = 3f))
                                    x0 += 16f
                                }
                            }
                            if (platform.conveyorBelt != 0f) {
                                val up = platform.conveyorBelt > 0f
                                var ax = drawX + shiverX + 8f
                                val yArr = platform.y - 3f
                                while (ax < drawX + shiverX + platform.width - 16f) {
                                    val pth = Path().apply {
                                        if (up) {
                                            moveTo(ax, yArr)
                                            lineTo(ax + 9f, yArr)
                                            lineTo(ax + 4.5f, yArr - 4f)
                                        } else {
                                            moveTo(ax + 9f, yArr)
                                            lineTo(ax, yArr)
                                            lineTo(ax + 4.5f, yArr - 4f)
                                        }
                                        close()
                                    }
                                    drawPath(
                                        pth,
                                        Color(0xE0FFFFFF).copy(alpha = 0.55f),
                                    )
                                    ax += 24f
                                }
                            }
                        }

                        blocks.forEach { block ->
                            val drawX = block.x - cameraX
                            if (drawX + block.size < -20f || drawX > size.width + 20f) return@forEach
                            val drawY = block.y - block.bounceOffset
                            val blockColor = when {
                                block.type == BlockType.Question && !block.used -> theme.questionColor
                                block.type == BlockType.Question && block.used -> theme.questionUsedColor
                                else -> theme.brickColor
                            }
                            drawRoundRect(blockColor, Offset(drawX, drawY), androidx.compose.ui.geometry.Size(block.size, block.size), CornerRadius(10f, 10f))
                            drawRoundRect(theme.brickShadeColor, Offset(drawX, drawY + block.size * 0.55f), androidx.compose.ui.geometry.Size(block.size, block.size * 0.2f), CornerRadius(8f, 8f))
                            if (block.type == BlockType.Question && !block.used) {
                                val questionPath = Path().apply {
                                    moveTo(drawX + block.size * 0.35f, drawY + block.size * 0.28f)
                                    lineTo(drawX + block.size * 0.62f, drawY + block.size * 0.28f)
                                    lineTo(drawX + block.size * 0.62f, drawY + block.size * 0.46f)
                                    lineTo(drawX + block.size * 0.5f, drawY + block.size * 0.56f)
                                    lineTo(drawX + block.size * 0.5f, drawY + block.size * 0.66f)
                                }
                                drawPath(questionPath, theme.questionMarkColor, style = Stroke(width = 6f))
                                drawCircle(theme.questionMarkColor, block.size * 0.05f, Offset(drawX + block.size * 0.5f, drawY + block.size * 0.78f))
                            }
                        }

                        coins.forEach { coin ->
                            val drawX = coin.x - cameraX
                            val bob = sin(globalAnim * 2.2f + coin.x * 0.015f) * (coin.size * 0.08f)
                            val cy = coin.y + bob
                            val cx = drawX + coin.size / 2
                            val (glow, fill) = when (coin.kind) {
                                CoinKind.Normal -> Color(0xFFFFB300).copy(alpha = 0.35f) to Color(0xFFFFD54F)
                                CoinKind.Beacon -> Color(0xFF00BCD4).copy(alpha = 0.4f) to Color(0xFF4FC3F7)
                                CoinKind.LorePage -> Color(0xFFFF8F00).copy(alpha = 0.4f) to Color(0xFFFFE082)
                            }
                            drawCircle(glow, coin.size * 0.55f, Offset(cx, cy))
                            drawCircle(fill, coin.size / 2, Offset(cx, cy))
                            drawCircle(Color(0xFFFFF4C2), coin.size / 3.1f, Offset(cx, cy), style = Stroke(width = 4f))
                        }

                        worldPickups.forEach { wp ->
                            val wdx = wp.x - cameraX
                            if (wdx + wp.size < -16f || wdx > size.width + 16f) return@forEach
                            val bobW = sin(globalAnim * 2.5f + wp.x * 0.01f) * (wp.size * 0.06f)
                            val cwy = wp.y + bobW + wp.size * 0.5f
                            val cwx = wdx + wp.size * 0.5f
                            when (wp.kind) {
                                WorldPickupKind.Snowberry -> {
                                    drawCircle(Color(0x33E53935), wp.size * 0.5f, Offset(cwx, cwy))
                                    drawCircle(Color(0xFFD32F2F), wp.size * 0.4f, Offset(cwx, cwy))
                                    drawLine(
                                        Color(0xFF81C784),
                                        Offset(cwx - 2f, cwy - wp.size * 0.4f),
                                        Offset(cwx + 1f, cwy - wp.size * 0.5f),
                                        strokeWidth = 3f,
                                    )
                                }
                                WorldPickupKind.GustSeed -> {
                                    val leaf = Path().apply {
                                        moveTo(cwx, cwy - wp.size * 0.35f)
                                        quadraticTo(
                                            cwx + wp.size * 0.4f, cwy,
                                            cwx, cwy + wp.size * 0.3f
                                        )
                                        quadraticTo(
                                            cwx - wp.size * 0.4f, cwy,
                                            cwx, cwy - wp.size * 0.35f
                                        )
                                    }
                                    drawPath(leaf, Color(0xFF66BB6A))
                                }
                                WorldPickupKind.GlintFragment -> {
                                    for (i in 0..3) {
                                        val ang = i * 1.57f + globalAnim
                                        val rr = wp.size * 0.2f
                                        drawLine(
                                            Color(0xFFFFF59D).copy(alpha = 0.9f),
                                            Offset(cwx, cwy),
                                            Offset(cwx + cos(ang) * rr, cwy + sin(ang) * rr),
                                            strokeWidth = 3.5f,
                                        )
                                    }
                                    drawCircle(Color(0xFFFFEB3B), wp.size * 0.2f, Offset(cwx, cwy), style = Stroke(width = 2f))
                                }
                            }
                        }

                        floatingCoins.forEach { coin ->
                            val drawX = coin.x - cameraX
                            drawCircle(Color(0xFFFFD54F), coin.size / 2, Offset(drawX + coin.size / 2, coin.y + coin.size / 2))
                            drawCircle(Color.White.copy(alpha = 0.8f), coin.size / 3.4f, Offset(drawX + coin.size / 2, coin.y + coin.size / 2), style = Stroke(width = 4f))
                        }

                        friendGoal?.let { flag ->
                            val drawX = flag.x - cameraX
                            val poleX = drawX + hero * 0.42f
                            val poleTop = flag.groundY - flag.height - hero * 0.1f
                            drawOval(Color(0xFFEFEFF6), Offset(drawX, flag.groundY - flag.height), androidx.compose.ui.geometry.Size(hero * 0.9f, hero * 1.05f))
                            drawOval(Color(0xFF1D3146), Offset(drawX + hero * 0.18f, flag.groundY - flag.height + hero * 0.22f), androidx.compose.ui.geometry.Size(hero * 0.54f, hero * 0.58f))
                            drawCircle(Color(0xFFFFD54F), hero * 0.08f, Offset(drawX + hero * 0.44f, flag.groundY - flag.height + hero * 0.58f))
                            drawLine(Color(0xFF5D4037), Offset(poleX, flag.groundY), Offset(poleX, poleTop), strokeWidth = 7f)
                            val wave = sin(globalAnim * 3f) * 6f
                            val flagPath = Path().apply {
                                moveTo(poleX + 4f, poleTop + hero * 0.12f)
                                lineTo(poleX + hero * 0.95f + wave, poleTop + hero * 0.28f)
                                lineTo(poleX + hero * 0.72f + wave * 0.5f, poleTop + hero * 0.52f)
                                close()
                            }
                            drawPath(flagPath, Color(0xFFE53935))
                            drawPath(flagPath, Color(0xFFFFCDD2), style = Stroke(width = 3f))
                        }

                        impactWaves.forEach { w ->
                            val t = (w.life / w.maxLife).coerceIn(0.12f, 1f)
                            val l = w.centerX - w.halfWidth - cameraX
                            val waveW = w.halfWidth * 2f
                            if (l + waveW > -40f && l < size.width + 40f) {
                                drawRect(
                                    Color(0xFF4FC3F7).copy(alpha = 0.14f * t + 0.1f),
                                    topLeft = Offset(l, w.surfaceY - 10f),
                                    size = androidx.compose.ui.geometry.Size(waveW, 16f)
                                )
                                drawLine(
                                    Color(0xFFE0F7FA).copy(alpha = 0.5f * t + 0.2f),
                                    start = Offset(l, w.surfaceY),
                                    end = Offset(l + waveW, w.surfaceY),
                                    strokeWidth = 4f
                                )
                            }
                        }
                        if (isBossFight && takamatsuBoss.isEntityReady) {
                            val b = takamatsuBoss.entity
                            val bx = b.x - cameraX
                            if (bx + b.width > -40f && bx < size.width + 40f) {
                                val bossBreathY = 1f + 0.04f * sin(globalAnim * 6f)
                                val flashAlpha =
                                    (b.damageFlashTimer / BOSS_DAMAGE_FLASH_MAX_S).coerceIn(0f, 1f) * 0.82f
                                withTransform({
                                    translate(
                                        left = bx + b.width * 0.5f,
                                        top = b.y + b.height,
                                    )
                                    scale(scaleX = 1f, scaleY = bossBreathY, pivot = Offset.Zero)
                                    translate(left = -b.width * 0.5f, top = -b.height)
                                }) {
                                    drawRoundRect(
                                        Color(0xFFFAFAFA),
                                        Offset(0f, 0f),
                                        androidx.compose.ui.geometry.Size(b.width, b.height * 0.5f),
                                        CornerRadius(16f, 20f),
                                    )
                                    drawRoundRect(
                                        Color(0xFFE8E8E8),
                                        Offset(0f, b.height * 0.5f),
                                        androidx.compose.ui.geometry.Size(b.width, b.height * 0.5f),
                                        CornerRadius(14f, 16f),
                                    )
                                    if (b.hasShield) {
                                        drawCircle(
                                            Color(0x8829B6F6),
                                            min(b.width, b.height) * 0.7f,
                                            Offset(b.width * 0.5f, b.height * 0.42f),
                                            style = Stroke(width = 5f),
                                        )
                                    }
                                    if (flashAlpha > 0f) {
                                        drawRoundRect(
                                            Color.White.copy(alpha = flashAlpha),
                                            Offset(0f, 0f),
                                            androidx.compose.ui.geometry.Size(b.width, b.height),
                                            CornerRadius(12f, 14f),
                                        )
                                    }
                                }
                            }
                        }

                        enemies.forEach { enemy ->
                            val drawX = enemy.x - cameraX
                            when (enemy.kind) {
                                EnemyKind.Seal -> {
                                    drawRoundRect(theme.sealBodyColor, Offset(drawX, enemy.y), androidx.compose.ui.geometry.Size(enemy.width, enemy.height), CornerRadius(20f, 20f))
                                    drawOval(theme.sealBellyColor, Offset(drawX + enemy.width * 0.2f, enemy.y + enemy.height * 0.28f), androidx.compose.ui.geometry.Size(enemy.width * 0.6f, enemy.height * 0.42f))
                                    drawCircle(Color.Black, enemy.width * 0.05f, Offset(drawX + enemy.width * 0.35f, enemy.y + enemy.height * 0.32f))
                                    drawCircle(Color.Black, enemy.width * 0.05f, Offset(drawX + enemy.width * 0.65f, enemy.y + enemy.height * 0.32f))
                                }
                                EnemyKind.Bird -> {
                                    val wingPath = Path().apply {
                                        moveTo(drawX, enemy.y + enemy.height * 0.6f)
                                        lineTo(drawX + enemy.width * 0.32f, enemy.y + enemy.height * 0.08f)
                                        lineTo(drawX + enemy.width * 0.5f, enemy.y + enemy.height * 0.54f)
                                        lineTo(drawX + enemy.width * 0.68f, enemy.y + enemy.height * 0.08f)
                                        lineTo(drawX + enemy.width, enemy.y + enemy.height * 0.6f)
                                    }
                                    drawPath(wingPath, theme.birdWingColor, style = Stroke(width = 10f))
                                    drawOval(theme.birdBodyColor, Offset(drawX + enemy.width * 0.28f, enemy.y + enemy.height * 0.28f), androidx.compose.ui.geometry.Size(enemy.width * 0.44f, enemy.height * 0.4f))
                                    drawCircle(Color.Black, enemy.width * 0.05f, Offset(drawX + enemy.width * 0.5f, enemy.y + enemy.height * 0.42f))
                                }
                                EnemyKind.SpikedSeal -> {
                                    drawRoundRect(
                                        theme.sealBodyColor.copy(alpha = 0.92f),
                                        Offset(drawX, enemy.y),
                                        androidx.compose.ui.geometry.Size(enemy.width, enemy.height),
                                        CornerRadius(18f, 18f),
                                    )
                                    repeat(4) { si ->
                                        val spikePath =
                                            Path().apply {
                                                val sx = drawX + enemy.width * (0.1f + si * 0.2f)
                                                moveTo(sx, enemy.y + enemy.height * 0.15f)
                                                lineTo(sx + enemy.width * 0.08f, enemy.y - enemy.height * 0.12f)
                                                lineTo(sx + enemy.width * 0.16f, enemy.y + enemy.height * 0.15f)
                                                close()
                                            }
                                        drawPath(spikePath, Color(0xFFECEFF1))
                                    }
                                    drawOval(theme.sealBellyColor, Offset(drawX + enemy.width * 0.24f, enemy.y + enemy.height * 0.34f), androidx.compose.ui.geometry.Size(enemy.width * 0.5f, enemy.height * 0.28f))
                                }
                                EnemyKind.Owl -> {
                                    val owlWing =
                                        Path().apply {
                                            moveTo(drawX, enemy.y + enemy.height * 0.62f)
                                            lineTo(drawX + enemy.width * 0.26f, enemy.y + enemy.height * 0.06f)
                                            lineTo(drawX + enemy.width * 0.5f, enemy.y + enemy.height * 0.34f)
                                            lineTo(drawX + enemy.width * 0.74f, enemy.y + enemy.height * 0.06f)
                                            lineTo(drawX + enemy.width, enemy.y + enemy.height * 0.62f)
                                        }
                                    drawPath(owlWing, theme.birdWingColor, style = Stroke(width = 12f))
                                    drawOval(theme.birdBodyColor, Offset(drawX + enemy.width * 0.24f, enemy.y + enemy.height * 0.22f), androidx.compose.ui.geometry.Size(enemy.width * 0.52f, enemy.height * 0.5f))
                                    drawCircle(Color.White, enemy.width * 0.09f, Offset(drawX + enemy.width * 0.42f, enemy.y + enemy.height * 0.42f))
                                    drawCircle(Color.White, enemy.width * 0.09f, Offset(drawX + enemy.width * 0.58f, enemy.y + enemy.height * 0.42f))
                                    drawCircle(Color.Black, enemy.width * 0.03f, Offset(drawX + enemy.width * 0.42f, enemy.y + enemy.height * 0.42f))
                                    drawCircle(Color.Black, enemy.width * 0.03f, Offset(drawX + enemy.width * 0.58f, enemy.y + enemy.height * 0.42f))
                                }
                                EnemyKind.SnowMole -> {
                                    drawOval(
                                        theme.sealBodyColor.copy(alpha = 0.9f),
                                        Offset(drawX, enemy.y + enemy.height * 0.12f),
                                        androidx.compose.ui.geometry.Size(enemy.width, enemy.height * 0.72f),
                                    )
                                    drawOval(
                                        theme.sealBellyColor.copy(alpha = 0.85f),
                                        Offset(drawX + enemy.width * 0.18f, enemy.y + enemy.height * 0.46f),
                                        androidx.compose.ui.geometry.Size(enemy.width * 0.64f, enemy.height * 0.28f),
                                    )
                                    drawCircle(Color(0xFF1A2A33), enemy.width * 0.045f, Offset(drawX + enemy.width * 0.38f, enemy.y + enemy.height * 0.36f))
                                    drawCircle(Color(0xFF1A2A33), enemy.width * 0.045f, Offset(drawX + enemy.width * 0.62f, enemy.y + enemy.height * 0.36f))
                                    drawLine(
                                        Color(0xFFB7CED8),
                                        Offset(drawX + enemy.width * 0.08f, enemy.y + enemy.height * 0.78f),
                                        Offset(drawX + enemy.width * 0.92f, enemy.y + enemy.height * 0.78f),
                                        strokeWidth = 4f,
                                    )
                                }
                                EnemyKind.LowIceArch -> {
                                    val arch =
                                        Path().apply {
                                            moveTo(drawX, enemy.y + enemy.height)
                                            quadraticTo(
                                                drawX + enemy.width * 0.5f,
                                                enemy.y - enemy.height * 0.62f,
                                                drawX + enemy.width,
                                                enemy.y + enemy.height,
                                            )
                                        }
                                    drawPath(arch, Color(0xFFB3E5FC), style = Stroke(width = 11f))
                                    drawPath(arch, Color.White.copy(alpha = 0.7f), style = Stroke(width = 4f))
                                    drawLine(
                                        Color(0xAA64FFDA),
                                        Offset(drawX + enemy.width * 0.16f, enemy.y + enemy.height + 7f),
                                        Offset(drawX + enemy.width * 0.84f, enemy.y + enemy.height + 7f),
                                        strokeWidth = 3f,
                                    )
                                }
                            }
                            if (enemy.hasIceShield) {
                                drawCircle(
                                    Color(0x8829B6F6),
                                    min(enemy.width, enemy.height) * 0.62f,
                                    Offset(drawX + enemy.width * 0.5f, enemy.y + enemy.height * 0.42f),
                                    style = Stroke(width = 5f),
                                )
                            }
                        }

                        levelNpcs.forEach { npc ->
                            val ndx = npc.x - cameraX
                            if (ndx + npc.width < -20f || ndx > size.width + 20f) return@forEach
                            when (npc.kind) {
                                NpcKind.Sign -> {
                                    drawRoundRect(
                                        Color(0xFF5D4037),
                                        Offset(ndx + npc.width * 0.35f, npc.y - npc.height * 0.1f),
                                        androidx.compose.ui.geometry.Size(npc.width * 0.3f, npc.height * 1.1f),
                                        CornerRadius(4f, 4f),
                                    )
                                    drawRoundRect(
                                        Color(0xFFFFF8E1).copy(alpha = 0.92f),
                                        Offset(ndx + 2f, npc.y),
                                        androidx.compose.ui.geometry.Size(npc.width - 4f, npc.height * 0.75f),
                                        CornerRadius(6f, 6f),
                                    )
                                    drawLine(
                                        Color(0xFF6D4C41),
                                        Offset(ndx + npc.width * 0.2f, npc.y + npc.height * 0.35f),
                                        Offset(ndx + npc.width * 0.8f, npc.y + npc.height * 0.35f),
                                        strokeWidth = 2f,
                                    )
                                }
                                else -> {
                                    val sk =
                                        when (npc.kind) {
                                            NpcKind.Elder -> Color(0xFF5C6A72) to Color(0xFFCFD8DC)
                                            NpcKind.Scout -> Color(0xFF455A64) to Color(0xFFB0BEC5)
                                            NpcKind.Villager -> theme.sealBodyColor to theme.sealBellyColor
                                            NpcKind.Sign -> theme.sealBodyColor to theme.sealBellyColor
                                        }
                                    drawOval(
                                        sk.first,
                                        Offset(ndx + npc.width * 0.12f, npc.y + npc.height * 0.18f),
                                        androidx.compose.ui.geometry.Size(npc.width * 0.76f, npc.height * 0.58f),
                                    )
                                    drawOval(
                                        sk.second,
                                        Offset(ndx + npc.width * 0.22f, npc.y + npc.height * 0.38f),
                                        androidx.compose.ui.geometry.Size(npc.width * 0.56f, npc.height * 0.3f),
                                    )
                                    drawCircle(
                                        sk.first,
                                        npc.width * 0.2f,
                                        Offset(ndx + npc.width * 0.5f, npc.y + npc.height * 0.2f),
                                    )
                                }
                            }
                        }

                        fishSnacks.forEach { fishSnack ->
                            val drawX = fishSnack.x - cameraX
                            val centerY = fishSnack.y + fishSnack.size * 0.45f
                            val fishPath = Path().apply {
                                moveTo(drawX, centerY)
                                quadraticTo(drawX + fishSnack.size * 0.28f, fishSnack.y, drawX + fishSnack.size * 0.68f, centerY)
                                quadraticTo(drawX + fishSnack.size * 0.28f, fishSnack.y + fishSnack.size * 0.9f, drawX, centerY)
                                close()
                            }
                            drawPath(fishPath, theme.fishBodyColor)
                            drawRoundRect(theme.fishTailColor, Offset(drawX + fishSnack.size * 0.6f, fishSnack.y + fishSnack.size * 0.18f), androidx.compose.ui.geometry.Size(fishSnack.size * 0.26f, fishSnack.size * 0.52f), CornerRadius(10f, 10f))
                        }

                        translate(tFore.x, tFore.y) {
                            drawStageForegroundDecor(
                                groundY = groundY,
                                globalAnim = globalAnim,
                                palette = StageDecorPalette(
                                    glowColor = theme.sunCoreColor,
                                    mistColor = theme.foregroundMistColor,
                                    shardColor = theme.foregroundShardColor,
                                ),
                            )
                        }

                        particles.forEach { p ->
                            val px = p.x - cameraX
                            if (px > -32f && px < size.width + 32f) {
                                val t = (p.life / p.maxLife).coerceIn(0.05f, 1f)
                                val a = t * 0.95f
                                val rad = p.baseRadius * (0.35f + 0.65f * t)
                                drawCircle(
                                    p.color.copy(alpha = p.color.alpha * a),
                                    rad,
                                    Offset(px, p.y),
                                )
                            }
                        }

                        if (fishDashTimer > 0f) drawCircle(Color(0x55FFB74D), hero * 0.88f, Offset(playerScreenX + hero / 2, playerY + hero / 2))
                        if (hasBubbleScarf) drawCircle(Color(0x5539C5FF), hero * 0.96f, Offset(playerScreenX + hero / 2, playerY + hero / 2))
                        if (hasSnowShield) {
                            drawCircle(Color(0x5590CAF9), hero * 1.08f, Offset(playerScreenX + hero / 2, playerY + hero / 2), style = Stroke(width = 6f))
                        }
                        if (gustBootsTimer > 0f) {
                            drawCircle(Color(0x55FFF176), hero * 0.72f, Offset(playerScreenX + hero / 2, playerY + hero * 0.9f))
                        }
                        if (auroraMagnetTimer > 0f) {
                            drawCircle(
                                Color(0x6635F3FF),
                                hero * (1.18f + 0.08f * sin(globalAnim * 3f)),
                                Offset(playerScreenX + hero / 2, playerY + hero / 2),
                                style = Stroke(width = 4f),
                            )
                        }
                        if (tuanTuanAssistTimer > 0f) {
                            drawCircle(Color(0x55A5D6A7), hero * 1.08f, Offset(playerScreenX + hero / 2, playerY + hero / 2))
                            repeat(10) { si ->
                                val ang = globalAnim * 2.4f + si * 0.62f
                                val rr = hero * (0.9f + (si % 3) * 0.12f)
                                val sx = playerScreenX + hero * 0.5f + cos(ang) * rr
                                val sy = playerY + hero * 0.35f + sin(ang) * rr * 0.5f
                                drawCircle(Color.White.copy(alpha = 0.55f), 3f + (si % 2), Offset(sx, sy))
                            }
                        }

                        if (companionDrorUnlocked) {
                            val drx = drorWorldX - cameraX
                            if (drx > -hero && drx < size.width + hero) {
                                drawDrorCompanion(
                                    image = drorSprite,
                                    screenX = drx,
                                    screenY = drorWorldY,
                                    destWidth = hero * 0.42f,
                                    playerScreenX = playerScreenX,
                                    globalAnim = globalAnim,
                                )
                            }
                        }

                        val moving = guguIsMovingHorizontally(
                            playerVelocityX,
                            abs(joystickX) > 0.12f,
                        )
                        val crouchVisual = crouchPressed && onGround
                        val footH = if (crouchVisual) hero * 0.74f else null
                        val guguBreathY = 1f + 0.035f * sin(globalAnim * 5.5f)
                        for (ph in dashPhantoms) {
                            val sx = ph.worldX - cameraX
                            if (sx > -ph.destSize * 2 && sx < size.width + ph.destSize * 2) {
                                val pa = (ph.life / ph.maxLife).coerceIn(0f, 1f) * 0.5f
                                drawGuguCharacterSprite(
                                    image = guguSprite,
                                    layout = guguSpriteLayout,
                                    screenX = sx,
                                    screenY = ph.worldY,
                                    destSize = ph.destSize,
                                    globalAnim = globalAnim,
                                    animTick = ph.animTick,
                                    facingRight = ph.facingRight,
                                    isMoving = ph.isMoving,
                                    breathScaleY = 1f,
                                    overallAlpha = pa,
                                )
                            }
                        }
                        drawGuguCharacterSprite(
                            image = guguSprite,
                            layout = guguSpriteLayout,
                            screenX = playerScreenX,
                            screenY = playerY,
                            destSize = hero,
                            globalAnim = globalAnim,
                            animTick = spriteAnimTick,
                            facingRight = playerFacingRight,
                            isMoving = moving,
                            breathScaleY = guguBreathY,
                            footAnchoredHeight = footH,
                        )
                        }
                    }
                    if (!started) {
                        Box(
                            modifier = Modifier
                                .fillMaxSize()
                                .clickable { jump() },
                            contentAlignment = Alignment.Center,
                        ) {
                            OverlayCard(
                                title = pres?.introTitle ?: "咕咕嘎嘎",
                                description = pres?.introDescription ?: "点击屏幕开始。",
                            )
                        }
                    }

                    if (gameOver) {
                        val failDescription =
                            if (currentLevel == GameLevel.MistDike && isBossFight) {
                                "本次得分 $score，收集小鱼干 $coinsCollected。\n高松鹅有护盾时，先用鱼干冲刺或团团支援破盾，再寻找踩踏窗口。"
                            } else {
                                "本次得分 $score，收集小鱼干 $coinsCollected。\n${pres?.failHint ?: ""}"
                            }
                        Box(
                            modifier = Modifier
                                .fillMaxSize()
                                .clickable { jump() },
                            contentAlignment = Alignment.Center,
                        ) {
                            OverlayCard(
                                title = "闯关失败",
                                description = failDescription,
                            )
                        }
                    }

                    if (levelClear) {
                        Box(
                            modifier = Modifier
                                .fillMaxSize()
                                .clickable { jump() },
                            contentAlignment = Alignment.Center,
                        ) {
                            OverlayCard(
                                title = pres?.victoryTitle ?: "通关",
                                description = pres?.victoryDescription ?: "",
                            )
                        }
                    }

                    if (chapterPreviewVisible) {
                        val ct = pres?.chapterPreviewTitle
                        val cd = pres?.chapterPreviewDescription
                        if (ct != null && cd != null) {
                            Box(
                                modifier = Modifier
                                    .fillMaxSize()
                                    .clickable { jump() },
                                contentAlignment = Alignment.TopCenter,
                            ) {
                                ChapterPreviewCard(title = ct, description = cd)
                            }
                        }
                    }
                    if (campUnlockNoticeVisible) {
                        Box(
                            modifier = Modifier
                                .fillMaxSize()
                                .clickable {
                                    campUnlockNoticeVisible = false
                                    jump()
                                },
                            contentAlignment = Alignment.TopCenter,
                        ) {
                            ChapterPreviewCard(
                                title = "团团归队，营地开放",
                                description = "这次收集到的小鱼干已经存进补给营地。回到主页后可以先升级冲刺、团团支援或补给航道，再继续北上。",
                            )
                        }
                    }
                    if (started && running && !gameOver && !levelClear && !isBossDefeatSequenceActive) {
                        IconButton(
                            onClick = { isPaused = true },
                            modifier =
                                Modifier
                                    .align(Alignment.TopEnd)
                                    .statusBarsPadding()
                                    .padding(top = 8.dp, end = 8.dp)
                                    .size(48.dp),
                        ) {
                            Icon(
                                Icons.Rounded.Pause,
                                contentDescription = "暂停",
                                tint = Color(0xFFE3EEF8),
                            )
                        }
                    }
                    if (isPaused && started && !gameOver && !levelClear) {
                        PauseOverlay(
                            onResume = { isPaused = false },
                            onQuit = {
                                isPaused = false
                                onExitToMenu?.invoke()
                            },
                            soundManager = soundManager,
                            saveRepository = saveRepository,
                        )
                    }
                }
            }
            GameStageControlDock {
                Row(
                    modifier = Modifier.fillMaxWidth(),
                    verticalAlignment = Alignment.Bottom,
                    horizontalArrangement = Arrangement.SpaceBetween,
                ) {
                    GameVirtualJoystick(
                        onHorizontalChange = { joystickX = it },
                        resetKey = inputSession,
                        modifier = Modifier.padding(end = 4.dp, bottom = 2.dp),
                        totalSize = 124.dp,
                    )
                    Spacer(Modifier.weight(1f))
                    Column(
                        horizontalAlignment = Alignment.End,
                        verticalArrangement = Arrangement.spacedBy(6.dp),
                    ) {
                        HoldButton(
                            text = "跳跃",
                            modifier = Modifier.width(112.dp),
                            onPressedChange = { pressed ->
                                jumpPressed = pressed
                                if (pressed) jump()
                            }
                        )
                        GameActionHoldButton(
                            text = "蹲下",
                            modifier = Modifier.width(112.dp),
                            onPressedChange = { crouchPressed = it }
                        )
                    }
                }
                if (rescuedTuanTuan) {
                    HoldButton(
                        text = if (tuanTuanAssistReady) "团团支援" else "团团休息中",
                        modifier = Modifier.fillMaxWidth().padding(top = 8.dp),
                        onPressedChange = { pressed -> if (pressed) triggerTuanTuanAssist() }
                    )
                }
            }
        }
        if (!isBossDefeatSequenceActive) {
            Column(
                modifier = Modifier
                    .align(Alignment.TopStart)
                    .statusBarsPadding()
                    .fillMaxWidth()
                    .padding(horizontal = 8.dp, vertical = 4.dp)
            ) {
            Row(
                modifier = Modifier.fillMaxWidth(),
                horizontalArrangement = Arrangement.SpaceBetween,
                verticalAlignment = Alignment.Top
            ) {
                GameStageTopBar(
                    modifier = Modifier.weight(1f),
                    title = pres?.introTitle ?: "咕咕嘎嘎",
                    subtitle = pres?.hudGoalLine ?: "继续向北前进，找回失散的伙伴。",
                    tags = storyTags,
                    onBack = onExitToMenu,
                    accentColor = sceneTheme.sunCoreColor
                )
                ScoreBoard(
                    modifier = Modifier
                        .widthIn(max = 300.dp)
                        .padding(start = 8.dp),
                    score = score,
                    bestScore = bestScore,
                    coinsCollected = coinsCollected,
                    progress = if (levelLength <= 0f) 0 else ((playerX / levelLength) * 100).toInt().coerceIn(0, 100),
                    fishDashTimer = fishDashTimer,
                    hasBubbleScarf = hasBubbleScarf,
                    hasSnowShield = hasSnowShield,
                    gustBootsTimer = gustBootsTimer,
                    auroraMagnetTimer = auroraMagnetTimer,
                    rescuedTuanTuan = rescuedTuanTuan,
                    tuanTuanAssistReady = tuanTuanAssistReady,
                    tuanTuanAssistTimer = tuanTuanAssistTimer,
                    goalStatusLine = activeLevel?.presentation?.hudGoalLine ?: "准备出发…"
                )
            }
            if (isBossFight && takamatsuBoss.isEntityReady) {
                val b = takamatsuBoss.entity
                val hpFrac = (b.hp / b.maxHp).coerceIn(0.02f, 1f)
                LinearProgressIndicator(
                    progress = { hpFrac },
                    modifier = Modifier
                        .fillMaxWidth()
                        .padding(horizontal = 8.dp, vertical = 2.dp)
                        .height(7.dp),
                    color = Color(0xFFC62828),
                    trackColor = Color(0xFF12090A).copy(alpha = 0.5f),
                )
            }
            if (tutorialHint != null) {
                TutorialHintCard(
                    modifier = Modifier.padding(top = 6.dp),
                    title = "新手引导",
                    description = tutorialHint,
                )
            }
            if (npcBubbleText != null) {
                TutorialHintCard(
                    modifier = Modifier.padding(top = if (tutorialHint != null) 4.dp else 6.dp),
                    title = "路人与留言",
                    description = npcBubbleText!!,
                )
            }
            if (mechanicHintText != null) {
                TutorialHintCard(
                    modifier = Modifier.padding(top = if (tutorialHint != null || npcBubbleText != null) 4.dp else 6.dp),
                    title = "机制提示",
                    description = mechanicHintText!!,
                )
            }
            }
        }
    }
}

@Preview(showBackground = true)
@Composable
private fun GamePreview() {
    MyGameTheme(dynamicColor = false) {
        GuguGagaGame()
    }
}

private fun defaultStorySceneTheme() =
    StorySceneTheme(
        outerGradientColors = listOf(Color(0xFF5BA8F0), Color(0xFF8ECDFA), Color(0xFFD8EEFC), Color(0xFFEAF9FF)),
        skyGradientColors = listOf(Color(0xFF6EB8EA), Color(0xFF9FD4F5), Color(0xFFC8E8FF)),
        waterColor = Color(0xFF4F8CC9),
        groundTopColor = Color(0xFF6CCB5F),
        groundBottomColor = Color(0xFF7C4C29),
        sunCoreColor = Color(0xFFFFFDE7),
        sunHaloEdgeColor = Color(0xFFFFFDE7).copy(alpha = 0f),
        platformTopColor = Color(0xFFC57A34),
        platformBottomColor = Color(0xFF8D5524),
        brickColor = Color(0xFFB96D34),
        brickShadeColor = Color(0xFF7A4720),
        questionColor = Color(0xFFFFC64D),
        questionUsedColor = Color(0xFFB0A48E),
        questionMarkColor = Color(0xFF6B3E12),
        sealBodyColor = Color(0xFF7F98A8),
        sealBellyColor = Color(0xFFEFF7FB),
        birdWingColor = Color(0xFF57697A),
        birdBodyColor = Color(0xFF8AA0B2),
        fishBodyColor = Color(0xFFFF8A65),
        fishTailColor = Color(0xFFFFCC80),
        hillSnowColor = Color(0xFFD8F0FF),
        hillStoneColor = Color(0xFF9DB4C5),
        hutWallColor = Color(0x80F3FBFF),
        hutRoofColor = Color(0xFF8BC4E8),
        hutBeamColor = Color(0xFF5D7B8D),
        foregroundMistColor = Color(0xFFC8E8FF),
        foregroundShardColor = Color(0xFF6CCB5F),
        snowflakeAlphaBase = 0.22f,
        snowflakeAlphaStep = 0.06f,
        cloudCount = 6,
        ridgeCount = 7,
        hillCount = 5,
        hutCount = 4,
    )
