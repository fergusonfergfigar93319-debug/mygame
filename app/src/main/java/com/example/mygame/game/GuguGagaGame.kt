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
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.statusBarsPadding
import androidx.compose.foundation.layout.widthIn
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.runtime.LaunchedEffect
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableFloatStateOf
import androidx.compose.runtime.mutableIntStateOf
import androidx.compose.runtime.mutableStateListOf
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.remember
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
import androidx.compose.ui.platform.LocalContext
import androidx.compose.ui.res.imageResource
import androidx.compose.ui.tooling.preview.Preview
import androidx.compose.ui.unit.dp
import com.example.mygame.R
import com.example.mygame.audio.SoundManager
import com.example.mygame.data.SaveRepository
import com.example.mygame.game.CoinKind
import com.example.mygame.game.level.GameLevel
import com.example.mygame.game.level.LevelCatalog
import com.example.mygame.game.level.LevelContent
import com.example.mygame.game.level.StorySceneTheme
import com.example.mygame.ui.common.GameStageControlDock
import com.example.mygame.ui.common.GameStageTopBar
import com.example.mygame.ui.theme.MyGameTheme
import kotlinx.coroutines.delay
import kotlin.math.abs
import kotlin.math.cos
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
    var tuanTuanAssistTimer by remember { mutableFloatStateOf(0f) }
    var tuanTuanAssistReady by remember { mutableStateOf(false) }
    var chapterPreviewVisible by remember { mutableStateOf(false) }
    var coyoteTimer by remember { mutableFloatStateOf(0f) }
    var jumpPressed by remember { mutableStateOf(false) }
    var bonusScore by remember { mutableIntStateOf(0) }
    var globalAnim by remember { mutableFloatStateOf(0f) }
    var tutorialMoved by remember { mutableStateOf(false) }
    var tutorialJumped by remember { mutableStateOf(false) }
    var tutorialCollectedPowerUp by remember { mutableStateOf(false) }
    var tutorialDefeatedEnemy by remember { mutableStateOf(false) }

    val coyoteMax = 0.12f

    var worldWidth by remember { mutableFloatStateOf(1f) }
    var worldHeight by remember { mutableFloatStateOf(1f) }
    var levelLength by remember { mutableFloatStateOf(2400f) }
    var cameraX by remember { mutableFloatStateOf(0f) }

    var playerX by remember { mutableFloatStateOf(100f) }
    var playerY by remember { mutableFloatStateOf(100f) }
    var playerVelocityX by remember { mutableFloatStateOf(0f) }
    var playerVelocityY by remember { mutableFloatStateOf(0f) }
    var onGround by remember { mutableStateOf(false) }

    var moveLeftPressed by remember { mutableStateOf(false) }
    var moveRightPressed by remember { mutableStateOf(false) }
    var playerFacingRight by remember { mutableStateOf(true) }
    var spriteAnimTick by remember { mutableIntStateOf(0) }

    val platforms = remember { mutableStateListOf<Platform>() }
    val pits = remember { mutableStateListOf<Pit>() }
    val enemies = remember { mutableStateListOf<Enemy>() }
    val coins = remember { mutableStateListOf<Coin>() }
    val blocks = remember { mutableStateListOf<Block>() }
    val floatingCoins = remember { mutableStateListOf<FloatingCoin>() }
    val fishSnacks = remember { mutableStateListOf<FishSnack>() }
    var friendGoal by remember { mutableStateOf<FriendGoal?>(null) }
    val particles = remember { mutableStateListOf<Particle>() }
    var hitStopTimer by remember { mutableFloatStateOf(0f) }
    var shakeTimer by remember { mutableFloatStateOf(0f) }
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
        hitStopTimer = 0f
        shakeTimer = 0f
        renderShakeX = 0f
        renderShakeY = 0f
        friendGoal = content.friendGoal
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
        tuanTuanAssistTimer = 0f
        tuanTuanAssistReady = rescuedTuanTuan
        chapterPreviewVisible = false
        jumpPressed = false
        bonusScore = 0
        particles.clear()
        hitStopTimer = 0f
        shakeTimer = 0f
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
            playerVelocityY = if (gustBootsTimer > 0f) -1120f else -980f
            onGround = false
            coyoteTimer = 0f
            soundManager?.playJump()
        }
    }

    fun endRun(win: Boolean) {
        running = false
        particles.clear()
        hitStopTimer = 0f
        shakeTimer = 0f
        renderShakeX = 0f
        renderShakeY = 0f
        gameOver = !win
        levelClear = win
        if (win) {
            persistTuanTuanRescue()
            tuanTuanAssistReady = true
            chapterPreviewVisible = activeLevel?.presentation?.chapterPreviewTitle != null
            if (currentLevel == GameLevel.CedarVillageRuins) {
                saveRepository.saveCompanionDrorUnlocked()
                companionDrorUnlocked = true
            }
        }
        persistBestScore()
    }

    fun triggerTuanTuanAssist() {
        if (!rescuedTuanTuan || !tuanTuanAssistReady || !started || gameOver || levelClear) return
        tuanTuanAssistReady = false
        tuanTuanAssistTimer = 4.5f
    }

    LaunchedEffect(worldWidth, worldHeight) {
        if (worldWidth > 1f && worldHeight > 1f && !started) {
            rebuildLevel()
            resetPlayerPosition()
        }
    }

    LaunchedEffect(Unit) {
        while (true) {
            delay(16)
            if (hitStopTimer <= 0f) {
                globalAnim += 0.05f
            }
        }
    }

    LaunchedEffect(running, started) {
        while (running && started) {
            delay(100)
            if (hitStopTimer <= 0f) {
                spriteAnimTick++
            }
        }
    }

    LaunchedEffect(running, worldWidth, worldHeight, fishDashTimer, hasBubbleScarf, hasSnowShield, gustBootsTimer, tuanTuanAssistTimer) {
        if (!running || worldWidth <= 1f || worldHeight <= 1f) return@LaunchedEffect

        val frameSeconds = 0.016f
        val gravity = 2100f
        val runSpeed =
            when {
                fishDashTimer > 0f -> 365f
                gustBootsTimer > 0f -> 330f
                else -> 290f
            }
        val friction = 0.78f
        val groundY = groundTop()

        while (running) {
            delay(16)

            if (shakeTimer > 0f) {
                val tBefore = shakeTimer
                shakeTimer = max(0f, shakeTimer - frameSeconds)
                val phase = (tBefore / StompFeel.SHAKE_DURATION_S).coerceIn(0f, 1f)
                val amp = StompFeel.SHAKE_MAX_PX * phase
                renderShakeX = (Random.nextFloat() - 0.5f) * 2f * amp
                renderShakeY = (Random.nextFloat() - 0.5f) * 2f * amp
            } else {
                renderShakeX = 0f
                renderShakeY = 0f
            }
            if (hitStopTimer > 0f) {
                hitStopTimer = max(0f, hitStopTimer - frameSeconds)
                continue
            }

            val dashActive = fishDashTimer > 0f
            fishDashTimer = max(0f, fishDashTimer - frameSeconds)
            gustBootsTimer = max(0f, gustBootsTimer - frameSeconds)
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
            playerX = (playerX + playerVelocityX * frameSeconds).coerceIn(0f, levelLength - size)
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
                                    floatingCoins += FloatingCoin(
                                        x = block.x + block.size * 0.2f,
                                        y = block.y - block.size * 0.24f,
                                        size = block.size * 0.54f,
                                        velocityY = -240f,
                                        life = 0.65f,
                                    )
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
                    ParticleSpawners.landingDust(
                        particles,
                        worldX = playerX,
                        footY = landingY + size,
                        playerWidth = size,
                    )
                    soundManager?.playLand()
                }
                playerVelocityY = 0f
                onGround = true
            } else {
                onGround = false
            }

            playerY = nextY

            if (playerY > worldHeight) {
                endRun(win = false)
                continue
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

            val collectedCoins = coins.filter { coin ->
                val coinRect = Rect(coin.x, coin.y, coin.x + coin.size, coin.y + coin.size)
                playerRect.overlaps(coinRect)
            }
            if (collectedCoins.isNotEmpty()) {
                coinsCollected += collectedCoins.size
                coins.removeAll(collectedCoins.toSet())
            }

            val eatenFish = fishSnacks.firstOrNull { fishSnack ->
                val fishRect = Rect(fishSnack.x, fishSnack.y, fishSnack.x + fishSnack.size, fishSnack.y + fishSnack.size)
                playerRect.overlaps(fishRect)
            }
            if (eatenFish != null) {
                val ex = eatenFish
                fishSnacks.remove(eatenFish)
                fishDashTimer = 7f
                tutorialCollectedPowerUp = true
                bonusScore += 50
                ParticleSpawners.fishSnackBurst(
                    particles,
                    centerX = ex.x + ex.size * 0.5f,
                    centerY = ex.y + ex.size * 0.45f,
                )
                soundManager?.playEatFish()
            }

            val stompedEnemy = enemies.firstOrNull { enemy ->
                val enemyRect = Rect(enemy.x, enemy.y, enemy.x + enemy.width, enemy.y + enemy.height)
                playerRect.overlaps(enemyRect) &&
                    enemy.kind.canBeStomped() &&
                    playerVelocityY > 0f &&
                    playerRect.bottom <= enemyRect.top + enemy.height * 0.5f
            }
            if (stompedEnemy != null) {
                val e = stompedEnemy
                enemies.remove(e)
                tutorialDefeatedEnemy = true
                playerVelocityY = -720f
                onGround = false
                bonusScore += 25
                hitStopTimer = StompFeel.HIT_STOP_S
                shakeTimer = StompFeel.SHAKE_DURATION_S
                ParticleSpawners.stompKillBurst(
                    particles,
                    centerX = e.x + e.width * 0.5f,
                    centerY = e.y + e.height * 0.42f,
                )
                soundManager?.playStomp()
            } else {
                val hitEnemyIndex = enemies.indexOfFirst { enemy ->
                    val enemyRect = Rect(enemy.x, enemy.y, enemy.x + enemy.width, enemy.y + enemy.height)
                    playerRect.overlaps(enemyRect)
                }
                if (hitEnemyIndex >= 0) {
                    if (fishDashTimer > 0f) {
                        fishDashTimer = 0f
                        enemies.removeAt(hitEnemyIndex)
                        tutorialDefeatedEnemy = true
                        playerVelocityY = -320f
                    } else if (hasSnowShield) {
                        hasSnowShield = false
                        playerVelocityY = -260f
                        playerVelocityX = if (playerFacingRight) -180f else 180f
                        hitStopTimer = StompFeel.HIT_STOP_S * 0.7f
                    } else {
                        endRun(win = false)
                        continue
                    }
                }
            }

            friendGoal?.let { flag ->
                val flagRect = Rect(flag.x - 24f, flag.groundY - flag.height, flag.x + 48f, flag.groundY)
                if (playerRect.overlaps(flagRect)) {
                    endRun(win = true)
                    continue
                }
            }

            if (onGround) {
                coyoteTimer = coyoteMax
            } else {
                coyoteTimer = max(0f, coyoteTimer - frameSeconds)
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
            }
            updateParticlesInPlace(particles, frameSeconds)

            score = coinsCollected * 10 + (playerX / 24f).toInt() + if (fishDashTimer > 0f) 25 else 0 + bonusScore
            val targetCam =
                (playerX - worldWidth * 0.35f).coerceIn(0f, max(levelLength - worldWidth, 0f))
            cameraX += (targetCam - cameraX) * min(1f, 12f * frameSeconds)
        }
    }

    val sceneTheme = activeLevel?.sceneTheme ?: defaultStorySceneTheme()
    val outerGradient = sceneTheme.outerGradientColors
    val pres = activeLevel?.presentation
    val tutorialHint =
        when {
            currentLevel != GameLevel.CedarVillageRuins || tutorialDefeatedEnemy -> null
            !tutorialMoved -> "先按住左右按钮移动，熟悉企鹅起步的节奏。"
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
                        drawStorySceneBackdrop(
                            theme = theme,
                            cameraX = cameraX,
                            globalAnim = globalAnim,
                            groundY = groundY,
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
                            drawRoundRect(theme.platformTopColor, Offset(drawX, platform.y), androidx.compose.ui.geometry.Size(platform.width, platform.height), CornerRadius(16f, 16f))
                            drawRoundRect(theme.platformBottomColor, Offset(drawX, platform.y + platform.height * 0.55f), androidx.compose.ui.geometry.Size(platform.width, platform.height * 0.45f), CornerRadius(12f, 12f))
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

                        drawStageForegroundDecor(
                            groundY = groundY,
                            globalAnim = globalAnim,
                            palette = StageDecorPalette(
                                glowColor = theme.sunCoreColor,
                                mistColor = theme.foregroundMistColor,
                                shardColor = theme.foregroundShardColor,
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
                        if (hasSnowShield) {
                            drawCircle(Color(0x5590CAF9), hero * 1.08f, Offset(playerScreenX + hero / 2, playerY + hero / 2), style = Stroke(width = 6f))
                        }
                        if (gustBootsTimer > 0f) {
                            drawCircle(Color(0x55FFF176), hero * 0.72f, Offset(playerScreenX + hero / 2, playerY + hero * 0.9f))
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
                            moveLeftPressed,
                            moveRightPressed,
                        )
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
                        Box(
                            modifier = Modifier
                                .fillMaxSize()
                                .clickable { jump() },
                            contentAlignment = Alignment.Center,
                        ) {
                            OverlayCard(
                                title = "闯关失败",
                                description = "本次得分 $score，收集小鱼干 $coinsCollected。\n${pres?.failHint ?: ""}",
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
                }
            }
            GameStageControlDock {
                Row(
                    modifier = Modifier.fillMaxWidth(),
                    horizontalArrangement = Arrangement.spacedBy(12.dp),
                ) {
                    HoldButton(text = "左移", modifier = Modifier.weight(1f), onPressedChange = { moveLeftPressed = it })
                    HoldButton(
                        text = "跳跃",
                        modifier = Modifier.weight(1f),
                        onPressedChange = { pressed ->
                            jumpPressed = pressed
                            if (pressed) jump()
                        }
                    )
                    HoldButton(text = "右移", modifier = Modifier.weight(1f), onPressedChange = { moveRightPressed = it })
                }
                if (rescuedTuanTuan) {
                    HoldButton(
                        text = if (tuanTuanAssistReady) "团团支援" else "团团休息中",
                        modifier = Modifier.fillMaxWidth(),
                        onPressedChange = { pressed -> if (pressed) triggerTuanTuanAssist() }
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
                    rescuedTuanTuan = rescuedTuanTuan,
                    tuanTuanAssistReady = tuanTuanAssistReady,
                    tuanTuanAssistTimer = tuanTuanAssistTimer,
                    goalStatusLine = activeLevel?.presentation?.hudGoalLine ?: "准备出发…"
                )
            }
            if (tutorialHint != null) {
                TutorialHintCard(
                    modifier = Modifier.padding(top = 6.dp),
                    title = "新手引导",
                    description = tutorialHint,
                )
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
