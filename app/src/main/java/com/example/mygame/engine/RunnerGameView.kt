package com.example.mygame.engine

import android.annotation.SuppressLint
import android.opengl.GLSurfaceView
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.runtime.Composable
import androidx.compose.runtime.DisposableEffect
import androidx.compose.runtime.remember
import androidx.compose.ui.Modifier
import androidx.compose.ui.platform.LocalContext
import androidx.compose.ui.viewinterop.AndroidView
import com.example.mygame.engine.input.SwipeDirection
import com.example.mygame.engine.input.SwipeGestureDetector

@SuppressLint("ClickableViewAccessibility")
@Composable
fun RunnerGameView(
    gameState: RunnerGameState,
    onSoundEvent: ((RunnerRenderer.SoundEvent) -> Unit)? = null,
    modifier: Modifier = Modifier,
    overlay: @Composable (restartRun: () -> Unit) -> Unit = { _ -> },
) {
    val renderer = remember { RunnerRenderer(gameState, onSoundEvent) }
    val restartRun: () -> Unit = {
        renderer.enqueueAction(RunnerRenderer.Action.Start)
    }

    Box(modifier = modifier.fillMaxSize()) {
        AndroidView(
            factory = { ctx ->
                GLSurfaceView(ctx).apply {
                    setEGLContextClientVersion(2)
                    setRenderer(renderer)
                    renderMode = GLSurfaceView.RENDERMODE_CONTINUOUSLY

                    val swipeDetector = SwipeGestureDetector(
                        onSwipe = { dir ->
                            when {
                                !renderer.isRunning && !renderer.isGameOver -> {
                                    renderer.enqueueAction(RunnerRenderer.Action.Start)
                                }
                                renderer.isGameOver -> {
                                    renderer.enqueueAction(RunnerRenderer.Action.Start)
                                }
                                else -> when (dir) {
                                    SwipeDirection.Up, SwipeDirection.Tap ->
                                        renderer.enqueueAction(RunnerRenderer.Action.Jump)
                                    SwipeDirection.Down ->
                                        renderer.enqueueAction(RunnerRenderer.Action.Slide)
                                    SwipeDirection.Left ->
                                        renderer.enqueueAction(RunnerRenderer.Action.MoveLeft)
                                    SwipeDirection.Right ->
                                        renderer.enqueueAction(RunnerRenderer.Action.MoveRight)
                                }
                            }
                        },
                    )
                    setOnTouchListener(swipeDetector)
                }
            },
            modifier = Modifier.fillMaxSize(),
        )
        overlay(restartRun)
    }
}
