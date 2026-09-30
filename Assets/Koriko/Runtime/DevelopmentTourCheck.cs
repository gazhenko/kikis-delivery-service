using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Koriko.Core;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.UI;

namespace Koriko
{
    /// <summary>Opt-in native review of the polish pass: title flyover, sound, carried parcel,
    /// wayfinding, delivery acting, lamplight, harbour life and settings. Uses the real input,
    /// motor, camera and HUD. Never reads or writes the player's save or preferences.</summary>
    public sealed class DevelopmentTourCheck : MonoBehaviour
    {
        public static bool Requested=>Debug.isDebugBuild&&Array.IndexOf(Environment.GetCommandLineArgs(),"--koriko-tour-check")>=0;
        GameApp app;Keyboard keyboard;AudioProbe probe;Camera view;CinemachineBrain brain;
        string output;bool failed;readonly List<string> checks=new List<string>();
        float frameTotal;int frames;bool timing;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Install(){if(Requested)new GameObject("Native polish tour").AddComponent<DevelopmentTourCheck>();}
        IEnumerator Start()
        {
            output=Path.Combine(Application.persistentDataPath,"tour-check");Directory.CreateDirectory(output);Directory.CreateDirectory(Path.Combine(output,"audio"));
            app=FindFirstObjectByType<GameApp>();keyboard=InputSystem.AddDevice<Keyboard>();
            Application.runInBackground=true;InputSystem.settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;
            view=Camera.main;brain=view.GetComponent<CinemachineBrain>();probe=view.gameObject.AddComponent<AudioProbe>();
            var sound=app.Sound;var life=app.Life;var props=app.Motor.Visual.GetComponentInChildren<RiderProps>();var rider=app.Rider;
            // Title: flyover camera, painted card, waltz.
            float until=Time.realtimeSinceStartup+8;
            while(sound&&!sound.Ready&&Time.realtimeSinceStartup<until)yield return null;
            Check(sound&&sound.Ready,"Procedural sound bank synthesizes at startup ("+(sound?sound.Summary:"missing")+")");
            yield return Seconds(2.5f);
            var director=FindFirstObjectByType<CameraDirector>();
            Check(!app.InGame&&director&&(ReferenceEquals(brain.ActiveVirtualCamera,director.Title)),"Title screen uses the flyover camera");
            probe.Reset();yield return Seconds(1);var titleMix=probe.Measure();
            Check(titleMix.valid&&titleMix.rms>.004f&&titleMix.peak<.99f,$"Title waltz and ambience reach the listener without clipping (RMS {titleMix.rms:F4}, peak {titleMix.peak:F3})");
            yield return Capture("01-title");
            timing=true;
            app.Begin(Difficulty.Cozy);yield return Seconds(.8f);
            Check(app.Hud.BakeryOpen&&sound.MusicLevel>.05f,"Bakery board opens with its music");
            yield return Capture("02-bakery-board");
            Check(app.Rules.Accept(app.Rules.State.jobs[0].id),"Collect the market loaf");app.Hud.ClosePanel();
            app.Motor.Face(95);yield return Seconds(1.2f);
            Check(props&&props.ParcelShown,"Collected parcel hangs from the broom");
            Check(life&&life.RingVisible&&life.RingTarget?.Id=="clock","Ribbon circle marks the clock-square court");
            Check(app.Hud.MarkerVisible,"Paper destination tag is shown");
            int steps=sound.Footsteps;
            yield return Hold(1.6f,Key.W);yield return Hold(.5f);
            Check(sound.Footsteps>steps,$"Walking produces planted footstep sounds ({sound.Footsteps-steps})");
            yield return Capture("03-parcel-on-foot");
            // The rear view players actually see while walking: arm swing and Jiji on her shoulder.
            InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.W));yield return Seconds(.7f);
            yield return Capture("03b-walk-rear-a");yield return Seconds(.1f);yield return Capture("03c-walk-rear-b");
            InputSystem.QueueStateEvent(keyboard,new KeyboardState());yield return Seconds(.5f);
            yield return Study("03d-shoulder-jiji-rear",new Vector3(-1.5f,1.75f,-3.3f),new Vector3(-.1f,1.7f,0),32);
            yield return Study("03e-street-eaves",null,null,60,new Vector3(-140,1.6f,3.5f),new Vector3(-138,10.5f,13));
            // Toward the clock tower: the tag should sit on screen over the court.
            var clock=Catalog.FindDestination("clock");
            app.Motor.Warp(new Point(clock.Landing.x-62,24,clock.Landing.z-18));app.Motor.Face(Mathf.Atan2(62,18)*Mathf.Rad2Deg);
            yield return Hold(1.4f,Key.W);
            Check(app.Motor.Mounted&&sound.WindLevel>.12f,$"Flight raises the wind bed ({sound.WindLevel:F2})");
            Check(app.Hud.MarkerVisible&&!app.Hud.MarkerOffscreen,"Destination tag rides over the court when it is in view");
            yield return Capture("04-flight-tag");
            app.Motor.Face(Mathf.Atan2(-62,-18)*Mathf.Rad2Deg);yield return Seconds(.5f);
            Check(app.Hud.MarkerVisible&&app.Hud.MarkerOffscreen,"Destination tag clamps to the edge with an arrow when behind");
            yield return Capture("05-tag-offscreen");
            app.Motor.Warp(new Point(clock.Landing.x,clock.Landing.y+6,clock.Landing.z));app.Motor.Face(200);yield return Seconds(.3f);
            yield return Tap(Key.E);
            until=Time.realtimeSinceStartup+10;while((!app.Motor.Grounded||!app.Rules.Near(clock))&&Time.realtimeSinceStartup<until)yield return null;
            yield return Seconds(.6f);
            Check(app.Rules.CanDeliver,"Assisted landing settles inside the ribbon circle");
            yield return Tap(Key.E);yield return Seconds(.35f);
            Check(app.Rules.State.delivered==1&&rider.Delivering,"Handing over the parcel starts the bow and wave");
            yield return Capture("06-delivery-stamp");
            yield return Study("07a-delivery-handover",new Vector3(-1.2f,1.75f,3.4f),new Vector3(0,1.45f,0),34);
            yield return Seconds(.45f);
            yield return Study("07b-delivery-wave",new Vector3(-1.2f,1.75f,3.4f),new Vector3(0,1.45f,0),34);
            Check(!props.ParcelShown,"The parcel leaves the broom after delivery");
            // Night street: lamplight and lit windows; the lantern once fitted.
            app.Rules.State.elapsed=228;app.Rules.State.upgrades[(int)Upgrade.Lantern]=1;
            app.Motor.Warp(new Point(-60,.2,1));app.Motor.Face(90);yield return Seconds(1.2f);
            Check(life.LampCount>=20,$"Street lamps light after dusk ({life.LampCount} lamps)");
            Check(props.LanternShown,"Fitted moonlight lantern hangs from the broom");
            yield return Capture("08-night-street-lantern");
            app.Motor.Warp(new Point(-60,14,1));app.Motor.Face(90);yield return Seconds(.8f);
            yield return Study("08b-night-lantern-flight",new Vector3(3.2f,1.8f,1.6f),new Vector3(0,1.2f,.5f),36);
            app.Motor.Warp(new Point(20,26,-40));app.Motor.Face(Mathf.Atan2(Daylight.MoonDirection.x,Daylight.MoonDirection.z)*Mathf.Rad2Deg,-6);
            yield return Hold(1.2f,Key.W);yield return Hold(.6f);
            yield return Capture("09-night-harbor-moon");
            // Morning over the water: sunrise path, gulls and chimney smoke.
            app.Rules.State.elapsed=12;app.Rules.State.upgrades[(int)Upgrade.Lantern]=0;
            app.Motor.Warp(new Point(30,20,-52));app.Motor.Face(160);yield return Seconds(1.5f);
            Check(sound.SeaLevel>.12f,$"The sea bed swells near the quay ({sound.SeaLevel:F2})");
            Check(life.NearestGull(view.transform.position).HasValue,"Gulls circle within sight of the harbour");
            probe.Record(12);
            yield return Capture("10-morning-harbor");
            app.Rules.State.elapsed=75;
            yield return Study("11-rooftop-smoke",null,null,52,new Vector3(-150,34,-40),new Vector3(-112,14,24));
            Check(life.ChimneyCount>=10&&life.PuffCount>30,$"Chimney smoke drifts from {life.ChimneyCount} chimneys ({life.PuffCount} puffs)");
            // Harbour boats ride the swell; headlands, islands and a hill town sit in the haze.
            Check(life.BoatCount>=5,$"Moored boats are separate assemblies that ride the swell ({life.BoatCount})");
            yield return Study("11b-harbor-boats",null,null,50,new Vector3(-30,9,-66),new Vector3(10,-1,-104));
            yield return Study("11c-bay-and-headlands",null,null,52,new Vector3(-20,34,-40),new Vector3(160,6,-330));
            yield return Study("11d-hill-town",null,null,48,new Vector3(40,38,20),new Vector3(300,52,300));
            app.Rules.State.elapsed=232;
            Check(life.Lighthouse,"The eastern headland lighthouse carries a lamp");
            yield return Study("11e-night-lighthouse",null,null,40,new Vector3(150,22,-95),new Vector3(402,26,-228));
            app.Rules.State.elapsed=75;
            // Parcel kinds: fragile, heavy and airship freight all look different on the broom.
            var s=app.Rules.State;s.upgrades[(int)Upgrade.Bristles]=1;s.upgrades[(int)Upgrade.Sling]=1;s.upgrades[(int)Upgrade.Stabilizer]=1;s.upgrades[(int)Upgrade.Compass]=1;s.money=500;
            string[] kinds={"Fragile","Heavy","Airship"};
            foreach(var kind in kinds)
            {
                app.Motor.Warp(Catalog.Home.Landing);app.Motor.Face(95);yield return Seconds(.3f);
                var job=app.Rules.State.jobs.FirstOrDefault(j=>j.kind.ToString()==kind&&!j.accepted);
                if(job==null){Check(false,"Missing "+kind+" job");continue;}
                Check(app.Rules.Accept(job.id),"Collect "+kind.ToLowerInvariant()+" freight");
                app.Motor.Warp(new Point(-100,14,9));yield return Seconds(.8f);
                yield return Study("12-parcel-"+kind.ToLowerInvariant(),new Vector3(-2.6f,1.9f,-2.4f),new Vector3(0,1.0f,-.4f),34);
                s.jobs.RemoveAll(j=>j.id==job.id);s.activeId="";yield return Seconds(.4f);
            }
            // Settings pages and back navigation.
            until=Time.realtimeSinceStartup+15;while(!probe.Recorded&&Time.realtimeSinceStartup<until)yield return null;
            Check(probe.Recorded,"Twelve seconds of the final mix were recorded at the listener");
            probe.SaveRecording(Path.Combine(output,"audio","mix-harbor-and-town.wav"));
            app.Motor.Warp(Catalog.Home.Landing);yield return Seconds(.3f);
            app.Hud.ToggleOptions();yield return Seconds(.4f);
            yield return Capture("13-settings");
            Press("Sound & display");yield return Seconds(.4f);
            Check(app.Hud.GetComponentsInChildren<Button>().Any(b=>b.name=="Master volume"),"Sound & display page offers volume, display and hint options");
            yield return Capture("14-sound-display");
            yield return Tap(Key.Escape);yield return Seconds(.3f);
            Check(app.Hud.GetComponentsInChildren<Button>().Any(b=>b.name=="Sound & display"),"Escape returns from a sub-page to settings");
            yield return Tap(Key.Escape);yield return Seconds(.3f);
            Check(!app.Hud.PanelOpen,"A second Escape closes settings");
            // Night crows share cel paint but not the rider's smear frame.
            var flock=FindFirstObjectByType<CrowFlock>();
            Check(flock&&flock.Material&&flock.Material.GetFloat("_Detached")>.5f,"Crows are detached from the rider smear deformation");
            Check(RenderSettings.fog,"Aerial fog is enabled for the painted distance");
            ExportClips(sound);
            int before=app.Rules.State.delivered;
            var backup=File.Exists(app.BackupPath)?File.GetLastWriteTimeUtc(app.BackupPath):DateTime.MinValue;
            app.NewGame(Difficulty.Challenging);yield return Seconds(.5f);
            var after=File.Exists(app.BackupPath)?File.GetLastWriteTimeUtc(app.BackupPath):DateTime.MinValue;
            Check(app.Rules.State.delivered==0&&app.Rules.Day==1&&app.Rules.State.difficulty==Difficulty.Challenging&&backup==after,$"New game starts fresh on day one (previous run had {before} deliveries) without touching player saves");
            Finish();
        }
        void ExportClips(Soundscape sound)
        {
            int count=0;bool clean=true;var lines=new List<string>{"clip,seconds,rate,peak,rms"};
            foreach(var name in sound.ClipNames.OrderBy(n=>n))
            {
                var clip=sound.Clip(name);var data=new float[clip.samples];clip.GetData(data,0);
                float peak=0;double sum=0;foreach(var v in data){if(float.IsNaN(v)){clean=false;break;}peak=Math.Max(peak,Math.Abs(v));sum+=v*v;}
                clean&=peak<=.99f&&peak>.05f;count++;
                lines.Add(FormattableString.Invariant($"{name},{clip.length:F2},{clip.frequency},{peak:F3},{Math.Sqrt(sum/Math.Max(1,data.Length)):F4}"));
                AudioProbe.WriteWav(Path.Combine(output,"audio",name+".wav"),data,data.Length,clip.frequency);
            }
            File.WriteAllLines(Path.Combine(output,"audio","levels.csv"),lines);
            Check(count>=30&&clean,$"{count} synthesized clips exported; all finite, audible and below clipping");
        }
        IEnumerator Study(string name,Vector3? local,Vector3? target,float fov,Vector3? world=null,Vector3? look=null)
        {
            brain.enabled=false;var canvas=app.Hud.GetComponent<Canvas>();canvas.enabled=false;
            float previous=view.fieldOfView;view.fieldOfView=fov;
            if(local.HasValue){view.transform.position=app.Motor.Visual.TransformPoint(local.Value);view.transform.LookAt(app.Motor.Visual.TransformPoint(target.Value));}
            else{view.transform.position=world.Value;view.transform.LookAt(look.Value);}
            // Clock changes made by the tour take effect on the next frame's lighting.
            yield return null;app.Lighting.Refresh(app.Rules,true);
            yield return Capture(name);
            view.fieldOfView=previous;canvas.enabled=true;brain.enabled=true;
        }
        void Press(string name)
        {
            var button=app.Hud.GetComponentsInChildren<Button>().FirstOrDefault(b=>b.name==name&&b.interactable);
            if(!button){Check(false,"Missing button "+name);return;}button.onClick.Invoke();
        }
        IEnumerator Hold(float seconds,params Key[] keys){InputSystem.QueueStateEvent(keyboard,new KeyboardState(keys));yield return Seconds(seconds);}
        IEnumerator Tap(Key key){InputSystem.QueueStateEvent(keyboard,new KeyboardState());yield return null;InputSystem.QueueStateEvent(keyboard,new KeyboardState(key));yield return null;yield return null;InputSystem.QueueStateEvent(keyboard,new KeyboardState());yield return null;}
        IEnumerator Seconds(float seconds){float end=Time.realtimeSinceStartup+seconds;while(Time.realtimeSinceStartup<end)yield return null;}
        IEnumerator Capture(string name){yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(Path.Combine(output,name+".png"));yield return Seconds(.25f);}
        void Update(){if(timing){frameTotal+=Time.unscaledDeltaTime;frames++;}}
        void Check(bool value,string label){checks.Add((value?"PASS ":"FAIL ")+label);failed|=!value;Debug.Log("KORIKO_TOUR "+checks[checks.Count-1]);}
        void Finish()
        {
            InputSystem.QueueStateEvent(keyboard,new KeyboardState());InputSystem.RemoveDevice(keyboard);
            checks.Add($"Renderer: {SystemInfo.graphicsDeviceName} / {SystemInfo.graphicsDeviceType}; resolution {Screen.width} x {Screen.height}; audio output {AudioSettings.outputSampleRate} Hz");
            checks.Add($"Mean frame time during the tour, including captures and warps (not a benchmark): {(frames>0?frameTotal/frames*1000:0):F2} ms");
            checks.Add("Synthetic keyboard input and scripted warps. Sound levels are measured at the listener; no human listening test is implied.");
            File.WriteAllLines(Path.Combine(output,"result.txt"),checks);Debug.Log("KORIKO_TOUR_CHECK "+(failed?"FAILED":"PASSED"));Application.Quit(failed?1:0);
        }
    }
}
