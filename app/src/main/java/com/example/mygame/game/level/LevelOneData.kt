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

object LevelOneData {

    fun build(worldWidth: Float, worldHeight: Float): LevelContent {
        val groundY = worldHeight * 0.82f
        val tile = worldWidth * 0.12f
        val hero = min(worldWidth, worldHeight) * 0.1f
        val levelLength = worldWidth * 5.1f

        val pits = listOf(
            Pit(startX = tile * 6.6f, endX = tile * 7.5f),
            Pit(startX = tile * 11.9f, endX = tile * 12.9f)
        )

        val platforms = listOf(
            Platform(x = tile * 3.0f, y = groundY - worldHeight * 0.18f, width = tile * 1.5f, height = 24f),
            Platform(x = tile * 4.9f, y = groundY - worldHeight * 0.3f, width = tile * 1.4f, height = 24f),
            Platform(x = tile * 8.1f, y = groundY - worldHeight * 0.22f, width = tile * 1.8f, height = 24f),
            Platform(x = tile * 10.2f, y = groundY - worldHeight * 0.34f, width = tile * 1.5f, height = 24f),
            Platform(x = tile * 13.4f, y = groundY - worldHeight * 0.24f, width = tile * 1.9f, height = 24f)
        )

        val blockSize = tile * 0.48f
        val blockRowY = groundY - worldHeight * 0.26f
        val blocks = listOf(
            Block(x = tile * 2.7f, y = blockRowY, size = blockSize, type = BlockType.Brick),
            Block(x = tile * 3.25f, y = blockRowY, size = blockSize, type = BlockType.Question, reward = BlockReward.Coin),
            Block(x = tile * 3.8f, y = blockRowY, size = blockSize, type = BlockType.Brick),
            Block(x = tile * 8.6f, y = groundY - worldHeight * 0.36f, size = blockSize, type = BlockType.Question, reward = BlockReward.Fish),
            Block(x = tile * 9.15f, y = groundY - worldHeight * 0.36f, size = blockSize, type = BlockType.Brick),
            Block(x = tile * 13.9f, y = groundY - worldHeight * 0.38f, size = blockSize, type = BlockType.Question, reward = BlockReward.Scarf)
        )

        val enemies = listOf(
            Enemy(
                x = tile * 4.2f,
                y = groundY - hero * 0.72f,
                width = hero * 0.78f,
                height = hero * 0.72f,
                patrolStart = tile * 3.7f,
                patrolEnd = tile * 5.6f,
                speed = 120f,
                kind = EnemyKind.Seal
            ),
            Enemy(
                x = tile * 9.7f,
                y = groundY - hero * 1.35f,
                width = hero * 0.7f,
                height = hero * 0.54f,
                patrolStart = tile * 8.3f,
                patrolEnd = tile * 10.8f,
                speed = 132f,
                kind = EnemyKind.Bird
            ),
            Enemy(
                x = tile * 14.0f,
                y = groundY - worldHeight * 0.24f - hero * 0.72f,
                width = hero * 0.78f,
                height = hero * 0.72f,
                patrolStart = tile * 13.4f,
                patrolEnd = tile * 15.1f,
                speed = 96f,
                kind = EnemyKind.Seal
            )
        )

        val coins = listOf(
            Coin(x = tile * 2.2f, y = groundY - worldHeight * 0.12f, size = hero * 0.42f),
            Coin(x = tile * 5.3f, y = groundY - worldHeight * 0.4f, size = hero * 0.42f),
            Coin(x = tile * 8.8f, y = groundY - worldHeight * 0.48f, size = hero * 0.42f),
            Coin(x = tile * 10.5f, y = groundY - worldHeight * 0.46f, size = hero * 0.42f),
            Coin(x = tile * 13.7f, y = groundY - worldHeight * 0.34f, size = hero * 0.42f),
            Coin(x = tile * 15.2f, y = groundY - worldHeight * 0.18f, size = hero * 0.42f)
        )

        val friendGoal = FriendGoal(
            x = levelLength - worldWidth * 0.32f,
            groundY = groundY,
            height = worldHeight * 0.35f
        )

        val presentation = LevelPresentation(
            introTitle = "咕咕嘎嘎 雪松村废墟",
            introDescription = "高松鹅摧毁了咕咕嘎嘎的家园，也让小伙伴们四散失落。\n穿过雪松村废墟，躲开冰壳海豹和风雪乌鸦，去救回失散的团团。",
            failHint = "可以先拿到鱼干冲刺或泡泡围巾，再通过倒塌小屋和巡逻敌人区。",
            victoryTitle = "成功救回团团",
            victoryDescription = "咕咕嘎嘎已经在雪松村废墟里找到了团团，两人决定一起追上高松鹅。\n下一步我们可以继续去冰湖回音谷，寻找新的伙伴线索。",
            chapterPreviewTitle = "第二关预告：冰湖回音谷",
            chapterPreviewDescription = "团团加入后，你已经可以在关卡中发动一次伙伴支援，用雪团击退敌人并减缓危险。\n下一站将前往冰湖回音谷，在对岸寻找新的伙伴线索。",
            hudGoalLine = "目标：穿过雪松村废墟，救回团团"
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
                Color(0xFF5BA8F0),
                Color(0xFF8ECDFA),
                Color(0xFFD8EEFC),
                Color(0xFFEAF9FF)
            ),
            stageSkyColors = listOf(
                Color(0xFF6EB8EA),
                Color(0xFF9FD4F5),
                Color(0xFFC8E8FF)
            ),
            pitWaterColor = Color(0xFF4F8CC9),
            groundGrassTop = Color(0xFF6CCB5F),
            groundGrassBottom = Color(0xFF7C4C29),
            sunCore = Color(0xFFFFFDE7),
            sunHaloEdge = Color(0xFFFFFDE7).copy(alpha = 0f)
        )
    }
}
