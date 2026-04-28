package com.example.mygame.ui.home

import androidx.compose.animation.core.FastOutSlowInEasing
import androidx.compose.animation.core.RepeatMode
import androidx.compose.animation.core.animateFloat
import androidx.compose.animation.core.animateFloatAsState
import androidx.compose.animation.core.infiniteRepeatable
import androidx.compose.animation.core.rememberInfiniteTransition
import androidx.compose.animation.core.tween
import androidx.compose.foundation.BorderStroke
import androidx.compose.foundation.Canvas
import androidx.compose.foundation.Image
import androidx.compose.foundation.background
import androidx.compose.foundation.border
import androidx.compose.foundation.clickable
import androidx.compose.foundation.interaction.MutableInteractionSource
import androidx.compose.foundation.interaction.collectIsPressedAsState
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.BoxWithConstraints
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.RowScope
import androidx.compose.foundation.layout.Spacer
import androidx.compose.foundation.layout.fillMaxHeight
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.navigationBarsPadding
import androidx.compose.foundation.layout.offset
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.size
import androidx.compose.foundation.layout.statusBarsPadding
import androidx.compose.foundation.layout.width
import androidx.compose.foundation.layout.widthIn
import androidx.compose.foundation.shape.CircleShape
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.automirrored.rounded.MenuBook
import androidx.compose.material.icons.automirrored.rounded.VolumeOff
import androidx.compose.material.icons.automirrored.rounded.VolumeUp
import androidx.compose.material.icons.rounded.EmojiEvents
import androidx.compose.material.icons.rounded.Explore
import androidx.compose.material.icons.rounded.Leaderboard
import androidx.compose.material.icons.rounded.LocalFireDepartment
import androidx.compose.material.icons.rounded.MusicNote
import androidx.compose.material.icons.rounded.MusicOff
import androidx.compose.material.icons.rounded.Person
import androidx.compose.material.icons.rounded.SetMeal
import androidx.compose.material.icons.rounded.Star
import androidx.compose.material.icons.rounded.Today
import androidx.compose.material.icons.rounded.SettingsSuggest
import androidx.compose.material.icons.rounded.Waves
import androidx.compose.material3.Button
import androidx.compose.material3.ButtonDefaults
import androidx.compose.material3.ExperimentalMaterial3Api
import androidx.compose.material3.Icon
import androidx.compose.material3.IconButton
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.OutlinedTextField
import androidx.compose.material3.Slider
import androidx.compose.material3.SliderDefaults
import androidx.compose.material3.Text
import androidx.compose.material3.TextButton
import androidx.compose.runtime.Composable
import androidx.compose.runtime.LaunchedEffect
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableFloatStateOf
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.setValue
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.draw.alpha
import androidx.compose.ui.draw.clip
import androidx.compose.ui.draw.scale
import androidx.compose.ui.draw.shadow
import androidx.compose.ui.platform.LocalContext
import androidx.compose.ui.geometry.Offset
import androidx.compose.ui.graphics.Brush
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.graphics.Path
import androidx.compose.ui.graphics.Shadow
import androidx.compose.ui.graphics.drawscope.DrawScope
import androidx.compose.ui.graphics.vector.ImageVector
import androidx.compose.ui.layout.ContentScale
import androidx.compose.ui.res.painterResource
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.text.style.TextAlign
import androidx.compose.ui.unit.DpSize
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import androidx.compose.ui.window.Dialog
import com.example.mygame.R
import com.example.mygame.audio.SoundManager
import com.example.mygame.data.CampUpgradeKind
import com.example.mygame.data.SaveRepository
import com.example.mygame.haptic.HapticManager
import com.example.mygame.game.modes.EndlessDailyChallenge
import kotlin.math.sin

private val LobbyBackgroundGradient =
    listOf(
        Color(0xFF071423),
        Color(0xFF10324B),
        Color(0xFF4D7F9B),
        Color(0xFFB9D9E8),
    )

private val AccentCyan = Color(0xFF64FFDA)
private val WarmGold = Color(0xFFFFD76A)

private data class LobbyRecommendation(
    val title: String,
    val subtitle: String,
    val actionLabel: String,
    val icon: ImageVector,
    val onClick: () -> Unit,
)

private data class LobbyBgmChoice(
    val key: String,
    val label: String,
    val description: String,
    val track: SoundManager.BgmTrack,
)

private val LobbyBgmChoices =
    listOf(
        LobbyBgmChoice("cozy", "悠闲雪夜", "柔和铃音与暖和弦，适合首页停留。", SoundManager.BgmTrack.LobbyCozy),
        LobbyBgmChoice("story", "冒险旋律", "更有追逐感，适合准备进入赛道。", SoundManager.BgmTrack.Story),
        LobbyBgmChoice("endless", "快跑节奏", "节奏更流动，适合挑战前热身。", SoundManager.BgmTrack.Endless),
        LobbyBgmChoice("none", "不播放", "首页保持安静，只保留音效。", SoundManager.BgmTrack.None),
    )

