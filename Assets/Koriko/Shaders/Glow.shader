Shader "Koriko/Glow"
{
    // Painted lamp light: a soft halo drawn toward the camera and flat warm pools
    // on the paving. Additive, unlit, and only present after dusk.
    Properties
    {
        _Color("Lamp paint",Color)=(1,.72,.38,1)
        _Intensity("Intensity",Range(0,3))=1
        _Always("Visible in daylight",Range(0,1))=0
    }
    SubShader
    {
        Tags { "Queue"="Transparent+20" "RenderType"="Transparent" "IgnoreProjector"="True" "RenderPipeline"="UniversalPipeline" }
        Pass
        {
            Tags { "LightMode"="UniversalForward" }
            Blend One One ZWrite Off ZTest LEqual Cull Off
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_fog
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            CBUFFER_START(UnityPerMaterial) float4 _Color; float _Intensity,_Always; CBUFFER_END
            float _KorikoDaylight;
            // uv: quad corner in -1..1. uv2.x: halo radius in metres; uv2.y: 1 = faces the camera, 0 = lies flat.
            struct A { float4 p:POSITION; float2 uv:TEXCOORD0; float2 uv2:TEXCOORD1; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct V { float4 p:SV_POSITION; float2 corner:TEXCOORD0; float2 kind:TEXCOORD1; float fog:TEXCOORD2; };
            V Vert(A a)
            {
                V v;UNITY_SETUP_INSTANCE_ID(a);
                float3 world=TransformObjectToWorld(a.p.xyz);
                if(a.uv2.y>.5)
                {
                    float3 view=TransformWorldToView(world);
                    // Nudge the drawing toward the viewer so the lamp glass never cuts it.
                    view.xy+=a.uv*a.uv2.x;view.z+=min(.35,-view.z*.5);
                    v.p=TransformWViewToHClip(view);
                }
                else v.p=TransformWorldToHClip(world);
                v.corner=a.uv;v.kind=a.uv2;v.fog=ComputeFogFactor(v.p.z);return v;
            }
            half4 Frag(V v):SV_Target
            {
                float r=length(v.corner);
                float night=max(_Always,1-smoothstep(.12,.62,_KorikoDaylight));
                float glow;
                if(v.kind.y>.5)glow=exp(-r*r*5.5)*.55+(1-smoothstep(.10,.24,r))*.55;
                else glow=(1-smoothstep(.15,1,r))*.20*(1-smoothstep(.85,1,r));
                half3 color=_Color.rgb*glow*night*_Intensity;
                return half4(MixFogColor(color,half3(0,0,0),v.fog),1);
            }
            ENDHLSL
        }
    }
}
