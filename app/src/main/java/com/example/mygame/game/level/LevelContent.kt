package com.example.mygame.game.level

import com.example.mygame.game.Block
import com.example.mygame.game.Coin
import com.example.mygame.game.Enemy
import com.example.mygame.game.FriendGoal
import com.example.mygame.game.Pit
import com.example.mygame.game.Platform

data class LevelPresentation(
    val introTitle: String,
    val introDescription: String,
    val failHint: String,
    val victoryTitle: String,
    val victoryDescription: String,
    val chapterPreviewTitle: String?,
    val chapterPreviewDescription: String?,
    val hudGoalLine: String,
)

data class LevelContent(
    val levelLength: Float,
    val pits: List<Pit>,
    val platforms: List<Platform>,
    val blocks: List<Block>,
    val enemies: List<Enemy>,
    val coins: List<Coin>,
    val friendGoal: FriendGoal,
    val presentation: LevelPresentation,
    val sceneTheme: StorySceneTheme,
)
