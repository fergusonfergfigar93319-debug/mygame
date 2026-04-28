package com.example.mygame.haptic

import android.content.Context
import android.os.Build
import android.os.VibrationEffect
import android.os.Vibrator
import android.os.VibratorManager
import com.example.mygame.data.SaveRepository

/**
 * 基于 [SaveRepository.getHapticIntensity] 的全局震动力度缩放，替代分散的
 * [androidx.compose.ui.hapticfeedback.HapticFeedback] 以统一手感。
 */
class HapticManager(
    context: Context,
    private val saveRepository: SaveRepository,
) {
    private val app = context.applicationContext
    private val vibrator: Vibrator? =
        run {
            if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.S) {
                (app.getSystemService(Context.VIBRATOR_MANAGER_SERVICE) as? VibratorManager)
                    ?.defaultVibrator
            } else {
                @Suppress("DEPRECATION")
                app.getSystemService(Context.VIBRATOR_SERVICE) as? Vibrator
            }
        }

    private fun canVibrate(): Boolean = try {
        vibrator?.hasVibrator() == true
    } catch (_: Exception) {
        false
    }

    /** 落地 / 重踏等「重」反馈（原 [androidx.compose.ui.hapticfeedback.HapticFeedbackType.LongPress] 语义位）。 */
    fun performStomp() {
        vibrateOneShot(
            durationMs = 50L,
            baseAmplitude = 190,
        )
    }

    /** 连跳 / 连击等「轻」反馈（原 [androidx.compose.ui.hapticfeedback.HapticFeedbackType.TextHandleMove] 语义位）。 */
    fun performMomentum() {
        vibrateOneShot(
            durationMs = 32L,
            baseAmplitude = 115,
        )
    }

    /**
     * 使用当前 [SaveRepository] 强度，适合大厅滑条在 [onValueChangeFinished] 中试听。
     */
    fun previewCurrentIntensity() {
        previewWithIntensity(saveRepository.getHapticIntensity())
    }

    /**
     * 使用指定强度播放短震（0 关闭），不再次读取偏好；用于刚写入前的试听。
     */
    fun previewWithIntensity(intensity: Float) {
        val v = vibrator
        if (intensity <= 0f || v == null || !canVibrate()) return
        val a = (220f * intensity).toInt().coerceIn(1, 255)
        if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.O) {
            v.vibrate(VibrationEffect.createOneShot(40, a))
        } else {
            @Suppress("DEPRECATION")
            v.vibrate(40)
        }
    }

    private fun vibrateOneShot(
        durationMs: Long,
        baseAmplitude: Int,
    ) {
        val v = vibrator
        val intensity = saveRepository.getHapticIntensity()
        if (intensity <= 0f || v == null || !canVibrate()) return
        val amplitude = (baseAmplitude * intensity).toInt().coerceIn(1, 255)
        if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.O) {
            v.vibrate(VibrationEffect.createOneShot(durationMs, amplitude))
        } else {
            if (intensity < 0.15f) return
            @Suppress("DEPRECATION")
            val dur = (durationMs * (0.5f + 0.5f * intensity)).toLong().coerceAtLeast(16L)
            v.vibrate(dur)
        }
    }
}
