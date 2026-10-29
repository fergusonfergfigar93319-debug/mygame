using UnityEngine;

namespace PenguinRun
{
    public enum RunnerAction
    {
        None,
        Jump,
        Slide,
        Left,
        Right,
        Dash,
        Start,
        Pause,
    }

    internal sealed class RunnerInput
    {
        private Vector2 start;
        private bool consumed;
        private bool active;
        private Vector2 lastTapPos;
        private float lastTapTime = -9f;
        private int activeFingerId = -1;
        private int resetFrame = -1;
        private bool touchBlocked;
        private bool hadTouches;
        private bool mouseBlocked;

        public void Reset()
        {
            CancelPointer();
            // A pause/resume callback must not re-arm contacts still on screen.
            touchBlocked |= Input.touchCount > 0;
            mouseBlocked |= hadTouches || Input.touchCount > 0 ||
                Input.GetMouseButton(0) || Input.GetMouseButtonDown(0) || Input.GetMouseButtonUp(0);
            resetFrame = Time.frameCount;
        }

        private void CancelPointer()
        {
            active = false;
            consumed = false;
            activeFingerId = -1;
            lastTapTime = -9f;
            lastTapPos = Vector2.zero;
        }

        public RunnerAction Poll()
        {
            if (resetFrame == Time.frameCount) return RunnerAction.None;
            var touchCount = Input.touchCount;
            if (touchCount == 0)
            {
                touchBlocked = false;
                if (hadTouches)
                {
                    // A completed tap retains its double-tap history; a lost contact does not.
                    if (active) CancelPointer();
                    mouseBlocked = true;
                }
                hadTouches = false;
            }
            else
            {
                hadTouches = true;
                mouseBlocked = true;
            }
            var kb = PollKeyboard();
            if (kb != RunnerAction.None)
            {
                Reset();
                return kb;
            }

            if (touchCount > 0 && touchBlocked) return RunnerAction.None;
            // Latch the whole contact group, including release frames.
            if (touchCount >= 2)
            {
                var any = false;
                var liveCount = 0;
                for (var i = 0; i < touchCount; i++)
                {
                    var phase = Input.GetTouch(i).phase;
                    if (phase == TouchPhase.Ended || phase == TouchPhase.Canceled) continue;
                    liveCount++;
                    any |= phase == TouchPhase.Began;
                }
                CancelPointer();
                touchBlocked = true;
                return RunnerGestureClassifier.IsTwoFingerPause(liveCount, any)
                    ? RunnerAction.Pause : RunnerAction.None;
            }

            if (touchCount > 0)
                return PollTouch();

            // Legacy touch input can synthesize mouse events after its last touch.
            if (mouseBlocked)
            {
                if (!Input.GetMouseButton(0) && !Input.GetMouseButtonDown(0)) mouseBlocked = false;
                return RunnerAction.None;
            }
            return PollMouse();
        }

        private static RunnerAction PollKeyboard()
        {
            // 冲刺优先：Boss 破绽反击需要可靠键盘入口（触屏仍为双击）。
            if (Input.GetKeyDown(KeyCode.LeftShift) || Input.GetKeyDown(KeyCode.RightShift)
                || Input.GetKeyDown(KeyCode.F))
                return RunnerAction.Dash;
            if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow))
                return RunnerAction.Jump;
            if (Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow))
                return RunnerAction.Slide;
            if (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A))
                return RunnerAction.Left;
            if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D))
                return RunnerAction.Right;
            if (Input.GetKeyDown(KeyCode.P) || Input.GetKeyDown(KeyCode.Escape))
                return RunnerAction.Pause;
            return RunnerAction.None;
        }

        private RunnerAction PollTouch()
        {
            var t = Input.GetTouch(0);
            if (active && activeFingerId != t.fingerId)
            {
                CancelPointer();
                touchBlocked = true;
                return RunnerAction.None;
            }
            switch (t.phase)
            {
                case TouchPhase.Began:
                    start = t.position;
                    consumed = false;
                    active = true;
                    activeFingerId = t.fingerId;
                    break;
                case TouchPhase.Moved:
                case TouchPhase.Stationary:
                    if (active && !consumed)
                    {
                        var a = Detect(t.position);
                        if (a != RunnerAction.None)
                        {
                            consumed = true;
                            return a;
                        }
                    }

                    break;
                case TouchPhase.Ended:
                    if (active)
                        return FinishPointer(t.position);
                    break;
                case TouchPhase.Canceled:
                    CancelPointer();
                    touchBlocked = true;
                    break;
            }

            return RunnerAction.None;
        }

        private RunnerAction PollMouse()
        {
            if (Input.GetMouseButtonDown(0))
            {
                start = Input.mousePosition;
                consumed = false;
                active = true;
                activeFingerId = -1;
            }

            if (Input.GetMouseButton(0) && active && !consumed)
            {
                var action = Detect(Input.mousePosition);
                if (action != RunnerAction.None)
                {
                    consumed = true;
                    return action;
                }
            }

            if (Input.GetMouseButtonUp(0) && active)
                return FinishPointer(Input.mousePosition);
            if (active && !Input.GetMouseButton(0)) CancelPointer();

            return RunnerAction.None;
        }

        private RunnerAction FinishPointer(Vector2 endPos)
        {
            var delta = endPos - start;
            active = false;
            activeFingerId = -1;
            var thresholds = RunnerGestureThresholds.ForViewport(Screen.width, Screen.height);
            if (!consumed && RunnerGestureClassifier.IsTap(delta, thresholds))
            {
                var now = Time.unscaledTime;
                var isDoubleTap = RunnerGestureClassifier.IsDoubleTap(
                    Vector2.Distance(endPos, lastTapPos), now - lastTapTime, thresholds);
                lastTapPos = endPos;
                lastTapTime = now;
                return isDoubleTap ? RunnerAction.Dash : RunnerAction.Start;
            }
            if (!consumed)
                return Detect(endPos);
            return RunnerAction.None;
        }

        private RunnerAction Detect(Vector2 current)
        {
            var delta = current - start;
            var thresholds = RunnerGestureThresholds.ForViewport(Screen.width, Screen.height);
            return RunnerGestureClassifier.ClassifySwipe(delta, thresholds);
        }
    }
}
