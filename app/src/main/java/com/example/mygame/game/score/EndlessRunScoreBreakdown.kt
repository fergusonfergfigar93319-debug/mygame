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
)
