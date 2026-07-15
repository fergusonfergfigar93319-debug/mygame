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

        private static readonly ThemeMechanicBeat[] CedarNormal =
        {
            new(0.8f, RunnerLane.Left, "cedar/swing_log", false, false, false),
            new(2.2f, RunnerLane.Right, "cedar/root_gallery", false, true, false),
            new(3.8f, RunnerLane.Center, "cedar/swing_log", false, false, false),
            new(5.4f, RunnerLane.Left, "cedar/root_gallery", false, true, false),
            new(7.0f, RunnerLane.Right, "cedar/swing_log", false, false, true),
        };
        private static readonly ThemeMechanicBeat[] CedarSignature =
        {
            new(0.7f, RunnerLane.Left, "cedar/ruin_escape", false, false, false),
            new(1.9f, RunnerLane.Right, "cedar/ruin_escape", false, true, false),
            new(3.1f, RunnerLane.Center, "cedar/ruin_escape", true, false, false),
            new(4.3f, RunnerLane.Left, "cedar/ruin_escape", false, true, false),
            new(5.5f, RunnerLane.Right, "cedar/ruin_escape", true, false, true),
            new(6.7f, RunnerLane.Center, "cedar/ruin_escape", false, false, true),
        };
        private static readonly ThemeMechanicBeat[] AuroraNormal =
        {
            new(0.7f, RunnerLane.Center, "aurora/polarity_gate", false, false, false),
            new(2.0f, RunnerLane.Left, "aurora/prism_beam", false, true, false),
            new(3.4f, RunnerLane.Right, "aurora/polarity_gate", false, false, false),
            new(4.9f, RunnerLane.Center, "aurora/prism_beam", false, true, false),
            new(6.5f, RunnerLane.Left, "aurora/polarity_gate", false, false, true),
        };
        private static readonly ThemeMechanicBeat[] AuroraSignature =
        {
            new(0.7f, RunnerLane.Left, "aurora/magnetic_rebuild", false, false, false),
            new(1.9f, RunnerLane.Center, "aurora/prism_beam", false, true, false),
            new(3.1f, RunnerLane.Right, "aurora/magnetic_rebuild", false, false, false),
            new(4.3f, RunnerLane.Center, "aurora/prism_beam", false, true, false),
            new(5.5f, RunnerLane.Left, "aurora/magnetic_rebuild", true, false, true),
            new(6.7f, RunnerLane.Right, "aurora/magnetic_rebuild", false, false, true),
        };
        private static readonly ThemeMechanicBeat[] MistNormal =
        {
            new(0.9f, RunnerLane.Right, "mist/decoy_shadow", false, false, false),
            new(2.4f, RunnerLane.Left, "mist/rotating_beacon", false, false, false),
            new(4.0f, RunnerLane.Center, "mist/decoy_shadow", false, false, false),
            new(5.8f, RunnerLane.Right, "mist/rotating_beacon", false, false, false),
            new(7.6f, RunnerLane.Center, "mist/rotating_beacon", false, false, true),
        };
        private static readonly ThemeMechanicBeat[] MistSignature =
        {
            new(0.8f, RunnerLane.Left, "mist/floodgate_chase", false, false, false),
            new(2.0f, RunnerLane.Right, "mist/decoy_shadow", false, false, false),
            new(3.2f, RunnerLane.Center, "mist/floodgate_chase", false, false, false),
            new(4.4f, RunnerLane.Left, "mist/decoy_shadow", false, false, false),
            new(5.6f, RunnerLane.Right, "mist/floodgate_chase", false, false, true),
            new(6.8f, RunnerLane.Center, "mist/floodgate_chase", false, false, true),
        };
        private static readonly ThemeMechanicBeat[] OceanNormal =
        {
            new(0.8f, RunnerLane.Left, "ocean/coral_lift", true, false, false),
            new(2.3f, RunnerLane.Center, "ocean/bubble_vault", true, false, false),
            new(3.9f, RunnerLane.Right, "ocean/coral_lift", false, true, false),
            new(5.5f, RunnerLane.Left, "ocean/bubble_vault", true, false, false),
            new(7.1f, RunnerLane.Center, "ocean/coral_lift", false, false, true),
        };
        private static readonly ThemeMechanicBeat[] OceanSignature =
        {
            new(0.7f, RunnerLane.Left, "ocean/wave_tunnel", true, false, false),
            new(1.9f, RunnerLane.Center, "ocean/wave_tunnel", false, true, false),
            new(3.1f, RunnerLane.Right, "ocean/wave_tunnel", true, false, false),
            new(4.3f, RunnerLane.Center, "ocean/wave_tunnel", false, true, false),
            new(5.5f, RunnerLane.Left, "ocean/wave_tunnel", true, false, true),
            new(6.7f, RunnerLane.Right, "ocean/wave_tunnel", true, false, true),
        };
        private static readonly ThemeMechanicBeat[] SkyNormal =
        {
            new(0.7f, RunnerLane.Center, "sky/wind_gate", false, false, false),
            new(2.2f, RunnerLane.Right, "sky/cloud_platform", true, false, false),
            new(3.8f, RunnerLane.Left, "sky/wind_gate", false, false, false),
            new(5.4f, RunnerLane.Center, "sky/cloud_platform", true, false, false),
            new(7.2f, RunnerLane.Right, "sky/wind_gate", false, false, true),
        };
        private static readonly ThemeMechanicBeat[] SkySignature =
        {
            new(0.7f, RunnerLane.Left, "sky/island_glide_chain", true, false, false),
            new(1.9f, RunnerLane.Center, "sky/island_glide_chain", true, false, false),
            new(3.1f, RunnerLane.Right, "sky/island_glide_chain", true, false, false),
            new(4.3f, RunnerLane.Left, "sky/island_glide_chain", true, false, false),
            new(5.5f, RunnerLane.Center, "sky/island_glide_chain", true, false, true),
            new(6.7f, RunnerLane.Right, "sky/island_glide_chain", true, false, true),
        };

        public static ReadOnlySpan<ThemeMechanicBeat> For(ThemeMechanicId id, bool signature)
        {
            return id switch
            {
                ThemeMechanicId.IceInertia => signature ? IceSignature : IceNormal,
                ThemeMechanicId.CedarCollapse => signature ? CedarSignature : CedarNormal,
                ThemeMechanicId.AuroraPolarity => signature ? AuroraSignature : AuroraNormal,
                ThemeMechanicId.MistBeacon => signature ? MistSignature : MistNormal,
                ThemeMechanicId.OceanTide => signature ? OceanSignature : OceanNormal,
                ThemeMechanicId.SkyWind => signature ? SkySignature : SkyNormal,
                _ => Empty,
            };
        }
    }
}