@Composable
fun HomeMenu(
    saveRepository: SaveRepository,
    soundManager: SoundManager,
    onStory: () -> Unit,
    onEndless: () -> Unit,
    onEndlessDaily: () -> Unit,
    onLeaderboard: () -> Unit,
    onCodex: () -> Unit,
    onCamp: () -> Unit,
) {
    val drorUnlocked = saveRepository.getCompanionDrorUnlocked()
    val rescuedTuanTuan = saveRepository.getRescuedTuanTuan()
    val isTakamatsuDefeated = saveRepository.getTakamatsuDefeated()
    val playerNickname = saveRepository.getPlayerNickname()
    val bestScore = saveRepository.getBestScore()
    val totalFishSnacks = saveRepository.getTotalFishSnacks()
    val todayBucket = EndlessDailyChallenge.todayBucketLocal()
    val todayAttempts = saveRepository.getDailyChallengeAttemptCount(todayBucket)
    val context = LocalContext.current
    val hapticManager = remember { HapticManager(context, saveRepository) }

    fun withUiSound(action: () -> Unit): () -> Unit = {
        soundManager.playUiSelect()
        action()
    }

    val canBuyAnyUpgrade =
        listOf(
            saveRepository.getCampUpgradeCost(CampUpgradeKind.Dash, saveRepository.getCampDashLevel()),
            saveRepository.getCampUpgradeCost(CampUpgradeKind.Tuan, saveRepository.getCampTuanLevel()),
            saveRepository.getCampUpgradeCost(CampUpgradeKind.Polar, saveRepository.getCampPolarIntuitionLevel()),
            saveRepository.getCampUpgradeCost(CampUpgradeKind.Magnet, saveRepository.getCampMagnetLevel()),
        ).filterNotNull().any { totalFishSnacks >= it }

    val recommendation =
        when {
            !rescuedTuanTuan ->
                LobbyRecommendation(
                    title = "开始快跑",
                    subtitle = "从雪松村废墟出发，救回第一位伙伴团团。",
                    actionLabel = "立即出发",
                    icon = Icons.Rounded.Explore,
                    onClick = withUiSound(onStory),
                )
            canBuyAnyUpgrade ->
                LobbyRecommendation(
                    title = "营地可升级",
                    subtitle = "鱼干已经够用，先强化一次再进下一局会更稳。",
                    actionLabel = "去营地",
                    icon = Icons.Rounded.LocalFireDepartment,
                    onClick = withUiSound(onCamp),
                )
            todayAttempts == 0 ->
                LobbyRecommendation(
                    title = "今日挑战未完成",
                    subtitle = "固定 seed 地图已经刷新，适合打一局可比较成绩。",
                    actionLabel = "挑战今日",
                    icon = Icons.Rounded.Today,
                    onClick = withUiSound(onEndlessDaily),
                )
            !isTakamatsuDefeated ->
                LobbyRecommendation(
                    title = "推进北境线索",
                    subtitle = "继续追击高松鹅，解锁更多图鉴和剧情记录。",
                    actionLabel = "开始快跑",
                    icon = Icons.Rounded.Star,
                    onClick = withUiSound(onStory),
                )
            else ->
                LobbyRecommendation(
                    title = "刷新快跑纪录",
                    subtitle = "雪原赛道已经开放，去跑出新的最远距离。",
                    actionLabel = "开始快跑",
                    icon = Icons.Rounded.Waves,
                    onClick = withUiSound(onEndless),
                )
        }

    var showAudioBubble by remember { mutableStateOf(false) }
    var showFeedbackDialog by remember { mutableStateOf(false) }
    val infiniteTransition = rememberInfiniteTransition(label = "lobby_motion")
    val breathScale by infiniteTransition.animateFloat(
        initialValue = 0.985f,
        targetValue = 1.015f,
        animationSpec = infiniteRepeatable(tween(2400, easing = FastOutSlowInEasing), RepeatMode.Reverse),
        label = "hero_breath",
    )
    val edgeShimmer by infiniteTransition.animateFloat(
        initialValue = 0.28f,
        targetValue = 0.9f,
        animationSpec = infiniteRepeatable(tween(2400, easing = FastOutSlowInEasing), RepeatMode.Reverse),
        label = "edge_shimmer",
    )
    val developerPulse by infiniteTransition.animateFloat(
        initialValue = 0.96f,
        targetValue = 1.04f,
        animationSpec = infiniteRepeatable(tween(2200, easing = FastOutSlowInEasing), RepeatMode.Reverse),
        label = "developer_pulse",
    )

    BoxWithConstraints(modifier = Modifier.fillMaxSize()) {
        val compact = maxWidth < 560.dp
        val contentWidth = if (compact) 0.9f else 0.48f
        val heroAlpha = if (compact) 0.2f else 0.32f

        Box(modifier = Modifier.fillMaxSize()) {
            Box(Modifier.fillMaxSize().background(Brush.verticalGradient(LobbyBackgroundGradient)))
            LobbyLandscapeBackdrop(Modifier.fillMaxSize(), compact = compact)
            LobbySnowEffect()

            Image(
                painter = painterResource(R.drawable.gugu_sprite),
                contentDescription = null,
                contentScale = ContentScale.Fit,
                alignment = Alignment.BottomEnd,
                modifier =
                    Modifier
                        .fillMaxHeight(if (compact) 0.68f else 0.9f)
                        .fillMaxWidth(if (compact) 0.68f else 0.68f)
                        .align(Alignment.BottomEnd)
                        .offset(x = if (compact) 56.dp else 0.dp, y = if (compact) (-8).dp else 0.dp)
                        .scale(breathScale)
                        .alpha(heroAlpha),
            )

            Box(
                modifier =
                    Modifier
                        .fillMaxSize()
                        .background(
                            Brush.verticalGradient(
                                colorStops =
                                    arrayOf(
                                        0f to Color(0x11000000),
                                        0.38f to Color.Transparent,
                                        0.68f to Color(0x2207111E),
                                        1f to Color(0x8807111E),
                                    ),
                            ),
                        ),
            )

            LobbyTopAssetBar(
                nickname = playerNickname,
                bestScore = bestScore,
                fish = totalFishSnacks,
                rescuedTuanTuan = rescuedTuanTuan,
                drorUnlocked = drorUnlocked,
                developerPulse = developerPulse,
                feedbackCount = saveRepository.getFeedbackCount(),
                onFeedback = {
                    soundManager.playUiSelect()
                    showFeedbackDialog = true
                },
            )

            Column(
                modifier =
                    Modifier
                        .fillMaxWidth(contentWidth)
                        .align(Alignment.CenterStart)
                        .padding(start = if (compact) 20.dp else 28.dp, end = 8.dp)
                        .offset(y = if (compact) (-10).dp else (-34).dp),
                verticalArrangement = Arrangement.spacedBy(10.dp),
            ) {
                Text(
                    text = if (isTakamatsuDefeated) "企鹅快跑" else "企鹅快跑",
                    style =
                        MaterialTheme.typography.displaySmall.copy(
                            shadow = Shadow(Color(0xAA000000), Offset(2f, 4f), blurRadius = 8f),
                        ),
                    fontWeight = FontWeight.Black,
                    color = if (isTakamatsuDefeated) Color(0xFFE0F7FA) else Color.White,
                    lineHeight = 40.sp,
                )
                Text(
                    text = if (isTakamatsuDefeated) "雪原归于平静，新的赛道仍在继续" else "咕咕嘎嘎的冰原跑酷之旅",
                    style =
                        MaterialTheme.typography.titleMedium.copy(
                            shadow = Shadow(Color(0x88000000), Offset(0f, 2f), blurRadius = 6f),
                        ),
                    color = Color(0xE6FFFFFF),
                )
                Spacer(modifier = Modifier.height(12.dp))
                RecommendationCard(recommendation)
                QuickModeButton("企鹅快跑", Icons.Rounded.Explore, withUiSound(onStory))
                QuickModeButton("自由跑酷", Icons.Rounded.Waves, withUiSound(onEndless))
                QuickModeButton("今日挑战", Icons.Rounded.Today, withUiSound(onEndlessDaily))
            }

            LobbyDock(
                edgeShimmer = edgeShimmer,
                showAudioBubble = showAudioBubble,
                onCamp = withUiSound(onCamp),
                onCodex = withUiSound(onCodex),
                onStory = withUiSound(onStory),
                onLeaderboard = withUiSound(onLeaderboard),
                onToggleAudio = { showAudioBubble = !showAudioBubble },
                modifier = Modifier.align(Alignment.BottomCenter),
            )

            if (showAudioBubble) {
                val scrimInteraction = remember { MutableInteractionSource() }
                Box(
                    modifier =
                        Modifier
                            .fillMaxSize()
                            .clickable(scrimInteraction, indication = null) { showAudioBubble = false }
                            .background(Color(0x33000000)),
                )
            }

            LobbyAudioQuickBubble(
                expanded = showAudioBubble,
                saveRepository = saveRepository,
                soundManager = soundManager,
                hapticManager = hapticManager,
                modifier = Modifier.align(Alignment.BottomEnd).padding(end = 12.dp, bottom = 102.dp),
            )
            FeedbackDialog(
                visible = showFeedbackDialog,
                saveRepository = saveRepository,
                soundManager = soundManager,
                onDismiss = { showFeedbackDialog = false },
            )
        }
    }
}

