using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using Koriko.Core;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using Unity.Cinemachine;

namespace Koriko
{
    /// <summary>Native ground-animation review through actual input/controller code.
    /// The opt-in QA session never reads or writes player saves.</summary>
    [DefaultExecutionOrder(100)]
    public sealed class DevelopmentWalkCheck:MonoBehaviour
    {
        public static bool Requested=>Array.IndexOf(Environment.GetCommandLineArgs(),"--koriko-walk-check")>=0;
        bool Record=>Array.IndexOf(Environment.GetCommandLineArgs(),"--koriko-record-walk")>=0;
        GameApp app;RiderPerformance rider;Keyboard keyboard;Gamepad pad;Camera camera;
        string output,clip,action;int frame;bool failed,recording,addedKeyboard;
        Vector3 view=new Vector3(4.2f,2.15f,6);
        StreamWriter table;readonly List<string> checks=new List<string>();
        float maxGrip,maxContact,maxSlide,peakWalk;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Install(){if(Requested)new GameObject("Native walking review").AddComponent<DevelopmentWalkCheck>();}
        IEnumerator Start()
        {
            output=Path.Combine(Application.persistentDataPath,"walk-check");Directory.CreateDirectory(output);
            app=FindFirstObjectByType<GameApp>();rider=app.Motor.Visual.GetComponentInChildren<RiderPerformance>();
            keyboard=Keyboard.current;if(keyboard==null){keyboard=InputSystem.AddDevice<Keyboard>();addedKeyboard=true;}
            pad=InputSystem.AddDevice<Gamepad>();
            Application.runInBackground=true;InputSystem.settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;
            Time.captureFramerate=24;camera=Camera.main;camera.GetComponent<CinemachineBrain>().enabled=false;
            yield return null;app.Begin(Difficulty.Cozy);app.Hud.ClosePanel();app.Hud.GetComponent<Canvas>().enabled=false;app.Rules.State.elapsed=75;
            yield return Frames(.8f,"Settle");
            Check(rider.HasWalkingRig,"Carry anchors, free-hand and walking cloth shapes survive import");
            Check(app.Motor.OnFoot&&app.Motor.Grounded,"Starting at the bakery puts Kiki on foot");
            Check(rider.BroomUpright>.95f&&rider.BroomTipClearance<.04f,"Resting broom stands at her side with its tip on the ground");
            BeginClip("01-walk-and-settle");
            yield return Frames(1.4f,"Rest / shoulder Jiji");
            var start=app.Motor.transform.position;
            yield return Frames(2.8f,"Walk forward",Key.W);
            Check(app.Motor.OnFoot&&app.Motor.Grounded&&Vector3.Distance(start,app.Motor.transform.position)>4,"Keyboard walking travels at street level without mounting");
            Check(peakWalk<=FlightMotor.WalkSpeed+.15f,"Ground movement is capped at walking speed");
            view=new Vector3(6,1.8f,.5f);
            yield return Frames(1.4f,"Turn and walk",Key.D);
            yield return Frames(1.3f,"Stop / settle feet");
            Check(new Vector2(app.Motor.Velocity.x,app.Motor.Velocity.z).magnitude<.04f,"Releasing walking input stops without broom coasting");
            Check(rider.BroomTipClearance<.04f,"Stopped broom returns to the ground");
            EndClip();
            app.Motor.Warp(Catalog.Home.Landing);yield return Frames(.6f,"Reset");view=new Vector3(4.7f,2.0f,5.6f);
            BeginClip("02-mount-flight-dismount");
            yield return Frames(.5f,"Carry at rest");
            yield return Frames(1.3f,"Mount / push off",Key.Space);
            Check(app.Motor.Mounted&&!app.Motor.Grounded&&rider.FlightPose>.97f,"Space mounts and climbs into a complete riding pose");
            yield return Frames(.7f,"Hover / seated Jiji");
            yield return Frames(.45f,"Accelerate",Key.W);
            yield return Frames(.30f,"Boost",Key.W,Key.LeftShift);
            yield return Frames(.55f,"Brake");
            Check(app.Motor.BeginApproach(Catalog.Home),"Flight can enter the original assisted landing approach");
            yield return Frames(4.5f,"Approach / step off / plant broom");
            Check(app.Motor.Grounded&&app.Motor.OnFoot&&rider.FlightPose<.02f,"Landing automatically dismounts into the standing carry pose");
            yield return Frames(.7f,"Walk after touchdown",Key.S);
            Check(app.Motor.OnFoot&&rider.WalkingWeight>.8f,"Walking works immediately after a flight and landing");
            yield return Frames(.5f,"Settle");EndClip();
            app.Motor.Warp(Catalog.Home.Landing);yield return Frames(.6f,"Reset");view=new Vector3(-4.5f,2.1f,5.6f);
            BeginClip("03-controller-ground-and-mount");
            start=app.Motor.transform.position;
            InputSystem.QueueStateEvent(pad,new GamepadState{leftStick=new Vector2(0,.5f)});
            yield return Frames(1.5f,"Analog half-speed walk");
            float halfSpeed=new Vector2(app.Motor.Velocity.x,app.Motor.Velocity.z).magnitude;
            Check(app.Motor.OnFoot&&halfSpeed>.3f&&halfSpeed<FlightMotor.WalkSpeed*.75f,"Controller stick supports a slower analog walk");
            InputSystem.QueueStateEvent(pad,new GamepadState());yield return Frames(.7f,"Controller stop");
            InputSystem.QueueStateEvent(pad,new GamepadState().WithButton(GamepadButton.South));yield return Frames(1.1f,"Controller mount / climb");
            Check(app.Motor.Mounted&&!app.Motor.Grounded,"Controller A mounts and climbs");
            InputSystem.QueueStateEvent(pad,new GamepadState().WithButton(GamepadButton.East));yield return Frames(2.3f,"Controller descend / dismount");
            InputSystem.QueueStateEvent(pad,new GamepadState());yield return Frames(.8f,"Controller rest");
            Check(app.Motor.OnFoot&&app.Motor.Grounded,"Controller descent returns to walking");EndClip();
            Check(maxGrip<.03f,"Contacting hands follow the broom through walking and transitions within 3 cm");
            Check(maxContact<.04f,"Planted shoes meet the street within 4 cm");
            Check(maxSlide<.035f,"Planted walking feet stay fixed within 3.5 cm per drawing");
            checks.Add($"Maximum hand error: {maxGrip:F4} m; sole error: {maxContact:F4} m; planted-foot displacement: {maxSlide:F4} m; walking speed: {peakWalk:F3} m/s");
            Finish();
        }
        void LateUpdate()
        {
            if(!camera||!app)return;
            camera.cullingMask=1<<9;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.86f,.85f,.80f);camera.fieldOfView=32;
            camera.transform.position=app.Motor.Visual.TransformPoint(view);camera.transform.LookAt(app.Motor.Visual.TransformPoint(new Vector3(0,1.45f,0)));
        }
        void BeginClip(string name)
        {
            clip=name;frame=0;recording=true;Directory.CreateDirectory(Path.Combine(output,clip));
            table=new StreamWriter(Path.Combine(output,clip+".csv"));table.WriteLine("frame,seconds,action,beat,speed,grounded,mounted,walk,flight,gripError,soleError,footSlide,broomClearance,smear");
        }
        void EndClip(){recording=false;table?.Dispose();table=null;}
        IEnumerator Frames(float seconds,string label,params Key[] keys)
        {
            action=label;InputSystem.QueueStateEvent(keyboard,new KeyboardState(keys));
            for(int i=0;i<Mathf.RoundToInt(seconds*24);i++)
            {
                yield return new WaitForEndOfFrame();
                if(!recording)continue;
                float speed=new Vector2(app.Motor.Velocity.x,app.Motor.Velocity.z).magnitude;
                maxGrip=Mathf.Max(maxGrip,rider.GripError);
                if(app.Motor.Grounded&&rider.FlightPose<.02f)
                {
                    maxContact=Mathf.Max(maxContact,rider.GroundContactError);maxSlide=Mathf.Max(maxSlide,rider.PlantedFootSlide);peakWalk=Mathf.Max(peakWalk,speed);
                }
                table.WriteLine(string.Format(CultureInfo.InvariantCulture,"{0},{1:F4},{2},{3},{4:F3},{5},{6},{7:F3},{8:F3},{9:F5},{10:F5},{11:F5},{12:F5},{13:F3}",frame,frame/24f,action,rider.Beat,speed,app.Motor.Grounded,app.Motor.Mounted,rider.WalkingWeight,rider.FlightPose,rider.GripError,rider.GroundContactError,rider.PlantedFootSlide,rider.BroomTipClearance,rider.SmearWeight));
                if(Record||i==3||i==8||i==16||i==25)ScreenCapture.CaptureScreenshot(Path.Combine(output,clip,$"frame_{frame:D5}.png"));
                frame++;
            }
        }
        void Check(bool value,string message){checks.Add((value?"PASS ":"FAIL ")+message);failed|=!value;Debug.Log("KORIKO_WALK "+checks[checks.Count-1]);}
        void Finish()
        {
            EndClip();InputSystem.QueueStateEvent(keyboard,new KeyboardState());InputSystem.RemoveDevice(pad);if(addedKeyboard)InputSystem.RemoveDevice(keyboard);Time.captureFramerate=0;
            checks.Add($"Renderer: {SystemInfo.graphicsDeviceName} / {SystemInfo.graphicsDeviceType}; resolution {Screen.width} x {Screen.height}");
            checks.Add("Synthetic keyboard and controller input, fixed 24 Hz animation review; not a performance benchmark or physical controller test.");
            File.WriteAllLines(Path.Combine(output,"result.txt"),checks);Debug.Log("KORIKO_WALK_CHECK "+(failed?"FAILED":"PASSED"));Application.Quit(failed?1:0);
        }
        void OnDestroy(){table?.Dispose();Time.captureFramerate=0;}
    }
}
