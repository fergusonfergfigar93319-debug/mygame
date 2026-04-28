package com.example.mygame.ui.leaderboard

import androidx.compose.animation.core.LinearEasing
import androidx.compose.animation.core.RepeatMode
import androidx.compose.animation.core.animateFloat
import androidx.compose.animation.core.infiniteRepeatable
import androidx.compose.animation.core.rememberInfiniteTransition
import androidx.compose.animation.core.tween
import androidx.compose.foundation.background
import androidx.compose.foundation.border
import androidx.compose.foundation.clickable
import androidx.compose.foundation.interaction.MutableInteractionSource
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.PaddingValues
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.Spacer
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.size
import androidx.compose.foundation.layout.statusBarsPadding
import androidx.compose.foundation.layout.width
import androidx.compose.foundation.lazy.LazyColumn
import androidx.compose.foundation.lazy.itemsIndexed
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.automirrored.rounded.ArrowBack
import androidx.compose.material.icons.rounded.Edit
import androidx.compose.material3.Icon
import androidx.compose.material3.IconButton
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.OutlinedTextField
import androidx.compose.material3.OutlinedTextFieldDefaults
import androidx.compose.material3.Text
import androidx.compose.material3.TextButton
import androidx.compose.runtime.Composable
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.setValue
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.draw.clip
import androidx.compose.ui.draw.drawBehind
import androidx.compose.ui.draw.drawWithContent
import androidx.compose.ui.geometry.CornerRadius
import androidx.compose.ui.geometry.Offset
import androidx.compose.ui.geometry.Size
import androidx.compose.ui.graphics.Brush
import androidx.compose.ui.graphics.BlendMode
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.graphics.TransformOrigin
import androidx.compose.ui.graphics.StrokeJoin
import androidx.compose.ui.graphics.drawscope.Stroke
import androidx.compose.ui.graphics.graphicsLayer
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.text.style.TextAlign
import androidx.compose.ui.text.style.TextOverflow
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import androidx.compose.ui.window.Dialog
import com.example.mygame.data.LeaderboardRepository
import com.example.mygame.data.LeaderboardSort
import com.example.mygame.data.SaveRepository
import com.example.mygame.data.model.LeaderboardEntry
import com.example.mygame.game.modes.EndlessDailyChallenge
import com.example.mygame.ui.home.LobbySnowEffect
import java.text.SimpleDateFormat
import java.util.Date
import java.util.Locale

private val LobbyBackgroundGradient =
    listOf(
        Color(0xFF071423),
        Color(0xFF10324B),
        Color(0xFF4D7F9B),
        Color(0xFFB9D9E8),
    )

