package com.example.mygame.ui.home

import androidx.compose.foundation.Image
import androidx.compose.foundation.background
import androidx.compose.foundation.border
import androidx.compose.foundation.clickable
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.Spacer
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.heightIn
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.size
import androidx.compose.foundation.layout.width
import androidx.compose.foundation.lazy.LazyRow
import androidx.compose.foundation.lazy.items
import androidx.compose.foundation.rememberScrollState
import androidx.compose.foundation.shape.CircleShape
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.foundation.verticalScroll
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.setValue
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.draw.alpha
import androidx.compose.ui.draw.clip
import androidx.compose.ui.graphics.Brush
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.graphics.ColorFilter
import androidx.compose.ui.graphics.ColorMatrix
import androidx.compose.ui.layout.ContentScale
import androidx.compose.ui.res.painterResource
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.text.style.TextOverflow
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import com.example.mygame.R
import com.example.mygame.data.SaveRepository
import com.example.mygame.game.level.GameLevel
import com.example.mygame.ui.common.GameBackRow
import com.example.mygame.ui.common.GameHeroCard
import com.example.mygame.ui.common.GameInfoPill
import com.example.mygame.ui.common.GameScreenBackground

private data class CodexCharacter(
    val id: String,
    val title: String,
    val subtitle: String,
    val description: String,
    val status: String,
    val resId: Int,
    val contentScale: ContentScale,
    val lockedVisual: Boolean,
)

private data class CollectibleNote(
    val title: String,
    val tag: String,
    val description: String,
    val iconRes: Int? = null,
)

@Composable
fun CodexScreen(
    saveRepository: SaveRepository,
    onBack: () -> Unit,
) {
    val drorUnlocked = saveRepository.getCompanionDrorUnlocked()
    val rescuedTuanTuan = saveRepository.getRescuedTuanTuan()
    val takamatsuDefeated = saveRepository.getTakamatsuDefeated()
    val resumeLevel = saveRepository.getResumeLevel()
    val playerNickname = saveRepository.getPlayerNickname()
    val bestScore = saveRepository.getBestScore()

    val characters =
        listOf(
            CodexCharacter(
                id = "gugu",
                title = "咕咕嘎嘎",
                subtitle = "雪原旅人",
                description = "家园被毁后踏上向北的旅程。她并不是最强的战士，却总能把失散的伙伴重新聚到一起。",
                status = "当前章节：${levelLabel(resumeLevel)}",
                resId = R.drawable.gugu_sprite,
                contentScale = ContentScale.Crop,
                lockedVisual = false,
            ),
            CodexCharacter(
                id = "boss",
                title = "高松鹅",
                subtitle = "北境威胁",
                description =
                    if (takamatsuDefeated) {
                        "已击败。他曾用冰盾和裂地阻拦旅程，现在只留下雪原深处的一串败退脚印。"
                    } else {
                        "毁掉雪松村的元凶。他一路向北撤退，留下被打破的村庄与失散的伙伴。"
                    },
                status = if (takamatsuDefeated) "威胁已解除" else "继续追踪",
                resId = R.drawable.portrait_takamatsu_goose,
                contentScale = ContentScale.Crop,
                lockedVisual = false,
            ),
            CodexCharacter(
                id = "tuan",
                title = "团团",
                subtitle = "支援伙伴",
                description = "擅长用雪球打乱敌人节奏，是第一位真正回到队伍中的伙伴。",
                status = if (rescuedTuanTuan) "已归队，可发动雪球支援" else "仍待救回",
                resId = R.drawable.companion_dror,
                contentScale = ContentScale.Fit,
                lockedVisual = !rescuedTuanTuan,
            ),
            CodexCharacter(
                id = "dror",
                title = "Dror",
                subtitle = "同行伙伴",
                description = "会跟在主角身侧，为旅程带来队伍感，也为后续解谜、标记隐藏路线留下扩展空间。",
                status = if (drorUnlocked) "已同行" else "暂未加入",
                resId = R.drawable.companion_dror,
                contentScale = ContentScale.Fit,
                lockedVisual = !drorUnlocked,
            ),
        )
    var expandedId by remember { mutableStateOf<String?>(null) }

    GameScreenBackground(modifier = Modifier.verticalScroll(rememberScrollState())) {
        GameBackRow(title = "冒险手册", onBack = onBack)
        GameHeroCard(
            title = "角色、道具与旅程",
            subtitle = "这里记录角色状态、章节事件、伙伴能力和隐藏收集物。它既是图鉴，也是咕咕嘎嘎一路北上的进度档案。",
        )
        StoryStatusPanel(
            resumeLevel = resumeLevel,
            rescuedTuanTuan = rescuedTuanTuan,
            drorUnlocked = drorUnlocked,
            playerNickname = playerNickname,
            bestScore = bestScore,
        )

        SectionTitle("人物档案")
        LazyRow(horizontalArrangement = Arrangement.spacedBy(12.dp), modifier = Modifier.fillMaxWidth()) {
            items(characters, key = { it.id }) { character ->
                CharacterProfileCard(
                    character = character,
                    expanded = expandedId == character.id,
                    onToggle = { expandedId = if (expandedId == character.id) null else character.id },
                )
            }
        }

        SectionTitle("章节事件记录")
        ChapterTimeline(resumeLevel = resumeLevel, rescuedTuanTuan = rescuedTuanTuan)

        SectionTitle("道具与隐藏线索")
        CollectiblePanel()

        SectionTitle("伙伴能力说明")
        AbilityPanel(rescuedTuanTuan = rescuedTuanTuan, drorUnlocked = drorUnlocked)
    }
}

