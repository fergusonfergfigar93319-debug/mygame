package com.example.mygame.ui.endless

import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.rememberScrollState
import androidx.compose.foundation.verticalScroll
import androidx.compose.material3.Button
import androidx.compose.material3.Card
import androidx.compose.material3.HorizontalDivider
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.Text
import androidx.compose.material3.TextButton
import androidx.compose.runtime.Composable
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.setValue
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.dp
import com.example.mygame.data.LeaderboardSubmitResult
import com.example.mygame.game.modes.EndlessBalanceConfig
import com.example.mygame.game.score.EndlessRunScoreBreakdown

@Composable
fun EndlessSettlementOverlay(
    breakdown: EndlessRunScoreBreakdown,
    submitResult: LeaderboardSubmitResult?,
    distanceUnits: Float,
    survivalSeconds: Float,
    playerIdShort: String,
    onRestart: () -> Unit,
    modifier: Modifier = Modifier,
) {
    var showDetail by remember { mutableStateOf(false) }

    Column(
        modifier = modifier
            .fillMaxSize()
            .padding(12.dp),
        verticalArrangement = Arrangement.Center,
        horizontalAlignment = Alignment.CenterHorizontally,
    ) {
        Card(modifier = Modifier.fillMaxWidth()) {
            Column(
                modifier = Modifier.padding(16.dp),
                verticalArrangement = Arrangement.spacedBy(8.dp),
            ) {
                Text(
                    "漂流结束",
                    style = MaterialTheme.typography.headlineSmall,
                    fontWeight = FontWeight.Bold,
                )
                Text(
                    "本局总分 ${breakdown.finalTotal}",
                    style = MaterialTheme.typography.titleMedium,
                    fontWeight = FontWeight.Bold,
                    color = Color(0xFF1A237E),
                )
                Text(
                    "路程 ${"%.0f".format(distanceUnits)} · 存活 ${survivalSeconds.toInt()}s",
                    style = MaterialTheme.typography.bodyMedium,
                )
                submitResult?.let { sr ->
                    Text(
                        "积分榜（总分）第 ${sr.rankByScore} 名" +
                            if (sr.madeTop20) " · 已进本地 Top 20" else "",
                        style = MaterialTheme.typography.bodySmall,
                        color = MaterialTheme.colorScheme.primary,
                    )
                }
                Text(
                    "玩家 id（匿名）· $playerIdShort",
                    style = MaterialTheme.typography.labelSmall,
                    color = MaterialTheme.colorScheme.onSurfaceVariant,
                )
                TextButton(onClick = { showDetail = !showDetail }) {
                    Text(if (showDetail) "收起明细" else "得分明细")
                }
                if (showDetail) {
                    HorizontalDivider()
                    Column(
                        modifier = Modifier
                            .fillMaxWidth()
                            .verticalScroll(rememberScrollState()),
                        verticalArrangement = Arrangement.spacedBy(6.dp),
                    ) {
                        DetailLine("距离分", breakdown.distanceScore)
                        DetailLine("收集分", breakdown.collectionScore)
                        DetailLine("动作分", breakdown.actionScore)
                        DetailLine("小计（倍率前）", breakdown.baseSubtotal, bold = true)
                        Text(
                            buildString {
                                val sc = EndlessBalanceConfig.scoring
                                append("存活 ${"%.1f".format(breakdown.survivalSeconds)}s → 倍率 ×")
                                append("${"%.2f".format(breakdown.survivalMultiplier)}")
                                append("（每 ${sc.multiplierStepEverySeconds.toInt()}s +${sc.multiplierStep}，上限 ×${sc.multiplierCap.toInt()}）")
                            },
                            style = MaterialTheme.typography.labelSmall,
                            color = MaterialTheme.colorScheme.onSurfaceVariant,
                        )
                        DetailLine("倍率带来的加分", breakdown.bonusFromMultiplier)
                        DetailLine("最终总分", breakdown.finalTotal, bold = true)
                    }
                }
                HorizontalDivider()
                Button(
                    onClick = onRestart,
                    modifier = Modifier.fillMaxWidth(),
                ) {
                    Text("再来一局")
                }
            }
        }
    }
}

@Composable
private fun DetailLine(label: String, value: Int, bold: Boolean = false) {
    RowSpaceBetween(
        label,
        value.toString(),
        bold,
    )
}

@Composable
private fun RowSpaceBetween(left: String, right: String, bold: Boolean) {
    Row(
        modifier = Modifier.fillMaxWidth(),
        horizontalArrangement = Arrangement.SpaceBetween,
    ) {
        Text(
            left,
            style = if (bold) MaterialTheme.typography.titleSmall else MaterialTheme.typography.bodyMedium,
            fontWeight = if (bold) FontWeight.Bold else FontWeight.Normal,
        )
        Text(
            right,
            style = if (bold) MaterialTheme.typography.titleSmall else MaterialTheme.typography.bodyMedium,
            fontWeight = if (bold) FontWeight.Bold else FontWeight.Normal,
        )
    }
}