private val AccentCyan = Color(0xFF64FFDA)
private val GlassFill = Color(0x18FFFFFF)
private val GlassBorder = Color(0x33FFFFFF)
private val Gold = Color(0xFFFFD700)
private val Silver = Color(0xFFC0C0C0)
private val Bronze = Color(0xFFCD7F32)

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
    var nicknameDraft by remember { mutableStateOf(saveRepository.getPlayerNickname()) }
    var showNicknameDialog by remember { mutableStateOf(false) }

    val playerId = remember(saveRepository) { saveRepository.getOrCreatePlayerId() }
    val todayBucket = remember { EndlessDailyChallenge.todayBucketLocal() }

    val entries =
        remember(sort, listScope, leaderboardRepository, todayBucket) {
            leaderboardRepository.getTopEntries(
                20,
                sort,
                if (listScope == LeaderboardListScope.DailyToday) todayBucket else null,
            )
        }

    val (best, recent, avg) =
        remember(listScope, leaderboardRepository, todayBucket) {
            when (listScope) {
                LeaderboardListScope.All ->
                    Triple(
                        leaderboardRepository.getBestEntry(),
                        leaderboardRepository.getMostRecentEntry(),
                        leaderboardRepository.getAverageTotalScore(),
                    )
                LeaderboardListScope.DailyToday -> {
                    val inBucket =
                        leaderboardRepository.getTopEntries(200, LeaderboardSort.ByTotalScore, todayBucket)
                    Triple(
                        inBucket.firstOrNull(),
                        inBucket.maxByOrNull { it.timestampMillis },
                        if (inBucket.isEmpty()) {
                            0.0
                        } else {
                            inBucket.sumOf { it.totalScore.toDouble() } / inBucket.size
                        },
                    )
                }
            }
        }

    Box(
        modifier =
            modifier
                .fillMaxSize()
                .background(Brush.verticalGradient(LobbyBackgroundGradient)),
    ) {
        LobbySnowEffect(Modifier.fillMaxSize())

        Column(Modifier.fillMaxSize().statusBarsPadding()) {
            LeaderboardTopBar(
                title = "积分榜",
                onBack = onBack,
                onEditNickname = {
                    nicknameDraft = saveRepository.getPlayerNickname()
                    showNicknameDialog = true
                },
            )

            val topMist = Color(0xFF071423)
            Box(Modifier.fillMaxSize().weight(1f, fill = true)) {
            LazyColumn(
                modifier =
                    Modifier
                        .fillMaxSize()
                        .drawWithContent {
                            drawContent()
                            val m = 28.dp.toPx()
                            if (m > 0f && size.width > 0f) {
                                drawRect(
                                    brush =
                                        Brush.verticalGradient(
                                            0f to topMist.copy(alpha = 0.94f),
                                            0.45f to topMist.copy(alpha = 0.35f),
                                            1f to Color.Transparent,
                                        ),
                                    size = Size(size.width, m),
                                )
                            }
                        },
                contentPadding = PaddingValues(horizontal = 16.dp, vertical = 8.dp),
                verticalArrangement = Arrangement.spacedBy(12.dp),
            ) {
                item {
                    Text(
                        text = "极夜雪原 · 荣誉榜",
                        color = Color.White.copy(alpha = 0.9f),
                        style = MaterialTheme.typography.titleLarge,
                        fontWeight = FontWeight.Bold,
                    )
                    Text(
                        text =
                            if (listScope == LeaderboardListScope.DailyToday) {
                                "今日挑战 · 固定种子的成绩"
                            } else {
                                "总榜、路程与存活 · 本地历史"
                            },
                        color = Color.White.copy(alpha = 0.45f),
                        style = MaterialTheme.typography.bodySmall,
                        modifier = Modifier.padding(top = 4.dp),
                    )
                }

                item {
                    StatsSummaryRow(
                        bestTotal = best?.totalScore,
                        recentTotal = recent?.totalScore,
                        avg = avg,
                    )
                }

                item {
                    FilterAndSortPanel(
                        listScope = listScope,
                        onListScope = { listScope = it },
                        sort = sort,
                        onSort = { sort = it },
                        statusText =
                            if (listScope == LeaderboardListScope.DailyToday) {
                                "今日：$todayBucket"
                            } else {
                                "当前：本地历史"
                            },
                    )
                }

                item {
                    Text(
                        text = "Top 20",
                        color = AccentCyan.copy(alpha = 0.9f),
                        style = MaterialTheme.typography.titleMedium,
                        fontWeight = FontWeight.ExtraBold,
                        modifier = Modifier.padding(top = 4.dp, bottom = 2.dp),
                    )
                }

                if (entries.isEmpty()) {
                    item {
                        GlassBlock(modifier = Modifier.fillMaxWidth()) {
                            Text(
                                text = "暂无记录，去极夜漂流留下第一条吧",
                                color = Color.White.copy(alpha = 0.5f),
                                textAlign = TextAlign.Center,
                                modifier = Modifier.fillMaxWidth().padding(24.dp),
                            )
                        }
                    }
                } else {
                    itemsIndexed(
                        items = entries,
                        key = { _, e -> e.id },
                    ) { index, entry ->
                        LeaderboardRankRow(
                            rank = index + 1,
                            entry = entry,
                            sort = sort,
                            isCurrentPlayer = entry.playerId == playerId,
                            showChallengeTag = listScope == LeaderboardListScope.All,
                        )
                    }
                }

                item { Spacer(Modifier.height(24.dp)) }
            }
            }
        }
    }

    if (showNicknameDialog) {
        NicknameEditDialog(
            value = nicknameDraft,
            onValueChange = { s -> nicknameDraft = s },
            onDismiss = { showNicknameDialog = false },
            onSave = {
                saveRepository.setPlayerNickname(nicknameDraft)
                showNicknameDialog = false
            },
        )
    }
}

