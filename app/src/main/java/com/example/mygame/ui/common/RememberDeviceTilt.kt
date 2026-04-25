package com.example.mygame.ui.common

import android.content.Context
import android.hardware.Sensor
import android.hardware.SensorEvent
import android.hardware.SensorEventListener
import android.hardware.SensorManager
import androidx.compose.animation.core.Spring
import androidx.compose.animation.core.animateFloatAsState
import androidx.compose.animation.core.spring
import androidx.compose.runtime.Composable
import androidx.compose.runtime.DisposableEffect
import androidx.compose.runtime.State
import androidx.compose.runtime.derivedStateOf
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableFloatStateOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.setValue
import androidx.compose.ui.geometry.Offset
import androidx.compose.ui.platform.LocalContext

/**
 * 基于 [Sensor.TYPE_GRAVITY] 的屏幕倾斜，输出平滑后的像素偏移，供主线背景视差使用。
 * 无传感器或模拟器上保持 [Offset.Zero]；**不得** 用于玩法碰撞与相机逻辑。
 */
@Composable
fun rememberDeviceTilt(
    maxOffsetPx: Float = 60f,
    invertX: Boolean = true,
    invertY: Boolean = true,
): State<Offset> {
    val context = LocalContext.current
    val sensorManager = remember(context) { context.getSystemService(Context.SENSOR_SERVICE) as SensorManager }
    var targetX by remember { mutableFloatStateOf(0f) }
    var targetY by remember { mutableFloatStateOf(0f) }

    DisposableEffect(sensorManager) {
        val gravity = sensorManager.getDefaultSensor(Sensor.TYPE_GRAVITY)
        val listener =
            object : SensorEventListener {
                override fun onSensorChanged(event: SensorEvent) {
                    val rawX = (event.values[0] / 9.81f).coerceIn(-1f, 1f)
                    val rawY = (event.values[1] / 9.81f).coerceIn(-1f, 1f)
                    val scaledX = rawX * maxOffsetPx
                    val scaledY = rawY * maxOffsetPx
                    targetX = if (invertX) -scaledX else scaledX
                    targetY = if (invertY) -scaledY else scaledY
                }

                override fun onAccuracyChanged(sensor: Sensor?, accuracy: Int) = Unit
            }
        if (gravity != null) {
            sensorManager.registerListener(listener, gravity, SensorManager.SENSOR_DELAY_GAME)
        }
        onDispose {
            sensorManager.unregisterListener(listener)
        }
    }

    val smoothX by animateFloatAsState(
        targetValue = targetX,
        animationSpec =
            spring(
                dampingRatio = Spring.DampingRatioNoBouncy,
                stiffness = Spring.StiffnessLow,
            ),
        label = "deviceTiltX",
    )
    val smoothY by animateFloatAsState(
        targetValue = targetY,
        animationSpec =
            spring(
                dampingRatio = Spring.DampingRatioNoBouncy,
                stiffness = Spring.StiffnessLow,
            ),
        label = "deviceTiltY",
    )
    return remember { derivedStateOf { Offset(smoothX, smoothY) } }
}
