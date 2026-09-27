using UnityEngine;

namespace Koriko
{
    public sealed class RiderPerformance : MonoBehaviour
    {
        public FlightMotor Motor;
        Transform body,head,bow,jiji,leftLeg,rightLeg,leftArm,rightArm;
        Quaternion bodyRest,headRest,bowRest,jijiRest,leftRest,rightRest,leftArmRest,rightArmRest;
        void Awake()
        {
            foreach(var t in GetComponentsInChildren<Transform>())
            {
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
            // Pose accents are sampled at 12 Hz; world motion and camera remain smooth.
            float pose=Mathf.Floor(Time.time*12)/12;
            float speed=Mathf.Clamp01(new Vector2(Motor.Velocity.x,Motor.Velocity.z).magnitude/24);
            float flight=Motor.Grounded?0:1;
            Pose(body,bodyRest,new Vector3(speed*11+Motor.Velocity.y*.35f,0,-Motor.Turn*15));
            Pose(head,headRest,new Vector3(-speed*7,Motor.Turn*12,Mathf.Sin(pose*1.8f)*1.5f));
            Pose(bow,bowRest,new Vector3(Mathf.Sin(pose*6)*flight*(3+speed*4),0,Mathf.Sin(pose*4)*3));
            Pose(jiji,jijiRest,new Vector3(speed*3,Mathf.Sin(pose*.7f)*12,-Motor.Turn*14));
            Pose(leftLeg,leftRest,new Vector3(-speed*15+Mathf.Sin(pose*3)*flight*3,0,-flight*4));
            Pose(rightLeg,rightRest,new Vector3(-speed*12+Mathf.Sin(pose*3+1)*flight*3,0,flight*4));
            Pose(leftArm,leftArmRest,new Vector3(speed*-4,0,Motor.Turn*2));
            Pose(rightArm,rightArmRest,new Vector3(speed*-4,0,Motor.Turn*2));
        }
    }
}
