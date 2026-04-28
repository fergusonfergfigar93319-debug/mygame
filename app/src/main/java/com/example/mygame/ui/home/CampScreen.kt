package com.example.mygame.ui.home

import androidx.compose.animation.core.animateFloatAsState
import androidx.compose.foundation.background
import androidx.compose.foundation.border
import androidx.compose.foundation.interaction.MutableInteractionSource
import androidx.compose.foundation.interaction.collectIsPressedAsState
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.PaddingValues
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.Spacer
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.navigationBarsPadding
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.size
import androidx.compose.foundation.layout.statusBarsPadding
import androidx.compose.foundation.layout.width
import androidx.compose.foundation.layout.widthIn
import androidx.compose.foundation.lazy.LazyColumn
import androidx.compose.foundation.shape.CircleShape
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.automirrored.rounded.ArrowBack
import androidx.compose.material.icons.rounded.AutoAwesome
import androidx.compose.material.icons.rounded.Explore
import androidx.compose.material.icons.rounded.Favorite
import androidx.compose.material.icons.rounded.Radar
import androidx.compose.material.icons.rounded.Speed
import androidx.compose.material3.Button
import androidx.compose.material3.ButtonDefaults
import androidx.compose.material3.Icon
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.Text
import androidx.compose.material3.TextButton
import androidx.compose.runtime.Composable
import androidx.compose.runtime.LaunchedEffect
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableIntStateOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.setValue
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.draw.clip
import androidx.compose.ui.draw.scale
import androidx.compose.ui.graphics.Brush
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.graphics.vector.ImageVector
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.text.style.TextAlign
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import com.example.mygame.data.CampUpgradeKind
import com.example.mygame.data.SaveRepository
import com.example.mygame.game.modes.EndlessBalanceConfig

private val CampNightGradient =
    listOf(
        Color(0xFF0B1420),
        Color(0xFF122436),
        Color(0xFF1A2835),
        Color(0xFF2A2018),
        Color(0xFF3D2B1F),
    )

private val AccentCyan = Color(0xFF64FFDA)
private val FishPillFill = Color(0xFFFEEE5A)
private val FishPillText = Color(0xFF1A1208)
private val GlassFill = Color(0x18FFFFFF)
private val GlassBorder = Color(0x33FFFFFF)

