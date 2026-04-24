package com.example.mygame.ui.leaderboard

import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.lazy.LazyColumn
import androidx.compose.foundation.lazy.itemsIndexed
import androidx.compose.material3.Button
import androidx.compose.material3.FilterChip
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.OutlinedTextField
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.setValue
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.dp
import com.example.mygame.data.LeaderboardRepository
import com.example.mygame.data.LeaderboardSort
import com.example.mygame.data.SaveRepository
import com.example.mygame.data.model.LeaderboardEntry
import com.example.mygame.game.modes.EndlessDailyChallenge
import com.example.mygame.ui.common.GameBackRow
import com.example.mygame.ui.common.GameHeroCard
import com.example.mygame.ui.common.GameScreenBackground
import com.example.mygame.ui.common.GameSectionCard
import java.text.SimpleDateFormat
import java.util.Date
import java.util.Locale

private enum class LeaderboardListScope {
    All,
    DailyToday,
}

@Composable
fun LeaderboardScreen(
    leaderboardRepository: LeaderboardRepository,
    saveRepository: SaveRepository,
    onBack: () -> Unit,
    modifier: Modifier = Modifier,
) {
    var sort by remember { mutableStateOf(LeaderboardSort.ByTotalScore) }
    var listScope by remember { mutableStateOf(LeaderboardListScope.All) }
    var nickname by remember { mutableStateOf(saveRepository.getPlayerNickname()) }

    val todayBucket = remember { EndlessDailyChallenge.todayBucketLocal() }
    val entries = remember(sort, listScope, leaderboardRepository, todayBucket) {
        leaderboardRepository.getTopEntries(
            20,
            sort,
            if (listScope == LeaderboardListScope.DailyToday) todayBucket else null,
        )
    }
    val (best, recent, avg) = remember(listScope, leaderboardRepository, todayBucket) {
        when (listScope) {
            LeaderboardListScope.All ->
                Triple(
                    leaderboardRepository.getBestEntry(),
                    leaderboardRepository.getMostRecentEntry(),
                    leaderboardRepository.getAverageTotalScore(),
                )

            LeaderboardListScope.DailyToday -> {
                val inBucket = leaderboardRepository.getTopEntries(200, LeaderboardSort.ByTotalScore, todayBucket)
                Triple(
                    inBucket.firstOrNull(),
                    inBucket.maxByOrNull { it.timestampMillis },
                    if (inBucket.isEmpty()) 0.0 else inBucket.sumOf { it.totalScore.toDouble() } / inBucket.size,
                )
            }
        }
    }

    GameScreenBackground(modifier = modifier.fillMaxSize()) {
        GameBackRow(title = "积分榜", onBack = onBack)

        GameHeroCard(
            title = "排行榜",
            subtitle = if (listScope == LeaderboardListScope.DailyToday) "今天的固定种子挑战成绩都在这里。" else "查看总榜、路程榜和历史最佳成绩。",
            accentStart = androidx.compose.ui.graphics.Color(0xFF21405F),
            accentEnd = androidx.compose.ui.graphics.Color(0xFF6A7EA8),
        )

        GameSectionCard {
            Text(
                text = "昵称设置",
                style = MaterialTheme.typography.titleMedium,
                fontWeight = FontWeight.Bold,
            )
            OutlinedTextField(
                value = nickname,
                onValueChange = { nickname = it },
                label = { Text("排行榜昵称") },
                singleLine = true,
                modifier = Modifier.fillMaxWidth(),
            )
            Button(onClick = { saveRepository.setPlayerNickname(nickname) }, modifier = Modifier.fillMaxWidth()) {
                Text("保存昵称")
            }
        }

        GameSectionCard {
            Text("榜单范围", fontWeight = FontWeight.Bold)
            Row(horizontalArrangement = Arrangement.spacedBy(8.dp)) {
                FilterChip(
                    selected = listScope == LeaderboardListScope.All,
                    onClick = { listScope = LeaderboardListScope.All },
                    label = { Text("全部") },
                )
                FilterChip(
                    selected = listScope == LeaderboardListScope.DailyToday,
                    onClick = { listScope = LeaderboardListScope.DailyToday },
                    label = { Text("今日挑战") },
                )
            }
            Text("排序方式", fontWeight = FontWeight.Bold)
            Row(horizontalArrangement = Arrangement.spacedBy(8.dp)) {
                FilterChip(selected = sort == LeaderboardSort.ByTotalScore, onClick = { sort = LeaderboardSort.ByTotalScore }, label = { Text("总分") })
                FilterChip(selected = sort == LeaderboardSort.ByDistance, onClick = { sort = LeaderboardSort.ByDistance }, label = { Text("路程") })
                FilterChip(selected = sort == LeaderboardSort.BySurvivalTime, onClick = { sort = LeaderboardSort.BySurvivalTime }, label = { Text("存活") })
            }
            Text(
                text = if (listScope == LeaderboardListScope.DailyToday) "今日挑战日期：$todayBucket" else "当前显示：本地历史记录",
                color = androidx.compose.ui.graphics.Color(0xFF536675),
            )
        }

        GameSectionCard {
            Text("统计摘要", fontWeight = FontWeight.Bold)
            Text("本地最佳：${best?.totalScore ?: "—"}")
            Text("最近一局：${recent?.totalScore ?: "—"}")
            Text("平均总分：${"%.1f".format(avg)}")
        }

        GameSectionCard(modifier = Modifier.weight(1f)) {
            Text(
                text = "Top 20",
                style = MaterialTheme.typography.titleMedium,
                fontWeight = FontWeight.Bold,
            )
            LazyColumn(
                verticalArrangement = Arrangement.spacedBy(8.dp),
                modifier = Modifier.fillMaxWidth(),
            ) {
                itemsIndexed(entries) { index, entry ->
                    LeaderboardRow(
                        rank = index + 1,
                        entry = entry,
                        showChallengeTag = listScope == LeaderboardListScope.All,
                    )
                }
            }
        }
    }
}

@Composable
private fun LeaderboardRow(
    rank: Int,
    entry: LeaderboardEntry,
    showChallengeTag: Boolean,
) {
    val df = remember { SimpleDateFormat("MM-dd HH:mm", Locale.getDefault()) }
    GameSectionCard {
        Row(
            modifier = Modifier.fillMaxWidth(),
            horizontalArrangement = Arrangement.SpaceBetween,
            verticalAlignment = Alignment.CenterVertically,
        ) {
            Text("#$rank ${entry.nickname}", fontWeight = FontWeight.Bold)
            Text("${entry.totalScore} 分", fontWeight = FontWeight.Bold)
        }
        Text(
            text = "路程 ${"%.0f".format(entry.distanceScoreUnits)} · 存活 ${entry.survivalSeconds.toInt()}s · 鱼干 ${entry.fishSnacks} · 信标 ${entry.beaconCount}",
            color = androidx.compose.ui.graphics.Color(0xFF536675),
        )
        if (showChallengeTag && entry.challengeBucket != null) {
            Text("挑战日：${entry.challengeBucket}", color = androidx.compose.ui.graphics.Color(0xFF1F6A8A))
        }
        if (entry.playerId.isNotBlank()) {
            Text("匿名 id：${entry.playerId.take(8)}…", color = androidx.compose.ui.graphics.Color(0xFF607D8B))
        }
        Text(df.format(Date(entry.timestampMillis)), color = androidx.compose.ui.graphics.Color(0xFF90A4AE))
    }
}
