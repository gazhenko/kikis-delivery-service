Shader "Koriko/Painted"
{
    Properties
    {
        _BaseMap("Painted surface atlas", 2D) = "white" {}
        _Color("Color", Color) = (1,1,1,1)
        _AtlasRect("Atlas rectangle", Vector) = (0,0,1,1)
        _TextureWeight("Paint amount", Range(0,1)) = 1
        _ShadowTint("Painted shadow color", Color) = (.60,.67,.79,1)
        _Softness("Shadow softness", Range(.005,.3)) = .12
        _Wind("Leaf movement", Range(0,1)) = 0
        _Emission("Window warmth", Range(0,2)) = 0
        _LightTint("Sunlit paint", Color) = (1,.98,.87,1)
        _PaintScale("Paint scale", Float) = 1
        _ShadowStrength("Cast shadow paint", Range(0,1)) = .7
        _NormalFlatten("Canopy normal simplification", Range(0,1)) = 0
        _VertexPaint("Authored paint tones", Range(0,1)) = 1
        _Face("Face", Float) = 0
        _Rim("Accent", Float) = 0
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" }
        HLSLINCLUDE
        #include "FilmCommon.hlsl"
        ENDHLSL
        Pass
        {
            Name "PaintedForward"
            Tags { "LightMode"="UniversalForward" }
            Cull Back
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            #pragma multi_compile_fog
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);
            struct Attributes { float4 positionOS:POSITION; float3 normalOS:NORMAL; float2 uv:TEXCOORD0; float4 color:COLOR; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct Varyings { float4 positionCS:SV_POSITION; float3 normalWS:TEXCOORD0; float2 uv:TEXCOORD1; float3 positionWS:TEXCOORD2; float fog:TEXCOORD3; float3 paint:TEXCOORD4; UNITY_VERTEX_INPUT_INSTANCE_ID UNITY_VERTEX_OUTPUT_STEREO };
            Varyings Vert(Attributes input)
            {
                Varyings o; UNITY_SETUP_INSTANCE_ID(input); UNITY_TRANSFER_INSTANCE_ID(input,o); UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                float3 world=PaintedWorldPosition(input.positionOS.xyz);
                o.positionWS=world; o.positionCS=TransformWorldToHClip(world);
                o.normalWS=TransformObjectToWorldNormal(input.normalOS); o.uv=input.uv; o.fog=ComputeFogFactor(o.positionCS.z);
                o.paint=lerp(float3(1,1,1),input.color.rgb,_VertexPaint);
                return o;
            }
            half4 Frag(Varyings input):SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                float2 mirrored=1-abs(frac(input.uv*_PaintScale*.5)*2-1);
                float2 uv=_AtlasRect.xy+mirrored*_AtlasRect.zw;
                half3 paint=SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,uv).rgb;
                half3 albedo=lerp(_Color.rgb,paint,_TextureWeight)*input.paint;
                Light sun=GetMainLight(TransformWorldToShadowCoord(input.positionWS));
                float3 normal=normalize(lerp(input.normalWS,float3(0,1,0),_NormalFlatten));
                float light=dot(normal,sun.direction);
                // Broad paint marks modulate a shadow edge, rather than becoming surface noise.
                float stroke=dot(paint,half3(.2126,.7152,.0722))-.5;
                float shade=smoothstep(-.08-_Softness,.34+_Softness,light+stroke*.16);
                float cast=smoothstep(.25,.72,sun.shadowAttenuation);
                shade*=lerp(1,cast,_ShadowStrength);
                half3 coloredShade=lerp(_ShadowTint.rgb,_LightTint.rgb,shade);
                half3 lightColor=lerp(half3(.30,.41,.63),half3(1,1,1),_KorikoDaylight);
                half3 color=albedo*coloredShade*lightColor;
                float night=1-smoothstep(.15,.70,_KorikoDaylight);
                color=lerp(color,half3(.86,.56,.25),saturate(_Emission*night));
                return half4(MixFog(color,input.fog),1);
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
            #pragma fragment ShadowFrag
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"
            float3 _LightDirection;
            struct A { float4 positionOS:POSITION; float3 normalOS:NORMAL; UNITY_VERTEX_INPUT_INSTANCE_ID };
            float4 ShadowVert(A input):SV_POSITION
            {
                UNITY_SETUP_INSTANCE_ID(input);
                float3 pos=PaintedWorldPosition(input.positionOS.xyz);
                float3 norm=TransformObjectToWorldNormal(input.normalOS);
                float4 clip=TransformWorldToHClip(ApplyShadowBias(pos,norm,_LightDirection));
                return ApplyShadowClamping(clip);
            }
            half4 ShadowFrag():SV_Target { return 0; }
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
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            struct DepthAttributes { float4 positionOS:POSITION; UNITY_VERTEX_INPUT_INSTANCE_ID };
            float4 DepthVert(DepthAttributes input):SV_POSITION { UNITY_SETUP_INSTANCE_ID(input); return TransformWorldToHClip(PaintedWorldPosition(input.positionOS.xyz)); }
            half4 DepthFrag():SV_Target { return 0; }
            ENDHLSL
        }
    }
}