@Composable
private fun StoryStatusPanel(
    resumeLevel: GameLevel,
    rescuedTuanTuan: Boolean,
    drorUnlocked: Boolean,
    playerNickname: String,
    bestScore: Int,
) {
    Column(
        modifier =
            Modifier
                .fillMaxWidth()
                .clip(RoundedCornerShape(20.dp))
                .background(Brush.linearGradient(listOf(Color(0xF6F8FCFF), Color(0xE8DDF3FB))))
                .border(1.dp, Color.White.copy(alpha = 0.9f), RoundedCornerShape(20.dp))
                .padding(14.dp),
        verticalArrangement = Arrangement.spacedBy(8.dp),
    ) {
        Text("当前故事阶段", style = MaterialTheme.typography.titleSmall, fontWeight = FontWeight.Black, color = Color(0xFF1A3044))
        Text(storySummaryForLevel(resumeLevel, rescuedTuanTuan, drorUnlocked), style = MaterialTheme.typography.bodyMedium, color = Color(0xFF3D5C72), lineHeight = 20.sp)
        Row(horizontalArrangement = Arrangement.spacedBy(8.dp)) {
            GameInfoPill("当前 ${levelLabel(resumeLevel)}")
            GameInfoPill("团团 ${if (rescuedTuanTuan) "已归队" else "未归队"}", rescuedTuanTuan)
            GameInfoPill("Dror ${if (drorUnlocked) "已同行" else "未同行"}", drorUnlocked)
        }
        Text("$playerNickname · 最高分 $bestScore", style = MaterialTheme.typography.labelSmall, color = Color(0xFF5F7A8E))
    }
}

@Composable
private fun SectionTitle(text: String) {
    Text(text, style = MaterialTheme.typography.labelLarge, fontWeight = FontWeight.Black, color = Color(0xFF21425A), letterSpacing = 0.3.sp)
}

@Composable
private fun CharacterProfileCard(
    character: CodexCharacter,
    expanded: Boolean,
    onToggle: () -> Unit,
) {
    val filter = if (character.lockedVisual) ColorFilter.colorMatrix(ColorMatrix().apply { setToSaturation(0.35f) }) else null
    Column(
        modifier =
            Modifier
                .width(176.dp)
                .clip(RoundedCornerShape(22.dp))
                .background(if (character.lockedVisual) Color(0xE8E8EEF3) else Color(0xF7F8FCFF))
                .border(1.dp, Color.White.copy(alpha = 0.9f), RoundedCornerShape(22.dp))
                .clickable { onToggle() }
                .padding(12.dp),
    ) {
        Image(
            painter = painterResource(character.resId),
            contentDescription = character.title,
            modifier = Modifier.size(width = 96.dp, height = 108.dp).clip(RoundedCornerShape(16.dp)).then(if (character.lockedVisual) Modifier.alpha(0.78f) else Modifier),
            contentScale = character.contentScale,
            colorFilter = filter,
        )
        Spacer(Modifier.height(8.dp))
        Text(character.title, style = MaterialTheme.typography.titleSmall, fontWeight = FontWeight.ExtraBold, maxLines = 1)
        Text(character.subtitle, style = MaterialTheme.typography.labelSmall, color = Color(0xFF5B768A))
        Text(character.status, style = MaterialTheme.typography.labelSmall, color = Color(0xFF0D6B86), fontWeight = FontWeight.Medium, maxLines = 2, overflow = TextOverflow.Ellipsis)
        Text(character.description, style = MaterialTheme.typography.bodySmall, color = Color(0xFF3D5C6E), maxLines = if (expanded) Int.MAX_VALUE else 2, overflow = TextOverflow.Ellipsis, lineHeight = 18.sp)
        Text(if (expanded) "点击收起" else "点击展开", style = MaterialTheme.typography.labelSmall, color = Color(0xFF6B8CA3))
    }
}

