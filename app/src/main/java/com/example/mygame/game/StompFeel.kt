package com.example.mygame.game

import kotlin.random.Random

/**
 * 踩怪与受击的手感常数：重踩踏 **3H**（顿帧 + 长振动马达 + 重震）；
 * **动量型**（冲刺穿怪、重落地）**无顿帧**，仅震幅 + 轻触觉，并用 [ShakeBias] 在若干帧内偏向 X/Y 采样。
 * 主循环中 `shakeAmplitude` 每帧衰减；[randomShakeOffsetPx] 系列在 Canvas translate 前采样偏移。
 */
object StompFeel {

    /** 冲刺穿怪、重落地时震屏取样的轴偏好，持续 [SHAKE_MOMENTUM_BIAS_FRAMES] 帧。 */
    enum class ShakeBias {
        None,
        /** 水平为主（鱼干冲刺穿怪）。 */
        Horizontal,
        /** 竖直为主（重落地）。 */
        Vertical,
    }
    /** 重踩踏顿帧（秒），约 3.75 帧 @16ms。 */
    const val HIT_STOP_S = 0.06f

    /** 护盾挡伤等轻顿帧。 */
    const val HIT_STOP_LIGHT_S = 0.042f

    /** 重踩踏时震屏初始振幅（逻辑像素，作用于 Canvas translate）。 */
    const val SHAKE_MAX_PX = 24f

    /** 轻受击/护盾等次级震幅上限。 */
    const val SHAKE_LIGHT_PX = 9f

    /**
     * 动量型反馈：鱼干冲刺穿怪（无顿帧、水平震屏为主；触觉为短促的 TextHandleMove）。
     */
    const val SHAKE_DASH_PX = 12f

    /**
     * 动量型反馈：重落地（无顿帧、竖直震屏为主，配合轻触觉）。
     */
    const val SHAKE_HARD_LAND_PX = 11f

    // --- 高松鹅 / Boss 战放大规格 ---

    /** P1 落地冲击波一帧的顿帧（与正弦相位的砸地同帧发出）。 */
    const val BOSS_LAND_HIT_STOP_S = 0.08f

    /** 破盾瞬间的夸张顿帧，配合碎盾粒子。 */
    const val BOSS_SHIELD_BREAK_HIT_STOP_S = 0.12f

    /** 击败演出首帧的顿帧；完整慢动作可另接时间缩放。 */
    const val BOSS_DEATH_HIT_STOP_S = 0.25f

    /** 仅竖直轴采样、凸显吨位的 Boss 震屏幅度。 */
    const val SHAKE_BOSS_LANDING_PX = 36f

    /**
     * 落地前竖直速度大于等于此值（向下为正）视为「重落地」。
     */
    const val HARD_LAND_IMPACT_VY = 520f

    /**
     * 动量型震屏在随机偏移时偏向水平/竖直的持续帧数。
     */
    const val SHAKE_MOMENTUM_BIAS_FRAMES = 10

    /** 每帧（~16ms）震幅衰减，模拟快速阻尼。 */
    const val SHAKE_DECAY_PER_FRAME = 0.75f

    /** 低于此振幅则清零，避免无限尾迹。 */
    const val SHAKE_CUTOFF_PX = 0.5f

    fun randomShakeOffsetPx(
        amplitude: Float,
        random: Random = Random.Default,
    ): Pair<Float, Float> {
        val ax = (random.nextFloat() - 0.5f) * 2f * amplitude
        val ay = (random.nextFloat() - 0.5f) * 2f * amplitude
        return ax to ay
    }

    fun randomShakeOffsetHorizontalDominant(
        amplitude: Float,
        random: Random = Random.Default,
    ): Pair<Float, Float> {
        val ax = (random.nextFloat() - 0.5f) * 2f * amplitude
        val ay = (random.nextFloat() - 0.5f) * 2f * (amplitude * 0.35f)
        return ax to ay
    }

    fun randomShakeOffsetVerticalDominant(
        amplitude: Float,
        random: Random = Random.Default,
    ): Pair<Float, Float> {
        val ax = (random.nextFloat() - 0.5f) * 2f * (amplitude * 0.35f)
        val ay = (random.nextFloat() - 0.5f) * 2f * amplitude
        return ax to ay
    }
}
