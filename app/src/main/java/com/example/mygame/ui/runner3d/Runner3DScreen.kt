package com.example.mygame.ui.runner3d

import androidx.compose.animation.AnimatedVisibility
import androidx.compose.animation.fadeIn
import androidx.compose.animation.fadeOut
import androidx.compose.foundation.background
import androidx.compose.foundation.clickable
import androidx.compose.foundation.layout.*
import androidx.compose.foundation.shape.CircleShape
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.material3.Text
import androidx.compose.runtime.*
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.draw.clip
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.platform.LocalContext
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.text.style.TextAlign
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import com.example.mygame.audio.SoundManager
import com.example.mygame.data.SaveRepository
import com.example.mygame.engine.PowerUpType
import com.example.mygame.engine.RunnerGameState
import com.example.mygame.engine.RunnerGameView
import com.example.mygame.engine.RunnerRenderer
import com.example.mygame.engine.RunnerSnapshot
import com.example.mygame.haptic.HapticManager

@Composable
fun Runner3DScreen(
    soundManager: SoundManager,
    hapticManager: HapticManager? = null,
    saveRepository: SaveRepository? = null,
    onExitToMenu: () -> Unit,
) {
    val gameState = remember { RunnerGameState() }
    val snapshot by gameState.snapshot.collectAsState()
    val powerUpEvent by gameState.powerUpEvent.collectAsState()
    val milestoneEvent by gameState.milestoneEvent.collectAsState()
    val speedPhaseEvent by gameState.speedPhaseEvent.collectAsState()

    var showTutorial by remember {
        mutableStateOf(saveRepository?.hasCompletedRunner3DTutorial() == false)
    }

    var toastType by remember { mutableStateOf<PowerUpType?>(null) }
    var lastPuVersion by remember { mutableIntStateOf(0) }
    LaunchedEffect(powerUpEvent) {
        val (ver, type) = powerUpEvent
        if (type != null && ver != lastPuVersion) { lastPuVersion = ver; toastType = type }
    }

    var showMilestone by remember { mutableStateOf(false) }
    var milestoneMeters by remember { mutableIntStateOf(0) }
    var lastMilestoneVer by remember { mutableIntStateOf(0) }
    LaunchedEffect(milestoneEvent) {
        val (ver, meters) = milestoneEvent
        if (meters > 0 && ver != lastMilestoneVer) {
            lastMilestoneVer = ver; milestoneMeters = meters; showMilestone = true
        }
    }

    var showNearMiss by remember { mutableStateOf(false) }
    var lastNearMissCount by remember { mutableIntStateOf(0) }
    LaunchedEffect(snapshot.nearMissCount) {
        if (snapshot.nearMissCount > lastNearMissCount) {
            lastNearMissCount = snapshot.nearMissCount; showNearMiss = true
        }
    }

    var showSpeedUp by remember { mutableStateOf(false) }
    var speedUpPhase by remember { mutableIntStateOf(0) }
    var lastSpeedPhaseVer by remember { mutableIntStateOf(0) }
    LaunchedEffect(speedPhaseEvent) {
        val (ver, phase) = speedPhaseEvent
        if (ver != lastSpeedPhaseVer && ver > 0) {
            lastSpeedPhaseVer = ver; speedUpPhase = phase; showSpeedUp = true
        }
    }

    RunnerGameView(
        gameState = gameState,
        onSoundEvent = { event ->
            when (event) {
                RunnerRenderer.SoundEvent.Jump -> {
                    soundManager.playJump()
                    hapticManager?.performMomentum()
                }
                RunnerRenderer.SoundEvent.Land -> {
                    soundManager.playLand()
                    hapticManager?.performStomp()
                }
                RunnerRenderer.SoundEvent.Slide -> {
                    soundManager.playIceCrack()
                    hapticManager?.performMomentum()
                }
                RunnerRenderer.SoundEvent.LaneSwitch -> {
                    hapticManager?.performMomentum()
                }
                RunnerRenderer.SoundEvent.Die -> {
                    soundManager.playIceCrack()
                    hapticManager?.performStomp()
                }
                RunnerRenderer.SoundEvent.CoinPickup -> soundManager.playCoinPickup()
                RunnerRenderer.SoundEvent.PowerUp -> {
                    soundManager.playPowerUp()
                    hapticManager?.performMomentum()
                }
                RunnerRenderer.SoundEvent.Stomp -> {
                    soundManager.playShieldBounce()
                    hapticManager?.performStomp()
                }
                RunnerRenderer.SoundEvent.NearMiss -> {
                    hapticManager?.performMomentum()
                }
            }
        },
    ) { restartRun ->
        Runner3DHud(snapshot = snapshot, onBack = onExitToMenu)

        // tap-to-start (only when not in tutorial and not running)
        AnimatedVisibility(
            visible = !showTutorial && !snapshot.isRunning && !snapshot.isGameOver,
            enter = fadeIn(),
            exit = fadeOut(),
        ) {
            TapToStartOverlay()
        }

        // game over
        AnimatedVisibility(
            visible = snapshot.isGameOver,
            enter = fadeIn(),
            exit = fadeOut(),
        ) {
            GameOverOverlay(snapshot = snapshot, onRestart = restartRun, onBack = onExitToMenu)
        }

        // power-up toast
        val currentToast = toastType
        if (currentToast != null) {
            PowerUpToast(type = currentToast, onDismiss = { toastType = null })
        }

        // milestone toast
        if (showMilestone) {
            MilestoneToast(meters = milestoneMeters, onDismiss = { showMilestone = false })
        }

        // near-miss toast
        if (showNearMiss) {
            NearMissToast(onDismiss = { showNearMiss = false })
        }

        // speed-up toast
        if (showSpeedUp) {
            SpeedUpToast(phase = speedUpPhase, onDismiss = { showSpeedUp = false })
        }

        // tutorial overlay (topmost)
        if (showTutorial) {
            TutorialOverlay(
                onComplete = {
                    showTutorial = false
                    saveRepository?.markRunner3DTutorialDone()
                },
            )
        }
    }
}