@Composable
private fun LobbyLandscapeBackdrop(
    modifier: Modifier,
    compact: Boolean,
) {
    Canvas(modifier = modifier) {
        val w = size.width
        val h = size.height
        drawCircle(Color(0x44DDF8FF), radius = w * 0.42f, center = Offset(w * 0.86f, h * 0.12f))
        drawCircle(Color(0x22FFFFFF), radius = w * 0.22f, center = Offset(w * 0.78f, h * 0.18f))
        drawRidge(w, h, 0.56f, Color(0x442A5A73), 0.08f)
        drawRidge(w, h, 0.64f, Color(0x553B7892), 0.12f)
        drawRidge(w, h, 0.73f, Color(0x66D7EEF6), 0.16f)
        drawRect(
            brush =
                Brush.verticalGradient(
                    colors =
                        listOf(
                            Color(0x18FFFFFF),
                            Color(0x44BFE6F2),
                            Color(0x66152B3B),
                        ),
                    startY = h * 0.62f,
                    endY = h,
                ),
            topLeft = Offset(0f, h * if (compact) 0.58f else 0.62f),
        )
        repeat(9) { i ->
            val x = w * (i / 8f)
            val y = h * (0.66f + 0.04f * sin(i * 1.7f))
            drawCircle(Color.White.copy(alpha = 0.1f), radius = w * 0.18f, center = Offset(x, y))
        }
    }
}

