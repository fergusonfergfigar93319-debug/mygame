using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
namespace PenguinRun.Tests
{
    public static class GameplayFairnessRegression
    {
        private static readonly List<string> Failures = new List<string>();
        public static int CaseCount { get; private set; }
        private static void Check(bool condition, string message)
        { if (!condition) throw new Exception(message); }
        private static void Case(string name, Action body)
        {
            CaseCount++;
            try { UnityEngine.Random.InitState(23); body(); }
            catch(Exception e) { Failures.Add(name + ": " + e.GetBaseException().Message); }
        }
        public static string[] Run()
        {
            Failures.Clear(); CaseCount=0;
            Case("inactive collision behavior cannot award near miss", () => {
                var f=new CollisionFixture(); f.Obstacle.Register(new ObstacleBehavior { IsCollidable=false });
                f.Check(); Check(f.NearMisses==0,"inactive obstacle awarded a near miss");
            });
            Case("ordinary near miss is awarded once", () => {
                var f=new CollisionFixture(); f.Check(); f.Check();
                Check(f.NearMisses==1,"standing close pass must score exactly once");
            });
            Case("reactivated obstacle can award a legitimate near miss", () => {
                var f=new CollisionFixture(); var behavior=new ObstacleBehavior { IsCollidable=false };
                f.Obstacle.Register(behavior); f.Check(); behavior.IsCollidable=true; f.Check();
                Check(f.NearMisses==1,"inactive pass must not consume the later reward");
            });
            Case("inactive obstacle cannot award perfect dodge", () => {
                var f=new CollisionFixture(); f.Obstacle.Register(new ObstacleBehavior { IsCollidable=false });
                f.Obstacle.transform.position=new Vector3(0f,0f,0f);
                f.PlayerY=2f; f.Motor.Grounded=false; f.Motor.PerfectActionWindow=0.3f;
                f.Motor.LastCriticalAction=RunnerAction.Jump; f.Check();
                Check(f.World.PerfectDodges==0,"inactive obstacle awarded a perfect dodge");
            });
            Case("perfect slide is awarded once", () => {
                var f=new CollisionFixture(); f.Obstacle.transform.position=new Vector3(0f,0f,0f);
                f.Spec=ObstacleColliderSpec.LowSlideGate; f.Motor.BodyHeight=0.86f; f.Slide=0.3f;
                f.Motor.PerfectActionWindow=0.3f; f.Motor.LastCriticalAction=RunnerAction.Slide;
                f.Check(); f.Check(); Check(f.World.PerfectDodges==1,"legitimate perfect slide must score once");
            });
            Case("sliding is not a near miss", () => {
                var f=new CollisionFixture(); f.Motor.BodyHeight=0.86f; f.Slide=0.3f;
                f.Check(); Check(f.NearMisses==0,"sliding was rewarded as standing near miss");
            });
            Case("rising low posture is not a near miss", () => {
                var f=new CollisionFixture(); f.Motor.BodyHeight=1f;
                f.Check(); Check(f.NearMisses==0,"crouch recovery was rewarded as near miss");
            });
            Case("airborne action is not a near miss", () => {
                var f=new CollisionFixture(); f.Motor.Grounded=false; f.PlayerY=0.2f;
                f.Check(); Check(f.NearMisses==0,"airborne action was rewarded as near miss");
            });
            Case("slide fallback without motor is not a near miss", () => {
                var f=new CollisionFixture(); f.Motor=null; f.Slide=0.3f;
                f.Check(); Check(f.NearMisses==0,"legacy slide timer must exclude near miss");
            });
            Case("jump clearance is not a near miss", () => {
                var f=new CollisionFixture(); f.PlayerY=2f;
                f.Check(); Check(f.NearMisses==0,"overhead pass must not award near miss");
            });
            Case("swept-only pass is not a near miss", () => {
                var f=new CollisionFixture(); f.Obstacle.transform.position=new Vector3(0.8f,0f,-1f);
                f.Sweep=2f; f.Check(); Check(f.NearMisses==0,"swept-only pass must not award near miss");
            });
            Case("near miss cannot later earn perfect dodge", () => {
                var f=new CollisionFixture(); f.Check();
                f.Obstacle.transform.position=new Vector3(0f,0f,0f);
                f.PlayerY=2f; f.Motor.Grounded=false;
                f.Motor.PerfectActionWindow=0.3f; f.Motor.LastCriticalAction=RunnerAction.Jump;
                f.Check(); Check(f.NearMisses==1 && f.World.PerfectDodges==0,"one obstacle earned both reward types");
            });
            Case("perfect dodge cannot later earn near miss", () => {
                var f=new CollisionFixture(); f.Obstacle.transform.position=new Vector3(0f,0f,0f);
                f.PlayerY=2f; f.Motor.Grounded=false;
                f.Motor.PerfectActionWindow=0.3f; f.Motor.LastCriticalAction=RunnerAction.Jump;
                f.Check(); Check(f.World.PerfectDodges==1,"perfect jump control must score");
                f.Obstacle.transform.position=new Vector3(0.8f,0f,0f);
                f.PlayerY=0f; f.Motor.Grounded=true; f.Check();
                Check(f.NearMisses==0,"perfect dodge later awarded another near miss");
            });
            Case("reset clears reward deduplication", () => {
                var f=new CollisionFixture(); f.Check(); f.System.Reset(); f.Check();
                Check(f.NearMisses==2,"new run must not inherit reward deduplication");
            });
            Case("variety survives adversarial random draws", () => {
                UnityEngine.Random.FixedIndex=0;
                AssertVariety(new SegmentWaveDirector(RunnerMapTheme.IceLakeEcho),80);
            });
            Case("all themes preserve variety", () => {
                foreach(RunnerMapTheme theme in Enum.GetValues(typeof(RunnerMapTheme)))
                    AssertVariety(new SegmentWaveDirector(theme),2000);
            });
            Case("single-pattern pool remains playable", () => {
                var d=new SegmentWaveDirector(RunnerMapTheme.IceLakeEcho);
                var only=new[] { RunnerSegmentCatalog.Fallback };
                typeof(SegmentWaveDirector).GetField("pool",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(d,only);
                d.ResetForRun();
                for(var i=0;i<15;i++) Check(d.ConsumeWave().PatternKind==SegmentPatternKind.Mixed,"single pattern pool failed");
            });
            Case("onboarding retains its fixed rotation", () => {
                var order=new[] {SegmentPatternKind.LaneWeave,SegmentPatternKind.JumpArc,SegmentPatternKind.SlideTunnel,SegmentPatternKind.RewardRun};
                foreach(RunnerMapTheme theme in Enum.GetValues(typeof(RunnerMapTheme))) {
                    var d=new SegmentWaveDirector(theme,true); var n=0;
                    for(var i=0;i<40;i++) { var wave=d.ConsumeWave(); if(d.LastConsumeWasFirstWave) Check(wave.PatternKind==order[n++%4],"onboarding rotation changed in "+theme); }
                }
            });
            Case("multiple definitions of the only pattern remain playable", () => {
                var d=new SegmentWaveDirector(RunnerMapTheme.IceLakeEcho);
                SetPool(d,new[] { Definition(0,SegmentPatternKind.Mixed), Definition(1,SegmentPatternKind.Mixed) });
                UnityEngine.Random.FixedIndex=1; d.ResetForRun();
                for(var i=0;i<15;i++) Check(d.ConsumeWave().PowerChanceDelta==1f,"same-pattern fallback excluded a valid definition");
            });
            Case("empty and null pools use fallback", () => {
                foreach(var pool in new RunnerSegmentDefinition[][] { Array.Empty<RunnerSegmentDefinition>(), null }) {
                    var d=new SegmentWaveDirector(RunnerMapTheme.IceLakeEcho); SetPool(d,pool); d.ResetForRun();
                    for(var i=0;i<15;i++) Check(d.ConsumeWave().PatternKind==SegmentPatternKind.Mixed,"empty pool fallback failed");
                }
            });
            Case("every eligible definition has a selection slot", () => {
                var pool=new[] { Definition(0,SegmentPatternKind.Mixed), Definition(1,SegmentPatternKind.JumpArc),
                    Definition(2,SegmentPatternKind.JumpArc), Definition(3,SegmentPatternKind.SlideTunnel) };
                for(var slot=0;slot<3;slot++) {
                    UnityEngine.Random.FixedIndex=0;
                    var d=new SegmentWaveDirector(RunnerMapTheme.IceLakeEcho); SetPool(d,pool); d.ResetForRun();
                    UnityEngine.Random.FixedIndex=slot;
                    Check(d.ConsumeWave().PatternKind==SegmentPatternKind.Mixed,"first selection control changed");
                    Check(d.ConsumeWave().PowerChanceDelta==slot+1,"eligible definition was skipped or duplicated");
                }
            });
            return Failures.ToArray();
        }
        private static RunnerSegmentDefinition Definition(int id, SegmentPatternKind kind)
            => new RunnerSegmentDefinition("fixture-"+id,1,id,0f,0f,0f,0f,0f,SegmentObstaclePalette.IceLake,kind);
        private static void SetPool(SegmentWaveDirector director, RunnerSegmentDefinition[] pool)
            => typeof(SegmentWaveDirector).GetField("pool",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(director,pool);
        private static void AssertVariety(SegmentWaveDirector d, int waves)
        {
            var last=(SegmentPatternKind)(-1);
            for(var i=0;i<waves;i++) {
                var wave=d.ConsumeWave();
                if(!d.LastConsumeWasFirstWave) continue;
                Check(wave.PatternKind!=last,"consecutive segments repeated "+last);
                last=wave.PatternKind;
            }
        }
        private sealed class CollisionFixture
        {
            public readonly CollisionSystem System=new CollisionSystem(new RunnerAudio(),new RunnerCombatTuning(),new RunUpgradeState());
            public readonly WorldDirector World=new WorldDirector();
            public readonly GameObject Obstacle=new GameObject();
            public PlayerMotor Motor=new PlayerMotor();
            public ObstacleColliderSpec Spec=ObstacleColliderSpec.SmallJumpable;
            public float PlayerY, Slide, Sweep;
            public int NearMisses;
            private readonly Transform player=new Transform();
            private string feedback="";
            private float feedbackTimer;
            public CollisionFixture() { Obstacle.transform.position=new Vector3(0.8f,0f,0f); }
            public void Check()
            {
                System.CheckCollisions(player,PlayerY,0f,Slide,0f,World,new SegmentSpawner(),
                    new List<GameObject>{Obstacle},new Dictionary<GameObject,ObstacleColliderSpec>{{Obstacle,Spec}},
                    ref feedback,ref feedbackTimer,null,Motor,obstacleSweepDz:Sweep,onNearMiss:()=>NearMisses++);
            }
        }
    }
}
