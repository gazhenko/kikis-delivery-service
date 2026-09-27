using UnityEngine;
using System.Collections.Generic;

namespace Koriko
{
    public sealed class RiderPerformance : MonoBehaviour
    {
        public FlightMotor Motor;
        Transform body,head,bow,jiji,leftLeg,rightLeg,leftKnee,rightKnee,leftFoot,rightFoot;
        Quaternion bodyRest,headRest,bowRest,jijiRest,leftRest,rightRest,leftKneeRest,rightKneeRest,leftFootRest,rightFootRest;
        readonly Dictionary<Transform,Vector3> eyes=new Dictionary<Transform,Vector3>();
        readonly List<(SkinnedMeshRenderer renderer,int shape)> cloth=new List<(SkinnedMeshRenderer,int)>();
        readonly List<Renderer> soles=new List<Renderer>();
        sealed class Arm
        {
            public Transform Shoulder,Elbow,Wrist,Grip;
            public Quaternion ShoulderRest,ElbowRest,WristFrame;
            public float UpperLength,LowerLength,Side;
        }
        Arm[] arms;
        int lastPose=-1;
        float groundOffset,flightPose;
        public float GripError { get; private set; }
        public float GroundContactError { get; private set; }=float.PositiveInfinity;
        public float GroundFloorHeight { get; private set; }=float.NegativeInfinity;
        public bool HasFlightCloth => cloth.Count>0;
        void Awake()
        {
            foreach(var t in GetComponentsInChildren<Transform>())
            {
                if(t.name.EndsWith("EyePivot")||t.name.StartsWith("Jiji eye")||t.name.StartsWith("Jiji pupil"))eyes.Add(t,t.localScale);
                if((t.name=="Left sole"||t.name=="Right sole")&&t.TryGetComponent<Renderer>(out var sole))soles.Add(sole);
                switch(t.name)
                {
                    case "Body":body=t;bodyRest=t.localRotation;break;
                    case "Head":head=t;headRest=t.localRotation;break;
                    case "Bow":bow=t;bowRest=t.localRotation;break;
                    case "Jiji":jiji=t;jijiRest=t.localRotation;break;
                    case "LeftLeg":leftLeg=t;leftRest=t.localRotation;break;
                    case "RightLeg":rightLeg=t;rightRest=t.localRotation;break;
                    case "LeftKnee":leftKnee=t;leftKneeRest=t.localRotation;break;
                    case "RightKnee":rightKnee=t;rightKneeRest=t.localRotation;break;
                    case "LeftFoot":leftFoot=t;leftFootRest=t.localRotation;break;
                    case "RightFoot":rightFoot=t;rightFootRest=t.localRotation;break;
                }
            }
            Transform Find(string name){foreach(var t in GetComponentsInChildren<Transform>())if(t.name==name)return t;return null;}
            arms=new Arm[2];
            for(int i=0;i<2;i++)
            {
                string side=i==0?"Left":"Right";
                var arm=new Arm{Shoulder=Find(side+"Arm"),Elbow=Find(side+"Forearm"),Wrist=Find(side+"Hand"),Grip=Find(side+"Grip"),Side=i==0?-1:1};
                if(!arm.Shoulder||!arm.Elbow||!arm.Wrist||!arm.Grip)continue;
                arm.ShoulderRest=arm.Shoulder.localRotation;arm.ElbowRest=arm.Elbow.localRotation;
                arm.UpperLength=Vector3.Distance(arm.Shoulder.position,arm.Elbow.position);arm.LowerLength=Vector3.Distance(arm.Elbow.position,arm.Wrist.position);
                arm.WristFrame=Quaternion.Inverse(Motor.Visual.rotation)*arm.Wrist.rotation;
                arms[i]=arm;
            }
            foreach(var renderer in GetComponentsInChildren<SkinnedMeshRenderer>())
                for(int i=0;i<renderer.sharedMesh.blendShapeCount;i++)
                    if(renderer.sharedMesh.GetBlendShapeName(i).EndsWith("Flight cloth"))cloth.Add((renderer,i));
        }
        void Pose(Transform part,Quaternion rest,Vector3 angles)
        {
            if(!part)return;
            // Imported FBX pivots may use Blender's local axes. Express every pose
            // in the flight visual's forward/up frame before applying it to that pivot.
            Quaternion basis=Quaternion.Inverse(part.parent.rotation)*Motor.Visual.rotation;
            part.localRotation=basis*Quaternion.Euler(angles)*Quaternion.Inverse(basis)*rest;
        }
        void LateUpdate()
        {
            if(!Motor)return;
            flightPose=Mathf.MoveTowards(flightPose,Motor.Grounded?0:1,Time.deltaTime*3.5f);
            // Hold the complete pose on twos. Input, translation, turning and camera stay smooth.
            int frame=Mathf.FloorToInt(Time.time*12);if(frame==lastPose){PlaceFeet();return;}lastPose=frame;
            float pose=frame/12f;
            float speed=Mathf.Clamp01(new Vector2(Motor.Velocity.x,Motor.Velocity.z).magnitude/24);
            float flight=flightPose;
            Pose(body,bodyRest,new Vector3(flight*7+speed*16+Motor.Velocity.y*.22f,0,-Motor.Turn*14));
            Pose(head,headRest,new Vector3(-flight*4-speed*13,Motor.Turn*12,Mathf.Sin(pose*1.8f)*1.1f));
            Pose(bow,bowRest,new Vector3(Mathf.Sin(pose*6)*flight*(3+speed*7),0,Mathf.Sin(pose*4)*4));
            Pose(jiji,jijiRest,new Vector3(speed*3,Mathf.Sin(pose*.7f)*12,-Motor.Turn*14));
            Pose(leftLeg,leftRest,new Vector3(-flight*(68+speed*7),0,-flight*4));
            Pose(rightLeg,rightRest,new Vector3(-flight*(65+speed*7),0,flight*4));
            Pose(leftKnee,leftKneeRest,new Vector3(flight*(99+Mathf.Sin(pose*2.8f)*2),0,0));
            Pose(rightKnee,rightKneeRest,new Vector3(flight*(97+Mathf.Sin(pose*2.8f+1)*2),0,0));
            Pose(leftFoot,leftFootRest,new Vector3(-flight*18,0,0));
            Pose(rightFoot,rightFootRest,new Vector3(-flight*16,0,0));
            foreach(var item in cloth)item.renderer.SetBlendShapeWeight(item.shape,flight*100);
            GripError=0;
            foreach(var arm in arms)if(arm!=null)SolveArm(arm);
            foreach(var eye in eyes)
            {
                float offset=eye.Key.name.StartsWith("Jiji")?2.4f:1.3f;
                float blink=Mathf.Repeat(pose+offset,5.2f)>5.05f?.08f:1;
                Vector3 up=eye.Key.InverseTransformDirection(Motor.Visual.up),scale=eye.Value;
                int axis=Mathf.Abs(up.x)>Mathf.Abs(up.y)?0:1;if(Mathf.Abs(up.z)>Mathf.Abs(up[axis]))axis=2;
                scale[axis]*=blink;eye.Key.localScale=scale;
            }
            PlaceFeet();
        }
        void PlaceFeet()
        {
            // The controller's skin can settle below its nominal floor origin.
            // Fit the visible soles to the actual surface without moving collision
            // or changing the delivery position. This also handles a fresh Warp.
            float target=0;
            GroundContactError=float.PositiveInfinity;
            GroundFloorHeight=float.NegativeInfinity;
            if(Motor.Grounded&&soles.Count==2&&Physics.Raycast(Motor.transform.position+Vector3.up*.75f,Vector3.down,out var floor,1.4f,1<<8,QueryTriggerInteraction.Ignore))
            {
                GroundFloorHeight=floor.point.y;
                float lowest=Mathf.Min(soles[0].bounds.min.y,soles[1].bounds.min.y);
                target=Mathf.Clamp(groundOffset+floor.point.y+.006f-lowest,-.35f,.35f);
                float next=Mathf.MoveTowards(groundOffset,target,Time.deltaTime*2);
                GroundContactError=Mathf.Abs(lowest+next-groundOffset-floor.point.y-.006f);
                groundOffset=next;
            }
            else groundOffset=Mathf.MoveTowards(groundOffset,target,Time.deltaTime*2);
            Motor.Visual.localPosition=Vector3.up*groundOffset;
        }
        void SolveArm(Arm arm)
        {
            arm.Shoulder.localRotation=arm.ShoulderRest;arm.Elbow.localRotation=arm.ElbowRest;
            Vector3 start=arm.Shoulder.position,target=arm.Grip.position,delta=target-start;
            float distance=Mathf.Clamp(delta.magnitude,.001f,arm.UpperLength+arm.LowerLength-.0001f);
            Vector3 direction=delta.normalized;
            Vector3 pole=Motor.Visual.TransformDirection(new Vector3(arm.Side,-.15f,-.25f));
            Vector3 bend=Vector3.ProjectOnPlane(pole,direction).normalized;
            float along=(arm.UpperLength*arm.UpperLength-arm.LowerLength*arm.LowerLength+distance*distance)/(2*distance);
            float away=Mathf.Sqrt(Mathf.Max(0,arm.UpperLength*arm.UpperLength-along*along));
            Vector3 elbow=start+direction*along+bend*away;
            arm.Shoulder.rotation=Quaternion.FromToRotation(arm.Elbow.position-start,elbow-start)*arm.Shoulder.rotation;
            arm.Elbow.rotation=Quaternion.FromToRotation(arm.Wrist.position-arm.Elbow.position,target-arm.Elbow.position)*arm.Elbow.rotation;
            arm.Wrist.rotation=Motor.Visual.rotation*arm.WristFrame;
            GripError=Mathf.Max(GripError,Vector3.Distance(arm.Wrist.position,target));
        }
    }
}
