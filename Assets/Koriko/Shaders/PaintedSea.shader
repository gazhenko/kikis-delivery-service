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
            float3 _KorikoMoonDirection,_KorikoSunDirection;
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
                // Broad painted bands of sea colour, each a flat wash, instead of a smooth gradient.
                float wash=sin(p.y*.024+p.x*.009);
                float band=floor(saturate(wash*.5+.5)*3)/3;
                half3 paint=_Color.rgb*lerp(.93,1.05,band);
                paint=lerp(paint,_FoamColor.rgb,stroke*.75);
                paint*=lerp(half3(.30,.43,.64),half3(1,1,1),_KorikoDaylight);
                // A path of drawn glints toward the moon at night and a low sun at golden hour.
                // Short dashes on the same 12 Hz drawing clock; no specular gloss or screen noise.
                float3 view=normalize(i.world-_WorldSpaceCameraPos);
                float3 bounce=float3(view.x,-view.y,view.z);
                float night=1-smoothstep(.10,.55,_KorikoDaylight);
                float3 sun=normalize(_KorikoSunDirection+float3(0,.0001,0));
                float lowSun=saturate(1-abs(sun.y)*3.2)*step(0,sun.y)*_KorikoDaylight;
                float moonPath=pow(saturate(dot(bounce,normalize(_KorikoMoonDirection))),70)*night;
                float sunPath=pow(saturate(dot(bounce,sun)),55)*lowSun;
                float glintRow=floor(p.y*.62+time*.9);
                float glint=smoothstep(.55,.9,sin(p.x*.83+glintRow*2.31+time*1.7))*smoothstep(.2,.7,sin(p.y*1.9+glintRow));
                paint=lerp(paint,half3(.80,.83,.82),saturate(moonPath*(.20+glint*.95)));
                paint=lerp(paint,half3(1.0,.80,.52),saturate(sunPath*(.15+glint*.85)));
                return half4(MixFog(paint,i.fog),1);
            }
            ENDHLSL
        }
        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode"="DepthOnly" }
            ZWrite On ColorMask R
            HLSLPROGRAM
            #pragma vertex DepthVert
            #pragma fragment DepthFrag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            struct DA { float4 p:POSITION; };
            float4 DepthVert(DA a):SV_POSITION { return TransformObjectToHClip(a.p.xyz); }
            half4 DepthFrag():SV_Target { return 0; }
            ENDHLSL
        }
        Pass
        {
            Name "DepthNormals"
            Tags { "LightMode"="DepthNormals" }
            ZWrite On
            HLSLPROGRAM
            #pragma vertex NormalsVert
            #pragma fragment NormalsFrag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            struct NA { float4 p:POSITION; };
            float4 NormalsVert(NA a):SV_POSITION { return TransformObjectToHClip(a.p.xyz); }
            half4 NormalsFrag():SV_Target { return half4(0,1,0,0); }
            ENDHLSL
        }
    }
}
