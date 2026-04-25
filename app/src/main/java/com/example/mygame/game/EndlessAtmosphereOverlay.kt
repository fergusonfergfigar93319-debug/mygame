package com.example.mygame.game

import androidx.compose.ui.geometry.Offset
import androidx.compose.ui.graphics.Brush
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.graphics.drawscope.DrawScope
import com.example.mygame.game.level.EndlessSegmentKind
import kotlin.math.sin

/**
 * 极夜漂流全屏氛围层，画在舞台实体之后、HUD 之前（由 [com.example.mygame.game.modes.EndlessMode] 中同一 [Canvas] 内调用顺序保证）。
 * 不阻塞 UI，仅作气象/环境可读性增强。
 */
fun DrawScope.drawEndlessSegmentAtmosphere(
    kind: EndlessSegmentKind?,
    blizzardIntensity: Float,
    globalAnim: Float,
    runElapsed: Float,
) {
    if (kind == null) return
    when (kind) {
        EndlessSegmentKind.BlizzardLowVis -> {
            if (blizzardIntensity < 0.02f) return
            drawBlizzardLowVisOverlay(blizzardIntensity, runElapsed)
        }
        EndlessSegmentKind.ThinIceGlide -> drawThinIceVignette(globalAnim)
        EndlessSegmentKind.RewardSafe -> drawRewardSafeWarmth(globalAnim)
        else -> Unit
    }
}

/** 风雪低能见：边雾径向 + 快速斜向雪线（替代原整块高不透明度白屏）。 */
private fun DrawScope.drawBlizzardLowVisOverlay(
    bzz: Float,
    runElapsed: Float,
) {
    val cx = size.width * 0.5f
    val cy = size.height * 0.35f
    val r0 = size.maxDimension * 0.88f
    drawRect(
        brush =
            Brush.radialGradient(
                0f to Color(0xFFFFFFFF).copy(alpha = 0.02f * bzz),
                0.48f to Color(0xFFFFFFFF).copy(alpha = 0.07f * bzz),
                1f to Color(0xFFFFFFFF).copy(alpha = 0.19f * bzz),
                center = Offset(cx, cy),
                radius = r0,
            ),
    )
    val t = runElapsed * 100f
    val n = 52
    for (i in 0 until n) {
        val baseY = (i * 197f) % size.height
        val phase = (i * 13.1f) % 97f
        val wobble = 1.05f + (i and 3) * 0.1f
        val x = (phase + t * wobble + i * 0.35f) % (size.width + 120f) - 60f
        val len = 11f + (i and 2) * 2.5f
        val x2 = x + len * 0.82f
        val y2 = baseY - len * 0.44f
        val a = (0.09f + 0.12f * bzz) * (0.5f + 0.5f * ((i * 5) and 1))
        drawLine(
            color = Color.White.copy(alpha = a.coerceIn(0.04f, 0.28f)),
            start = Offset(x, baseY),
            end = Offset(x2, y2),
            strokeWidth = 1.05f + bzz * 1.2f,
        )
    }
}

/** 薄冰：冷色边晕 + 极弱呼吸。 */
private fun DrawScope.drawThinIceVignette(globalAnim: Float) {
    val pulse = 0.1f + 0.02f * sin(globalAnim * 1.1f)
    drawRect(
        brush =
            Brush.radialGradient(
                0f to Color(0x00081A2E),
                0.62f to Color(0x00081A2E),
                1f to Color(0xFF81D4FA).copy(alpha = 0.1f * (0.85f + pulse * 0.3f)),
                center = Offset(size.width * 0.5f, size.height * 0.42f),
                radius = size.maxDimension * 0.78f,
            ),
    )
}

/** 补给段：极淡的暖意提示安全区。 */
private fun DrawScope.drawRewardSafeWarmth(globalAnim: Float) {
    val w = 0.06f + 0.01f * sin(globalAnim * 0.6f)
    drawRect(
        brush =
            Brush.radialGradient(
                0f to Color(0x00FFECB3),
                1f to Color(0x99FFECB3).copy(alpha = w),
                center = Offset(size.width * 0.5f, size.height * 0.55f),
                radius = size.maxDimension * 0.9f,
            ),
    )
}
