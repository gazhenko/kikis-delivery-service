Shader "Koriko/PaintedSea"
{
    Properties
    {
        _Color("Sea paint",Color)=(.22,.53,.57,1)
        _FoamColor("Drawn wave paint",Color)=(.72,.86,.78,1)
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" }
        Pass
        {
            Tags { "LightMode"="UniversalForward" }
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_fog
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            CBUFFER_START(UnityPerMaterial) float4 _Color,_FoamColor; CBUFFER_END
            float _KorikoDaylight;
            struct A { float4 p:POSITION; };
            struct V { float4 p:SV_POSITION; float3 world:TEXCOORD0; float fog:TEXCOORD1; };
            V Vert(A a){V v;v.world=TransformObjectToWorld(a.p.xyz);v.p=TransformWorldToHClip(v.world);v.fog=ComputeFogFactor(v.p.z);return v;}
            half4 Frag(V i):SV_Target
            {
                float time=floor(_Time.y*12)/12;
                float2 p=i.world.xz;
                float row=floor((p.y+time*.32)/8.5);
                float wave=sin(p.x*.10+row*1.73)*.85+sin(p.x*.31+row)*.13;
                float strokeDistance=abs(frac((p.y+time*.32+wave)/8.5)-.5)*8.5;
                float width=max(fwidth(strokeDistance),.025);
                float stroke=1-smoothstep(.065,.065+width,strokeDistance);
                float segment=sin(p.x*.16+row*2.7);
                stroke*=smoothstep(.14,.30,segment)*(1-smoothstep(.88,.97,segment));
                float distance=length(_WorldSpaceCameraPos.xz-p);
                stroke*=1-smoothstep(80,210,distance);
                half3 paint=_Color.rgb*lerp(.94,1.04,smoothstep(-.2,.25,sin(p.y*.024+p.x*.009)));
                paint=lerp(paint,_FoamColor.rgb,stroke*.75);
                paint*=lerp(half3(.30,.43,.64),half3(1,1,1),_KorikoDaylight);
                return half4(MixFog(paint,i.fog),1);
            }
            ENDHLSL
        }
    }
}
