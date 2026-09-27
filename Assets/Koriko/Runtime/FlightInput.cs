using UnityEngine;
using UnityEngine.InputSystem;

namespace Koriko
{
    public sealed class FlightInput : MonoBehaviour
    {
        public Vector2 Move { get; private set; }
        public Vector2 Look { get; private set; }
        public float Lift { get; private set; }
        public bool Boost { get; private set; }
        public bool Interact { get; private set; }
        public bool Menu { get; private set; }
        public bool Back { get; private set; }
        public bool Controller => Gamepad.current != null && (Keyboard.current == null || Time.unscaledTime-lastKeyboard>3);
        float lastKeyboard;

        public void Read()
        {
            Move=Vector2.zero;Look=Vector2.zero;Lift=0;Boost=Interact=Menu=Back=false;
            var k=Keyboard.current;var g=Gamepad.current;var mouse=Mouse.current;
            if(k!=null)
            {
                Move=new Vector2((k.dKey.isPressed?1:0)-(k.aKey.isPressed?1:0),(k.wKey.isPressed?1:0)-(k.sKey.isPressed?1:0));
                Lift=(k.spaceKey.isPressed?1:0)-((k.leftCtrlKey.isPressed||k.cKey.isPressed)?1:0);
                Boost=k.leftShiftKey.isPressed;Interact=k.eKey.wasPressedThisFrame;Menu=k.tabKey.wasPressedThisFrame;Back=k.escapeKey.wasPressedThisFrame;
                if(k.anyKey.wasPressedThisFrame)lastKeyboard=Time.unscaledTime;
            }
            if(mouse!=null&&mouse.rightButton.isPressed)Look=mouse.delta.ReadValue()*.10f;
            if(g!=null)
            {
                var move=g.leftStick.ReadValue();if(move.sqrMagnitude>.015f)Move=move;
                var look=g.rightStick.ReadValue();if(look.sqrMagnitude>.02f)Look+=look*110*Time.unscaledDeltaTime;
                Lift+=(g.buttonSouth.isPressed?1:0)-(g.buttonEast.isPressed?1:0);
                Boost|=g.rightTrigger.ReadValue()>.35f;Interact|=g.buttonWest.wasPressedThisFrame;Menu|=g.buttonNorth.wasPressedThisFrame;Back|=g.startButton.wasPressedThisFrame;
            }
            Move=Vector2.ClampMagnitude(Move,1);Lift=Mathf.Clamp(Lift,-1,1);
        }
    }
}
