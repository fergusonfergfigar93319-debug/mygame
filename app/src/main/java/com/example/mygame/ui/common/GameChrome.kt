package com.example.mygame.ui.common

import androidx.compose.foundation.Canvas
import androidx.compose.foundation.background
import androidx.compose.foundation.border
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.ColumnScope
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.shape.CircleShape
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.material3.Card
import androidx.compose.material3.CardDefaults
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.Text
import androidx.compose.material3.TextButton
import androidx.compose.runtime.Composable
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.geometry.Offset
import androidx.compose.ui.graphics.Brush
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.graphics.Path
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import kotlin.math.sin

val GameShellGradient =
    listOf(
        Color(0xFFECF8FD),
        Color(0xFFD7ECF7),
        Color(0xFFB8D8E8),
    )

@Composable
fun GameScreenBackground(
    modifier: Modifier = Modifier,
    content: @Composable ColumnScope.() -> Unit,
) {
    Box(
        modifier =
            modifier
                .fillMaxSize()
                .background(Brush.verticalGradient(GameShellGradient)),
    ) {
        Canvas(Modifier.fillMaxSize()) {
            val w = size.width
            val h = size.height
            drawCircle(Color(0x55FFFFFF), radius = w * 0.45f, center = Offset(w * 0.86f, h * 0.08f))
            drawCircle(Color(0x2D64FFDA), radius = w * 0.32f, center = Offset(w * 0.1f, h * 0.28f))
            drawGameShellRidge(w, h, 0.72f, Color(0x44FFFFFF), 0.12f)
            drawGameShellRidge(w, h, 0.82f, Color(0x66C2E4F0), 0.08f)
        }
        Column(
            modifier =
                Modifier
                    .fillMaxSize()
                    .padding(start = 18.dp, end = 18.dp, top = 16.dp, bottom = 32.dp),
            verticalArrangement = Arrangement.spacedBy(12.dp),
            content = content,
        )
    }
}

private fun androidx.compose.ui.graphics.drawscope.DrawScope.drawGameShellRidge(
    w: Float,
    h: Float,
    baseRatio: Float,
    color: Color,
    ampRatio: Float,
) {
    val path =
        Path().apply {
            moveTo(0f, h)
            lineTo(0f, h * baseRatio)
            repeat(8) { i ->
                val x = w * i / 7f
                val y = h * (baseRatio - ampRatio * (0.35f + 0.65f * sin(i * 1.42f).coerceAtLeast(0f)))
                lineTo(x, y)
            }
            lineTo(w, h)
            close()
        }
    drawPath(path, color)
}

@Composable
fun GameHeroCard(
    title: String,
    subtitle: String,
    modifier: Modifier = Modifier,
    accentStart: Color = Color(0xFF214562),
    accentEnd: Color = Color(0xFF73B7D4),
) {
    Card(
        modifier = modifier.fillMaxWidth(),
        shape = RoundedCornerShape(24.dp),
        colors = CardDefaults.cardColors(containerColor = Color.Transparent),
        elevation = CardDefaults.cardElevation(defaultElevation = 0.dp),
    ) {
        Column(
            modifier =
                Modifier
                    .fillMaxWidth()
                    .background(
                        Brush.linearGradient(
                            listOf(accentStart, accentEnd, Color(0xFFB9E4EF)),
                        ),
                    )
                    .border(1.dp, Color(0x55FFFFFF), RoundedCornerShape(24.dp))
                    .padding(20.dp),
            verticalArrangement = Arrangement.spacedBy(8.dp),
        ) {
            Text(title, style = MaterialTheme.typography.headlineMedium, fontWeight = FontWeight.ExtraBold, color = Color.White)
            Text(subtitle, style = MaterialTheme.typography.bodyMedium, color = Color(0xFFEAF8FF), lineHeight = 22.sp, letterSpacing = 0.2.sp)
        }
    }
}

@Composable
fun GameSectionCard(
    modifier: Modifier = Modifier,
    content: @Composable ColumnScope.() -> Unit,
) {
    Card(
        modifier = modifier.fillMaxWidth(),
        shape = RoundedCornerShape(20.dp),
        colors = CardDefaults.cardColors(containerColor = Color(0xF2F8FCFF)),
        elevation = CardDefaults.cardElevation(defaultElevation = 0.5.dp),
    ) {
        Column(
            modifier =
                Modifier
                    .fillMaxWidth()
                    .border(1.dp, Color(0xAAFFFFFF), RoundedCornerShape(20.dp))
                    .padding(14.dp),
            verticalArrangement = Arrangement.spacedBy(8.dp),
            content = content,
        )
    }
}

@Composable
fun GameInfoPill(
    label: String,
    active: Boolean = true,
    modifier: Modifier = Modifier,
) {
    Row(
        modifier =
            modifier
                .background(if (active) Color(0xFFDDF7F0) else Color(0xFFE8F0F5), RoundedCornerShape(999.dp))
                .padding(horizontal = 10.dp, vertical = 6.dp),
        verticalAlignment = Alignment.CenterVertically,
        horizontalArrangement = Arrangement.spacedBy(6.dp),
    ) {
        Box(
            modifier =
                Modifier
                    .background(if (active) Color(0xFF1AAE8C) else Color(0xFF90A4AE), CircleShape)
                    .padding(4.dp),
        )
        Text(text = label, style = MaterialTheme.typography.labelMedium, color = if (active) Color(0xFF136B5A) else Color(0xFF607D8B))
    }
}

@Composable
fun GameBackRow(
    title: String,
    onBack: () -> Unit,
    modifier: Modifier = Modifier,
    trailing: @Composable (() -> Unit)? = null,
) {
    Row(
        modifier = modifier.fillMaxWidth(),
        horizontalArrangement = Arrangement.SpaceBetween,
        verticalAlignment = Alignment.CenterVertically,
    ) {
        TextButton(onClick = onBack) {
            Text("返回")
        }
        Text(title, style = MaterialTheme.typography.titleLarge, fontWeight = FontWeight.Black, color = Color(0xFF183247))
        Box(contentAlignment = Alignment.CenterEnd) {
            trailing?.invoke() ?: Box(modifier = Modifier.padding(horizontal = 20.dp))
        }
    }
}
