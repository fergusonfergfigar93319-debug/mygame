package com.example.mygame.ui.home

import androidx.compose.foundation.background
import androidx.compose.foundation.border
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.Spacer
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.size
import androidx.compose.foundation.lazy.LazyColumn
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.rounded.AutoAwesome
import androidx.compose.material.icons.rounded.Explore
import androidx.compose.material.icons.rounded.Favorite
import androidx.compose.material.icons.rounded.Radar
import androidx.compose.material.icons.rounded.Speed
import androidx.compose.material3.Button
import androidx.compose.material3.ButtonDefaults
import androidx.compose.material3.Icon
import androidx.compose.material3.LinearProgressIndicator
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.runtime.LaunchedEffect
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableIntStateOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.setValue
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.draw.clip
import androidx.compose.ui.graphics.Brush
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.graphics.vector.ImageVector
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import com.example.mygame.data.CampUpgradeKind
import com.example.mygame.data.SaveRepository
import com.example.mygame.game.modes.EndlessBalanceConfig
import com.example.mygame.ui.common.GameBackRow
import com.example.mygame.ui.common.GameHeroCard
import com.example.mygame.ui.common.GameInfoPill
import com.example.mygame.ui.common.GameScreenBackground

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

    GameScreenBackground {
        GameBackRow(
            title = "补给营地",
            onBack = onBack,
            trailing = { GameInfoPill(label = "鱼干 $totalFish") },
        )
        GameHeroCard(
            title = "雪线边的临时小屋",
            subtitle = "把每一局带回来的鱼干换成更顺手的能力。营地升级只做轻量强化，让操作、路线和判断仍然是胜负核心。",
        )
        LazyColumn(
            verticalArrangement = Arrangement.spacedBy(12.dp),
            modifier = Modifier.fillMaxWidth(),
        ) {
            item {
                CampUpgradeCard(
                    title = "冲刺强化",
                    subtitle = "鱼干冲刺",
                    description = "拾取鱼干后延长冲刺状态，更容易覆盖长裂隙、盾敌和 Boss 破盾窗口。",
                    icon = Icons.Rounded.Speed,
                    currentLabel = { lv ->
                        val cur = 7f + lv
                        if (lv >= 2) "冲刺持续 ${cur.format1()} 秒，已达上限" else "当前 ${cur.format1()} 秒；下一档 ${(cur + 1f).format1()} 秒"
                    },
                    currentLevel = dashLv,
                    upgradeCost = saveRepository.getCampUpgradeCost(CampUpgradeKind.Dash, dashLv),
                    totalFish = totalFish,
                    onUpgrade = { if (saveRepository.tryPurchaseCampUpgrade(CampUpgradeKind.Dash)) refresh++ },
                )
            }
            item {
                CampUpgradeCard(
                    title = "团团好感",
                    subtitle = "雪球支援",
                    description = "团团支援持续更久，适合通过混合敌人路段、薄冰平台和 Boss 护盾阶段。",
                    icon = Icons.Rounded.Favorite,
                    currentLabel = { lv ->
                        val cur = 4.5f + lv * 0.55f
                        val next = 4.5f + (lv + 1) * 0.55f
                        if (lv >= 2) "掩护持续 ${cur.format1()} 秒，已达上限" else "当前 ${cur.format1()} 秒；下一档 ${next.format1()} 秒"
                    },
                    currentLevel = tuanLv,
                    upgradeCost = saveRepository.getCampUpgradeCost(CampUpgradeKind.Tuan, tuanLv),
                    totalFish = totalFish,
                    onUpgrade = { if (saveRepository.tryPurchaseCampUpgrade(CampUpgradeKind.Tuan)) refresh++ },
                )
            }
            item {
                val base = EndlessBalanceConfig.rewardSpacingWorldWidthMultiplier
                CampUpgradeCard(
                    title = "极地直觉",
                    subtitle = "补给航道",
                    description = "无尽模式中更容易插入补给休整段，给玩家短暂喘息和追分窗口。",
                    icon = Icons.Rounded.Explore,
                    currentLabel = { lv ->
                        val cur = base * (1f - 0.1f * lv)
                        val next = base * (1f - 0.1f * (lv + 1))
                        if (lv >= 2) "间隔系数 ${cur.format1()}，已达上限" else "当前间隔系数 ${cur.format1()}；下一档 ${next.format1()}"
                    },
                    currentLevel = polarLv,
                    upgradeCost = saveRepository.getCampUpgradeCost(CampUpgradeKind.Polar, polarLv),
                    totalFish = totalFish,
                    onUpgrade = { if (saveRepository.tryPurchaseCampUpgrade(CampUpgradeKind.Polar)) refresh++ },
                )
            }
            item {
                CampUpgradeCard(
                    title = "磁针调校",
                    subtitle = "极光磁针",
                    description = "极光磁针吸附时间更长，让高风险金币线变成更值得尝试的路线选择。",
                    icon = Icons.Rounded.Radar,
                    currentLabel = { lv ->
                        val cur = 10f + lv * 1.5f
                        val next = 10f + (lv + 1) * 1.5f
                        if (lv >= 2) "吸附持续 ${cur.format1()} 秒，已达上限" else "当前 ${cur.format1()} 秒；下一档 ${next.format1()} 秒"
                    },
                    currentLevel = magnetLv,
                    upgradeCost = saveRepository.getCampUpgradeCost(CampUpgradeKind.Magnet, magnetLv),
                    totalFish = totalFish,
                    onUpgrade = { if (saveRepository.tryPurchaseCampUpgrade(CampUpgradeKind.Magnet)) refresh++ },
                )
            }
            item { Spacer(modifier = Modifier.height(8.dp)) }
        }
    }
}

