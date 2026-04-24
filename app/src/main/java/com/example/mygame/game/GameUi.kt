package com.example.mygame.game

import android.view.MotionEvent
import androidx.compose.foundation.background
import androidx.compose.foundation.border
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.material3.Card
import androidx.compose.material3.LinearProgressIndicator
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.ui.Alignment
import androidx.compose.ui.ExperimentalComposeUiApi
import androidx.compose.ui.Modifier
import androidx.compose.ui.draw.clip
import androidx.compose.ui.graphics.Brush
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.input.pointer.pointerInteropFilter
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.text.style.TextAlign
import androidx.compose.ui.unit.dp

@OptIn(ExperimentalComposeUiApi::class)
@Composable
fun HoldButton(
    text: String,
    modifier: Modifier = Modifier,
    onPressedChange: (Boolean) -> Unit
) {
    Box(
        modifier = modifier
            .height(56.dp)
            .border(1.dp, Color(0xFF9BBFD4), RoundedCornerShape(16.dp))
            .background(
                brush = Brush.verticalGradient(
                    colors = listOf(Color(0xFFF2F9FF), Color(0xFFD8EAF5))
                ),
                shape = RoundedCornerShape(16.dp)
            )
            .pointerInteropFilter { event ->
                when (event.action) {
                    MotionEvent.ACTION_DOWN -> {
                        onPressedChange(true)
                        true
                    }

                    MotionEvent.ACTION_UP, MotionEvent.ACTION_CANCEL -> {
                        onPressedChange(false)
                        true
                    }

                    else -> true
                }
            },
        contentAlignment = Alignment.Center
    ) {
        Text(
            text = text,
            style = MaterialTheme.typography.titleMedium,
            fontWeight = FontWeight.Bold,
            color = Color(0xFF244256)
        )
    }
}

@Composable
fun ScoreBoard(
    score: Int,
    bestScore: Int,
    coinsCollected: Int,
    progress: Int,
    fishDashTimer: Float,
    hasBubbleScarf: Boolean,
    rescuedTuanTuan: Boolean,
    tuanTuanAssistReady: Boolean,
    tuanTuanAssistTimer: Float,
    goalStatusLine: String,
) {
    Card(
        modifier = Modifier.fillMaxWidth(),
        shape = RoundedCornerShape(24.dp)
    ) {
        Column(
            modifier = Modifier
                .fillMaxWidth()
                .padding(horizontal = 18.dp, vertical = 16.dp),
            verticalArrangement = Arrangement.spacedBy(12.dp)
        ) {
            androidx.compose.foundation.layout.Row(
                modifier = Modifier.fillMaxWidth(),
                horizontalArrangement = Arrangement.SpaceBetween,
                verticalAlignment = Alignment.CenterVertically
            ) {
                StatBlock(label = "得分", value = score.toString())
                StatBlock(label = "最高分", value = bestScore.toString())
                StatBlock(label = "小鱼干", value = coinsCollected.toString())
                StatBlock(label = "进度", value = "$progress%")
            }
            LinearProgressIndicator(
                progress = { progress.coerceIn(0, 100) / 100f },
                modifier = Modifier
                    .fillMaxWidth()
                    .height(8.dp)
                    .clip(RoundedCornerShape(4.dp)),
                color = Color(0xFF2E7D32),
                trackColor = Color(0xFFE3ECEF),
            )
            androidx.compose.foundation.layout.Row(
                modifier = Modifier.fillMaxWidth(),
                horizontalArrangement = Arrangement.spacedBy(8.dp)
            ) {
                BuffChip(label = "鱼干", active = fishDashTimer > 0f)
                BuffChip(label = "围巾", active = hasBubbleScarf)
                BuffChip(label = "团团", active = rescuedTuanTuan)
                BuffChip(label = "支援", active = tuanTuanAssistReady && tuanTuanAssistTimer <= 0f)
            }
            Text(
                text = when {
                    tuanTuanAssistTimer > 0f ->
                        "状态：团团正在投掷雪团掩护你"
                    fishDashTimer > 0f && hasBubbleScarf ->
                        "状态：鱼干冲刺已生效，泡泡围巾已装备"
                    fishDashTimer > 0f ->
                        "状态：鱼干冲刺中"
                    hasBubbleScarf ->
                        "状态：泡泡围巾可以缓降"
                    rescuedTuanTuan && tuanTuanAssistReady ->
                        "状态：团团已待命，可发动一次支援"
                    rescuedTuanTuan ->
                        "状态：团团已加入队伍"
                    else ->
                        goalStatusLine
                },
                style = MaterialTheme.typography.bodyMedium,
                color = Color(0xFF54606D)
            )
        }
    }
}

