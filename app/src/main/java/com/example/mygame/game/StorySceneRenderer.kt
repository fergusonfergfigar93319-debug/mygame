package com.example.mygame.game

import androidx.compose.ui.geometry.CornerRadius
import androidx.compose.ui.geometry.Offset
import androidx.compose.ui.geometry.Size
import androidx.compose.ui.graphics.Brush
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.graphics.Path
import androidx.compose.ui.graphics.drawscope.DrawScope
import androidx.compose.ui.graphics.drawscope.translate
import com.example.mygame.game.level.StorySceneTheme
import kotlin.math.sin

/**
 * 无尽极夜：独立绘制层次（与主线 [drawStorySceneBackdrop] 同 API 风格，便于视差与美术迭代）。
 */
fun DrawScope.drawEndlessNightBackdrop(
    cameraX: Float,
    globalAnim: Float,
    groundY: Float,
    blizzardIntensity: Float,
) {
    drawRect(brush = Brush.verticalGradient(listOf(Color(0xFF0B1430), Color(0xFF1A2B4A), Color(0xFF243B5A))))

    // 极光帶：慢漂移 + 视差
    val aurT = globalAnim * 0.4f
    for (b in 0..2) {
        val baseX = b * size.width * 0.4f
        val ax = (baseX - cameraX * StoryParallax.AURORA) % (size.width * 1.2f) - size.width * 0.1f
        val ay = size.height * (0.08f + b * 0.06f) + sin(aurT + b) * 12f
        val path =
            Path().apply {
                moveTo(ax, ay)
                quadraticTo(
                    ax + size.width * 0.4f,
                    ay + 40f,
                    ax + size.width * 0.9f,
                    ay + 8f,
                )
                quadraticTo(
                    ax + size.width * 0.5f,
                    ay - 20f,
                    ax,
                    ay,
                )
                close()
            }
        drawPath(
            path,
            Color(0xFF4DD0E1).copy(alpha = 0.08f + blizzardIntensity * 0.04f),
        )
    }

    // 大月亮：慢视差
    val moonX = size.width * 0.82f - cameraX * 0.05f
    val moonY = size.height * 0.1f
    drawCircle(
        brush =
            Brush.radialGradient(
                colors = listOf(Color(0xFFE8EAF6).copy(alpha = 0.45f), Color.Transparent),
                center = Offset(moonX, moonY),
                radius = size.width * 0.2f,
            ),
        radius = size.width * 0.04f,
        center = Offset(moonX, moonY),
    )

    // 星光：极慢视差
    repeat(48) { i ->
        val seed = i * 4423f
        val sx = (seed * 0.0017f % 1f) * size.width
        val sy = (seed * 0.0023f % 1f) * size.height * 0.55f
        val tw = 0.35f + 0.45f * sin(aurT * 2.1f + i * 0.7f)
        val px = (sx - cameraX * StoryParallax.ENDLESS_STAR) % (size.width + 20f) - 10f
        drawCircle(Color.White.copy(alpha = 0.12f * tw + blizzardIntensity * 0.08f), 1f + (i % 2), Offset(px, sy))
    }

    // 中远景山脊
    repeat(5) { index ->
        val ridgeX = scrollTileX(index * size.width * 0.5f, cameraX, 0.18f, size.width * 1.8f)
        drawOval(
            color = Color(0xFF1E2F4A).copy(alpha = 0.55f),
            topLeft = Offset(ridgeX - size.width * 0.02f, groundY - 180f - index * 8f),
            size = Size(size.width * 0.65f, 120f - index * 6f),
        )
    }
}

