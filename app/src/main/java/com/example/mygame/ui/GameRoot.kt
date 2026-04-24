package com.example.mygame.ui

import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.material3.Surface
import androidx.compose.runtime.Composable
import androidx.compose.runtime.LaunchedEffect
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.setValue
import androidx.compose.ui.Modifier
import androidx.compose.ui.platform.LocalContext
import com.example.mygame.audio.SoundManager
import com.example.mygame.data.LocalLeaderboardRepository
import com.example.mygame.data.SaveRepository
import com.example.mygame.game.GuguGagaGame
import com.example.mygame.game.modes.EndlessMode
import com.example.mygame.game.modes.EndlessRunPreset
import com.example.mygame.ui.home.CodexScreen
import com.example.mygame.ui.home.HomeMenu
import com.example.mygame.ui.leaderboard.LeaderboardScreen

private enum class AppScreen {
    Home,
    Story,
    Endless,
    EndlessDaily,
    Leaderboard,
    Codex,
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

    LaunchedEffect(screen) {
        when (screen) {
            AppScreen.Story -> soundManager.setBgm(SoundManager.BgmTrack.Story)
            AppScreen.Endless, AppScreen.EndlessDaily -> soundManager.setBgm(SoundManager.BgmTrack.Endless)
            else -> soundManager.setBgm(SoundManager.BgmTrack.None)
        }
    }

    Surface(modifier = modifier.fillMaxSize()) {
        when (screen) {
            AppScreen.Home -> HomeMenu(
                saveRepository = saveRepository,
                onStory = { screen = AppScreen.Story },
                onEndless = { screen = AppScreen.Endless },
                onEndlessDaily = { screen = AppScreen.EndlessDaily },
                onLeaderboard = { screen = AppScreen.Leaderboard },
                onCodex = { screen = AppScreen.Codex },
            )

            AppScreen.Story -> GuguGagaGame(
                soundManager = soundManager,
                onExitToMenu = { screen = AppScreen.Home },
            )

            AppScreen.Endless -> EndlessMode(
                saveRepository = saveRepository,
                leaderboardRepository = leaderboardRepository,
                soundManager = soundManager,
                onExitToMenu = { screen = AppScreen.Home },
                runPreset = EndlessRunPreset.Casual,
            )

            AppScreen.EndlessDaily -> EndlessMode(
                saveRepository = saveRepository,
                leaderboardRepository = leaderboardRepository,
                soundManager = soundManager,
                onExitToMenu = { screen = AppScreen.Home },
                runPreset = EndlessRunPreset.DailyChallenge,
            )

            AppScreen.Leaderboard -> LeaderboardScreen(
                leaderboardRepository = leaderboardRepository,
                saveRepository = saveRepository,
                onBack = { screen = AppScreen.Home },
            )

            AppScreen.Codex -> CodexScreen(
                saveRepository = saveRepository,
                onBack = { screen = AppScreen.Home },
            )
        }
    }
}
