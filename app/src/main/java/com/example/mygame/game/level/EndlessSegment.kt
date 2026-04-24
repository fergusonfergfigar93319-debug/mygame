package com.example.mygame.game.level

import com.example.mygame.game.Block
import com.example.mygame.game.Coin
import com.example.mygame.game.Enemy
import com.example.mygame.game.Pit
import com.example.mygame.game.Platform

fun SegmentGeometry.offsetWorldX(baseX: Float): SegmentGeometry = copy(
    pits = pits.map { Pit(it.startX + baseX, it.endX + baseX) },
    platforms = platforms.map { p -> p.copy(x = p.x + baseX) },
    enemies = enemies.map { e ->
        e.copy(
            x = e.x + baseX,
            patrolStart = e.patrolStart + baseX,
            patrolEnd = e.patrolEnd + baseX,
        )
    },
    coins = coins.map { c -> c.copy(x = c.x + baseX) },
    blocks = blocks.map { b -> b.copy(x = b.x + baseX) },
)

/** 极夜漂流 — 拼接片段类型（用于难度与表现区分）。 */
enum class EndlessSegmentKind {
    FlatChase,
    PitJump,
    ThinIceGlide,
    BlizzardLowVis,
    RewardSafe,
    DangerMixed,
    BranchChoice,
}

/**
 * 一段已实例化在世界坐标中的几何体。
 * [width] 为本段在 X 轴占用长度，下一段从 baseX + width 开始拼接。
 */
data class SegmentGeometry(
    val width: Float,
    val kind: EndlessSegmentKind,
    val pits: List<Pit>,
    val platforms: List<Platform>,
    val enemies: List<Enemy>,
    val coins: List<Coin>,
    val blocks: List<Block>,
    val speedMultiplier: Float = 1f,
    /** 0..1，风雪低能见遮罩强度 */
    val blizzardIntensity: Float = 0f,
)

/** 记录玩家所处区间，用于速度与风雪叠加 */
data class EndlessSegmentSpan(
    val startX: Float,
    val endX: Float,
    val kind: EndlessSegmentKind,
    val speedMultiplier: Float,
    val blizzardIntensity: Float,
)