private fun DrawScope.drawRidge(
    w: Float,
    h: Float,
    baseRatio: Float,
    color: Color,
    ampRatio: Float,
) {
    val path = Path().apply {
        moveTo(0f, h)
        lineTo(0f, h * baseRatio)
        val steps = 7
        for (i in 0..steps) {
            val x = w * i / steps
            val y = h * (baseRatio - ampRatio * (0.35f + 0.65f * sin(i * 1.31f).coerceAtLeast(0f)))
            lineTo(x, y)
        }
        lineTo(w, h)
        close()
    }
    drawPath(path, color)
}

@Composable
private fun RecommendationCard(recommendation: LobbyRecommendation) {
    val interaction = remember { MutableInteractionSource() }
    val pressed by interaction.collectIsPressedAsState()
    val scale by animateFloatAsState(if (pressed) 0.97f else 1f, label = "recommendation_scale")
    Column(
        modifier =
            Modifier
                .fillMaxWidth()
                .scale(scale)
                .clip(RoundedCornerShape(22.dp))
                .background(Brush.linearGradient(listOf(Color(0x426E9CB1), Color(0x2635F3FF))))
                .border(1.dp, AccentCyan.copy(alpha = 0.68f), RoundedCornerShape(22.dp))
                .clickable(interactionSource = interaction, indication = null, onClick = recommendation.onClick)
                .padding(15.dp),
        verticalArrangement = Arrangement.spacedBy(8.dp),
    ) {
        Row(verticalAlignment = Alignment.CenterVertically, horizontalArrangement = Arrangement.spacedBy(10.dp)) {
            Icon(recommendation.icon, contentDescription = null, tint = AccentCyan, modifier = Modifier.size(21.dp))
            Text(recommendation.title, style = MaterialTheme.typography.titleMedium, color = Color.White, fontWeight = FontWeight.Bold)
        }
        Text(recommendation.subtitle, style = MaterialTheme.typography.bodySmall, color = Color(0xE6EAF7FF), lineHeight = 18.sp)
        Text(recommendation.actionLabel, style = MaterialTheme.typography.labelLarge, color = WarmGold, fontWeight = FontWeight.ExtraBold)
    }
}

@Composable
private fun LobbyTopAssetBar(
    nickname: String,
    bestScore: Int,
    fish: Int,
    rescuedTuanTuan: Boolean,
    drorUnlocked: Boolean,
    developerPulse: Float,
    feedbackCount: Int,
    onFeedback: () -> Unit,
) {
    Row(
        modifier = Modifier.fillMaxWidth().statusBarsPadding().padding(horizontal = 16.dp, vertical = 10.dp),
        horizontalArrangement = Arrangement.SpaceBetween,
        verticalAlignment = Alignment.Top,
    ) {
        Row(horizontalArrangement = Arrangement.spacedBy(8.dp), verticalAlignment = Alignment.CenterVertically) {
            AssetCapsule {
                Icon(Icons.Rounded.Person, contentDescription = null, tint = Color.White, modifier = Modifier.size(16.dp))
                Text(nickname, style = MaterialTheme.typography.labelMedium, color = Color.White, fontWeight = FontWeight.Medium)
            }
            AssetCapsule {
                Icon(Icons.Rounded.EmojiEvents, contentDescription = null, tint = AccentCyan, modifier = Modifier.size(16.dp))
                Text("$bestScore", style = MaterialTheme.typography.labelMedium, color = Color.White, fontWeight = FontWeight.SemiBold)
            }
        }
        Column(horizontalAlignment = Alignment.End, verticalArrangement = Arrangement.spacedBy(6.dp)) {
            Row(horizontalArrangement = Arrangement.spacedBy(6.dp)) {
                CompanionDot("团团", rescuedTuanTuan)
                CompanionDot("Dror", drorUnlocked)
            }
            DeveloperCreditCard(
                pulse = developerPulse,
                feedbackCount = feedbackCount,
                onFeedback = onFeedback,
            )
            AssetCapsule {
                Icon(Icons.Rounded.SetMeal, contentDescription = null, tint = WarmGold, modifier = Modifier.size(16.dp))
                Text("$fish", style = MaterialTheme.typography.labelMedium, color = WarmGold, fontWeight = FontWeight.Bold)
            }
        }
    }
}

