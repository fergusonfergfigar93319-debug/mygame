using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
namespace PenguinRun.Tests
{
    public static class RunnerInputRegression
    {
        static readonly List<string> failures = new List<string>();
        public static int CaseCount { get; private set; }
        static Touch T(int id, TouchPhase phase, float x=0, float y=0) => new Touch { fingerId=id, phase=phase, position=new Vector2(x,y) };
        static RunnerAction Frame(RunnerInput input, params Touch[] touches) { Input.Frame(touches); return input.Poll(); }
        static void Expect(RunnerAction actual, RunnerAction expected, string name) { if(actual!=expected) failures.Add(name+": expected "+expected+", got "+actual); }
        static void Case(string name, Action body) { CaseCount++; try { Input.Frame(); Input.mousePosition=Vector2.zero; body(); } catch(Exception e) { failures.Add(name+": "+e.GetBaseException().Message); } }
        static void Reset(RunnerInput input)
        {
            var reset=typeof(RunnerInput).GetMethod("Reset");
            if(reset==null) throw new Exception("RunnerInput.Reset is missing");
            reset.Invoke(input,null);
        }
        public static string[] Run()
        {
            failures.Clear();
            CaseCount = 0;
            Case("ordinary controls",()=> {
                var r=new RunnerInput(); Frame(r,T(1,TouchPhase.Began));
                Expect(Frame(r,T(1,TouchPhase.Ended)),RunnerAction.Start,"tap");
                Frame(r); Frame(r,T(2,TouchPhase.Began));
                Expect(Frame(r,T(2,TouchPhase.Ended)),RunnerAction.Dash,"double tap");
                Frame(r); Frame(r,T(3,TouchPhase.Began));
                Expect(Frame(r,T(3,TouchPhase.Moved,150)),RunnerAction.Right,"right swipe");
                Expect(Frame(r,T(3,TouchPhase.Ended,150)),RunnerAction.None,"swipe releases once");
            });
            Case("cancel tap",()=> { var r=new RunnerInput(); Frame(r,T(1,TouchPhase.Began)); Expect(Frame(r,T(1,TouchPhase.Canceled)),RunnerAction.None,"canceled tap"); });
            Case("cancel swipe",()=> { var r=new RunnerInput(); Frame(r,T(1,TouchPhase.Began)); Expect(Frame(r,T(1,TouchPhase.Canceled,200)),RunnerAction.None,"canceled swipe"); });
            Case("cancel breaks tap chain",()=> {
                var r=new RunnerInput(); Frame(r,T(1,TouchPhase.Began)); Frame(r,T(1,TouchPhase.Ended)); Frame(r);
                Frame(r,T(2,TouchPhase.Began)); Frame(r,T(2,TouchPhase.Canceled)); Frame(r);
                Frame(r,T(3,TouchPhase.Began)); Expect(Frame(r,T(3,TouchPhase.Ended)),RunnerAction.Start,"tap after cancellation");
            });
            Case("foreign finger",()=> { var r=new RunnerInput(); Frame(r,T(7,TouchPhase.Began)); Expect(Frame(r,T(8,TouchPhase.Moved,180)),RunnerAction.None,"replacement finger cannot inherit swipe"); });
            Case("lost contact",()=> { var r=new RunnerInput(); Frame(r,T(7,TouchPhase.Began)); Frame(r); Expect(Frame(r,T(7,TouchPhase.Ended)),RunnerAction.None,"lost touch cannot finish later"); });
            Case("multi touch latch",()=> {
                var r=new RunnerInput(); Frame(r,T(1,TouchPhase.Began));
                Expect(Frame(r,T(1,TouchPhase.Stationary),T(2,TouchPhase.Began)),RunnerAction.Pause,"two-finger pause");
                Expect(Frame(r,T(1,TouchPhase.Stationary),T(2,TouchPhase.Stationary),T(3,TouchPhase.Began)),RunnerAction.None,"third finger must not toggle again");
                Expect(Frame(r,T(1,TouchPhase.Ended)),RunnerAction.None,"pause release cannot start or dash");
                Frame(r); Expect(Frame(r,T(5,TouchPhase.Began),T(6,TouchPhase.Began)),RunnerAction.Pause,"new gesture can pause");
            });
            Case("ended contacts not counted",()=> { var r=new RunnerInput(); Frame(r,T(1,TouchPhase.Began)); Expect(Frame(r,T(1,TouchPhase.Ended),T(2,TouchPhase.Began)),RunnerAction.None,"one ending plus one beginning is not two-finger pause"); });
            Case("reset waits for release",()=> {
                var r=new RunnerInput(); Frame(r,T(1,TouchPhase.Began)); Reset(r);
                Expect(Frame(r,T(1,TouchPhase.Stationary),T(2,TouchPhase.Began)),RunnerAction.None,"reset must not rearm held touch");
                Expect(Frame(r,T(1,TouchPhase.Ended)),RunnerAction.None,"reset clears pending tap");
                Frame(r); Frame(r,T(3,TouchPhase.Began)); Expect(Frame(r,T(3,TouchPhase.Ended)),RunnerAction.Start,"fresh input after reset");
            });
            Case("reset clears double tap",()=> { var r=new RunnerInput(); Frame(r,T(1,TouchPhase.Began)); Frame(r,T(1,TouchPhase.Ended)); Input.Frame(); Reset(r); Frame(r); Frame(r,T(2,TouchPhase.Began)); Expect(Frame(r,T(2,TouchPhase.Ended)),RunnerAction.Start,"reset double tap history"); });
            Case("mouse reset",()=> { var r=new RunnerInput(); Input.Frame(); Input.mouseDown=Input.mouseHeld=true; r.Poll(); Reset(r); Input.Frame(); Input.mouseHeld=true; Input.mousePosition=new Vector2(200,0); Expect(r.Poll(),RunnerAction.None,"held mouse after reset"); Input.Frame(); Input.mouseUp=true; Expect(r.Poll(),RunnerAction.None,"mouse release after reset"); });
            Case("keyboard interrupts touch",()=> { var r=new RunnerInput(); Frame(r,T(1,TouchPhase.Began)); Input.Frame(T(1,TouchPhase.Stationary)); Input.keys.Add(KeyCode.Space); Expect(r.Poll(),RunnerAction.Jump,"keyboard mapping unchanged"); Expect(Frame(r,T(1,TouchPhase.Ended)),RunnerAction.None,"keyboard cannot leave latent tap"); });
            Case("simulated mouse echo",()=> { var r=new RunnerInput(); Frame(r,T(1,TouchPhase.Began)); Frame(r,T(1,TouchPhase.Canceled)); Input.Frame(); Input.mouseDown=Input.mouseHeld=true; r.Poll(); Input.Frame(); Input.mouseUp=true; Expect(r.Poll(),RunnerAction.None,"touch cannot replay through mouse"); });
            Case("ordinary mouse",()=> {
                var r=new RunnerInput();
                Input.Frame(); Input.mouseDown=Input.mouseHeld=true; r.Poll();
                Input.Frame(); Input.mouseUp=true; Expect(r.Poll(),RunnerAction.Start,"mouse tap");
                Input.Frame(); Input.mouseDown=Input.mouseHeld=true; r.Poll();
                Input.Frame(); Input.mouseUp=true; Expect(r.Poll(),RunnerAction.Dash,"mouse double tap");
            });
            Case("four swipe directions",()=> {
                var deltas=new[] {new Vector2(-150,0),new Vector2(150,0),new Vector2(0,150),new Vector2(0,-150)};
                var actions=new[] {RunnerAction.Left,RunnerAction.Right,RunnerAction.Jump,RunnerAction.Slide};
                for(var i=0;i<deltas.Length;i++) {
                    var r=new RunnerInput(); Frame(r,T(1,TouchPhase.Began));
                    Expect(Frame(r,T(1,TouchPhase.Moved,deltas[i].x,deltas[i].y)),actions[i],"swipe "+actions[i]);
                }
            });
            Case("keyboard mapping",()=> {
                var keys=new[] {KeyCode.LeftShift,KeyCode.RightShift,KeyCode.F,KeyCode.Space,KeyCode.W,KeyCode.UpArrow,KeyCode.S,KeyCode.DownArrow,KeyCode.LeftArrow,KeyCode.A,KeyCode.RightArrow,KeyCode.D,KeyCode.P,KeyCode.Escape};
                var actions=new[] {RunnerAction.Dash,RunnerAction.Dash,RunnerAction.Dash,RunnerAction.Jump,RunnerAction.Jump,RunnerAction.Jump,RunnerAction.Slide,RunnerAction.Slide,RunnerAction.Left,RunnerAction.Left,RunnerAction.Right,RunnerAction.Right,RunnerAction.Pause,RunnerAction.Pause};
                var r=new RunnerInput();
                for(var i=0;i<keys.Length;i++) { Input.Frame(); Input.keys.Add(keys[i]); Expect(r.Poll(),actions[i],"key "+keys[i]); }
            });
            Case("reset suppresses same frame",()=> {
                var r=new RunnerInput(); Input.Frame(); Input.keys.Add(KeyCode.Space); Reset(r);
                Expect(r.Poll(),RunnerAction.None,"callback cannot replay same-frame input");
                Input.Frame(); Input.keys.Add(KeyCode.Space); Expect(r.Poll(),RunnerAction.Jump,"new key after reset");
            });
            Case("reset preserves multi-touch latch",()=> {
                var r=new RunnerInput(); Expect(Frame(r,T(1,TouchPhase.Began),T(2,TouchPhase.Began)),RunnerAction.Pause,"initial pause"); Reset(r);
                Expect(Frame(r,T(1,TouchPhase.Stationary),T(3,TouchPhase.Began)),RunnerAction.None,"pause callback cannot re-arm group");
                Expect(Frame(r,T(1,TouchPhase.Moved,200)),RunnerAction.None,"remaining finger cannot swipe");
                Frame(r); Frame(r,T(4,TouchPhase.Began)); Expect(Frame(r,T(4,TouchPhase.Ended)),RunnerAction.Start,"fresh contact after release");
            });
            return failures.ToArray();
        }
    }
}
