package com.example.mygame.game.level

import androidx.compose.ui.graphics.Color
import com.example.mygame.game.Block
import com.example.mygame.game.BlockReward
import com.example.mygame.game.BlockType
import com.example.mygame.game.Coin
import com.example.mygame.game.Enemy
import com.example.mygame.game.EnemyKind
import com.example.mygame.game.FriendGoal
import com.example.mygame.game.Pit
import com.example.mygame.game.Platform
import kotlin.math.min

/**
 * 第二关「冰湖回音谷」——当前为可玩占位数据：更短路程、冰湖配色与精简敌人，
 * 便于后续替换为正式关卡配置与机关占位。
 */
object LevelTwoData {

    fun build(worldWidth: Float, worldHeight: Float): LevelContent {
        val groundY = worldHeight * 0.82f
        val tile = worldWidth * 0.12f
        val hero = min(worldWidth, worldHeight) * 0.1f
        val levelLength = worldWidth * 3.85f

        val pits = listOf(
            Pit(startX = tile * 4.2f, endX = tile * 5.05f)
        )

        val platforms = listOf(
            Platform(x = tile * 2.4f, y = groundY - worldHeight * 0.22f, width = tile * 1.35f, height = 24f),
            Platform(x = tile * 6.2f, y = groundY - worldHeight * 0.32f, width = tile * 1.5f, height = 24f),
            Platform(x = tile * 8.5f, y = groundY - worldHeight * 0.2f, width = tile * 1.2f, height = 24f)
        )

        val blockSize = tile * 0.48f
        val blockRowY = groundY - worldHeight * 0.24f
        val blocks = listOf(
            Block(x = tile * 1.5f, y = blockRowY, size = blockSize, type = BlockType.Question, reward = BlockReward.Coin),
            Block(x = tile * 1.95f, y = blockRowY, size = blockSize, type = BlockType.Brick),
            Block(x = tile * 5.9f, y = groundY - worldHeight * 0.38f, size = blockSize, type = BlockType.Question, reward = BlockReward.Fish),
            Block(x = tile * 9.6f, y = groundY - worldHeight * 0.34f, size = blockSize, type = BlockType.Question, reward = BlockReward.Scarf)
        )

        val enemies = listOf(
            Enemy(
                x = tile * 3.4f,
                y = groundY - hero * 0.72f,
                width = hero * 0.78f,
                height = hero * 0.72f,
                patrolStart = tile * 2.8f,
                patrolEnd = tile * 5.8f,
                speed = 110f,
                kind = EnemyKind.Seal
            ),
            Enemy(
                x = tile * 7.8f,
                y = groundY - hero * 1.28f,
                width = hero * 0.68f,
                height = hero * 0.52f,
                patrolStart = tile * 6.9f,
                patrolEnd = tile * 9.2f,
                speed = 124f,
                kind = EnemyKind.Bird
            )
        )

        val coins = listOf(
            Coin(x = tile * 2.0f, y = groundY - worldHeight * 0.12f, size = hero * 0.42f),
            Coin(x = tile * 5.5f, y = groundY - worldHeight * 0.42f, size = hero * 0.42f),
            Coin(x = tile * 8.9f, y = groundY - worldHeight * 0.26f, size = hero * 0.42f)
        )

        val friendGoal = FriendGoal(
            x = levelLength - worldWidth * 0.3f,
            groundY = groundY,
            height = worldHeight * 0.35f
        )

        val presentation = LevelPresentation(
            introTitle = "咕咕嘎嘎 冰湖回音谷（占位）",
            introDescription = "湖面倒映着风声，对岸有微弱的回音。\n这是第二关的可玩草稿：更短、更冷的试炼路线，后续会在此接入薄冰、风向与机关占位。",
            failHint = "利用平台越过冰隙，鱼干冲刺可以帮助穿过巡逻更紧的海豹路线。",
            victoryTitle = "穿过冰湖试炼",
            victoryDescription = "你们抵达了对岸的回声浅滩。正式版将在这里接入新伙伴线索与解谜机关。\n当前占位关卡可重复挑战，用于验证流程与数值。",
            chapterPreviewTitle = null,
            chapterPreviewDescription = null,
            hudGoalLine = "目标：穿越冰湖回音谷，抵达对岸回声浅滩"
        )

        return LevelContent(
            levelLength = levelLength,
            pits = pits,
            platforms = platforms,
            blocks = blocks,
            enemies = enemies,
            coins = coins,
            friendGoal = friendGoal,
            presentation = presentation,
            outerGradientColors = listOf(
                Color(0xFF3D6FA8),
                Color(0xFF6B9BD4),
                Color(0xFFB8D4EA),
                Color(0xFFE8F2FA)
            ),
            stageSkyColors = listOf(
                Color(0xFF4A7BA7),
                Color(0xFF7BA3C9),
                Color(0xFFB0CDE4)
            ),
            pitWaterColor = Color(0xFF2C5A82),
            groundGrassTop = Color(0xFF8FA89A),
            groundGrassBottom = Color(0xFF5C6B63),
            sunCore = Color(0xFFE3F2FD),
            sunHaloEdge = Color(0xFFE3F2FD).copy(alpha = 0f)
        )
    }
}
