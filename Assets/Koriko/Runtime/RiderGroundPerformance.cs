using UnityEngine;

namespace Koriko
{
    // Ground acting shares the flight rig. Foot plants live in world space;
    // neither the gait clock nor the broom prop drives the collision controller.
    public sealed partial class RiderPerformance
    {
        sealed class WalkingLeg
        {
            public Transform Hip,Knee,Foot;
            public Renderer Sole;
            public Quaternion HipRest,KneeRest,FootFrame,PlantRotation,TargetRotation;
            public float Upper,Lower,SoleOffset,Side,SettleAge;
            public Vector3 Plant,SwingStart,Target,SettleEnd,PreviousFoot;
            public bool Swing,Settling,Measured;
        }
        WalkingLeg[] walkingLegs;
        Transform carriedBroom,carryGrip,broomTip,cat;
        Vector3 catBroomPoint,catShoulderPoint,freeHandPoint,lastGroundPosition;
        Quaternion catBroomFrame,catShoulderFrame,leftFreeHandFrame;
        float walkPhase=.35f,walkWeight,stepSway;
        bool feetReady,wasWalking,freeHandReady;
        Vector3 freeHandLocal;Quaternion freeHandLocalRotation;
        public float WalkingWeight=>walkWeight;
        public float WalkingPhase=>walkPhase;
        public float BroomTipClearance {get;private set;}
        public float BroomUpright=>carriedBroom?Mathf.Abs(Vector3.Dot((broomTip.position-carriedBroom.position).normalized,Vector3.up)):0;
        public float PlantedFootSlide {get;private set;}
        public bool HasWalkingRig=>carryGrip&&broomTip&&shapes.ContainsKey("Walk left")&&shapes.ContainsKey("Left hand relaxed");
        // Planted steps, for footstep sound. Settling shuffles are marked soft.
        public int Footfalls {get;private set;}
        public Vector3 LastFootfall {get;private set;}
        public bool LastFootfallSoft {get;private set;}
        public Transform BroomNode=>carriedBroom;
        public Transform BroomTip=>broomTip;
        void Footfall(Vector3 point,bool soft){Footfalls++;LastFootfall=point;LastFootfallSoft=soft;}

