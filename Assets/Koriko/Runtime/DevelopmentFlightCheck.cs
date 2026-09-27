using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Koriko.Core;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace Koriko
{
    /// <summary>Opt-in native-player check. Drives the real input system, motor and camera.
    /// It never loads or writes the player's save. Results are evidence only after it runs.</summary>
    public sealed class DevelopmentFlightCheck : MonoBehaviour
    {
        public static bool Requested => Debug.isDebugBuild&&Array.IndexOf(Environment.GetCommandLineArgs(),"--koriko-flight-check")>=0;
        GameApp app;
        Keyboard keyboard;
        bool addedKeyboard,failed;
        string output;
        readonly List<string> checks=new List<string>();
        float elapsed,frameTotal;
        int frames;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Install()
        {
            if(Requested)new GameObject("Development flight check").AddComponent<DevelopmentFlightCheck>();
        }
        IEnumerator Start()
        {
            output=Path.Combine(Application.persistentDataPath,"flight-check");Directory.CreateDirectory(output);
            app=FindFirstObjectByType<GameApp>();
            if(!app){Fail("Missing GameApp");yield break;}
            keyboard=Keyboard.current;
            if(keyboard==null){keyboard=InputSystem.AddDevice<Keyboard>();addedKeyboard=true;}
            Application.runInBackground=true;
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
            yield return FlyTo(Catalog.Home);
            if(failed)yield break;
            double before=app.Rules.State.elapsed;
            app.Sleep();
            Check(app.Rules.State.elapsed-before>=Rules.SleepSeconds&&app.Rules.State.elapsed-before<Rules.SleepSeconds+.1,"Sleep advances eight hours");
            Check(app.Rules.State.energy==100,"Sleep restores energy");
            app.Hud.ShowBakery();yield return Capture("05-return-and-rest");
            foreach(var renderer in FindObjectsByType<Renderer>(FindObjectsSortMode.None))
                foreach(var material in renderer.sharedMaterials)
                    if(material==null||material.shader==null||!material.shader.isSupported){Fail("Unsupported or missing scene material");yield break;}
            Check(true,"All scene material shaders supported on this renderer");
            Finish();
        }
        IEnumerator FlyTo(Destination destination)
        {
            float ceiling=Mathf.Max(app.Motor.transform.position.y+18,(float)destination.Landing.y+17);
            float timeout=Time.realtimeSinceStartup+20;
            while(app.Motor.transform.position.y<ceiling-.6f)
            {
                if(Time.realtimeSinceStartup>timeout){Fail("Takeoff blocked before "+destination.Id);yield break;}
                Keys(Key.Space);yield return null;
            }
            Keys();
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
                if(Time.realtimeSinceStartup>timeout){Fail("Landing blocked at "+destination.Id);yield break;}
                Keys();yield return null;
            }
            yield return new WaitForSeconds(.4f);
            Check(app.Motor.Frame.Speed<2.5,"Land slowly at "+destination.Id);
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
        void Update(){elapsed+=Time.unscaledDeltaTime;if(elapsed>3){frameTotal+=Time.unscaledDeltaTime;frames++;}}
        void Check(bool ok,string name){if(!ok){Fail(name);return;}checks.Add("PASS "+name);Debug.Log("KORIKO_CHECK "+name);}
        void Fail(string message){if(failed)return;failed=true;checks.Add("FAIL "+message);Finish();}
        void Finish()
        {
            if(keyboard!=null){Keys();if(addedKeyboard)InputSystem.RemoveDevice(keyboard);}
            checks.Add("Renderer: "+SystemInfo.graphicsDeviceName+" / "+SystemInfo.graphicsDeviceType);
            checks.Add("Resolution: "+Screen.width+" x "+Screen.height);
            checks.Add("Mean frame time across this automated route: "+(frames>0?frameTotal/frames*1000:0).ToString("F2")+" ms");
            checks.Add("Synthetic keyboard input; this does not establish physical controller or human playtest quality.");
            File.WriteAllLines(Path.Combine(output,"result.txt"),checks);
            Debug.Log("KORIKO_FLIGHT_CHECK "+(failed?"FAILED":"PASSED")+" "+output);
            Application.Quit(failed?1:0);
        }
    }
}
