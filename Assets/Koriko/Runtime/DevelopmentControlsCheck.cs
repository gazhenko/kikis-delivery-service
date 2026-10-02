using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Koriko.Core;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.InputSystem.DualShock;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Koriko
{
    /// <summary>Input-device-to-gameplay checks in a native player. Uses the real
    /// Input System, motor, camera, UI raycaster and controller submit/navigation.</summary>
    public sealed class DevelopmentControlsCheck:MonoBehaviour
    {
        public static bool Requested=>Array.IndexOf(Environment.GetCommandLineArgs(),"--koriko-controls-check")>=0;
        GameApp app;Keyboard keys;Mouse mouse;Gamepad pad;
        string output;bool failed;readonly List<string> checks=new List<string>();
        float Speed=>new Vector2(app.Motor.Velocity.x,app.Motor.Velocity.z).magnitude;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Install(){if(Requested)new GameObject("Native controls matrix").AddComponent<DevelopmentControlsCheck>();}
        IEnumerator Start()
        {
            output=Path.Combine(Application.persistentDataPath,"controls-check");Directory.CreateDirectory(output);
            app=FindFirstObjectByType<GameApp>();keys=InputSystem.AddDevice<Keyboard>();mouse=InputSystem.AddDevice<Mouse>();pad=InputSystem.AddDevice<Gamepad>();
            Application.runInBackground=true;InputSystem.settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;Time.captureFramerate=30;
            yield return null;app.Begin(Difficulty.Cozy);app.Hud.ClosePanel();app.Rules.State.elapsed=75;
            yield return Wait(.6f);
            var start=app.Motor.transform.position;
            yield return Keyboard(.9f,Key.W);float straight=Speed;
            Check(app.Motor.OnFoot&&Vector3.Distance(start,app.Motor.transform.position)>1,"Keyboard W walks without mounting");
            yield return Keyboard(.6f,Key.W,Key.D);
            Check(Mathf.Abs(Speed-straight)<.08f,"Keyboard diagonal walking is normalized");
            yield return Keyboard(.6f);Check(Speed<.04f,"Keyboard release stops walking");
            float yaw=app.Motor.CameraOrbit.eulerAngles.y;
            yield return MouseLook(new Vector2(12,0),.3f);
            Check(Mathf.Abs(Mathf.DeltaAngle(yaw,app.Motor.CameraOrbit.eulerAngles.y))>8,"Free mouse look turns the real camera");
            app.Input.AlwaysMouseLook=false;yaw=app.Motor.CameraOrbit.eulerAngles.y;
            yield return MouseLook(new Vector2(12,0),.2f);
            Check(Mathf.Abs(Mathf.DeltaAngle(yaw,app.Motor.CameraOrbit.eulerAngles.y))<.1f,"Hold-to-look mode ignores unheld mouse motion");
            yield return MouseLook(new Vector2(12,0),.2f,true);
            Check(Mathf.Abs(Mathf.DeltaAngle(yaw,app.Motor.CameraOrbit.eulerAngles.y))>5,"Holding the right mouse button enables camera look");
            app.Input.AlwaysMouseLook=true;app.Input.InvertLookY=false;
            yield return MouseLook(new Vector2(0,6),.1f);float normal=app.Input.Look.y;
            app.Input.InvertLookY=true;yield return MouseLook(new Vector2(0,6),.1f);
            Check(normal>0&&app.Input.Look.y<0,"Mouse vertical inversion reverses look input");app.Input.InvertLookY=false;
            yield return Keyboard(.1f,Key.R);
            Check(Mathf.Abs(Mathf.DeltaAngle(app.Motor.CameraOrbit.eulerAngles.y,app.Motor.Visual.eulerAngles.y))<1,"Keyboard R recenters behind Kiki");
            yield return Keyboard(.4f);
            start=app.Motor.transform.position;
            yield return Controller(.5f,new GamepadState{leftStick=new Vector2(.08f,.07f),rightStick=new Vector2(.06f,.08f)});
            Check(Vector3.Distance(start,app.Motor.transform.position)<.015f,"Controller stick drift inside the dead zone does not move Kiki");
            yield return Controller(.6f,new GamepadState{leftStick=new Vector2(0,.5f)});float half=Speed;
            yield return Controller(.6f,new GamepadState{leftStick=Vector2.up});
            Check(half>.3f&&half<1.2f&&Speed>1.9f,"Controller analog walking spans slow to full speed");
            Check(app.Input.Controller,"Controller activity immediately selects controller prompts");
            yield return Controller(.5f,new GamepadState());
            yield return Keyboard(.2f,Key.W);Check(!app.Input.Controller,"Keyboard input immediately reclaims controls from a connected gamepad");
            yield return Keyboard(3.2f);Check(!app.Input.Controller,"An idle connected gamepad does not steal prompts after a timeout");
            app.Motor.Warp(Catalog.Home.Landing);yield return Wait(.3f);
            yield return Keyboard(1.2f,Key.Space);Check(app.Motor.Mounted&&app.Motor.transform.position.y>6,"Keyboard Space mounts and climbs");
            yield return Keyboard(.5f);start=app.Motor.transform.position;
            yield return Keyboard(.8f,Key.F);
            Check(app.Motor.Cruising&&Speed>10,"Holding F toggles cruise once and flies without holding W");
            yield return Keyboard(.7f,Key.LeftShift);Check(app.Motor.Boosting&&Speed>20,"Shift boosts while cruising");
            yield return Keyboard(.45f,Key.W,Key.LeftShift,Key.Q);
            Check(!app.Motor.Cruising&&!app.Motor.Boosting&&Speed<.7f,"Q brake overrides movement and boost and cancels cruise");
            yield return Keyboard(.2f);float height=app.Motor.transform.position.y;
            yield return Keyboard(.55f,Key.C);Check(app.Motor.transform.position.y<height-2,"Keyboard C descends");
            yield return Keyboard(.4f);app.Motor.Warp(new Point(-70,28,-31));yield return Wait(.3f);
            yield return Controller(.8f,new GamepadState().WithButton(GamepadButton.LeftStick));
            Check(app.Motor.Cruising&&Speed>10,"Controller L3 toggles sustained cruise once");
            yield return Controller(.7f,new GamepadState{rightTrigger=1});Check(app.Motor.Boosting&&Speed>20,"Controller right trigger boosts cruise");
            yield return Controller(.45f,new GamepadState{leftTrigger=1,rightTrigger=1,leftStick=Vector2.up});
            Check(!app.Motor.Cruising&&!app.Motor.Boosting&&Speed<.7f,"Controller left trigger brakes and overrides boost");
            height=app.Motor.transform.position.y;yaw=app.Motor.CameraOrbit.eulerAngles.y;
            yield return Controller(.65f,new GamepadState{rightStick=new Vector2(.65f,0)}.WithButton(GamepadButton.RightShoulder));
            Check(app.Motor.transform.position.y>height+2&&Mathf.Abs(Mathf.DeltaAngle(yaw,app.Motor.CameraOrbit.eulerAngles.y))>10,"Right bumper climbs while the right stick remains available to look");
            height=app.Motor.transform.position.y;yield return Controller(.8f,new GamepadState().WithButton(GamepadButton.LeftShoulder));
            Check(app.Motor.transform.position.y<height-2,"Left bumper descends");
            yield return Controller(.1f,new GamepadState().WithButton(GamepadButton.RightStick));
            Check(Mathf.Abs(Mathf.DeltaAngle(app.Motor.CameraOrbit.eulerAngles.y,app.Motor.Visual.eulerAngles.y))<1,"Controller R3 recenters the camera");
            yield return Controller(.2f,new GamepadState());
            yield return Controller(.7f,new GamepadState().WithButton(GamepadButton.LeftStick));
            InputSystem.RemoveDevice(pad);pad=null;yield return Wait(.6f);
            Check(!app.Motor.Cruising&&Speed<.5f&&!app.Input.Controller,"Controller disconnection cancels cruise and returns to keyboard control");
            pad=InputSystem.AddDevice<Gamepad>();yield return Wait(.2f);
            yield return Menus();
            yield return StreetLanding();
            // Exercise the PlayStation family mapping without claiming a USB or
            // Bluetooth hardware test. The same actions drive both layouts.
            InputSystem.RemoveDevice(pad);pad=InputSystem.AddDevice<DualShockGamepad>();yield return Wait(.2f);
            yield return Controller(.2f,new GamepadState{leftStick=Vector2.up});
            Check(app.Input.PlayStation&&app.Input.West=="Square"&&app.Input.Rise=="R1"&&app.Input.Controller,"PlayStation layout uses Square and R1 prompts");
            yield return Controller(.3f,new GamepadState());yield return Capture("05-controller-ground-hud");
            Finish();
        }
        IEnumerator Menus()
        {
            app.Motor.Warp(Catalog.Home.Landing);yield return Keyboard(.4f);
            yield return Keyboard(.1f,Key.Escape);yield return Keyboard(.1f);
            Check(app.Hud.PanelOpen&&!app.Input.WantsMouseCapture,"Escape opens settings and releases the mouse");
            var start=app.Motor.transform.position;
            double before=app.Rules.State.elapsed;
            yield return Keyboard(.4f,Key.W,Key.Space,Key.LeftShift);
            Check(app.Motor.OnFoot&&Vector3.Distance(start,app.Motor.transform.position)<.02f&&app.Rules.State.elapsed>before+.3,"Menus block walking and accidental takeoff while time continues");
            yield return Keyboard(.1f);yield return MouseClick("Controls & camera");
            Check(app.Hud.GetComponentsInChildren<Button>().Any(b=>b.name=="Invert vertical look"),"Mouse click opens controls through the UI raycaster");
            bool inverted=app.Input.InvertLookY;yield return MouseClick("Invert vertical look");
            Check(app.Input.InvertLookY!=inverted,"Mouse click changes the camera inversion option");
            Check(app.GetComponentsInChildren<Collider>().All(c=>c.gameObject.layer!=10)&&Physics.GetIgnoreLayerCollision(9,10)
                &&FindObjectsByType<MeshCollider>(FindObjectsSortMode.None).Any(c=>c.gameObject.layer==10),"Detailed facade collision is available to the camera without snagging the rider");
            Check(!Physics.CheckSphere(Camera.main.transform.position,.12f,1<<10,QueryTriggerInteraction.Ignore),"Ground camera clears the bakery awning and facade surfaces");
            yield return Capture("01-controls-mouse");
            float sensitivity=app.Input.ControllerSensitivity;
            Select("Stick sensitivity");yield return Controller(.12f,new GamepadState().WithButton(GamepadButton.South));yield return Controller(.15f,new GamepadState());
            Check(app.Input.ControllerSensitivity!=sensitivity,"Controller submit changes the sensitivity option");
            Select("Mouse sensitivity");yield return Controller(.15f,new GamepadState().WithButton(GamepadButton.DpadDown));yield return Controller(.15f,new GamepadState());
            Check(EventSystem.current.currentSelectedGameObject?.name=="Stick sensitivity","Controller D-pad navigates the controls menu");
            yield return Capture("02-controls-controller");
            yield return Controller(.2f,new GamepadState().WithButton(GamepadButton.East));yield return Controller(.2f,new GamepadState());
            Check(app.Hud.PanelOpen&&app.Hud.GetComponentsInChildren<Button>().Any(b=>b.name=="Controls & camera")&&app.Motor.OnFoot,"Controller B steps back from controls to settings");
            yield return Controller(.2f,new GamepadState().WithButton(GamepadButton.East));yield return Controller(.2f,new GamepadState());
            Check(!app.Hud.PanelOpen&&app.Motor.OnFoot,"Controller B closes settings without descending or mounting");
            yield return Controller(.15f,new GamepadState().WithButton(GamepadButton.Start));yield return Controller(.15f,new GamepadState());
            Select("Back outside");yield return Controller(.5f,new GamepadState().WithButton(GamepadButton.South));
            Check(!app.Hud.PanelOpen&&app.Motor.OnFoot,"Holding controller submit while closing a menu does not launch Kiki");
            yield return Controller(.15f,new GamepadState());yield return Controller(.6f,new GamepadState().WithButton(GamepadButton.South));
            Check(app.Motor.Mounted,"A fresh controller A press takes off after the menu button is released");
            yield return Controller(.4f,new GamepadState());
            yield return Controller(.15f,new GamepadState().WithButton(GamepadButton.Start));yield return Controller(.15f,new GamepadState());
            float height=app.Motor.transform.position.y;
            yield return Controller(.4f,new GamepadState().WithButton(GamepadButton.East));
            Check(!app.Hud.PanelOpen&&Mathf.Abs(app.Motor.transform.position.y-height)<.2f,"Holding menu cancel does not bleed into in-flight descent");
            yield return Controller(.2f,new GamepadState());
            app.Input.InvertLookY=false;app.Input.ControllerSensitivity=1;
        }
        IEnumerator StreetLanding()
        {
            Vector3 target=Vector3.zero;bool found=false;
            for(int x=-75;x<=75&&!found;x+=15)
            {
                var origin=new Vector3(x,14,-32);
                if(!Physics.Raycast(origin,Vector3.down,out var hit,16,1<<8,QueryTriggerInteraction.Ignore)||hit.normal.y<.9f||hit.point.y>1)continue;
                if(Catalog.Destinations.Any(d=>new Point(hit.point.x,hit.point.y,hit.point.z).HorizontalDistance(d.Landing)<18))continue;
                if(Physics.CheckCapsule(hit.point+Vector3.up*.48f,hit.point+Vector3.up*1.72f,.4f,1<<8,QueryTriggerInteraction.Ignore))continue;
                target=hit.point;found=true;
            }
            Check(found,"A clear street landing surface exists outside delivery courts");if(!found)yield break;
            app.Motor.Warp(new Point(target.x,target.y+6,target.z));yield return Keyboard(.3f);
            yield return Keyboard(.1f,Key.E);yield return Keyboard(.15f);
            Check(app.Motor.Approaching,"Keyboard E starts landing on a clear ordinary street");
            yield return Keyboard(.1f,Key.E);yield return Keyboard(.1f);
            Check(!app.Motor.Approaching,"A second E cancels the landing approach");
            yield return Controller(.1f,new GamepadState().WithButton(GamepadButton.West));yield return Controller(.2f,new GamepadState());
            Check(app.Motor.Approaching,"Controller X starts the same street landing assistance");
            yield return Controller(.2f,new GamepadState{leftTrigger=1});Check(!app.Motor.Approaching,"Controller brake cancels assisted landing");
            yield return Controller(.1f,new GamepadState());yield return Controller(.1f,new GamepadState().WithButton(GamepadButton.West));yield return Controller(4,new GamepadState());
            Check(app.Motor.OnFoot&&app.Motor.Grounded,"Street landing dismounts safely into walking");
            yield return Capture("03-street-landing");
            app.Hud.ToggleOptions();yield return Wait(.2f);Select("Back outside");yield return Keyboard(.1f,Key.Enter);yield return Keyboard(.2f);
            Check(!app.Hud.PanelOpen&&app.Motor.OnFoot,"Keyboard Enter submits the focused menu button without mounting");
            yield return Capture("04-keyboard-ground-hud");
        }
        void Select(string name)
        {
            var button=app.Hud.GetComponentsInChildren<Button>().FirstOrDefault(b=>b.name==name&&b.interactable);
            if(!button){Check(false,"Missing UI button: "+name);return;}EventSystem.current.SetSelectedGameObject(button.gameObject);
        }
        IEnumerator MouseClick(string name)
        {
            yield return Wait(.12f);
            var button=app.Hud.GetComponentsInChildren<Button>().FirstOrDefault(b=>b.name==name&&b.interactable);
            if(!button){Check(false,"Missing mouse target: "+name);yield break;}
            Canvas.ForceUpdateCanvases();var rect=(RectTransform)button.transform;
            var position=RectTransformUtility.WorldToScreenPoint(null,rect.TransformPoint(rect.rect.center));
            InputSystem.QueueStateEvent(mouse,new MouseState{position=position});yield return Wait(.1f);
            InputSystem.QueueStateEvent(mouse,new MouseState{position=position}.WithButton(MouseButton.Left));yield return Wait(.1f);
            InputSystem.QueueStateEvent(mouse,new MouseState{position=position});yield return Wait(.12f);
        }
        IEnumerator MouseLook(Vector2 delta,float seconds,bool held=false)
        {
            InputSystem.QueueStateEvent(keys,new KeyboardState());if(pad!=null)InputSystem.QueueStateEvent(pad,new GamepadState());
            for(int i=0;i<Mathf.RoundToInt(seconds*30);i++)
            {var state=new MouseState{delta=delta};if(held)state=state.WithButton(MouseButton.Right);InputSystem.QueueStateEvent(mouse,state);yield return null;}
        }
        IEnumerator Keyboard(float seconds,params Key[] pressed)
        {
            if(pad!=null)InputSystem.QueueStateEvent(pad,new GamepadState());InputSystem.QueueStateEvent(mouse,new MouseState());InputSystem.QueueStateEvent(keys,new KeyboardState(pressed));yield return Wait(seconds);
        }
        IEnumerator Controller(float seconds,GamepadState state)
        {
            InputSystem.QueueStateEvent(keys,new KeyboardState());InputSystem.QueueStateEvent(mouse,new MouseState());InputSystem.QueueStateEvent(pad,state);yield return Wait(seconds);
        }
        IEnumerator Wait(float seconds){for(int i=0;i<Mathf.Max(2,Mathf.RoundToInt(seconds*30));i++)yield return null;}
        IEnumerator Capture(string name){yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(Path.Combine(output,name+".png"));yield return Wait(.2f);}
        void Check(bool value,string label){checks.Add((value?"PASS ":"FAIL ")+label);failed|=!value;Debug.Log("KORIKO_CONTROLS "+checks[checks.Count-1]);}
        void Finish()
        {
            InputSystem.RemoveDevice(keys);InputSystem.RemoveDevice(mouse);if(pad!=null)InputSystem.RemoveDevice(pad);Time.captureFramerate=0;
            checks.Add($"Renderer: {SystemInfo.graphicsDeviceName} / {SystemInfo.graphicsDeviceType}; resolution {Screen.width} x {Screen.height}");
            checks.Add("Native synthetic Keyboard, Mouse, Gamepad and DualShockGamepad events. Real motor, camera, menu navigation and raycasting. No physical USB/Bluetooth controller or mouse ergonomics test; fixed capture timing is not a performance benchmark.");
            File.WriteAllLines(Path.Combine(output,"result.txt"),checks);Debug.Log("KORIKO_CONTROLS_CHECK "+(failed?"FAILED":"PASSED"));Application.Quit(failed?1:0);
        }
        void OnDestroy(){Time.captureFramerate=0;}
    }
}
