package com.example.mygame.ui.home

import androidx.compose.foundation.Image
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.size
import androidx.compose.foundation.rememberScrollState
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.foundation.verticalScroll
import androidx.compose.material3.MaterialTheme
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
import com.example.mygame.game.level.GameLevel
import com.example.mygame.ui.common.GameBackRow
import com.example.mygame.ui.common.GameHeroCard
import com.example.mygame.ui.common.GameInfoPill
import com.example.mygame.ui.common.GameScreenBackground
import com.example.mygame.ui.common.GameSectionCard

@Composable
fun CodexScreen(
    saveRepository: SaveRepository,
    onBack: () -> Unit,
) {
    val drorUnlocked = saveRepository.getCompanionDrorUnlocked()
    val rescuedTuanTuan = saveRepository.getRescuedTuanTuan()
    val resumeLevel = saveRepository.getResumeLevel()
    val playerNickname = saveRepository.getPlayerNickname()
    val bestScore = saveRepository.getBestScore()

    GameScreenBackground(modifier = Modifier.verticalScroll(rememberScrollState())) {
        GameBackRow(title = "图鉴", onBack = onBack)

        GameHeroCard(
            title = "角色与旅程图鉴",
            subtitle = "这里记录着这段旅程中的主要角色、伙伴状态、章节事件与当前主线进度。",
        )

        GameSectionCard {
            Text(
                text = "当前故事阶段",
                style = MaterialTheme.typography.titleMedium,
                fontWeight = FontWeight.Bold,
            )
            Text(
                text = storySummaryForLevel(resumeLevel, rescuedTuanTuan, drorUnlocked),
                style = MaterialTheme.typography.bodyMedium,
                color = Color(0xFF536675),
            )
            Row(horizontalArrangement = Arrangement.spacedBy(8.dp)) {
                GameInfoPill("当前章节 ${levelLabel(resumeLevel)}")
                GameInfoPill("团团 ${if (rescuedTuanTuan) "已归队" else "未归队"}", rescuedTuanTuan)
                GameInfoPill("Dror ${if (drorUnlocked) "已同行" else "未同行"}", drorUnlocked)
            }
        }

        CharacterCard(
            title = "咕咕嘎嘎",
            subtitle = "主角",
            description = "家园被毁后，咕咕嘎嘎踏上了向北的旅程。她不是最强的战士，却总能把伙伴重新聚到一起。",
            status = "当前章节：${levelLabel(resumeLevel)}",
            resId = R.drawable.gugu_sprite,
            contentScale = ContentScale.Crop,
        )

        CharacterCard(
            title = "高松鹅",
            subtitle = "Boss",
            description = "毁掉家园的元凶。他一路向北撤退，留下被打碎的村庄、失散的伙伴和越来越危险的雪原。",
            status = "主线目标：继续追踪他的足迹",
            resId = R.drawable.portrait_takamatsu_goose,
            contentScale = ContentScale.Crop,
        )

        CharacterCard(
            title = "团团",
            subtitle = "支援伙伴",
            description = "团团擅长用雪团打乱敌人的节奏，是旅程里第一个真正回到队伍中的伙伴。",
            status = if (rescuedTuanTuan) "状态：已归队，可发动支援" else "状态：仍待救回",
            resId = R.drawable.companion_dror,
            contentScale = ContentScale.Fit,
        )

        CharacterCard(
            title = "Dror",
            subtitle = "同行伙伴",
            description = "Dror 会一路跟在主角身边，为旅程带来陪伴感，也让队伍不再显得孤单。",
            status = if (drorUnlocked) "状态：已同行" else "状态：暂未加入",
            resId = R.drawable.companion_dror,
            contentScale = ContentScale.Fit,
        )

        GameSectionCard {
            Text(
                text = "章节事件记录",
                style = MaterialTheme.typography.titleMedium,
                fontWeight = FontWeight.Bold,
            )
            ChapterEventItem(
                chapter = "第一章：雪松村废墟",
                status = when (resumeLevel) {
                    GameLevel.CedarVillageRuins -> "当前章节"
                    else -> "已穿越"
                },
                description =
                    if (rescuedTuanTuan) {
                        "家园被毁后的第一段旅程已经完成，团团也成功回到队伍。"
                    } else {
                        "咕咕嘎嘎从家园废墟出发，开始寻找第一个失散伙伴。"
                    },
            )
            ChapterEventItem(
                chapter = "第二章：冰湖回音谷",
                status = when (resumeLevel) {
                    GameLevel.CedarVillageRuins -> "即将抵达"
                    GameLevel.IceLakeEchoValley -> "当前章节"
                    GameLevel.MistDike -> "已穿越"
                },
                description = "回音谷的高台、裂隙和冰湖把危险放大，也把高松鹅留下的线索送回队伍耳边。",
            )
            ChapterEventItem(
                chapter = "第三章：北境雾堤",
                status = if (resumeLevel == GameLevel.MistDike) "当前章节" else "待深入",
                description = "雾堤是当前主线的前沿地带，也是追上高松鹅之前最迷离的一段试探。",
            )
        }

        GameSectionCard {
            Text(
                text = "伙伴能力说明",
                style = MaterialTheme.typography.titleMedium,
                fontWeight = FontWeight.Bold,
            )
            AbilityLine(
                name = "团团 · 雪团支援",
                unlocked = rescuedTuanTuan,
                description = "按下支援按钮后，团团会短时间压制敌人的节奏，帮助你通过危险路段。",
            )
            AbilityLine(
                name = "Dror · 同行陪伴",
                unlocked = drorUnlocked,
                description = "Dror 会跟随主角移动，让旅途更有队伍感，也为后续扩展提示型能力留出了位置。",
            )
        }

        GameSectionCard {
            Text(
                text = "队伍面板",
                style = MaterialTheme.typography.titleMedium,
                fontWeight = FontWeight.Bold,
            )
            Row(horizontalArrangement = Arrangement.spacedBy(8.dp)) {
                GameInfoPill("团团 ${if (rescuedTuanTuan) "已归队" else "待救回"}", rescuedTuanTuan)
                GameInfoPill("Dror ${if (drorUnlocked) "已同行" else "未加入"}", drorUnlocked)
            }
            Text("玩家昵称：$playerNickname")
            Text("当前最高分：$bestScore")
        }
    }
}

