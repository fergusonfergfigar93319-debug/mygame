namespace PenguinRun
{
    internal sealed class RunSettlementGate
    {
        public bool IsSettled { get; private set; }

        public bool TryBeginSettlement()
        {
            if (IsSettled) return false;
            IsSettled = true;
            return true;
        }

        public void ResetForNextAttempt()
        {
            IsSettled = false;
        }
    }
}
