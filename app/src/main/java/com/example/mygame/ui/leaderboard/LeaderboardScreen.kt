package com.example.mygame.ui.leaderboard

import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.lazy.LazyColumn
import androidx.compose.foundation.lazy.itemsIndexed
import androidx.compose.material3.Button
import androidx.compose.material3.Card
import androidx.compose.material3.FilterChip
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.OutlinedTextField
import androidx.compose.material3.Text
import androidx.compose.material3.TextButton
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
import java.text.SimpleDateFormat
import java.util.Date
import java.util.Locale

@Composable
fun LeaderboardScreen(
    leaderboardRepository: LeaderboardRepository,
    saveRepository: SaveRepository,
    onBack: () -> Unit,
    modifier: Modifier = Modifier,
) {
    var sort by remember { mutableStateOf(LeaderboardSort.ByTotalScore) }
    var nickname by remember { mutableStateOf(saveRepository.getPlayerNickname()) }

    val entries = remember(sort, leaderboardRepository) {
        leaderboardRepository.getTopEntries(20, sort)
    }
    val best = remember(leaderboardRepository) { leaderboardRepository.getBestEntry() }
    val recent = remember(leaderboardRepository) { leaderboardRepository.getMostRecentEntry() }
    val avg = remember(leaderboardRepository) { leaderboardRepository.getAverageTotalScore() }

    Column(
        modifier = modifier
            .fillMaxSize()
            .padding(20.dp),
        verticalArrangement = Arrangement.spacedBy(12.dp),
    ) {
        Row(
            modifier = Modifier.fillMaxWidth(),
            horizontalArrangement = Arrangement.SpaceBetween,
            verticalAlignment = Alignment.CenterVertically,
        ) {
            TextButton(onClick = onBack) { Text("返回") }
            Text(
                "积分榜（本地 Top 20）",
                style = MaterialTheme.typography.titleLarge,
                fontWeight = FontWeight.Bold,
            )
        }
        OutlinedTextField(
            value = nickname,
            onValueChange = { nickname = it },
            label = { Text("昵称（用于新纪录）") },
            singleLine = true,
            modifier = Modifier.fillMaxWidth(),
        )
        Button(
            onClick = { saveRepository.setPlayerNickname(nickname) },
            modifier = Modifier.fillMaxWidth(),
        ) {
            Text("保存昵称")
        }
        Row(horizontalArrangement = Arrangement.spacedBy(8.dp)) {
            FilterChip(
                selected = sort == LeaderboardSort.ByTotalScore,
                onClick = { sort = LeaderboardSort.ByTotalScore },
                label = { Text("总分") },
            )
            FilterChip(
                selected = sort == LeaderboardSort.ByDistance,
                onClick = { sort = LeaderboardSort.ByDistance },
                label = { Text("路程") },
            )
            FilterChip(
                selected = sort == LeaderboardSort.BySurvivalTime,
                onClick = { sort = LeaderboardSort.BySurvivalTime },
                label = { Text("存活") },
            )
        }
        Card(modifier = Modifier.fillMaxWidth()) {
            Column(Modifier.padding(12.dp), verticalArrangement = Arrangement.spacedBy(4.dp)) {
                Text("统计", style = MaterialTheme.typography.titleSmall, fontWeight = FontWeight.Bold)
                Text("本地最佳：${best?.totalScore ?: "—"}  最近一局：${recent?.totalScore ?: "—"}")
                Text("历史平均总分：${"%.1f".format(avg)}")
            }
        }
        LazyColumn(
            verticalArrangement = Arrangement.spacedBy(8.dp),
            modifier = Modifier.weight(1f),
        ) {
            itemsIndexed(entries) { index, e ->
                LeaderboardRow(rank = index + 1, entry = e)
            }
        }
    }
}

@Composable
private fun LeaderboardRow(rank: Int, entry: LeaderboardEntry) {
    val df = remember { SimpleDateFormat("MM-dd HH:mm", Locale.getDefault()) }
    Card(modifier = Modifier.fillMaxWidth()) {
        Column(Modifier.padding(12.dp)) {
            Row(
                Modifier.fillMaxWidth(),
                horizontalArrangement = Arrangement.SpaceBetween,
            ) {
                Text("#$rank ${entry.nickname}", fontWeight = FontWeight.Bold)
                Text("${entry.totalScore} 分", fontWeight = FontWeight.Bold)
            }
            Text(
                "路程 ${"%.0f".format(entry.distanceScoreUnits)} · 存活 ${entry.survivalSeconds.toInt()}s · 鱼干 ${entry.fishSnacks} · 信标 ${entry.beaconCount}",
                style = MaterialTheme.typography.bodySmall,
            )
            if (entry.playerId.isNotBlank()) {
                Text(
                    "id · ${entry.playerId.take(8)}…",
                    style = MaterialTheme.typography.labelSmall,
                    color = MaterialTheme.colorScheme.onSurfaceVariant,
                )
            }
            Text(
                df.format(Date(entry.timestampMillis)),
                style = MaterialTheme.typography.labelSmall,
                color = MaterialTheme.colorScheme.onSurfaceVariant,
            )
        }
    }
}
