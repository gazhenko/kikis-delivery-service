using System;
using System.Collections;
using System.IO;
using Koriko.Core;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace Koriko
{
    /// <summary>Opt-in trailer capture: a scripted tour through the real game at a fixed 24 fps,
    /// written as PNG drawings for Tools/make-trailer.sh. Uses the real motor, camera and HUD and
    /// never reads or writes player saves. Sound is mixed afterwards from the exported clips.</summary>
    public sealed class DevelopmentTrailerCapture : MonoBehaviour
    {
        public static bool Requested=>Array.IndexOf(Environment.GetCommandLineArgs(),"--koriko-trailer")>=0;
        GameApp app;Keyboard keyboard;Camera view;CinemachineBrain brain;Canvas hud;
        string output;int frame;StreamWriter log;
        Vector3? cinemaPosition;float cinemaFov=46,cinemaDrift;int cinemaStart;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Install(){if(Requested)new GameObject("Trailer capture").AddComponent<DevelopmentTrailerCapture>();}
        IEnumerator Start()
        {
            output=Path.Combine(Application.persistentDataPath,"trailer");Directory.CreateDirectory(output);
            foreach(var old in Directory.GetFiles(output,"*.png"))File.Delete(old);
            app=FindFirstObjectByType<GameApp>();keyboard=InputSystem.AddDevice<Keyboard>();
            view=Camera.main;brain=view.GetComponent<CinemachineBrain>();hud=app.Hud.GetComponent<Canvas>();
            Application.runInBackground=true;InputSystem.settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;
            Time.captureFramerate=24;
            log=new StreamWriter(Path.Combine(output,"shots.csv"));log.WriteLine("shot,firstFrame,frames");
            var sound=app.Sound;float until=Time.realtimeSinceStartup+8;
            while(sound&&!sound.Ready&&Time.realtimeSinceStartup<until)yield return null;
            yield return null;
            // 1. The late-afternoon flyover that sits behind the title.
            hud.enabled=false;
            yield return Shot("01-flyover",5.5f);
            // 2. Morning. The camera settles down to Kiki in the courtyard and she walks out, parcel on the broom.
            app.Begin(Difficulty.Cozy);app.Hud.ClosePanel();app.Rules.State.elapsed=22;
            app.Rules.Accept(app.Rules.State.jobs[0].id);app.Motor.Face(95);
            yield return Shot("02-courtyard",2.6f);
            yield return Shot("03-walk-out",3.4f,Key.W);
            // 3. Push off and climb over the bakery roof.
            yield return Shot("04-takeoff",1.4f,Key.Space);
            yield return Shot("05-climb",2.4f,Key.W,Key.Space);
            // 4. Along the shopping street toward the clock tower, with the game's tag and ring showing.
            app.Motor.Warp(new Point(-96,16,2));app.Motor.Face(90);hud.enabled=true;
            yield return Shot("06-street",6.0f,Key.W);
            hud.enabled=false;
            // 5. A fixed painter's view of the square as she crosses it.
            app.Motor.Warp(new Point(-46,13,10));app.Motor.Face(84);
            Cinema(new Vector3(-6,5.5f,34),52);
            yield return Shot("07-square-wide",4.2f,Key.W);
            Cinema(null);
            // 6. The harbour: the quay, the boats, the gulls and the lighthouse ahead.
            app.Rules.State.elapsed=40;
            app.Motor.Warp(new Point(-30,11,-62));app.Motor.Face(92,-2);
            yield return Shot("08-harbour",5.5f,Key.W);
            // 7. Landing in the clock square and handing the loaf over.
            var clock=Catalog.FindDestination("clock");
            app.Motor.Warp(new Point(clock.Landing.x-6,clock.Landing.y+7,clock.Landing.z-4));app.Motor.Face(200,16);hud.enabled=true;
            yield return Tap(Key.E);
            yield return Shot("09-landing",3.6f);
            if(app.Rules.CanDeliver){yield return Tap(Key.E);}
            yield return Shot("10-delivery",3.2f);
            hud.enabled=false;
            // 8. Evening over the lamplit streets with the moonlight lantern.
            app.Rules.State.elapsed=229;app.Rules.State.upgrades[(int)Upgrade.Lantern]=1;
            app.Motor.Warp(new Point(-70,13,1));app.Motor.Face(90,6);
            yield return Shot("11-lamplight",5.0f,Key.W);
            // 9. The moon over the bay.
            app.Motor.Warp(new Point(24,27,-38));app.Motor.Face(Mathf.Atan2(Daylight.MoonDirection.x,Daylight.MoonDirection.z)*Mathf.Rad2Deg,-7);
            yield return Shot("12-moon",4.2f,Key.W);
            // 10. Home at dusk: Kiki and Jiji at the bakery door, a slow push-in for the title.
            app.Rules.State.elapsed=146;app.Rules.State.upgrades[(int)Upgrade.Lantern]=0;
            app.Motor.Warp(Catalog.Home.Landing);app.Motor.Face(170);
            Cinema(new Vector3(-109.5f,2.4f,3.2f),34,.08f);
            yield return Shot("13-home",4.5f);
            Cinema(null);
            log.Dispose();Time.captureFramerate=0;
            File.WriteAllText(Path.Combine(output,"done.txt"),frame+" frames at 24 fps, "+Screen.width+" x "+Screen.height);
            Debug.Log("KORIKO_TRAILER_DONE "+output);
            Application.Quit(0);
        }
        void Cinema(Vector3? position,float fov=46,float drift=0)
        {
            cinemaPosition=position;cinemaFov=fov;cinemaDrift=drift;cinemaStart=frame;
            if(brain)brain.enabled=position==null;
            if(position==null)view.fieldOfView=52;
        }
        void LateUpdate()
        {
            if(cinemaPosition==null||!app)return;
            // A fixed painter's viewpoint that follows Kiki with its gaze, with an optional slow push-in.
            var target=app.Motor.Visual.TransformPoint(new Vector3(0,1.25f,0));
            var position=cinemaPosition.Value;
            if(cinemaDrift>0)position=Vector3.MoveTowards(position,target,cinemaDrift*(frame-cinemaStart)/24f);
            view.transform.position=position;view.transform.LookAt(target);view.fieldOfView=cinemaFov;
        }
        IEnumerator Tap(Key key)
        {
            InputSystem.QueueStateEvent(keyboard,new KeyboardState());yield return null;
            InputSystem.QueueStateEvent(keyboard,new KeyboardState(key));yield return null;yield return null;
            InputSystem.QueueStateEvent(keyboard,new KeyboardState());yield return null;
        }
        IEnumerator Shot(string name,float seconds,params Key[] keys)
        {
            int first=frame,count=Mathf.RoundToInt(seconds*24);
            InputSystem.QueueStateEvent(keyboard,new KeyboardState(keys));
            for(int i=0;i<count;i++)
            {
                yield return new WaitForEndOfFrame();
                ScreenCapture.CaptureScreenshot(Path.Combine(output,$"frame_{frame:D5}.png"));frame++;
            }
            InputSystem.QueueStateEvent(keyboard,new KeyboardState());
            log.WriteLine($"{name},{first},{count}");log.Flush();
        }
        void OnDestroy(){log?.Dispose();Time.captureFramerate=0;}
    }
}