@Composable
private fun DeveloperCreditCard(
    pulse: Float,
    feedbackCount: Int,
    onFeedback: () -> Unit,
) {
    Row(
        modifier =
            Modifier
                .scale(pulse)
                .clip(RoundedCornerShape(22.dp))
                .background(Brush.horizontalGradient(listOf(Color(0x66071423), Color(0x334DFFFF))))
                .border(1.dp, AccentCyan.copy(alpha = 0.5f), RoundedCornerShape(22.dp))
                .clickable(onClick = onFeedback)
                .padding(horizontal = 9.dp, vertical = 7.dp),
        verticalAlignment = Alignment.CenterVertically,
        horizontalArrangement = Arrangement.spacedBy(8.dp),
    ) {
        Box(contentAlignment = Alignment.Center) {
            Box(
                modifier =
                    Modifier
                        .size(43.dp)
                        .clip(CircleShape)
                        .background(AccentCyan.copy(alpha = 0.16f)),
            )
            Image(
                painter = painterResource(R.drawable.developer_qq_avatar),
                contentDescription = "开发者 QQ 头像",
                contentScale = ContentScale.Crop,
                modifier =
                    Modifier
                        .size(36.dp)
                        .clip(CircleShape)
                        .border(1.dp, Color.White.copy(alpha = 0.72f), CircleShape),
            )
        }
        Column(modifier = Modifier.widthIn(max = 156.dp), verticalArrangement = Arrangement.spacedBy(1.dp)) {
            Text("开发者", style = MaterialTheme.typography.labelSmall, color = AccentCyan, fontWeight = FontWeight.Bold)
            Text(
                "不是团员为啥要做青年大学习",
                style = MaterialTheme.typography.labelSmall,
                color = Color.White,
                fontWeight = FontWeight.SemiBold,
                maxLines = 2,
                lineHeight = 13.sp,
            )
            Text(
                if (feedbackCount > 0) "反馈 $feedbackCount 条 · 点击提交意见" else "点击提交反馈",
                style = MaterialTheme.typography.labelSmall,
                color = Color(0xB3FFFFFF),
                maxLines = 1,
            )
        }
    }
}

@Composable
private fun AssetCapsule(content: @Composable RowScope.() -> Unit) {
    Row(
        modifier =
            Modifier
                .clip(CircleShape)
                .background(Color(0x4A061423))
                .border(1.dp, Color(0x22FFFFFF), CircleShape)
                .padding(horizontal = 12.dp, vertical = 6.dp),
        verticalAlignment = Alignment.CenterVertically,
        horizontalArrangement = Arrangement.spacedBy(6.dp),
        content = content,
    )
}

@Composable
private fun CompanionDot(name: String, active: Boolean) {
    Row(verticalAlignment = Alignment.CenterVertically, horizontalArrangement = Arrangement.spacedBy(4.dp)) {
        Text(name, style = MaterialTheme.typography.labelSmall, color = if (active) Color.White else Color(0x99FFFFFF))
        Box(Modifier.size(7.dp).clip(CircleShape).background(if (active) AccentCyan else Color(0x55FFFFFF)))
    }
}

@Composable
private fun QuickModeButton(label: String, icon: ImageVector, onClick: () -> Unit) {
    val interaction = remember { MutableInteractionSource() }
    val pressed by interaction.collectIsPressedAsState()
    val scale by animateFloatAsState(if (pressed) 0.96f else 1f, label = "quick_mode_scale")
    Row(
        modifier =
            Modifier
                .scale(scale)
                .clip(RoundedCornerShape(32.dp))
                .background(Brush.horizontalGradient(listOf(Color(0x246A9CB5), Color(0x3835F3FF))))
                .border(1.dp, Color(0x4DFFFFFF), RoundedCornerShape(32.dp))
                .clickable(interactionSource = interaction, indication = null, onClick = onClick)
                .padding(horizontal = 16.dp, vertical = 12.dp),
        verticalAlignment = Alignment.CenterVertically,
        horizontalArrangement = Arrangement.spacedBy(10.dp),
    ) {
        Icon(icon, contentDescription = null, tint = AccentCyan, modifier = Modifier.size(22.dp))
        Text(label, style = MaterialTheme.typography.titleSmall, color = Color.White, fontWeight = FontWeight.SemiBold)
    }
}

