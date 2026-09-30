Shader "Koriko/CourtRing"
{
    // A chalk-and-ribbon circle painted on the active delivery court. Its radius is
    // the real delivery radius, so landing inside it is exactly what counts.
    Properties
    {
        _Color("Ribbon paint",Color)=(.96,.73,.30,1)
        _Alpha("Opacity",Range(0,1))=1
        _Ready("Inside and slow",Range(0,1))=0
    }
    SubShader
    {
        Tags { "Queue"="Transparent-10" "RenderType"="Transparent" "IgnoreProjector"="True" "RenderPipeline"="UniversalPipeline" }
        Pass
        {
            Tags { "LightMode"="UniversalForward" }
            Blend SrcAlpha OneMinusSrcAlpha ZWrite Off ZTest LEqual Cull Off Offset -2,-2
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_fog
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            CBUFFER_START(UnityPerMaterial) float4 _Color; float _Alpha,_Ready; CBUFFER_END
            float _KorikoDaylight;
            struct A { float4 p:POSITION; float2 uv:TEXCOORD0; };
            struct V { float4 p:SV_POSITION; float2 uv:TEXCOORD0; float fog:TEXCOORD1; };
            V Vert(A a){V v;v.p=TransformObjectToHClip(a.p.xyz);v.uv=a.uv;v.fog=ComputeFogFactor(v.p.z);return v;}
            half4 Frag(V v):SV_Target
            {
                float r=length(v.uv);float width=max(fwidth(r),.004);
                float time=floor(_Time.y*12)/12;
                float angle=atan2(v.uv.y,v.uv.x);
                // Twenty drawn dashes walk slowly around the circle.
                float dash=smoothstep(.30,.36,frac(angle/(2*PI)*20+time*.18));
                dash*=1-smoothstep(.80,.86,frac(angle/(2*PI)*20+time*.18));
                float band=smoothstep(.885-width,.885+width,r)*(1-smoothstep(.965-width,.965+width,r));
                float inner=(1-smoothstep(.80,.86,r))*(.07+.11*_Ready);
                float edge=smoothstep(.80-width,.80+width,r)*(1-smoothstep(.83-width,.83+width,r))*(.35+.4*_Ready);
                float alpha=saturate(band*lerp(.55,.95,dash)+inner+edge)*_Alpha;
                half3 paint=lerp(_Color.rgb,half3(1,.93,.72),_Ready*.45);
                paint*=lerp(half3(.62,.62,.72),half3(1,1,1),_KorikoDaylight);
                return half4(MixFog(paint,v.fog),alpha);
            }
            ENDHLSL
        }
    }
}
