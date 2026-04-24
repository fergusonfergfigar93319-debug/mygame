package com.example.mygame.game

import android.graphics.Rect as AndRect
import androidx.compose.ui.graphics.ImageBitmap
import androidx.compose.ui.graphics.asAndroidBitmap
import androidx.compose.ui.graphics.drawscope.DrawScope
import androidx.compose.ui.graphics.nativeCanvas
import kotlin.math.sin

/**
 * Dror 跟宠：世界坐标每帧向主角身后目标点插值，绘制时保持宽高比并朝向主角。
 */
fun lerpDrorTowardTarget(
    drorX: Float,
    drorY: Float,
    playerX: Float,
    playerY: Float,
    facingRight: Boolean,
    hero: Float,
    frameSeconds: Float,
    velocityX: Float,
): Pair<Float, Float> {
    val behind = if (facingRight) -1f else 1f
    val targetX = playerX + behind * hero * 0.52f
    val targetY = playerY + hero * 0.07f
    val k = minOf(1f, (14f + kotlin.math.abs(velocityX) * 0.018f) * frameSeconds)
    val nx = drorX + (targetX - drorX) * k
    val ny = drorY + (targetY - drorY) * k
    return nx to ny
}

fun initialDrorPosition(
    playerX: Float,
    playerY: Float,
    facingRight: Boolean,
    hero: Float,
): Pair<Float, Float> {
    val behind = if (facingRight) -1f else 1f
    return (playerX + behind * hero * 0.52f) to (playerY + hero * 0.07f)
}


fun DrawScope.drawDrorCompanion(
    image: ImageBitmap,
    screenX: Float,
    screenY: Float,
    destWidth: Float,
    playerScreenX: Float,
    globalAnim: Float,
) {
    val bmp = image.asAndroidBitmap()
    if (bmp.width <= 0 || bmp.height <= 0) return
    val aspect = bmp.height.toFloat() / bmp.width.toFloat()
    val w = destWidth.toInt().coerceAtLeast(1)
    val h = (destWidth * aspect).toInt().coerceAtLeast(1)
    val bob = sin(globalAnim * 4.2f) * destWidth * 0.028f
    val drawY = screenY + bob
    val c = drawContext.canvas.nativeCanvas
    val srcRct = AndRect(0, 0, bmp.width, bmp.height)
    // 原画默认可视为朝右；Dror 在主角右侧时需水平翻转以朝向主角。
    val flipToFacePlayer = screenX > playerScreenX
    if (!flipToFacePlayer) {
        val l = screenX.toInt()
        val t = drawY.toInt()
        c.drawBitmap(bmp, srcRct, AndRect(l, t, l + w, t + h), null)
    } else {
        c.save()
        c.translate(screenX + destWidth, drawY)
        c.scale(-1f, 1f)
        c.drawBitmap(bmp, srcRct, AndRect(0, 0, w, h), null)
        c.restore()
    }
}
