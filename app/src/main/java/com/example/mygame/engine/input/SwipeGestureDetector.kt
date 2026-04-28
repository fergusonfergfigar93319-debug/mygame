package com.example.mygame.engine.input

import android.view.MotionEvent
import android.view.View
import kotlin.math.abs

enum class SwipeDirection { Up, Down, Left, Right, Tap }

class SwipeGestureDetector(
    private val onSwipe: (SwipeDirection) -> Unit,
    private val swipeThreshold: Float = 80f,
    private val tapThreshold: Float = 20f,
) : View.OnTouchListener {

    private var startX = 0f
    private var startY = 0f

    override fun onTouch(v: View, event: MotionEvent): Boolean {
        when (event.actionMasked) {
            MotionEvent.ACTION_DOWN -> {
                startX = event.x
                startY = event.y
                return true
            }
            MotionEvent.ACTION_UP -> {
                val dx = event.x - startX
                val dy = event.y - startY
                val absDx = abs(dx)
                val absDy = abs(dy)

                if (absDx < tapThreshold && absDy < tapThreshold) {
                    onSwipe(SwipeDirection.Tap)
                } else if (absDx > absDy && absDx >= swipeThreshold) {
                    onSwipe(if (dx > 0) SwipeDirection.Right else SwipeDirection.Left)
                } else if (absDy >= swipeThreshold) {
                    onSwipe(if (dy > 0) SwipeDirection.Down else SwipeDirection.Up)
                }
                return true
            }
        }
        return false
    }
}
