package com.example.mygame.ui.home

import androidx.compose.foundation.Image
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.size
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.material3.Button
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.OutlinedButton
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.draw.clip
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.layout.ContentScale
import androidx.compose.ui.res.painterResource
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.dp
import com.example.mygame.R
import com.example.mygame.data.SaveRepository
import com.example.mygame.ui.common.GameHeroCard
import com.example.mygame.ui.common.GameInfoPill
import com.example.mygame.ui.common.GameScreenBackground
import com.example.mygame.ui.common.GameSectionCard

private data class ModeCardInfo(
    val title: String,
    val subtitle: String,
    val description: String,
    val actionLabel: String,
    val onClick: () -> Unit,
)

@Composable
fun HomeMenu(
    saveRepository: SaveRepository,
    onStory: () -> Unit,
    onEndless: () -> Unit,
    onEndlessDaily: () -> Unit,
    onLeaderboard: () -> Unit,
    onCodex: () -> Unit,
) {
    val drorUnlocked = saveRepository.getCompanionDrorUnlocked()
    val rescuedTuanTuan = saveRepository.getRescuedTuanTuan()
    val cards =
        listOf(
            ModeCardInfo(
                title = "主线旅程",
                subtitle = "推进章节、找回伙伴、追击高松鹅",
                description = "适合沉浸体验剧情、关卡与伙伴能力的成长线。",
                actionLabel = "进入主线",
                onClick = onStory,
            ),
            ModeCardInfo(
                title = "极夜漂流",
                subtitle = "自由无尽挑战",
                description = "随机航道更适合刷分和练操作，节奏最纯粹。",
                actionLabel = "开始漂流",
                onClick = onEndless,
            ),
            ModeCardInfo(
                title = "今日无尽",
                subtitle = "固定种子的公平挑战",
                description = "每天一张相同地图，适合和朋友比较路线与细节。",
                actionLabel = "挑战今日",
                onClick = onEndlessDaily,
            ),
            ModeCardInfo(
                title = "积分榜",
                subtitle = "查看本地战绩与成长",
                description = "快速查看总榜、路程榜与今日挑战成绩。",
                actionLabel = "打开榜单",
                onClick = onLeaderboard,
            ),
        )

    GameScreenBackground {
        GameHeroCard(
            title = "咕咕嘎嘎",
            subtitle = "雪原上的家园已被毁去，但旅程才刚刚开始。找回伙伴，穿过冰湖、雾堤和极夜航道，一步步逼近高松鹅。",
        )

        Row(horizontalArrangement = Arrangement.spacedBy(8.dp)) {
            GameInfoPill(label = "团团 ${if (rescuedTuanTuan) "已归队" else "待救回"}", active = rescuedTuanTuan)
            GameInfoPill(label = "Dror ${if (drorUnlocked) "已同行" else "未解锁"}", active = drorUnlocked)
            GameInfoPill(label = "最高分 ${saveRepository.getBestScore()}")
        }

        GameSectionCard {
            Text(
                text = "图鉴入口",
                style = MaterialTheme.typography.titleMedium,
                fontWeight = FontWeight.Bold,
                color = Color(0xFF183247),
            )
            Text(
                text = "统一查看角色立绘、伙伴状态和当前故事线索。",
                style = MaterialTheme.typography.bodyMedium,
                color = Color(0xFF536675),
            )
            Row(
                modifier = Modifier.fillMaxWidth(),
                horizontalArrangement = Arrangement.spacedBy(14.dp),
            ) {
                CodexPortrait(
                    title = "高松鹅",
                    subtitle = "Boss",
                    resId = R.drawable.portrait_takamatsu_goose,
                    contentScale = ContentScale.Crop,
                    modifier = Modifier.weight(1f),
                )
                CodexPortrait(
                    title = "Dror",
                    subtitle = if (drorUnlocked) "已同行" else "伙伴线索",
                    resId = R.drawable.companion_dror,
                    contentScale = ContentScale.Fit,
                    modifier = Modifier.weight(1f),
                )
            }
            OutlinedButton(onClick = onCodex, modifier = Modifier.fillMaxWidth()) {
                Text("打开正式图鉴")
            }
        }

        cards.forEach { card ->
            GameSectionCard {
                Text(
                    text = card.title,
                    style = MaterialTheme.typography.titleLarge,
                    fontWeight = FontWeight.ExtraBold,
                    color = Color(0xFF183247),
                )
                Text(
                    text = card.subtitle,
                    style = MaterialTheme.typography.titleSmall,
                    color = Color(0xFF3A617E),
                )
                Text(
                    text = card.description,
                    style = MaterialTheme.typography.bodyMedium,
                    color = Color(0xFF536675),
                )
                Button(onClick = card.onClick, modifier = Modifier.fillMaxWidth()) {
                    Text(card.actionLabel)
                }
            }
        }
    }
}

@Composable
private fun CodexPortrait(
    title: String,
    subtitle: String,
    resId: Int,
    contentScale: ContentScale,
    modifier: Modifier = Modifier,
) {
    Column(
        modifier = modifier,
        verticalArrangement = Arrangement.spacedBy(10.dp),
        horizontalAlignment = Alignment.CenterHorizontally,
    ) {
        Image(
            painter = painterResource(resId),
            contentDescription = title,
            modifier =
                Modifier
                    .size(width = 92.dp, height = 116.dp)
                    .clip(RoundedCornerShape(18.dp)),
            contentScale = contentScale,
        )
        Text(
            text = title,
            style = MaterialTheme.typography.titleSmall,
            fontWeight = FontWeight.Bold,
        )
        Text(
            text = subtitle,
            style = MaterialTheme.typography.labelMedium,
            color = Color(0xFF607D8B),
        )
    }
}
