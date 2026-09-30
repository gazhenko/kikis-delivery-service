Shader "Koriko/PaintedSky"
{
    Properties { _MainTex("Painted panorama", 2D) = "white" {} }
    SubShader
    {
        Tags { "Queue"="Background" "RenderType"="Background" "PreviewType"="Skybox" "RenderPipeline"="UniversalPipeline" }
        Cull Off ZWrite Off
        Pass
        {
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex);
            float _KorikoDaylight;
            float4 _KorikoHorizonColor;
            float3 _KorikoMoonDirection;
            struct Attributes { float4 positionOS:POSITION; };
            struct Varyings { float4 positionCS:SV_POSITION; float3 direction:TEXCOORD0; };
            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionCS=TransformObjectToHClip(input.positionOS.xyz);
                output.direction=input.positionOS.xyz;
                return output;
            }
            float Hash(float3 p){p=frac(p*float3(.1031,.1030,.0973));p+=dot(p,p.yxz+33.33);return frac((p.x+p.y)*p.z);}
            half4 Frag(Varyings input):SV_Target
            {
                float3 direction=normalize(input.direction);
                // The cloud banks drift very slowly; one lap of the panorama takes about eighteen minutes.
                float2 uv=float2(frac(atan2(direction.x,direction.z)/(2*PI)+.5+_Time.y*.0009),asin(clamp(direction.y,-1,1))/PI+.5);
                // atan2 wraps longitude from 1 to 0. The uncorrected derivative
                // selects the coarsest mip along that seam, drawing a bright line.
                float2 dx=ddx(uv),dy=ddy(uv);
                dx.x-=round(dx.x);dy.x-=round(dy.x);
                half3 painted=SAMPLE_TEXTURE2D_GRAD(_MainTex,sampler_MainTex,uv,dx,dy).rgb;
                // Open blue sky is saturated; cloud banks are warm and pale. Stars stay behind clouds.
                float openSky=smoothstep(.10,.26,painted.b-painted.r);
                painted*=lerp(half3(.09,.14,.27),half3(1,1,1),_KorikoDaylight);
                float night=1-smoothstep(.08,.5,_KorikoDaylight);
                if(night>.001&&direction.y>0)
                {
                    // Sparse hand-placed-looking points; each twinkles on its own slow beat.
                    float3 grid=direction*92;float3 cell=floor(grid);
                    float seed=Hash(cell),size=Hash(cell+17.1);
                    float3 offset=(float3(Hash(cell+3.7),Hash(cell+9.2),Hash(cell+5.3))-.5)*.5;
                    float distanceToStar=length(frac(grid)-.5-offset);
                    float star=step(.968,seed)*(1-smoothstep(.07+size*.09,.13+size*.12,distanceToStar));
                    star*=.62+.38*sin(_Time.y*(1.3+size*2.4)+seed*61);
                    painted+=half3(.93,.91,.80)*star*night*openSky*smoothstep(.04,.32,direction.y)*.85;
                    // A painted moon over the harbour, with a soft ring in the night haze.
                    float3 moon=normalize(_KorikoMoonDirection);
                    float facing=dot(direction,moon);
                    float disc=smoothstep(.99964,.99975,facing);
                    float shade=smoothstep(.99962,.99996,dot(direction,normalize(moon+float3(-.009,.005,0))));
                    half3 moonPaint=lerp(half3(.86,.84,.74),half3(.99,.96,.84),shade);
                    float halo=pow(saturate(facing),1400)*.30+pow(saturate(facing),120)*.10;
                    painted=lerp(painted,moonPaint,disc*night);
                    painted+=half3(.70,.74,.80)*halo*night*(1-disc);
                }
                // The painted horizon meets the same atmospheric color as distant geometry.
                float horizon=1-smoothstep(-.02,.19,direction.y);
                return half4(lerp(painted,_KorikoHorizonColor.rgb,horizon),1);
            }
            ENDHLSL
        }
    }
}
