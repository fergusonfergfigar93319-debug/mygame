package com.example.mygame.ui

import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.padding
import androidx.compose.material3.Button
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.Surface
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.setValue
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.platform.LocalContext
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.dp
import com.example.mygame.data.LocalLeaderboardRepository
import com.example.mygame.data.SaveRepository
import com.example.mygame.game.GuguGagaGame
import com.example.mygame.game.modes.EndlessMode
import com.example.mygame.ui.leaderboard.LeaderboardScreen

private enum class AppScreen {
    Home,
    Story,
    Endless,
    Leaderboard,
}

@Composable
fun GameRoot(modifier: Modifier = Modifier) {
    val context = LocalContext.current
    val saveRepository = remember { SaveRepository(context) }
    val leaderboardRepository = remember { LocalLeaderboardRepository(context) }
    var screen by remember { mutableStateOf(AppScreen.Home) }

    Surface(modifier = modifier.fillMaxSize()) {
        when (screen) {
            AppScreen.Home -> HomeMenu(
                onStory = { screen = AppScreen.Story },
                onEndless = { screen = AppScreen.Endless },
                onLeaderboard = { screen = AppScreen.Leaderboard },
            )
            AppScreen.Story -> GuguGagaGame(onExitToMenu = { screen = AppScreen.Home })
            AppScreen.Endless -> EndlessMode(
                saveRepository = saveRepository,
                leaderboardRepository = leaderboardRepository,
                onExitToMenu = { screen = AppScreen.Home },
            )
            AppScreen.Leaderboard -> LeaderboardScreen(
                leaderboardRepository = leaderboardRepository,
                saveRepository = saveRepository,
                onBack = { screen = AppScreen.Home },
            )
        }
    }
}

@Composable
private fun HomeMenu(
    onStory: () -> Unit,
    onEndless: () -> Unit,
    onLeaderboard: () -> Unit,
) {
    Column(
        modifier = Modifier
            .fillMaxSize()
            .padding(24.dp),
        verticalArrangement = Arrangement.spacedBy(16.dp, Alignment.CenterVertically),
        horizontalAlignment = Alignment.CenterHorizontally,
    ) {
        Text(
            "咕咕嘎嘎",
            style = MaterialTheme.typography.headlineLarge,
            fontWeight = FontWeight.Bold,
        )
        Text(
            "选择模式",
            style = MaterialTheme.typography.titleMedium,
            color = MaterialTheme.colorScheme.onSurfaceVariant,
        )
        Button(onClick = onStory, modifier = Modifier.fillMaxWidth(0.88f)) {
            Text("主线模式")
        }
        Button(onClick = onEndless, modifier = Modifier.fillMaxWidth(0.88f)) {
            Text("极夜漂流（无尽）")
        }
        Button(onClick = onLeaderboard, modifier = Modifier.fillMaxWidth(0.88f)) {
            Text("排行榜")
        }
    }
}