@Composable
private fun LeaderboardTopBar(
    title: String,
    onBack: () -> Unit,
    onEditNickname: () -> Unit,
) {
    Row(
        modifier =
            Modifier
                .fillMaxWidth()
                .padding(horizontal = 4.dp, vertical = 4.dp),
        verticalAlignment = Alignment.CenterVertically,
    ) {
        TextButton(onClick = onBack) {
            Icon(
                Icons.AutoMirrored.Rounded.ArrowBack,
                contentDescription = null,
                tint = AccentCyan,
                modifier = Modifier.size(20.dp).padding(end = 4.dp),
            )
            Text("返回", color = AccentCyan, fontWeight = FontWeight.SemiBold)
        }
        Text(
            text = title,
            color = Color.White,
            style = MaterialTheme.typography.titleLarge,
            fontWeight = FontWeight.Bold,
            modifier = Modifier.weight(1f),
            textAlign = TextAlign.Center,
        )
        IconButton(onClick = onEditNickname) {
            Icon(
                Icons.Rounded.Edit,
                contentDescription = "编辑排行榜昵称",
                tint = Color.White.copy(alpha = 0.85f),
            )
        }
    }
}

@Composable
private fun StatsSummaryRow(
    bestTotal: Int?,
    recentTotal: Int?,
    avg: Double,
) {
    Row(
        modifier = Modifier.fillMaxWidth(),
        horizontalArrangement = Arrangement.spacedBy(10.dp),
    ) {
        StatMiniCell("本地最佳", if (bestTotal != null) "$bestTotal" else "—", 0, Modifier.weight(1f))
        StatMiniCell("最近一局", if (recentTotal != null) "$recentTotal" else "—", 1, Modifier.weight(1f))
        StatMiniCell("平均总分", "%.1f".format(avg), 2, Modifier.weight(1f))
    }
}

