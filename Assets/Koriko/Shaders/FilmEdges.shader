Shader "Koriko/FilmEdges"
{
    // A full-screen pass after the opaque town is painted: the painter's line and the film grade.
    // Lines are drawn where depth breaks (silhouettes) and where surfaces crease (roof edges,
    // corners), in a warm dark ink that thins with distance and fades into the haze. The grade
    // warms the daylight, lifts the blacks a little and keeps colour rich, like film stock.
    Properties
    {
        _InkColor("Painter's line",Color)=(.33,.19,.14,1)
        _EdgeStrength("Silhouette line",Range(0,1))=.65
        _CreaseStrength("Crease line",Range(0,1))=.30
        _Warmth("Film warmth",Range(0,1))=.55
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" }
        ZWrite Off Cull Off ZTest Always
        Pass
        {
            Name "FilmEdges"
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareNormalsTexture.hlsl"
            float4 _InkColor;
            float _EdgeStrength,_CreaseStrength,_Warmth;
            float _KorikoDaylight;
            float Eye(float2 uv){ return LinearEyeDepth(SampleSceneDepth(uv),_ZBufferParams); }
            half4 Frag(Varyings input):SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                float2 uv=input.texcoord;
                float2 px=1.0/_ScreenParams.xy;
                half3 color=SAMPLE_TEXTURE2D_X(_BlitTexture,sampler_LinearClamp,uv).rgb;
                float d=Eye(uv);
                // Silhouettes: a Roberts cross on eye depth, with the threshold growing with distance
                // so a far roof is not scribbled over by its own tiles.
                float a=Eye(uv+px*float2( 1, 1)),b=Eye(uv+px*float2(-1,-1));
                float c=Eye(uv+px*float2( 1,-1)),e=Eye(uv+px*float2(-1, 1));
                float nearest=min(min(a,b),min(c,e));
                float jump=abs(a-b)+abs(c-e);
                float silhouette=smoothstep(.035*nearest+.025,.12*nearest+.09,jump);
                // Creases: where the normal turns sharply between neighbouring pixels.
                float3 n=SampleSceneNormals(uv);
                float turn=0;
                turn=max(turn,1-dot(n,SampleSceneNormals(uv+px*float2( 1,0))));
                turn=max(turn,1-dot(n,SampleSceneNormals(uv+px*float2(-1,0))));
                turn=max(turn,1-dot(n,SampleSceneNormals(uv+px*float2(0, 1))));
                turn=max(turn,1-dot(n,SampleSceneNormals(uv+px*float2(0,-1))));
                float crease=smoothstep(.30,.70,turn)*(1-silhouette);
                float mark=saturate(silhouette*_EdgeStrength+crease*_CreaseStrength);
                // Against the open sky the painter leaves the edge to the colour change.
                float sky=step(600,max(max(a,b),max(c,e)));
                mark*=lerp(1,.45,sky);
                // The line thins into the distance and dissolves with the haze.
                mark*=1-smoothstep(90,380,nearest);
                // Night ink is bluer and fainter, like a line under moonlight.
                half3 ink=lerp(_InkColor.rgb*half3(.55,.65,1.0),_InkColor.rgb,_KorikoDaylight);
                color=lerp(color,color*ink*2.1,mark);
                // Film grade.
                float lum=dot(color,half3(.2126,.7152,.0722));
                color=lerp(color,color*half3(1.045,1.0,.945),_Warmth*_KorikoDaylight);
                color=lerp(lum.xxx,color,1.07);
                color=color*.965+.018;
                return half4(color,1);
            }
            ENDHLSL
        }
    }
}
