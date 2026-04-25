package com.example.mygame.ui.common

import androidx.compose.foundation.background
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.Spacer
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.width
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.automirrored.rounded.ExitToApp
import androidx.compose.material.icons.automirrored.rounded.VolumeOff
import androidx.compose.material.icons.automirrored.rounded.VolumeUp
import androidx.compose.material.icons.rounded.MusicNote
import androidx.compose.material.icons.rounded.MusicOff
import androidx.compose.material.icons.rounded.PlayArrow
import androidx.compose.material3.Button
import androidx.compose.material3.ButtonDefaults
import androidx.compose.material3.Card
import androidx.compose.material3.CardDefaults
import androidx.compose.material3.Icon
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.OutlinedButton
import androidx.compose.material3.Text
import androidx.compose.material3.TextButton
import androidx.compose.runtime.Composable
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.setValue
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.dp
import com.example.mygame.audio.SoundManager
import com.example.mygame.data.SaveRepository

@Composable
fun PauseOverlay(
    onResume: () -> Unit,
    onQuit: () -> Unit,
    soundManager: SoundManager?,
    saveRepository: SaveRepository,
) {
    var bgmEnabled by remember { mutableStateOf(saveRepository.getBgmEnabled()) }
    var sfxEnabled by remember { mutableStateOf(saveRepository.getSfxEnabled()) }

    Box(
        modifier =
            Modifier
                .fillMaxSize()
                .background(Color(0xA60B1420)),
        contentAlignment = Alignment.Center,
    ) {
        Card(
            modifier = Modifier.width(300.dp),
            shape = RoundedCornerShape(22.dp),
            colors =
                CardDefaults.cardColors(
                    containerColor = Color(0xFFF9FCFF).copy(alpha = 0.96f),
                ),
            elevation = CardDefaults.cardElevation(defaultElevation = 8.dp),
        ) {
            Column(
                modifier = Modifier.padding(24.dp),
                horizontalAlignment = Alignment.CenterHorizontally,
                verticalArrangement = Arrangement.spacedBy(16.dp),
            ) {
                Text(
                    text = "游戏暂停",
                    style = MaterialTheme.typography.headlineSmall,
                    fontWeight = FontWeight.ExtraBold,
                    color = Color(0xFF183247),
                )

                Spacer(modifier = Modifier.height(4.dp))

                Button(
                    onClick = onResume,
                    modifier =
                        Modifier
                            .fillMaxWidth()
                            .height(54.dp),
                    colors = ButtonDefaults.buttonColors(containerColor = Color(0xFF23405E)),
                    shape = RoundedCornerShape(16.dp),
                ) {
                    Icon(Icons.Rounded.PlayArrow, contentDescription = null)
                    Spacer(modifier = Modifier.width(8.dp))
                    Text("继续旅程", style = MaterialTheme.typography.titleMedium)
                }

                Row(
                    modifier = Modifier.fillMaxWidth(),
                    horizontalArrangement = Arrangement.spacedBy(12.dp),
                ) {
                    OutlinedButton(
                        onClick = {
                            if (soundManager == null) return@OutlinedButton
                            bgmEnabled = !bgmEnabled
                            saveRepository.setBgmEnabled(bgmEnabled)
                            soundManager.setBgmEnabled(bgmEnabled)
                        },
                        modifier =
                            Modifier
                                .weight(1f)
                                .height(50.dp),
                        enabled = soundManager != null,
                        shape = RoundedCornerShape(12.dp),
                    ) {
                        Icon(
                            imageVector = if (bgmEnabled) Icons.Rounded.MusicNote else Icons.Rounded.MusicOff,
                            contentDescription = "切换背景音乐",
                            tint =
                                if (soundManager == null) {
                                    Color(0xFFB0BEC5)
                                } else if (bgmEnabled) {
                                    Color(0xFF23405E)
                                } else {
                                    Color(0xFF90A4AE)
                                },
                        )
                    }
                    OutlinedButton(
                        onClick = {
                            if (soundManager == null) return@OutlinedButton
                            sfxEnabled = !sfxEnabled
                            saveRepository.setSfxEnabled(sfxEnabled)
                            soundManager.setSfxEnabled(sfxEnabled)
                        },
                        modifier =
                            Modifier
                                .weight(1f)
                                .height(50.dp),
                        enabled = soundManager != null,
                        shape = RoundedCornerShape(12.dp),
                    ) {
                        Icon(
                            imageVector =
                                if (sfxEnabled) {
                                    Icons.AutoMirrored.Rounded.VolumeUp
                                } else {
                                    Icons.AutoMirrored.Rounded.VolumeOff
                                },
                            contentDescription = "切换音效",
                            tint =
                                if (soundManager == null) {
                                    Color(0xFFB0BEC5)
                                } else if (sfxEnabled) {
                                    Color(0xFF23405E)
                                } else {
                                    Color(0xFF90A4AE)
                                },
                        )
                    }
                }

                Spacer(modifier = Modifier.height(4.dp))

                TextButton(
                    onClick = onQuit,
                    modifier =
                        Modifier
                            .fillMaxWidth()
                            .height(50.dp),
                ) {
                    Icon(
                        Icons.AutoMirrored.Rounded.ExitToApp,
                        contentDescription = null,
                        tint = Color(0xFFD32F2F),
                    )
                    Spacer(modifier = Modifier.width(8.dp))
                    Text("放弃并返回", color = Color(0xFFD32F2F), fontWeight = FontWeight.Bold)
                }
            }
        }
    }
}
