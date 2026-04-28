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
import com.example.mygame.engine.PowerUpType
import kotlinx.coroutines.delay

private data class PowerUpInfo(
    val emoji: String,
    val name: String,
    val desc: String,
    val color: Color,
)

private val powerUpInfo = mapOf(
    PowerUpType.FishDash to PowerUpInfo(
        emoji = "🐟",
        name = "鱼干冲刺",
        desc = "无敌冲刺！3 秒内穿透所有障碍物",
        color = Color(0xFFFF8F00),
    ),
    PowerUpType.BubbleScarf to PowerUpInfo(
        emoji = "🫧",
        name = "泡泡围巾",
        desc = "滞空滑翔！跳跃时下落更慢",
        color = Color(0xFF29B6F6),
    ),
    PowerUpType.SnowShield to PowerUpInfo(
        emoji = "🛡️",
        name = "雪盾",
        desc = "免死护盾！抵挡一次碰撞伤害",
        color = Color(0xFF66BB6A),
    ),
    PowerUpType.GustBoots to PowerUpInfo(
        emoji = "👢",
        name = "风靴",
        desc = "弹跳加强！跳跃高度大幅提升",
        color = Color(0xFFAB47BC),
    ),
    PowerUpType.AuroraMagnet to PowerUpInfo(
        emoji = "🧲",
        name = "极光磁铁",
        desc = "自动吸附！附近金币自动飞来",
        color = Color(0xFFFFCA28),
    ),
)

@Composable
fun PowerUpToast(
    type: PowerUpType,
    onDismiss: () -> Unit,
) {
    val info = powerUpInfo[type] ?: return
    var visible by remember { mutableStateOf(true) }

    LaunchedEffect(type) {
        delay(2200)
        visible = false
        delay(300)
        onDismiss()
    }

    AnimatedVisibility(
        visible = visible,
        enter = slideInVertically(tween(280)) { -it / 2 } + fadeIn(tween(280)),
        exit = slideOutVertically(tween(250)) { -it / 2 } + fadeOut(tween(250)),
    ) {
        Box(
            modifier = Modifier.fillMaxWidth(),
            contentAlignment = Alignment.TopCenter,
        ) {
            Row(
                modifier = Modifier
                    .padding(top = 100.dp, start = 32.dp, end = 32.dp)
                    .background(
                        color = info.color.copy(alpha = 0.92f),
                        shape = RoundedCornerShape(16.dp),
                    )
                    .padding(horizontal = 20.dp, vertical = 14.dp),
                verticalAlignment = Alignment.CenterVertically,
                horizontalArrangement = Arrangement.spacedBy(14.dp),
            ) {
                Text(info.emoji, fontSize = 36.sp)
                Column {
                    Text(
                        text = info.name,
                        color = Color.White,
                        fontSize = 17.sp,
                        fontWeight = FontWeight.Bold,
                    )
                    Text(
                        text = info.desc,
                        color = Color.White.copy(alpha = 0.9f),
                        fontSize = 13.sp,
                        lineHeight = 18.sp,
                    )
                }
            }
        }
    }
}
