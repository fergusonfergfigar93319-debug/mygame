package com.example.mygame

import android.os.Bundle
import androidx.activity.ComponentActivity
import androidx.activity.compose.setContent
import androidx.activity.enableEdgeToEdge
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.material3.Surface
import androidx.compose.ui.Modifier
import com.example.mygame.audio.SoundManager
import com.example.mygame.ui.GameRoot
import com.example.mygame.ui.theme.MyGameTheme

class MainActivity : ComponentActivity() {

    private lateinit var soundManager: SoundManager

    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        soundManager = SoundManager(applicationContext)
        enableEdgeToEdge()
        setContent {
            MyGameTheme(dynamicColor = false) {
                Surface(modifier = Modifier.fillMaxSize()) {
                    GameRoot(soundManager = soundManager)
                }
            }
        }
    }

    override fun onPause() {
        if (::soundManager.isInitialized) {
            soundManager.pauseBgm()
        }
        super.onPause()
    }

    override fun onResume() {
        super.onResume()
        if (::soundManager.isInitialized) {
            soundManager.resumeBgm()
        }
    }

    override fun onDestroy() {
        if (::soundManager.isInitialized) {
            soundManager.release()
        }
        super.onDestroy()
    }
}
