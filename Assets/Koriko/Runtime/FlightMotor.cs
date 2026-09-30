using Koriko.Core;
using UnityEngine;
using Unity.Cinemachine;

namespace Koriko
{
    [RequireComponent(typeof(CharacterController))]
    public sealed class FlightMotor : MonoBehaviour
    {
        public Transform CameraOrbit;
        public Transform Visual;
        public Vector3 Velocity { get; private set; }
        public bool Grounded { get; private set; }
        public bool Mounted { get; private set; }
        public bool OnFoot => !Mounted;
        public const float WalkSpeed=2.1f;
        public bool Boosting { get; private set; }
        public bool Cruising { get; private set; }
        public bool Approaching => approach != null;
        public float Turn { get; private set; }
        // Animation reads intention before inertia finishes a turn. Input and
        // collision remain authoritative; posing never delays the controls.
        public Vector3 DesiredVelocity { get; private set; }
        public float LiftIntent { get; private set; }
        public int WarpVersion { get; private set; }
        CharacterController body;
        Destination approach;
        float yaw=85,pitch=12,lookIdle;
        public FlightFrame Frame => new FlightFrame(new Point(transform.position.x,transform.position.y,transform.position.z),new Vector2(Velocity.x,Velocity.z).magnitude,Grounded,Boosting);
        void Awake()
        {
            body=GetComponent<CharacterController>();
            Physics.IgnoreLayerCollision(9,10,true); // Detailed shells affect the camera only.
        }
        public void Warp(Point point)
        {
            if(body==null)body=GetComponent<CharacterController>();
            body.enabled=false;transform.position=new Vector3((float)point.x,(float)point.y+.02f,(float)point.z);body.enabled=true;
            Velocity=DesiredVelocity=Vector3.zero;LiftIntent=Turn=0;Boosting=Cruising=false;approach=null;
            Grounded=FloorBelow(out var floor)&&transform.position.y-floor.point.y<.3f;
            Mounted=!Grounded;WarpVersion++;
            if(CameraOrbit)
            {
                Vector3 delta=transform.position+Vector3.up*1.45f-CameraOrbit.position;
                CameraOrbit.position+=delta;CinemachineCore.OnTargetObjectWarped(CameraOrbit,delta);
            }
        }
        /// <summary>Turn Kiki and the camera to a heading, e.g. out of the bakery door after a hospital return.</summary>
        public void Face(float degrees,float cameraPitch=12)
        {
            yaw=degrees;pitch=Mathf.Clamp(cameraPitch,-28,48);lookIdle=0;
            if(Visual)Visual.rotation=Quaternion.Euler(0,degrees,0);
            if(CameraOrbit)CameraOrbit.rotation=Quaternion.Euler(pitch,yaw,0);
        }
        public bool BeginApproach(Destination destination)
        {
            if(destination==null||OnFoot)return false;
            var p=destination.Landing;
            if(new Point(transform.position.x,transform.position.y,transform.position.z).HorizontalDistance(p)>destination.Radius+8||Mathf.Abs(transform.position.y-(float)p.y)>18)return false;
            Cruising=false;approach=destination;return true;
        }
        public void CancelApproach(){approach=null;Cruising=false;}
        public bool LandHere()
        {
            if(OnFoot||!Physics.Raycast(transform.position+Vector3.up*.2f,Vector3.down,out var floor,18.5f,1<<8,QueryTriggerInteraction.Ignore)||floor.normal.y<.7f)return false;
            if(Physics.CheckCapsule(floor.point+Vector3.up*.48f,floor.point+Vector3.up*1.72f,.37f,1<<8,QueryTriggerInteraction.Ignore))return false;
            return BeginApproach(new Destination("street","the street","",new Point(floor.point.x,floor.point.y,floor.point.z)));
        }
        public void Simulate(float dt,FlightInput input,Rules rules,bool blocked)
        {
            if(dt<=0||!CameraOrbit)return;
            Vector2 move=blocked?Vector2.zero:input.Move;
            if(blocked)approach=null;
            float lift=blocked?0:input.Lift;
            LiftIntent=lift;
            bool floor=FloorBelow(out var hit);
            Grounded=floor&&transform.position.y-hit.point.y<.3f&&lift<=0;
            if(lift>0)Mounted=true;
            else if(Grounded&&Velocity.y<.5f)Mounted=false;
            if(blocked||OnFoot||input.Brake||input.DeviceLost||move.y<-.15f)Cruising=false;
            else if(input.CruiseToggle&&!Approaching)Cruising=!Cruising;
            if(Cruising)move=Vector2.ClampMagnitude(new Vector2(move.x,Mathf.Max(.85f,move.y)),1);
            if(!blocked&&input.Brake){move=Vector2.zero;approach=null;}
            Boosting=Mounted&&!blocked&&!input.Brake&&input.Boost&&move.sqrMagnitude>.05f&&rules.State.energy>3;
            if(!blocked&&input.Recenter&&Visual){yaw=Visual.eulerAngles.y;pitch=12;lookIdle=0;}
            if(!blocked&&input.Look.sqrMagnitude>.0001f){yaw+=input.Look.x;pitch=Mathf.Clamp(pitch-input.Look.y,-28,48);lookIdle=0;}else lookIdle+=dt;
            if(lookIdle>2.4f&&move.y>.2f&&Mathf.Abs(move.x)<.4f&&Visual)yaw=Mathf.LerpAngle(yaw,Visual.eulerAngles.y,dt*input.FollowSpeed);
            var forward=Quaternion.Euler(0,yaw,0)*Vector3.forward;var right=Quaternion.Euler(0,yaw,0)*Vector3.right;
            Vector3 desired=(forward*move.y+right*move.x)*(Mounted?(float)rules.CruiseSpeed*(Boosting?1.65f:1):WalkSpeed);
            // On foot the controller follows steps and falls under gravity. It
            // cannot coast at broom speed or hover after walking over an edge.
            desired.y=Mounted?lift*9:Grounded?-2:Mathf.Max(-18,Velocity.y-20*dt);
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
            if(Grounded&&lift<=0)desired.y=-1.8f;
            // Hover safely over the sea; water is not a landing surface.
            if(transform.position.z< -76&&transform.position.y<2.0f&&desired.y<0)
            {
                desired.y=Mathf.Max(0,(2-transform.position.y)*3);
                if(!Grounded)Mounted=true; // Catch a fall from the quay on the broom.
            }
            DesiredVelocity=desired;
            float response=Mounted?(input.Brake||blocked?13:move.sqrMagnitude>.05f?4.5f:7):14;
            Vector3 velocity=Vector3.Lerp(Velocity,desired,1-Mathf.Exp(-dt*response));
            if(OnFoot)velocity.y=desired.y;
            Vector3 before=transform.position;
            CollisionFlags flags=body.Move(velocity*dt);
            Velocity=(transform.position-before)/dt;
            if((flags&CollisionFlags.Below)!=0&&lift<=0){Grounded=true;Mounted=false;Boosting=false;}
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
        bool FloorBelow(out RaycastHit hit)=>Physics.SphereCast(transform.position+Vector3.up*.7f,.25f,Vector3.down,out hit,.95f,1<<8,QueryTriggerInteraction.Ignore);
    }
}