@Composable
fun CampScreen(
    saveRepository: SaveRepository,
    onBack: () -> Unit,
) {
    var refresh by remember { mutableIntStateOf(0) }
    val totalFish = remember(refresh) { saveRepository.getTotalFishSnacks() }
    val dashLv = remember(refresh) { saveRepository.getCampDashLevel() }
    val tuanLv = remember(refresh) { saveRepository.getCampTuanLevel() }
    val polarLv = remember(refresh) { saveRepository.getCampPolarIntuitionLevel() }
    val magnetLv = remember(refresh) { saveRepository.getCampMagnetLevel() }

    LaunchedEffect(Unit) {
        saveRepository.markCampVisited()
    }

    Box(
        modifier =
            Modifier
                .fillMaxSize()
                .background(Brush.verticalGradient(CampNightGradient)),
    ) {
        CampFireSparksEffect(Modifier.fillMaxSize())

        Column(Modifier.fillMaxSize().statusBarsPadding()) {
            CampTopBar(totalFish = totalFish, onBack = onBack)

            LazyColumn(
                modifier = Modifier
                    .fillMaxSize()
                    .navigationBarsPadding(),
                contentPadding = PaddingValues(horizontal = 20.dp, vertical = 12.dp),
                verticalArrangement = Arrangement.spacedBy(18.dp),
            ) {
                item {
                    CampHeader(
                        title = "雪线边的临时小屋",
                        subtitle = "在这里，带回来的每一条鱼干都能多一分活下去的把握。",
                    )
                }
                item {
                    CampUpgradeEntry(
                        title = "冲刺强化",
                        subtitle = "鱼干冲刺",
                        description = "拾取鱼干后延长冲刺状态，更容易覆盖长裂隙、盾敌和 Boss 破盾窗口。",
                        icon = Icons.Rounded.Speed,
                        currentLabel = { lv ->
                            val cur = 7f + lv
                            if (lv >= 2) "冲刺约 ${cur.format1()}s · 已达上限" else "体感：约 ${cur.format1()}s → 下一档 ${(cur + 1f).format1()}s"
                        },
                        currentLevel = dashLv,
                        upgradeCost = saveRepository.getCampUpgradeCost(CampUpgradeKind.Dash, dashLv),
                        totalFish = totalFish,
                        onUpgrade = { if (saveRepository.tryPurchaseCampUpgrade(CampUpgradeKind.Dash)) refresh++ },
                    )
                }
                item {
                    CampUpgradeEntry(
                        title = "团团好感",
                        subtitle = "雪球支援",
                        description = "团团支援持续更久，适合通过混合敌人路段、薄冰平台和 Boss 护盾阶段。",
                        icon = Icons.Rounded.Favorite,
                        currentLabel = { lv ->
                            val cur = 4.5f + lv * 0.55f
                            val next = 4.5f + (lv + 1) * 0.55f
                            if (lv >= 2) "掩护约 ${cur.format1()}s · 已达上限" else "体感：约 ${cur.format1()}s → 约 ${next.format1()}s"
                        },
                        currentLevel = tuanLv,
                        upgradeCost = saveRepository.getCampUpgradeCost(CampUpgradeKind.Tuan, tuanLv),
                        totalFish = totalFish,
                        onUpgrade = { if (saveRepository.tryPurchaseCampUpgrade(CampUpgradeKind.Tuan)) refresh++ },
                    )
                }
                item {
                    val base = EndlessBalanceConfig.rewardSpacingWorldWidthMultiplier
                    CampUpgradeEntry(
                        title = "极地直觉",
                        subtitle = "补给航道",
                        description = "无尽模式中更容易插入补给休整段，给玩家短暂喘息和追分窗口。",
                        icon = Icons.Rounded.Explore,
                        currentLabel = { lv ->
                            val cur = base * (1f - 0.1f * lv)
                            val next = base * (1f - 0.1f * (lv + 1))
                            if (lv >= 2) "休整更容易 · 系数 ${cur.format1()} 已满" else "间隔：${cur.format1()} → 下一档 ${next.format1()}"
                        },
                        currentLevel = polarLv,
                        upgradeCost = saveRepository.getCampUpgradeCost(CampUpgradeKind.Polar, polarLv),
                        totalFish = totalFish,
                        onUpgrade = { if (saveRepository.tryPurchaseCampUpgrade(CampUpgradeKind.Polar)) refresh++ },
                    )
                }
                item {
                    CampUpgradeEntry(
                        title = "磁针调校",
                        subtitle = "极光磁针",
                        description = "极光磁针吸附时间更长，让高风险金币线变成更值得尝试的路线选择。",
                        icon = Icons.Rounded.Radar,
                        currentLabel = { lv ->
                            val cur = 10f + lv * 1.5f
                            val next = 10f + (lv + 1) * 1.5f
                            if (lv >= 2) "吸附约 ${cur.format1()}s · 已达上限" else "体感：约 ${cur.format1()}s → 约 ${next.format1()}s"
                        },
                        currentLevel = magnetLv,
                        upgradeCost = saveRepository.getCampUpgradeCost(CampUpgradeKind.Magnet, magnetLv),
                        totalFish = totalFish,
                        onUpgrade = { if (saveRepository.tryPurchaseCampUpgrade(CampUpgradeKind.Magnet)) refresh++ },
                    )
                }
                item { Spacer(Modifier.height(12.dp)) }
            }
        }
    }
}

@Composable
private fun CampTopBar(
    totalFish: Int,
    onBack: () -> Unit,
) {
    Row(
        modifier = Modifier.fillMaxWidth().padding(horizontal = 4.dp, vertical = 4.dp),
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
            text = "补给营地",
            color = Color.White,
            style = MaterialTheme.typography.titleLarge,
            fontWeight = FontWeight.Bold,
            modifier = Modifier.weight(1f),
            textAlign = TextAlign.Center,
        )
        Row(
            modifier =
                Modifier
                    .widthIn(min = 100.dp)
                    .clip(RoundedCornerShape(999.dp))
                    .background(FishPillFill)
                    .border(1.dp, Color(0x99FFF59D), RoundedCornerShape(999.dp))
                    .padding(horizontal = 12.dp, vertical = 7.dp),
            verticalAlignment = Alignment.CenterVertically,
            horizontalArrangement = Arrangement.spacedBy(8.dp),
        ) {
            Box(Modifier.size(7.dp).clip(CircleShape).background(Color(0xFF2E7D32)))
            Row(verticalAlignment = Alignment.CenterVertically) {
                Text("鱼干 ", color = FishPillText.copy(alpha = 0.88f), style = MaterialTheme.typography.labelMedium, fontWeight = FontWeight.SemiBold)
                Text(
                    text = "$totalFish",
                    color = FishPillText,
                    style = MaterialTheme.typography.titleSmall,
                    fontWeight = FontWeight.Black,
                )
            }
        }
    }
}

