package com.example.mygame.game.modes

/**
 * 极夜漂流积分参数（由 [EndlessBalanceConfig.scoring] 持有默认实例）。
 */
data class EndlessScoringConfig(
    val distanceSamplePx: Float = 36f,
    val distancePointsPerSample: Int = 3,
    val multiplierStepEverySeconds: Float = 20f,
    val multiplierStep: Float = 0.25f,
    val multiplierCap: Float = 3f,
    val coinNormalPoints: Int = 12,
    val coinBeaconPoints: Int = 85,
    val coinLorePoints: Int = 120,
    val fishSnackCollectionPoints: Int = 50,
    val fishChainTimerSeconds: Float = 10f,
    val fishChainBonusBase: Int = 10,
    val fishChainBonusPerExtra: Int = 5,
    val stompActionPoints: Int = 25,
    val perfectJumpChainBonus: Int = 35,
    val assistActionPoints: Int = 40,
)

/**
 * 极夜漂流可调参数集中配置，便于平衡迭代而少改逻辑代码。
 */
object EndlessBalanceConfig {

    // --- 难度时段（秒）---
    const val tier0EndsAtSeconds: Float = 30f
    const val tier1EndsAtSeconds: Float = 90f

    // --- 世界拼接 / 裁剪 ---
    /** 玩家前方保持约 `worldWidth * 此值` 的已生成路面 */
    const val horizonAheadWidthMultiplier: Float = 2.4f
    /** 单帧最多追加片段数，防止异常卡死 */
    const val maxSegmentsPerEnsure: Int = 12
    /** 玩家身后超过此距离（px）的实体被裁剪 */
    const val pruneDistanceBehindPlayer: Float = 1_000f
    /** 鱼干道具在尾部外可反弹的额外前移量 */
    const val fishBounceExtraAhead: Float = 400f
    /** 玩家相对尾缘留空（系数 * 玩家宽） */
    const val playerTailMarginFactor: Float = 0.2f

    // --- 补给强制插入（路程）---
    /** 超过约 `worldWidth * 此值` 未经过补给段则下一段强制安全补给 */
    const val rewardSpacingWorldWidthMultiplier: Float = 3.8f

    // --- 片段随机权重（0..99 的 roll，上界为累计阈值）---
    object Tier0Roll {
        const val flatChaseWideUntil: Int = 40
        const val pitJumpNarrowUntil: Int = 70
    }

    object Tier1Roll {
        const val pitJumpUntil: Int = 25
        const val thinIceUntil: Int = 45
        const val blizzardUntil: Int = 65
        const val dangerUntil: Int = 85
    }

    object Tier2Roll {
        const val pitJumpUntil: Int = 22
        const val branchUntil: Int = 38
        const val blizzardUntil: Int = 55
        const val dangerUntil: Int = 75
        const val thinIceUntil: Int = 88
    }

    // --- 环境与手感修正（与片段几何绑定）---
    const val thinIceSpeedMultiplier: Float = 1.14f
    const val blizzardIntensityLight: Float = 0.28f
    const val blizzardIntensityDense: Float = 0.42f
    /** 每 1.0 风雪强度对跑动速度的乘性衰减系数（在 EndlessMode 中与强度相乘后从 1 减去） */
    const val blizzardRunSpeedPenaltyPerIntensity: Float = 0.12f

    /** 积分曲线（与 [com.example.mygame.game.score.EndlessScoreBook] 共用） */
    val scoring: EndlessScoringConfig = EndlessScoringConfig()
}
