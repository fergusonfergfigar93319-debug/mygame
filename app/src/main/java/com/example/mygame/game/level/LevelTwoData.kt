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
import com.example.mygame.game.LevelNpc
import com.example.mygame.game.NpcKind
import com.example.mygame.game.Pit
import com.example.mygame.game.Platform
import com.example.mygame.game.WorldPickup
import com.example.mygame.game.WorldPickupKind
import kotlin.math.min

/** 第二关：冰湖回音谷。强调冰面滑行、薄冰、裂隙与高低平台节奏。 */
object LevelTwoData {

    fun build(worldWidth: Float, worldHeight: Float): LevelContent {
        val groundY = worldHeight * 0.82f
        val tile = worldWidth * 0.12f
        val hero = min(worldWidth, worldHeight) * 0.1f
        val levelLength = worldWidth * 6.9f

        val pits =
            listOf(
                Pit(startX = tile * 6.05f, endX = tile * 6.68f),
                Pit(startX = tile * 11.18f, endX = tile * 12.05f),
                Pit(startX = tile * 17.45f, endX = tile * 18.36f),
                Pit(startX = tile * 22.6f, endX = tile * 23.4f),
                Pit(startX = tile * 28.0f, endX = tile * 28.75f),
                Pit(startX = tile * 33.0f, endX = tile * 33.8f),
            )

        val platforms =
            listOf(
                Platform(x = tile * 2.35f, y = groundY - worldHeight * 0.17f, width = tile * 1.45f, height = 24f),
                Platform(x = tile * 3.55f, y = groundY - 12f, width = tile * 3.4f, height = 16f, surfaceFriction = 0.24f, conveyorBelt = 62f),
                Platform(x = tile * 4.45f, y = groundY - worldHeight * 0.23f, width = tile * 1.55f, height = 24f),
                Platform(
                    x = tile * 7.05f,
                    y = groundY - worldHeight * 0.18f,
                    width = tile * 1.9f,
                    height = 24f,
                    surfaceFriction = 0.32f,
                    isFragile = true,
                ),
                Platform(x = tile * 8.55f, y = groundY - worldHeight * 0.31f, width = tile * 1.35f, height = 22f),
                Platform(x = tile * 9.65f, y = groundY - worldHeight * 0.24f, width = tile * 1.55f, height = 24f),
                Platform(x = tile * 12.35f, y = groundY - worldHeight * 0.17f, width = tile * 1.85f, height = 24f),
                Platform(x = tile * 13.9f, y = groundY - worldHeight * 0.29f, width = tile * 1.65f, height = 24f),
                Platform(x = tile * 15.7f, y = groundY - worldHeight * 0.2f, width = tile * 1.6f, height = 24f),
                Platform(
                    x = tile * 16.0f,
                    y = groundY - worldHeight * 0.1f,
                    width = tile * 0.88f,
                    height = 20f,
                    surfaceFriction = 0.3f,
                    bounceImpulse = 740f,
                ),
                Platform(x = tile * 18.55f, y = groundY - worldHeight * 0.16f, width = tile * 1.95f, height = 24f),
                Platform(x = tile * 20.0f, y = groundY - 12f, width = tile * 2.2f, height = 16f, surfaceFriction = 0.22f, conveyorBelt = -75f),
                Platform(x = tile * 21.0f, y = groundY - worldHeight * 0.26f, width = tile * 1.4f, height = 22f, surfaceFriction = 0.45f, isFragile = true),
                Platform(x = tile * 22.0f, y = groundY - worldHeight * 0.33f, width = tile * 1.2f, height = 22f),
                Platform(x = tile * 24.0f, y = groundY - worldHeight * 0.2f, width = tile * 1.8f, height = 24f, surfaceFriction = 0.28f),
                Platform(x = tile * 25.2f, y = groundY - worldHeight * 0.3f, width = tile * 1.4f, height = 22f),
                Platform(x = tile * 27.0f, y = groundY - worldHeight * 0.15f, width = tile * 1.5f, height = 24f),
                Platform(
                    x = tile * 28.2f,
                    y = groundY - worldHeight * 0.11f,
                    width = tile * 0.9f,
                    height = 20f,
                    surfaceFriction = 0.35f,
                    bounceImpulse = 700f,
                ),
                Platform(x = tile * 30.0f, y = groundY - worldHeight * 0.19f, width = tile * 1.7f, height = 24f),
                Platform(x = tile * 31.5f, y = groundY - 11f, width = tile * 2.4f, height = 16f, surfaceFriction = 0.26f, conveyorBelt = 80f),
                Platform(x = tile * 33.0f, y = groundY - worldHeight * 0.22f, width = tile * 1.5f, height = 24f, surfaceFriction = 0.4f, isFragile = true),
                Platform(x = tile * 35.0f, y = groundY - worldHeight * 0.18f, width = tile * 1.55f, height = 24f),
                Platform(x = tile * 36.5f, y = groundY - worldHeight * 0.3f, width = tile * 1.3f, height = 22f),
            )

        val blockSize = tile * 0.48f
        val blockRowY = groundY - worldHeight * 0.25f
        val blocks =
            listOf(
                Block(x = tile * 2.05f, y = blockRowY, size = blockSize, type = BlockType.Brick),
                Block(x = tile * 2.5f, y = blockRowY, size = blockSize, type = BlockType.Question, reward = BlockReward.Coin),
                Block(x = tile * 2.95f, y = blockRowY, size = blockSize, type = BlockType.Brick),
                Block(x = tile * 5.95f, y = groundY - blockSize * 1.05f, size = blockSize, type = BlockType.Question, reward = BlockReward.Fish),
                Block(x = tile * 6.95f, y = groundY - worldHeight * 0.29f, size = blockSize, type = BlockType.Question, reward = BlockReward.Fish),
                Block(x = tile * 8.15f, y = groundY - worldHeight * 0.28f, size = blockSize, type = BlockType.Question, reward = BlockReward.Shield),
                Block(x = tile * 10.35f, y = groundY - worldHeight * 0.31f, size = blockSize, type = BlockType.Brick),
                Block(x = tile * 10.8f, y = groundY - worldHeight * 0.31f, size = blockSize, type = BlockType.Question, reward = BlockReward.Magnet),
                Block(x = tile * 13.65f, y = groundY - worldHeight * 0.34f, size = blockSize, type = BlockType.Question, reward = BlockReward.Scarf),
                Block(x = tile * 15.55f, y = groundY - worldHeight * 0.26f, size = blockSize, type = BlockType.Question, reward = BlockReward.Boots),
                Block(x = tile * 16.45f, y = groundY - worldHeight * 0.32f, size = blockSize, type = BlockType.Brick),
                Block(x = tile * 19.35f, y = groundY - worldHeight * 0.34f, size = blockSize, type = BlockType.Question, reward = BlockReward.Fish),
                Block(x = tile * 20.2f, y = groundY - worldHeight * 0.36f, size = blockSize, type = BlockType.Question, reward = BlockReward.Coin),
                Block(x = tile * 24.0f, y = groundY - worldHeight * 0.3f, size = blockSize, type = BlockType.Question, reward = BlockReward.Shield),
                Block(x = tile * 26.5f, y = groundY - worldHeight * 0.32f, size = blockSize, type = BlockType.Question, reward = BlockReward.Fish),
                Block(x = tile * 29.0f, y = groundY - worldHeight * 0.28f, size = blockSize, type = BlockType.Question, reward = BlockReward.Magnet),
                Block(x = tile * 32.0f, y = groundY - worldHeight * 0.3f, size = blockSize, type = BlockType.Question, reward = BlockReward.Boots),
                Block(x = tile * 35.0f, y = groundY - worldHeight * 0.3f, size = blockSize, type = BlockType.Question, reward = BlockReward.Fish),
            )

        val enemies =
            listOf(
                Enemy(tile * 3.85f, groundY - hero * 0.72f, hero * 0.78f, hero * 0.72f, tile * 3.15f, tile * 5.55f, 118f, EnemyKind.Seal),
                Enemy(tile * 8.45f, groundY - hero * 1.32f, hero * 0.7f, hero * 0.54f, tile * 7.5f, tile * 10.65f, 128f, EnemyKind.Owl),
                Enemy(tile * 6.88f, groundY - hero * 0.72f, hero * 0.92f, hero * 0.72f, tile * 6.88f, tile * 6.88f + hero * 0.92f, 0f, EnemyKind.Seal, hasIceShield = true),
                Enemy(tile * 11.92f, groundY - hero * 0.48f, hero * 0.74f, hero * 0.48f, tile * 11.72f, tile * 12.9f, 82f, EnemyKind.SnowMole),
                Enemy(tile * 13.35f, groundY - hero * 0.72f, hero * 0.78f, hero * 0.72f, tile * 12.65f, tile * 15.05f, 108f, EnemyKind.SpikedSeal),
                Enemy(tile * 16.85f, groundY - hero * 1.22f, hero * 0.68f, hero * 0.52f, tile * 15.95f, tile * 18.45f, 122f, EnemyKind.Bird),
                Enemy(tile * 22.0f, groundY - hero * 0.72f, hero * 0.75f, hero * 0.72f, tile * 21.2f, tile * 23.4f, 100f, EnemyKind.Seal),
                Enemy(tile * 25.0f, groundY - hero * 1.15f, hero * 0.68f, hero * 0.52f, tile * 24.0f, tile * 26.2f, 118f, EnemyKind.Bird),
                Enemy(tile * 30.0f, groundY - hero * 0.48f, hero * 0.72f, hero * 0.48f, tile * 29.5f, tile * 31.2f, 88f, EnemyKind.SnowMole),
                Enemy(tile * 35.0f, groundY - hero * 0.72f, hero * 0.78f, hero * 0.72f, tile * 34.2f, tile * 36.4f, 112f, EnemyKind.SpikedSeal),
            )

        val c = hero * 0.42f
        val coins =
            listOf(
                Coin(x = tile * 1.85f, y = groundY - worldHeight * 0.11f, size = c),
                Coin(x = tile * 5.15f, y = groundY - worldHeight * 0.39f, size = c),
                Coin(x = tile * 8.05f, y = groundY - worldHeight * 0.45f, size = c),
                Coin(x = tile * 8.92f, y = groundY - worldHeight * 0.52f, size = c, kind = CoinKind.LorePage),
                Coin(x = tile * 10.95f, y = groundY - worldHeight * 0.18f, size = c),
                Coin(x = tile * 11.65f, y = groundY - worldHeight * 0.44f, size = c),
                Coin(x = tile * 14.55f, y = groundY - worldHeight * 0.16f, size = c),
                Coin(x = tile * 16.95f, y = groundY - worldHeight * 0.42f, size = c),
                Coin(x = tile * 17.85f, y = groundY - worldHeight * 0.37f, size = c, kind = CoinKind.Beacon),
                Coin(x = tile * 18.15f, y = groundY - worldHeight * 0.28f, size = c),
                Coin(x = tile * 20.15f, y = groundY - worldHeight * 0.14f, size = c),
                Coin(x = tile * 21.5f, y = groundY - worldHeight * 0.4f, size = c),
                Coin(x = tile * 24.0f, y = groundY - worldHeight * 0.2f, size = c),
                Coin(x = tile * 26.0f, y = groundY - worldHeight * 0.38f, size = c),
                Coin(x = tile * 28.5f, y = groundY - worldHeight * 0.22f, size = c, kind = CoinKind.Beacon),
                Coin(x = tile * 30.0f, y = groundY - worldHeight * 0.4f, size = c),
                Coin(x = tile * 32.5f, y = groundY - worldHeight * 0.2f, size = c),
                Coin(x = tile * 35.0f, y = groundY - worldHeight * 0.36f, size = c),
            )

        val worldPickups =
            listOf(
                WorldPickup(x = tile * 1.0f, y = groundY - 12f, size = hero * 0.28f, kind = WorldPickupKind.GustSeed),
                WorldPickup(x = tile * 8.0f, y = groundY - worldHeight * 0.2f, size = hero * 0.28f, kind = WorldPickupKind.Snowberry),
                WorldPickup(x = tile * 15.0f, y = groundY - 14f, size = hero * 0.28f, kind = WorldPickupKind.GustSeed),
                WorldPickup(x = tile * 19.0f, y = groundY - worldHeight * 0.2f, size = hero * 0.28f, kind = WorldPickupKind.GlintFragment),
                WorldPickup(x = tile * 25.0f, y = groundY - worldHeight * 0.2f, size = hero * 0.28f, kind = WorldPickupKind.Snowberry),
                WorldPickup(x = tile * 31.0f, y = groundY - 12f, size = hero * 0.28f, kind = WorldPickupKind.GustSeed),
            )

        val npcs =
            listOf(
                LevelNpc(
                    x = tile * 1.1f,
                    y = groundY - hero * 0.9f,
                    width = hero * 0.5f,
                    height = hero * 0.85f,
                    kind = NpcKind.Scout,
                    line = "逆风履带别硬顶方向——顺着滑下去反而省体力。",
                ),
                LevelNpc(
                    x = tile * 14.0f,
                    y = groundY - hero * 0.88f,
                    width = hero * 0.5f,
                    height = hero * 0.84f,
                    kind = NpcKind.Villager,
                    line = "两朵回音苔垫一高一低，中间那段薄冰记得速过。",
                ),
                LevelNpc(
                    x = tile * 23.0f,
                    y = groundY - hero * 0.42f,
                    width = hero * 0.8f,
                    height = hero * 0.38f,
                    kind = NpcKind.Sign,
                    line = "冰面传送带会把你甩向深裂隙，先抢高位再下跳。",
                ),
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
                failHint = "多利用弹簧苔与反向履带调整位置；风种与浆果在长途里能续一段节奏。",
                victoryTitle = "抵达回声浅滩",
                victoryDescription = "当你们落上回声浅滩时，原本混乱的风声忽然整齐了一瞬，像是远方有人在雾里回应这趟旅程。\n" +
                    "高松鹅的足迹继续指向更北的雾堤，而那里，或许正藏着下一位伙伴的消息。",
                chapterPreviewTitle = "第三关预告：北境雾堤",
                chapterPreviewDescription = "冰湖之后，旅程会进入更压抑也更陌生的区域：雾堤、暗流、窄路和被遮住视线的追逐节奏。\n" +
                    "下一章会更强调伙伴协作与读图能力，也会让高松鹅的威胁感真正靠近。",
                hudGoalLine = "目标：利用弹簧板与对向履带，穿越更长冰湖裂隙，抵达回声浅滩",
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
            npcs = npcs,
            worldPickups = worldPickups,
        )
    }
}
