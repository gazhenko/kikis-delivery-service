using Koriko.Core;
using UnityEngine;

namespace Koriko
{
    public sealed class Daylight : MonoBehaviour
    {
        public Light Sun;
        public Camera Camera;
        // The title flyover holds a late-afternoon light while no game clock runs.
        public float TitlePhase=.462f;
        public static readonly Vector3 MoonDirection=new Vector3(.30f,.36f,-1).normalized;
        readonly Color daySky=new Color(.63f,.76f,.79f);
        readonly Color nightSky=new Color(.067f,.10f,.19f);
        public float Night {get;private set;}
        public void Refresh(Rules rules,bool playing)
        {
            float phase=playing?(float)(rules.State.elapsed%Rules.CycleSeconds/Rules.CycleSeconds):TitlePhase;
            float altitude=Mathf.Sin(phase*Mathf.PI*2);
            float daylight=Mathf.SmoothStep(0,1,Mathf.Clamp01((altitude+.23f)*2.15f));
            Night=1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(.15f,.7f,daylight));
            Color sky=Color.Lerp(nightSky,daySky,daylight);
            float sunset=(1-Mathf.Abs(altitude)*5)*daylight;
            sky=Color.Lerp(sky,new Color(.83f,.64f,.52f),Mathf.Clamp01(sunset)*.6f);
            Shader.SetGlobalFloat("_KorikoDaylight",daylight);
            Shader.SetGlobalColor("_KorikoHorizonColor",sky.linear);
            Shader.SetGlobalVector("_KorikoMoonDirection",MoonDirection);
            if(Camera)
            {
                // A separate foreground key, from high and to the side of the camera, like a cel
                // painter's light: every form gets a clear shadow shape on its far side, while
                // the face (flattened toward the viewer) stays readable from every flight angle.
                Vector3 key=(-Camera.transform.forward*.30f+Camera.transform.right*.78f+Vector3.up*.72f).normalized;
                Shader.SetGlobalVector("_KorikoCelLightDirection",key);
            }
            // Aerial perspective: the far side of town stays readable while hills and
            // headlands dissolve into the painted horizon.
            RenderSettings.fog=true;RenderSettings.fogMode=FogMode.Linear;RenderSettings.fogStartDistance=95;RenderSettings.fogEndDistance=640;
            RenderSettings.fogColor=sky;RenderSettings.ambientLight=Color.Lerp(new Color(.22f,.28f,.43f),new Color(.7f,.75f,.71f),daylight);
            if(Camera)Camera.backgroundColor=sky;
            if(Sun)
            {
                Sun.transform.rotation=Quaternion.Euler(phase*360,-32,0);
                Sun.intensity=Mathf.Lerp(.2f,1.15f,daylight);
                Sun.color=Color.Lerp(new Color(.54f,.64f,.91f),new Color(1,.91f,.76f),daylight);
                Shader.SetGlobalVector("_KorikoSunDirection",-Sun.transform.forward);
            }
        }
    }
}
