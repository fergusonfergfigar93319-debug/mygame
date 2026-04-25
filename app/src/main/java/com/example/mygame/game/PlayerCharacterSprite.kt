package com.example.mygame.game

import android.graphics.Paint as AndroidPaint
import android.graphics.Rect as AndRect
import androidx.compose.ui.graphics.ImageBitmap
import androidx.compose.ui.graphics.asAndroidBitmap
import androidx.compose.ui.graphics.drawscope.DrawScope
import androidx.compose.ui.graphics.nativeCanvas
import androidx.compose.ui.unit.IntOffset
import androidx.compose.ui.unit.IntSize
import kotlin.math.abs
import kotlin.math.max
import kotlin.math.sin

/**
 * 精灵表布局。单张整图时使用 [GuguSpriteLayout.singleImage]；
 * 日后若使用多帧表，只调此处与资源即可。
 */
data class GuguSpriteLayout(
    val frameWidth: Int,
    val frameHeight: Int,
    val idleFrames: Int = 1,
    val runFrames: Int = 1,
    val idleRow: Int = 0,
    val runRow: Int = 0,
) {
    companion object {
        fun singleImage(bitmap: ImageBitmap) = GuguSpriteLayout(
            frameWidth = bitmap.width,
            frameHeight = bitmap.height,
        )
    }
}

/**
 * 在 [DrawScope] 中绘制咕咕嘎嘎。单图模式下用 [globalAnim] 与 [animTick] 做轻量待机动效与跑动节奏；
 * 多帧表就绪后，[animTick] 用于行/列循环。绘制走系统 [android.graphics.Canvas.drawBitmap]，与当前 Compose 版本解耦。
 */
fun DrawScope.drawGuguCharacterSprite(
    image: ImageBitmap,
    layout: GuguSpriteLayout,
    screenX: Float,
    screenY: Float,
    destSize: Float,
    globalAnim: Float,
    animTick: Int,
    facingRight: Boolean,
    isMoving: Boolean,
    /** 脚底为锚的竖直缩放，用于呼吸感或终结残影。 */
    breathScaleY: Float = 1f,
    overallAlpha: Float = 1f,
) {
    if (layout.frameWidth <= 0 || layout.frameHeight <= 0) return
    val fw = layout.frameWidth
    val fh = layout.frameHeight
    val col = if (isMoving) {
        animTick % max(1, layout.runFrames)
    } else {
        (animTick / 2) % max(1, layout.idleFrames)
    }
    val row = if (isMoving) layout.runRow else layout.idleRow
    val srcOffset = IntOffset(
        (col * fw).coerceAtMost(max(0, image.width - fw)),
        (row * fh).coerceAtMost(max(0, image.height - fh)),
    )
    val srcSize = IntSize(
        fw.coerceAtMost(image.width - srcOffset.x),
        fh.coerceAtMost(image.height - srcOffset.y),
    )
    val w = destSize.toInt().coerceAtLeast(1)
    val bobY = if (isMoving) {
        sin(globalAnim * 5f) * destSize * 0.02f
    } else {
        sin(globalAnim * 2.2f) * destSize * 0.04f
    }
    val drawY = screenY + bobY

    val bmp = image.asAndroidBitmap()
    val c = drawContext.canvas.nativeCanvas
    val paint =
        if (overallAlpha >= 0.999f) {
            null
        } else {
            AndroidPaint().apply {
                isAntiAlias = true
                alpha = (overallAlpha * 255f).toInt().coerceIn(0, 255)
            }
        }
    val srcL = srcOffset.x
    val srcT = srcOffset.y
    val srcR = (srcL + srcSize.width).coerceAtMost(bmp.width)
    val srcB = (srcT + srcSize.height).coerceAtMost(bmp.height)
    if (srcR <= srcL || srcB <= srcT) return
    val srcRct = AndRect(srcL, srcT, srcR, srcB)

    val scaleBody = breathScaleY != 1f
    if (scaleBody) {
        c.save()
        val px = screenX + destSize * 0.5f
        val py = drawY + destSize
        c.translate(px, py)
        c.scale(1f, breathScaleY)
        c.translate(-px, -py)
    }
    if (facingRight) {
        val l = screenX.toInt()
        val t = drawY.toInt()
        c.drawBitmap(bmp, srcRct, AndRect(l, t, l + w, t + w), paint)
    } else {
        c.save()
        c.translate(screenX, drawY)
        c.scale(-1f, 1f, destSize * 0.5f, destSize * 0.5f)
        c.drawBitmap(bmp, srcRct, AndRect(0, 0, w, w), paint)
        c.restore()
    }
    if (scaleBody) {
        c.restore()
    }
}

/**
 * 是否视为跑动。左右键按下或水平速度超阈值时视为移动。
 */
fun guguIsMovingHorizontally(velocityX: Float, left: Boolean, right: Boolean, threshold: Float = 40f): Boolean =
    left || right || abs(velocityX) > threshold
