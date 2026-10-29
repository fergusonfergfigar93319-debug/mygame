// Production CollisionSystem and SegmentWaveDirector run unchanged in this harness.
// These stand-ins isolate engine objects, presentation, and unrelated defense logic.
// This is not an engine-physics, damage-resolution, or device integration test.
using System;
using System.Collections.Generic;
namespace UnityEngine
{
    public struct Vector3
    {
        public float x, y, z;
        public Vector3(float x, float y, float z) { this.x=x; this.y=y; this.z=z; }
    }
    public sealed class Transform { public Vector3 position; }
    public sealed class GameObject
    {
        public readonly Transform transform = new Transform();
        private readonly List<object> components = new List<object>();
        public void Register(object component) => components.Add(component);
        public bool TryGetComponent<T>(out T found) where T : class
        {
            foreach(var c in components) if(c is T match) { found=match; return true; }
            found=null; return false;
        }
    }
    public static class Random
    {
        private static System.Random seeded = new System.Random(1);
        public static int? FixedIndex;
        public static void InitState(int seed) { seeded=new System.Random(seed); FixedIndex=null; }
        public static int Range(int min, int max) => FixedIndex.HasValue
            ? min + FixedIndex.Value % Math.Max(1,max-min) : seeded.Next(min,max);
    }
}
namespace PenguinRun.Core
{
    public static class GameEvents
    {
        public const string PerfectDodge="perfect", PlayerHurt="hurt";
    }
    public static class EventBus { public static void Emit(string name) { } }
}
namespace PenguinRun
{
    internal class RunnerAudio { public void PlayShieldBreak() { } public void PlayCrash() { } }
    internal class RunnerCombatTuning { }
    internal class RunUpgradeState
    {
        public float ShieldGraceSeconds => 0f;
        // Neutral upgrade boundary; defense calls still fail loudly below.
        public float HitInvulnerabilityBonusSeconds => 0f;
    }
    internal class WorldDirector
    {
        public float HitInvulnerabilityTimer, ShieldTimer;
        public int PerfectDodges;
        public void AddPerfectDodge() { PerfectDodges++; }
        public void SetHitInvulnerability(float t) { HitInvulnerabilityTimer=t; }
        public void BreakPerfectDodgeStreak() { }
    }
    internal class SegmentSpawner { public bool TryRemoveObstacle(UnityEngine.GameObject o) => true; }
    internal class ObstacleBehavior { public bool IsCollidable=true; }
    internal class ShatterWall : ObstacleBehavior { public bool TryShatter() => false; }
    internal class PlayerMotor
    {
        public float BodyHeight=1.56f, PerfectActionWindow;
        public bool Grounded=true;
        public bool IsLowPosture => BodyHeight <= ObstacleColliderSpec.DefaultPassUnderMaxHeight;
        public RunnerAction LastCriticalAction;
    }
    internal enum HitOutcomeKind { None, PerfectDodge, BlockedByDefense, Damage, GameOver }
    internal struct HitOutcome
    {
        public HitOutcomeKind Kind { get; set; }
        public bool ConsumeShield { get; set; }
        public bool RemoveObstacle { get; set; }
        public float InvulnerabilitySeconds { get; set; }
        public float FeedbackDuration { get; set; }
        public string FeedbackText { get; set; }
    }
    internal static class RunnerHitResolver
    {
        public static HitOutcome TryResolveObstacleDefense(WorldDirector world, ObstacleColliderSpec spec, RunnerCombatTuning combat)
            => throw new InvalidOperationException("Unexpected defense call in a non-contact scoring scenario.");
    }
}
