using Koriko.Core;
using UnityEngine;

namespace Koriko
{
    public sealed class Daylight : MonoBehaviour
    {
        public Light Sun;
        public Camera Camera;
        readonly Color daySky=new Color(.63f,.76f,.79f);
        readonly Color nightSky=new Color(.067f,.10f,.19f);
        public void Refresh(Rules rules,bool playing)
        {
            float phase=playing?(float)(rules.State.elapsed%Rules.CycleSeconds/Rules.CycleSeconds):.18f;
            float altitude=Mathf.Sin(phase*Mathf.PI*2);
            float daylight=Mathf.SmoothStep(0,1,Mathf.Clamp01((altitude+.23f)*2.15f));
            Color sky=Color.Lerp(nightSky,daySky,daylight);
            float sunset=(1-Mathf.Abs(altitude)*5)*daylight;
            sky=Color.Lerp(sky,new Color(.83f,.64f,.52f),Mathf.Clamp01(sunset)*.6f);
            Shader.SetGlobalFloat("_KorikoDaylight",daylight);
            Shader.SetGlobalColor("_KorikoHorizonColor",sky.linear);
            RenderSettings.fog=true;RenderSettings.fogMode=FogMode.Linear;RenderSettings.fogStartDistance=115;RenderSettings.fogEndDistance=390;
            RenderSettings.fogColor=sky;RenderSettings.ambientLight=Color.Lerp(new Color(.22f,.28f,.43f),new Color(.7f,.75f,.71f),daylight);
            if(Camera)Camera.backgroundColor=sky;
            if(Sun)
            {
                Sun.transform.rotation=Quaternion.Euler(phase*360,-32,0);
                Sun.intensity=Mathf.Lerp(.2f,1.15f,daylight);
                Sun.color=Color.Lerp(new Color(.54f,.64f,.91f),new Color(1,.91f,.76f),daylight);
            }
        }
    }
}
