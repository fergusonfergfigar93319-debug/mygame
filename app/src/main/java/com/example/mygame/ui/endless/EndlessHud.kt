package com.example.mygame.ui.endless

import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.material3.Card
import androidx.compose.material3.LinearProgressIndicator
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.draw.clip
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.dp
import com.example.mygame.game.level.EndlessSegmentKind

@Composable
fun EndlessHud(
    totalScore: Int,
    distanceUnits: Float,
    survivalSeconds: Float,
    multiplier: Float,
    fishSnacks: Int,
    beacons: Int,
    lorePages: Int,
    distanceScore: Int,
    collectionScore: Int,
    actionScore: Int,
    fishDashActive: Boolean,
    hasScarf: Boolean,
    assistReady: Boolean,
    assistTimer: Float,
    currentSegmentKind: EndlessSegmentKind?,
    modifier: Modifier = Modifier,
) {
    Card(
        modifier = modifier.fillMaxWidth(),
        shape = RoundedCornerShape(22.dp),
    ) {
        Column(
            modifier = Modifier.padding(horizontal = 14.dp, vertical = 12.dp),
            verticalArrangement = Arrangement.spacedBy(8.dp),
        ) {
            Text(
                text = "极夜漂流",
                style = MaterialTheme.typography.titleMedium,
                fontWeight = FontWeight.Bold,
                color = Color(0xFF1A237E),
            )
            Row(
                modifier = Modifier.fillMaxWidth(),
                horizontalArrangement = Arrangement.SpaceBetween,
            ) {
                MiniStat("总分", totalScore.toString())
                MiniStat("路程", String.format("%.0f", distanceUnits))
                MiniStat("存活", "${survivalSeconds.toInt()}s")
                MiniStat("倍率", String.format("%.2f×", multiplier))
            }
            LinearProgressIndicator(
                progress = { (survivalSeconds % 20f / 20f).coerceIn(0f, 1f) },
                modifier = Modifier
                    .fillMaxWidth()
                    .height(4.dp)
                    .clip(RoundedCornerShape(2.dp)),
                color = Color(0xFF3949AB),
                trackColor = Color(0xFFE8EAF6),
            )
            Row(horizontalArrangement = Arrangement.spacedBy(10.dp)) {
                MiniStat("鱼干", "$fishSnacks")
                MiniStat("信标", "$beacons")
                MiniStat("残页", "$lorePages")
            }
            Text(
                text = "分项 距:$distanceScore 集:$collectionScore 技:$actionScore",
                style = MaterialTheme.typography.labelSmall,
                color = Color(0xFF5C6BC0),
            )
            currentSegmentKind?.let { k ->
                Text(
                    text = "航道：${segmentLabel(k)}",
                    style = MaterialTheme.typography.labelMedium,
                    color = Color(0xFF455A64),
                )
            }
            Text(
                text = buildString {
                    if (fishDashActive) append("鱼干冲刺 ")
                    if (hasScarf) append("泡泡围巾 ")
                    if (assistTimer > 0f) append("团团掩护 ")
                    else if (assistReady) append("团团就绪 ")
                }.ifBlank { "保持节奏，注意冰隙与风雪段" },
                style = MaterialTheme.typography.bodySmall,
                color = Color(0xFF546E7A),
            )
        }
    }
}

@Composable
private fun MiniStat(label: String, value: String) {
    Column(horizontalAlignment = Alignment.CenterHorizontally) {
        Text(label, style = MaterialTheme.typography.labelSmall, color = Color(0xFF607D8B))
        Text(value, style = MaterialTheme.typography.titleSmall, fontWeight = FontWeight.Bold)
    }
}

private fun segmentLabel(k: EndlessSegmentKind): String = when (k) {
    EndlessSegmentKind.FlatChase -> "平地节奏"
    EndlessSegmentKind.PitJump -> "裂谷跳跃"
    EndlessSegmentKind.ThinIceGlide -> "薄冰滑行"
    EndlessSegmentKind.BlizzardLowVis -> "风雪低能见"
    EndlessSegmentKind.RewardSafe -> "安全补给"
    EndlessSegmentKind.DangerMixed -> "混合危险"
    EndlessSegmentKind.BranchChoice -> "双选险路"
}
