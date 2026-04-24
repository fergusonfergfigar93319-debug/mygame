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
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.material3.Card
import androidx.compose.material3.Text
import androidx.compose.material3.TextButton
import androidx.compose.runtime.Composable
import androidx.compose.runtime.LaunchedEffect
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableFloatStateOf
import androidx.compose.runtime.mutableIntStateOf
import androidx.compose.runtime.mutableStateListOf
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.setValue
import androidx.compose.ui.Modifier
import androidx.compose.ui.geometry.CornerRadius
import androidx.compose.ui.geometry.Offset
import androidx.compose.ui.geometry.Rect
import androidx.compose.ui.graphics.Brush
import androidx.compose.ui.Alignment
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.geometry.Size
import androidx.compose.ui.graphics.Path
import androidx.compose.ui.unit.dp
import com.example.mygame.data.LeaderboardRepository
import com.example.mygame.data.LeaderboardSubmitResult
import com.example.mygame.data.LocalLeaderboardRepository
import com.example.mygame.data.SaveRepository
import com.example.mygame.data.model.LeaderboardEntry
import com.example.mygame.game.BlockReward
import com.example.mygame.game.BlockType
import com.example.mygame.game.CoinKind
import com.example.mygame.game.EnemyKind
import com.example.mygame.game.FishSnack
import com.example.mygame.game.FloatingCoin
import com.example.mygame.game.HoldButton
import com.example.mygame.game.OverlayCard
import com.example.mygame.game.level.EndlessSegmentKind
import com.example.mygame.game.level.EndlessSegmentPool
import com.example.mygame.game.level.EndlessSegmentSpan
import com.example.mygame.game.level.offsetWorldX
import com.example.mygame.game.score.EndlessRunScoreBreakdown
import com.example.mygame.game.score.EndlessScoreBook
import com.example.mygame.ui.endless.EndlessHud
import com.example.mygame.ui.endless.EndlessSettlementOverlay
import kotlinx.coroutines.delay
import kotlin.math.cos
import kotlin.math.max
import kotlin.math.min
import kotlin.math.sin
import kotlin.random.Random