@Composable
private fun LobbyDock(
    edgeShimmer: Float,
    showAudioBubble: Boolean,
    onCamp: () -> Unit,
    onCodex: () -> Unit,
    onStory: () -> Unit,
    onLeaderboard: () -> Unit,
    onToggleAudio: () -> Unit,
    modifier: Modifier = Modifier,
) {
    Row(
        modifier =
            modifier
                .fillMaxWidth(0.92f)
                .navigationBarsPadding()
                .padding(bottom = 20.dp)
                .clip(RoundedCornerShape(32.dp))
                .background(Color(0x386E9CB1))
                .border(
                    width = 1.dp,
                    brush = Brush.linearGradient(listOf(AccentCyan.copy(alpha = edgeShimmer * 0.5f), Color(0x77FFFFFF), AccentCyan.copy(alpha = edgeShimmer * 0.45f))),
                    shape = RoundedCornerShape(32.dp),
                )
                .padding(vertical = 10.dp, horizontal = 12.dp),
        horizontalArrangement = Arrangement.SpaceBetween,
        verticalAlignment = Alignment.CenterVertically,
    ) {
        LobbyDockIcon(Icons.Rounded.LocalFireDepartment, "营地", onCamp)
        LobbyDockIcon(Icons.AutoMirrored.Rounded.MenuBook, "图鉴", onCodex)
        val storyInteraction = remember { MutableInteractionSource() }
        val storyPressed by storyInteraction.collectIsPressedAsState()
        val storyScale by animateFloatAsState(if (storyPressed) 0.94f else 1f, label = "story_btn_scale")
        Button(
            onClick = onStory,
            modifier = Modifier.scale(storyScale).height(48.dp).widthIn(min = 120.dp),
            shape = RoundedCornerShape(24.dp),
            interactionSource = storyInteraction,
            colors = ButtonDefaults.buttonColors(containerColor = AccentCyan, contentColor = Color(0xFF0B1420)),
            border = BorderStroke(1.dp, Color(0x99FFFFFF)),
            elevation = ButtonDefaults.buttonElevation(defaultElevation = 0.dp),
        ) {
            Text("开始快跑", style = MaterialTheme.typography.titleSmall, fontWeight = FontWeight.Bold)
        }
        LobbyDockIcon(Icons.Rounded.Leaderboard, "榜单", onLeaderboard)
        LobbyDockIcon(
            Icons.Rounded.SettingsSuggest,
            if (showAudioBubble) "收起" else "体验",
            onToggleAudio,
        )
    }
}

@Composable
private fun LobbyDockIcon(icon: ImageVector, label: String, onClick: () -> Unit) {
    val interaction = remember { MutableInteractionSource() }
    val pressed by interaction.collectIsPressedAsState()
    val scale by animateFloatAsState(if (pressed) 0.92f else 1f, label = "dock_icon_scale")
    Column(horizontalAlignment = Alignment.CenterHorizontally, verticalArrangement = Arrangement.spacedBy(2.dp), modifier = Modifier.width(56.dp)) {
        IconButton(
            onClick = onClick,
            interactionSource = interaction,
            modifier = Modifier.size(44.dp).scale(scale).clip(CircleShape).background(Color(0x30FFFFFF)).border(1.dp, Color(0x3DFFFFFF), CircleShape),
        ) {
            Icon(icon, contentDescription = label, tint = Color.White, modifier = Modifier.size(22.dp))
        }
        Text(label, style = MaterialTheme.typography.labelSmall, color = Color(0xE6FFFFFF), maxLines = 1)
    }
}

