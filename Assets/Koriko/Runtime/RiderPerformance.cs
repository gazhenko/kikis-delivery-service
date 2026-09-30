using UnityEngine;
using System.Collections.Generic;

namespace Koriko
{
    /// <summary>Flight performance: authored action accents, overlapping springs and
    /// short deformation exposures. Never moves collision or changes control timing.</summary>
    [DefaultExecutionOrder(40)]
    public sealed partial class RiderPerformance : MonoBehaviour
    {
        public FlightMotor Motor;
        sealed class Part
        {
            public Transform Node;
            public Quaternion Rest;
            public Vector3 Position;
            public Part(Transform node){Node=node;if(node){Rest=node.localRotation;Position=node.localPosition;}}
        }
        sealed class Spring
        {
            public float Value,Velocity;
            public float Step(float target,float frequency,float damping,float dt)
            {
                // Bounded integration keeps secondary motion stable at low frame rates.
                int count=Mathf.Max(1,Mathf.CeilToInt(dt*120));float h=dt/count,w=frequency*2*Mathf.PI;
                for(int i=0;i<count;i++){Velocity+=(w*w*(target-Value)-2*damping*w*Velocity)*h;Value+=Velocity*h;}
                return Value;
            }
            public void Reset(float value=0){Value=value;Velocity=0;}
        }
        sealed class Arm
        {
            public Transform Shoulder,Elbow,Wrist,Grip;
            public Quaternion ShoulderRest,ElbowRest,WristFrame;
            public float UpperLength,LowerLength,Side;
        }
        readonly Dictionary<string,Part> parts=new Dictionary<string,Part>();
        readonly Dictionary<Transform,Vector3> eyes=new Dictionary<Transform,Vector3>();
        readonly Dictionary<string,List<(SkinnedMeshRenderer renderer,int shape)>> shapes=new Dictionary<string,List<(SkinnedMeshRenderer,int)>>();
        readonly List<Renderer> soles=new List<Renderer>();
        readonly Spring pitch=new Spring(),bank=new Spring(),gaze=new Spring(),headPitch=new Spring();
        readonly Spring ribbon=new Spring(),ribbonLeft=new Spring(),ribbonRight=new Spring(),hem=new Spring();
        readonly Spring legDrag=new Spring(),bagSway=new Spring(),tail=new Spring(),catHead=new Spring(),broomPitch=new Spring();
        Arm[] arms;
        Renderer quietMouth,breathMouth;
        Vector3 previousVelocity,smearDirection;
        float clock,groundOffset,flightPose,speed,acceleration,previousSpeed;
        float takeoffAge=10,landingAge=10,boostAge=10,brakeAge=10,airTime,idleTime,blinkAge=10,nextBlink=2.3f;
        float smearAge=10,smearTurn,lastTurn,smearCooldown,landingImpact,deliverAge=10;
        bool wasGrounded=true,wasBoosting,initialized;
        int warpVersion;
        static readonly int WorldToPose=Shader.PropertyToID("_KorikoRiderWorldToPose"),PoseToWorld=Shader.PropertyToID("_KorikoRiderPoseToWorld"),Smear=Shader.PropertyToID("_KorikoRiderSmear");
        static readonly int LeftHip=Shader.PropertyToID("_KorikoRiderLeftHip"),LeftKnee=Shader.PropertyToID("_KorikoRiderLeftKnee"),RightHip=Shader.PropertyToID("_KorikoRiderRightHip"),RightKnee=Shader.PropertyToID("_KorikoRiderRightKnee");
        public float GripError {get;private set;}
        public float GroundContactError {get;private set;}=float.PositiveInfinity;
        public float GroundFloorHeight {get;private set;}=float.NegativeInfinity;
        public bool HasFlightCloth=>shapes.ContainsKey("Flight cloth");
        public bool HasSecondaryShapes=>shapes.ContainsKey("Hair stream")&&shapes.ContainsKey("Hem left")&&shapes.ContainsKey("Hem right");
        public float SmearWeight {get;private set;}
        public float BodyBank=>bank.Value;
        public float BodyPitch=>pitch.Value;
        public float SecondarySwing=>ribbonLeft.Value;
        public float Gaze=>gaze.Value;
        public float FlightPose=>flightPose;
        public int Takeoffs {get;private set;}
        public int Landings {get;private set;}
        public int SmearCount {get;private set;}
        public string Beat {get;private set;}="Rest";
        /// <summary>Hand the parcel over: a small bow, then a wave with the free hand.</summary>
        public void Deliver(){deliverAge=0;}
        public bool Delivering=>deliverAge<1.9f;

