namespace PenguinRun.Game
{
    internal sealed class RunLaunchGate
    {
        public bool IsBusy { get; private set; }

        public bool TryBegin()
        {
            if (IsBusy) return false;
            IsBusy = true;
            return true;
        }

        public void Reset()
        {
            IsBusy = false;
        }
    }
}
