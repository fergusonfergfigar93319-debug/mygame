namespace PenguinRun
{
    /// <summary>Run-local longitudinal ownership. No clocks advance outside simulation.</summary>
    internal sealed class RouteEncounterReservation
    {
        public bool Active { get; private set; }
        public float EndZ { get; private set; }

        public bool TryReserve(float startZ, float length)
        {
            if (Active || !Finite(startZ) || !Finite(length) || length <= 0f
                || !Finite(startZ + length)) return false;
            EndZ = startZ + length;
            Active = true;
            return true;
        }

        public void Advance(float dt, float speed, float despawnZ)
        {
            if (!Active || !Finite(dt) || !Finite(speed) || !Finite(despawnZ)
                || dt <= 0f || speed < 0f) return;
            EndZ -= dt * speed;
            if (EndZ < despawnZ) Clear();
        }

        public void Clear() { Active = false; EndZ = 0f; }
        private static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
    }
}