fun DrawScope.drawStorySceneBackdrop(
    theme: StorySceneTheme,
    cameraX: Float,
    globalAnim: Float,
    groundY: Float,
    /** 设备重力视差：远景（天穹、日轮）。 */
    tiltParallaxFar: Offset = Offset.Zero,
    /** 中景：云、山脊、雪丘、废屋、林线、地平线雾。 */
    tiltParallaxMid: Offset = Offset.Zero,
    /** 近景：飘雪粒子（Gameplay 与碰撞层不应用视差）。 */
    tiltParallaxFore: Offset = Offset.Zero,
) {
    val pad = StoryParallax.TILT_SKY_OVERSCAN_PX
    // --- 远景：防穿帮扩绘天幕 ---
    translate(tiltParallaxFar.x, tiltParallaxFar.y) {
        drawRect(
            brush = Brush.verticalGradient(theme.skyGradientColors),
            topLeft = Offset(-pad, -pad),
            size = Size(size.width + 2f * pad, size.height + 2f * pad),
        )
        val sunCenterX = size.width * 0.88f - cameraX * StoryParallax.SUN
        val sunCenterY = size.height * 0.1f
        drawCircle(
            brush =
                Brush.radialGradient(
                    colors = listOf(theme.sunCoreColor.copy(alpha = 0.95f), theme.sunHaloEdgeColor),
                    center = Offset(sunCenterX, sunCenterY),
                    radius = size.width * 0.22f,
                ),
            radius = size.width * 0.06f,
            center = Offset(sunCenterX, sunCenterY),
        )
    }

    translate(tiltParallaxMid.x, tiltParallaxMid.y) {
        repeat(theme.cloudCount) { index ->
            val cloudX =
                scrollTileX(index * size.width * 0.35f, cameraX, StoryParallax.CLOUD, size.width * 1.4f)
            drawCircle(Color.White.copy(alpha = 0.92f), size.width * 0.06f, Offset(cloudX + 70f, size.height * 0.16f))
            drawCircle(Color.White.copy(alpha = 0.92f), size.width * 0.05f, Offset(cloudX + 120f, size.height * 0.14f))
            drawCircle(Color.White.copy(alpha = 0.92f), size.width * 0.05f, Offset(cloudX + 165f, size.height * 0.16f))
        }

        repeat(theme.ridgeCount) { index ->
            val ridgeX = scrollTileX(index * size.width * 0.48f, cameraX, StoryParallax.RIDGE_FAR, size.width * 2f)
            val ridgeAlpha = 0.35f + (index % 3) * 0.08f
            drawOval(
                color = theme.skyGradientColors.last().copy(alpha = ridgeAlpha),
                topLeft = Offset(ridgeX - size.width * 0.05f, groundY - 100f),
                size = Size(size.width * 0.55f, 95f),
            )
        }

        repeat(6) { index ->
            val midX = scrollTileX(index * size.width * 0.4f, cameraX, StoryParallax.RIDGE_MID, size.width * 1.9f)
            val midA = 0.18f + (index % 2) * 0.04f
            drawOval(
                color = theme.hillStoneColor.copy(alpha = midA),
                topLeft = Offset(midX - size.width * 0.04f, groundY - 88f - (index % 3) * 6f),
                size = Size(size.width * 0.5f, 78f - (index % 3) * 4f),
            )
        }

        repeat(theme.hillCount) { index ->
            val hillX = scrollTileX(index * size.width * 0.45f, cameraX, StoryParallax.HILL, size.width * 2.2f)
            drawCircle(theme.hillSnowColor, size.width * 0.18f, Offset(hillX + 80f, groundY + 30f))
            drawRect(theme.hillStoneColor, Offset(hillX + 36f, groundY - 28f), Size(34f, 58f))
        }

        repeat(theme.hutCount) { index ->
            val hutX = scrollTileX(index * size.width * 0.62f, cameraX, StoryParallax.HUT, size.width * 2.6f)
            drawRoundRect(
                color = theme.hutWallColor,
                topLeft = Offset(hutX + 40f, groundY - 72f),
                size = Size(74f, 54f),
                cornerRadius = CornerRadius(10f, 10f),
            )
            val roofPath =
                Path().apply {
                    moveTo(hutX + 30f, groundY - 68f)
                    lineTo(hutX + 78f, groundY - 106f)
                    lineTo(hutX + 124f, groundY - 68f)
                    close()
                }
            drawPath(roofPath, theme.hutRoofColor)
            drawLine(theme.hutBeamColor, Offset(hutX + 34f, groundY - 18f), Offset(hutX + 18f, groundY + 16f), 8f)
        }

        repeat(8) { index ->
            val fx = scrollTileX(index * size.width * 0.5f, cameraX, StoryParallax.FOREST, size.width * 2.2f) + 20f
            val fh = 26f + (index % 3) * 8f
            drawRoundRect(
                color = theme.birdWingColor.copy(alpha = 0.38f + (index % 2) * 0.08f),
                topLeft = Offset(fx, groundY - 18f - fh),
                size = Size(10f, fh),
                cornerRadius = CornerRadius(3f, 3f),
            )
        }

        // 地平线轻雾
        drawRect(
            topLeft = Offset(-pad, groundY - 160f),
            size = Size(size.width + 2f * pad, 120f + pad),
            brush =
                Brush.verticalGradient(
                    listOf(
                        Color.Transparent,
                        theme.foregroundMistColor.copy(alpha = 0.12f),
                        theme.foregroundMistColor.copy(alpha = 0.28f),
                    ),
                ),
        )
    }

    translate(tiltParallaxFore.x, tiltParallaxFore.y) {
        repeat(26) { i ->
            val seed = i * 7919f
            val sx = (seed * 0.0007f % 1f) * size.width
            val sy = (seed * 0.0011f % 1f) * size.height * 0.75f
            val drift = globalAnim * (12f + (i % 4) * 3f) + i * 17f
            val px =
                (sx + sin(drift * 0.08f) * 18f + drift * 6f - cameraX * StoryParallax.SNOWFLAKE) %
                    (size.width + 24f) - 12f
            val py = sy + drift * 5f % (size.height * 0.8f)
            val alpha = theme.snowflakeAlphaBase + (i % 4) * theme.snowflakeAlphaStep
            drawCircle(Color.White.copy(alpha = alpha), 1.6f + (i % 3), Offset(px, py))
        }
    }
}