@Composable
private fun CampHeader(
    title: String,
    subtitle: String,
) {
    Column(
        modifier =
            Modifier
                .fillMaxWidth()
                .clip(RoundedCornerShape(22.dp))
                .background(GlassFill)
                .border(1.dp, GlassBorder, RoundedCornerShape(22.dp))
                .padding(18.dp),
        verticalArrangement = Arrangement.spacedBy(8.dp),
    ) {
        Text(
            text = title,
            color = Color.White,
            style = MaterialTheme.typography.titleLarge,
            fontWeight = FontWeight.ExtraBold,
        )
        Text(
            text = subtitle,
            color = Color(0xCCFFFFFF),
            style = MaterialTheme.typography.bodySmall,
            lineHeight = 20.sp,
        )
    }
}

@Composable
private fun CampLevelDots(
    currentLevel: Int,
    maxLevel: Int,
) {
    Row(horizontalArrangement = Arrangement.spacedBy(5.dp), verticalAlignment = Alignment.CenterVertically) {
        repeat(maxLevel) { idx ->
            val on = currentLevel > idx
            Box(
                modifier =
                    Modifier
                        .size(10.dp, 8.dp)
                        .clip(RoundedCornerShape(2.dp))
                        .background(if (on) AccentCyan else Color.White.copy(alpha = 0.12f))
                        .border(
                            width = if (on) 0.5.dp else 0.5.dp,
                            color = if (on) AccentCyan.copy(0.75f) else Color.White.copy(0.08f),
                            shape = RoundedCornerShape(2.dp),
                        ),
            )
        }
    }
}

@Composable
private fun CampUpgradeEntry(
    title: String,
    subtitle: String,
    description: String,
    icon: ImageVector,
    currentLabel: (Int) -> String,
    currentLevel: Int,
    upgradeCost: Int?,
    totalFish: Int,
    onUpgrade: () -> Unit,
) {
    val maxLv = 2
    val cost = upgradeCost
    val canUpgrade = currentLevel < maxLv && cost != null
    val requiredFish = cost ?: 0
    val canPay = totalFish >= requiredFish

    val buttonInteraction = remember { MutableInteractionSource() }
    val buttonPressed by buttonInteraction.collectIsPressedAsState()
    val buttonScale by animateFloatAsState(
        if (canUpgrade && canPay && buttonPressed) 0.97f else 1f,
        label = "camp_upgrade_scale",
    )

    Column(
        modifier =
            Modifier
                .fillMaxWidth()
                .clip(RoundedCornerShape(24.dp))
                .background(GlassFill)
                .border(1.dp, GlassBorder, RoundedCornerShape(24.dp))
                .padding(18.dp),
        verticalArrangement = Arrangement.spacedBy(12.dp),
    ) {
        Row(verticalAlignment = Alignment.CenterVertically) {
            Icon(
                imageVector = icon,
                contentDescription = null,
                tint = AccentCyan,
                modifier = Modifier.size(32.dp).padding(end = 12.dp),
            )
            Column(Modifier.weight(1f)) {
                Text(
                    text = title,
                    color = Color.White,
                    style = MaterialTheme.typography.titleMedium,
                    fontWeight = FontWeight.Bold,
                )
                Text(
                    text = subtitle,
                    color = Color(0x88FFFFFF),
                    style = MaterialTheme.typography.labelSmall,
                )
            }
            CampLevelDots(currentLevel, maxLv)
        }
        Text(
            text = description,
            color = Color(0xCCFFFFFF),
            style = MaterialTheme.typography.bodySmall,
            lineHeight = 20.sp,
        )
        Text(
            text = currentLabel(currentLevel),
            color = Color(0x55FFFFFF),
            style = MaterialTheme.typography.labelSmall,
            lineHeight = 16.sp,
        )
        if (canUpgrade) {
            Button(
                onClick = onUpgrade,
                enabled = canPay,
                modifier = Modifier.fillMaxWidth().height(50.dp).scale(buttonScale),
                shape = RoundedCornerShape(16.dp),
                interactionSource = buttonInteraction,
                colors =
                    ButtonDefaults.buttonColors(
                        containerColor = AccentCyan,
                        contentColor = Color(0xFF0B1420),
                        disabledContainerColor = Color(0xFF64FFDA).copy(alpha = 0.18f),
                        disabledContentColor = Color(0x66FFFFFF),
                    ),
            ) {
                Icon(Icons.Rounded.AutoAwesome, contentDescription = null, modifier = Modifier.size(18.dp))
                Spacer(Modifier.width(8.dp))
                Text("升级 $requiredFish 鱼干", fontWeight = FontWeight.ExtraBold)
            }
        } else {
            Text(
                text = "已满级",
                color = Color(0x99FFFFFF),
                style = MaterialTheme.typography.labelLarge,
                fontWeight = FontWeight.SemiBold,
                modifier = Modifier.fillMaxWidth().padding(vertical = 12.dp),
            )
        }
    }
}

private fun Float.format1(): String = "%.1f".format(this)
