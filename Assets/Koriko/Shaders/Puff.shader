Shader "Koriko/Puff"
{
    // Chimney smoke and takeoff dust drawn as flat cel puffs: a lit upper shape,
    // a painted shadow shape and a slightly lobed edge. No texture or noise.
    Properties { _ShadowTint("Shadow paint",Color)=(.72,.74,.80,1) }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" "RenderPipeline"="UniversalPipeline" }
        Pass
        {
            Tags { "LightMode"="UniversalForward" }
            Blend SrcAlpha OneMinusSrcAlpha ZWrite Off ZTest LEqual Cull Off
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_fog
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            CBUFFER_START(UnityPerMaterial) float4 _ShadowTint; CBUFFER_END
            float _KorikoDaylight;
            // uv: corner in -1..1; uv2.x: radius in metres, uv2.y: drawing rotation; color: paint and opacity.
            struct A { float4 p:POSITION; float2 uv:TEXCOORD0; float2 uv2:TEXCOORD1; float4 color:COLOR; };
            struct V { float4 p:SV_POSITION; float2 corner:TEXCOORD0; float4 color:TEXCOORD1; float fog:TEXCOORD2; float seed:TEXCOORD3; };
            V Vert(A a)
            {
                V v;float3 view=TransformWorldToView(TransformObjectToWorld(a.p.xyz));
                float s,c;sincos(a.uv2.y,s,c);
                view.xy+=float2(a.uv.x*c-a.uv.y*s,a.uv.x*s+a.uv.y*c)*a.uv2.x;
                v.p=TransformWViewToHClip(view);v.corner=a.uv;v.color=a.color;v.fog=ComputeFogFactor(v.p.z);v.seed=a.uv2.y;return v;
            }
            half4 Frag(V v):SV_Target
            {
                float angle=atan2(v.corner.y,v.corner.x);
                float edge=.80+.07*sin(angle*5+v.seed*3)+.04*sin(angle*9-v.seed*5);
                float r=length(v.corner);
                float width=max(fwidth(r),.01);
                float shape=1-smoothstep(edge-width,edge+width,r);
                // Light from above: the lower part of each puff takes the painted shadow shape.
                float lit=smoothstep(-.18-width,-.18+width,v.corner.y*.85-v.corner.x*.25+.12*sin(angle*3+v.seed));
                half3 paint=lerp(v.color.rgb*_ShadowTint.rgb,v.color.rgb,lit);
                paint*=lerp(half3(.36,.44,.62),half3(1,1,1),_KorikoDaylight);
                return half4(MixFog(paint,v.fog),shape*v.color.a);
            }
            ENDHLSL
        }
    }
}
