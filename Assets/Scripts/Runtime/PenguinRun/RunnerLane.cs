namespace PenguinRun
{
    public enum RunnerLane
    {
        Left = -1,
        Center = 0,
        Right = 1,
    }

    public static class RunnerLaneExtensions
    {
        public static RunnerLane MoveLeft(this RunnerLane lane)
        {
            return lane == RunnerLane.Right ? RunnerLane.Center :
                lane == RunnerLane.Center ? RunnerLane.Left : RunnerLane.Left;
        }

        public static RunnerLane MoveRight(this RunnerLane lane)
        {
            return lane == RunnerLane.Left ? RunnerLane.Center :
                lane == RunnerLane.Center ? RunnerLane.Right : RunnerLane.Right;
        }

        public static float ToX(this RunnerLane lane, float laneWidth)
        {
            return (int)lane * laneWidth;
        }
    }
}
