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
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" }
        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        CBUFFER_START(UnityPerMaterial)
            float4 _BaseMap_ST, _Color, _AtlasRect, _ShadowTint;
            float _TextureWeight, _Softness, _Wind, _Emission;
        CBUFFER_END
        float3 PaintedWorldPosition(float3 positionOS)
        {
            float3 world=TransformObjectToWorld(positionOS);
            world.x+=sin(world.z*.45+_Time.y*1.2)*sin(world.y*.8+_Time.y)*.065*_Wind;
            return world;
        }
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
            float _KorikoDaylight;
            struct Attributes { float4 positionOS:POSITION; float3 normalOS:NORMAL; float2 uv:TEXCOORD0; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct Varyings { float4 positionCS:SV_POSITION; float3 normalWS:TEXCOORD0; float2 uv:TEXCOORD1; float3 positionWS:TEXCOORD2; float fog:TEXCOORD3; UNITY_VERTEX_INPUT_INSTANCE_ID UNITY_VERTEX_OUTPUT_STEREO };
            Varyings Vert(Attributes input)
            {
                Varyings o; UNITY_SETUP_INSTANCE_ID(input); UNITY_TRANSFER_INSTANCE_ID(input,o); UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                float3 world=PaintedWorldPosition(input.positionOS.xyz);
                o.positionWS=world; o.positionCS=TransformWorldToHClip(world);
                o.normalWS=TransformObjectToWorldNormal(input.normalOS); o.uv=input.uv; o.fog=ComputeFogFactor(o.positionCS.z);
                return o;
            }
            half4 Frag(Varyings input):SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                float2 mirrored=1-abs(frac(input.uv*.5)*2-1);
                float2 uv=_AtlasRect.xy+mirrored*_AtlasRect.zw;
                half3 paint=SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,uv).rgb;
                half3 albedo=lerp(_Color.rgb,paint,_TextureWeight);
                Light sun=GetMainLight(TransformWorldToShadowCoord(input.positionWS));
                half n=dot(normalize(input.normalWS),sun.direction)*.5+.5;
                half shade=lerp(.12,.62,smoothstep(.35-_Softness,.35+_Softness,n));
                shade=lerp(shade,1,smoothstep(.72-_Softness,.72+_Softness,n));
                shade=lerp(.12,shade,sun.shadowAttenuation);
                half3 coloredShade=lerp(_ShadowTint.rgb,half3(1.06,1.01,.91),shade);
                half3 lightColor=lerp(half3(.26,.32,.52),half3(1,1,1),_KorikoDaylight);
                half3 color=albedo*coloredShade*lightColor;
                color+=_Emission*half3(1,.65,.28)*(1-_KorikoDaylight);
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
