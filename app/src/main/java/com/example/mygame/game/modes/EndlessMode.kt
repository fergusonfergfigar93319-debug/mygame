package com.example.mygame.game.modes

import androidx.compose.foundation.Canvas
import androidx.compose.foundation.background
import androidx.compose.foundation.clickable
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.size
import androidx.compose.foundation.layout.statusBarsPadding
import androidx.compose.foundation.layout.widthIn
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
import androidx.compose.ui.Modifier
import androidx.compose.ui.geometry.CornerRadius
import androidx.compose.ui.geometry.Offset
import androidx.compose.ui.geometry.Rect
import androidx.compose.ui.graphics.Brush
import androidx.compose.ui.Alignment
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.graphics.ImageBitmap
import androidx.compose.ui.geometry.Size
import androidx.compose.ui.graphics.Path
import androidx.compose.ui.graphics.drawscope.Stroke
import androidx.compose.ui.graphics.drawscope.translate
import androidx.compose.ui.hapticfeedback.HapticFeedbackType
import androidx.compose.ui.platform.LocalHapticFeedback
import androidx.compose.ui.res.imageResource
import androidx.compose.ui.unit.dp
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.rounded.Pause
import androidx.compose.material3.Icon
import androidx.compose.material3.IconButton
import com.example.mygame.R
import com.example.mygame.audio.SoundManager
import com.example.mygame.data.LeaderboardRepository
import com.example.mygame.data.LeaderboardSubmitResult
import com.example.mygame.data.LocalLeaderboardRepository
import com.example.mygame.data.SaveRepository
import com.example.mygame.data.model.LeaderboardEntry
import com.example.mygame.game.BlockReward
import com.example.mygame.game.BlockType
import com.example.mygame.game.canBeStomped
import com.example.mygame.game.CoinKind
import com.example.mygame.game.EnemyKind
import com.example.mygame.game.FRAGILE_ICE_STAND_S
import com.example.mygame.game.FishSnack
import com.example.mygame.game.FloatingCoin
import com.example.mygame.game.GuguSpriteLayout
import com.example.mygame.game.horizontalGroundDampening
import com.example.mygame.game.DashPhantom
import com.example.mygame.game.Particle
import com.example.mygame.game.ParticleSpawners
import com.example.mygame.game.StompFeel
import com.example.mygame.game.standingSurfaceFriction
import com.example.mygame.game.drawDrorCompanion
import com.example.mygame.game.drawEndlessNightBackdrop
import com.example.mygame.game.drawEndlessSegmentAtmosphere
import com.example.mygame.game.drawGuguCharacterSprite
import com.example.mygame.game.drawStageForegroundDecor
import com.example.mygame.game.initialDrorPosition
import com.example.mygame.game.lerpDrorTowardTarget
import com.example.mygame.game.guguIsMovingHorizontally
import com.example.mygame.game.HoldButton
import com.example.mygame.game.updateDashPhantomsInPlace
import com.example.mygame.game.updateParticlesInPlace
import com.example.mygame.game.OverlayCard
import com.example.mygame.game.StageDecorPalette
import com.example.mygame.game.level.EndlessSegmentKind
import com.example.mygame.game.level.EndlessSegmentPool
import com.example.mygame.game.level.EndlessSegmentSpan
import com.example.mygame.game.level.offsetWorldX
import com.example.mygame.game.score.EndlessRunScoreBreakdown
import com.example.mygame.game.score.EndlessScoreBook
import com.example.mygame.ui.common.GameStageControlDock
import com.example.mygame.ui.common.GameStageTopBar
import com.example.mygame.ui.common.PauseOverlay
import com.example.mygame.ui.endless.EndlessHud
import com.example.mygame.ui.endless.EndlessSettlementOverlay
import kotlinx.coroutines.delay
import java.util.concurrent.ThreadLocalRandom
import kotlin.math.abs
import kotlin.math.cos
import kotlin.math.hypot
import kotlin.math.max
import kotlin.math.min
import kotlin.math.sin
import kotlin.random.Random

