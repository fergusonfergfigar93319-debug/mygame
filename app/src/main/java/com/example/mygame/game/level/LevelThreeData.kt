package com.example.mygame.game.level

import androidx.compose.ui.graphics.Color
import com.example.mygame.game.Block
import com.example.mygame.game.BlockReward
import com.example.mygame.game.BlockType
import com.example.mygame.game.BossArenaSpec
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

/**
 * 第三关「北境雾堤」：雾中堤道热身段后进入高松鹅竞技场。
 */
object LevelThreeData {

    fun build(worldWidth: Float, worldHeight: Float): LevelContent {
        val groundY = worldHeight * 0.82f
        val tile = worldWidth * 0.12f
        val hero = min(worldWidth, worldHeight) * 0.1f
        val levelLength = worldWidth * 5.6f

        val pits = listOf(
            Pit(startX = tile * 1.2f, endX = tile * 1.9f),
            Pit(startX = tile * 3.4f, endX = tile * 4.15f),
            Pit(startX = tile * 5.0f, endX = tile * 5.7f),
            Pit(startX = tile * 7.2f, endX = tile * 7.9f),
            Pit(startX = tile * 9.2f, endX = tile * 9.9f),
        )

        val platforms = listOf(
            Platform(x = tile * 0.7f, y = groundY - worldHeight * 0.16f, width = tile * 0.7f, height = 20f, surfaceFriction = 0.88f),
            Platform(x = tile * 0.0f, y = groundY - 9f, width = tile * 0.6f, height = 14f, surfaceFriction = 0.85f, conveyorBelt = 55f),
            Platform(x = tile * 2.0f, y = groundY - worldHeight * 0.19f, width = tile * 1.3f, height = 24f),
            Platform(x = tile * 2.4f, y = groundY - worldHeight * 0.32f, width = tile * 1.1f, height = 22f),
            Platform(
                x = tile * 2.6f,
                y = groundY - worldHeight * 0.1f,
                width = tile * 0.7f,
                height = 20f,
                bounceImpulse = 700f,
                surfaceFriction = 0.9f,
            ),
            Platform(x = tile * 2.0f, y = groundY - worldHeight * 0.12f, width = tile * 1.55f, height = 16f, surfaceFriction = 0.85f, conveyorBelt = 58f),
            Platform(x = tile * 3.6f, y = groundY - worldHeight * 0.2f, width = tile * 1.2f, height = 24f),
            Platform(
                x = tile * 4.2f,
                y = groundY - worldHeight * 0.36f,
                width = tile * 0.7f,
                height = 20f,
                surfaceFriction = 0.48f,
                isFragile = true,
            ),
            Platform(x = tile * 4.8f, y = groundY - worldHeight * 0.16f, width = tile * 1.2f, height = 24f),
            Platform(x = tile * 5.0f, y = groundY - worldHeight * 0.13f, width = tile * 1.15f, height = 16f, surfaceFriction = 0.45f, conveyorBelt = -46f),
            Platform(x = tile * 6.2f, y = groundY - worldHeight * 0.22f, width = tile * 1.4f, height = 24f),
            Platform(
                x = tile * 6.0f,
                y = groundY - worldHeight * 0.12f,
                width = tile * 0.8f,
                height = 20f,
                bounceImpulse = 720f,
            ),
            Platform(x = tile * 6.4f, y = groundY - worldHeight * 0.3f, width = tile * 1.0f, height = 22f),
            Platform(x = tile * 7.0f, y = groundY - worldHeight * 0.2f, width = tile * 1.3f, height = 24f),
            Platform(
                x = tile * 7.6f,
                y = groundY - worldHeight * 0.11f,
                width = tile * 0.9f,
                height = 20f,
                surfaceFriction = 0.4f,
                isFragile = true,
            ),
            Platform(x = tile * 8.0f, y = groundY - 10f, width = tile * 1.75f, height = 16f, surfaceFriction = 0.42f, conveyorBelt = 60f),
            Platform(x = tile * 8.0f, y = groundY - worldHeight * 0.2f, width = tile * 1.4f, height = 24f),
        )

        val blockSize = tile * 0.48f
        val blockRowY = groundY - worldHeight * 0.24f
        val blocks = listOf(
            Block(x = tile * 0.5f, y = blockRowY, size = blockSize, type = BlockType.Question, reward = BlockReward.Coin),
            Block(x = tile * 0.3f, y = groundY - worldHeight * 0.3f, size = blockSize, type = BlockType.Question, reward = BlockReward.Fish),
            Block(x = tile * 1.4f, y = blockRowY, size = blockSize, type = BlockType.Brick),
            Block(x = tile * 1.9f, y = blockRowY, size = blockSize, type = BlockType.Question, reward = BlockReward.Magnet),
            Block(x = tile * 2.4f, y = blockRowY, size = blockSize, type = BlockType.Brick),
            Block(x = tile * 2.0f, y = groundY - worldHeight * 0.3f, size = blockSize, type = BlockType.Question, reward = BlockReward.Shield),
            Block(x = tile * 2.0f, y = groundY - worldHeight * 0.36f, size = blockSize, type = BlockType.Question, reward = BlockReward.Boots),
            Block(x = tile * 3.5f, y = groundY - worldHeight * 0.3f, size = blockSize, type = BlockType.Question, reward = BlockReward.Fish),
            Block(x = tile * 4.5f, y = groundY - worldHeight * 0.3f, size = blockSize, type = BlockType.Question, reward = BlockReward.Coin),
            Block(x = tile * 4.0f, y = groundY - worldHeight * 0.17f, size = blockSize, type = BlockType.Question, reward = BlockReward.Scarf),
            Block(x = tile * 5.0f, y = groundY - worldHeight * 0.3f, size = blockSize, type = BlockType.Question, reward = BlockReward.Fish),
        )

        val enemies = listOf(
            Enemy(
                x = tile * 0.1f,
                y = groundY - hero * 0.65f,
                width = hero * 0.7f,
                height = hero * 0.6f,
                patrolStart = tile * 0.0f,
                patrolEnd = tile * 0.55f,
                speed = 95f,
                kind = EnemyKind.SnowMole,
            ),
            Enemy(
                x = tile * 2.0f,
                y = groundY - hero * 0.7f,
                width = hero * 0.75f,
                height = hero * 0.7f,
                patrolStart = tile * 1.4f,
                patrolEnd = tile * 2.2f,
                speed = 102f,
                kind = EnemyKind.Seal,
            ),
            Enemy(
                x = tile * 1.0f,
                y = groundY - hero * 0.4f,
                width = hero * 0.72f,
                height = hero * 0.48f,
                patrolStart = tile * 0.9f,
                patrolEnd = tile * 1.6f,
                speed = 80f,
                kind = EnemyKind.SnowMole,
            ),
            Enemy(
                x = tile * 3.0f,
                y = groundY - hero * 1.25f,
                width = hero * 0.68f,
                height = hero * 0.5f,
                patrolStart = tile * 2.4f,
                patrolEnd = tile * 3.1f,
                speed = 120f,
                kind = EnemyKind.Bird,
            ),
            Enemy(
                x = tile * 2.0f,
                y = groundY - worldHeight * 0.3f - hero * 0.72f,
                width = hero * 0.75f,
                height = hero * 0.7f,
                patrolStart = tile * 1.8f,
                patrolEnd = tile * 2.6f,
                speed = 100f,
                kind = EnemyKind.SpikedSeal,
            ),
            Enemy(
                x = tile * 4.0f,
                y = groundY - hero * 0.72f,
                width = hero * 0.75f,
                height = hero * 0.7f,
                patrolStart = tile * 3.6f,
                patrolEnd = tile * 4.5f,
                speed = 98f,
                kind = EnemyKind.Seal,
            ),
            Enemy(
                x = tile * 3.0f,
                y = groundY - hero * 0.4f,
                width = hero * 0.7f,
                height = hero * 0.48f,
                patrolStart = tile * 2.8f,
                patrolEnd = tile * 3.6f,
                speed = 85f,
                kind = EnemyKind.SnowMole,
            ),
            Enemy(
                x = tile * 4.0f,
                y = groundY - hero * 0.4f,
                width = hero * 0.72f,
                height = hero * 0.48f,
                patrolStart = tile * 3.4f,
                patrolEnd = tile * 4.1f,
                speed = 88f,
                kind = EnemyKind.SnowMole,
            ),
        )

        val c = hero * 0.42f
        val coins = listOf(
            Coin(x = tile * 0.0f, y = groundY - worldHeight * 0.1f, size = c),
            Coin(x = tile * 0.0f, y = groundY - 11f, size = c),
            Coin(x = tile * 0.4f, y = groundY - worldHeight * 0.1f, size = c),
            Coin(x = tile * 1.2f, y = groundY - worldHeight * 0.1f, size = c, kind = CoinKind.LorePage),
            Coin(x = tile * 1.2f, y = groundY - 12f, size = c),
            Coin(x = tile * 1.6f, y = groundY - worldHeight * 0.38f, size = c),
            Coin(x = tile * 0.0f, y = groundY - 12f, size = c, kind = CoinKind.Beacon),
            Coin(x = tile * 0.0f, y = 12f, size = c),
            Coin(x = tile * 0.0f, y = 12f, size = c, kind = CoinKind.Beacon),
            Coin(x = tile * 1.0f, y = 12f, size = c, kind = CoinKind.Beacon),
            Coin(x = tile * 2.0f, y = groundY - worldHeight * 0.1f, size = c),
            Coin(x = tile * 2.0f, y = 12f, size = c),
            Coin(x = tile * 2.2f, y = groundY - worldHeight * 0.4f, size = c),
            Coin(x = tile * 3.0f, y = groundY - worldHeight * 0.1f, size = c),
            Coin(x = tile * 3.5f, y = 12f, size = c, kind = CoinKind.Beacon),
            Coin(x = tile * 4.2f, y = groundY - worldHeight * 0.12f, size = c),
        )

        val worldPickups = listOf(
            WorldPickup(x = tile * 0.2f, y = groundY - 12f, size = hero * 0.28f, kind = WorldPickupKind.GustSeed),
            WorldPickup(x = tile * 0.9f, y = groundY - 12f, size = hero * 0.28f, kind = WorldPickupKind.Snowberry),
            WorldPickup(x = tile * 1.5f, y = groundY - 12f, size = hero * 0.28f, kind = WorldPickupKind.GlintFragment),
            WorldPickup(x = tile * 2.2f, y = groundY - 12f, size = hero * 0.28f, kind = WorldPickupKind.GustSeed),
            WorldPickup(x = tile * 3.0f, y = groundY - 12f, size = hero * 0.28f, kind = WorldPickupKind.Snowberry),
            WorldPickup(x = tile * 3.8f, y = groundY - 12f, size = hero * 0.28f, kind = WorldPickupKind.GustSeed),
            WorldPickup(x = tile * 4.4f, y = groundY - 12f, size = hero * 0.28f, kind = WorldPickupKind.Snowberry),
        )

        val npcs = listOf(
            LevelNpc(
                x = tile * 0.15f,
                y = groundY - hero * 0.92f,
                width = hero * 0.5f,
                height = hero * 0.86f,
                kind = NpcKind.Elder,
                line = "别急着冲进锁焦圈：先在这条堤上把苔垫、反向履带和鱼干箱摸熟。",
            ),
            LevelNpc(
                x = tile * 2.5f,
                y = groundY - hero * 0.9f,
                width = hero * 0.5f,
                height = hero * 0.84f,
                kind = NpcKind.Scout,
                line = "看见脚下箭头就往那边卸力——和冰盾海豹硬顶只会浪费冲刺。",
            ),
            LevelNpc(
                x = tile * 4.25f,
                y = groundY - hero * 0.4f,
                width = hero * 0.82f,
                height = hero * 0.4f,
                kind = NpcKind.Sign,
                line = "再往前即 Boss 触发线；镜头锁定时记得留鱼干/团援给破盾段。",
            ),
        )

        val friendGoal = FriendGoal(
            x = levelLength - worldWidth * 0.32f,
            groundY = groundY,
            height = worldHeight * 0.35f,
        )
        val bossArena =
            BossArenaSpec(
                triggerAtPlayerX = tile * 5.8f,
                arenaCenterWorldX = tile * 5.75f,
            )

        val presentation = LevelPresentation(
            introTitle = "咕咕嘎嘎 · 北境雾堤（高松鹅试炼）",
            introDescription = "越过冰湖之后，北境的雾像墙一样合拢。前行不久便会踏入一片被锁焦的堤上竞技场，与高松鹅正面对决。\n" +
                "一阶段以落地冲击波压迫走位；二阶段它张开冰盾，需鱼干冲刺或团团掩护时重踏破盾；三阶段整片平台开始变成脆弱薄冰。",
            failHint = "进门前吃够环境道具；Boss 有护盾时别硬踩，用冲刺/团援节奏破盾。",
            victoryTitle = "雾堤试炼通过",
            victoryDescription = "高松鹅第一次败退。雾堤的冷风仍贴在羽毛上，但路已经向前多挪了一步。\n" +
                "营地的补给与团团的掩护会在之后的旅途里更频繁地救你一命。",
            chapterPreviewTitle = null,
            chapterPreviewDescription = null,
            hudGoalLine = "目标：通过雾中堤道热身段 → 触发 Boss；善用苔垫、补给与团援。",
        )

        return LevelContent(
            levelLength = levelLength,
            pits = pits,
            platforms = platforms,
            blocks = blocks,
            enemies = enemies,
            coins = coins,
            friendGoal = friendGoal,
            bossArena = bossArena,
            presentation = presentation,
            sceneTheme =
                StorySceneTheme(
                    outerGradientColors = listOf(
                        Color(0xFF1E2D3D),
                        Color(0xFF2F4552),
                        Color(0xFF4D6575),
                        Color(0xFF9BABC0),
                    ),
                    skyGradientColors = listOf(
                        Color(0xFF2A3A4A),
                        Color(0xFF4A5C6C),
                        Color(0xFF7A8C9A),
                    ),
                    waterColor = Color(0xFF1A2836),
                    groundTopColor = Color(0xFF5A6B62),
                    groundBottomColor = Color(0xFF3A4240),
                    sunCoreColor = Color(0xFFE0E4E8).copy(alpha = 0.85f),
                    sunHaloEdgeColor = Color(0xFFB0B8C0).copy(alpha = 0f),
                    platformTopColor = Color(0xFF7B8C92),
                    platformBottomColor = Color(0xFF4D5960),
                    brickColor = Color(0xFF7D7E84),
                    brickShadeColor = Color(0xFF55575C),
                    questionColor = Color(0xFFD3C39C),
                    questionUsedColor = Color(0xFF8C8A84),
                    questionMarkColor = Color(0xFF45464A),
                    sealBodyColor = Color(0xFF73808A),
                    sealBellyColor = Color(0xFFDCE4E8),
                    birdWingColor = Color(0xFF4C5A65),
                    birdBodyColor = Color(0xFF74828B),
                    fishBodyColor = Color(0xFFFFB38A),
                    fishTailColor = Color(0xFFF8D9B8),
                    hillSnowColor = Color(0xFFCDD6DE),
                    hillStoneColor = Color(0xFF768491),
                    hutWallColor = Color(0x55DCE5EE),
                    hutRoofColor = Color(0xFF8A97A4),
                    hutBeamColor = Color(0xFF5B6770),
                    foregroundMistColor = Color(0xFFA7B7C3),
                    foregroundShardColor = Color(0xFF7A8C9A),
                    snowflakeAlphaBase = 0.18f,
                    snowflakeAlphaStep = 0.05f,
                    cloudCount = 3,
                    ridgeCount = 7,
                    hillCount = 4,
                    hutCount = 1,
                ),
            npcs = npcs,
            worldPickups = worldPickups,
        )
    }
}