@Composable
private fun ChapterTimeline(
    resumeLevel: GameLevel,
    rescuedTuanTuan: Boolean,
) {
    val events =
        listOf(
            ChapterEvent("第一章：雪松村废墟", chapterStatus(1, resumeLevel)) to if (rescuedTuanTuan) "废墟段已完成，团团已归队。" else "从废墟出发，寻找第一位伙伴。",
            ChapterEvent("第二章：冰湖回音谷", chapterStatus(2, resumeLevel)) to "高台、裂隙与冰湖放大危险，也带回高松鹅的线索。",
            ChapterEvent("第三章：北境雾堤", chapterStatus(3, resumeLevel)) to "雾堤是追上高松鹅前最迷离的一段，也是 Boss 战爆发点。",
        )
    Column(
        modifier =
            Modifier
                .fillMaxWidth()
                .clip(RoundedCornerShape(20.dp))
                .background(Color(0xEAF8FCFF))
                .border(1.dp, Color.White.copy(alpha = 0.9f), RoundedCornerShape(20.dp))
                .padding(14.dp),
    ) {
        events.forEachIndexed { index, (meta, blurb) ->
            Row(Modifier.fillMaxWidth(), verticalAlignment = Alignment.Top) {
                Column(horizontalAlignment = Alignment.CenterHorizontally, modifier = Modifier.width(22.dp)) {
                    Box(Modifier.size(12.dp).background(Color(0xFF2E8AA0), CircleShape))
                    if (index < events.lastIndex) {
                        Spacer(Modifier.width(2.dp).height(42.dp).background(Color(0xFFB0C4D4), RoundedCornerShape(1.dp)))
                    } else {
                        Spacer(Modifier.height(4.dp))
                    }
                }
                Column(Modifier.weight(1f).padding(bottom = 10.dp, start = 4.dp).heightIn(min = 40.dp), verticalArrangement = Arrangement.spacedBy(4.dp)) {
                    Row(Modifier.fillMaxWidth(), horizontalArrangement = Arrangement.SpaceBetween, verticalAlignment = Alignment.CenterVertically) {
                        Text(meta.title, style = MaterialTheme.typography.titleSmall, fontWeight = FontWeight.Bold, color = Color(0xFF1A3344))
                        Text(meta.badge, style = MaterialTheme.typography.labelSmall, color = Color(0xFF5A7A8C))
                    }
                    Text(blurb, style = MaterialTheme.typography.bodySmall, color = Color(0xFF4A6678), lineHeight = 18.sp)
                }
            }
        }
    }
}

