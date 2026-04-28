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

object LevelOneData {

    fun build(worldWidth: Float, worldHeight: Float): LevelContent {
        val groundY = worldHeight * 0.82f
        val tile = worldWidth * 0.12f
        val hero = min(worldWidth, worldHeight) * 0.1f
        val levelLength = worldWidth * 6.55f

        val pits = listOf(
            Pit(startX = tile * 6.65f, endX = tile * 7.42f),
            Pit(startX = tile * 11.95f, endX = tile * 12.78f),
            Pit(startX = tile * 16.35f, endX = tile * 17.2f),
            Pit(startX = tile * 22.1f, endX = tile * 22.88f),
            Pit(startX = tile * 30.2f, endX = tile * 30.95f),
        )

        val platforms = listOf(
            Platform(x = tile * 3.0f, y = groundY - worldHeight * 0.18f, width = tile * 1.5f, height = 24f),
            Platform(x = tile * 4.75f, y = groundY - worldHeight * 0.25f, width = tile * 1.55f, height = 24f),
            // 薄冰教学：正下方为实地，碎板后安全落回地面
            Platform(
                x = tile * 5.35f,
                y = groundY - worldHeight * 0.12f,
                width = tile * 0.95f,
                height = 22f,
                isFragile = true,
            ),
            Platform(
                x = tile * 5.95f,
                y = groundY - worldHeight * 0.15f,
                width = tile * 0.88f,
                height = 22f,
                isFragile = true,
            ),
            Platform(x = tile * 7.85f, y = groundY - worldHeight * 0.18f, width = tile * 2.05f, height = 24f),
            Platform(x = tile * 9.6f, y = groundY - worldHeight * 0.27f, width = tile * 1.65f, height = 24f),
            Platform(x = tile * 10.1f, y = groundY - 10f, width = tile * 2.4f, height = 16f, surfaceFriction = 0.9f, conveyorBelt = 70f),
            Platform(x = tile * 13.05f, y = groundY - worldHeight * 0.2f, width = tile * 2.15f, height = 24f),
            // 起伏：缓坡高台
            Platform(x = tile * 12.1f, y = groundY - worldHeight * 0.11f, width = tile * 0.8f, height = 20f, surfaceFriction = 0.95f),
            Platform(x = tile * 14.8f, y = groundY - worldHeight * 0.14f, width = tile * 0.9f, height = 20f, surfaceFriction = 0.95f),
            Platform(x = tile * 17.6f, y = groundY - worldHeight * 0.22f, width = tile * 1.5f, height = 24f),
            Platform(x = tile * 19.0f, y = groundY - worldHeight * 0.3f, width = tile * 1.45f, height = 22f),
            Platform(
                x = tile * 19.4f,
                y = groundY - worldHeight * 0.1f,
                width = tile * 0.9f,
                height = 20f,
                bounceImpulse = 720f,
            ),
            Platform(x = tile * 20.2f, y = groundY - worldHeight * 0.24f, width = tile * 1.75f, height = 24f),
            Platform(x = tile * 22.4f, y = groundY - worldHeight * 0.19f, width = tile * 1.6f, height = 24f),
            Platform(x = tile * 24.0f, y = groundY - worldHeight * 0.28f, width = tile * 1.2f, height = 22f, surfaceFriction = 0.5f, isFragile = true),
            Platform(x = tile * 25.0f, y = groundY - worldHeight * 0.35f, width = tile * 1.4f, height = 22f),
            Platform(x = tile * 27.0f, y = groundY - worldHeight * 0.2f, width = tile * 2.0f, height = 24f),
            Platform(x = tile * 29.0f, y = groundY - 11f, width = tile * 2.6f, height = 18f, surfaceFriction = 0.88f, conveyorBelt = -58f),
            Platform(x = tile * 31.5f, y = groundY - worldHeight * 0.18f, width = tile * 1.85f, height = 24f),
            Platform(x = tile * 33.0f, y = groundY - worldHeight * 0.3f, width = tile * 1.5f, height = 22f),
            Platform(x = tile * 35.0f, y = groundY - worldHeight * 0.16f, width = tile * 1.7f, height = 24f),
            Platform(x = tile * 36.6f, y = groundY - worldHeight * 0.24f, width = tile * 1.35f, height = 22f),
            Platform(x = tile * 38.5f, y = groundY - worldHeight * 0.32f, width = tile * 1.5f, height = 22f),
        )

        val blockSize = tile * 0.48f
        val blockRowY = groundY - worldHeight * 0.26f
        val blocks = listOf(
            Block(x = tile * 2.7f, y = blockRowY, size = blockSize, type = BlockType.Brick),
            Block(x = tile * 3.25f, y = blockRowY, size = blockSize, type = BlockType.Question, reward = BlockReward.Shield),
            Block(x = tile * 3.8f, y = blockRowY, size = blockSize, type = BlockType.Brick),
            Block(x = tile * 8.45f, y = groundY - worldHeight * 0.32f, size = blockSize, type = BlockType.Question, reward = BlockReward.Fish),
            Block(x = tile * 9.0f, y = groundY - worldHeight * 0.32f, size = blockSize, type = BlockType.Brick),
            Block(x = tile * 12.9f, y = groundY - worldHeight * 0.22f, size = blockSize, type = BlockType.Question, reward = BlockReward.Boots),
            Block(x = tile * 13.75f, y = groundY - worldHeight * 0.33f, size = blockSize, type = BlockType.Question, reward = BlockReward.Scarf),
            Block(x = tile * 15.05f, y = groundY - worldHeight * 0.29f, size = blockSize, type = BlockType.Question, reward = BlockReward.Magnet),
            Block(x = tile * 18.0f, y = groundY - worldHeight * 0.36f, size = blockSize, type = BlockType.Question, reward = BlockReward.Coin),
            Block(x = tile * 20.5f, y = groundY - worldHeight * 0.34f, size = blockSize, type = BlockType.Brick),
            Block(x = tile * 20.9f, y = groundY - worldHeight * 0.34f, size = blockSize, type = BlockType.Question, reward = BlockReward.Fish),
            Block(x = tile * 23.2f, y = groundY - worldHeight * 0.32f, size = blockSize, type = BlockType.Question, reward = BlockReward.Shield),
            Block(x = tile * 26.0f, y = groundY - worldHeight * 0.3f, size = blockSize, type = BlockType.Question, reward = BlockReward.Coin),
            Block(x = tile * 28.5f, y = groundY - worldHeight * 0.28f, size = blockSize, type = BlockType.Question, reward = BlockReward.Fish),
            Block(x = tile * 32.0f, y = groundY - worldHeight * 0.3f, size = blockSize, type = BlockType.Question, reward = BlockReward.Boots),
            Block(x = tile * 35.2f, y = groundY - worldHeight * 0.31f, size = blockSize, type = BlockType.Question, reward = BlockReward.Magnet),
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
            ),
            Enemy(
                x = tile * 18.5f,
                y = groundY - hero * 0.72f,
                width = hero * 0.78f,
                height = hero * 0.72f,
                patrolStart = tile * 17.8f,
                patrolEnd = tile * 19.8f,
                speed = 110f,
                kind = EnemyKind.Seal
            ),
            Enemy(
                x = tile * 25.0f,
                y = groundY - worldHeight * 0.3f - hero * 0.72f,
                width = hero * 0.8f,
                height = hero * 0.72f,
                patrolStart = tile * 24.2f,
                patrolEnd = tile * 26.2f,
                speed = 102f,
                kind = EnemyKind.SpikedSeal
            ),
            Enemy(
                x = tile * 33.0f,
                y = groundY - hero * 0.7f,
                width = hero * 0.7f,
                height = hero * 0.5f,
                patrolStart = tile * 32.0f,
                patrolEnd = tile * 33.6f,
                speed = 100f,
                kind = EnemyKind.SnowMole
            ),
            Enemy(
                x = tile * 37.0f,
                y = groundY - hero * 1.2f,
                width = hero * 0.68f,
                height = hero * 0.5f,
                patrolStart = tile * 35.5f,
                patrolEnd = tile * 38.0f,
                speed = 125f,
                kind = EnemyKind.Bird
            ),
        )

