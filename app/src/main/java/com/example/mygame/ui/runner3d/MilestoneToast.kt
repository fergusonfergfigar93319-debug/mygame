package com.example.mygame.ui.runner3d

import androidx.compose.animation.*
import androidx.compose.animation.core.tween
import androidx.compose.foundation.background
import androidx.compose.foundation.layout.*
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.material3.Text
import androidx.compose.runtime.*
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import kotlinx.coroutines.delay

private val speedPhaseNames = listOf("热身", "加速", "发力", "冲刺", "高速", "极速", "极限")
private val speedPhaseColors = listOf(
    Color(0xFF4FC3F7), Color(0xFF81C784), Color(0xFFFFD54F),
    Color(0xFFFF8A65), Color(0xFFEF5350), Color(0xFFCE93D8), Color(0xFFFFFFFF),
)

@Composable
fun MilestoneToast(meters: Int, onDismiss: () -> Unit) {
    var visible by remember { mutableStateOf(true) }
    LaunchedEffect(meters) {
        delay(1800)
        visible = false
        delay(300)
        onDismiss()
    }
    AnimatedVisibility(
        visible = visible,
        enter = slideInVertically(tween(250)) { it } + fadeIn(tween(250)),
        exit = slideOutVertically(tween(220)) { it } + fadeOut(tween(220)),
    ) {
        Box(modifier = Modifier.fillMaxWidth(), contentAlignment = Alignment.BottomCenter) {
            Text(
                text = "🏁 ${meters}m",
                color = Color.White,
                fontSize = 20.sp,
                fontWeight = FontWeight.Bold,
                modifier = Modifier
                    .padding(bottom = 80.dp)
                    .background(Color(0xFF1565C0).copy(alpha = 0.88f), RoundedCornerShape(20.dp))
                    .padding(horizontal = 24.dp, vertical = 10.dp),
            )
        }
    }
}

@Composable
fun NearMissToast(onDismiss: () -> Unit) {
    var visible by remember { mutableStateOf(true) }
    LaunchedEffect(Unit) {
        delay(900)
        visible = false
        delay(200)
        onDismiss()
    }
    AnimatedVisibility(
        visible = visible,
        enter = scaleIn(tween(150)) + fadeIn(tween(150)),
        exit = scaleOut(tween(180)) + fadeOut(tween(180)),
    ) {
        Box(modifier = Modifier.fillMaxSize(), contentAlignment = Alignment.Center) {
            Text(
                text = "险！+50",
                color = Color(0xFFFFEB3B),
                fontSize = 26.sp,
                fontWeight = FontWeight.Bold,
                modifier = Modifier
                    .background(Color.Black.copy(alpha = 0.5f), RoundedCornerShape(12.dp))
                    .padding(horizontal = 20.dp, vertical = 8.dp),
            )
        }
    }
}

@Composable
fun SpeedUpToast(phase: Int, onDismiss: () -> Unit) {
    var visible by remember { mutableStateOf(true) }
    val color = speedPhaseColors.getOrElse(phase) { Color.White }
    val label = speedPhaseNames.getOrElse(phase) { "极限" }

    LaunchedEffect(phase) {
        delay(1600)
        visible = false
        delay(280)
        onDismiss()
    }

    AnimatedVisibility(
        visible = visible,
        enter = scaleIn(tween(200)) + fadeIn(tween(200)),
        exit = scaleOut(tween(200)) + fadeOut(tween(200)),
    ) {
        Box(modifier = Modifier.fillMaxSize(), contentAlignment = Alignment.Center) {
            Column(
                horizontalAlignment = Alignment.CenterHorizontally,
                modifier = Modifier
                    .background(Color.Black.copy(alpha = 0.55f), RoundedCornerShape(16.dp))
                    .padding(horizontal = 28.dp, vertical = 14.dp),
            ) {
                Text(
                    text = "⚡ 速度提升",
                    color = Color.White.copy(alpha = 0.8f),
                    fontSize = 14.sp,
                )
                Text(
                    text = label,
                    color = color,
                    fontSize = 32.sp,
                    fontWeight = FontWeight.Bold,
                )
            }
        }
    }
}