        void Awake()
        {
            foreach(var t in GetComponentsInChildren<Transform>())
            {
                if(!parts.ContainsKey(t.name))parts.Add(t.name,new Part(t));
                if(t.name.EndsWith("EyePivot")||t.name.StartsWith("Jiji eye")||t.name.StartsWith("Jiji pupil"))eyes.Add(t,t.localScale);
                if((t.name=="Left sole"||t.name=="Right sole")&&t.TryGetComponent<Renderer>(out var sole))soles.Add(sole);
            }
            quietMouth=Node("Quiet smile")?.GetComponent<Renderer>();breathMouth=Node("Breath mouth")?.GetComponent<Renderer>();
            arms=new Arm[2];
            for(int i=0;i<2;i++)
            {
                string side=i==0?"Left":"Right";
                var arm=new Arm{Shoulder=Node(side+"Arm"),Elbow=Node(side+"Forearm"),Wrist=Node(side+"Hand"),Grip=Node(side+"Grip"),Side=i==0?-1:1};
                if(!arm.Shoulder||!arm.Elbow||!arm.Wrist||!arm.Grip)continue;
                arm.ShoulderRest=arm.Shoulder.localRotation;arm.ElbowRest=arm.Elbow.localRotation;
                arm.UpperLength=Vector3.Distance(arm.Shoulder.position,arm.Elbow.position);arm.LowerLength=Vector3.Distance(arm.Elbow.position,arm.Wrist.position);
                arm.WristFrame=Quaternion.Inverse(arm.Grip.rotation)*arm.Wrist.rotation;arms[i]=arm;
            }
            foreach(var renderer in GetComponentsInChildren<SkinnedMeshRenderer>())
            {
                // Morph and brief smear extremes must not disappear at the frustum edge.
                var bounds=renderer.localBounds;bounds.Expand(.9f);renderer.localBounds=bounds;
                for(int i=0;i<renderer.sharedMesh.blendShapeCount;i++)
                {
                    string name=renderer.sharedMesh.GetBlendShapeName(i);int dot=name.LastIndexOf('.');if(dot>=0)name=name.Substring(dot+1);
                    if(!shapes.ContainsKey(name))shapes.Add(name,new List<(SkinnedMeshRenderer,int)>());
                    shapes[name].Add((renderer,i));
                }
            }
            InitializeGroundPerformance();
        }
        Transform Node(string name)=>parts.TryGetValue(name,out var p)?p.Node:null;
        void Pose(string name,Vector3 angles)
        {
            if(!parts.TryGetValue(name,out var part)||!part.Node)return;
            // FBX local axes differ from the game's right/up/forward convention.
            Quaternion basis=Quaternion.Inverse(part.Node.parent.rotation)*Motor.Visual.rotation;
            part.Node.localRotation=basis*Quaternion.Euler(angles)*Quaternion.Inverse(basis)*part.Rest;
        }
        void Offset(string name,Vector3 offset)
        {
            if(parts.TryGetValue(name,out var part)&&part.Node)
                part.Node.localPosition=part.Position+part.Node.parent.InverseTransformVector(Motor.Visual.TransformDirection(offset));
        }
        void Shape(string name,float weight)
        {
            if(shapes.TryGetValue(name,out var entries))foreach(var item in entries)item.renderer.SetBlendShapeWeight(item.shape,Mathf.Clamp01(weight)*100);
        }
        static float Ease(float value)=>Mathf.SmoothStep(0,1,Mathf.Clamp01(value));
        static float Accent(float age,float attack,float end)=>age<attack?Ease(age/attack):1-Ease((age-attack)/(end-attack));
        void ResetMotion()
        {
            warpVersion=Motor.WarpVersion;wasGrounded=Motor.Grounded;wasBoosting=Motor.Boosting;
            previousVelocity=Motor.Velocity;previousSpeed=speed=0;acceleration=groundOffset=0;
            takeoffAge=landingAge=boostAge=brakeAge=smearAge=10;airTime=idleTime=0;
            flightPose=Motor.Mounted?1:0;smearCooldown=0;lastTurn=0;
            foreach(var s in new[]{pitch,bank,gaze,headPitch,ribbon,ribbonLeft,ribbonRight,hem,legDrag,bagSway,tail,catHead,broomPitch})s.Reset();
            Shader.SetGlobalVector(Smear,Vector4.zero);initialized=true;
            ResetGroundPerformance();
        }
        void StartSmear(Vector3 direction,float angular)
        {
            if(smearCooldown>0||flightPose<.65f)return;
            smearAge=0;smearCooldown=.38f;smearDirection=direction;smearTurn=angular;SmearCount++;
        }
        void LateUpdate()
        {
            if(!Motor)return;
            if(!initialized||Motor.WarpVersion!=warpVersion)ResetMotion();
            float dt=Mathf.Min(Time.deltaTime,.075f);if(dt<=0)return;
            clock+=dt;takeoffAge+=dt;landingAge+=dt;boostAge+=dt;brakeAge+=dt;smearAge+=dt;blinkAge+=dt;smearCooldown-=dt;deliverAge+=dt;
            if(!Motor.OnFoot)deliverAge=10;
            float actualSpeed=new Vector2(Motor.Velocity.x,Motor.Velocity.z).magnitude;
            AdvanceWalk(dt,actualSpeed);
            speed=Mathf.Lerp(speed,Mathf.Clamp01(actualSpeed/26),1-Mathf.Exp(-dt*10));
            float rawAcceleration=(actualSpeed-previousSpeed)/dt;
            acceleration=Mathf.Lerp(acceleration,Mathf.Clamp(rawAcceleration,-65,65),1-Mathf.Exp(-dt*9));
            previousSpeed=actualSpeed;
            if(wasGrounded&&!Motor.Grounded){takeoffAge=0;airTime=0;Takeoffs++;blinkAge=0;}
            if(!wasGrounded&&Motor.Grounded&&airTime>.25f)
            {landingAge=0;landingImpact=Mathf.Clamp01(Mathf.Abs(previousVelocity.y)/7+.3f);Landings++;blinkAge=0;}
            airTime=Motor.Grounded?0:airTime+dt;
            if(Motor.Boosting&&!wasBoosting){boostAge=0;StartSmear(new Vector3(0,.04f,-.42f),0);}
            if(acceleration< -23&&brakeAge>.8f&&speed>.28f&&!Motor.Grounded)
            {brakeAge=0;StartSmear(new Vector3(0,.04f,.19f),0);}
            float turn=Motor.Turn;
            if(!Motor.Grounded&&speed>.25f&&Mathf.Abs(turn)>.5f&&(Mathf.Abs(lastTurn)<.5f||Mathf.Sign(turn)!=Mathf.Sign(lastTurn)))
                StartSmear(new Vector3(-Mathf.Sign(turn)*.25f,0,0),-Mathf.Sign(turn)*.46f);
            lastTurn=turn;wasGrounded=Motor.Grounded;wasBoosting=Motor.Boosting;previousVelocity=Motor.Velocity;
            float requestedFlight=Motor.Mounted?Ease((takeoffAge-.09f)/.56f):0;
            flightPose=Mathf.MoveTowards(flightPose,requestedFlight,dt*(Motor.Grounded?3.0f:4.0f));
            float flight=flightPose;
            idleTime=actualSpeed<.2f&&Mathf.Abs(Motor.LiftIntent)<.1f?idleTime+dt:0;
            float idle=1-Ease(speed*5);
            float breath=Mathf.Sin(clock*1.75f)+.28f*Mathf.Sin(clock*3.1f+.6f);
            float hover=flight*idle*(.026f*Mathf.Sin(clock*2.0f)+.012f*Mathf.Sin(clock*3.15f+1));
            // These timings describe a performance, not a change to flight physics:
            // compress, reach, fold into the seat; absorb a landing, then recover.
            float compress=Accent(takeoffAge,.09f,.22f);
            float reach=Accent(Mathf.Max(0,takeoffAge-.12f),.14f,.53f);
            float land=Accent(landingAge,.09f,.46f)*landingImpact;
            float recover=Accent(Mathf.Max(0,landingAge-.25f),.16f,.62f)*landingImpact;
            float boost=Accent(boostAge,.18f,.72f);
            float brake=Accent(brakeAge,.16f,.85f);
            // Handing over a parcel: a quick polite bow that springs back up.
            float bow=Accent(deliverAge,.22f,1.05f);
            float airLean=flight*(5+speed*15+(Motor.Boosting?8:0));
            float bodyPitch=pitch.Step(airLean+Mathf.Clamp(acceleration*.20f,-10,8)+compress*12-reach*9-land*12+recover*3-brake*9+idle*breath*.65f+walkWeight*3+bow*13,3.3f,.75f,dt);
            float bodyBank=bank.Step(-turn*(18+speed*13)*flight+idle*breath*1.2f+stepSway*2.8f,3.0f,.62f,dt);
            // Look into the new heading first. The shoulders follow and the
            // heavy ends (feet, bag, bow) arrive later and overshoot once.
            Vector3 intent=Motor.Visual.InverseTransformDirection(Motor.DesiredVelocity);
            float look=actualSpeed>.3f?Mathf.Clamp(Mathf.Atan2(intent.x,Mathf.Max(.5f,intent.z))*Mathf.Rad2Deg,-32,32):0;
            float glancePhase=Mathf.Repeat(idleTime,9.2f);
            float glance=idleTime>2?Accent(Mathf.Max(0,glancePhase-3.1f),.65f,2.8f)*-25:0;
            float lookYaw=gaze.Step(look+glance,6,.9f,dt);
            float lookPitch=headPitch.Step(-bodyPitch*.73f-Motor.LiftIntent*7+brake*5+breath*.65f+bow*9,5,.82f,dt);
            float bodyY=hover+breath*.005f-compress*.065f+reach*.052f-land*.105f+recover*.022f-walkWeight*(.090f+.022f*Mathf.Cos(walkPhase*Mathf.PI*4));
            Offset("Body",new Vector3(-bodyBank*.0007f+stepSway*.015f,bodyY,-boost*.020f+brake*.025f));
            Pose("Body",new Vector3(bodyPitch,turn*flight*4-stepSway*3.5f,bodyBank));
            Pose("Head",new Vector3(lookPitch,lookYaw,-bodyBank*.36f));
            // The broom noses up into a climb and counters the rider's weight.
            float broomAngle=broomPitch.Step(-Motor.Velocity.y*.75f+brake*5-compress*4+reach*5,3.7f,.7f,dt);
            float drag=legDrag.Step(Mathf.Clamp(acceleration*.25f,-11,10)+Motor.Velocity.y*.55f,2.0f,.60f,dt)*flight;
            float footSwing=idle*flight*Mathf.Sin(clock*2.1f)*5;
            float tuck=flight*(67+speed*7);
            Pose("LeftLeg",new Vector3(-tuck+drag*.36f+brake*7-compress*12,-bodyBank*.16f,-flight*4-bodyBank*.10f));
            Pose("RightLeg",new Vector3(-tuck+3+drag*.46f+brake*10-compress*16,bodyBank*.08f,flight*4-bodyBank*.14f));
            Pose("LeftKnee",new Vector3(flight*97-drag+footSwing-brake*9+compress*23+land*21,0,0));
            Pose("RightKnee",new Vector3(flight*94-drag*.70f-footSwing*.65f-brake*12+compress*28+land*25,0,0));
            Pose("LeftFoot",new Vector3(-flight*18+drag*.5f-footSwing*.7f-compress*11-land*12,flight*2,-bodyBank*.12f));
            Pose("RightFoot",new Vector3(-flight*14+drag*.7f+footSwing*.4f-compress*12-land*13,-flight*3,-bodyBank*.12f));
            float flutterTime=Mathf.Floor(clock*24)/24;
            float wind=flight*(.22f+speed*.70f);
            float ribbonTarget=walkWeight*Mathf.Sin(walkPhase*Mathf.PI*4)*4-speed*19-Mathf.Clamp(acceleration*.4f,-12,12)-Motor.Velocity.y*.7f;
            float ribbonPitch=ribbon.Step(ribbonTarget,3.6f,.48f,dt);
            float leftFlutter=ribbonLeft.Step(stepSway*6+turn*22+wind*Mathf.Sin(flutterTime*10.4f)*8,4.2f,.38f,dt);
            float rightFlutter=ribbonRight.Step(-stepSway*4+turn*15+wind*Mathf.Sin(flutterTime*9.1f+1.8f)*7,3.7f,.43f,dt);
            Pose("Bow",new Vector3(ribbonPitch*.28f,0,-bodyBank*.15f));
            Pose("LeftBowLoop",new Vector3(ribbonPitch+leftFlutter*.45f,leftFlutter*.25f,leftFlutter*.45f));
            Pose("RightBowLoop",new Vector3(ribbonPitch+rightFlutter*.5f,-rightFlutter*.2f,rightFlutter*.45f));
            float clothSway=hem.Step(-turn*.65f+Mathf.Sin(flutterTime*6.3f)*wind*.35f,2.6f,.55f,dt);
            Shape("Flight cloth",flight);
            Shape("Hem left",flight*Mathf.Clamp01(.15f+wind*.22f-clothSway));
            Shape("Hem right",flight*Mathf.Clamp01(.15f+wind*.22f+clothSway));
            Shape("Walk left",(1-flight)*Mathf.Max(0,-stepSway));Shape("Walk right",(1-flight)*Mathf.Max(0,stepSway));
            Shape("Hair stream",flight*Mathf.Clamp01(speed*.75f+Mathf.Max(0,acceleration)*.013f));
            Shape("Hair left",Mathf.Max(0,-clothSway)*flight+Mathf.Max(0,-stepSway)*.18f);Shape("Hair right",Mathf.Max(0,clothSway)*flight+Mathf.Max(0,stepSway)*.18f);
            float bagAngle=bagSway.Step(bodyBank*.44f-acceleration*.10f+stepSway*7,2.2f,.58f,dt);
            Pose("Satchel",new Vector3(-drag*.45f,bagAngle*.25f,bagAngle*.48f));
            float catLook=catHead.Step(-lookYaw*.65f+idle*Mathf.Sin(clock*.63f)*14,3.0f,.7f,dt);
            float tailAngle=tail.Step(bodyBank*.8f+wind*Mathf.Sin(clock*3.3f)*12+bow*Mathf.Sin(deliverAge*9)*22,2.1f,.55f,dt);
            Pose("JijiHead",new Vector3(-speed*7-brake*5,catLook,bodyBank*.16f));
            Pose("JijiTail",new Vector3(-drag*.3f,tailAngle,tailAngle*.35f));
            Pose("LeftJijiEar",new Vector3(-speed*12,-turn*8,-boost*16+idle*Mathf.Sin(clock*1.7f)*2));
            Pose("RightJijiEar",new Vector3(-speed*10,-turn*8,boost*13+idle*Mathf.Sin(clock*1.3f+2)*2));
            AnimateFace(lookYaw,boost,brake,land);
            if(!PlaceWalkingFeet(dt)){PlaceFeet();feetReady=false;}
            PositionBroom(flight,hover,broomAngle,bodyBank);
            GroundArms(flight);
            PositionJiji(flight,speed*8+Mathf.Clamp(acceleration*.18f,-6,8),-bodyBank*.35f);
            ClothContact("LeftLeg","LeftKnee",LeftHip,LeftKnee,Mathf.Max(flight,walkWeight));
            ClothContact("RightLeg","RightKnee",RightHip,RightKnee,Mathf.Max(flight,walkWeight));
            // Two 1/24-second drawings bridge a fast change of pose. All other
            // frames are undeformed, including the face and its painted details.
            int exposure=Mathf.FloorToInt(smearAge*24);
            SmearWeight=exposure==1?1:exposure==2?.55f:0;
            Shader.SetGlobalMatrix(WorldToPose,Motor.Visual.worldToLocalMatrix);
            Shader.SetGlobalMatrix(PoseToWorld,Motor.Visual.localToWorldMatrix);
            Shader.SetGlobalVector(Smear,new Vector4(smearDirection.x,smearDirection.y,smearDirection.z,smearTurn)*SmearWeight);
            Beat=Motor.OnFoot?(Delivering?"Deliver / bow and wave":landingAge<.7f?"Dismount / settle":walkWeight>.15f?"Walk / carry broom":"Rest / broom at side"):takeoffAge<.75f?"Mount / reach":boostAge<.72f?"Boost / tuck":brakeAge<.85f?"Brake / catch balance":Mathf.Abs(bodyBank)>6?"Bank / follow through":speed<.15f?"Hover / breathe":"Cruise / wind";
        }
        void ClothContact(string hipName,string kneeName,int hipId,int kneeId,float amount)
        {
            var hip=Node(hipName);var knee=Node(kneeName);if(!hip||!knee)return;
            var a=hip.position;var b=knee.position;
            Shader.SetGlobalVector(hipId,new Vector4(a.x,a.y,a.z,amount));
            Shader.SetGlobalVector(kneeId,new Vector4(b.x,b.y,b.z,.095f));
        }
        void AnimateFace(float look,float boost,float brake,float land)
        {
            if(clock>=nextBlink){blinkAge=0;nextBlink=clock+3.1f+Mathf.Repeat(clock*1.73f,2.6f);}
            float blink=blinkAge<.065f?Mathf.Lerp(1,.04f,blinkAge/.065f):blinkAge<.115f?.04f:Mathf.Lerp(.04f,1,Ease((blinkAge-.115f)/.095f));
            foreach(var eye in eyes)
            {
                bool cat=eye.Key.name.StartsWith("Jiji");
                float catBlink=Mathf.Repeat(clock+1.9f,5.7f)>.0f&&Mathf.Repeat(clock+1.9f,5.7f)<.16f?.08f:1;
                float aperture=cat?catBlink:blink*(1-boost*.14f);
                Vector3 up=eye.Key.InverseTransformDirection(Motor.Visual.up),scale=eye.Value;
                int axis=Mathf.Abs(up.x)>Mathf.Abs(up.y)?0:1;if(Mathf.Abs(up.z)>Mathf.Abs(up[axis]))axis=2;
                scale[axis]*=aperture;eye.Key.localScale=scale;
            }
            Shape("Gaze left",Mathf.Max(0,-look/32));Shape("Gaze right",Mathf.Max(0,look/32));
            Pose("LeftBrow",new Vector3(0,0,-boost*8+brake*5));Pose("RightBrow",new Vector3(0,0,boost*6-brake*7));
            bool open=takeoffAge>.12f&&takeoffAge<.34f||brake>.65f||land>.7f;
            if(quietMouth)quietMouth.enabled=!open;if(breathMouth)breathMouth.enabled=open;
        }
        void PlaceFeet()
        {
            GroundContactError=float.PositiveInfinity;GroundFloorHeight=float.NegativeInfinity;
            if(Motor.Grounded&&soles.Count==2&&Physics.Raycast(Motor.transform.position+Vector3.up*.75f,Vector3.down,out var floor,1.4f,1<<8,QueryTriggerInteraction.Ignore))
            {
                GroundFloorHeight=floor.point.y;
                float lowest=Mathf.Min(soles[0].bounds.min.y,soles[1].bounds.min.y);
                float target=Mathf.Clamp(groundOffset+floor.point.y+.006f-lowest,-.80f,.45f);
                // Fit each displayed drawing immediately so the landing accent
                // bends the knees without leaving feet below the street surface.
                GroundContactError=Mathf.Abs(lowest+target-groundOffset-floor.point.y-.006f);groundOffset=target;
            }
            else groundOffset=Mathf.MoveTowards(groundOffset,0,Time.deltaTime*2);
            Motor.Visual.localPosition=Vector3.up*groundOffset;
        }
        void SolveArm(Arm arm,Vector3 target,Quaternion wristRotation,bool measureGrip)
        {
            arm.Shoulder.localRotation=arm.ShoulderRest;arm.Elbow.localRotation=arm.ElbowRest;
            Vector3 start=arm.Shoulder.position,delta=target-start;
            float distance=Mathf.Clamp(delta.magnitude,.001f,arm.UpperLength+arm.LowerLength-.0001f);
            Vector3 direction=delta.normalized;
            Vector3 pole=Motor.Visual.TransformDirection(Vector3.Lerp(new Vector3(arm.Side*.25f,-1,-.10f),new Vector3(arm.Side,-.15f,-.25f),Ease(flightPose)));
            Vector3 bend=Vector3.ProjectOnPlane(pole,direction).normalized;
            float along=(arm.UpperLength*arm.UpperLength-arm.LowerLength*arm.LowerLength+distance*distance)/(2*distance);
            float away=Mathf.Sqrt(Mathf.Max(0,arm.UpperLength*arm.UpperLength-along*along));
            Vector3 elbow=start+direction*along+bend*away;
            arm.Shoulder.rotation=Quaternion.FromToRotation(arm.Elbow.position-start,elbow-start)*arm.Shoulder.rotation;
            arm.Elbow.rotation=Quaternion.FromToRotation(arm.Wrist.position-arm.Elbow.position,target-arm.Elbow.position)*arm.Elbow.rotation;
            arm.Wrist.rotation=wristRotation;
            if(measureGrip)GripError=Mathf.Max(GripError,Vector3.Distance(arm.Wrist.position,target));
        }
        void OnDisable(){Shader.SetGlobalVector(Smear,Vector4.zero);}
    }
}