        val c = hero * 0.42f
        val coins = listOf(
            Coin(x = tile * 2.2f, y = groundY - worldHeight * 0.12f, size = c),
            Coin(x = tile * 5.3f, y = groundY - worldHeight * 0.4f, size = c),
            Coin(x = tile * 6.18f, y = groundY - worldHeight * 0.27f, size = c, kind = CoinKind.LorePage),
            Coin(x = tile * 8.8f, y = groundY - worldHeight * 0.48f, size = c),
            Coin(x = tile * 10.5f, y = groundY - worldHeight * 0.46f, size = c),
            Coin(x = tile * 11.15f, y = groundY - worldHeight * 0.5f, size = c, kind = CoinKind.Beacon),
            Coin(x = tile * 13.7f, y = groundY - worldHeight * 0.34f, size = c),
            Coin(x = tile * 15.2f, y = groundY - worldHeight * 0.18f, size = c),
            Coin(x = tile * 16.0f, y = groundY - worldHeight * 0.15f, size = c),
            Coin(x = tile * 18.3f, y = groundY - worldHeight * 0.38f, size = c),
            Coin(x = tile * 21.2f, y = groundY - worldHeight * 0.44f, size = c),
            Coin(x = tile * 23.8f, y = groundY - worldHeight * 0.36f, size = c),
            Coin(x = tile * 26.5f, y = groundY - worldHeight * 0.2f, size = c),
            Coin(x = tile * 28.0f, y = groundY - worldHeight * 0.34f, size = c, kind = CoinKind.Beacon),
            Coin(x = tile * 30.0f, y = groundY - worldHeight * 0.3f, size = c),
            Coin(x = tile * 32.5f, y = groundY - worldHeight * 0.22f, size = c),
            Coin(x = tile * 35.0f, y = groundY - worldHeight * 0.2f, size = c),
            Coin(x = tile * 37.0f, y = groundY - worldHeight * 0.28f, size = c),
        )

