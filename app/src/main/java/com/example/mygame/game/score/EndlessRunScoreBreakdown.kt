package com.example.mygame.game.score

/**
 * 单局结算分项，用于「得分明细」展示。
 */
data class EndlessRunScoreBreakdown(
    val distanceScore: Int,
    val collectionScore: Int,
    val actionScore: Int,
    val baseSubtotal: Int,
    val survivalMultiplier: Float,
    val finalTotal: Int,
    /** 倍率带来的增量：final - base（floor 后与整数展示一致） */
    val bonusFromMultiplier: Int,
    val survivalSeconds: Float,
    /**
     * 生存倍率当前生效档位（0 起）：与 `1 + tier * multiplierStep` 封顶后的公式一致。
     * 达到上限后档位不再增加，用于与 [survivalMultiplier] 对齐展示。
     */
    val multiplierTier: Int,
    /** 进入当前 [multiplierTier] 以来经过的存活秒数（封顶档后仍会随时间增大）。 */
    val secondsIntoTier: Float,
)