@OptIn(ExperimentalMaterial3Api::class)
@Composable
private fun LobbyAudioQuickBubble(
    expanded: Boolean,
    saveRepository: SaveRepository,
    soundManager: SoundManager,
    hapticManager: HapticManager,
    modifier: Modifier = Modifier,
) {
    if (!expanded) return
    var bgmEnabled by remember { mutableStateOf(saveRepository.getBgmEnabled()) }
    var sfxEnabled by remember { mutableStateOf(saveRepository.getSfxEnabled()) }
    var hapticSlider by remember { mutableFloatStateOf(saveRepository.getHapticIntensity()) }
    var lobbyBgmTrack by remember { mutableStateOf(saveRepository.getLobbyBgmTrack()) }
    LaunchedEffect(expanded) {
        bgmEnabled = saveRepository.getBgmEnabled()
        sfxEnabled = saveRepository.getSfxEnabled()
        hapticSlider = saveRepository.getHapticIntensity()
        lobbyBgmTrack = saveRepository.getLobbyBgmTrack()
    }
    val thumbInteraction = remember { MutableInteractionSource() }
    Column(
        modifier = modifier.widthIn(min = 224.dp).clip(RoundedCornerShape(20.dp)).background(Color(0xE6071423)).border(1.dp, Color(0x66FFFFFF), RoundedCornerShape(20.dp)).padding(12.dp),
        verticalArrangement = Arrangement.spacedBy(10.dp),
    ) {
        Text(
            "音频与触觉",
            style = MaterialTheme.typography.labelLarge,
            color = Color.White,
            fontWeight = FontWeight.Bold,
        )
        AudioToggleRow("背景音乐", bgmEnabled, if (bgmEnabled) Icons.Rounded.MusicNote else Icons.Rounded.MusicOff) {
            soundManager.playUiSelect()
            bgmEnabled = !bgmEnabled
            saveRepository.setBgmEnabled(bgmEnabled)
            soundManager.setBgmEnabled(bgmEnabled)
            if (bgmEnabled) {
                val choice = LobbyBgmChoices.firstOrNull { it.key == lobbyBgmTrack } ?: LobbyBgmChoices.first()
                soundManager.setBgm(choice.track)
            }
        }
        Text("首页曲风", style = MaterialTheme.typography.labelMedium, color = Color(0xCCFFFFFF), fontWeight = FontWeight.SemiBold)
        Column(verticalArrangement = Arrangement.spacedBy(6.dp)) {
            LobbyBgmChoices.forEach { choice ->
                LobbyBgmChoiceRow(
                    choice = choice,
                    selected = lobbyBgmTrack == choice.key,
                    onClick = {
                        soundManager.playUiSelect()
                        lobbyBgmTrack = choice.key
                        saveRepository.setLobbyBgmTrack(choice.key)
                        if (choice.track != SoundManager.BgmTrack.None) {
                            bgmEnabled = true
                            saveRepository.setBgmEnabled(true)
                            soundManager.setBgmEnabled(true)
                        }
                        soundManager.setBgm(choice.track)
                    },
                )
            }
        }
        AudioToggleRow("音效", sfxEnabled, if (sfxEnabled) Icons.AutoMirrored.Rounded.VolumeUp else Icons.AutoMirrored.Rounded.VolumeOff) {
            soundManager.playUiSelect()
            sfxEnabled = !sfxEnabled
            saveRepository.setSfxEnabled(sfxEnabled)
            soundManager.setSfxEnabled(sfxEnabled)
        }
        Text("触感力度", style = MaterialTheme.typography.labelMedium, color = Color(0xCCFFFFFF), fontWeight = FontWeight.SemiBold)
        Row(
            modifier = Modifier.fillMaxWidth(),
            verticalAlignment = Alignment.CenterVertically,
            horizontalArrangement = Arrangement.spacedBy(8.dp),
        ) {
            Text("关", style = MaterialTheme.typography.labelSmall, color = Color(0x88FFFFFF), modifier = Modifier.width(20.dp), textAlign = TextAlign.End)
            Slider(
                value = hapticSlider,
                onValueChange = { hapticSlider = it },
                onValueChangeFinished = {
                    saveRepository.setHapticIntensity(hapticSlider)
                    hapticManager.previewWithIntensity(hapticSlider)
                },
                valueRange = 0f..1f,
                modifier = Modifier.weight(1f).height(28.dp),
                colors =
                    SliderDefaults.colors(
                        thumbColor = Color.White,
                        activeTrackColor = AccentCyan,
                        inactiveTrackColor = Color(0x33FFFFFF),
                    ),
                thumb = {
                    SliderDefaults.Thumb(
                        interactionSource = thumbInteraction,
                        colors =
                            SliderDefaults.colors(
                                thumbColor = Color.White,
                                activeTrackColor = AccentCyan,
                                inactiveTrackColor = Color(0x33FFFFFF),
                            ),
                        thumbSize = DpSize(20.dp, 20.dp),
                        modifier = Modifier.shadow(4.dp, CircleShape, ambientColor = AccentCyan.copy(0.45f), spotColor = AccentCyan),
                    )
                },
            )
            Text("强", style = MaterialTheme.typography.labelSmall, color = Color(0x88FFFFFF), modifier = Modifier.width(20.dp))
        }
        Text(
            if (hapticSlider <= 0.02f) "（已关闭震动）" else "当前 ${(hapticSlider * 100f).toInt()}%",
            style = MaterialTheme.typography.labelSmall,
            color = Color(0x66FFFFFF),
            modifier = Modifier.fillMaxWidth(),
        )
    }
}