@Composable
private fun Runner3DHud(snapshot: RunnerSnapshot, onBack: () -> Unit) {
    Box(modifier = Modifier.fillMaxSize()) {
        Row(
            modifier = Modifier
                .fillMaxWidth()
                .statusBarsPadding()
                .padding(horizontal = 16.dp, vertical = 8.dp),
            horizontalArrangement = Arrangement.SpaceBetween,
            verticalAlignment = Alignment.CenterVertically,
        ) {
            Box(
                modifier = Modifier
                    .size(40.dp)
                    .clip(CircleShape)
                    .background(Color.Black.copy(alpha = 0.4f))
                    .clickable { onBack() },
                contentAlignment = Alignment.Center,
            ) {
                Text("<", color = Color.White, fontSize = 20.sp)
            }

            Column(horizontalAlignment = Alignment.End) {
                Text(
                    text = "${snapshot.score}",
                    color = Color.White,
                    fontSize = 28.sp,
                    fontWeight = FontWeight.Bold,
                )
                Text(
                    text = "${snapshot.distance.toInt()}m",
                    color = Color.White.copy(alpha = 0.7f),
                    fontSize = 14.sp,
                )
            }
        }

        if (snapshot.isRunning) {
            Row(
                modifier = Modifier
                    .align(Alignment.TopCenter)
                    .statusBarsPadding()
                    .padding(top = 48.dp),
                horizontalArrangement = Arrangement.spacedBy(12.dp),
            ) {
                HudChip("🪙 ${snapshot.coins}")
                if (snapshot.multiplier > 1f) HudChip("x${String.format("%.1f", snapshot.multiplier)}")
                if (snapshot.comboCount >= 3) HudChip("🔥 ${snapshot.comboCount}")
            }

            Column(
                modifier = Modifier
                    .align(Alignment.CenterStart)
                    .padding(start = 8.dp),
                verticalArrangement = Arrangement.spacedBy(4.dp),
            ) {
                if (snapshot.fishDashActive) PowerUpIndicator("🐟")
                if (snapshot.snowShieldActive) PowerUpIndicator("🛡️")
                if (snapshot.gustBootsActive) PowerUpIndicator("👢")
                if (snapshot.magnetActive) PowerUpIndicator("🧲")
                if (snapshot.hasBubbleScarf) PowerUpIndicator("🫧")
            }

            // speed bar — bottom right
            SpeedBar(
                modifier = Modifier
                    .align(Alignment.BottomEnd)
                    .padding(end = 12.dp, bottom = 24.dp),
                speed = snapshot.runSpeed,
                phase = snapshot.speedPhase,
            )
        }
    }
}

@Composable
private fun HudChip(text: String) {
    Text(
        text = text,
        color = Color.White,
        fontSize = 16.sp,
        fontWeight = FontWeight.Medium,
        modifier = Modifier
            .background(Color.Black.copy(alpha = 0.4f), RoundedCornerShape(12.dp))
            .padding(horizontal = 10.dp, vertical = 4.dp),
    )
}

@Composable
private fun PowerUpIndicator(emoji: String) {
    Text(
        text = emoji,
        fontSize = 24.sp,
        modifier = Modifier
            .background(Color.Black.copy(alpha = 0.3f), CircleShape)
            .padding(4.dp),
    )
}

private val phaseLabels = listOf("热身", "加速", "发力", "冲刺", "高速", "极速", "极限")
private val phaseColors = listOf(
    Color(0xFF4FC3F7), // 热身 — 浅蓝
    Color(0xFF81C784), // 加速 — 绿
    Color(0xFFFFD54F), // 发力 — 黄
    Color(0xFFFF8A65), // 冲刺 — 橙
    Color(0xFFEF5350), // 高速 — 红
    Color(0xFFCE93D8), // 极速 — 紫
    Color(0xFFFFFFFF), // 极限 — 白
)

