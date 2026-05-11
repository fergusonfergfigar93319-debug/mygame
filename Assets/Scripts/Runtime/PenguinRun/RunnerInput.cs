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
        private const float SwipeThreshold = 58f;
        private const float TapThreshold = 22f;
        private const float DoubleTapMaxGapSeconds = 0.3f;
        private const float DoubleTapMaxDistance = 52f;
        private Vector2 start;
        private bool consumed;
        private bool active;
        private Vector2 lastTapPos;
        private float lastTapTime = -9f;

        public RunnerAction Poll()
        {
            var kb = PollKeyboard();
            if (kb != RunnerAction.None)
                return kb;

            // 双指触摸 = 暂停
            if (Input.touchCount >= 2)
            {
                var any = false;
                for (var i = 0; i < Input.touchCount; i++)
                {
                    if (Input.GetTouch(i).phase == TouchPhase.Began)
                    {
                        any = true;
                        break;
                    }
                }
                if (any) return RunnerAction.Pause;
            }

            if (Input.touchCount > 0)
                return PollTouch();

            return PollMouse();
        }

        private static RunnerAction PollKeyboard()
        {
            if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow))
                return RunnerAction.Jump;
            if (Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow))
                return RunnerAction.Slide;
            if (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A))
                return RunnerAction.Left;
            if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D))
                return RunnerAction.Right;
            if (Input.GetKeyDown(KeyCode.P))
                return RunnerAction.Pause;
            return RunnerAction.None;
        }

        private RunnerAction PollTouch()
        {
            var t = Input.GetTouch(0);
            switch (t.phase)
            {
                case TouchPhase.Began:
                    start = t.position;
                    consumed = false;
                    active = true;
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
                case TouchPhase.Canceled:
                    if (active)
                        return FinishPointer(t.position);
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

            return RunnerAction.None;
        }

        private RunnerAction FinishPointer(Vector2 endPos)
        {
            var delta = endPos - start;
            active = false;
            if (!consumed && Mathf.Abs(delta.x) < TapThreshold && Mathf.Abs(delta.y) < TapThreshold)
            {
                var now = Time.unscaledTime;
                var isDoubleTap =
                    now - lastTapTime <= DoubleTapMaxGapSeconds &&
                    Vector2.Distance(endPos, lastTapPos) <= DoubleTapMaxDistance;
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
            var absX = Mathf.Abs(delta.x);
            var absY = Mathf.Abs(delta.y);
            if (absX >= SwipeThreshold && absX > absY * 1.12f)
                return delta.x > 0 ? RunnerAction.Right : RunnerAction.Left;
            if (absY >= SwipeThreshold && absY > absX * 0.86f)
                return delta.y > 0 ? RunnerAction.Jump : RunnerAction.Slide;
            return RunnerAction.None;
        }
    }
}