@Composable
private fun StatMiniCell(
    label: String,
    value: String,
    /** 错开三格的流光相位，使边缘更有「扫描」感 */
    phaseIndex: Int,
    modifier: Modifier = Modifier,
) {
    val transition = rememberInfiniteTransition(label = "stat_shimmer")
    val baseAngle by transition.animateFloat(
        initialValue = 0f,
        targetValue = 360f,
        animationSpec =
            infiniteRepeatable(
                animation = tween(4_200, easing = LinearEasing),
                repeatMode = RepeatMode.Restart,
            ),
        label = "sweep",
    )
    val angle = baseAngle + phaseIndex * 28f
    val outerShape = RoundedCornerShape(18.dp)
    val innerShape = RoundedCornerShape(16.dp)
    val innerPad = 2.5.dp
    Box(modifier.clip(outerShape)) {
        Box(
            Modifier
                .matchParentSize()
                .graphicsLayer {
                    rotationZ = angle
                    transformOrigin = TransformOrigin(0.5f, 0.5f)
                }
                .drawBehind {
                    val w = size.width
                    val h = size.height
                    if (w <= 0f || h <= 0f) return@drawBehind
                    val c = Offset(w * 0.5f, h * 0.5f)
                    val sw = 2.3.dp.toPx()
                    val half = sw * 0.5f
                    val cr = 18.dp.toPx() - sw
                    drawRoundRect(
                        brush =
                            Brush.sweepGradient(
                                0.0f to Color.Transparent,
                                0.2f to AccentCyan.copy(alpha = 0.2f),
                                0.35f to AccentCyan.copy(alpha = 0.9f),
                                0.5f to AccentCyan.copy(alpha = 0.2f),
                                0.75f to Color.Transparent,
                                1.0f to Color.Transparent,
                                center = c,
                            ),
                        topLeft = Offset(half, half),
                        size = Size(w - sw, h - sw),
                        cornerRadius = CornerRadius(cr, cr),
                        style = Stroke(width = sw, join = StrokeJoin.Round),
                    )
                },
        ) {}
        Box(
            Modifier
                .matchParentSize()
                .padding(innerPad)
                .clip(innerShape)
                .background(GlassFill),
        ) {
            Column(
                modifier = Modifier.fillMaxWidth().padding(vertical = 10.dp, horizontal = 8.dp),
                horizontalAlignment = Alignment.CenterHorizontally,
            ) {
                Text(
                    text = label,
                    color = Color.White.copy(alpha = 0.45f),
                    style = MaterialTheme.typography.labelSmall,
                )
                Text(
                    text = value,
                    color = Color.White,
                    fontWeight = FontWeight.Bold,
                    fontSize = 16.sp,
                    maxLines = 1,
                    overflow = TextOverflow.Ellipsis,
                )
            }
        }
    }
}

@Composable
private fun FilterAndSortPanel(
    listScope: LeaderboardListScope,
    onListScope: (LeaderboardListScope) -> Unit,
    sort: LeaderboardSort,
    onSort: (LeaderboardSort) -> Unit,
    statusText: String,
) {
    GlassBlock(Modifier.fillMaxWidth()) {
        Column(Modifier.padding(14.dp), verticalArrangement = Arrangement.spacedBy(12.dp)) {
            Text("榜单范围", color = Color.White.copy(alpha = 0.5f), style = MaterialTheme.typography.labelMedium)
            Row(horizontalArrangement = Arrangement.spacedBy(8.dp)) {
                GlowPill("全部", listScope == LeaderboardListScope.All) {
                    onListScope(LeaderboardListScope.All)
                }
                GlowPill("今日挑战", listScope == LeaderboardListScope.DailyToday) {
                    onListScope(LeaderboardListScope.DailyToday)
                }
            }
            Text("排序", color = Color.White.copy(alpha = 0.5f), style = MaterialTheme.typography.labelMedium)
            Row(horizontalArrangement = Arrangement.spacedBy(8.dp)) {
                GlowPill("总分", sort == LeaderboardSort.ByTotalScore) { onSort(LeaderboardSort.ByTotalScore) }
                GlowPill("路程", sort == LeaderboardSort.ByDistance) { onSort(LeaderboardSort.ByDistance) }
                GlowPill("存活", sort == LeaderboardSort.BySurvivalTime) {
                    onSort(LeaderboardSort.BySurvivalTime)
                }
            }
            Text(
                text = statusText,
                color = AccentCyan.copy(alpha = 0.55f),
                style = MaterialTheme.typography.labelSmall,
            )
        }
    }
}

@Composable
private fun GlowPill(
    text: String,
    selected: Boolean,
    onClick: () -> Unit,
) {
    val interaction = remember { MutableInteractionSource() }
    Column(
        horizontalAlignment = Alignment.CenterHorizontally,
        modifier =
            Modifier
                .clip(RoundedCornerShape(12.dp))
                .clickable(interaction, indication = null, onClick = onClick)
                .padding(horizontal = 4.dp, vertical = 2.dp),
    ) {
        Text(
            text = text,
            color = if (selected) AccentCyan else Color.White.copy(alpha = 0.65f),
            fontWeight = if (selected) FontWeight.ExtraBold else FontWeight.Medium,
            style = MaterialTheme.typography.labelLarge,
        )
        Spacer(Modifier.height(4.dp))
        Box(
            modifier =
                Modifier
                    .height(3.dp)
                    .width(36.dp)
                    .clip(RoundedCornerShape(2.dp))
                    .background(
                        if (selected) {
                            Brush.horizontalGradient(
                                listOf(AccentCyan.copy(alpha = 0.2f), AccentCyan, AccentCyan.copy(alpha = 0.2f)),
                            )
                        } else {
                            Brush.horizontalGradient(
                                listOf(Color.Transparent, Color.Transparent),
                            )
                        },
                    ),
        )
    }
}

