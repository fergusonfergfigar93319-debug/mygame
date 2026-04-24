package com.example.mygame.game.score

import com.example.mygame.game.CoinKind
import com.example.mygame.game.modes.EndlessScoringConfig
import kotlin.math.max

/**
 * 极夜漂流积分：距离 + 收集 + 动作，再乘生存倍率。
 * 数值来自 [EndlessScoringConfig]，默认使用 [com.example.mygame.game.modes.EndlessBalanceConfig.scoring]。
 */
class EndlessScoreBook(
    private val cfg: EndlessScoringConfig,
) {

    var distanceScore: Int = 0
        private set
    var collectionScore: Int = 0
        private set
    var actionScore: Int = 0
        private set

    var lastDistanceSampleX: Float = -2_000f
        private set
    var survivalSeconds: Float = 0f
        private set

    var fishSnacksEaten: Int = 0
        private set
    var beacons: Int = 0
        private set
    var lorePages: Int = 0
        private set
    var stomps: Int = 0
        private set

    private var fishChainTimer: Float = 0f
    private var fishChain: Int = 0

    fun reset() {
        distanceScore = 0
        collectionScore = 0
        actionScore = 0
        lastDistanceSampleX = -2_000f
        survivalSeconds = 0f
        fishSnacksEaten = 0
        beacons = 0
        lorePages = 0
        stomps = 0
        fishChainTimer = 0f
        fishChain = 0
    }

    fun tickFrame(playerX: Float, dt: Float) {
        survivalSeconds += dt
        fishChainTimer = max(0f, fishChainTimer - dt)
        if (fishChainTimer <= 0f) fishChain = 0

        while (playerX - lastDistanceSampleX >= cfg.distanceSamplePx) {
            lastDistanceSampleX += cfg.distanceSamplePx
            distanceScore += cfg.distancePointsPerSample
        }
    }

    fun survivalMultiplier(): Float =
        (1f + (survivalSeconds / cfg.multiplierStepEverySeconds).toInt() * cfg.multiplierStep)
            .coerceAtMost(cfg.multiplierCap)

    fun baseSubtotal(): Int = distanceScore + collectionScore + actionScore

    fun totalScore(): Int = (baseSubtotal() * survivalMultiplier()).toInt()

    fun breakdown(): EndlessRunScoreBreakdown {
        val base = baseSubtotal()
        val mult = survivalMultiplier()
        val final = (base * mult).toInt()
        return EndlessRunScoreBreakdown(
            distanceScore = distanceScore,
            collectionScore = collectionScore,
            actionScore = actionScore,
            baseSubtotal = base,
            survivalMultiplier = mult,
            finalTotal = final,
            bonusFromMultiplier = final - base,
            survivalSeconds = survivalSeconds,
        )
    }

    fun onCoinPickup(kind: CoinKind) {
        when (kind) {
            CoinKind.Normal -> collectionScore += cfg.coinNormalPoints
            CoinKind.Beacon -> {
                collectionScore += cfg.coinBeaconPoints
                beacons += 1
            }
            CoinKind.LorePage -> {
                collectionScore += cfg.coinLorePoints
                lorePages += 1
            }
        }
    }

    fun onFishSnackEaten() {
        fishSnacksEaten += 1
        collectionScore += cfg.fishSnackCollectionPoints
        fishChain += 1
        fishChainTimer = cfg.fishChainTimerSeconds
        if (fishChain >= 2) {
            actionScore += cfg.fishChainBonusBase + (fishChain - 2) * cfg.fishChainBonusPerExtra
        }
    }

    fun onStompEnemy() {
        stomps += 1
        actionScore += cfg.stompActionPoints
    }

    fun onPerfectJumpChainBonus() {
        actionScore += cfg.perfectJumpChainBonus
    }

    fun onAssistUsed() {
        actionScore += cfg.assistActionPoints
    }
}