@Composable
fun EndlessMode(
    saveRepository: SaveRepository,
    leaderboardRepository: LeaderboardRepository,
    onExitToMenu: () -> Unit,
    runPreset: EndlessRunPreset = EndlessRunPreset.Casual,
    soundManager: SoundManager? = null,
    companionDrorUnlocked: Boolean = saveRepository.getCompanionDrorUnlocked(),
) {
    val rescuedTuanTuan = remember { saveRepository.getRescuedTuanTuan() }
    val segmentRng = remember(runPreset) { SegmentRng(runPreset) }
    val guguSprite = ImageBitmap.imageResource(R.drawable.gugu_sprite)
    val guguSpriteLayout = remember(guguSprite) { GuguSpriteLayout.singleImage(guguSprite) }
    val drorSprite = ImageBitmap.imageResource(R.drawable.companion_dror)

    var worldWidth by remember { mutableFloatStateOf(1f) }
    var worldHeight by remember { mutableFloatStateOf(1f) }
    var worldTailX by remember { mutableFloatStateOf(0f) }
    var lastRewardEndX by remember { mutableFloatStateOf(-8_000f) }
    var runElapsed by remember { mutableFloatStateOf(0f) }

    val segmentSpans = remember { mutableStateListOf<EndlessSegmentSpan>() }
    val pits = remember { mutableStateListOf<com.example.mygame.game.Pit>() }
    val platforms = remember { mutableStateListOf<com.example.mygame.game.Platform>() }
    val enemies = remember { mutableStateListOf<com.example.mygame.game.Enemy>() }
    val coins = remember { mutableStateListOf<com.example.mygame.game.Coin>() }
    val blocks = remember { mutableStateListOf<com.example.mygame.game.Block>() }
    val floatingCoins = remember { mutableStateListOf<FloatingCoin>() }
    val fishSnacks = remember { mutableStateListOf<FishSnack>() }
    val particles = remember { mutableStateListOf<Particle>() }
    val dashPhantoms = remember { mutableStateListOf<DashPhantom>() }
    var hitStopTimer by remember { mutableFloatStateOf(0f) }
    var shakeAmplitude by remember { mutableFloatStateOf(0f) }
    var shakeBias by remember { mutableStateOf(StompFeel.ShakeBias.None) }
    var shakeBiasFrames by remember { mutableIntStateOf(0) }
    var renderShakeX by remember { mutableFloatStateOf(0f) }
    var renderShakeY by remember { mutableFloatStateOf(0f) }

    val scoreBook = remember { EndlessScoreBook(EndlessBalanceConfig.scoring) }

    var playerX by remember { mutableFloatStateOf(100f) }
    var playerY by remember { mutableFloatStateOf(100f) }
    var drorWorldX by remember { mutableFloatStateOf(0f) }
    var drorWorldY by remember { mutableFloatStateOf(0f) }
    var playerVelocityX by remember { mutableFloatStateOf(0f) }
    var playerVelocityY by remember { mutableFloatStateOf(0f) }
    var onGround by remember { mutableStateOf(false) }
    var prevOnGround by remember { mutableStateOf(true) }
    var expectingLandingBonus by remember { mutableStateOf(false) }
    var landingsChain by remember { mutableIntStateOf(0) }

    var cameraX by remember { mutableFloatStateOf(0f) }
    var moveLeftPressed by remember { mutableStateOf(false) }
    var moveRightPressed by remember { mutableStateOf(false) }
    var jumpPressed by remember { mutableStateOf(false) }
    var playerFacingRight by remember { mutableStateOf(true) }
    var spriteAnimTick by remember { mutableIntStateOf(0) }

    var fishDashTimer by remember { mutableFloatStateOf(0f) }
    var hasBubbleScarf by remember { mutableStateOf(false) }
    var hasSnowShield by remember { mutableStateOf(false) }
    var gustBootsTimer by remember { mutableFloatStateOf(0f) }
    var auroraMagnetTimer by remember { mutableFloatStateOf(0f) }
    var tuanTuanAssistTimer by remember { mutableFloatStateOf(0f) }
    var tuanTuanAssistReady by remember { mutableStateOf(rescuedTuanTuan) }

    var coyoteTimer by remember { mutableFloatStateOf(0f) }
    val coyoteMax = 0.12f

    var started by remember { mutableStateOf(false) }
    var running by remember { mutableStateOf(false) }
    var gameOver by remember { mutableStateOf(false) }
    var isPaused by remember { mutableStateOf(false) }
    val isPausedState = rememberUpdatedState(isPaused)

    var globalAnim by remember { mutableFloatStateOf(0f) }
    var currentSegmentKind by remember { mutableStateOf<EndlessSegmentKind?>(null) }

    var submitResult by remember { mutableStateOf<LeaderboardSubmitResult?>(null) }
    var lastBreakdown by remember { mutableStateOf<EndlessRunScoreBreakdown?>(null) }
    var lastChallengeBucket by remember { mutableStateOf<String?>(null) }
    var lastDailyAttemptCount by remember { mutableStateOf<Int?>(null) }
    var previousDailyBestScore by remember { mutableStateOf<Int?>(null) }

    var stompHapticNonce by remember { mutableIntStateOf(0) }
    var momentumHapticNonce by remember { mutableIntStateOf(0) }
    val haptic = LocalHapticFeedback.current

    fun groundTop() = worldHeight * 0.82f
    fun playerSize() = min(worldWidth, worldHeight) * 0.1f

    fun intersectsPit(left: Float, right: Float): Boolean =
        pits.any { pit -> right > pit.startX && left < pit.endX }

    fun findSpan(px: Float): EndlessSegmentSpan? =
        segmentSpans.firstOrNull { px >= it.startX && px < it.endX }

    fun appendSegment() {
        if (worldWidth <= 1f) return
        val geom = EndlessSegmentPool.roll(
            worldWidth,
            worldHeight,
            runElapsed,
            segmentRng.random,
            playerX,
            lastRewardEndX,
            rewardSpacingWidthMultiplier = saveRepository.getCampRewardSpacingWidthMultiplier(),
        )
        val base = worldTailX
        val shifted = geom.offsetWorldX(base)
        val endX = base + geom.width
        segmentSpans.add(
            EndlessSegmentSpan(
                startX = base,
                endX = endX,
                kind = geom.kind,
                speedMultiplier = geom.speedMultiplier,
                blizzardIntensity = geom.blizzardIntensity,
            ),
        )
        worldTailX = endX
        if (geom.kind == EndlessSegmentKind.RewardSafe) {
            lastRewardEndX = worldTailX
        }
        pits.addAll(shifted.pits)
        platforms.addAll(shifted.platforms)
        enemies.addAll(shifted.enemies)
        coins.addAll(shifted.coins)
        blocks.addAll(shifted.blocks)
    }

    fun pruneWorld() {
        val cut = playerX - EndlessBalanceConfig.pruneDistanceBehindPlayer
        pits.removeAll { it.endX < cut }
        platforms.removeAll { it.x + it.width < cut }
        enemies.removeAll { it.x + it.width < cut }
        coins.removeAll { it.x + it.size < cut }
        blocks.removeAll { it.x + it.size < cut }
        segmentSpans.removeAll { it.endX < cut }
    }

    fun ensureWorld() {
        if (worldWidth <= 1f) return
        var guard = 0
        while (
            worldTailX < playerX + worldWidth * EndlessBalanceConfig.horizonAheadWidthMultiplier &&
            guard < EndlessBalanceConfig.maxSegmentsPerEnsure
        ) {
            appendSegment()
            guard++
        }
    }

    fun resetEndless() {
        segmentRng.reseed()
        worldTailX = 0f
        lastRewardEndX = -8_000f
        runElapsed = 0f
        segmentSpans.clear()
        pits.clear()
        platforms.clear()
        enemies.clear()
        coins.clear()
        blocks.clear()
        floatingCoins.clear()
        fishSnacks.clear()
        particles.clear()
        dashPhantoms.clear()
        hitStopTimer = 0f
        shakeAmplitude = 0f
        shakeBias = StompFeel.ShakeBias.None
        shakeBiasFrames = 0
        renderShakeX = 0f
        renderShakeY = 0f
        isPaused = false
        scoreBook.reset()
        fishDashTimer = 0f
        hasBubbleScarf = false
        hasSnowShield = false
        gustBootsTimer = 0f
        auroraMagnetTimer = 0f
        tuanTuanAssistTimer = 0f
        tuanTuanAssistReady = rescuedTuanTuan
        gameOver = false
        submitResult = null
        lastBreakdown = null
        lastChallengeBucket = null
        lastDailyAttemptCount = null
        previousDailyBestScore = null
        landingsChain = 0
        expectingLandingBonus = false
        prevOnGround = true
        repeat(2) { appendSegment() }
        val size = playerSize()
        playerX = worldWidth * 0.12f
        playerY = groundTop() - size
        playerVelocityX = 0f
        playerVelocityY = 0f
        onGround = true
        coyoteTimer = coyoteMax
        cameraX = 0f
        if (companionDrorUnlocked) {
            val (ix, iy) = initialDrorPosition(playerX, playerY, playerFacingRight, size)
            drorWorldX = ix
            drorWorldY = iy
        }
        started = true
        running = true
    }

    fun endRunDead() {
        if (gameOver) return
        running = false
        isPaused = false
        particles.clear()
        dashPhantoms.clear()
        hitStopTimer = 0f
        shakeAmplitude = 0f
        shakeBias = StompFeel.ShakeBias.None
        shakeBiasFrames = 0
        renderShakeX = 0f
        renderShakeY = 0f
        gameOver = true
        lastBreakdown = scoreBook.breakdown()
        val challengeBucket =
            if (runPreset == EndlessRunPreset.DailyChallenge) EndlessDailyChallenge.todayBucketLocal() else null
        lastChallengeBucket = challengeBucket
        previousDailyBestScore = challengeBucket?.let { saveRepository.getDailyChallengeBestScore(it) }
        lastDailyAttemptCount = challengeBucket?.let { saveRepository.incrementDailyChallengeAttempt(it) }
        val entry = LeaderboardEntry(
            id = LocalLeaderboardRepository.newEntryId(),
            playerId = saveRepository.getOrCreatePlayerId(),
            nickname = saveRepository.getPlayerNickname(),
            totalScore = lastBreakdown!!.finalTotal,
            distanceScoreUnits = playerX / 10f,
            fishSnacks = scoreBook.fishSnacksEaten,
            beaconCount = scoreBook.beacons,
            lorePageCount = scoreBook.lorePages,
            survivalSeconds = runElapsed,
            timestampMillis = System.currentTimeMillis(),
            rescuedTuanTuan = saveRepository.getRescuedTuanTuan(),
            challengeBucket = challengeBucket,
        )
        submitResult = leaderboardRepository.submit(entry)
        challengeBucket?.let { saveRepository.updateDailyChallengeBestScore(it, lastBreakdown!!.finalTotal) }
        if (scoreBook.fishSnacksEaten > 0) {
            saveRepository.addFishSnacks(scoreBook.fishSnacksEaten)
        }
    }

    fun jump() {
        if (!started) {
            if (worldWidth > 1f && worldHeight > 1f) resetEndless()
            return
        }
        if (gameOver) {
            resetEndless()
            return
        }
        if (onGround || coyoteTimer > 0f) {
            expectingLandingBonus = true
            playerVelocityY = if (gustBootsTimer > 0f) -1120f else -980f
            onGround = false
            coyoteTimer = 0f
            soundManager?.playJump()
        }
    }

    fun triggerAssist() {
        if (!rescuedTuanTuan || !tuanTuanAssistReady || !started || gameOver || !running) return
        tuanTuanAssistReady = false
        tuanTuanAssistTimer = saveRepository.getTuanAssistDurationSeconds()
        scoreBook.onAssistUsed()
    }

    LaunchedEffect(stompHapticNonce) {
        if (stompHapticNonce > 0) {
            haptic.performHapticFeedback(HapticFeedbackType.LongPress)
        }
    }

    LaunchedEffect(momentumHapticNonce) {
        if (momentumHapticNonce > 0) {
            haptic.performHapticFeedback(HapticFeedbackType.TextHandleMove)
        }
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

    LaunchedEffect(isPaused, started, gameOver) {
        if (!started || gameOver) return@LaunchedEffect
        if (isPaused) soundManager?.pauseBgm() else soundManager?.resumeBgm()
    }

    LaunchedEffect(running, worldWidth, worldHeight) {
        if (!running || worldWidth <= 1f || worldHeight <= 1f) return@LaunchedEffect

        val frameSeconds = 0.016f
        val gravity = 2100f
        val friction = 0.78f

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
            runElapsed += frameSeconds
            val dashActive = fishDashTimer > 0f
            fishDashTimer = max(0f, fishDashTimer - frameSeconds)
            gustBootsTimer = max(0f, gustBootsTimer - frameSeconds)
            auroraMagnetTimer = max(0f, auroraMagnetTimer - frameSeconds)
            tuanTuanAssistTimer = max(0f, tuanTuanAssistTimer - frameSeconds)

            ensureWorld()
            val span = findSpan(playerX + playerSize() * 0.5f)
            currentSegmentKind = span?.kind
            val speedMul = span?.speedMultiplier ?: 1f
            val blizzardMul =
                1f - (span?.blizzardIntensity ?: 0f) * EndlessBalanceConfig.blizzardRunSpeedPenaltyPerIntensity

            var runSpeed =
                (
                    when {
                        fishDashTimer > 0f -> 365f
                        gustBootsTimer > 0f -> 330f
                        else -> 290f
                    }
                ) * speedMul * blizzardMul

            val groundY = groundTop()
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

            playerVelocityX = when {
                moveLeftPressed && !moveRightPressed -> -runSpeed
                moveRightPressed && !moveLeftPressed -> runSpeed
                else -> playerVelocityX * horizontalDamp
            }
            if (abs(playerVelocityX) > 20f) {
                playerFacingRight = playerVelocityX > 0f
            }

            playerVelocityY += gravity * frameSeconds
            if (hasBubbleScarf && playerVelocityY > 180f && !onGround) {
                playerVelocityY *= 0.88f
            }
            if (!jumpPressed && playerVelocityY < -380f) {
                playerVelocityY *= 0.52f
            }

            val rightBound = worldTailX + EndlessBalanceConfig.fishBounceExtraAhead
            playerX = (playerX + playerVelocityX * frameSeconds).coerceIn(
                0f,
                max(worldTailX - size * EndlessBalanceConfig.playerTailMarginFactor, 0f),
            )

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
                                    scoreBook.onCoinPickup(CoinKind.Normal)
                                    soundManager?.playCoinPickup()
                                    floatingCoins += FloatingCoin(
                                        x = block.x + block.size * 0.25f,
                                        y = block.y - block.size * 0.35f,
                                        size = block.size * 0.5f,
                                        velocityY = -280f,
                                        life = 0.55f,
                                    )
                                }
                                BlockReward.Fish -> {
                                    blocks[index] = blocks[index].copy(used = true)
                                    fishSnacks += FishSnack(
                                        x = block.x + block.size * 0.08f,
                                        y = block.y + block.size * 0.2f,
                                        size = block.size * 0.82f,
                                        velocityX = 90f,
                                        velocityY = 0f,
                                        emerging = true,
                                        progress = 0f,
                                    )
                                }
                                BlockReward.Scarf -> {
                                    blocks[index] = blocks[index].copy(used = true)
                                    hasBubbleScarf = true
                                    soundManager?.playPowerUp()
                                }
                                BlockReward.Shield -> {
                                    blocks[index] = blocks[index].copy(used = true)
                                    hasSnowShield = true
                                    soundManager?.playPowerUp()
                                }
                                BlockReward.Boots -> {
                                    blocks[index] = blocks[index].copy(used = true)
                                    gustBootsTimer = 8f
                                    soundManager?.playPowerUp()
                                }
                                BlockReward.Magnet -> {
                                    blocks[index] = blocks[index].copy(used = true)
                                    auroraMagnetTimer = saveRepository.getMagnetDurationSeconds()
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
            } else {
                onGround = false
            }

            playerY = nextY

            if (onGround && !prevOnGround) {
                if (expectingLandingBonus) {
                    landingsChain++
                    if (landingsChain >= 3) {
                        scoreBook.onPerfectJumpChainBonus()
                        landingsChain = 0
                    }
                } else {
                    landingsChain = 0
                }
                expectingLandingBonus = false
            }
            prevOnGround = onGround

            if (playerY > worldHeight) {
                endRunDead()
                continue
            }

            val pBtm = playerY + size
            val pL = playerX
            val pR = playerX + size
            val epf = 8f
            for (pi in platforms.lastIndex downTo 0) {
                val p = platforms[pi]
                if (!p.isFragile) continue
                val standF =
                    onGround && pR > p.x && pL < p.x + p.width &&
                        pBtm >= p.y - epf && pBtm <= p.y + epf * 2f
                if (standF) {
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
                val effectiveSpeed = if (tuanTuanAssistTimer > 0f) enemy.speed * 0.2f else enemy.speed
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

            val nextFloat = mutableListOf<FloatingCoin>()
            floatingCoins.forEach { c ->
                val u = c.copy(
                    y = c.y + c.velocityY * frameSeconds,
                    velocityY = c.velocityY + 900f * frameSeconds,
                    life = c.life - frameSeconds,
                )
                if (u.life > 0f) nextFloat += u
            }
            floatingCoins.clear()
            floatingCoins.addAll(nextFloat)

            val nextFish = mutableListOf<FishSnack>()
            fishSnacks.forEach { fishSnack ->
                if (fishSnack.emerging) {
                    val progress = (fishSnack.progress + frameSeconds * 2f).coerceAtMost(1f)
                    val emergeY = fishSnack.y - fishSnack.size * 0.9f * frameSeconds * 2f
                    nextFish += fishSnack.copy(y = emergeY, progress = progress, emerging = progress < 1f)
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
                    }
                    if (fishLanded) {
                        fishY = fishLandingY
                        fishVelocityY = 0f
                    }
                    if (fishX <= 0f || fishX + fishSnack.size >= rightBound) {
                        fishVelocityX = -fishVelocityX
                    }
                    if (fishY <= worldHeight) {
                        nextFish += fishSnack.copy(
                            x = fishX.coerceIn(0f, rightBound - fishSnack.size),
                            y = fishY,
                            velocityX = fishVelocityX,
                            velocityY = fishVelocityY,
                            emerging = false,
                            progress = 1f,
                        )
                    }
                }
            }
            fishSnacks.clear()
            fishSnacks.addAll(nextFish)

            val playerRect = Rect(playerX, playerY, playerX + size, playerY + size)
            if (auroraMagnetTimer > 0f && coins.isNotEmpty()) {
                val magnetRadius = size * 5.2f
                val pullSpeed = 720f
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

            val collected = coins.filter { coin ->
                val r = Rect(coin.x, coin.y, coin.x + coin.size, coin.y + coin.size)
                playerRect.overlaps(r)
            }
            collected.forEach { scoreBook.onCoinPickup(it.kind) }
            if (collected.isNotEmpty()) {
                coins.removeAll(collected.toSet())
                soundManager?.playCoinPickup()
            }

            val eatenFish = fishSnacks.firstOrNull { fishSnack ->
                val r = Rect(fishSnack.x, fishSnack.y, fishSnack.x + fishSnack.size, fishSnack.y + fishSnack.size)
                playerRect.overlaps(r)
            }
            if (eatenFish != null) {
                val ex = eatenFish
                fishSnacks.remove(eatenFish)
                fishDashTimer = saveRepository.getFishDashDurationSeconds()
                scoreBook.onFishSnackEaten()
                ParticleSpawners.fishSnackBurst(
                    particles,
                    centerX = ex.x + ex.size * 0.5f,
                    centerY = ex.y + ex.size * 0.45f,
                )
                soundManager?.playEatFish()
            }

            val stompedEnemy = enemies.firstOrNull { enemy ->
                val er = Rect(enemy.x, enemy.y, enemy.x + enemy.width, enemy.y + enemy.height)
                playerRect.overlaps(er) &&
                    enemy.kind.canBeStomped() &&
                    playerVelocityY > 0f &&
                    playerRect.bottom <= er.top + enemy.height * 0.5f
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
                playerVelocityY = -720f
                onGround = false
                scoreBook.onStompEnemy()
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
                val hitEnemyIndex = enemies.indexOfFirst { enemy ->
                    val er = Rect(enemy.x, enemy.y, enemy.x + enemy.width, enemy.y + enemy.height)
                    playerRect.overlaps(er)
                }
                if (hitEnemyIndex >= 0) {
                    if (fishDashTimer > 0f) {
                        val e = enemies[hitEnemyIndex]
                        val ecx = e.x + e.width * 0.5f
                        val ecy = e.y + e.height * 0.42f
                        fishDashTimer = 0f
                        enemies.removeAt(hitEnemyIndex)
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
                            scoreBook.onStompEnemy()
                            soundManager?.playStomp()
                            soundManager?.duckBgmOnHeavyStomp()
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
                    } else {
                        endRunDead()
                        continue
                    }
                }
            }

            if (onGround) coyoteTimer = coyoteMax else coyoteTimer = max(0f, coyoteTimer - frameSeconds)

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
                        moveLeftPressed,
                        moveRightPressed,
                    ),
                )
            }
            updateDashPhantomsInPlace(dashPhantoms, frameSeconds)
            updateParticlesInPlace(particles, frameSeconds)

            scoreBook.tickFrame(playerX, frameSeconds)

            val targetCam = (playerX - worldWidth * 0.35f).coerceAtLeast(0f)
            cameraX += (targetCam - cameraX) * min(1f, 12f * frameSeconds)

            pruneWorld()
        }
    }

    val nightOuter = listOf(Color(0xFF1A237E), Color(0xFF283593), Color(0xFF3949AB), Color(0xFF5C6BC0))
    val endlessHeaderTitle = if (runPreset == EndlessRunPreset.DailyChallenge) "今日无尽" else "极夜漂流"
    val endlessTags =
        buildList {
            add(if (runPreset == EndlessRunPreset.DailyChallenge) "今日挑战" else "无尽")
            if (rescuedTuanTuan) add("团团")
            if (companionDrorUnlocked) add("Dror")
            if (runPreset == EndlessRunPreset.DailyChallenge) add(EndlessDailyChallenge.todayBucketLocal())
        }
    val topAccent = if (runPreset == EndlessRunPreset.DailyChallenge) Color(0xFF4DB6AC) else Color(0xFF90CAF9)
    val endlessSub = endlessSubtitle(runPreset, currentSegmentKind)

    Box(Modifier.fillMaxSize().background(Brush.verticalGradient(nightOuter))) {
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
                    val span = findSpan(playerX + hero * 0.5f)
                    val bzz = span?.blizzardIntensity ?: 0f

                    translate(renderShakeX, renderShakeY) {
                    drawEndlessNightBackdrop(cameraX, globalAnim, groundY, bzz)
                    repeat(34) { i ->
                        val seed = i * 7919f
                        val sx = (seed * 0.0007f % 1f) * size.width
                        val sy = (seed * 0.0011f % 1f) * size.height * 0.75f
                        val drift = globalAnim * (14f + (i % 4) * 3f) + i * 19f
                        val px = (sx + sin(drift * 0.07f) * 22f + drift * 7f) % (size.width + 28f) - 14f
                        val py = (sy + drift * 6f % (size.height * 0.85f))
                        drawCircle(Color.White.copy(alpha = 0.18f + bzz * 0.25f), 1.8f + (i % 3), Offset(px, py))
                    }

                    var segmentStart = 0f
                    pits.forEach { pit ->
                        val segmentEnd = pit.startX
                        val drawStart = segmentStart - cameraX
                        val drawWidth = segmentEnd - segmentStart
                        if (drawWidth > 0f) {
                            drawRect(Color(0xFF90A4AE), Offset(drawStart, groundY), androidx.compose.ui.geometry.Size(drawWidth, pitDepth))
                            drawRect(Color(0xFF546E7A), Offset(drawStart, groundY + 16f), androidx.compose.ui.geometry.Size(drawWidth, pitDepth))
                        }
                        drawRect(
                            color = Color(0xFF1C3554),
                            topLeft = Offset(pit.startX - cameraX, groundY),
                            size = androidx.compose.ui.geometry.Size(pit.endX - pit.startX, pitDepth),
                        )
                        segmentStart = pit.endX
                    }
                    if (segmentStart < worldTailX) {
                        val drawStart = segmentStart - cameraX
                        val drawWidth = worldTailX - segmentStart
                        drawRect(Color(0xFF90A4AE), Offset(drawStart, groundY), androidx.compose.ui.geometry.Size(drawWidth, pitDepth))
                        drawRect(Color(0xFF546E7A), Offset(drawStart, groundY + 16f), androidx.compose.ui.geometry.Size(drawWidth, pitDepth))
                    }

                    platforms.forEach { platform ->
                        val drawX = platform.x - cameraX
                        if (drawX + platform.width < -40f || drawX > size.width + 40f) return@forEach
                        val shx =
                            if (platform.isFragile && platform.fragileTimeLeft != null) {
                                val prog =
                                    1f - (platform.fragileTimeLeft!! / FRAGILE_ICE_STAND_S).coerceIn(0f, 1f)
                                sin(globalAnim * 28f) * 5.5f * prog
                            } else {
                                0f
                            }
                        val col = if (platform.isFragile) Color(0xFFB3E5FC) else Color(0xFF78909C)
                        drawRoundRect(col, Offset(drawX + shx, platform.y), androidx.compose.ui.geometry.Size(platform.width, platform.height), CornerRadius(14f, 14f))
                    }
                    blocks.forEach { block ->
                        val drawX = block.x - cameraX
                        if (drawX + block.size < -20f || drawX > size.width + 20f) return@forEach
                        val drawY = block.y - block.bounceOffset
                        val blockColor = when {
                            block.type == BlockType.Question && !block.used -> Color(0xFFFFCA28)
                            block.type == BlockType.Question && block.used -> Color(0xFF8D6E63)
                            else -> Color(0xFF6D4C41)
                        }
                        drawRoundRect(blockColor, Offset(drawX, drawY), androidx.compose.ui.geometry.Size(block.size, block.size), CornerRadius(8f, 8f))
                    }
                    coins.forEach { coin ->
                        val drawX = coin.x - cameraX
                        val bob = sin(globalAnim * 2.2f + coin.x * 0.012f) * (coin.size * 0.08f)
                        val cy = coin.y + bob
                        val cx = drawX + coin.size / 2
                        val (fill, glow) = when (coin.kind) {
                            CoinKind.Normal -> Color(0xFFFFD54F) to Color(0xFFFFB300).copy(alpha = 0.35f)
                            CoinKind.Beacon -> Color(0xFF4FC3F7) to Color(0xFF00BCD4).copy(alpha = 0.45f)
                            CoinKind.LorePage -> Color(0xFFFFE082) to Color(0xFFFF8F00).copy(alpha = 0.4f)
                        }
                        drawCircle(glow, coin.size * 0.55f, Offset(cx, cy))
                        drawCircle(fill, coin.size / 2, Offset(cx, cy))
                    }
                    enemies.forEach { enemy ->
                        val drawX = enemy.x - cameraX
                        when (enemy.kind) {
                            EnemyKind.Seal -> {
                                drawRoundRect(Color(0xFF607D8B), Offset(drawX, enemy.y), androidx.compose.ui.geometry.Size(enemy.width, enemy.height), CornerRadius(18f, 18f))
                            }
                            EnemyKind.Bird -> {
                                drawOval(Color(0xFF78909C), Offset(drawX + enemy.width * 0.25f, enemy.y + enemy.height * 0.28f), androidx.compose.ui.geometry.Size(enemy.width * 0.5f, enemy.height * 0.45f))
                            }
                            EnemyKind.SpikedSeal -> {
                                drawRoundRect(Color(0xFF607D8B), Offset(drawX, enemy.y), androidx.compose.ui.geometry.Size(enemy.width, enemy.height), CornerRadius(18f, 18f))
                                repeat(4) { si ->
                                    val spike = Path().apply {
                                        val sx = drawX + enemy.width * (0.12f + si * 0.18f)
                                        moveTo(sx, enemy.y + enemy.height * 0.18f)
                                        lineTo(sx + enemy.width * 0.07f, enemy.y - enemy.height * 0.1f)
                                        lineTo(sx + enemy.width * 0.14f, enemy.y + enemy.height * 0.18f)
                                        close()
                                    }
                                    drawPath(spike, Color(0xFFECEFF1))
                                }
                            }
                            EnemyKind.Owl -> {
                                val owlWing = Path().apply {
                                    moveTo(drawX, enemy.y + enemy.height * 0.62f)
                                    lineTo(drawX + enemy.width * 0.26f, enemy.y + enemy.height * 0.06f)
                                    lineTo(drawX + enemy.width * 0.5f, enemy.y + enemy.height * 0.34f)
                                    lineTo(drawX + enemy.width * 0.74f, enemy.y + enemy.height * 0.06f)
                                    lineTo(drawX + enemy.width, enemy.y + enemy.height * 0.62f)
                                }
                                drawPath(owlWing, Color(0xFF607D8B), style = Stroke(width = 10f))
                                drawOval(Color(0xFF90A4AE), Offset(drawX + enemy.width * 0.22f, enemy.y + enemy.height * 0.2f), androidx.compose.ui.geometry.Size(enemy.width * 0.56f, enemy.height * 0.5f))
                            }
                            EnemyKind.SnowMole -> {
                                drawOval(Color(0xFF546E7A), Offset(drawX, enemy.y + enemy.height * 0.12f), androidx.compose.ui.geometry.Size(enemy.width, enemy.height * 0.72f))
                                drawOval(Color(0xFFE0F2F1), Offset(drawX + enemy.width * 0.18f, enemy.y + enemy.height * 0.46f), androidx.compose.ui.geometry.Size(enemy.width * 0.64f, enemy.height * 0.28f))
                                drawCircle(Color(0xFF102530), enemy.width * 0.045f, Offset(drawX + enemy.width * 0.38f, enemy.y + enemy.height * 0.36f))
                                drawCircle(Color(0xFF102530), enemy.width * 0.045f, Offset(drawX + enemy.width * 0.62f, enemy.y + enemy.height * 0.36f))
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
                    fishSnacks.forEach { fishSnack ->
                        val drawX = fishSnack.x - cameraX
                        val centerY = fishSnack.y + fishSnack.size * 0.45f
                        val fishPath = Path().apply {
                            moveTo(drawX, centerY)
                            quadraticTo(drawX + fishSnack.size * 0.28f, fishSnack.y, drawX + fishSnack.size * 0.68f, centerY)
                            quadraticTo(drawX + fishSnack.size * 0.28f, fishSnack.y + fishSnack.size * 0.9f, drawX, centerY)
                            close()
                        }
                        drawPath(fishPath, Color(0xFFFFAB91))
                    }
                    drawStageForegroundDecor(
                        groundY = groundY,
                        globalAnim = globalAnim,
                        palette = StageDecorPalette(
                            glowColor = Color(0xFFE8EAF6),
                            mistColor = Color(0xFFB3E5FC),
                            shardColor = Color(0xFF81D4FA),
                        ),
                    )
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
                    if (hasSnowShield) drawCircle(Color(0x5590CAF9), hero * 1.06f, Offset(playerScreenX + hero / 2, playerY + hero / 2), style = Stroke(width = 5f))
                    if (gustBootsTimer > 0f) drawCircle(Color(0x55FFF176), hero * 0.72f, Offset(playerScreenX + hero / 2, playerY + hero * 0.9f))
                    if (auroraMagnetTimer > 0f) {
                        drawCircle(
                            Color(0x6635F3FF),
                            hero * (1.18f + 0.08f * sin(globalAnim * 3f)),
                            Offset(playerScreenX + hero / 2, playerY + hero / 2),
                            style = Stroke(width = 4f),
                        )
                    }
                    if (tuanTuanAssistTimer > 0f) {
                        drawCircle(Color(0x55A5D6A7), hero * 1.05f, Offset(playerScreenX + hero / 2, playerY + hero / 2))
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
                        moveLeftPressed,
                        moveRightPressed,
                    )
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
                    )
                    drawEndlessSegmentAtmosphere(
                        kind = currentSegmentKind,
                        blizzardIntensity = bzz,
                        globalAnim = globalAnim,
                        runElapsed = runElapsed,
                    )
                    }
                }

                if (started && running && !gameOver) {
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
                if (isPaused && started && !gameOver) {
                    PauseOverlay(
                        onResume = { isPaused = false },
                        onQuit = {
                            isPaused = false
                            onExitToMenu()
                        },
                        soundManager = soundManager,
                        saveRepository = saveRepository,
                    )
                }

                if (!started) {
                    Box(
                        modifier = Modifier
                            .fillMaxSize()
                            .clickable { jump() },
                        contentAlignment = Alignment.Center,
                    ) {
                        OverlayCard(
                            title = if (runPreset == EndlessRunPreset.DailyChallenge) "今日无尽" else "极夜漂流",
                            description = if (runPreset == EndlessRunPreset.DailyChallenge) {
                                val b = EndlessDailyChallenge.todayBucketLocal()
                                "本日地图种子固定（$b），便于公平比拼。\n与休闲无尽规则相同，成绩会计入「今日挑战」桶。\n点击屏幕开始。"
                            } else {
                                "暴风雪中的碎冰航道。\n搜集补给、信标与残页，撑过更久、走得更远。\n点击屏幕开始。"
                            },
                        )
                    }
                }
                if (gameOver && lastBreakdown != null) {
                    Box(modifier = Modifier.fillMaxSize()) {
                        Box(
                            modifier = Modifier
                                .fillMaxSize()
                                .background(Color(0x99000000)),
                        )
                        Box(
                            modifier = Modifier.fillMaxSize(),
                            contentAlignment = Alignment.Center,
                        ) {
                            EndlessSettlementOverlay(
                                breakdown = lastBreakdown!!,
                                submitResult = submitResult,
                                distanceUnits = playerX / 10f,
                                survivalSeconds = runElapsed,
                                playerIdShort = saveRepository.getOrCreatePlayerId().take(8),
                                challengeBucket = lastChallengeBucket,
                                dailyAttemptCount = lastDailyAttemptCount,
                                previousDailyBestScore = previousDailyBestScore,
                                onRestart = { resetEndless() },
                            )
                        }
                    }
                }
            }
        }
        GameStageControlDock {
            Row(modifier = Modifier.fillMaxWidth(), horizontalArrangement = Arrangement.spacedBy(10.dp)) {
                HoldButton(modifier = Modifier.weight(1f), text = "左移", onPressedChange = { moveLeftPressed = it })
                HoldButton(
                    modifier = Modifier.weight(1f),
                    text = "跳跃",
                    onPressedChange = { pressed ->
                        jumpPressed = pressed
                        if (pressed) jump()
                    },
                )
                HoldButton(modifier = Modifier.weight(1f), text = "右移", onPressedChange = { moveRightPressed = it })
            }
            if (rescuedTuanTuan) {
                HoldButton(
                    modifier = Modifier.fillMaxWidth(),
                    text = if (tuanTuanAssistReady) "团团支援" else "团团休息中",
                    onPressedChange = { p -> if (p) triggerAssist() },
                )
            }
        }
        }
        Column(
            modifier = Modifier
                .align(Alignment.TopStart)
                .statusBarsPadding()
                .fillMaxWidth()
                .padding(horizontal = 8.dp, vertical = 4.dp)
        ) {
            GameStageTopBar(
                title = endlessHeaderTitle,
                subtitle = endlessSub,
                tags = endlessTags,
                onBack = onExitToMenu,
                accentColor = topAccent
            )
            EndlessHud(
                modifier = Modifier
                    .fillMaxWidth()
                    .padding(top = 6.dp),
                totalScore = scoreBook.totalScore(),
                distanceUnits = playerX / 10f,
                survivalSeconds = runElapsed,
                multiplier = scoreBook.survivalMultiplier(),
                fishSnacks = scoreBook.fishSnacksEaten,
                beacons = scoreBook.beacons,
                lorePages = scoreBook.lorePages,
                distanceScore = scoreBook.distanceScore,
                collectionScore = scoreBook.collectionScore,
                actionScore = scoreBook.actionScore,
                fishDashActive = fishDashTimer > 0f,
                hasScarf = hasBubbleScarf,
                magnetActive = auroraMagnetTimer > 0f,
                assistReady = tuanTuanAssistReady,
                assistTimer = tuanTuanAssistTimer,
                currentSegmentKind = currentSegmentKind,
            )
        }
    }
}

