using Unity.Cinemachine;
using UnityEngine;

namespace Koriko
{
    /// <summary>Framing only; steering and collision are untouched. A slow flyover sits behind
    /// the title card, walking brings the camera closer to Kiki, and flight pulls back a little
    /// further while boosting. Development checks that pose their own camera disable the app.</summary>
    public sealed class CameraDirector : MonoBehaviour
    {
        public GameApp App;
        public CinemachineCamera Follow,Title;
        public CinemachineThirdPersonFollow Body;
        public CinemachineBrain Brain;
        public float WalkDistance=5.6f,FlightDistance=7.5f;
        public float TitleAngle {get;private set;}=3.55f;
        RiderPerformance rider;
        float boost;bool wasPlaying;

        void Start()
        {
            if(App)rider=App.Motor.Visual.GetComponentInChildren<RiderPerformance>();
            // Development captures need the gameplay camera immediately after Begin.
            if(Brain&&DevelopmentFlightCheck.Requested)Brain.DefaultBlend=new CinemachineBlendDefinition(CinemachineBlendDefinition.Styles.Cut,0);
            PlaceTitle(0);
        }
        void Update()
        {
            if(!App||!Follow||!Title)return;
            bool playing=App.InGame;
            Title.Priority=playing?0:20;
            if(!playing){PlaceTitle(Time.unscaledDeltaTime);wasPlaying=false;return;}
            if(!wasPlaying&&Brain&&!DevelopmentFlightCheck.Requested)Brain.DefaultBlend=new CinemachineBlendDefinition(CinemachineBlendDefinition.Styles.EaseInOut,2.4f);
            wasPlaying=true;
            if(!App.enabled||!Body)return;
            float dt=Mathf.Min(Time.deltaTime,.1f);
            float flight=rider?rider.FlightPose:App.Motor.Mounted?1:0;
            boost=Mathf.MoveTowards(boost,App.Motor.Boosting?1:0,dt*(App.Motor.Boosting?1.6f:.9f));
            float distance=Mathf.Lerp(WalkDistance,FlightDistance,flight)+boost*1.2f;
            Body.CameraDistance=Mathf.Lerp(Body.CameraDistance,distance,1-Mathf.Exp(-dt*2.2f));
            Body.VerticalArmLength=Mathf.Lerp(Body.VerticalArmLength,Mathf.Lerp(.32f,.45f,flight),1-Mathf.Exp(-dt*2.2f));
            var lens=Follow.Lens;lens.FieldOfView=Mathf.Lerp(lens.FieldOfView,52+boost*5,1-Mathf.Exp(-dt*3));Follow.Lens=lens;
        }
        void PlaceTitle(float dt)
        {
            if(!Title)return;
            // A slow high circle over the roofs, looking across the market toward the harbour.
            TitleAngle+=dt*.016f;
            var center=new Vector3(-8,0,-6);
            var position=center+new Vector3(Mathf.Cos(TitleAngle)*168,54+Mathf.Sin(TitleAngle*1.7f)*5,Mathf.Sin(TitleAngle)*168);
            var look=new Vector3(8,9,-18);
            Title.transform.SetPositionAndRotation(position,Quaternion.LookRotation(look-position,Vector3.up));
        }
    }
}
