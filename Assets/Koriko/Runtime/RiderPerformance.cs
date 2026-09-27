using UnityEngine;
using System.Collections.Generic;

namespace Koriko
{
    public sealed class RiderPerformance : MonoBehaviour
    {
        public FlightMotor Motor;
        Transform body,head,bow,jiji,leftLeg,rightLeg,leftArm,rightArm;
        Quaternion bodyRest,headRest,bowRest,jijiRest,leftRest,rightRest,leftArmRest,rightArmRest;
        readonly Dictionary<Transform,Vector3> eyes=new Dictionary<Transform,Vector3>();
        int lastPose=-1;
        float groundOffset;
        void Awake()
        {
            foreach(var t in GetComponentsInChildren<Transform>())
            {
                if(t.name.StartsWith("Eye white")||t.name.StartsWith("Brown iris")||t.name.StartsWith("Pupil")||t.name.StartsWith("Eye glint")||t.name.StartsWith("Jiji eye")||t.name.StartsWith("Jiji pupil"))eyes.Add(t,t.localScale);
                switch(t.name)
                {
                    case "Body":body=t;bodyRest=t.localRotation;break;
                    case "Head":head=t;headRest=t.localRotation;break;
                    case "Bow":bow=t;bowRest=t.localRotation;break;
                    case "Jiji":jiji=t;jijiRest=t.localRotation;break;
                    case "LeftLeg":leftLeg=t;leftRest=t.localRotation;break;
                    case "RightLeg":rightLeg=t;rightRest=t.localRotation;break;
                    case "LeftArm":leftArm=t;leftArmRest=t.localRotation;break;
                    case "RightArm":rightArm=t;rightArmRest=t.localRotation;break;
                }
            }
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
            groundOffset=Mathf.MoveTowards(groundOffset,Motor.Grounded?-.18f:0,Time.deltaTime*1.2f);
            Motor.Visual.localPosition=Vector3.up*groundOffset;
            // Hold the complete pose on twos. Input, translation, turning and camera stay smooth.
            int frame=Mathf.FloorToInt(Time.time*12);if(frame==lastPose)return;lastPose=frame;
            float pose=frame/12f;
            float speed=Mathf.Clamp01(new Vector2(Motor.Velocity.x,Motor.Velocity.z).magnitude/24);
            float flight=Motor.Grounded?0:1;
            Pose(body,bodyRest,new Vector3(flight*3+speed*18+Motor.Velocity.y*.35f,0,-Motor.Turn*18));
            Pose(head,headRest,new Vector3(-speed*12,Motor.Turn*16,Mathf.Sin(pose*1.8f)*1.5f));
            Pose(bow,bowRest,new Vector3(Mathf.Sin(pose*6)*flight*(3+speed*7),0,Mathf.Sin(pose*4)*4));
            Pose(jiji,jijiRest,new Vector3(speed*3,Mathf.Sin(pose*.7f)*12,-Motor.Turn*14));
            Pose(leftLeg,leftRest,new Vector3(-speed*15+Mathf.Sin(pose*3)*flight*3,0,-flight*4));
            Pose(rightLeg,rightRest,new Vector3(-speed*12+Mathf.Sin(pose*3+1)*flight*3,0,flight*4));
            Pose(leftArm,leftArmRest,new Vector3(speed*-11,0,Motor.Turn*2));
            Pose(rightArm,rightArmRest,new Vector3(speed*-11,0,Motor.Turn*2));
            foreach(var eye in eyes)
            {
                float offset=eye.Key.name.StartsWith("Jiji")?2.4f:1.3f;
                float blink=Mathf.Repeat(pose+offset,5.2f)>5.05f?.08f:1;
                Vector3 up=eye.Key.InverseTransformDirection(Motor.Visual.up),scale=eye.Value;
                int axis=Mathf.Abs(up.x)>Mathf.Abs(up.y)?0:1;if(Mathf.Abs(up.z)>Mathf.Abs(up[axis]))axis=2;
                scale[axis]*=blink;eye.Key.localScale=scale;
            }
        }
    }
}