@Composable
private fun GlassBlock(
    modifier: Modifier = Modifier,
    content: @Composable () -> Unit,
) {
    Box(
        modifier =
            modifier
                .clip(RoundedCornerShape(18.dp))
                .background(GlassFill)
                .border(1.dp, GlassBorder, RoundedCornerShape(18.dp)),
    ) {
        content()
    }
}

@Composable
private fun LeaderboardRankRow(
    rank: Int,
    entry: LeaderboardEntry,
    sort: LeaderboardSort,
    isCurrentPlayer: Boolean,
    showChallengeTag: Boolean,
) {
    val rankTint =
        when (rank) {
            1 -> Gold
            2 -> Silver
            3 -> Bronze
            else -> Color.White.copy(alpha = 0.85f)
        }
    val df = remember { SimpleDateFormat("MM-dd HH:mm", Locale.getDefault()) }
    val primary = entry.primaryDisplay(sort)
    val primaryLabel = entry.primaryLabel(sort)

    val championBreath = rememberInfiniteTransition(label = "champion_breath")
    val breathScale by
        championBreath.animateFloat(
            initialValue = 1f,
            targetValue = 1.02f,
            animationSpec =
                infiniteRepeatable(
                    animation = tween(3_600, easing = LinearEasing),
                    repeatMode = RepeatMode.Reverse,
                ),
            label = "breath",
        )
    val rowScale = if (rank == 1) breathScale else 1f

    val rowShape = RoundedCornerShape(16.dp)
    Box(
        modifier =
            Modifier
                .fillMaxWidth()
                .graphicsLayer {
                    transformOrigin = TransformOrigin(0.5f, 0.5f)
                    scaleX = rowScale
                    scaleY = rowScale
                },
    ) {
    Row(
        modifier =
            Modifier
                .fillMaxWidth()
                .clip(rowShape)
                .drawBehind {
                    val rPx = 16.dp.toPx()
                    if (rank <= 3) {
                        val cx = 0.08f * size.width
                        val cy = 0.5f * size.height
                        val c = Offset(cx, cy)
                        val rad = size.width / 1.5f
                        drawCircle(
                            brush =
                                Brush.radialGradient(
                                    0f to rankTint.copy(alpha = 0.45f),
                                    0.7f to rankTint.copy(alpha = 0.1f),
                                    1f to Color.Transparent,
                                    center = c,
                                    radius = rad,
                                ),
                            radius = rad,
                            center = c,
                            blendMode = BlendMode.Screen,
                        )
                    }
                    val fill =
                        if (isCurrentPlayer) {
                            Color(0x4464FFDA)
                        } else {
                            Color(0x16FFFFFF)
                        }
                    drawRoundRect(
                        color = fill,
                        topLeft = Offset.Zero,
                        size = Size(size.width, size.height),
                        cornerRadius = CornerRadius(rPx, rPx),
                    )
                }
                .border(
                    1.dp,
                    if (isCurrentPlayer) AccentCyan.copy(alpha = 0.5f) else Color.White.copy(alpha = 0.12f),
                    rowShape,
                )
                .padding(horizontal = 14.dp, vertical = 12.dp),
        verticalAlignment = Alignment.CenterVertically,
    ) {
            Text(
                text = "#$rank",
                color = rankTint,
                style = MaterialTheme.typography.titleLarge,
                fontWeight = FontWeight.Black,
                modifier = Modifier.width(48.dp),
            )
            Column(modifier = Modifier.weight(1f)) {
                Text(
                    text = entry.nickname,
                    color = Color.White,
                    fontWeight = FontWeight.Bold,
                    maxLines = 1,
                    overflow = TextOverflow.Ellipsis,
                )
                Text(
                    text = buildString {
                        append("路程 ${"%.0f".format(entry.distanceScoreUnits)}")
                        append(" · 存活 ${entry.survivalSeconds.toInt()}s")
                        if (showChallengeTag && entry.challengeBucket != null) {
                            append(" · 挑战 ${entry.challengeBucket}")
                        }
                    },
                    color = Color.White.copy(alpha = 0.4f),
                    style = MaterialTheme.typography.labelSmall,
                )
                Text(
                    text = df.format(Date(entry.timestampMillis)),
                    color = Color.White.copy(alpha = 0.32f),
                    style = MaterialTheme.typography.labelSmall,
                )
            }
            Column(horizontalAlignment = Alignment.End) {
                Text(
                    text = primary,
                    color = if (rank <= 3) rankTint else AccentCyan,
                    fontWeight = FontWeight.ExtraBold,
                    style = MaterialTheme.typography.titleMedium,
                )
                Text(
                    text = primaryLabel,
                    color = Color.White.copy(alpha = 0.4f),
                    style = MaterialTheme.typography.labelSmall,
                )
            }
    }
    }
}

