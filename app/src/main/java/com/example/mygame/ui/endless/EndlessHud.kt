package com.example.mygame.ui.endless

import androidx.compose.animation.animateColorAsState
import androidx.compose.animation.core.animateFloatAsState
import androidx.compose.animation.core.tween
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.Spacer
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.width
import androidx.compose.foundation.shape.CircleShape
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.filled.AutoStories
import androidx.compose.material.icons.filled.EmojiEvents
import androidx.compose.material.icons.filled.Flag
import androidx.compose.material.icons.filled.Navigation
import androidx.compose.material.icons.filled.SetMeal
import androidx.compose.material.icons.filled.Timer
import androidx.compose.material3.LinearProgressIndicator
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.runtime.getValue
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.draw.clip
import androidx.compose.ui.draw.scale
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import com.example.mygame.game.level.EndlessSegmentKind
import com.example.mygame.game.modes.EndlessBalanceConfig
import com.example.mygame.ui.common.FloatingHudIconResource
import com.example.mygame.ui.common.FloatingHudIconStat
import com.example.mygame.ui.common.FloatingHudStatusLabel
import com.example.mygame.ui.common.floatingHudTextShadow
import kotlin.math.ceil

@Composable
fun EndlessHud(
    totalScore: Int,
    distanceUnits: Float,
    survivalSeconds: Float,
    multiplier: Float,
    fishSnacks: Int,
    beacons: Int,
    lorePages: Int,
    distanceScore: Int,
    collectionScore: Int,
    actionScore: Int,
    fishDashActive: Boolean,
    hasScarf: Boolean,
    snowShieldActive: Boolean,
    gustBootsActive: Boolean,
    slideFlowActive: Boolean,
    magnetActive: Boolean,
    assistReady: Boolean,
    assistTimer: Float,
    currentSegmentKind: EndlessSegmentKind?,
    modifier: Modifier = Modifier,
) {
    val scoring = EndlessBalanceConfig.scoring
    val stepSeconds = scoring.multiplierStepEverySeconds.coerceAtLeast(1f)
    val tierElapsed = survivalSeconds % stepSeconds
    val tierProgress = (tierElapsed / stepSeconds).coerceIn(0f, 1f)
    val multiplierCapped = multiplier >= scoring.multiplierCap
    val nextTierSeconds = ceil((stepSeconds - tierElapsed).coerceIn(0f, stepSeconds)).toInt()

    val iconAccent = Color(0xFF90CAF9)
    val resTint = Color(0xFF80CBC4)
    val isHighValue = multiplier > 1.0f
    val multColor by animateColorAsState(
        targetValue = if (isHighValue) Color(0xFFFFD700) else Color.White,
        animationSpec = tween(durationMillis = 300),
        label = "multColor",
    )
    val multScale by animateFloatAsState(
        targetValue = if (isHighValue) 1.1f else 1.0f,
        animationSpec = tween(durationMillis = 300),
        label = "multScale",
    )
    val statusLineActive =
        fishDashActive ||
            hasScarf ||
            snowShieldActive ||
            gustBootsActive ||
            slideFlowActive ||
            magnetActive ||
            assistTimer > 0f ||
            (assistReady && assistTimer <= 0f)

    Column(
        modifier =
            modifier
                .fillMaxWidth()
                .padding(horizontal = 16.dp, vertical = 12.dp),
        verticalArrangement = Arrangement.spacedBy(10.dp),
    ) {
        Row(
            modifier = Modifier.fillMaxWidth(),
            horizontalArrangement = Arrangement.SpaceBetween,
            verticalAlignment = Alignment.Top,
        ) {
            Row(
                horizontalArrangement = Arrangement.spacedBy(10.dp),
                verticalAlignment = Alignment.CenterVertically,
            ) {
                FloatingHudIconStat(
                    icon = Icons.Filled.EmojiEvents,
                    label = "总分",
                    value = totalScore.toString(),
                    iconTint = iconAccent,
                )
                FloatingHudIconStat(
                    icon = Icons.Filled.Navigation,
                    label = "路程",
                    value = String.format("%.0f", distanceUnits),
                    iconTint = iconAccent,
                )
                FloatingHudIconStat(
                    icon = Icons.Filled.Timer,
                    label = "存活",
                    value = "${survivalSeconds.toInt()}s",
                    iconTint = iconAccent,
                )
            }
            Column(horizontalAlignment = Alignment.End, modifier = Modifier.scale(multScale)) {
                Text(
                    text = "倍率",
                    style =
                        MaterialTheme.typography.labelSmall.copy(
                            shadow = floatingHudTextShadow(),
                        ),
                    color = Color(0xCCFFFFFF),
                )
                Text(
                    text = String.format("%.2fx", multiplier),
                    style =
                        MaterialTheme.typography.titleMedium.copy(
                            shadow = floatingHudTextShadow(),
                        ),
                    color = multColor,
                    fontWeight = FontWeight.ExtraBold,
                )
            }
        }
        Row(
            modifier = Modifier.fillMaxWidth(),
            verticalAlignment = Alignment.CenterVertically,
            horizontalArrangement = Arrangement.spacedBy(12.dp),
        ) {
            Row(
                horizontalArrangement = Arrangement.spacedBy(12.dp),
                verticalAlignment = Alignment.CenterVertically,
            ) {
                FloatingHudIconResource(
                    icon = Icons.Filled.SetMeal,
                    value = fishSnacks.toString(),
                    iconTint = resTint,
                )
                FloatingHudIconResource(
                    icon = Icons.Filled.Flag,
                    value = beacons.toString(),
                    iconTint = resTint,
                )
                FloatingHudIconResource(
                    icon = Icons.Filled.AutoStories,
                    value = lorePages.toString(),
                    iconTint = resTint,
                )
            }
            Spacer(modifier = Modifier.weight(1f))
            Text(
                text = "距离 $distanceScore · 收集 $collectionScore · 动作 $actionScore",
                style =
                    MaterialTheme.typography.labelSmall.copy(
                        shadow = floatingHudTextShadow(),
                    ),
                color = Color(0xCCFFFFFF),
            )
        }
        Row(
            modifier = Modifier.fillMaxWidth(),
            verticalAlignment = Alignment.CenterVertically,
            horizontalArrangement = Arrangement.spacedBy(10.dp),
        ) {
            LinearProgressIndicator(
                progress = { if (multiplierCapped) 1f else tierProgress },
                modifier = Modifier.weight(1f).height(4.dp).clip(CircleShape),
                color = Color(0xCC64FFDA),
                trackColor = Color(0x33FFFFFF),
            )
            Text(
                text = if (multiplierCapped) "倍率已封顶" else "下档 ${nextTierSeconds.coerceAtLeast(1)}s",
                style =
                    MaterialTheme.typography.labelSmall.copy(
                        shadow = floatingHudTextShadow(),
                    ),
                color = Color(0xDDEFFFFF),
            )
        }
        currentSegmentKind?.let { kind ->
            Text(
                text = segmentLabel(kind),
                style =
                    MaterialTheme.typography.labelSmall.copy(
                        shadow = floatingHudTextShadow(),
                    ),
                color = Color(0xE8F5F5F5),
            )
        }
        Row(
            modifier = Modifier.fillMaxWidth(),
            horizontalArrangement = Arrangement.spacedBy(10.dp),
        ) {
            FloatingHudStatusLabel("鱼干冲刺", fishDashActive)
            FloatingHudStatusLabel("泡泡围巾", hasScarf)
            FloatingHudStatusLabel("雪壳护盾", snowShieldActive)
            FloatingHudStatusLabel("风种长跳", gustBootsActive)
            FloatingHudStatusLabel("滑铲顺风", slideFlowActive)
            FloatingHudStatusLabel("磁针吸附", magnetActive)
            FloatingHudStatusLabel("团团掩护", assistTimer > 0f)
            FloatingHudStatusLabel("团团就绪", assistReady && assistTimer <= 0f)
        }
        if (!statusLineActive) {
            Text(
                text = "注意裂谷、薄冰与风雪段；补给航道是拉开倍率的好机会。",
                style =
                    MaterialTheme.typography.labelSmall.copy(
                        shadow = floatingHudTextShadow(),
                    ),
                color = Color(0xCCFFFFFF),
                lineHeight = 16.sp,
                maxLines = 2,
            )
        }
    }
}

private fun segmentLabel(kind: EndlessSegmentKind): String =
    when (kind) {
        EndlessSegmentKind.FlatChase -> "航道：平地追逐"
        EndlessSegmentKind.PitJump -> "航道：裂谷跳跃"
        EndlessSegmentKind.ThinIceGlide -> "航道：薄冰滑行"
        EndlessSegmentKind.BlizzardLowVis -> "航道：风雪低能见"
        EndlessSegmentKind.RewardSafe -> "航道：补给整备"
        EndlessSegmentKind.DangerMixed -> "航道：混合危险"
        EndlessSegmentKind.BranchChoice -> "航道：双线险路"
    }
