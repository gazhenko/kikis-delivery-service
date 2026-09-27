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
            struct Attributes { float4 positionOS:POSITION; };
            struct Varyings { float4 positionCS:SV_POSITION; float3 direction:TEXCOORD0; };
            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionCS=TransformObjectToHClip(input.positionOS.xyz);
                output.direction=input.positionOS.xyz;
                return output;
            }
            half4 Frag(Varyings input):SV_Target
            {
                float3 direction=normalize(input.direction);
                float2 uv=float2(atan2(direction.x,direction.z)/(2*PI)+.5,asin(clamp(direction.y,-1,1))/PI+.5);
                half3 painted=SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex,uv).rgb;
                painted*=lerp(half3(.09,.14,.27),half3(1,1,1),_KorikoDaylight);
                // The painted horizon meets the same atmospheric color as distant geometry.
                float horizon=1-smoothstep(-.02,.19,direction.y);
                return half4(lerp(painted,_KorikoHorizonColor.rgb,horizon),1);
            }
            ENDHLSL
        }
    }
}
