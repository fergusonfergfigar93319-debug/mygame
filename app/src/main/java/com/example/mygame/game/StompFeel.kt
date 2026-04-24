package com.example.mygame.game

/** 踩怪顿帧、震屏参数；仅美术/手感常数。 */
object StompFeel {
    /** 顿帧时长（秒），约 2.5～4 个 16ms 步。 */
    const val HIT_STOP_S = 0.055f
    /** 震屏总时长，用于与 [shakeTimer] 初始值一致。 */
    const val SHAKE_DURATION_S = 0.12f
    /** 最大屏幕偏移（像素，渲染层）。 */
    const val SHAKE_MAX_PX = 18f
}
