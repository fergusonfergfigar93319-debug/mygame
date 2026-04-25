package com.example.mygame.game.level

import androidx.compose.ui.graphics.Color
import com.example.mygame.game.Block
import com.example.mygame.game.BlockReward
import com.example.mygame.game.BlockType
import com.example.mygame.game.Coin
import com.example.mygame.game.CoinKind
import com.example.mygame.game.Enemy
import com.example.mygame.game.EnemyKind
import com.example.mygame.game.FriendGoal
import com.example.mygame.game.Pit
import com.example.mygame.game.Platform
import kotlin.math.min

/** 第二关：冰湖回音谷。强调冰面滑行、薄冰、裂隙与高低平台节奏。 */
object LevelTwoData {

    fun build(worldWidth: Float, worldHeight: Float): LevelContent {
        val groundY = worldHeight * 0.82f
        val tile = worldWidth * 0.12f
        val hero = min(worldWidth, worldHeight) * 0.1f
        val levelLength = worldWidth * 5.42f

        val pits =
            listOf(
                Pit(startX = tile * 5.95f, endX = tile * 6.78f),
                Pit(startX = tile * 11.05f, endX = tile * 12.28f),
                Pit(startX = tile * 17.35f, endX = tile * 18.55f),
            )

        val platforms =
            listOf(
                Platform(x = tile * 2.35f, y = groundY - worldHeight * 0.17f, width = tile * 1.45f, height = 24f),
                Platform(x = tile * 3.55f, y = groundY - 12f, width = tile * 3.4f, height = 16f, surfaceFriction = 0.24f),
                Platform(x = tile * 4.55f, y = groundY - worldHeight * 0.26f, width = tile * 1.35f, height = 24f),
                Platform(x = tile * 7.15f, y = groundY - worldHeight * 0.21f, width = tile * 1.65f, height = 24f, surfaceFriction = 0.26f, isFragile = true),
                Platform(x = tile * 8.85f, y = groundY - worldHeight * 0.38f, width = tile * 1.15f, height = 22f),
                Platform(x = tile * 9.75f, y = groundY - worldHeight * 0.27f, width = tile * 1.3f, height = 24f),
                Platform(x = tile * 12.55f, y = groundY - worldHeight * 0.2f, width = tile * 1.55f, height = 24f),
                Platform(x = tile * 14.15f, y = groundY - worldHeight * 0.35f, width = tile * 1.45f, height = 24f),
                Platform(x = tile * 15.85f, y = groundY - worldHeight * 0.23f, width = tile * 1.35f, height = 24f),
                Platform(x = tile * 18.75f, y = groundY - worldHeight * 0.19f, width = tile * 1.65f, height = 24f),
            )

        val blockSize = tile * 0.48f
        val blockRowY = groundY - worldHeight * 0.25f
        val blocks =
            listOf(
                Block(x = tile * 2.05f, y = blockRowY, size = blockSize, type = BlockType.Brick),
                Block(x = tile * 2.5f, y = blockRowY, size = blockSize, type = BlockType.Question, reward = BlockReward.Coin),
                Block(x = tile * 2.95f, y = blockRowY, size = blockSize, type = BlockType.Brick),
                Block(x = tile * 5.95f, y = groundY - blockSize * 1.05f, size = blockSize, type = BlockType.Question, reward = BlockReward.Fish),
                Block(x = tile * 6.95f, y = groundY - worldHeight * 0.33f, size = blockSize, type = BlockType.Question, reward = BlockReward.Fish),
                Block(x = tile * 8.15f, y = groundY - worldHeight * 0.28f, size = blockSize, type = BlockType.Question, reward = BlockReward.Shield),
                Block(x = tile * 10.35f, y = groundY - worldHeight * 0.36f, size = blockSize, type = BlockType.Brick),
                Block(x = tile * 10.8f, y = groundY - worldHeight * 0.36f, size = blockSize, type = BlockType.Question, reward = BlockReward.Magnet),
                Block(x = tile * 13.65f, y = groundY - worldHeight * 0.4f, size = blockSize, type = BlockType.Question, reward = BlockReward.Scarf),
                Block(x = tile * 15.55f, y = groundY - worldHeight * 0.3f, size = blockSize, type = BlockType.Question, reward = BlockReward.Boots),
                Block(x = tile * 16.45f, y = groundY - worldHeight * 0.32f, size = blockSize, type = BlockType.Brick),
                Block(x = tile * 19.35f, y = groundY - worldHeight * 0.34f, size = blockSize, type = BlockType.Question, reward = BlockReward.Fish),
            )

        val enemies =
            listOf(
                Enemy(tile * 3.85f, groundY - hero * 0.72f, hero * 0.78f, hero * 0.72f, tile * 3.15f, tile * 5.55f, 118f, EnemyKind.Seal),
                Enemy(tile * 8.45f, groundY - hero * 1.32f, hero * 0.7f, hero * 0.54f, tile * 7.5f, tile * 10.65f, 128f, EnemyKind.Owl),
                Enemy(tile * 6.88f, groundY - hero * 0.72f, hero * 0.92f, hero * 0.72f, tile * 6.88f, tile * 6.88f + hero * 0.92f, 0f, EnemyKind.Seal, hasIceShield = true),
                Enemy(tile * 11.92f, groundY - hero * 0.48f, hero * 0.74f, hero * 0.48f, tile * 11.72f, tile * 12.9f, 82f, EnemyKind.SnowMole),
                Enemy(tile * 13.35f, groundY - hero * 0.72f, hero * 0.78f, hero * 0.72f, tile * 12.65f, tile * 15.05f, 108f, EnemyKind.SpikedSeal),
                Enemy(tile * 16.85f, groundY - hero * 1.22f, hero * 0.68f, hero * 0.52f, tile * 15.95f, tile * 18.45f, 122f, EnemyKind.Bird),
            )

        val coins =
            listOf(
                Coin(x = tile * 1.85f, y = groundY - worldHeight * 0.11f, size = hero * 0.42f),
                Coin(x = tile * 5.15f, y = groundY - worldHeight * 0.39f, size = hero * 0.42f),
                Coin(x = tile * 8.05f, y = groundY - worldHeight * 0.45f, size = hero * 0.42f),
                Coin(x = tile * 8.92f, y = groundY - worldHeight * 0.52f, size = hero * 0.42f, kind = CoinKind.LorePage),
                Coin(x = tile * 10.95f, y = groundY - worldHeight * 0.18f, size = hero * 0.42f),
                Coin(x = tile * 11.65f, y = groundY - worldHeight * 0.44f, size = hero * 0.42f),
                Coin(x = tile * 14.55f, y = groundY - worldHeight * 0.16f, size = hero * 0.42f),
                Coin(x = tile * 16.95f, y = groundY - worldHeight * 0.42f, size = hero * 0.42f),
                Coin(x = tile * 17.85f, y = groundY - worldHeight * 0.37f, size = hero * 0.42f, kind = CoinKind.Beacon),
                Coin(x = tile * 18.15f, y = groundY - worldHeight * 0.28f, size = hero * 0.42f),
                Coin(x = tile * 20.15f, y = groundY - worldHeight * 0.14f, size = hero * 0.42f),
            )

        val friendGoal =
            FriendGoal(
                x = levelLength - worldWidth * 0.32f,
                groundY = groundY,
                height = worldHeight * 0.35f,
            )

        val presentation =
            LevelPresentation(
                introTitle = "咕咕嘎嘎 · 冰湖回音谷",
                introDescription = "冰湖像一面裂开的镜子，把每一步脚印都放大成回音。\n" +
                    "咕咕嘎嘎和团团必须越过更宽的裂隙、更滑的冰面和更紧密的巡逻，才能继续追向北方。",
                failHint = "先抢高位平台，再决定用长跳抢距离、用雪盾硬吃压力，或用极光磁针收掉安全线上的鱼干。",
                victoryTitle = "抵达回声浅滩",
                victoryDescription = "当你们落上回声浅滩时，原本混乱的风声忽然整齐了一瞬，像是远方有人在雾里回应这趟旅程。\n" +
                    "高松鹅的足迹继续指向更北的雾堤，而那里，或许正藏着下一位伙伴的消息。",
                chapterPreviewTitle = "第三关预告：北境雾堤",
                chapterPreviewDescription = "冰湖之后，旅程会进入更压抑也更陌生的区域：雾堤、暗流、窄路和被遮住视线的追逐节奏。\n" +
                    "下一章会更强调伙伴协作与读图能力，也会让高松鹅的威胁感真正靠近。",
                hudGoalLine = "目标：穿越冰湖裂隙与回音崖，抵达回声浅滩",
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
                    outerGradientColors = listOf(Color(0xFF2A4A6E), Color(0xFF3D6FA8), Color(0xFF6B9BD4), Color(0xFFC5DDF0)),
                    skyGradientColors = listOf(Color(0xFF35577D), Color(0xFF5E88B0), Color(0xFF8FB4D4)),
                    waterColor = Color(0xFF1E4566),
                    groundTopColor = Color(0xFF7A9B8E),
                    groundBottomColor = Color(0xFF4A5C56),
                    sunCoreColor = Color(0xFFE8EAF6),
                    sunHaloEdgeColor = Color(0xFFB0BEC5).copy(alpha = 0f),
                    platformTopColor = Color(0xFF8FA8B8),
                    platformBottomColor = Color(0xFF627784),
                    brickColor = Color(0xFF8FA1AC),
                    brickShadeColor = Color(0xFF576A74),
                    questionColor = Color(0xFFF7D26A),
                    questionUsedColor = Color(0xFF95A3A9),
                    questionMarkColor = Color(0xFF3C4E57),
                    sealBodyColor = Color(0xFF6D8697),
                    sealBellyColor = Color(0xFFE7F0F5),
                    birdWingColor = Color(0xFF536879),
                    birdBodyColor = Color(0xFF7C95A8),
                    fishBodyColor = Color(0xFFFFAB91),
                    fishTailColor = Color(0xFFFFE0B2),
                    hillSnowColor = Color(0xFFE0F4FF),
                    hillStoneColor = Color(0xFF88A4B8),
                    hutWallColor = Color(0x66E3F2FF),
                    hutRoofColor = Color(0xFF89B7D6),
                    hutBeamColor = Color(0xFF4C6678),
                    foregroundMistColor = Color(0xFFB8D4E8),
                    foregroundShardColor = Color(0xFF8FB4D4),
                    snowflakeAlphaBase = 0.2f,
                    snowflakeAlphaStep = 0.05f,
                    cloudCount = 4,
                    ridgeCount = 8,
                    hillCount = 6,
                    hutCount = 2,
                ),
        )
    }
}
