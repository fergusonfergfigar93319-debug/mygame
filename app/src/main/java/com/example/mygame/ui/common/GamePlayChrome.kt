package com.example.mygame.ui.common

import androidx.compose.foundation.background
import androidx.compose.foundation.border
import androidx.compose.foundation.gestures.awaitEachGesture
import androidx.compose.foundation.gestures.awaitFirstDown
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.ColumnScope
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.navigationBarsPadding
import androidx.compose.foundation.layout.size
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.material3.Icon
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.Surface
import androidx.compose.material3.Text
import androidx.compose.material3.TextButton
import androidx.compose.runtime.Composable
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.draw.clip
import androidx.compose.ui.geometry.Offset
import androidx.compose.ui.graphics.Brush
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.graphics.Shadow
import androidx.compose.ui.graphics.vector.ImageVector
import androidx.compose.ui.input.pointer.pointerInput
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.text.style.TextAlign
import androidx.compose.ui.text.style.TextOverflow
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp

/** 无底板 HUD：雪地/蓝天背景下保证正文可读。 */
fun floatingHudTextShadow(): Shadow =
    Shadow(
        color = Color(0xCC000000),
        offset = Offset(1.5f, 2.5f),
        blurRadius = 5f,
    )

@Composable
fun FloatingHudStatusLabel(
    label: String,
    active: Boolean,
    modifier: Modifier = Modifier,
) {
    Text(
        text = label,
        modifier = modifier,
        style =
            MaterialTheme.typography.labelMedium.copy(
                shadow = floatingHudTextShadow(),
            ),
        color =
            if (active) {
                Color(0xFFB3E5FC)
            } else {
                Color(0xFF78909C).copy(alpha = 0.65f)
            },
        fontWeight = if (active) FontWeight.Bold else FontWeight.Medium,
    )
}

@Composable
fun FloatingHudIconStat(
    icon: ImageVector,
    label: String,
    value: String,
    modifier: Modifier = Modifier,
    iconTint: Color = Color(0xFF90CAF9),
) {
    Row(
        modifier = modifier,
        verticalAlignment = Alignment.CenterVertically,
        horizontalArrangement = Arrangement.spacedBy(4.dp),
    ) {
        Icon(icon, contentDescription = null, modifier = Modifier.size(16.dp), tint = iconTint)
        Column {
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
                    MaterialTheme.typography.bodySmall.copy(
                        shadow = floatingHudTextShadow(),
                    ),
                color = Color.White,
                fontWeight = FontWeight.SemiBold,
            )
        }
    }
}

@Composable
fun FloatingHudIconResource(
    icon: ImageVector,
    value: String,
    modifier: Modifier = Modifier,
    iconTint: Color = Color(0xFF80CBC4),
) {
    Row(
        modifier = modifier,
        verticalAlignment = Alignment.CenterVertically,
        horizontalArrangement = Arrangement.spacedBy(2.dp),
    ) {
        Icon(icon, contentDescription = null, modifier = Modifier.size(14.dp), tint = iconTint)
        Text(
            text = value,
            style =
                MaterialTheme.typography.labelLarge.copy(
                    shadow = floatingHudTextShadow(),
                ),
            color = Color.White,
            fontWeight = FontWeight.Bold,
        )
    }
}

@Composable
fun GameActionHoldButton(
    text: String,
    modifier: Modifier = Modifier,
    onPressedChange: (Boolean) -> Unit,
) {
    Box(
        modifier =
            modifier
                .height(56.dp)
                .border(1.dp, Color(0xFF9BBFD4), RoundedCornerShape(16.dp))
                .background(
                    brush = Brush.verticalGradient(listOf(Color(0xFFF2F9FF), Color(0xFFD8EAF5))),
                    shape = RoundedCornerShape(16.dp),
                )
                .pointerInput(onPressedChange) {
                    awaitEachGesture {
                        awaitFirstDown(requireUnconsumed = false)
                        onPressedChange(true)
                        try {
                            do {
                                val event = awaitPointerEvent()
                                val stillPressed = event.changes.any { it.pressed }
                            } while (stillPressed)
                        } finally {
                            onPressedChange(false)
                        }
                    }
                },
        contentAlignment = Alignment.Center,
    ) {
        Text(
            text = text,
            style = MaterialTheme.typography.titleMedium,
            fontWeight = FontWeight.Bold,
            color = Color(0xFF244256),
        )
    }
}

