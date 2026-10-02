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
    /// <summary>Opt-in deterministic native motion reel. Real input, physics and
    /// rider rendering, captured at 24 simulation frames/sec. Not a benchmark.</summary>
    [DefaultExecutionOrder(100)]
    public sealed class DevelopmentMotionCheck:MonoBehaviour
    {
        public static bool Requested=>Array.IndexOf(Environment.GetCommandLineArgs(),"--koriko-motion-check")>=0;
        GameApp app;
        RiderPerformance rider;
        Keyboard keyboard;
        Camera reviewCamera;
        bool addedKeyboard,isolated=true,recording,failed;
        int frame,totalFrames,smearFrames,currentSmearRun,longestSmearRun;
        string output,clip,action;
        Vector3 view=new Vector3(4.6f,2.05f,5.1f);
        StreamWriter telemetry;
        readonly List<string> checks=new List<string>();
        float maxGrip,maxContact,minBank,maxBank,minPitch=100,maxPitch=-100,minBow=100,maxBow=-100,maxGaze,peakSpeed;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Install(){if(Requested)new GameObject("Native motion review").AddComponent<DevelopmentMotionCheck>();}
        IEnumerator Start()
        {
            output=Path.Combine(Application.persistentDataPath,"motion-check");Directory.CreateDirectory(output);
            app=FindFirstObjectByType<GameApp>();if(!app){Finish("Missing GameApp");yield break;}
            keyboard=Keyboard.current;if(keyboard==null){keyboard=InputSystem.AddDevice<Keyboard>();addedKeyboard=true;}
            Application.runInBackground=true;InputSystem.settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;
            Time.captureFramerate=24;
            reviewCamera=Camera.main;reviewCamera.GetComponent<CinemachineBrain>().enabled=false;
            yield return null;
            app.Begin(Difficulty.Cozy);app.Hud.ClosePanel();app.Hud.GetComponent<Canvas>().enabled=false;
            rider=app.Motor.Visual.GetComponentInChildren<RiderPerformance>();
            if(!rider||!rider.HasSecondaryShapes){Finish("Missing performance morphs");yield break;}
            app.Rules.State.elapsed=75;
            yield return Frames(.6f,"Settle");
            BeginClip("01-takeoff-boost-brake");
            yield return Frames(1.3f,"Standing / breathe");
            yield return Frames(2.4f,"Takeoff",Key.Space);
            yield return Frames(.8f,"Hover");
            yield return Frames(1.0f,"Accelerate",Key.W);
            yield return Frames(1.1f,"Boost",Key.W,Key.LeftShift);
            yield return Frames(.8f,"Cruise",Key.W);
            yield return Frames(1.5f,"Brake / recover");
            EndClip();
            app.Motor.Warp(new Point(0,50,0));yield return Frames(.9f,"Reset in clear air");
            view=new Vector3(3.8f,2.2f,5.3f);
            BeginClip("02-banks-and-smears");
            yield return Frames(1.2f,"Accelerate",Key.W);
            yield return Frames(.9f,"Bank right",Key.W,Key.D);
            yield return Frames(.8f,"Boost right",Key.D,Key.LeftShift);
            yield return Frames(.8f,"Reverse left",Key.A,Key.LeftShift);
            yield return Frames(.8f,"Turn back",Key.S);
            yield return Frames(1.4f,"Brake / follow through");
            yield return Frames(2.4f,"Hover / feet / Jiji");
            EndClip();
            var home=Catalog.Home.Landing;
            app.Motor.Warp(new Point(home.x,home.y+8,home.z));yield return Frames(.7f,"Reset above landing court");
            view=new Vector3(4.8f,1.95f,4.7f);
            BeginClip("03-landing-and-rest");
            yield return Frames(.5f,"Hover");
            yield return Frames(2.4f,"Descend / touch down",Key.C);
            yield return Frames(3.7f,"Absorb / recover / look around");
            EndClip();
            Check(app.Motor.Grounded&&rider.GroundContactError<.035f,"Landing returns visible shoes to the court within 3.5 cm");
            Check(rider.SmearWeight==0,"Rest has no smear deformation");
            // A fourth clip keeps the game's real camera, environment and HUD.
            isolated=false;reviewCamera.cullingMask=~0;reviewCamera.clearFlags=CameraClearFlags.Skybox;reviewCamera.GetComponent<CinemachineBrain>().enabled=true;
            app.Hud.GetComponent<Canvas>().enabled=true;
            app.Motor.Warp(new Point(-85,31,-32));yield return Frames(.7f,"Gameplay camera settle");
            BeginClip("04-gameplay-camera");
            yield return Frames(1.1f,"Cruise",Key.W);
            yield return Frames(1.0f,"Boost",Key.W,Key.LeftShift);
            yield return Frames(.9f,"Turn right",Key.W,Key.D);
            yield return Frames(.9f,"Bank left",Key.A);
            yield return Frames(1.0f,"Brake");
            yield return Frames(1.2f,"Climb",Key.Space);
            yield return Frames(1.8f,"Hover");
            EndClip();
            // The three airborne Warps are shot setup, not player takeoffs.
            Check(rider.Takeoffs>=1&&rider.Landings>=1,"Real controls exercise takeoff, cruising, steering, braking and touchdown");
            Check(peakSpeed>23,"Boost reaches actual flight speed above 23 m/s");
            Check(maxGrip<.025f,"Hands follow the animated broom within 2.5 cm throughout the reel");
            Check(maxContact<.035f,"Grounded drawings keep the lowest shoe on the floor");
            Check(minBank< -8&&maxBank>8&&maxGaze>12,"Left/right banks and leading gaze produce distinct poses");
            Check(maxPitch-minPitch>16&&maxBow-minBow>12,"Body action and delayed bow motion have visible pose range");
            Check(smearFrames>=6&&longestSmearRun<=3&&rider.SmearCount>=3,"Smears occur briefly on fast actions, never as a continuous trail");
            Check(rider.HasSecondaryShapes,"Hair and cloth wind morphs survived native import");
            checks.Add($"Samples: {totalFrames}; smear drawings: {smearFrames}; longest exposure: {longestSmearRun}/24 s; events: {rider.SmearCount}; takeoffs: {rider.Takeoffs}; touchdowns: {rider.Landings}");
            checks.Add($"Max hand error {maxGrip:F4} m; max sole error {maxContact:F4} m; bank range {minBank:F1}..{maxBank:F1} degrees; body pitch {minPitch:F1}..{maxPitch:F1}");
            checks.Add($"Bow overlap range {minBow:F1}..{maxBow:F1} degrees; peak speed {peakSpeed:F2} m/s");
            yield return null;Finish();
        }
        void LateUpdate()
        {
            if(!app||!reviewCamera||!isolated)return;
            reviewCamera.cullingMask=1<<9;reviewCamera.clearFlags=CameraClearFlags.SolidColor;reviewCamera.backgroundColor=new Color(.86f,.85f,.80f);reviewCamera.fieldOfView=33;
            reviewCamera.transform.position=app.Motor.Visual.TransformPoint(view);
            reviewCamera.transform.LookAt(app.Motor.Visual.TransformPoint(new Vector3(0,1.32f,-.1f)));
        }
        void BeginClip(string name)
        {
            clip=name;frame=0;recording=true;Directory.CreateDirectory(Path.Combine(output,clip));
            telemetry=new StreamWriter(Path.Combine(output,clip+".csv"));
            telemetry.WriteLine("frame,seconds,action,beat,speed,grounded,bodyPitch,bodyBank,gaze,bow,smear,gripError,soleError");
        }
        void EndClip(){recording=false;telemetry?.Dispose();telemetry=null;}
        IEnumerator Frames(float seconds,string label,params Key[] keys)
        {
            action=label;InputSystem.QueueStateEvent(keyboard,new KeyboardState(keys));
            for(int i=0;i<Mathf.RoundToInt(seconds*24);i++)
            {
                yield return new WaitForEndOfFrame();
                if(!recording)continue;
                float velocity=new Vector2(app.Motor.Velocity.x,app.Motor.Velocity.z).magnitude;
                maxGrip=Mathf.Max(maxGrip,rider.GripError);peakSpeed=Mathf.Max(peakSpeed,velocity);
                if(app.Motor.Grounded&&float.IsFinite(rider.GroundContactError))maxContact=Mathf.Max(maxContact,rider.GroundContactError);
                minBank=Mathf.Min(minBank,rider.BodyBank);maxBank=Mathf.Max(maxBank,rider.BodyBank);maxGaze=Mathf.Max(maxGaze,Mathf.Abs(rider.Gaze));
                minPitch=Mathf.Min(minPitch,rider.BodyPitch);maxPitch=Mathf.Max(maxPitch,rider.BodyPitch);minBow=Mathf.Min(minBow,rider.SecondarySwing);maxBow=Mathf.Max(maxBow,rider.SecondarySwing);
                if(rider.SmearWeight>0){smearFrames++;currentSmearRun++;longestSmearRun=Mathf.Max(longestSmearRun,currentSmearRun);}else currentSmearRun=0;
                telemetry.WriteLine(string.Format(CultureInfo.InvariantCulture,"{0},{1:F4},{2},{3},{4:F3},{5},{6:F3},{7:F3},{8:F3},{9:F3},{10:F3},{11:F5},{12:F5}",frame,frame/24f,action,rider.Beat,velocity,app.Motor.Grounded,rider.BodyPitch,rider.BodyBank,rider.Gaze,rider.SecondarySwing,rider.SmearWeight,rider.GripError,rider.GroundContactError));
                ScreenCapture.CaptureScreenshot(Path.Combine(output,clip,$"frame_{frame:D5}.png"));frame++;totalFrames++;
            }
        }
        void Check(bool value,string message){checks.Add((value?"PASS ":"FAIL ")+message);failed|=!value;}
        void Finish(string error=null)
        {
            EndClip();if(error!=null){failed=true;checks.Add("FAIL "+error);}
            if(keyboard!=null){InputSystem.QueueStateEvent(keyboard,new KeyboardState());if(addedKeyboard)InputSystem.RemoveDevice(keyboard);}
            Time.captureFramerate=0;
            checks.Add("Native Unity captures: "+SystemInfo.graphicsDeviceName+" / "+SystemInfo.graphicsDeviceType+" at "+Screen.width+" x "+Screen.height);
            checks.Add("Fixed 24 Hz simulation capture for animation review, using synthetic keyboard input. This is not a frame-rate benchmark or a physical controller test.");
            File.WriteAllLines(Path.Combine(output,"result.txt"),checks);Debug.Log("KORIKO_MOTION_CHECK "+(failed?"FAILED":"PASSED"));Application.Quit(failed?1:0);
        }
        void OnDestroy(){telemetry?.Dispose();Time.captureFramerate=0;}
    }
}
