package com.example.mygame.game

import androidx.compose.ui.geometry.Offset
import androidx.compose.ui.geometry.Size
import androidx.compose.ui.graphics.Brush
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.graphics.Path
import androidx.compose.ui.graphics.drawscope.DrawScope
import kotlin.math.sin

data class StageDecorPalette(
    val glowColor: Color,
    val mistColor: Color,
    val shardColor: Color,
)

fun DrawScope.drawStageForegroundDecor(
    groundY: Float,
    globalAnim: Float,
    palette: StageDecorPalette,
) {
    val shimmer = 0.12f + ((sin(globalAnim * 1.6f) + 1f) * 0.5f) * 0.08f

    drawRect(
        brush =
            Brush.verticalGradient(
                listOf(
                    Color.Transparent,
                    palette.mistColor.copy(alpha = 0.06f),
                    palette.mistColor.copy(alpha = 0.14f),
                ),
                startY = groundY - size.height * 0.16f,
                endY = size.height,
            ),
        topLeft = Offset(0f, groundY - size.height * 0.16f),
        size = Size(size.width, size.height - groundY + size.height * 0.16f),
    )

    fun shardPath(baseX: Float, width: Float, height: Float, lean: Float): Path =
        Path().apply {
            moveTo(baseX, size.height)
            lineTo(baseX + width * 0.36f, size.height - height * 0.42f)
            lineTo(baseX + width * (0.58f + lean), size.height - height)
            lineTo(baseX + width, size.height)
            close()
        }

    val leftShard = shardPath(size.width * 0.02f, size.width * 0.16f, size.height * 0.22f, -0.06f)
    val midShard = shardPath(size.width * 0.76f, size.width * 0.11f, size.height * 0.18f, 0.03f)
    val rightShard = shardPath(size.width * 0.88f, size.width * 0.1f, size.height * 0.28f, -0.04f)

    listOf(leftShard, midShard, rightShard).forEachIndexed { index, path ->
        val alphaScale = 0.1f + index * 0.05f
        drawPath(path, palette.shardColor.copy(alpha = alphaScale + shimmer * 0.5f))
        drawPath(
            path = path,
            brush =
                Brush.verticalGradient(
                    listOf(
                        palette.glowColor.copy(alpha = 0.08f + shimmer * 0.5f),
                        Color.Transparent,
                    ),
                ),
        )
    }
}