@Composable
private fun FeedbackDialog(
    visible: Boolean,
    saveRepository: SaveRepository,
    soundManager: SoundManager,
    onDismiss: () -> Unit,
) {
    if (!visible) return
    var category by remember { mutableStateOf("体验建议") }
    var content by remember { mutableStateOf("") }
    var submittedCount by remember { mutableStateOf<Int?>(null) }
    val categories = listOf("体验建议", "玩法道具", "地图关卡", "问题反馈")

    Dialog(onDismissRequest = onDismiss) {
        Column(
            modifier =
                Modifier
                    .fillMaxWidth()
                    .clip(RoundedCornerShape(26.dp))
                    .background(Brush.verticalGradient(listOf(Color(0xF20B1B2D), Color(0xF2255269))))
                    .border(1.dp, AccentCyan.copy(alpha = 0.5f), RoundedCornerShape(26.dp))
                    .padding(18.dp),
            verticalArrangement = Arrangement.spacedBy(12.dp),
        ) {
            Row(verticalAlignment = Alignment.CenterVertically, horizontalArrangement = Arrangement.spacedBy(10.dp)) {
                Image(
                    painter = painterResource(R.drawable.developer_qq_avatar),
                    contentDescription = null,
                    contentScale = ContentScale.Crop,
                    modifier = Modifier.size(44.dp).clip(CircleShape).border(1.dp, Color.White.copy(alpha = 0.75f), CircleShape),
                )
                Column(Modifier.weight(1f)) {
                    Text("玩家反馈", style = MaterialTheme.typography.titleMedium, color = Color.White, fontWeight = FontWeight.Black)
                    Text("提交后会先保存在本地，方便后续整理优化。", style = MaterialTheme.typography.labelSmall, color = Color(0xB3FFFFFF))
                }
            }
            Text(
                "开发者：不是团员为啥要做青年大学习",
                style = MaterialTheme.typography.labelMedium,
                color = AccentCyan,
                fontWeight = FontWeight.Bold,
            )
            Row(horizontalArrangement = Arrangement.spacedBy(8.dp), modifier = Modifier.fillMaxWidth()) {
                categories.forEach { item ->
                    Text(
                        text = item,
                        modifier =
                            Modifier
                                .weight(1f)
                                .clip(RoundedCornerShape(14.dp))
                                .background(if (category == item) AccentCyan.copy(alpha = 0.22f) else Color(0x1FFFFFFF))
                                .border(1.dp, if (category == item) AccentCyan else Color(0x22FFFFFF), RoundedCornerShape(14.dp))
                                .clickable {
                                    soundManager.playUiSelect()
                                    category = item
                                }
                                .padding(vertical = 8.dp),
                        style = MaterialTheme.typography.labelSmall,
                        color = if (category == item) Color.White else Color(0xCCFFFFFF),
                        fontWeight = FontWeight.Bold,
                        textAlign = TextAlign.Center,
                        maxLines = 1,
                    )
                }
            }
            OutlinedTextField(
                value = content,
                onValueChange = { content = it.take(300) },
                modifier = Modifier.fillMaxWidth(),
                label = { Text("写下你的建议或遇到的问题") },
                placeholder = { Text("例如：第二关裂隙太难、希望增加新道具、首页按钮可以更明显……") },
                minLines = 4,
                maxLines = 6,
                singleLine = false,
            )
            Text("${content.length}/300", style = MaterialTheme.typography.labelSmall, color = Color(0x88FFFFFF), modifier = Modifier.fillMaxWidth(), textAlign = TextAlign.End)
            submittedCount?.let {
                Text("已收到第 $it 条反馈，感谢帮企鹅快跑变得更好。", style = MaterialTheme.typography.bodySmall, color = WarmGold, fontWeight = FontWeight.SemiBold)
            }
            Row(modifier = Modifier.fillMaxWidth(), horizontalArrangement = Arrangement.End, verticalAlignment = Alignment.CenterVertically) {
                TextButton(onClick = onDismiss) {
                    Text("关闭", color = Color(0xCCFFFFFF))
                }
                Button(
                    onClick = {
                        soundManager.playPowerUp()
                        submittedCount = saveRepository.savePlayerFeedback(category, content)
                        content = ""
                    },
                    enabled = content.trim().length >= 4,
                    colors = ButtonDefaults.buttonColors(containerColor = AccentCyan, contentColor = Color(0xFF071423)),
                    shape = RoundedCornerShape(18.dp),
                ) {
                    Text("提交反馈", fontWeight = FontWeight.Bold)
                }
            }
        }
    }
}

@Composable
private fun LobbyBgmChoiceRow(
    choice: LobbyBgmChoice,
    selected: Boolean,
    onClick: () -> Unit,
) {
    Row(
        modifier =
            Modifier
                .fillMaxWidth()
                .clip(RoundedCornerShape(14.dp))
                .background(if (selected) AccentCyan.copy(alpha = 0.18f) else Color(0x18FFFFFF))
                .border(1.dp, if (selected) AccentCyan.copy(alpha = 0.74f) else Color(0x22FFFFFF), RoundedCornerShape(14.dp))
                .clickable(onClick = onClick)
                .padding(horizontal = 10.dp, vertical = 8.dp),
        verticalAlignment = Alignment.CenterVertically,
        horizontalArrangement = Arrangement.spacedBy(9.dp),
    ) {
        Box(
            modifier =
                Modifier
                    .size(10.dp)
                    .clip(CircleShape)
                    .background(if (selected) AccentCyan else Color(0x66FFFFFF)),
        )
        Column(modifier = Modifier.weight(1f), verticalArrangement = Arrangement.spacedBy(1.dp)) {
            Text(choice.label, style = MaterialTheme.typography.labelMedium, color = Color.White, fontWeight = FontWeight.Bold)
            Text(choice.description, style = MaterialTheme.typography.labelSmall, color = Color(0xAAFFFFFF), lineHeight = 14.sp)
        }
    }
}

@Composable
private fun AudioToggleRow(label: String, enabled: Boolean, icon: ImageVector, onClick: () -> Unit) {
    Row(
        modifier = Modifier.fillMaxWidth().clip(RoundedCornerShape(12.dp)).background(Color(0x22FFFFFF)).clickable(onClick = onClick).padding(horizontal = 12.dp, vertical = 10.dp),
        verticalAlignment = Alignment.CenterVertically,
        horizontalArrangement = Arrangement.spacedBy(10.dp),
    ) {
        Icon(icon, contentDescription = label, tint = if (enabled) AccentCyan else Color(0xFF90A4AE), modifier = Modifier.size(22.dp))
        Text(label, style = MaterialTheme.typography.bodyMedium, color = Color.White, fontWeight = FontWeight.Medium)
    }
}
