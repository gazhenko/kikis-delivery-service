using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Koriko.Core;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Unity.Profiling;
using Unity.Cinemachine;

namespace Koriko
{
    /// <summary>Opt-in native-player check. Drives the real input system, motor and camera.
    /// It never loads or writes the player's save. Results are evidence only after it runs.</summary>
    public sealed class DevelopmentFlightCheck : MonoBehaviour
    {
        public static bool ArtOnly => Array.IndexOf(Environment.GetCommandLineArgs(),"--koriko-art-check")>=0;
        public static bool CharacterOnly => Array.IndexOf(Environment.GetCommandLineArgs(),"--koriko-character-check")>=0;
        public static bool EnvironmentOnly => Array.IndexOf(Environment.GetCommandLineArgs(),"--koriko-environment-check")>=0;
        public static bool Requested => (ArtOnly||CharacterOnly||EnvironmentOnly||DevelopmentControlsCheck.Requested||DevelopmentWalkCheck.Requested||DevelopmentMotionCheck.Requested||DevelopmentTourCheck.Requested||DevelopmentTrailerCapture.Requested||Array.IndexOf(Environment.GetCommandLineArgs(),"--koriko-flight-check")>=0);
        GameApp app;
        Keyboard keyboard;
        Gamepad gamepad;
        bool addedKeyboard,failed;
        string output;
        readonly List<string> checks=new List<string>();
        float elapsed,frameTotal;
        int frames;
        ProfilerRecorder drawCalls,triangles;
        long peakDrawCalls,peakTriangles;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Install()
        {
            if(Requested&&!DevelopmentMotionCheck.Requested&&!DevelopmentWalkCheck.Requested&&!DevelopmentControlsCheck.Requested&&!DevelopmentTourCheck.Requested&&!DevelopmentTrailerCapture.Requested)new GameObject("Development flight check").AddComponent<DevelopmentFlightCheck>();
        }
        IEnumerator Start()
        {
            output=Path.Combine(Application.persistentDataPath,EnvironmentOnly?"environment-check":CharacterOnly?"character-check":ArtOnly?"art-check":"flight-check");Directory.CreateDirectory(output);
            drawCalls=ProfilerRecorder.StartNew(ProfilerCategory.Render,"Draw Calls Count",1);
            triangles=ProfilerRecorder.StartNew(ProfilerCategory.Render,"Triangles Count",1);
            app=FindFirstObjectByType<GameApp>();
            if(!app){Fail("Missing GameApp");yield break;}
            keyboard=Keyboard.current;
            if(keyboard==null){keyboard=InputSystem.AddDevice<Keyboard>();addedKeyboard=true;}
            Application.runInBackground=true;
            InputSystem.settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;
            yield return null;
            app.Begin(Difficulty.Cozy);app.Hud.ClosePanel();
            if(EnvironmentOnly){yield return EnvironmentViews();if(!failed)Finish();yield break;}
            if(CharacterOnly){yield return CharacterViews();if(!failed)Finish();yield break;}
            if(ArtOnly){yield return ArtViews();if(!failed)Finish();yield break;}
            Check(app.Rules.Accept(app.Rules.State.jobs[0].id),"Collect bakery parcel");
            yield return Capture("01-bakery");
            yield return FlyTo(Catalog.FindDestination("clock"));
            if(failed)yield break;
            yield return KeyFrames(Key.E);
            Check(app.Rules.State.delivered==1,"Deliver at clock square through interaction input");
            yield return Capture("02-clock-square");
            yield return FlyTo(Catalog.FindDestination("harbor"));
            if(failed)yield break;
            yield return Capture("03-harbor");
            yield return FlyTo(Catalog.FindDestination("madame"));
            if(failed)yield break;
            yield return Capture("04-madame-garden");
            yield return FlyTo(Catalog.FindDestination("tombo"));
            if(failed)yield break;
            yield return Capture("04b-tombo-workshop");
            // Approach below the envelope; the cargo court is under the airship.
            yield return FlyTo(Catalog.FindDestination("airship"),46);
            if(failed)yield break;
            yield return Capture("05-airship-platform");
            yield return FlyTo(Catalog.Home,46);
            if(failed)yield break;
            double before=app.Rules.State.elapsed;
            app.Sleep();
            // Decimal simulation timestamps can subtract to 99.99999999999999.
            // Use a symmetric sub-microsecond tolerance instead of a strict lower bound.
            Check(Math.Abs(app.Rules.State.elapsed-before-Rules.SleepSeconds)<1e-7,"Sleep advances eight hours");if(failed)yield break;
            Check(app.Rules.State.energy==100,"Sleep restores energy");
            yield return new WaitForSeconds(1.6f);
            app.Hud.ShowBakery();yield return Capture("06-return-and-rest");
            yield return CheckBakeryWithGamepad();
            if(failed)yield break;
            var restored=new Rules();
            Check(restored.Restore(JsonUtility.FromJson<State>(JsonUtility.ToJson(app.Rules.State)))&&restored.State.delivered==app.Rules.State.delivered&&restored.State.money==app.Rules.State.money,"Unity save serialization roundtrip");
            app.Hud.ClosePanel();
            app.Rules.State.elapsed=75;yield return Capture("07-noon");
            app.Rules.State.elapsed=147;yield return Capture("08-sunset");
            app.Rules.State.elapsed=225;yield return Capture("09-night");
            foreach(var renderer in FindObjectsByType<Renderer>(FindObjectsSortMode.None))
                foreach(var material in renderer.sharedMaterials)
                    if(material==null||material.shader==null||!material.shader.isSupported){Fail("Unsupported or missing scene material");yield break;}
            Check(true,"All scene material shaders supported on this renderer");
            Finish();
        }
        IEnumerator CharacterViews()
        {
            var camera=Camera.main;camera.GetComponent<CinemachineBrain>().enabled=false;
            app.Hud.GetComponent<Canvas>().enabled=false;app.enabled=false;
            camera.cullingMask=1<<9;camera.clearFlags=CameraClearFlags.SolidColor;
            app.Rules.State.elapsed=75;
            yield return CharacterShot("01-front",new Vector3(0,1.65f,5.4f),new Vector3(0,1.30f,0),33,camera);
            yield return CharacterShot("02-three-quarter",new Vector3(3.6f,2.1f,5),new Vector3(0,1.30f,0),33,camera);
            yield return CharacterShot("03-profile",new Vector3(5.4f,1.65f,0),new Vector3(0,1.30f,0),33,camera);
            yield return CharacterShot("04-rear",new Vector3(-3.2f,1.9f,-5),new Vector3(0,1.30f,-.1f),33,camera);
            yield return CharacterShot("05-portrait",new Vector3(1.35f,2.14f,3),new Vector3(0,2.08f,.03f),24,camera);
            var rider=app.Motor.Visual.GetComponentInChildren<RiderPerformance>();
            Check(rider&&rider.GripError<.02f,"Standing carry keeps the holding hand on the broom (within 2 cm)");if(failed)yield break;
            Check(rider.HasFlightCloth,"Imported flight cloth shape is available");if(failed)yield break;
            // Closing the bakery requires a released lift button before takeoff (the menu
            // guard); give the re-enabled app one neutral frame, as a player would.
            app.enabled=true;Keys();yield return null;yield return null;Keys(Key.Space);
            float ceiling=app.Motor.transform.position.y+25;
            float timeout=Time.realtimeSinceStartup+8;
            while(app.Motor.transform.position.y<ceiling)
            {
                if(Time.realtimeSinceStartup>timeout){Fail("Character study takeoff blocked");yield break;}
                yield return null;
            }
            Keys(Key.W,Key.LeftShift);yield return new WaitForSeconds(2);
            app.enabled=false;Keys();
            Check(!app.Motor.Grounded&&new Vector2(app.Motor.Velocity.x,app.Motor.Velocity.z).magnitude>20,"Character cruising pose uses the real flight motor above 20 m/s");if(failed)yield break;
            yield return CharacterShot("06-flight-three-quarter",new Vector3(3.6f,2.1f,5),new Vector3(0,1.38f,-.1f),33,camera);
            yield return CharacterShot("07-flight-profile",new Vector3(5.4f,1.65f,0),new Vector3(0,1.38f,-.1f),33,camera);
            yield return CharacterShot("08-flight-rear",new Vector3(-3.2f,2.4f,-5),new Vector3(0,1.38f,-.1f),33,camera);
            Check(rider.GripError<.02f,"Leaning flight pose keeps both hands on the broom (within 2 cm)");if(failed)yield break;
            bool clothFollowsPose=true;int clothSurfaces=0;
            foreach(var renderer in app.Motor.Visual.GetComponentsInChildren<SkinnedMeshRenderer>())
                for(int i=0;i<renderer.sharedMesh.blendShapeCount;i++)
                    if(renderer.sharedMesh.GetBlendShapeName(i).EndsWith("Flight cloth"))
                    {clothSurfaces++;clothFollowsPose&=renderer.GetBlendShapeWeight(i)>95;}
            Check(clothSurfaces>=2&&clothFollowsPose,"Flight cloth, drawn folds and contour follow the bent-knee pose");if(failed)yield break;
            foreach(var renderer in app.Motor.Visual.GetComponentsInChildren<Renderer>())
                foreach(var material in renderer.sharedMaterials)
                    if(!material||!material.shader||!material.shader.isSupported){Fail("Unsupported character material");yield break;}
            Check(true,"Character materials supported on this renderer");
        }
        IEnumerator CharacterShot(string name,Vector3 position,Vector3 target,float fov,Camera camera)
        {
            camera.transform.position=app.Motor.Visual.TransformPoint(position);
            camera.transform.LookAt(app.Motor.Visual.TransformPoint(target));camera.fieldOfView=fov;
            app.Lighting.Refresh(app.Rules,true);camera.backgroundColor=new Color(.86f,.85f,.80f);
            yield return Capture(name);
        }
        IEnumerator EnvironmentViews()
        {
            app.Rules.State.elapsed=75;
            yield return Capture("01-bakery-player-view");
            app.enabled=false;app.Hud.GetComponent<Canvas>().enabled=false;
            var camera=Camera.main;camera.GetComponent<CinemachineBrain>().enabled=false;
            camera.cullingMask=1<<8;camera.fieldOfView=52;
            yield return EnvironmentShot("02-town-roofscape",new Vector3(-125,62,-85),new Vector3(-8,5,43),75,camera);
            yield return EnvironmentShot("03-bakery-street",new Vector3(-137,4,-2),new Vector3(-110,5,20),75,camera);
            yield return EnvironmentShot("04-clock-square",new Vector3(-16,10,1),new Vector3(24,15,36),75,camera);
            yield return EnvironmentShot("05-garden-rooms",new Vector3(56,27,15),new Vector3(110,4,55),75,camera);
            yield return EnvironmentShot("06-public-garden",new Vector3(-10,10,46),new Vector3(25,3,68),75,camera);
            yield return EnvironmentShot("07-harbor-street",new Vector3(93,5,-71),new Vector3(123,5,-43),75,camera);
            yield return EnvironmentShot("08-working-waterfront",new Vector3(-28,20,-130),new Vector3(18,2,-52),75,camera);
            yield return EnvironmentShot("09-pasture-orchard",new Vector3(-164,22,73),new Vector3(-118,8,124),75,camera);
            yield return EnvironmentShot("10-madame-approach",new Vector3(77,12,75),new Vector3(92,13,111),75,camera);
            yield return EnvironmentShot("11-bakery-evening",new Vector3(-132,5,-3),new Vector3(-112,4,23),147,camera);
            yield return EnvironmentShot("12-town-night",new Vector3(-30,24,-8),new Vector3(20,9,32),225,camera);
            foreach(var renderer in FindObjectsByType<Renderer>(FindObjectsSortMode.None))
                foreach(var material in renderer.sharedMaterials)
                    if(!material||!material.shader||!material.shader.isSupported){Fail("Unsupported environment material on "+renderer.name);yield break;}
            Check(true,"Twelve native environment views captured; all scene shaders supported");
        }
        IEnumerator EnvironmentShot(string name,Vector3 position,Vector3 target,double time,Camera camera)
        {
            camera.transform.position=position;camera.transform.LookAt(target);
            app.Rules.State.elapsed=time;app.Lighting.Refresh(app.Rules,true);
            yield return Capture(name);
        }
        IEnumerator ArtViews()
        {
            // Fixed native viewpoints for art review. No player saves or gameplay assertions.
            app.Rules.State.elapsed=75;
            yield return Capture("01-bakery-noon-hud");
            app.enabled=false;
            var canvas=app.Hud.GetComponent<Canvas>();canvas.enabled=false;
            var follow=FindFirstObjectByType<CinemachineThirdPersonFollow>();
            follow.Damping=Vector3.zero;
            yield return ArtView("02-bakery-front",Catalog.Home.Landing,0,12,10,75,follow);
            yield return ArtView("03-kiki-three-quarter",Catalog.Home.Landing,235,3,3.4f,75,follow);
            yield return ArtView("04-clock-square",Catalog.FindDestination("clock").Landing,12,-8,12,75,follow);
            yield return ArtView("05-garden-overlook",new Point(90,14,91),-130,24,10,75,follow);
            yield return ArtView("06-painted-sea",new Point(121,10,-55),190,12,10,75,follow);
            yield return ArtView("07-bakery-sunset",Catalog.Home.Landing,0,12,10,147,follow);
            yield return ArtView("08-bakery-night",Catalog.Home.Landing,0,12,10,225,follow);
            foreach(var renderer in FindObjectsByType<Renderer>(FindObjectsSortMode.None))
                foreach(var material in renderer.sharedMaterials)
                    if(material==null||material.shader==null||!material.shader.isSupported){Fail("Unsupported or missing art material: "+(material?material.name:"null")+" on "+renderer.name);yield break;}
            Check(true,"Art viewpoints captured with all scene shaders supported");
        }
        IEnumerator ArtView(string name,Point point,float yaw,float pitch,float distance,double time,CinemachineThirdPersonFollow follow)
        {
            app.Motor.Warp(point);app.Rules.State.elapsed=time;
            follow.CameraDistance=distance;
            app.Motor.CameraOrbit.rotation=Quaternion.Euler(pitch,yaw,0);
            yield return new WaitForSeconds(.25f);
            app.Lighting.Refresh(app.Rules,true);
            yield return Capture(name);
        }
        IEnumerator FlyTo(Destination destination,float cruiseHeight=-1)
        {
            float ceiling=cruiseHeight>0?cruiseHeight:Mathf.Max(app.Motor.transform.position.y+18,(float)destination.Landing.y+17);
            float timeout=Time.realtimeSinceStartup+20;
            while(app.Motor.transform.position.y<ceiling-.6f)
            {
                if(Time.realtimeSinceStartup>timeout){Fail("Takeoff blocked before "+destination.Id);yield break;}
                Keys(Key.Space);yield return null;
            }
            Keys();
            yield return Capture("Cruise-toward-"+destination.Id);
            var target=new Vector3((float)destination.Landing.x,ceiling,(float)destination.Landing.z);
            timeout=Time.realtimeSinceStartup+50;
            while(Vector2.Distance(new Vector2(app.Motor.transform.position.x,app.Motor.transform.position.z),new Vector2(target.x,target.z))>7)
            {
                if(Time.realtimeSinceStartup>timeout){Fail("Flight timed out approaching "+destination.Id);yield break;}
                var delta=target-app.Motor.transform.position;
                var local=Quaternion.Inverse(Quaternion.Euler(0,app.Motor.CameraOrbit.eulerAngles.y,0))*delta;
                float threshold=new Vector2(local.x,local.z).magnitude*.30f;
                var keys=new List<Key>();
                if(local.z>threshold)keys.Add(Key.W);else if(local.z< -threshold)keys.Add(Key.S);
                if(local.x>threshold)keys.Add(Key.D);else if(local.x< -threshold)keys.Add(Key.A);
                if(delta.y>.8f)keys.Add(Key.Space);else if(delta.y< -.8f)keys.Add(Key.C);
                Keys(keys.ToArray());yield return null;
            }
            Keys();yield return new WaitForSeconds(.8f);
            // Enter the same height window as a human pilot before assisted landing.
            timeout=Time.realtimeSinceStartup+20;
            while(app.Motor.transform.position.y>(float)destination.Landing.y+15)
            {
                if(Time.realtimeSinceStartup>timeout){Fail("Descent blocked at "+destination.Id);yield break;}
                Keys(Key.C);yield return null;
            }
            Keys();yield return new WaitForSeconds(.5f);
            yield return KeyFrames(Key.E);
            timeout=Time.realtimeSinceStartup+15;
            while(!app.Motor.Grounded||!app.Rules.Near(destination))
            {
                if(Time.realtimeSinceStartup>timeout){Fail("Landing blocked at "+destination.Id+"; position="+app.Motor.transform.position+" grounded="+app.Motor.Grounded+" approach="+app.Motor.Approaching);yield break;}
                Keys();yield return null;
            }
            yield return new WaitForSeconds(.4f);
            Check(app.Motor.Frame.Speed<2.5,"Land slowly at "+destination.Id);
            var rider=app.Motor.Visual.GetComponentInChildren<RiderPerformance>();
            Check(rider&&rider.GroundContactError<.035f&&Mathf.Abs(rider.GroundFloorHeight-(float)destination.Landing.y)<.075f,"Shoes meet the visible landing court at "+destination.Id);if(failed)yield break;
        }
        IEnumerator CheckBakeryWithGamepad()
        {
            gamepad=InputSystem.AddDevice<Gamepad>();
            yield return PressUi("Pantry");if(failed)yield break;
            int flour=app.Rules.State.pantry[0],money=app.Rules.State.money;
            yield return PressUi("4 coins","Flour");if(failed)yield break;
            Check(app.Rules.State.pantry[0]==flour+1&&app.Rules.State.money==money-4,"Buy ingredient using controller UI submit");
            yield return PressUi("Kitchen");if(failed)yield break;
            double before=app.Rules.State.elapsed;
            yield return PressUi("Cook","Café au lait");if(failed)yield break;
            Check(app.Rules.Cooking,"Recipe starts through controller UI");
            yield return new WaitForSeconds(3.3f);
            Check(app.Rules.Has(Advantage.Awake)&&!app.Rules.Cooking,"Coffee completes and grants its awake buff");
            Check(app.Rules.State.elapsed>=before+3,"Time continues while the kitchen is open");
            // Use actual navigation events to reach a row below the viewport.
            for(int i=0;i<4;i++)
            {
                InputSystem.QueueStateEvent(gamepad,new GamepadState().WithButton(GamepadButton.DpadDown));
                yield return null;yield return null;
                InputSystem.QueueStateEvent(gamepad,new GamepadState());yield return null;yield return null;
            }
            var selected=EventSystem.current.currentSelectedGameObject;
            var scroll=app.Hud.GetComponentInChildren<ScrollRect>();
            Check(selected&&scroll&&scroll.content.anchoredPosition.y>10,"Controller navigation scrolls lower recipe rows into view");
            yield return Capture("Kitchen-controller");
            yield return PressUi("Broom");if(failed)yield break;
            if(app.Rules.State.money>=65)
            {
                yield return PressUi("65 coins","Silver birch bristles  0/3");if(failed)yield break;
                Check(app.Rules.Level(Upgrade.Bristles)==1,"Broom purchase applies through controller UI");
            }
            app.Hud.ClosePanel();
            // Closing a menu deliberately blocks a held submit button from
            // becoming takeoff. Observe a neutral frame before a fresh press.
            InputSystem.QueueStateEvent(gamepad,new GamepadState());yield return null;yield return null;
            float startHeight=app.Motor.transform.position.y;
            float until=Time.realtimeSinceStartup+.6f;
            while(Time.realtimeSinceStartup<until){InputSystem.QueueStateEvent(gamepad,new GamepadState().WithButton(GamepadButton.South));yield return null;}
            InputSystem.QueueStateEvent(gamepad,new GamepadState());yield return null;
            Check(app.Motor.transform.position.y>startHeight+1,"Controller rise input reaches the flight motor");
            if(failed)yield break;
            InputSystem.RemoveDevice(gamepad);gamepad=null;
        }
        IEnumerator PressUi(string name,string row=null)
        {
            yield return null;
            var button=app.Hud.GetComponentsInChildren<Button>().FirstOrDefault(b=>b.name==name&&b.interactable&&(row==null||b.transform.parent.name==row));
            if(!button){Fail("Missing enabled UI action: "+name+" / "+row);yield break;}
            EventSystem.current.SetSelectedGameObject(button.gameObject);
            InputSystem.QueueStateEvent(gamepad,new GamepadState());yield return null;
            InputSystem.QueueStateEvent(gamepad,new GamepadState().WithButton(GamepadButton.South));yield return null;yield return null;
            InputSystem.QueueStateEvent(gamepad,new GamepadState());yield return null;yield return null;
        }
        IEnumerator KeyFrames(Key key)
        {Keys();yield return null;Keys(key);yield return null;yield return null;Keys();yield return null;}
        void Keys(params Key[] keys){InputSystem.QueueStateEvent(keyboard,new KeyboardState(keys));}
        IEnumerator Capture(string name)
        {
            yield return new WaitForSeconds(.2f);yield return new WaitForEndOfFrame();
            ScreenCapture.CaptureScreenshot(Path.Combine(output,name+".png"));
            yield return new WaitForSeconds(.3f);
        }
        void Update()
        {
            elapsed+=Time.unscaledDeltaTime;
            if(elapsed>3){frameTotal+=Time.unscaledDeltaTime;frames++;peakDrawCalls=Math.Max(peakDrawCalls,drawCalls.LastValue);peakTriangles=Math.Max(peakTriangles,triangles.LastValue);}
        }
        void Check(bool ok,string name){if(!ok){Fail(name);return;}checks.Add("PASS "+name);Debug.Log("KORIKO_CHECK "+name);}
        void Fail(string message){if(failed)return;failed=true;checks.Add("FAIL "+message);Finish();}
        void Finish()
        {
            if(gamepad!=null){InputSystem.RemoveDevice(gamepad);gamepad=null;}
            if(keyboard!=null){Keys();if(addedKeyboard)InputSystem.RemoveDevice(keyboard);}
            checks.Add("Renderer: "+SystemInfo.graphicsDeviceName+" / "+SystemInfo.graphicsDeviceType);
            checks.Add("Resolution: "+Screen.width+" x "+Screen.height);
            checks.Add((ArtOnly||CharacterOnly||EnvironmentOnly?"Mean frame time during viewpoint captures (not a benchmark): ":"Mean frame time across this automated route: ")+(frames>0?frameTotal/frames*1000:0).ToString("F2")+" ms");
            if(peakDrawCalls>0)checks.Add("Peak recorded draw calls: "+peakDrawCalls);
            if(peakTriangles>0)checks.Add("Peak recorded triangles: "+peakTriangles);
            drawCalls.Dispose();triangles.Dispose();
            checks.Add(CharacterOnly?"Native character study on a neutral background with synthetic takeoff input; not a route or performance benchmark.":ArtOnly||EnvironmentOnly?"Fixed camera captures for visual inspection; not a traversal or input test.":"Synthetic keyboard and gamepad input; this does not establish physical controller or human playtest quality.");
            File.WriteAllLines(Path.Combine(output,"result.txt"),checks);
            Debug.Log("KORIKO_FLIGHT_CHECK "+(failed?"FAILED":"PASSED")+" "+output);
            Application.Quit(failed?1:0);
        }
    }
}
