package com.example.mygame.ui

import androidx.compose.animation.core.Animatable
import androidx.compose.animation.core.FastOutSlowInEasing
import androidx.compose.animation.core.tween
import androidx.compose.foundation.background
import androidx.compose.foundation.clickable
import androidx.compose.foundation.interaction.MutableInteractionSource
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.Surface
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.runtime.LaunchedEffect
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.rememberCoroutineScope
import androidx.compose.runtime.setValue
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.platform.LocalContext
import com.example.mygame.audio.SoundManager
import com.example.mygame.data.LocalLeaderboardRepository
import com.example.mygame.data.SaveRepository
import com.example.mygame.game.GuguGagaGame
import com.example.mygame.game.modes.EndlessMode
import com.example.mygame.game.modes.EndlessRunPreset
import com.example.mygame.ui.home.CampScreen
import com.example.mygame.ui.home.CodexScreen
import com.example.mygame.ui.home.HomeMenu
import com.example.mygame.ui.leaderboard.LeaderboardScreen
import kotlinx.coroutines.launch

private enum class AppScreen {
    Home,
    Story,
    Endless,
    EndlessDaily,
    Leaderboard,
    Codex,
    Camp,
}

@Composable
fun GameRoot(
    soundManager: SoundManager,
    modifier: Modifier = Modifier,
) {
    val context = LocalContext.current
    val saveRepository = remember { SaveRepository(context) }
    val leaderboardRepository = remember { LocalLeaderboardRepository(context) }
    var screen by remember { mutableStateOf(AppScreen.Home) }

    val scope = rememberCoroutineScope()
    val transitionAlpha = remember { Animatable(0f) }
    var isTransitioning by remember { mutableStateOf(false) }
    val transitionOverlayInteraction = remember { MutableInteractionSource() }

    fun navigateWithTransition(target: AppScreen) {
        if (isTransitioning || screen == target) return
        scope.launch {
            isTransitioning = true
            transitionAlpha.animateTo(1f, tween(360, easing = FastOutSlowInEasing))
            screen = target
            transitionAlpha.animateTo(0f, tween(460, easing = FastOutSlowInEasing))
            isTransitioning = false
        }
    }

    LaunchedEffect(Unit) {
        soundManager.syncAudioFromSave(saveRepository)
    }

    LaunchedEffect(screen) {
        when (screen) {
            AppScreen.Story -> soundManager.setBgm(SoundManager.BgmTrack.Story)
            AppScreen.Endless, AppScreen.EndlessDaily -> soundManager.setBgm(SoundManager.BgmTrack.Endless)
            else -> soundManager.setBgm(SoundManager.BgmTrack.None)
        }
    }

    Surface(modifier = modifier.fillMaxSize()) {
        Box(modifier = Modifier.fillMaxSize()) {
            when (screen) {
                AppScreen.Home ->
                    HomeMenu(
                        saveRepository = saveRepository,
                        soundManager = soundManager,
                        onStory = { navigateWithTransition(AppScreen.Story) },
                        onEndless = { navigateWithTransition(AppScreen.Endless) },
                        onEndlessDaily = { navigateWithTransition(AppScreen.EndlessDaily) },
                        onLeaderboard = { navigateWithTransition(AppScreen.Leaderboard) },
                        onCodex = { navigateWithTransition(AppScreen.Codex) },
                        onCamp = { navigateWithTransition(AppScreen.Camp) },
                    )

                AppScreen.Story ->
                    GuguGagaGame(
                        soundManager = soundManager,
                        onExitToMenu = { navigateWithTransition(AppScreen.Home) },
                    )

                AppScreen.Endless ->
                    EndlessMode(
                        saveRepository = saveRepository,
                        leaderboardRepository = leaderboardRepository,
                        soundManager = soundManager,
                        onExitToMenu = { navigateWithTransition(AppScreen.Home) },
                        runPreset = EndlessRunPreset.Casual,
                    )

                AppScreen.EndlessDaily ->
                    EndlessMode(
                        saveRepository = saveRepository,
                        leaderboardRepository = leaderboardRepository,
                        soundManager = soundManager,
                        onExitToMenu = { navigateWithTransition(AppScreen.Home) },
                        runPreset = EndlessRunPreset.DailyChallenge,
                    )

                AppScreen.Leaderboard ->
                    LeaderboardScreen(
                        leaderboardRepository = leaderboardRepository,
                        saveRepository = saveRepository,
                        onBack = { navigateWithTransition(AppScreen.Home) },
                    )

                AppScreen.Codex ->
                    CodexScreen(
                        saveRepository = saveRepository,
                        onBack = { navigateWithTransition(AppScreen.Home) },
                    )

                AppScreen.Camp ->
                    CampScreen(
                        saveRepository = saveRepository,
                        onBack = { navigateWithTransition(AppScreen.Home) },
                    )
            }

            if (isTransitioning || transitionAlpha.value > 0f) {
                val curtain = transitionAlpha.value.coerceIn(0f, 1f)
                Box(
                    modifier =
                        Modifier
                            .fillMaxSize()
                            .background(Color(0xFF07111E).copy(alpha = curtain))
                            .clickable(
                                interactionSource = transitionOverlayInteraction,
                                indication = null,
                                onClick = {},
                            ),
                    contentAlignment = Alignment.Center,
                ) {
                    if (curtain > 0.82f) {
                        Text(
                            text = "风雪呼啸...",
                            color = Color.White.copy(alpha = ((curtain - 0.82f) / 0.18f).coerceIn(0f, 1f) * 0.85f),
                            style = MaterialTheme.typography.labelLarge,
                        )
                    }
                }
            }
        }
    }
}