private fun Float.format1(): String = "%.1f".format(this)

@Composable
private fun CampUpgradeCard(
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
    val cost = upgradeCost
    val canUpgrade = currentLevel < 2 && cost != null
    Column(
        modifier =
            Modifier
                .fillMaxWidth()
                .clip(RoundedCornerShape(22.dp))
                .background(
                    Brush.linearGradient(
                        listOf(Color(0xF7F8FCFF), Color(0xE9DDF3FB)),
                    ),
                )
                .border(1.dp, Color.White.copy(alpha = 0.9f), RoundedCornerShape(22.dp))
                .padding(16.dp),
        verticalArrangement = Arrangement.spacedBy(10.dp),
    ) {
        Row(verticalAlignment = Alignment.CenterVertically) {
            Icon(icon, contentDescription = null, tint = Color(0xFF1E5A7A), modifier = Modifier.padding(end = 10.dp))
            Column(Modifier.weight(1f)) {
                Text(title, style = MaterialTheme.typography.titleMedium, fontWeight = FontWeight.Black, color = Color(0xFF153246))
                Text(subtitle, style = MaterialTheme.typography.labelMedium, color = Color(0xFF3D6E8A))
            }
            GameInfoPill(label = "Lv $currentLevel / 2", active = currentLevel > 0)
        }
        Text(description, style = MaterialTheme.typography.bodySmall, lineHeight = 18.sp, color = Color(0xFF4A6175))
        LinearProgressIndicator(
            progress = { currentLevel / 2f },
            modifier = Modifier.fillMaxWidth().height(6.dp).clip(RoundedCornerShape(999.dp)),
            color = Color(0xFF1AAE8C),
            trackColor = Color(0xFFE1EDF4),
        )
        Text(currentLabel(currentLevel), style = MaterialTheme.typography.labelSmall, color = Color(0xFF5A7288))
        if (canUpgrade) {
            val requiredFish = cost ?: 0
            val canPay = totalFish >= requiredFish
            Button(
                onClick = onUpgrade,
                enabled = canPay,
                modifier = Modifier.fillMaxWidth(),
                shape = RoundedCornerShape(14.dp),
                colors =
                    ButtonDefaults.buttonColors(
                        containerColor = Color(0xFF225F86),
                        contentColor = Color(0xFFF5FAFF),
                        disabledContainerColor = Color(0xFFC9D7E2),
                    ),
            ) {
                Icon(Icons.Rounded.AutoAwesome, contentDescription = null, modifier = Modifier.size(18.dp).padding(end = 4.dp))
                Text("升级：$requiredFish 鱼干", fontWeight = FontWeight.Bold)
            }
        } else {
            Text("已满级", style = MaterialTheme.typography.labelLarge, fontWeight = FontWeight.SemiBold, color = Color(0xFF2E6B4A))
        }
    }
}