@Composable
private fun CollectiblePanel() {
    val notes =
        listOf(
            CollectibleNote("小鱼干", "冲刺", "拾取后获得短时间冲刺，适合跨越宽裂隙、破冰盾或抢救失误路线。"),
            CollectibleNote("泡泡围巾", "护盾", "为咕咕嘎嘎挡下一次危险，适合留到高压敌人和 Boss 护盾阶段。"),
            CollectibleNote("长跳靴", "跳跃", "临时提升跳跃窗口和高度，鼓励走高台路线寻找隐藏奖励。"),
            CollectibleNote("极光磁针", "吸附", "吸引附近鱼干、信标和剧情残页。营地升级后持续时间更长。", R.drawable.ic_item_aurora_magnet),
            CollectibleNote("蓝色信标", "探索分", "通常放在安全路线之外，拾取会提供额外分数，也暗示附近可能有隐藏路线。", R.drawable.ic_item_beacon),
            CollectibleNote("剧情残页", "彩蛋", "记录高松鹅撤退留下的碎片线索，主线拾取会给更高奖励分。", R.drawable.ic_item_lore_page),
        )
    Column(
        modifier =
            Modifier
                .fillMaxWidth()
                .clip(RoundedCornerShape(20.dp))
                .background(Color(0xEAF8FCFF))
                .border(1.dp, Color.White.copy(alpha = 0.9f), RoundedCornerShape(20.dp))
                .padding(14.dp),
        verticalArrangement = Arrangement.spacedBy(10.dp),
    ) {
        notes.forEach { note ->
            Row(horizontalArrangement = Arrangement.spacedBy(10.dp), verticalAlignment = Alignment.Top) {
                if (note.iconRes != null) {
                    Image(
                        painter = painterResource(note.iconRes),
                        contentDescription = note.title,
                        modifier = Modifier.size(42.dp).clip(RoundedCornerShape(14.dp)),
                    )
                } else {
                    GameInfoPill(note.tag, active = true)
                }
                Column(Modifier.weight(1f), verticalArrangement = Arrangement.spacedBy(2.dp)) {
                    Text(note.title, style = MaterialTheme.typography.labelLarge, fontWeight = FontWeight.Bold, color = Color(0xFF1A3344))
                    Text("${note.tag} · ${note.description}", style = MaterialTheme.typography.bodySmall, color = Color(0xFF3D5C6E), lineHeight = 18.sp)
                }
            }
        }
    }
}

@Composable
private fun AbilityPanel(
    rescuedTuanTuan: Boolean,
    drorUnlocked: Boolean,
) {
    Column(
        modifier =
            Modifier
                .fillMaxWidth()
                .clip(RoundedCornerShape(20.dp))
                .background(Color(0xEAF8FCFF))
                .border(1.dp, Color.White.copy(alpha = 0.9f), RoundedCornerShape(20.dp))
                .padding(14.dp),
        verticalArrangement = Arrangement.spacedBy(12.dp),
    ) {
        AbilityLine("团团 · 雪球支援", rescuedTuanTuan, "短时间干扰敌人并制造安全窗口，适合在裂谷、Boss 护盾和拥挤路段前使用。")
        AbilityLine("Dror · 同行陪伴", drorUnlocked, "跟随主角移动，强化队伍陪伴感；后续可扩展为解谜、标记隐藏道路或提醒危险。")
    }
}

private data class ChapterEvent(val title: String, val badge: String)

private fun chapterStatus(index: Int, resume: GameLevel): String =
    when (index) {
        1 -> if (resume == GameLevel.CedarVillageRuins) "当前" else "已穿越"
        2 ->
            when (resume) {
                GameLevel.CedarVillageRuins -> "即将"
                GameLevel.IceLakeEchoValley -> "当前"
                GameLevel.MistDike -> "已穿越"
            }
        3 -> if (resume == GameLevel.MistDike) "当前" else "待深入"
        else -> "-"
    }

private fun storySummaryForLevel(level: GameLevel, rescuedTuanTuan: Boolean, drorUnlocked: Boolean): String =
    buildString {
        when (level) {
            GameLevel.CedarVillageRuins -> append("从雪松村废墟出发，在风雪中寻找第一位伙伴。")
            GameLevel.IceLakeEchoValley -> append("已踏入冰湖回音谷，远方风声与危险一同逼近。")
            GameLevel.MistDike -> append("正在穿越北境雾堤，在迷离雪雾中继续逼近高松鹅。")
        }
        append(if (rescuedTuanTuan) " 团团已归队。" else " 团团仍流落在外。")
        append(if (drorUnlocked) " Dror 同行中。" else " Dror 尚未入队。")
    }

private fun levelLabel(level: GameLevel): String =
    when (level) {
        GameLevel.CedarVillageRuins -> "雪松村废墟"
        GameLevel.IceLakeEchoValley -> "冰湖回音谷"
        GameLevel.MistDike -> "北境雾堤"
    }

@Composable
private fun AbilityLine(name: String, unlocked: Boolean, description: String) {
    Column(verticalArrangement = Arrangement.spacedBy(4.dp)) {
        Row(horizontalArrangement = Arrangement.spacedBy(8.dp), verticalAlignment = Alignment.CenterVertically) {
            Text(name, style = MaterialTheme.typography.labelLarge, fontWeight = FontWeight.SemiBold)
            GameInfoPill(if (unlocked) "已解锁" else "未解锁", unlocked)
        }
        Text(description, style = MaterialTheme.typography.bodySmall, color = Color(0xFF3D5C6E))
    }
}
