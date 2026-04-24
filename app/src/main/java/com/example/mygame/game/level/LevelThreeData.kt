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
 * 第三关「北境雾堤」占位可玩段：短流程试玩，验证主线骨架与 [GameLevel] 延伸。
 * 正式机制与体量化后可在此文件内迭代，无需改 [LevelCatalog] 的注册名。
 */
object LevelThreeData {

    fun build(worldWidth: Float, worldHeight: Float): LevelContent {
        val groundY = worldHeight * 0.82f
        val tile = worldWidth * 0.12f
        val hero = min(worldWidth, worldHeight) * 0.1f
        val levelLength = worldWidth * 3.2f

        val pits = listOf(
            Pit(startX = tile * 4.1f, endX = tile * 5.0f),
        )

        val platforms = listOf(
            Platform(x = tile * 2.4f, y = groundY - worldHeight * 0.2f, width = tile * 1.4f, height = 24f),
            Platform(x = tile * 3.1f, y = groundY - worldHeight * 0.32f, width = tile * 1.2f, height = 22f),
            Platform(x = tile * 5.4f, y = groundY - worldHeight * 0.18f, width = tile * 1.5f, height = 24f),
        )

        val blockSize = tile * 0.48f
        val blockRowY = groundY - worldHeight * 0.24f
        val blocks = listOf(
            Block(x = tile * 1.4f, y = blockRowY, size = blockSize, type = BlockType.Brick),
            Block(x = tile * 1.9f, y = blockRowY, size = blockSize, type = BlockType.Question, reward = BlockReward.Coin),
            Block(x = tile * 2.4f, y = blockRowY, size = blockSize, type = BlockType.Brick),
        )

        val enemies = listOf(
            Enemy(
                x = tile * 4.0f,
                y = groundY - hero * 0.72f,
                width = hero * 0.78f,
                height = hero * 0.72f,
                patrolStart = tile * 3.2f,
                patrolEnd = tile * 4.7f,
                speed = 108f,
                kind = EnemyKind.Seal,
            ),
            Enemy(
                x = tile * 5.5f,
                y = groundY - hero * 1.18f,
                width = hero * 0.68f,
                height = hero * 0.52f,
                patrolStart = tile * 4.85f,
                patrolEnd = tile * 6.15f,
                speed = 118f,
                kind = EnemyKind.Bird,
            ),
        )

        val coins = listOf(
            Coin(x = tile * 2.0f, y = groundY - worldHeight * 0.1f, size = hero * 0.42f),
            Coin(x = tile * 3.5f, y = groundY - worldHeight * 0.38f, size = hero * 0.42f),
            Coin(x = tile * 5.9f, y = groundY - worldHeight * 0.2f, size = hero * 0.42f),
        )

        val friendGoal = FriendGoal(
            x = levelLength - worldWidth * 0.32f,
            groundY = groundY,
            height = worldHeight * 0.35f,
        )

        val presentation = LevelPresentation(
            introTitle = "咕咕嘎嘎 · 北境雾堤（试玩段）",
            introDescription = "越过冰湖之后，北境的风突然变得安静，只剩下雾堤像一道灰白色的墙横在前方。\n" +
                "这一段还是试玩区，但已经能看见第三章真正的气质：更窄的落点、更危险的视野和更沉的压迫感。",
            failHint = "这里更考验落点判断和节奏控制，别急着冲，先把平台节奏读清楚。",
            victoryTitle = "试玩段抵达",
            victoryDescription = "你们暂时停在雾堤外缘，脚下是冷硬的堤石，前方则是看不清尽头的雾海。\n完整第三关和新的剧情事件还在制作中，但追逐高松鹅的方向已经越来越明确。",
            chapterPreviewTitle = null,
            chapterPreviewDescription = null,
            hudGoalLine = "目标：试玩段 · 穿越雾影堤岸，抵达外缘营地点",
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
        )
    }
}