@Composable
fun EndlessMode(
    saveRepository: SaveRepository,
    leaderboardRepository: LeaderboardRepository,
    onExitToMenu: () -> Unit,
) {
    val rescuedTuanTuan = remember { saveRepository.getRescuedTuanTuan() }
    val random = remember { Random(System.currentTimeMillis()) }

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

    val scoreBook = remember { EndlessScoreBook(EndlessBalanceConfig.scoring) }

    var playerX by remember { mutableFloatStateOf(100f) }
    var playerY by remember { mutableFloatStateOf(100f) }
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

    var fishDashTimer by remember { mutableFloatStateOf(0f) }
    var hasBubbleScarf by remember { mutableStateOf(false) }
    var tuanTuanAssistTimer by remember { mutableFloatStateOf(0f) }
    var tuanTuanAssistReady by remember { mutableStateOf(rescuedTuanTuan) }

    var coyoteTimer by remember { mutableFloatStateOf(0f) }
    val coyoteMax = 0.12f

    var started by remember { mutableStateOf(false) }
    var running by remember { mutableStateOf(false) }
    var gameOver by remember { mutableStateOf(false) }

    var globalAnim by remember { mutableFloatStateOf(0f) }
    var currentSegmentKind by remember { mutableStateOf<EndlessSegmentKind?>(null) }

    var submitResult by remember { mutableStateOf<LeaderboardSubmitResult?>(null) }
    var lastBreakdown by remember { mutableStateOf<EndlessRunScoreBreakdown?>(null) }

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
            random,
            playerX,
            lastRewardEndX,
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
        scoreBook.reset()
        fishDashTimer = 0f
        hasBubbleScarf = false
        tuanTuanAssistTimer = 0f
        tuanTuanAssistReady = rescuedTuanTuan
        gameOver = false
        submitResult = null
        lastBreakdown = null
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
        started = true
        running = true
    }

    fun endRunDead() {
        if (gameOver) return
        running = false
        gameOver = true
        lastBreakdown = scoreBook.breakdown()
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
        )
        submitResult = leaderboardRepository.submit(entry)
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
            playerVelocityY = -980f
            onGround = false
            coyoteTimer = 0f
        }
    }

    fun triggerAssist() {
        if (!rescuedTuanTuan || !tuanTuanAssistReady || !started || gameOver || !running) return
        tuanTuanAssistReady = false
        tuanTuanAssistTimer = 4.5f
        scoreBook.onAssistUsed()
    }

    LaunchedEffect(Unit) {
        while (true) {
            delay(16)
            globalAnim += 0.05f
        }
    }

    LaunchedEffect(running, worldWidth, worldHeight) {
        if (!running || worldWidth <= 1f || worldHeight <= 1f) return@LaunchedEffect

        val frameSeconds = 0.016f
        val gravity = 2100f
        val friction = 0.78f

        while (running) {
            delay(16)
            runElapsed += frameSeconds
            fishDashTimer = max(0f, fishDashTimer - frameSeconds)
            tuanTuanAssistTimer = max(0f, tuanTuanAssistTimer - frameSeconds)

            ensureWorld()
            val span = findSpan(playerX + playerSize() * 0.5f)
            currentSegmentKind = span?.kind
            val speedMul = span?.speedMultiplier ?: 1f
            val blizzardMul =
                1f - (span?.blizzardIntensity ?: 0f) * EndlessBalanceConfig.blizzardRunSpeedPenaltyPerIntensity

            var runSpeed = (if (fishDashTimer > 0f) 365f else 290f) * speedMul * blizzardMul

            val groundY = groundTop()
            val size = playerSize()
            val previousY = playerY
            val previousBottom = previousY + size

            playerVelocityX = when {
                moveLeftPressed && !moveRightPressed -> -runSpeed
                moveRightPressed && !moveLeftPressed -> runSpeed
                else -> playerVelocityX * friction
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
                                }
                            }
                        }
                        break
                    }
                }
            }

            if (landed && playerVelocityY >= 0f) {
                nextY = landingY
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

            val collected = coins.filter { coin ->
                val r = Rect(coin.x, coin.y, coin.x + coin.size, coin.y + coin.size)
                playerRect.overlaps(r)
            }
            collected.forEach { scoreBook.onCoinPickup(it.kind) }
            coins.removeAll(collected.toSet())

            val eatenFish = fishSnacks.firstOrNull { fishSnack ->
                val r = Rect(fishSnack.x, fishSnack.y, fishSnack.x + fishSnack.size, fishSnack.y + fishSnack.size)
                playerRect.overlaps(r)
            }
            if (eatenFish != null) {
                fishSnacks.remove(eatenFish)
                fishDashTimer = 7f
                scoreBook.onFishSnackEaten()
            }

            val stompedEnemy = enemies.firstOrNull { enemy ->
                val er = Rect(enemy.x, enemy.y, enemy.x + enemy.width, enemy.y + enemy.height)
                playerRect.overlaps(er) &&
                    playerVelocityY > 0f &&
                    playerRect.bottom <= er.top + enemy.height * 0.5f
            }
            if (stompedEnemy != null) {
                enemies.remove(stompedEnemy)
                playerVelocityY = -720f
                onGround = false
                scoreBook.onStompEnemy()
            } else {
                val hitEnemyIndex = enemies.indexOfFirst { enemy ->
                    val er = Rect(enemy.x, enemy.y, enemy.x + enemy.width, enemy.y + enemy.height)
                    playerRect.overlaps(er)
                }
                if (hitEnemyIndex >= 0) {
                    if (fishDashTimer > 0f) {
                        fishDashTimer = 0f
                        enemies.removeAt(hitEnemyIndex)
                        playerVelocityY = -320f
                    } else {
                        endRunDead()
                        continue
                    }
                }
            }

            if (onGround) coyoteTimer = coyoteMax else coyoteTimer = max(0f, coyoteTimer - frameSeconds)

            scoreBook.tickFrame(playerX, frameSeconds)

            val targetCam = (playerX - worldWidth * 0.35f).coerceAtLeast(0f)
            cameraX += (targetCam - cameraX) * min(1f, 12f * frameSeconds)

            pruneWorld()
        }
    }

    val nightOuter = listOf(Color(0xFF1A237E), Color(0xFF283593), Color(0xFF3949AB), Color(0xFF5C6BC0))

    Column(
        modifier = Modifier
            .fillMaxSize()
            .background(Brush.verticalGradient(nightOuter))
            .padding(horizontal = 16.dp, vertical = 12.dp),
        verticalArrangement = Arrangement.spacedBy(10.dp),
    ) {
        Row(modifier = Modifier.fillMaxWidth(), horizontalArrangement = Arrangement.SpaceBetween) {
            TextButton(onClick = onExitToMenu) { Text("返回") }
        }
        EndlessHud(
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
            assistReady = tuanTuanAssistReady,
            assistTimer = tuanTuanAssistTimer,
            currentSegmentKind = currentSegmentKind,
        )
        Card(modifier = Modifier.fillMaxWidth().weight(1f), shape = RoundedCornerShape(24.dp)) {
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

                    drawRect(
                        brush = Brush.verticalGradient(
                            listOf(Color(0xFF1E3A5F), Color(0xFF2C4870), Color(0xFF3D5A80)),
                        ),
                    )
                    drawCircle(
                        brush = Brush.radialGradient(
                            colors = listOf(Color(0xFFE8EAF6).copy(alpha = 0.5f), Color.Transparent),
                            center = Offset(size.width * 0.85f, size.height * 0.12f),
                            radius = size.width * 0.18f,
                        ),
                        radius = size.width * 0.04f,
                        center = Offset(size.width * 0.85f, size.height * 0.12f),
                    )
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
                        drawRoundRect(Color(0xFF78909C), Offset(drawX, platform.y), androidx.compose.ui.geometry.Size(platform.width, platform.height), CornerRadius(14f, 14f))
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
                    if (fishDashTimer > 0f) drawCircle(Color(0x55FFB74D), hero * 0.88f, Offset(playerScreenX + hero / 2, playerY + hero / 2))
                    if (hasBubbleScarf) drawCircle(Color(0x5539C5FF), hero * 0.96f, Offset(playerScreenX + hero / 2, playerY + hero / 2))
                    if (tuanTuanAssistTimer > 0f) {
                        drawCircle(Color(0x55A5D6A7), hero * 1.05f, Offset(playerScreenX + hero / 2, playerY + hero / 2))
                    }
                    drawRoundRect(Color(0xFF263238), Offset(playerScreenX, playerY), androidx.compose.ui.geometry.Size(hero, hero), CornerRadius(22f, 22f))
                    drawOval(Color(0xFFECEFF1), Offset(playerScreenX + hero * 0.18f, playerY + hero * 0.16f), androidx.compose.ui.geometry.Size(hero * 0.64f, hero * 0.72f))
                    drawRect(
                        color = if (fishDashTimer > 0f) Color(0xFFFFA726) else Color(0xFF4FC3F7),
                        topLeft = Offset(playerScreenX + hero * 0.12f, playerY + hero * 0.08f),
                        size = androidx.compose.ui.geometry.Size(hero * 0.76f, hero * 0.18f),
                    )
                    if (bzz > 0.05f) {
                        drawRect(Color.White.copy(alpha = bzz * 0.35f))
                    }
                }

                if (!started) {
                    OverlayCard(
                        title = "极夜漂流",
                        description = "暴风雪中的碎冰航道。\n搜集补给、信标与残页，撑过更久、走得更远。\n点击屏幕开始。",
                    )
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
                                onRestart = { resetEndless() },
                            )
                        }
                    }
                }
            }
        }
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
