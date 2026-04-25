package com.example.mygame.ui.home

import androidx.compose.animation.core.LinearEasing
import androidx.compose.animation.core.RepeatMode
import androidx.compose.animation.core.animateFloat
import androidx.compose.animation.core.infiniteRepeatable
import androidx.compose.animation.core.rememberInfiniteTransition
import androidx.compose.animation.core.tween
import androidx.compose.foundation.Canvas
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.runtime.Composable
import androidx.compose.runtime.getValue
import androidx.compose.ui.Modifier
import androidx.compose.ui.draw.alpha
import androidx.compose.ui.geometry.Offset
import androidx.compose.ui.graphics.Color
import kotlin.math.sin

/**
 * 大厅极夜雪花氛围：纯 [Canvas] 绘制，无额外资源。
 */
@Composable
fun LobbySnowEffect(modifier: Modifier = Modifier) {
    val infiniteTransition = rememberInfiniteTransition(label = "lobby_snow")
    val phase by infiniteTransition.animateFloat(
        initialValue = 0f,
        targetValue = 1f,
        animationSpec =
            infiniteRepeatable(
                animation = tween(8000, easing = LinearEasing),
                repeatMode = RepeatMode.Restart,
            ),
        label = "snow_phase",
    )

    Canvas(modifier = modifier.fillMaxSize().alpha(0.3f)) {
        val w = size.width
        val h = size.height
        if (w <= 0f || h <= 0f) return@Canvas
        val count = 42
        for (i in 0 until count) {
            val seed = i.toFloat() * 9973.571f
            val baseX = (seed * 1.7f % w + w) % w
            val baseY = (seed * 0.31f % h + h) % h
            val driftX = phase * w * 0.08f
            val driftY = phase * h * 0.45f
            val sway = sin((phase + i / count.toFloat()) * 6.283185f).toFloat() * 14f
            val x = (baseX + driftX + sway + w) % w
            val y = (baseY + driftY + h) % h
            val radiusPx = 1.1f + (i % 4)
            val twinkle =
                (0.35f + 0.35f * sin((phase * 12f + seed * 0.01f).toDouble()).toFloat()).coerceIn(
                    0.15f,
                    0.88f,
                )
            drawCircle(
                color = Color.White.copy(alpha = twinkle),
                radius = radiusPx,
                center = Offset(x, y),
            )
        }
    }
}
