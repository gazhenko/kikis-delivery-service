Shader "Koriko/Ink"
{
    Properties { _Color("Ink",Color)=(.12,.12,.18,1) _Thickness("Width in pixels",Range(0,4))=1.25 }
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" "Queue"="Geometry-1" }
        Pass
        {
            Tags { "LightMode"="SRPDefaultUnlit" }
            Cull Front ZWrite On
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_fog
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            CBUFFER_START(UnityPerMaterial) float4 _Color; float _Thickness; CBUFFER_END
            struct A { float4 p:POSITION; float3 n:NORMAL; float4 color:COLOR; };
            struct V { float4 p:SV_POSITION; float fog:TEXCOORD0; };
            V Vert(A a)
            {
                V v;float3 world=TransformObjectToWorld(a.p.xyz);v.p=TransformWorldToHClip(world);
                float3 normal=TransformWorldToViewDir(TransformObjectToWorldNormal(a.n),true);
                float2 direction=normal.xy/max(length(normal.xy),.0001);
                // Perspective independent width; authored alpha tapers selected contours.
                v.p.xy+=direction*(2.0/_ScreenParams.xy)*_Thickness*a.color.a*v.p.w;
                v.fog=ComputeFogFactor(v.p.z);return v;
            }
            half4 Frag(V v):SV_Target{return half4(MixFog(_Color.rgb,v.fog),1);}
            ENDHLSL
        }
    }
}
