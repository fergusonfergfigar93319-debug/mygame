package com.example.mygame

import android.os.Bundle
import androidx.activity.ComponentActivity
import androidx.activity.compose.setContent
import androidx.activity.enableEdgeToEdge
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.material3.Surface
import androidx.compose.ui.Modifier
import com.example.mygame.ui.GameRoot
import com.example.mygame.ui.theme.MyGameTheme

class MainActivity : ComponentActivity() {
    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        enableEdgeToEdge()
        setContent {
            MyGameTheme(dynamicColor = false) {
                Surface(modifier = Modifier.fillMaxSize()) {
                    GameRoot()
                }
            }
        }
    }
}
