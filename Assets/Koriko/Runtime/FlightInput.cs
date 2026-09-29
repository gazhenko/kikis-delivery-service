using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.DualShock;

namespace Koriko
{
    public sealed class FlightInput : MonoBehaviour
    {
        public Vector2 Move { get; private set; }
        public Vector2 Look { get; private set; }
        public float Lift { get; private set; }
        public bool Boost { get; private set; }
        public bool Brake { get; private set; }
        public bool CruiseToggle { get; private set; }
        public bool Recenter { get; private set; }
        public bool Interact { get; private set; }
        public bool Menu { get; private set; }
        public bool Back { get; private set; }
        public bool Controller { get; private set; }
        public bool DeviceLost {get;private set;}
        public bool PlayStation=>Gamepad.current is DualShockGamepad;
        public string South=>PlayStation?"Cross":"A";
        public string West=>PlayStation?"Square":"X";
        public string North=>PlayStation?"Triangle":"Y";
        public string Rise=>PlayStation?"R1":"RB";
        public string Descend=>PlayStation?"L1":"LB";
        public string BoostButton=>PlayStation?"R2":"RT";
        public string BrakeButton=>PlayStation?"L2":"LT";
        public float MouseSensitivity=1,ControllerSensitivity=1,Deadzone=.15f,FollowSpeed=.8f;
        public bool InvertLookY,AlwaysMouseLook=true;
        public bool WantsMouseCapture {get;private set;}
        bool releaseLift,focused=true,captured;
        Vector4 previousPadAxes;
        Gamepad previousPad;
        const string Preferences="koriko.controls.";

