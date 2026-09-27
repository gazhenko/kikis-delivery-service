using Koriko.Core;
using UnityEngine;

namespace Koriko
{
    [RequireComponent(typeof(CharacterController))]
    public sealed class FlightMotor : MonoBehaviour
    {
        public Transform CameraOrbit;
        public Transform Visual;
        public Vector3 Velocity { get; private set; }
        public bool Grounded { get; private set; }
        public bool Boosting { get; private set; }
        public bool Approaching => approach != null;
        public float Turn { get; private set; }
        CharacterController body;
        Destination approach;
        float yaw=85,pitch=12,lookIdle;
        public FlightFrame Frame => new FlightFrame(new Point(transform.position.x,transform.position.y,transform.position.z),new Vector2(Velocity.x,Velocity.z).magnitude,Grounded,Boosting);
        void Awake(){body=GetComponent<CharacterController>();}
        public void Warp(Point point)
        {
            if(body==null)body=GetComponent<CharacterController>();
            body.enabled=false;transform.position=new Vector3((float)point.x,(float)point.y+.02f,(float)point.z);body.enabled=true;
            Velocity=Vector3.zero;approach=null;Grounded=true;
            if(CameraOrbit)CameraOrbit.position=transform.position+Vector3.up*1.45f;
        }
        public bool BeginApproach(Destination destination)
        {
            if(destination==null)return false;
            var p=destination.Landing;
            if(new Point(transform.position.x,transform.position.y,transform.position.z).HorizontalDistance(p)>destination.Radius+8||Mathf.Abs(transform.position.y-(float)p.y)>18)return false;
            approach=destination;return true;
        }
        public void Simulate(float dt,FlightInput input,Rules rules,bool blocked)
        {
            if(dt<=0||!CameraOrbit)return;
            Vector2 move=blocked?Vector2.zero:input.Move;
            float lift=blocked?0:input.Lift;
            Boosting=!blocked&&input.Boost&&move.sqrMagnitude>.05f&&rules.State.energy>3;
            if(!blocked&&input.Look.sqrMagnitude>.0001f){yaw+=input.Look.x;pitch=Mathf.Clamp(pitch-input.Look.y,-28,48);lookIdle=0;}else lookIdle+=dt;
            if(lookIdle>2.4f&&move.sqrMagnitude>.02f&&Visual)yaw=Mathf.LerpAngle(yaw,Visual.eulerAngles.y,dt*.8f);
            var forward=Quaternion.Euler(0,yaw,0)*Vector3.forward;var right=Quaternion.Euler(0,yaw,0)*Vector3.right;
            Vector3 desired=(forward*move.y+right*move.x)*(float)rules.CruiseSpeed*(Boosting?1.65f:1);
            desired.y=lift*9;
            if(approach!=null)
            {
                if(move.sqrMagnitude>.12f||lift>0||Boosting)approach=null;
                else
                {
                    Vector3 destination=new Vector3((float)approach.Landing.x,(float)approach.Landing.y,(float)approach.Landing.z);
                    var diff=destination-transform.position;
                    desired=Vector3.ClampMagnitude(new Vector3(diff.x,0,diff.z)*2,6);
                    desired.y=Mathf.Clamp(diff.y*1.8f,-6,4);
                    if(new Vector2(diff.x,diff.z).magnitude<1.5f&&Mathf.Abs(diff.y)<.7f)
                    {
                        desired.y=-1.8f;
                        // Keep descending until the controller actually reaches the floor.
                        // Ending on proximity alone leaves the broom hovering above it.
                        if(Grounded)approach=null;
                    }
                }
            }
            bool floor=Physics.SphereCast(transform.position+Vector3.up*.7f,.25f,Vector3.down,out var hit,.95f,1<<8,QueryTriggerInteraction.Ignore);
            Grounded=floor&&transform.position.y-hit.point.y<.3f&&lift<=0;
            if(Grounded&&lift<=0)desired.y=-1.8f;
            // Hover safely over the sea; water is not a landing surface.
            if(transform.position.z< -76&&transform.position.y<2.0f&&desired.y<0)desired.y=Mathf.Max(0,(2-transform.position.y)*3);
            Vector3 velocity=Vector3.Lerp(Velocity,desired,1-Mathf.Exp(-dt*(move.sqrMagnitude>.05f?4.5f:7)));
            Vector3 before=transform.position;
            CollisionFlags flags=body.Move(velocity*dt);
            Velocity=(transform.position-before)/dt;
            if((flags&CollisionFlags.Below)!=0&&lift<=0)Grounded=true;
            var pos=transform.position;
            pos.x=Mathf.Clamp(pos.x,-165,176);pos.z=Mathf.Clamp(pos.z,-142,181);pos.y=Mathf.Clamp(pos.y,-.2f,88);
            if((pos-transform.position).sqrMagnitude>.0001f){body.enabled=false;transform.position=pos;body.enabled=true;}
            if(Visual)
            {
                var flat=new Vector3(Velocity.x,0,Velocity.z);
                if(flat.sqrMagnitude>.3f)
                {
                    float targetYaw=Mathf.Atan2(flat.x,flat.z)*Mathf.Rad2Deg;
                    Turn=Mathf.Clamp(Mathf.DeltaAngle(Visual.eulerAngles.y,targetYaw)/55,-1,1);
                    float next=Mathf.LerpAngle(Visual.eulerAngles.y,targetYaw,1-Mathf.Exp(-7*dt));
                    Visual.rotation=Quaternion.Euler(0,next,0);
                }
                else Turn=Mathf.Lerp(Turn,0,dt*5);
            }
            CameraOrbit.position=transform.position+Vector3.up*1.45f;
            CameraOrbit.rotation=Quaternion.Euler(pitch,yaw,0);
        }
    }
}