        val worldPickups = listOf(
            WorldPickup(x = tile * 1.15f, y = groundY - hero * 0.2f, size = hero * 0.28f, kind = WorldPickupKind.Snowberry),
            WorldPickup(x = tile * 5.0f, y = groundY - worldHeight * 0.2f, size = hero * 0.28f, kind = WorldPickupKind.GustSeed),
            WorldPickup(x = tile * 11.4f, y = groundY - worldHeight * 0.2f, size = hero * 0.28f, kind = WorldPickupKind.Snowberry),
            WorldPickup(x = tile * 17.0f, y = groundY - worldHeight * 0.2f, size = hero * 0.28f, kind = WorldPickupKind.GustSeed),
            WorldPickup(x = tile * 24.5f, y = groundY - worldHeight * 0.2f, size = hero * 0.28f, kind = WorldPickupKind.GlintFragment),
            WorldPickup(x = tile * 29.0f, y = groundY - 14f, size = hero * 0.28f, kind = WorldPickupKind.Snowberry),
            WorldPickup(x = tile * 33.0f, y = groundY - worldHeight * 0.2f, size = hero * 0.28f, kind = WorldPickupKind.Snowberry),
        )

        val npcs = listOf(
            LevelNpc(
                x = tile * 0.55f,
                y = groundY - hero * 0.95f,
                width = hero * 0.52f,
                height = hero * 0.88f,
                kind = NpcKind.Elder,
                line = "这儿曾是村口的集市。顺着履带走到对岸，比硬跳坑稳多了。",
            ),
            LevelNpc(
                x = tile * 7.5f,
                y = groundY - hero * 0.88f,
                width = hero * 0.5f,
                height = hero * 0.82f,
                kind = NpcKind.Villager,
                line = "青苔藓垫会把你弹向高处——别慌，是村里修栈桥时留下的老机关。",
            ),
            LevelNpc(
                x = tile * 22.2f,
                y = groundY - hero * 0.1f,
                width = hero * 0.85f,
                height = hero * 0.35f,
                kind = NpcKind.Sign,
                line = "前方裂隙多，先踩薄冰练脚感，再进巡逻区。",
            ),
        )

        val friendGoal = FriendGoal(
            x = levelLength - worldWidth * 0.32f,
            groundY = groundY,
            height = worldHeight * 0.35f
        )

        val presentation = LevelPresentation(
            introTitle = "咕咕嘎嘎 雪松村废墟",
            introDescription = "雪松村的灯火已经熄灭，只剩断掉的栈桥、被掀翻的小屋和满地风雪。\n咕咕嘎嘎知道团团还活着，于是她决定穿过废墟，把第一位伙伴从混乱中带回来。",
            failHint = "注意履带和弹簧苔垫的节奏；用环境拾取物补一段长跳或加分，比硬冲敌人更稳。",
            victoryTitle = "成功救回团团",
            victoryDescription = "在废墟尽头，咕咕嘎嘎终于找到了团团。熟悉的呼喊声重新回到耳边，旅途第一次不再只剩自己一个。\n两只小企鹅立刻决定继续北上，因为高松鹅留下的足迹正通往冰湖回音谷。",
            chapterPreviewTitle = "第二关预告：冰湖回音谷",
            chapterPreviewDescription = "团团归队后，队伍第一次拥有了真正的支援能力。\n下一站是风声会回响的冰湖回音谷，那里可能藏着新的伙伴消息，也可能藏着高松鹅提前布下的埋伏。",
            hudGoalLine = "目标：穿过加长废墟带，利用履带/苔垫与村民提示，救回团团"
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
            npcs = npcs,
            worldPickups = worldPickups,
        )
    }
}