        void InitializeGroundPerformance()
        {
            carriedBroom=Node("Broom");carryGrip=Node("CarryGrip");broomTip=Node("BroomGroundTip");cat=Node("Jiji");
            walkingLegs=new WalkingLeg[2];
            for(int i=0;i<2;i++)
            {
                string side=i==0?"Left":"Right";
                var leg=new WalkingLeg{Hip=Node(side+"Leg"),Knee=Node(side+"Knee"),Foot=Node(side+"Foot"),Sole=Node(side+" sole")?.GetComponent<Renderer>(),Side=i==0?-1:1};
                if(!leg.Hip||!leg.Knee||!leg.Foot||!leg.Sole)continue;
                leg.HipRest=leg.Hip.localRotation;leg.KneeRest=leg.Knee.localRotation;
                leg.FootFrame=Quaternion.Inverse(Motor.Visual.rotation)*leg.Foot.rotation;
                leg.Upper=Vector3.Distance(leg.Hip.position,leg.Knee.position);leg.Lower=Vector3.Distance(leg.Knee.position,leg.Foot.position);
                leg.SoleOffset=leg.Foot.position.y-leg.Sole.bounds.min.y;walkingLegs[i]=leg;
            }
            if(cat&&carriedBroom)
            {
                catBroomPoint=carriedBroom.InverseTransformPoint(cat.position);catBroomFrame=Quaternion.Inverse(carriedBroom.rotation)*cat.rotation;
                catShoulderPoint=Node("Body").InverseTransformPoint(Motor.Visual.TransformPoint(new Vector3(-.29f,1.61f,-.06f)));
                catShoulderFrame=Quaternion.Inverse(Node("Body").rotation)*cat.rotation;
                // An upright carried broom must not turn Jiji upside down.
                cat.SetParent(Motor.Visual,true);
            }
            // The free hand rests on the front of the satchel, as Kiki's does in the film.
            freeHandPoint=Node("Body").InverseTransformPoint(Motor.Visual.TransformPoint(new Vector3(-.41f,1.00f,.115f)));
            var left=Node("LeftHand");
            if(left)leftFreeHandFrame=Quaternion.Inverse(Motor.Visual.rotation)*Quaternion.FromToRotation(Motor.Visual.TransformDirection(new Vector3(.65f,-.64f,.25f)),Vector3.down)*left.rotation;
        }
        void ResetGroundPerformance()
        {
            feetReady=wasWalking=freeHandReady=false;walkWeight=0;walkPhase=.35f;lastGroundPosition=Motor.transform.position;
            PlantedFootSlide=0;BroomTipClearance=0;
        }
        void AdvanceWalk(float dt,float actualSpeed)
        {
            Vector3 travel=Motor.transform.position-lastGroundPosition;travel.y=0;lastGroundPosition=Motor.transform.position;
            bool moving=Motor.OnFoot&&Motor.Grounded&&actualSpeed>.065f;
            walkWeight=Mathf.MoveTowards(walkWeight,moving?Mathf.Clamp01(actualSpeed/FlightMotor.WalkSpeed):0,dt*5);
            if(moving)walkPhase=Mathf.Repeat(walkPhase+Mathf.Min(travel.magnitude,.25f)/Mathf.Lerp(.90f,1.65f,walkWeight)+Mathf.Abs(Motor.Turn)*dt*.5f,1);
            stepSway=Mathf.Sin(walkPhase*Mathf.PI*2)*walkWeight;
        }
        void PositionBroom(float flight,float hover,float angle,float bodyBank)
        {
            if(!carriedBroom||!broomTip)return;
            float mount=Ease(flight);
            float carry=1-mount;
            Pose("Broom",new Vector3(Mathf.Lerp(-87+stepSway*2,angle,mount),carry*3,Mathf.Lerp(-5+stepSway*1.5f,bodyBank*.28f,mount)));
            // First sweep the broom beside the hip, then bring the seat under
            // Kiki. The arc avoids pulling a vertical shaft through her torso.
            // Held close at her side, so the carrying elbow stays bent rather than locked out.
            float side=.46f*(1-Ease((flight-.34f)/.66f));
            Offset("Broom",new Vector3(side,carry*.72f+hover*.6f,carry*.23f+Mathf.Sin(mount*Mathf.PI)*.15f));
            Restore("Broom");
            BroomTipClearance=float.PositiveInfinity;
            if(Motor.Grounded&&flight<.025f&&Physics.Raycast(broomTip.position+Vector3.up*.7f,Vector3.down,out var floor,1.8f,1<<8,QueryTriggerInteraction.Ignore))
            {
                float lift=.014f+walkWeight*(.065f+.018f*(1+Mathf.Cos(walkPhase*Mathf.PI*4)));
                carriedBroom.position+=Vector3.up*(floor.point.y+lift-broomTip.position.y);
                BroomTipClearance=broomTip.position.y-floor.point.y;
            }
        }
        void GroundArms(float flight)
        {
            GripError=0;
            if(arms==null||arms.Length<2||!carryGrip)return;
            float rightGrab=Ease((flight-.18f)/.70f),leftGrab=Ease((flight-.45f)/.44f);
            if(arms[1]!=null)
            {
                Vector3 target=Vector3.Lerp(carryGrip.position,arms[1].Grip.position,rightGrab);
                Quaternion rotation=Quaternion.Slerp(carryGrip.rotation,arms[1].Grip.rotation,rightGrab)*arms[1].WristFrame;
                SolveArm(arms[1],target,rotation,true);
            }
            if(arms[0]!=null)
            {
                var body=Node("Body");
                // Two beats after the bow: the free hand reaches out to hand the parcel over,
                // then rises beside the face and waves goodbye.
                float give=Accent(Mathf.Max(0,deliverAge-.08f),.18f,.86f)*(1-flight);
                float wave=Accent(Mathf.Max(0,deliverAge-.78f),.22f,1.10f)*(1-flight);
                float waveSwing=Mathf.Sin(Mathf.Max(0,deliverAge-.86f)*11f)*wave;
                Vector3 reach=new Vector3(.16f*give-.10f*wave+waveSwing*.09f,.40f*give+.86f*wave,.44f*give+.10f*wave);
                // The free (left) arm opposes the left leg: back when the left heel strikes
                // (phase 0), forward when the right heel strikes (phase .5). More forward than
                // back, with the elbow kept soft, so it never locks straight behind her.
                float opposed=-Mathf.Cos(walkPhase*Mathf.PI*2)*walkWeight;
                float swing=opposed>0?opposed*.13f:opposed*.07f;
                Vector3 walking=new Vector3(.03f*walkWeight,.035f*walkWeight+.03f*Mathf.Max(0,opposed)-.012f*Mathf.Abs(opposed),swing);
                if(drawing||!freeHandReady)
                {
                    Vector3 free=body.TransformPoint(freeHandPoint)+Motor.Visual.TransformDirection(walking+reach);
                    Quaternion freeRotation=Motor.Visual.rotation*Quaternion.Euler(opposed*6-give*35-wave*40,0,-3-waveSwing*20)*leftFreeHandFrame;
                    freeHandLocal=body.InverseTransformPoint(free);freeHandLocalRotation=Quaternion.Inverse(body.rotation)*freeRotation;freeHandReady=true;
                }
                Vector3 drawnFree=body.TransformPoint(freeHandLocal);Quaternion drawnRotation=body.rotation*freeHandLocalRotation;
                SolveArm(arms[0],Vector3.Lerp(drawnFree,arms[0].Grip.position,leftGrab),Quaternion.Slerp(drawnRotation,arms[0].Grip.rotation*arms[0].WristFrame,leftGrab),leftGrab>.99f);
            }
            Shape("Left hand relaxed",1-leftGrab);
        }
        void PositionJiji(float flight,float lean,float roll)
        {
            if(!cat||!carriedBroom)return;
            var body=Node("Body");float hop=Ease(flight);
            cat.position=Vector3.Lerp(body.TransformPoint(catShoulderPoint),carriedBroom.TransformPoint(catBroomPoint),hop)+Vector3.up*(Mathf.Sin(hop*Mathf.PI)*.23f);
            Quaternion frame=Quaternion.Slerp(body.rotation*catShoulderFrame,carriedBroom.rotation*catBroomFrame,hop);
            // On the shoulder Jiji turns a little outward, so his ears and profile read from behind.
            cat.rotation=Motor.Visual.rotation*Quaternion.Euler(lean,-22*(1-hop),roll)*Quaternion.Inverse(Motor.Visual.rotation)*frame;
        }
        Vector3 FootSupport(Vector3 point,WalkingLeg leg)
        {
            if(Physics.Raycast(point+Vector3.up*.8f,Vector3.down,out var hit,1.8f,1<<8,QueryTriggerInteraction.Ignore))point.y=hit.point.y+leg.SoleOffset+.006f;
            return point;
        }
        Vector3 RestFoot(WalkingLeg leg,float ahead=0)
        {
            Vector3 heading=Motor.Velocity;heading.y=0;
            if(heading.sqrMagnitude<.01f)heading=Motor.Visual.forward;else heading.Normalize();
            return FootSupport(Motor.Visual.TransformPoint(new Vector3(leg.Side*.192f,.139f,.025f))+heading*ahead,leg);
        }
        bool PlaceWalkingFeet(float dt)
        {
            if(!Motor.Grounded||flightPose>.025f||walkingLegs==null)return false;
            bool moving=Motor.Velocity.sqrMagnitude>.025f&&new Vector2(Motor.Velocity.x,Motor.Velocity.z).magnitude>.065f;
            if(!feetReady)
            {
                foreach(var leg in walkingLegs)if(leg!=null){leg.Plant=leg.Target=RestFoot(leg);leg.PlantRotation=Motor.Visual.rotation;leg.Swing=leg.Settling=leg.Measured=false;}
                feetReady=true;
            }
            GroundContactError=0;PlantedFootSlide=0;
            if(Physics.Raycast(Motor.transform.position+Vector3.up*.75f,Vector3.down,out var floor,1.4f,1<<8,QueryTriggerInteraction.Ignore))GroundFloorHeight=floor.point.y;
            foreach(var leg in walkingLegs)
            {
                if(leg==null)continue;
                float phase=Mathf.Repeat(walkPhase+(leg.Side>0?.5f:0),1);
                bool swing=moving&&phase>=.55f;
                if(moving)
                {
                    leg.Settling=false;
                    if(swing)
                    {
                        if(!leg.Swing)leg.SwingStart=leg.Target;
                        float t=(phase-.55f)/.45f;
                        // The swing foot is drawn on the drawing clock; the planted foot stays exact every frame.
                        if(drawing||!leg.Swing)leg.Target=Vector3.Lerp(leg.SwingStart,RestFoot(leg,Mathf.Lerp(.24f,.44f,walkWeight)),Ease(t))+Vector3.up*(Mathf.Sin(t*Mathf.PI)*.145f);
                    }
                    else
                    {
                        // The foot always lands at the full step, whatever drawing the swing was holding;
                        // a short plant left the stance leg overreached as the body walked on.
                        if(leg.Swing){leg.Plant=FootSupport(RestFoot(leg,Mathf.Lerp(.24f,.44f,walkWeight)),leg);leg.PlantRotation=Motor.Visual.rotation;Footfall(leg.Plant,false);}
                        leg.Target=leg.Plant;
                    }
                }
                else
                {
                    if(wasWalking)
                    {
                        leg.SwingStart=leg.Target;leg.SettleEnd=RestFoot(leg);leg.SettleAge=leg.Side<0?0:-.10f;
                        leg.Settling=Vector3.Distance(leg.Target,leg.SettleEnd)>.06f;
                    }
                    if(leg.Settling)
                    {
                        leg.SettleAge+=dt;float t=Mathf.Clamp01(leg.SettleAge/.28f);
                        if(drawing||t>=1)leg.Target=Vector3.Lerp(leg.SwingStart,leg.SettleEnd,Ease(t))+Vector3.up*(Mathf.Sin(t*Mathf.PI)*.075f);
                        if(t>=1){leg.Plant=leg.Target;leg.PlantRotation=Motor.Visual.rotation;leg.Settling=false;Footfall(leg.Plant,true);}
                    }
                    else leg.Target=leg.Plant;
                }
                leg.Swing=swing;
                leg.TargetRotation=Quaternion.Slerp(leg.PlantRotation,Motor.Visual.rotation,swing?Ease((phase-.55f)/.45f):leg.Settling?Ease(leg.SettleAge/.28f):0)*leg.FootFrame;
            }
            // Pelvis support is solved before either leg. Clamping an
            // unreachable ankle alone makes a planted shoe skate above the road.
            float lower=0;
            foreach(var leg in walkingLegs)if(leg!=null)
            {
                Vector3 delta=leg.Hip.position-leg.Target;
                float reach=(leg.Upper+leg.Lower)*.98f;
                float height=Mathf.Sqrt(Mathf.Max(.04f,reach*reach-delta.x*delta.x-delta.z*delta.z));
                lower=Mathf.Max(lower,delta.y-height);
            }
            Restore("Body");Node("Body").position-=Vector3.up*Mathf.Clamp(lower,0,.32f);
            foreach(var leg in walkingLegs)
            {
                if(leg==null)continue;
                SolveLeg(leg,leg.Target,leg.TargetRotation);
                if(!leg.Swing&&!leg.Settling)
                {
                    float floorY=leg.Target.y-leg.SoleOffset-.006f;
                    GroundContactError=Mathf.Max(GroundContactError,Mathf.Abs(leg.Sole.bounds.min.y-floorY-.006f));
                    if(leg.Measured)PlantedFootSlide=Mathf.Max(PlantedFootSlide,Vector3.Distance(leg.Foot.position,leg.PreviousFoot));
                }
                leg.Measured=!leg.Swing&&!leg.Settling;leg.PreviousFoot=leg.Foot.position;
            }
            wasWalking=moving;return true;
        }
        void SolveLeg(WalkingLeg leg,Vector3 target,Quaternion rotation)
        {
            leg.Hip.localRotation=leg.HipRest;leg.Knee.localRotation=leg.KneeRest;
            Vector3 start=leg.Hip.position,delta=target-start;
            float distance=Mathf.Clamp(delta.magnitude,.001f,leg.Upper+leg.Lower-.001f);
            Vector3 direction=delta.normalized;
            Vector3 bend=Vector3.ProjectOnPlane(Motor.Visual.forward+Motor.Visual.right*leg.Side*.08f,direction).normalized;
            float along=(leg.Upper*leg.Upper-leg.Lower*leg.Lower+distance*distance)/(2*distance);
            Vector3 knee=start+direction*along+bend*Mathf.Sqrt(Mathf.Max(0,leg.Upper*leg.Upper-along*along));
            leg.Hip.rotation=Quaternion.FromToRotation(leg.Knee.position-start,knee-start)*leg.Hip.rotation;
            leg.Knee.rotation=Quaternion.FromToRotation(leg.Foot.position-leg.Knee.position,target-leg.Knee.position)*leg.Knee.rotation;
            leg.Foot.rotation=rotation;
        }
    }
}