@Composable
fun ChapterPreviewCard(
    title: String,
    description: String
) {
    Box(modifier = Modifier.fillMaxSize()) {
        Box(
            modifier = Modifier
                .fillMaxSize()
                .background(Color(0x99000000))
        )
        Box(
            modifier = Modifier.fillMaxSize(),
            contentAlignment = Alignment.TopCenter
        ) {
            Card(
                modifier = Modifier
                    .fillMaxWidth()
                    .padding(horizontal = 24.dp, vertical = 20.dp),
                shape = RoundedCornerShape(24.dp)
            ) {
                Column(
                    modifier = Modifier.padding(20.dp),
                    verticalArrangement = Arrangement.spacedBy(10.dp),
                    horizontalAlignment = Alignment.Start
                ) {
                    Text(
                        text = title,
                        style = MaterialTheme.typography.titleLarge,
                        fontWeight = FontWeight.Bold
                    )
                    Text(
                        text = description,
                        style = MaterialTheme.typography.bodyMedium
                    )
                }
            }
        }
    }
}

@Composable
private fun BuffChip(label: String, active: Boolean) {
    Text(
        text = label,
        modifier = Modifier
            .clip(RoundedCornerShape(8.dp))
            .background(
                if (active) Color(0xFFC8E6C9) else Color(0xFFECEFF1)
            )
            .padding(horizontal = 8.dp, vertical = 4.dp),
        style = MaterialTheme.typography.labelSmall,
        color = if (active) Color(0xFF1B5E20) else Color(0xFF607D8B)
    )
}

@Composable
fun StatBlock(label: String, value: String) {
    Column(horizontalAlignment = Alignment.CenterHorizontally) {
        Text(
            text = label,
            style = MaterialTheme.typography.labelMedium,
            color = Color(0xFF5B6470)
        )
        Text(
            text = value,
            style = MaterialTheme.typography.headlineSmall,
            fontWeight = FontWeight.Bold,
            color = Color(0xFF202733)
        )
    }
}

@Composable
fun OverlayCard(
    title: String,
    description: String
) {
    Box(modifier = Modifier.fillMaxSize()) {
        Box(
            modifier = Modifier
                .fillMaxSize()
                .background(Color(0x99000000))
        )
        Box(
            modifier = Modifier.fillMaxSize(),
            contentAlignment = Alignment.Center
        ) {
            Card(
                modifier = Modifier
                    .fillMaxWidth()
                    .padding(24.dp),
                shape = RoundedCornerShape(28.dp)
            ) {
                Column(
                    modifier = Modifier.padding(24.dp),
                    verticalArrangement = Arrangement.spacedBy(14.dp),
                    horizontalAlignment = Alignment.CenterHorizontally
                ) {
                    Text(
                        text = title,
                        style = MaterialTheme.typography.headlineMedium,
                        fontWeight = FontWeight.Bold
                    )
                    Text(
                        text = description,
                        style = MaterialTheme.typography.bodyLarge,
                        textAlign = TextAlign.Center
                    )
                    Text(
                        text = "点击屏幕开始或重试",
                        style = MaterialTheme.typography.labelLarge,
                        color = Color(0xFF5B6470)
                    )
                }
            }
        }
    }
}
