package com.example.mygame.ui.runner3d

import androidx.compose.animation.core.*
import androidx.compose.foundation.background
import androidx.compose.foundation.layout.*
import androidx.compose.foundation.shape.CircleShape
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.material3.Text
import androidx.compose.runtime.*
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.draw.alpha
import androidx.compose.ui.draw.clip
import androidx.compose.ui.geometry.Offset
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.input.pointer.pointerInput
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.text.style.TextAlign
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import kotlin.math.abs

private enum class TutorialStep(
    val title: String,
    val desc: String,
    val hint: String,
    val arrow: String,
) {
    SwipeUp("跳跃", "向上滑动屏幕，让企鹅跳过障碍物", "试试向上滑动", "↑"),
    SwipeDown("滑铲", "向下滑动屏幕，让企鹅低身穿过低矮障碍", "试试向下滑动", "↓"),
    SwipeLeft("向左换道", "向左滑动屏幕，切换到左侧跑道", "试试向左滑动", "←"),
    SwipeRight("向右换道", "向右滑动屏幕，切换到右侧跑道", "试试向右滑动", "→"),
}

private enum class SwipeDir { Up, Down, Left, Right }

private fun detectSwipe(start: Offset, end: Offset, threshold: Float = 60f): SwipeDir? {
    val dx = end.x - start.x
    val dy = end.y - start.y
    return when {
        abs(dx) > abs(dy) && abs(dx) > threshold -> if (dx > 0) SwipeDir.Right else SwipeDir.Left
        abs(dy) > threshold -> if (dy > 0) SwipeDir.Down else SwipeDir.Up
        else -> null
    }
}

private fun TutorialStep.matches(dir: SwipeDir) = when (this) {
    TutorialStep.SwipeUp -> dir == SwipeDir.Up
    TutorialStep.SwipeDown -> dir == SwipeDir.Down
    TutorialStep.SwipeLeft -> dir == SwipeDir.Left
    TutorialStep.SwipeRight -> dir == SwipeDir.Right
}

@Composable
fun TutorialOverlay(onComplete: () -> Unit) {
    var stepIndex by remember { mutableIntStateOf(0) }
    var stepDone by remember { mutableStateOf(false) }
    val steps = TutorialStep.entries

    if (stepIndex >= steps.size) {
        LaunchedEffect(Unit) { onComplete() }
        return
    }

    val step = steps[stepIndex]

    val pulse = rememberInfiniteTransition(label = "pulse")
    val arrowOffset by pulse.animateFloat(
        initialValue = 0f, targetValue = 20f,
        animationSpec = infiniteRepeatable(tween(650, easing = FastOutSlowInEasing), RepeatMode.Reverse),
        label = "ao",
    )
    val arrowAlpha by pulse.animateFloat(
        initialValue = 0.45f, targetValue = 1f,
        animationSpec = infiniteRepeatable(tween(650), RepeatMode.Reverse),
        label = "aa",
    )

    var dragStart by remember { mutableStateOf(Offset.Zero) }

    Box(
        modifier = Modifier
            .fillMaxSize()
            .background(Color.Black.copy(alpha = 0.75f))
            .pointerInput(step) {
                awaitPointerEventScope {
                    while (true) {
                        val down = awaitPointerEvent()
                        val press = down.changes.firstOrNull() ?: continue
                        if (press.pressed) dragStart = press.position
                        val up = awaitPointerEvent()
                        val release = up.changes.firstOrNull() ?: continue
                        if (!release.pressed) {
                            val dir = detectSwipe(dragStart, release.position)
                            if (dir != null && step.matches(dir) && !stepDone) {
                                stepDone = true
                            }
                        }
                    }
                }
            },
        contentAlignment = Alignment.Center,
    ) {
        Column(
            horizontalAlignment = Alignment.CenterHorizontally,
            verticalArrangement = Arrangement.spacedBy(14.dp),
            modifier = Modifier.padding(horizontal = 32.dp),
        ) {
            // progress dots
            Row(horizontalArrangement = Arrangement.spacedBy(8.dp)) {
                steps.forEachIndexed { i, _ ->
                    Box(
                        modifier = Modifier
                            .size(if (i == stepIndex) 10.dp else 7.dp)
                            .clip(CircleShape)
                            .background(
                                when {
                                    i < stepIndex -> Color(0xFF66BB6A)
                                    i == stepIndex -> Color.White
                                    else -> Color.White.copy(alpha = 0.25f)
                                },
                            ),
                    )
                }
            }

            Spacer(Modifier.height(4.dp))

            // animated arrow
            val arrowMod = when (step) {
                TutorialStep.SwipeUp -> Modifier.offset(y = (-arrowOffset).dp)
                TutorialStep.SwipeDown -> Modifier.offset(y = arrowOffset.dp)
                TutorialStep.SwipeLeft -> Modifier.offset(x = (-arrowOffset).dp)
                TutorialStep.SwipeRight -> Modifier.offset(x = arrowOffset.dp)
            }
            Text(
                text = step.arrow,
                fontSize = 80.sp,
                color = Color.White,
                modifier = arrowMod.alpha(arrowAlpha),
            )

            Spacer(Modifier.height(4.dp))

            Text(step.title, color = Color.White, fontSize = 26.sp, fontWeight = FontWeight.Bold)

            Text(
                text = step.desc,
                color = Color.White.copy(alpha = 0.82f),
                fontSize = 15.sp,
                textAlign = TextAlign.Center,
                lineHeight = 22.sp,
            )

            Spacer(Modifier.height(8.dp))

            if (!stepDone) {
                Text(
                    text = step.hint,
                    color = Color(0xFF90CAF9),
                    fontSize = 14.sp,
                    modifier = Modifier
                        .background(Color.White.copy(alpha = 0.1f), RoundedCornerShape(20.dp))
                        .padding(horizontal = 20.dp, vertical = 8.dp),
                )
            } else {
                LaunchedEffect(stepIndex) {
                    kotlinx.coroutines.delay(550)
                    stepDone = false
                    stepIndex++
                }
                Text(
                    text = "✓  做到了！",
                    color = Color(0xFF81C784),
                    fontSize = 17.sp,
                    fontWeight = FontWeight.Bold,
                    modifier = Modifier
                        .background(Color(0xFF1B5E20).copy(alpha = 0.55f), RoundedCornerShape(20.dp))
                        .padding(horizontal = 20.dp, vertical = 8.dp),
                )
            }

            Spacer(Modifier.height(20.dp))

            // skip
            Box(
                modifier = Modifier
                    .clip(RoundedCornerShape(12.dp))
                    .pointerInput(Unit) {
                        awaitPointerEventScope {
                            while (true) {
                                val e = awaitPointerEvent()
                                if (e.changes.any { !it.pressed }) onComplete()
                            }
                        }
                    }
                    .padding(horizontal = 16.dp, vertical = 6.dp),
            ) {
                Text("跳过引导", color = Color.White.copy(alpha = 0.35f), fontSize = 13.sp)
            }
        }
    }
}
