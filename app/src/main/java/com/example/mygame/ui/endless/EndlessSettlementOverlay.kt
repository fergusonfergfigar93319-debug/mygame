package com.example.mygame.ui.endless

import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.rememberScrollState
import androidx.compose.foundation.verticalScroll
import androidx.compose.material3.Button
import androidx.compose.material3.HorizontalDivider
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.Text
import androidx.compose.material3.TextButton
import androidx.compose.runtime.Composable
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.setValue
import androidx.compose.ui.Modifier
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.dp
import com.example.mygame.data.LeaderboardSubmitResult
import com.example.mygame.game.modes.EndlessBalanceConfig
import com.example.mygame.game.modes.EndlessDailyChallenge
import com.example.mygame.game.score.EndlessRunScoreBreakdown
import com.example.mygame.ui.common.GameOverlayPanel

@Composable
fun EndlessSettlementOverlay(
    breakdown: EndlessRunScoreBreakdown,
    submitResult: LeaderboardSubmitResult?,
    distanceUnits: Float,
    survivalSeconds: Float,
    playerIdShort: String,
    challengeBucket: String?,
    dailyAttemptCount: Int?,
    previousDailyBestScore: Int?,
    onRestart: () -> Unit,
    modifier: Modifier = Modifier,
) {
    var showDetail by remember { mutableStateOf(false) }
    val improvedDailyBest =
        challengeBucket != null && previousDailyBestScore != null && breakdown.finalTotal > previousDailyBestScore
    val dailyDelta =
        previousDailyBestScore
            ?.takeIf { challengeBucket != null && it > 0 && breakdown.finalTotal <= it }
            ?.let { it - breakdown.finalTotal }

    GameOverlayPanel(
        title = "极夜漂流结算",
        description = "这一次漂流结束了，来看看你把这条航道跑到了哪里。",
        modifier = modifier,
        footnote = "可以查看明细后再来一局",
    ) {
        Text(
            text = "本局总分 ${breakdown.finalTotal}",
            style = MaterialTheme.typography.titleLarge,
            fontWeight = FontWeight.Bold,
            color = Color(0xFF234C7B),
        )
        Text(
            text = "路程 ${"%.0f".format(distanceUnits)} · 存活 ${survivalSeconds.toInt()}s",
            color = Color(0xFF536675),
        )
        submitResult?.let { sr ->
            Text(
                text =
                    buildString {
                        append("总榜第 ${sr.rankByScore} 名")
                        if (sr.madeTop20) append(" · 已进入本地 Top 20")
                        sr.rankInChallengeBucket?.let { append(" · 今日榜第 $it 名") }
                    },
                color = Color(0xFF1F6A8A),
            )
        }
        if (!challengeBucket.isNullOrBlank()) {
            Text(
                text = "挑战日：$challengeBucket · 种子版本 ${EndlessDailyChallenge.SEED_SALT_V1}",
                color = Color(0xFF607D8B),
            )
            dailyAttemptCount?.let {
                Text(
                    text = "今日第 $it 次尝试",
                    color = Color(0xFF607D8B),
                )
            }
            when {
                improvedDailyBest -> Text("刷新今日最佳！", color = Color(0xFF1B8A5A), fontWeight = FontWeight.Bold)
                dailyDelta != null -> Text("距离今日最佳还差 $dailyDelta 分", color = Color(0xFF607D8B))
                previousDailyBestScore == 0 -> Text("这是今天的第一条成绩记录。", color = Color(0xFF607D8B))
            }
        }
        Text("匿名 id：$playerIdShort", color = Color(0xFF607D8B))

        TextButton(onClick = { showDetail = !showDetail }) {
            Text(if (showDetail) "收起得分明细" else "展开得分明细")
        }

        if (showDetail) {
            HorizontalDivider()
            Column(
                modifier = Modifier.fillMaxWidth().verticalScroll(rememberScrollState()),
                verticalArrangement = Arrangement.spacedBy(8.dp),
            ) {
                DetailLine("距离分", breakdown.distanceScore)
                DetailLine("收集分", breakdown.collectionScore)
                DetailLine("动作分", breakdown.actionScore)
                DetailLine("倍率前小计", breakdown.baseSubtotal, true)
                Text(
                    text =
                        "倍率规则：每 ${EndlessBalanceConfig.scoring.multiplierStepEverySeconds.toInt()} 秒提升 ${EndlessBalanceConfig.scoring.multiplierStep}，上限 x${EndlessBalanceConfig.scoring.multiplierCap.toInt()}",
                    color = Color(0xFF607D8B),
                )
                Text(
                    text = "倍率档位 ${breakdown.multiplierTier} · 本档已坚持 ${"%.1f".format(breakdown.secondsIntoTier)}s",
                    color = Color(0xFF607D8B),
                )
                DetailLine("倍率额外加分", breakdown.bonusFromMultiplier)
                DetailLine("最终总分", breakdown.finalTotal, true)
            }
        }

        Button(onClick = onRestart, modifier = Modifier.fillMaxWidth()) {
            Text("再来一局")
        }
    }
}

@Composable
private fun DetailLine(
    label: String,
    value: Int,
    bold: Boolean = false,
) {
    Row(
        modifier = Modifier.fillMaxWidth(),
        horizontalArrangement = Arrangement.SpaceBetween,
    ) {
        Text(
            text = label,
            style = if (bold) MaterialTheme.typography.titleSmall else MaterialTheme.typography.bodyMedium,
            fontWeight = if (bold) FontWeight.Bold else FontWeight.Normal,
        )
        Text(
            text = value.toString(),
            style = if (bold) MaterialTheme.typography.titleSmall else MaterialTheme.typography.bodyMedium,
            fontWeight = if (bold) FontWeight.Bold else FontWeight.Normal,
        )
    }
}