        void Awake()
        {
            if(DevelopmentFlightCheck.Requested)return;
            MouseSensitivity=Mathf.Clamp(PlayerPrefs.GetFloat(Preferences+"mouse",1),.5f,2);
            ControllerSensitivity=Mathf.Clamp(PlayerPrefs.GetFloat(Preferences+"stick",1),.5f,2);
            Deadzone=Mathf.Clamp(PlayerPrefs.GetFloat(Preferences+"deadzone",.15f),.08f,.3f);
            FollowSpeed=Mathf.Clamp(PlayerPrefs.GetFloat(Preferences+"follow",.8f),0,1.6f);
            InvertLookY=PlayerPrefs.GetInt(Preferences+"invert",0)!=0;
            AlwaysMouseLook=PlayerPrefs.GetInt(Preferences+"freeMouse",1)!=0;
        }
        public void SavePreferences()
        {
            if(DevelopmentFlightCheck.Requested)return;
            PlayerPrefs.SetFloat(Preferences+"mouse",MouseSensitivity);PlayerPrefs.SetFloat(Preferences+"stick",ControllerSensitivity);
            PlayerPrefs.SetFloat(Preferences+"deadzone",Deadzone);PlayerPrefs.SetFloat(Preferences+"follow",FollowSpeed);
            PlayerPrefs.SetInt(Preferences+"invert",InvertLookY?1:0);PlayerPrefs.SetInt(Preferences+"freeMouse",AlwaysMouseLook?1:0);PlayerPrefs.Save();
        }
        public static Vector2 ReadStick(Vector2 raw,float deadzone)
        {
            float size=raw.magnitude;if(size<=deadzone)return Vector2.zero;
            return raw/size*Mathf.Clamp01((size-deadzone)/(1-deadzone));
        }
        public void RequireLiftRelease(){releaseLift=true;Lift=0;}
        public void Read(bool menuOpen=false)
        {
            Move=Look=Vector2.zero;Lift=0;Boost=Brake=CruiseToggle=Recenter=Interact=Menu=Back=DeviceLost=false;
            var k=Keyboard.current;var g=Gamepad.current;var mouse=Mouse.current;
            Vector2 rawMove=g!=null?g.leftStick.ReadUnprocessedValue():Vector2.zero;
            Vector2 rawLook=g!=null?g.rightStick.ReadUnprocessedValue():Vector2.zero;
            Vector2 stickMove=ReadStick(rawMove,Deadzone),stickLook=ReadStick(rawLook,Deadzone);
            Vector4 axes=new Vector4(rawMove.x,rawMove.y,rawLook.x,rawLook.y);
            if(g!=previousPad){DeviceLost=previousPad!=null;previousPadAxes=axes;previousPad=g;Controller=false;}
            bool padActivity=g!=null&&(((axes-previousPadAxes).sqrMagnitude>.008f&&(stickMove.sqrMagnitude+stickLook.sqrMagnitude)>.001f)
                ||g.buttonSouth.wasPressedThisFrame||g.buttonEast.wasPressedThisFrame||g.buttonWest.wasPressedThisFrame||g.buttonNorth.wasPressedThisFrame
                ||g.leftShoulder.wasPressedThisFrame||g.rightShoulder.wasPressedThisFrame||g.leftTrigger.wasPressedThisFrame||g.rightTrigger.wasPressedThisFrame
                ||g.leftStickButton.wasPressedThisFrame||g.rightStickButton.wasPressedThisFrame||g.startButton.wasPressedThisFrame||(g.dpad.up.isPressed||g.dpad.down.isPressed||g.dpad.left.isPressed||g.dpad.right.isPressed));
            previousPadAxes=axes;
            Vector2 mouseDelta=mouse!=null?mouse.delta.ReadValue():Vector2.zero;
            bool keyboardActivity=k!=null&&k.anyKey.wasPressedThisFrame;
            bool mouseActivity=mouse!=null&&(mouseDelta.sqrMagnitude>.25f||mouse.leftButton.wasPressedThisFrame||mouse.rightButton.wasPressedThisFrame);
            if(padActivity)Controller=true;
            if(keyboardActivity||mouseActivity)Controller=false;
            if(Controller&&g!=null)
            {
                Move=stickMove;
                // Fine aiming around the center, full turn rate at the rim.
                Look=stickLook*Mathf.Lerp(.55f,1,stickLook.magnitude)*115*ControllerSensitivity*Time.unscaledDeltaTime;
                Lift=((g.buttonSouth.isPressed||g.rightShoulder.isPressed)?1:0)-((g.buttonEast.isPressed||g.leftShoulder.isPressed)?1:0);
                Boost=g.rightTrigger.ReadValue()>.25f;Brake=g.leftTrigger.ReadValue()>.25f;
                CruiseToggle=g.leftStickButton.wasPressedThisFrame;Recenter=g.rightStickButton.wasPressedThisFrame;
            }
            else
            {
                if(k!=null)
                {
                    Move=new Vector2((k.dKey.isPressed?1:0)-(k.aKey.isPressed?1:0),(k.wKey.isPressed?1:0)-(k.sKey.isPressed?1:0));
                    Lift=(k.spaceKey.isPressed?1:0)-((k.leftCtrlKey.isPressed||k.cKey.isPressed)?1:0);
                    Boost=k.leftShiftKey.isPressed;Brake=k.qKey.isPressed;CruiseToggle=k.fKey.wasPressedThisFrame;Recenter=k.rKey.wasPressedThisFrame;
                }
                if(mouse!=null&&(AlwaysMouseLook||mouse.rightButton.isPressed))Look=mouseDelta*.11f*MouseSensitivity;
            }
            // Menu/interaction edges remain available from either device. An
            // idle connected controller cannot steal movement or HUD prompts.
            if(k!=null){Interact=k.eKey.wasPressedThisFrame;Menu=k.tabKey.wasPressedThisFrame;Back=k.escapeKey.wasPressedThisFrame;}
            if(g!=null){Interact|=g.buttonWest.wasPressedThisFrame;Menu|=g.buttonNorth.wasPressedThisFrame;Back|=g.startButton.wasPressedThisFrame||(menuOpen&&g.buttonEast.wasPressedThisFrame);}
            if(InvertLookY)Look=new Vector2(Look.x,-Look.y);
            Move=Vector2.ClampMagnitude(Move,1);
            if(menuOpen){releaseLift=true;CruiseToggle=Recenter=false;}
            // UI submit/cancel also live on A/B: require release before those
            // held buttons can mount or descend when the menu closes.
            if(releaseLift){if(Mathf.Abs(Lift)<.1f)releaseLift=false;Lift=0;}
            WantsMouseCapture=!menuOpen&&AlwaysMouseLook&&!Controller&&focused;
            if(!DevelopmentFlightCheck.Requested)
            {
                if(WantsMouseCapture&&!captured){Cursor.lockState=CursorLockMode.Locked;Cursor.visible=false;captured=true;}
                else if(!WantsMouseCapture&&captured)ReleaseCursor();
            }
        }
        void ReleaseCursor(){Cursor.lockState=CursorLockMode.None;Cursor.visible=true;captured=false;}
        void OnApplicationFocus(bool value){focused=value;if(!value){releaseLift=true;if(captured)ReleaseCursor();}}
        void OnDisable(){if(captured)ReleaseCursor();}
    }
}
