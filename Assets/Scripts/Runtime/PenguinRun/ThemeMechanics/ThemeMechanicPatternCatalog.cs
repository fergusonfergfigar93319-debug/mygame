using System;

namespace PenguinRun
{
    internal static class ThemeMechanicPatternCatalog
    {
        private static readonly ThemeMechanicBeat[] Empty = Array.Empty<ThemeMechanicBeat>();

        private static readonly ThemeMechanicBeat[] IceNormal =
        {
            new(0.8f, RunnerLane.Left, "ice/cracked_plate", true, false, false),
            new(2.1f, RunnerLane.Right, "ice/crystal_rebound_gate", false, false, false),
            new(3.6f, RunnerLane.Center, "ice/cracked_plate", true, false, false),
            new(5.2f, RunnerLane.Left, "ice/crystal_rebound_gate", false, false, true),
            new(6.7f, RunnerLane.Right, "ice/cracked_plate", true, false, true),
        };

        private static readonly ThemeMechanicBeat[] IceSignature =
        {
            new(0.7f, RunnerLane.Center, "ice/floe_bridge", true, false, false),
            new(2.0f, RunnerLane.Left, "ice/floe_bridge", true, false, false),
            new(3.3f, RunnerLane.Right, "ice/floe_bridge", true, false, false),
            new(4.6f, RunnerLane.Center, "ice/floe_bridge", true, false, true),
            new(5.9f, RunnerLane.Left, "ice/floe_bridge", true, false, true),
        };

        public static ReadOnlySpan<ThemeMechanicBeat> For(ThemeMechanicId id, bool signature)
        {
            if (id != ThemeMechanicId.IceInertia)
            {
                return Empty;
            }

            return signature ? IceSignature : IceNormal;
        }
    }
}
