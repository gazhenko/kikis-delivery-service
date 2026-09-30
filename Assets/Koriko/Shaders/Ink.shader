Shader "Koriko/Ink"
{
    Properties { _Color("Ink",Color)=(.12,.12,.18,1) _Thickness("Width in pixels",Range(0,4))=1.25 _Cloth("Rider cloth contact",Range(0,1))=0 }
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
            #include "CharacterDeform.hlsl"
            CBUFFER_START(UnityPerMaterial) float4 _Color; float _Thickness, _Cloth; CBUFFER_END
            struct A { float4 p:POSITION; float3 n:NORMAL; float4 color:COLOR; };
            struct V { float4 p:SV_POSITION; float fog:TEXCOORD0; };
            V Vert(A a)
            {
                V v;float3 world=RiderWorldPosition(a.p.xyz,_Cloth);v.p=TransformWorldToHClip(world);
                float3 normal=TransformWorldToViewDir(TransformObjectToWorldNormal(a.n),true);
                float2 direction=normal.xy/max(length(normal.xy),.0001);
                // Width is set in 1080p pixels and scales with the window, like a line drawn on
                // the cel; it thins with distance so a far rider is not outlined too heavily.
                // Authored vertex alpha still tapers selected contours.
                float scale=_ScreenParams.y/1080.0*lerp(1.0,.55,saturate((v.p.w-5.0)/25.0));
                v.p.xy+=direction*(2.0/_ScreenParams.xy)*_Thickness*scale*a.color.a*v.p.w;
                v.fog=ComputeFogFactor(v.p.z);return v;
            }
            half4 Frag(V v):SV_Target{return half4(MixFog(_Color.rgb,v.fog),1);}
            ENDHLSL
        }
    }
}