@Composable
fun GameHudShell(
    title: String,
    modifier: Modifier = Modifier,
    titleColor: Color = Color(0xFF1B3248),
    content: @Composable ColumnScope.() -> Unit,
) {
    GameSectionCard(modifier = modifier) {
        Text(
            text = title,
            style = MaterialTheme.typography.titleMedium,
            fontWeight = FontWeight.Bold,
            color = titleColor,
        )
        content()
    }
}

@Composable
fun GameOverlayPanel(
    title: String,
    description: String,
    modifier: Modifier = Modifier,
    footnote: String? = null,
    alignTop: Boolean = false,
    content: @Composable (ColumnScope.() -> Unit)? = null,
) {
    Box(modifier = Modifier.fillMaxSize()) {
        Box(
            modifier =
                Modifier
                    .fillMaxSize()
                    .background(Color(0x99000000)),
        )
        Box(
            modifier = Modifier.fillMaxSize(),
            contentAlignment = if (alignTop) Alignment.TopCenter else Alignment.Center,
        ) {
            GameSectionCard(
                modifier =
                    modifier
                        .fillMaxWidth()
                        .padding(horizontal = 24.dp, vertical = if (alignTop) 20.dp else 24.dp),
            ) {
                Text(
                    text = title,
                    style = MaterialTheme.typography.headlineSmall,
                    fontWeight = FontWeight.ExtraBold,
                )
                Text(
                    text = description,
                    style = MaterialTheme.typography.bodyLarge,
                    textAlign = TextAlign.Center,
                )
                content?.invoke(this)
                if (!footnote.isNullOrBlank()) {
                    Text(
                        text = footnote,
                        style = MaterialTheme.typography.labelLarge,
                        color = Color(0xFF5B6470),
                    )
                }
            }
        }
    }
}

/**
 * 全屏玩法顶部条：与舞台画布分离的轻量信息栏（替代大段「英雄卡」式头部）。
 */
@Composable
fun GameStageTopBar(
    title: String,
    subtitle: String,
    tags: List<String>,
    onBack: (() -> Unit)?,
    accentColor: Color,
    modifier: Modifier = Modifier,
) {
    Column(
        modifier = modifier,
        verticalArrangement = Arrangement.spacedBy(4.dp),
    ) {
        Row(
            verticalAlignment = Alignment.CenterVertically,
            horizontalArrangement = Arrangement.spacedBy(4.dp),
        ) {
            onBack?.let { back ->
                TextButton(onClick = back) { Text("返回", fontSize = 14.sp) }
            }
            Column(modifier = Modifier.weight(1f)) {
                Text(
                    text = title,
                    style = MaterialTheme.typography.titleMedium,
                    fontWeight = FontWeight.ExtraBold,
                    color = Color(0xFF0D1B2A),
                    maxLines = 1,
                    overflow = TextOverflow.Ellipsis,
                )
                Text(
                    text = subtitle,
                    style = MaterialTheme.typography.labelMedium,
                    color = Color(0xFF3D5360),
                    maxLines = 2,
                    overflow = TextOverflow.Ellipsis,
                )
            }
        }
        if (tags.isNotEmpty()) {
            Row(horizontalArrangement = Arrangement.spacedBy(6.dp)) {
                tags.take(4).forEach { tag ->
                    Surface(
                        shape = RoundedCornerShape(999.dp),
                        color = accentColor.copy(alpha = 0.2f),
                    ) {
                        Text(
                            text = tag,
                            modifier = Modifier.padding(horizontal = 8.dp, vertical = 2.dp),
                            style = MaterialTheme.typography.labelSmall,
                            color = Color(0xFF1A3044),
                        )
                    }
                }
            }
        }
    }
}

@Composable
fun GameStageControlDock(
    modifier: Modifier = Modifier,
    content: @Composable ColumnScope.() -> Unit,
) {
    Box(
        modifier =
            modifier
                .fillMaxWidth()
                .navigationBarsPadding(),
    ) {
        GameSectionCard(
            modifier =
                Modifier
                    .fillMaxWidth()
                    .clip(RoundedCornerShape(28.dp)),
        ) {
            content()
        }
    }
}