@Composable
private fun SpeedBar(speed: Float, phase: Int, modifier: Modifier = Modifier) {
    val baseSpeed = 8f
    val maxSpeed = 42f
    val fill = ((speed - baseSpeed) / (maxSpeed - baseSpeed)).coerceIn(0f, 1f)
    val color = phaseColors.getOrElse(phase) { Color.White }
    val label = phaseLabels.getOrElse(phase) { "极限" }

    Column(
        modifier = modifier,
        horizontalAlignment = Alignment.End,
        verticalArrangement = Arrangement.spacedBy(3.dp),
    ) {
        Text(
            text = label,
            color = color,
            fontSize = 11.sp,
            fontWeight = FontWeight.Bold,
        )
        Box(
            modifier = Modifier
                .width(80.dp)
                .height(6.dp)
                .background(Color.White.copy(alpha = 0.15f), RoundedCornerShape(3.dp)),
        ) {
            Box(
                modifier = Modifier
                    .fillMaxHeight()
                    .fillMaxWidth(fill)
                    .background(color, RoundedCornerShape(3.dp)),
            )
        }
        Text(
            text = "${speed.toInt()} u/s",
            color = Color.White.copy(alpha = 0.5f),
            fontSize = 10.sp,
        )
    }
}

@Composable
private fun TapToStartOverlay() {
    Box(modifier = Modifier.fillMaxSize(), contentAlignment = Alignment.Center) {
        Column(horizontalAlignment = Alignment.CenterHorizontally) {
            Text("企鹅快跑 3D", color = Color.White, fontSize = 36.sp, fontWeight = FontWeight.Bold)
            Spacer(Modifier.height(16.dp))
            Text("点击屏幕开始", color = Color.White.copy(alpha = 0.7f), fontSize = 18.sp)
            Spacer(Modifier.height(8.dp))
            Text(
                "上滑跳跃 · 下滑滑铲 · 左右滑换道",
                color = Color.White.copy(alpha = 0.5f),
                fontSize = 14.sp,
            )
        }
    }
}

@Composable
private fun GameOverOverlay(
    snapshot: RunnerSnapshot,
    onRestart: () -> Unit,
    onBack: () -> Unit,
) {
    Box(
        modifier = Modifier.fillMaxSize(),
    ) {
        // 暗区点击 = 再来一次（与底部文案一致）；中间卡片叠在上层，按钮各自消费
        Box(
            modifier = Modifier
                .matchParentSize()
                .background(Color.Black.copy(alpha = 0.6f))
                .clickable { onRestart() },
        )
        Column(
            horizontalAlignment = Alignment.CenterHorizontally,
            modifier = Modifier
                .align(Alignment.Center)
                .background(Color(0xFF1A1A2E), RoundedCornerShape(16.dp))
                .padding(32.dp),
        ) {
            Text("游戏结束", color = Color.White, fontSize = 28.sp, fontWeight = FontWeight.Bold)
            Spacer(Modifier.height(16.dp))

            ScoreRow("距离", "${snapshot.distance.toInt()}m")
            ScoreRow("金币", "${snapshot.coins}")
            ScoreRow("险过", "${snapshot.nearMissCount} 次")
            ScoreRow("距离分", "${snapshot.distanceScore}")
            ScoreRow("收集分", "${snapshot.collectionScore}")
            ScoreRow("倍率", "x${String.format("%.1f", snapshot.multiplier)}")
            Spacer(Modifier.height(8.dp))
            Text(
                "总分: ${snapshot.score}",
                color = Color(0xFFFFD700),
                fontSize = 24.sp,
                fontWeight = FontWeight.Bold,
            )

            Spacer(Modifier.height(24.dp))

            Row(horizontalArrangement = Arrangement.spacedBy(16.dp)) {
                OverlayButton("再来一次", onClick = onRestart)
                OverlayButton("返回", onClick = onBack)
            }

            Spacer(Modifier.height(8.dp))
            Text(
                "点击屏幕重新开始",
                color = Color.White.copy(alpha = 0.5f),
                fontSize = 12.sp,
            )
        }
    }
}

@Composable
private fun ScoreRow(label: String, value: String) {
    Row(
        modifier = Modifier
            .fillMaxWidth(0.6f)
            .padding(vertical = 2.dp),
        horizontalArrangement = Arrangement.SpaceBetween,
    ) {
        Text(label, color = Color.White.copy(alpha = 0.7f), fontSize = 16.sp)
        Text(value, color = Color.White, fontSize = 16.sp, fontWeight = FontWeight.Medium)
    }
}

@Composable
private fun OverlayButton(text: String, onClick: () -> Unit) {
    Box(
        modifier = Modifier
            .clip(RoundedCornerShape(8.dp))
            .background(Color(0xFF3A3A5C))
            .clickable { onClick() }
            .padding(horizontal = 20.dp, vertical = 10.dp),
    ) {
        Text(text, color = Color.White, fontSize = 16.sp)
    }
}
