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

namespace Koriko
{
    /// <summary>Opt-in native-player check. Drives the real input system, motor and camera.
    /// It never loads or writes the player's save. Results are evidence only after it runs.</summary>
    public sealed class DevelopmentFlightCheck : MonoBehaviour
    {
        public static bool Requested => Debug.isDebugBuild&&Array.IndexOf(Environment.GetCommandLineArgs(),"--koriko-flight-check")>=0;
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
            if(Requested)new GameObject("Development flight check").AddComponent<DevelopmentFlightCheck>();
        }
        IEnumerator Start()
        {
            output=Path.Combine(Application.persistentDataPath,"flight-check");Directory.CreateDirectory(output);
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
            // Approach below the envelope; the cargo court is under the airship.
            yield return FlyTo(Catalog.FindDestination("airship"),46);
            if(failed)yield break;
            yield return Capture("05-airship-platform");
            yield return FlyTo(Catalog.Home,46);
            if(failed)yield break;
            double before=app.Rules.State.elapsed;
            app.Sleep();
            Check(app.Rules.State.elapsed-before>=Rules.SleepSeconds&&app.Rules.State.elapsed-before<Rules.SleepSeconds+.1,"Sleep advances eight hours");
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
            float startHeight=app.Motor.transform.position.y;
            float until=Time.realtimeSinceStartup+.6f;
            while(Time.realtimeSinceStartup<until){InputSystem.QueueStateEvent(gamepad,new GamepadState().WithButton(GamepadButton.South));yield return null;}
            InputSystem.QueueStateEvent(gamepad,new GamepadState());yield return null;
            Check(app.Motor.transform.position.y>startHeight+1,"Controller rise input reaches the flight motor");
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
            checks.Add("Mean frame time across this automated route: "+(frames>0?frameTotal/frames*1000:0).ToString("F2")+" ms");
            if(peakDrawCalls>0)checks.Add("Peak recorded draw calls: "+peakDrawCalls);
            if(peakTriangles>0)checks.Add("Peak recorded triangles: "+peakTriangles);
            drawCalls.Dispose();triangles.Dispose();
            checks.Add("Synthetic keyboard and gamepad input; this does not establish physical controller or human playtest quality.");
            File.WriteAllLines(Path.Combine(output,"result.txt"),checks);
            Debug.Log("KORIKO_FLIGHT_CHECK "+(failed?"FAILED":"PASSED")+" "+output);
            Application.Quit(failed?1:0);
        }
    }
}
