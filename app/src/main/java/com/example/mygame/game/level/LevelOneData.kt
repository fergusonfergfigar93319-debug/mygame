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
            Block(x = tile * 3.25f, y = blockRowY, size = blockSize, type = BlockType.Question, reward = BlockReward.Shield),
            Block(x = tile * 3.8f, y = blockRowY, size = blockSize, type = BlockType.Brick),
            Block(x = tile * 8.6f, y = groundY - worldHeight * 0.36f, size = blockSize, type = BlockType.Question, reward = BlockReward.Fish),
            Block(x = tile * 9.15f, y = groundY - worldHeight * 0.36f, size = blockSize, type = BlockType.Brick),
            Block(x = tile * 12.9f, y = groundY - worldHeight * 0.22f, size = blockSize, type = BlockType.Question, reward = BlockReward.Boots),
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
                kind = EnemyKind.Owl
            ),
            Enemy(
                x = tile * 14.0f,
                y = groundY - worldHeight * 0.24f - hero * 0.72f,
                width = hero * 0.78f,
                height = hero * 0.72f,
                patrolStart = tile * 13.4f,
                patrolEnd = tile * 15.1f,
                speed = 96f,
                kind = EnemyKind.SpikedSeal
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
            introDescription = "雪松村的灯火已经熄灭，只剩断掉的栈桥、被掀翻的小屋和满地风雪。\n咕咕嘎嘎知道团团还活着，于是她决定穿过废墟，把第一位伙伴从混乱中带回来。",
            failHint = "先在前半段熟悉移动、跳跃和顶箱，再用新拿到的道具穿过敌人巡逻区会更稳。",
            victoryTitle = "成功救回团团",
            victoryDescription = "在废墟尽头，咕咕嘎嘎终于找到了团团。熟悉的呼喊声重新回到耳边，旅途第一次不再只剩自己一个。\n两只小企鹅立刻决定继续北上，因为高松鹅留下的足迹正通往冰湖回音谷。",
            chapterPreviewTitle = "第二关预告：冰湖回音谷",
            chapterPreviewDescription = "团团归队后，队伍第一次拥有了真正的支援能力。\n下一站是风声会回响的冰湖回音谷，那里可能藏着新的伙伴消息，也可能藏着高松鹅提前布下的埋伏。",
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
            sceneTheme =
                StorySceneTheme(
                    outerGradientColors = listOf(
                        Color(0xFF5BA8F0),
                        Color(0xFF8ECDFA),
                        Color(0xFFD8EEFC),
                        Color(0xFFEAF9FF),
                    ),
                    skyGradientColors = listOf(
                        Color(0xFF6EB8EA),
                        Color(0xFF9FD4F5),
                        Color(0xFFC8E8FF),
                    ),
                    waterColor = Color(0xFF4F8CC9),
                    groundTopColor = Color(0xFF6CCB5F),
                    groundBottomColor = Color(0xFF7C4C29),
                    sunCoreColor = Color(0xFFFFFDE7),
                    sunHaloEdgeColor = Color(0xFFFFFDE7).copy(alpha = 0f),
                    platformTopColor = Color(0xFFC57A34),
                    platformBottomColor = Color(0xFF8D5524),
                    brickColor = Color(0xFFB96D34),
                    brickShadeColor = Color(0xFF7A4720),
                    questionColor = Color(0xFFFFC64D),
                    questionUsedColor = Color(0xFFB0A48E),
                    questionMarkColor = Color(0xFF6B3E12),
                    sealBodyColor = Color(0xFF7F98A8),
                    sealBellyColor = Color(0xFFEFF7FB),
                    birdWingColor = Color(0xFF57697A),
                    birdBodyColor = Color(0xFF8AA0B2),
                    fishBodyColor = Color(0xFFFF8A65),
                    fishTailColor = Color(0xFFFFCC80),
                    hillSnowColor = Color(0xFFD8F0FF),
                    hillStoneColor = Color(0xFF9DB4C5),
                    hutWallColor = Color(0x80F3FBFF),
                    hutRoofColor = Color(0xFF8BC4E8),
                    hutBeamColor = Color(0xFF5D7B8D),
                    foregroundMistColor = Color(0xFFC8E8FF),
                    foregroundShardColor = Color(0xFF6CCB5F),
                    snowflakeAlphaBase = 0.22f,
                    snowflakeAlphaStep = 0.06f,
                    cloudCount = 6,
                    ridgeCount = 7,
                    hillCount = 5,
                    hutCount = 4,
                ),
        )
    }
}
