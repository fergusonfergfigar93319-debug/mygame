package com.example.mygame.ui.endless

import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.shape.RoundedCornerShape
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
import com.example.mygame.game.StatBlock
import com.example.mygame.game.level.EndlessSegmentKind
import com.example.mygame.ui.common.GameHudShell
import com.example.mygame.ui.common.GameInfoPill

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
    GameHudShell(
        title = "极夜漂流",
        modifier = modifier,
        titleColor = Color(0xFF1A237E),
    ) {
        Row(
            modifier = Modifier.fillMaxWidth(),
            horizontalArrangement = Arrangement.SpaceBetween,
            verticalAlignment = Alignment.CenterVertically,
        ) {
            StatBlock("总分", totalScore.toString())
            StatBlock("路程", String.format("%.0f", distanceUnits))
            StatBlock("存活", "${survivalSeconds.toInt()}s")
            StatBlock("倍率", String.format("%.2fx", multiplier))
        }
        LinearProgressIndicator(
            progress = { (survivalSeconds % 20f / 20f).coerceIn(0f, 1f) },
            modifier =
                Modifier
                    .fillMaxWidth()
                    .height(4.dp)
                    .clip(RoundedCornerShape(2.dp)),
            color = Color(0xFF3949AB),
            trackColor = Color(0xFFE8EAF6),
        )
        Row(horizontalArrangement = Arrangement.spacedBy(8.dp)) {
            GameInfoPill("鱼干 $fishSnacks")
            GameInfoPill("信标 $beacons")
            GameInfoPill("残页 $lorePages")
        }
        Text(
            text = "分项：距离 $distanceScore · 收集 $collectionScore · 动作 $actionScore",
            style = MaterialTheme.typography.labelMedium,
            color = Color(0xFF5C6BC0),
        )
        currentSegmentKind?.let { kind ->
            Text(
                text = "当前航道：${segmentLabel(kind)}",
                style = MaterialTheme.typography.bodyMedium,
                color = Color(0xFF546E7A),
            )
        }
        Text(
            text =
                buildString {
                    if (fishDashActive) append("鱼干冲刺 ")
                    if (hasScarf) append("泡泡围巾 ")
                    if (assistTimer > 0f) append("团团掩护 ")
                    else if (assistReady) append("团团就绪 ")
                }.ifBlank { "保持节奏，注意裂谷、薄冰和风雪段的变化。" },
            style = MaterialTheme.typography.bodySmall,
            color = Color(0xFF546E7A),
            fontWeight = FontWeight.Medium,
        )
    }
}

private fun segmentLabel(kind: EndlessSegmentKind): String =
    when (kind) {
        EndlessSegmentKind.FlatChase -> "平地追逐"
        EndlessSegmentKind.PitJump -> "裂谷跳跃"
        EndlessSegmentKind.ThinIceGlide -> "薄冰滑行"
        EndlessSegmentKind.BlizzardLowVis -> "风雪低能见"
        EndlessSegmentKind.RewardSafe -> "补给整备"
        EndlessSegmentKind.DangerMixed -> "混合危险"
        EndlessSegmentKind.BranchChoice -> "双线险路"
    }
