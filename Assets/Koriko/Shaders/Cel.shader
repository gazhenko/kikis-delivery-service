Shader "Koriko/CharacterCel"
{
    Properties
    {
        _Color("Lit cel paint", Color)=(.25,.29,.43,1)
        _ShadowTint("Shadow cel paint", Color)=(.13,.17,.29,1)
        _LightTint("Light accent", Color)=(.39,.43,.57,1)
        _Softness("Cel edge antialias", Range(.001,.05))=.006
        _Face("Face normal simplification", Range(0,1))=0
        _Rim("Restrained edge accent", Range(0,1))=.1
        _Cloth("Rider cloth contact", Range(0,1))=0
        _BaseMap("Paint atlas", 2D)="white"{}
        _AtlasRect("Atlas cell", Vector)=(0,0,1,1)
        _TextureWeight("Paint amount", Range(0,1))=0
        _Wind("Movement", Range(0,1))=0
        _Emission("Emission", Range(0,2))=0
        _PaintScale("Paint scale", Float)=1
        _ShadowStrength("Cast shadow", Range(0,1))=0
        _NormalFlatten("Normal flatten", Range(0,1))=0
        _VertexPaint("Vertex paint", Range(0,1))=0
        _Detached("Town prop outside the rider pose", Float)=0
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" }
        Pass
        {
            Name "CharacterCel"
            Tags { "LightMode"="UniversalForward" }
            Cull Back
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_fog
            #pragma multi_compile_instancing
            #include "FilmCommon.hlsl"
            #include "CharacterDeform.hlsl"
            struct A { float4 p:POSITION; float3 n:NORMAL; float4 color:COLOR; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct V { float4 p:SV_POSITION; float3 n:TEXCOORD0; float3 world:TEXCOORD1; float fog:TEXCOORD2; UNITY_VERTEX_INPUT_INSTANCE_ID UNITY_VERTEX_OUTPUT_STEREO };
            V Vert(A a)
            {
                V o;UNITY_SETUP_INSTANCE_ID(a);UNITY_TRANSFER_INSTANCE_ID(a,o);UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.world=CelWorldPosition(a.p.xyz);o.p=TransformWorldToHClip(o.world);
                o.n=TransformObjectToWorldNormal(a.n);o.fog=ComputeFogFactor(o.p.z);return o;
            }
            half4 Frag(V i):SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(i);
                float3 n=normalize(i.n);
                float3 view=GetWorldSpaceNormalizeViewDir(i.world);
                // Face planes are authored in the model; reduce nose/cheek chatter further.
                n=normalize(lerp(n,normalize(n+view*.65),_Face));
                float light=dot(n,normalize(_KorikoCelLightDirection));
                float edge=max(fwidth(light)*.75,_Softness);
                float lit=smoothstep(.12-edge,.12+edge,light);
                half3 color=lerp(_ShadowTint.rgb,_Color.rgb,lit);
                float accent=smoothstep(.87-edge,.87+edge,light)*_Rim;
                color=lerp(color,_LightTint.rgb,accent);
                // A cel remains legible in the blue night palette, like a painted foreground layer.
                color*=lerp(half3(.52,.62,.85),half3(1,1,1),_KorikoDaylight);
                return half4(MixFog(color,i.fog),1);
            }
            ENDHLSL
        }
        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode"="ShadowCaster" }
            ZWrite On ZTest LEqual ColorMask 0 Cull Back
            HLSLPROGRAM
            #pragma vertex ShadowVert
            #pragma fragment DepthFrag
            #pragma multi_compile_instancing
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW
            #include "FilmCommon.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"
            #include "CharacterDeform.hlsl"
            float3 _LightDirection;
            float3 _LightPosition;
            struct A { float4 p:POSITION;float3 n:NORMAL;UNITY_VERTEX_INPUT_INSTANCE_ID };
            float4 ShadowVert(A a):SV_POSITION
            {
                UNITY_SETUP_INSTANCE_ID(a);
                float3 world=CelWorldPosition(a.p.xyz),normal=TransformObjectToWorldNormal(a.n);
                #if defined(_CASTING_PUNCTUAL_LIGHT_SHADOW)
                    float3 light=normalize(_LightPosition-world);
                #else
                    float3 light=_LightDirection;
                #endif
                float4 clip=TransformWorldToHClip(ApplyShadowBias(world,normal,light));
                #if UNITY_REVERSED_Z
                    clip.z=min(clip.z,UNITY_NEAR_CLIP_VALUE*clip.w);
                #else
                    clip.z=max(clip.z,UNITY_NEAR_CLIP_VALUE*clip.w);
                #endif
                return clip;
            }
            half4 DepthFrag():SV_Target{return 0;}
            ENDHLSL
        }
        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode"="DepthOnly" }
            ZWrite On ColorMask R Cull Back
            HLSLPROGRAM
            #pragma vertex DepthVert
            #pragma fragment DepthFrag
            #pragma multi_compile_instancing
            #include "FilmCommon.hlsl"
            #include "CharacterDeform.hlsl"
            struct A { float4 p:POSITION;UNITY_VERTEX_INPUT_INSTANCE_ID };
            float4 DepthVert(A a):SV_POSITION{UNITY_SETUP_INSTANCE_ID(a);return TransformWorldToHClip(CelWorldPosition(a.p.xyz));}
            half4 DepthFrag():SV_Target{return 0;}
            ENDHLSL
        }
    }
}
