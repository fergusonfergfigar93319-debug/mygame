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
 * 补给营地营火感：自屏幕下缘向上升起的细小火星与烟点，无额外资源。
 */
@Composable
fun CampFireSparksEffect(modifier: Modifier = Modifier) {
    val transition = rememberInfiniteTransition(label = "camp_fire")
    val phase by transition.animateFloat(
        initialValue = 0f,
        targetValue = 1f,
        animationSpec =
            infiniteRepeatable(
                animation = tween(9_200, easing = LinearEasing),
                repeatMode = RepeatMode.Restart,
            ),
        label = "ember_phase",
    )
    val warmA = Color(0xFFFF7A45)
    val warmB = Color(0xFFFFD54A)

    Canvas(modifier = modifier.fillMaxSize().alpha(0.55f)) {
        val w = size.width
        val h = size.height
        if (w <= 0f || h <= 0f) return@Canvas
        val count = 56
        for (i in 0 until count) {
            val seed = i * 8803.7f
            val t = (phase * 0.9f + seed * 0.0017f) % 1f
            // 自下方向上飘，近顶端渐隐
            val y = h * 0.8f - t * h * 0.68f
            if (y < h * 0.06f) continue
            val sway = (sin((phase * 20f + i * 0.4f).toDouble()).toFloat() * 20f)
            val x = ((seed * 0.37f) % (w * 0.9f) + w * 0.05f + sway)
            val radiusPx = 0.9f + (i % 5) * 0.32f
            val riseAlpha = 0.18f + 0.7f * (1f - t)
            val tw =
                0.45f + 0.5f * (sin((phase * 9f + i * 0.19f).toDouble()).toFloat() * 0.5f + 0.5f)
            val fade = (riseAlpha * tw).coerceIn(0.1f, 0.9f)
            val mix = (i and 1) == 0
            val c = (if (mix) warmA else warmB).copy(alpha = fade * 0.7f)
            drawCircle(
                color = c,
                radius = radiusPx,
                center = Offset(
                    (x + w) % w,
                    y,
                ),
            )
        }
    }
}
