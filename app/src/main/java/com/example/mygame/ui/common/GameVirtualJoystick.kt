package com.example.mygame.ui.common

import androidx.compose.foundation.background
import androidx.compose.foundation.gestures.awaitEachGesture
import androidx.compose.foundation.gestures.awaitFirstDown
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.offset
import androidx.compose.foundation.layout.size
import androidx.compose.foundation.shape.CircleShape
import androidx.compose.runtime.Composable
import androidx.compose.runtime.LaunchedEffect
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.setValue
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.draw.clip
import androidx.compose.ui.geometry.Offset
import androidx.compose.ui.graphics.Brush
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.input.pointer.pointerInput
import androidx.compose.ui.unit.Dp
import androidx.compose.ui.unit.IntOffset
import androidx.compose.ui.unit.dp
import kotlin.math.hypot
import kotlin.math.min
import kotlin.math.roundToInt

/**
 * 左下区域虚拟横轴摇杆：输出 [-1,1] 的水平分量，带死区与抬手归零。
 */
@Composable
fun GameVirtualJoystick(
    onHorizontalChange: (Float) -> Unit,
    modifier: Modifier = Modifier,
    totalSize: Dp = 128.dp,
    /** 与父级同步时（重开一局等）归零。 */
    resetKey: Int = 0,
) {
    var stickOffset by remember { mutableStateOf(Offset.Zero) }

    LaunchedEffect(resetKey) {
        stickOffset = Offset.Zero
        onHorizontalChange(0f)
    }

    Box(
        modifier = modifier
            .size(totalSize)
            .clip(CircleShape)
            .background(
                Brush.radialGradient(
                    listOf(
                        Color(0x66E3F2FD),
                        Color(0x22FFFFFF),
                    ),
                ),
            )
            .pointerInput(Unit) {
                val rMax = min(size.width, size.height) * 0.38f
                awaitEachGesture {
                    val down = awaitFirstDown(requireUnconsumed = false)
                    fun updateFrom(position: Offset) {
                        val c = Offset(size.width * 0.5f, size.height * 0.5f)
                        val d = position - c
                        val m = hypot(d.x, d.y)
                        val p = if (m > rMax) d / m * rMax else d
                        stickOffset = p
                        onHorizontalChange(normalizeH(p.x, rMax))
                    }
                    updateFrom(down.position)
                    do {
                        val event = awaitPointerEvent()
                        val active = event.changes.firstOrNull { it.id == down.id && it.pressed }
                        if (active != null) updateFrom(active.position)
                    } while (active != null)
                    stickOffset = Offset.Zero
                    onHorizontalChange(0f)
                }
            },
        contentAlignment = Alignment.Center,
    ) {
        Box(Modifier.fillMaxSize()) {
            Box(
                modifier = Modifier
                    .align(Alignment.Center)
                    .offset {
                        IntOffset(
                            stickOffset.x.roundToInt(),
                            stickOffset.y.roundToInt(),
                        )
                    }
                    .size(44.dp)
                    .clip(CircleShape)
                    .background(Brush.linearGradient(listOf(Color(0xE8F5FE), Color(0xFFB3E5FC)))),
            )
        }
    }
}

private fun normalizeH(x: Float, r: Float): Float {
    if (r < 0.1f) return 0f
    val t = (x / r).coerceIn(-1f, 1f)
    val dz = 0.14f
    return if (kotlin.math.abs(t) < dz) 0f else {
        val sign = if (t < 0f) -1f else 1f
        sign * ((kotlin.math.abs(t) - dz) / (1f - dz)).coerceIn(0f, 1f)
    }
}