/** 无尽路面拼接专用 RNG：休闲每局新种子，每日挑战每局从当日种子重置。 */
private fun endlessSubtitle(
    preset: EndlessRunPreset,
    kind: EndlessSegmentKind?,
): String {
    val area =
        when (kind) {
            EndlessSegmentKind.FlatChase -> "平地追逐航道"
            EndlessSegmentKind.PitJump -> "裂谷跳跃航道"
            EndlessSegmentKind.ThinIceGlide -> "薄冰滑行航道"
            EndlessSegmentKind.BlizzardLowVis -> "风雪低能见航道"
            EndlessSegmentKind.RewardSafe -> "补给休整航道"
            EndlessSegmentKind.DangerMixed -> "混合危险航道"
            EndlessSegmentKind.BranchChoice -> "双选险路航道"
            null -> "等待航道生成"
        }
    return if (preset == EndlessRunPreset.DailyChallenge) {
        "固定种子挑战：$area"
    } else {
        "自由无尽挑战：$area"
    }
}

private class SegmentRng(private val preset: EndlessRunPreset) {
    var random: Random = create()
        private set

    fun reseed() {
        random = create()
    }

    private fun create(): Random = when (preset) {
        EndlessRunPreset.Casual -> Random(ThreadLocalRandom.current().nextLong())
        EndlessRunPreset.DailyChallenge -> Random(EndlessDailyChallenge.seedForToday())
    }
}
