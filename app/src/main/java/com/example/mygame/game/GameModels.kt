package com.example.mygame.game

data class Platform(
    val x: Float,
    val y: Float,
    val width: Float,
    val height: Float,
    /**
     * 地表摩擦系数：1 = 普通地面；数值越小越滑（冰面常用约 0.18–0.3）。
     * 仅作用于脚底站稳、无水平输入时的速度衰减。
     */
    val surfaceFriction: Float = 1f,
    /** 脆弱薄冰：站立后 [fragileTimeLeft] 倒计时，归零后碎裂。 */
    val isFragile: Boolean = false,
    /** 剩余可站立时间（秒），非脆弱或未站上去时为 `null`；离板重置。 */
    val fragileTimeLeft: Float? = null,
)

/** 脆弱薄冰自站定起至碎裂的时长（秒）。 */
const val FRAGILE_ICE_STAND_S = 0.45f

/** 结合地面 [surfaceFriction] 得到本帧水平阻尼系数（无输入时 v 乘该值）。 */
fun horizontalGroundDampening(baseFriction: Float, surfaceFriction: Float): Float {
    val s = surfaceFriction.coerceIn(0.05f, 1f)
    return 1f - (1f - baseFriction) * s
}

/**
 * 根据当前脚底接触面取摩擦系数；多面重叠时取最小值（最滑者生效）。
 */
fun standingSurfaceFriction(
    onGround: Boolean,
    playerX: Float,
    playerY: Float,
    playerSize: Float,
    groundY: Float,
    overPit: Boolean,
    platforms: List<Platform>,
    blocks: List<Block>,
): Float {
    if (!onGround) return 1f
    val bottom = playerY + playerSize
    val left = playerX
    val right = playerX + playerSize
    val eps = 8f
    val matches = ArrayList<Float>(4)
    if (!overPit && bottom >= groundY - eps && bottom <= groundY + eps * 2f) {
        matches += 1f
    }
    for (p in platforms) {
        if (right > p.x && left < p.x + p.width && bottom >= p.y - eps && bottom <= p.y + eps * 2f) {
            matches += p.surfaceFriction
        }
    }
    for (b in blocks) {
        val top = b.y - b.bounceOffset
        if (right > b.x && left < b.x + b.size && bottom >= top - eps && bottom <= top + eps * 2f) {
            matches += 1f
        }
    }
    return matches.minOrNull() ?: 1f
}

data class Pit(
    val startX: Float,
    val endX: Float
)

data class Enemy(
    val x: Float,
    val y: Float,
    val width: Float,
    val height: Float,
    val patrolStart: Float,
    val patrolEnd: Float,
    val speed: Float,
    val kind: EnemyKind = EnemyKind.Seal,
    val direction: Float = 1f,
    /**
     * 冰盾：通常踩踏只会弹开（轻顿帧）；仅在鱼干冲刺或「团团掩护」期间可踩碎，视为正常踩怪。
     */
    val hasIceShield: Boolean = false,
)

enum class EnemyKind {
    Seal,
    Bird,
    SpikedSeal,
    Owl,
    SnowMole,
}

fun EnemyKind.canBeStomped(): Boolean = this != EnemyKind.SpikedSeal

enum class CoinKind {
    Normal,
    Beacon,
    LorePage,
}

data class Coin(
    val x: Float,
    val y: Float,
    val size: Float,
    val kind: CoinKind = CoinKind.Normal,
)

enum class BlockType {
    Brick,
    Question
}

enum class BlockReward {
    Coin,
    Fish,
    Scarf,
    Shield,
    Boots,
    Magnet,
}

data class Block(
    val x: Float,
    val y: Float,
    val size: Float,
    val type: BlockType,
    val reward: BlockReward = BlockReward.Coin,
    val used: Boolean = false,
    val bounceOffset: Float = 0f
)

data class FloatingCoin(
    val x: Float,
    val y: Float,
    val size: Float,
    val velocityY: Float,
    val life: Float
)

data class FishSnack(
    val x: Float,
    val y: Float,
    val size: Float,
    val velocityX: Float,
    val velocityY: Float,
    val emerging: Boolean = true,
    val progress: Float = 0f
)

data class FriendGoal(
    val x: Float,
    val groundY: Float,
    val height: Float
)

// --- 高松鹅 Takamatsu Goose Boss ---

/**
 * 主线 Boss 战状态机；由同模块控制器类推进时间轴。
 * [SHIELDED] 期间 [BossEntity.hasShield] 为 true，需鱼干冲刺或团团掩护时重踩才破盾。
 */
enum class BossState {
    /** 进场，镜头已锁。 */
    INTRO,
    /** 跳跃压迫 + 落地冲击波。 */
    JUMPING,
    /** 开冰盾，可刷小怪；破盾后由控制器切入 [STUNNED]。 */
    SHIELDED,
    /** 被破盾后的硬直。 */
    STUNNED,
    /** 全平台变脆弱薄冰，Boss 更激进。 */
    ENRAGED,
    /** 演出。 */
    DYING,
}

/** Boss 受击/破盾时白闪峰值时长（秒），绘制用 [BossEntity.damageFlashTimer] 归一化。 */
const val BOSS_DAMAGE_FLASH_MAX_S = 0.18f

data class BossEntity(
    var x: Float,
    var y: Float,
    val width: Float,
    val height: Float,
    var hp: Float = 100f,
    val maxHp: Float = 100f,
    var state: BossState = BossState.INTRO,
    /** 状态内已流逝时间（秒），供控制器做分段逻辑。 */
    var stateTime: Float = 0f,
    var hasShield: Boolean = false,
    /** 大于 0 时在身体上叠白色闪屏。 */
    var damageFlashTimer: Float = 0f,
)

/**
 * 地面外扩的「冰刺波」判定位；半宽每帧外扩，与玩家脚底相交则判定命中。
 */
data class ImpactWave(
    val centerX: Float,
    var halfWidth: Float,
    val surfaceY: Float,
    var life: Float,
    val maxLife: Float = 0.5f,
    val growSpeed: Float = 520f,
)

/**
 * 在 [LevelContent] 中挂接；存在时 [GuguGagaGame] 在达到 [triggerAtPlayerX] 后切 Boss 战与锁镜头。
 */
data class BossArenaSpec(
    val triggerAtPlayerX: Float,
    val arenaCenterWorldX: Float,
)