@Composable
private fun CharacterCard(
    title: String,
    subtitle: String,
    description: String,
    status: String,
    resId: Int,
    contentScale: ContentScale,
) {
    GameSectionCard {
        Row(
            modifier = Modifier.fillMaxWidth(),
            horizontalArrangement = Arrangement.spacedBy(14.dp),
            verticalAlignment = Alignment.CenterVertically,
        ) {
            Image(
                painter = painterResource(resId),
                contentDescription = title,
                modifier =
                    Modifier
                        .size(width = 96.dp, height = 118.dp)
                        .clip(RoundedCornerShape(18.dp)),
                contentScale = contentScale,
            )
            Column(
                modifier = Modifier.weight(1f),
                verticalArrangement = Arrangement.spacedBy(6.dp),
            ) {
                Text(
                    text = title,
                    style = MaterialTheme.typography.titleLarge,
                    fontWeight = FontWeight.Bold,
                )
                Text(
                    text = subtitle,
                    style = MaterialTheme.typography.labelLarge,
                    color = Color(0xFF5B7486),
                )
                Text(
                    text = description,
                    style = MaterialTheme.typography.bodyMedium,
                    color = Color(0xFF304D62),
                )
                Text(
                    text = status,
                    style = MaterialTheme.typography.labelMedium,
                    color = Color(0xFF1F6A8A),
                    fontWeight = FontWeight.SemiBold,
                )
            }
        }
    }
}

private fun storySummaryForLevel(
    level: GameLevel,
    rescuedTuanTuan: Boolean,
    drorUnlocked: Boolean,
): String =
    buildString {
        when (level) {
            GameLevel.CedarVillageRuins -> {
                append("故事从雪松村废墟开始。咕咕嘎嘎刚刚失去家园，还在风雪里寻找第一个伙伴。")
            }

            GameLevel.IceLakeEchoValley -> {
                append("队伍已经穿过雪松村，踏入冰湖回音谷。这里的风声会把远方的危险和线索一起送回来。")
            }

            GameLevel.MistDike -> {
                append("队伍继续向北推进，正在穿越北境雾堤。旅程已经不再只是逃离，而是在主动逼近高松鹅。")
            }
        }
        append(if (rescuedTuanTuan) " 团团已经归队。" else " 团团仍未回到队伍。")
        append(if (drorUnlocked) " Dror 也已经加入同行。" else " Dror 仍只是旅途中的线索。")
    }

private fun levelLabel(level: GameLevel): String =
    when (level) {
        GameLevel.CedarVillageRuins -> "雪松村废墟"
        GameLevel.IceLakeEchoValley -> "冰湖回音谷"
        GameLevel.MistDike -> "北境雾堤"
    }

@Composable
private fun AbilityLine(
    name: String,
    unlocked: Boolean,
    description: String,
) {
    Column(verticalArrangement = Arrangement.spacedBy(4.dp)) {
        Row(
            horizontalArrangement = Arrangement.spacedBy(8.dp),
            verticalAlignment = Alignment.CenterVertically,
        ) {
            Text(
                text = name,
                style = MaterialTheme.typography.titleSmall,
                fontWeight = FontWeight.Bold,
            )
            GameInfoPill(if (unlocked) "已解锁" else "未解锁", unlocked)
        }
        Text(
            text = description,
            style = MaterialTheme.typography.bodyMedium,
            color = Color(0xFF304D62),
        )
    }
}

@Composable
private fun ChapterEventItem(
    chapter: String,
    status: String,
    description: String,
) {
    Column(verticalArrangement = Arrangement.spacedBy(6.dp)) {
        Row(
            modifier = Modifier.fillMaxWidth(),
            horizontalArrangement = Arrangement.SpaceBetween,
            verticalAlignment = Alignment.CenterVertically,
        ) {
            Text(
                text = chapter,
                style = MaterialTheme.typography.titleSmall,
                fontWeight = FontWeight.Bold,
            )
            GameInfoPill(status)
        }
        Text(
            text = description,
            style = MaterialTheme.typography.bodyMedium,
            color = Color(0xFF536675),
        )
    }
}
