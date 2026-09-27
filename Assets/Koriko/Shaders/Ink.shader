Shader "Koriko/Ink"
{
    Properties { _Color("Ink",Color)=(.075,.07,.09,1) _Thickness("Width",Range(0,.03))=.006 }
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
            struct A { float4 p:POSITION; float3 n:NORMAL; };
            struct V { float4 p:SV_POSITION; float fog:TEXCOORD0; };
            V Vert(A a){V v;v.p=TransformObjectToHClip(a.p.xyz+a.n*_Thickness);v.fog=ComputeFogFactor(v.p.z);return v;}
            half4 Frag(V v):SV_Target{return half4(MixFog(_Color.rgb,v.fog),1);}
            ENDHLSL
        }
    }
}
