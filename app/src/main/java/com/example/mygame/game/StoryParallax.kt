package com.example.mygame.game

/**
 * 主线舞台背景视差系数，均相对世界摄像机 [com.example.mygame.game.GuguGagaGame] 中的 `cameraX` 位移。
 * `1f` 表示与地形/角色同速；越小越远。仅影响装饰层，地面与玩法实体仍用完整 `cameraX`。
 */
object StoryParallax {
    /** 飘雪 / 氛围粒子，略慢于场景。 */
    const val SNOWFLAKE = 0.08f

    /** 日/月与光晕，几乎不动。 */
    const val SUN = 0.04f

    /** 云层。 */
    const val CLOUD = 0.15f

    /** 远景淡色山脊剪影。 */
    const val RIDGE_FAR = 0.22f

    /** 中景雪坡 + 塔状块。 */
    const val HILL = 0.40f

    /** 中远景第二层淡色山脊。 */
    const val RIDGE_MID = 0.30f

    /** 中近景林线剪影。 */
    const val FOREST = 0.48f

    /** 近景倒塌小屋。 */
    const val HUT = 0.55f

    /** 无尽：远景星点。 */
    const val ENDLESS_STAR = 0.12f

    /** 无尽：极光帶。 */
    const val AURORA = 0.06f

    // --- 设备重力视差（主线 [drawStorySceneBackdrop]）：远、中、近三层相对 [rememberDeviceTilt] 最大像素比例 ---

    /** 天穹 / 日轮。 */
    const val TILT_LAYER_FAR = 0.2f

    /** 云、脊线、林线、山丘、废屋、地平线雾。 */
    const val TILT_LAYER_MID = 0.6f

    /** 近景飘雪与 `SceneDecor` 前景雾片。 */
    const val TILT_LAYER_FORE = 1.4f

    /** 天空渐变色扩绘，防倾斜时露边。 */
    const val TILT_SKY_OVERSCAN_PX = 100f
}

/** 用于无限横向平铺的装饰层：在视口内稳定取模。 */
internal fun scrollTileX(
    baseX: Float,
    cameraX: Float,
    factor: Float,
    period: Float,
): Float {
    val shift = baseX - cameraX * factor
    val p = period.coerceAtLeast(1f)
    var t = shift % p
    if (t < 0f) t += p
    return t
}
