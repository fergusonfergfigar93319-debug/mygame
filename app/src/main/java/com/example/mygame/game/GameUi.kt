package com.example.mygame.game

import androidx.compose.foundation.background
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.width
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.material3.LinearProgressIndicator
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.draw.clip
import androidx.compose.ui.graphics.Brush
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import com.example.mygame.ui.common.FloatingHudStatusLabel
import com.example.mygame.ui.common.GameActionHoldButton
import com.example.mygame.ui.common.GameHeroCard
import com.example.mygame.ui.common.GameHudShell
import com.example.mygame.ui.common.GameInfoPill
import com.example.mygame.ui.common.GameOverlayPanel
import com.example.mygame.ui.common.floatingHudTextShadow

@Composable
fun HoldButton(
    text: String,
    modifier: Modifier = Modifier,
    onPressedChange: (Boolean) -> Unit,
) {
    GameActionHoldButton(
        text = text,
        modifier = modifier,
        onPressedChange = onPressedChange,
    )
}

@Composable
fun ScoreBoard(
    score: Int,
    bestScore: Int,
    coinsCollected: Int,
    progress: Int,
    fishDashTimer: Float,
    hasBubbleScarf: Boolean,
    hasSnowShield: Boolean,
    gustBootsTimer: Float,
    auroraMagnetTimer: Float,
    rescuedTuanTuan: Boolean,
    tuanTuanAssistReady: Boolean,
    tuanTuanAssistTimer: Float,
    goalStatusLine: String,
    modifier: Modifier = Modifier,
) {
    Column(
        modifier = modifier,
        verticalArrangement = Arrangement.spacedBy(6.dp),
    ) {
        Text(
            text = "主线状态",
            style =
                MaterialTheme.typography.titleMedium.copy(
                    shadow = floatingHudTextShadow(),
                ),
            fontWeight = FontWeight.Black,
            color = Color.White,
        )
        Row(
            modifier = Modifier.fillMaxWidth(),
            horizontalArrangement = Arrangement.SpaceBetween,
            verticalAlignment = Alignment.CenterVertically,
        ) {
            FloatingHudStat(label = "得分", value = score.toString())
            FloatingHudStat(label = "最高分", value = bestScore.toString())
            FloatingHudStat(label = "小鱼干", value = coinsCollected.toString())
            FloatingHudStat(label = "进度", value = "$progress%")
        }
        LinearProgressIndicator(
            progress = { progress.coerceIn(0, 100) / 100f },
            modifier =
                Modifier
                    .fillMaxWidth()
                    .height(5.dp)
                    .clip(RoundedCornerShape(999.dp)),
            color = Color(0xE664FFDA),
            trackColor = Color(0x33FFFFFF),
        )
        Row(horizontalArrangement = Arrangement.spacedBy(10.dp)) {
            FloatingHudStatusLabel("鱼干冲刺", fishDashTimer > 0f)
            FloatingHudStatusLabel("泡泡围巾", hasBubbleScarf)
            FloatingHudStatusLabel("雪盾", hasSnowShield)
            FloatingHudStatusLabel("长跳靴", gustBootsTimer > 0f)
        }
        Row(horizontalArrangement = Arrangement.spacedBy(10.dp)) {
            FloatingHudStatusLabel("磁针", auroraMagnetTimer > 0f)
            FloatingHudStatusLabel("团团", rescuedTuanTuan)
            FloatingHudStatusLabel("支援", tuanTuanAssistReady && tuanTuanAssistTimer <= 0f)
        }
        Text(
            text =
                when {
                    auroraMagnetTimer > 0f -> "状态：极光磁针正在吸引附近鱼干，适合冲进金币线。"
                    hasSnowShield && gustBootsTimer > 0f -> "状态：护盾和长跳靴都已激活，适合大胆推进。"
                    tuanTuanAssistTimer > 0f -> "状态：团团正在用雪球掩护你前进。"
                    fishDashTimer > 0f && hasBubbleScarf -> "状态：鱼干冲刺和泡泡围巾同时生效，节奏很强。"
                    fishDashTimer > 0f -> "状态：鱼干冲刺生效中，可以撞开部分危险。"
                    hasBubbleScarf -> "状态：泡泡围巾会帮你缓降。"
                    hasSnowShield -> "状态：雪盾可以抵挡一次碰撞。"
                    gustBootsTimer > 0f -> "状态：长跳靴生效中，跳得更高更远。"
                    rescuedTuanTuan && tuanTuanAssistReady -> "状态：团团已待命，随时可以发动支援。"
                    rescuedTuanTuan -> "状态：团团已经回到队伍。"
                    else -> goalStatusLine
                },
            style =
                MaterialTheme.typography.bodySmall.copy(
                    shadow = floatingHudTextShadow(),
                ),
            color = Color(0xFFF5F5F5),
            lineHeight = 18.sp,
        )
    }
}

@Composable
private fun FloatingHudStat(
    label: String,
    value: String,
) {
    Column(horizontalAlignment = Alignment.CenterHorizontally) {
        Text(
            text = label,
            style =
                MaterialTheme.typography.labelSmall.copy(
                    shadow = floatingHudTextShadow(),
                ),
            color = Color(0xE0ECEFF1),
        )
        Text(
            text = value,
            style =
                MaterialTheme.typography.titleMedium.copy(
                    shadow = floatingHudTextShadow(),
                ),
            fontWeight = FontWeight.Black,
            color = Color.White,
        )
    }
}

@Composable
fun TutorialHintCard(
    title: String,
    description: String,
    modifier: Modifier = Modifier,
) {
    GameHudShell(
        title = title,
        modifier = modifier,
        titleColor = Color(0xFF1F6A8A),
    ) {
        Row(
            horizontalArrangement = Arrangement.spacedBy(12.dp),
            verticalAlignment = Alignment.CenterVertically,
        ) {
            Box(
                modifier =
                    Modifier
                        .width(6.dp)
                        .height(42.dp)
                        .clip(RoundedCornerShape(999.dp))
                        .background(
                            Brush.verticalGradient(
                                listOf(Color(0xFF4FC3F7), Color(0xFF81D4FA)),
                            ),
                        ),
            )
            Text(
                text = description,
                style = MaterialTheme.typography.bodyMedium,
                color = Color(0xFF4C6170),
            )
        }
    }
}

@Composable
fun JourneyHeaderCard(
    title: String,
    subtitle: String,
    accentColor: Color,
    tags: List<String>,
    modifier: Modifier = Modifier,
) {
    Column(
        modifier = modifier.fillMaxWidth(),
        verticalArrangement = Arrangement.spacedBy(8.dp),
    ) {
        GameHeroCard(
            title = title,
            subtitle = subtitle,
            accentStart = accentColor.copy(alpha = 0.9f),
            accentEnd = Color.White.copy(alpha = 0.18f),
        )
        if (tags.isNotEmpty()) {
            Row(horizontalArrangement = Arrangement.spacedBy(8.dp)) {
                tags.take(4).forEach { tag ->
                    GameInfoPill(label = tag)
                }
            }
        }
    }
}

@Composable
fun ChapterPreviewCard(
    title: String,
    description: String,
) {
    GameOverlayPanel(
        title = title,
        description = description,
        alignTop = true,
    )
}

@Composable
fun OverlayCard(
    title: String,
    description: String,
) {
    GameOverlayPanel(
        title = title,
        description = description,
        footnote = "点击屏幕开始或重试",
    )
}