private fun LeaderboardEntry.primaryLabel(sort: LeaderboardSort): String =
    when (sort) {
        LeaderboardSort.ByTotalScore -> "总分"
        LeaderboardSort.ByDistance -> "路程"
        LeaderboardSort.BySurvivalTime -> "存活"
    }

private fun LeaderboardEntry.primaryDisplay(sort: LeaderboardSort): String =
    when (sort) {
        LeaderboardSort.ByTotalScore -> "$totalScore"
        LeaderboardSort.ByDistance -> "%.0f".format(distanceScoreUnits)
        LeaderboardSort.BySurvivalTime -> "${survivalSeconds.toInt()}s"
    }

@Composable
private fun NicknameEditDialog(
    value: String,
    onValueChange: (String) -> Unit,
    onDismiss: () -> Unit,
    onSave: () -> Unit,
) {
    Dialog(onDismissRequest = onDismiss) {
        GlassBlock(Modifier.fillMaxWidth()) {
            Column(Modifier.padding(20.dp), verticalArrangement = Arrangement.spacedBy(12.dp)) {
                Text(
                    text = "排行榜昵称",
                    color = Color.White,
                    style = MaterialTheme.typography.titleMedium,
                    fontWeight = FontWeight.Bold,
                )
                OutlinedTextField(
                    value = value,
                    onValueChange = onValueChange,
                    singleLine = true,
                    label = { Text("昵称", color = Color.White.copy(alpha = 0.5f)) },
                    colors =
                        OutlinedTextFieldDefaults.colors(
                            focusedTextColor = Color.White,
                            unfocusedTextColor = Color.White,
                            focusedBorderColor = AccentCyan,
                            unfocusedBorderColor = Color.White.copy(alpha = 0.3f),
                            cursorColor = AccentCyan,
                        ),
                    modifier = Modifier.fillMaxWidth(),
                )
                Row(
                    modifier = Modifier.fillMaxWidth(),
                    horizontalArrangement = Arrangement.End,
                    verticalAlignment = Alignment.CenterVertically,
                ) {
                    TextButton(onClick = onDismiss) {
                        Text("取消", color = Color.White.copy(alpha = 0.7f))
                    }
                    TextButton(onClick = onSave) {
                        Text("保存", color = AccentCyan, fontWeight = FontWeight.Bold)
                    }
                }
            }
        }
    }
}
