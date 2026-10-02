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
        [Enum(UnityEngine.Rendering.CullMode)] _Cull("Faces drawn", Float) = 2
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
            Cull [_Cull]
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
            struct Varyings { float4 positionCS:SV_POSITION; float3 normalWS:TEXCOORD0; float2 uv:TEXCOORD1; float3 positionWS:TEXCOORD2; float fog:TEXCOORD3; float4 paint:TEXCOORD4; UNITY_VERTEX_INPUT_INSTANCE_ID UNITY_VERTEX_OUTPUT_STEREO };
            Varyings Vert(Attributes input)
            {
                Varyings o; UNITY_SETUP_INSTANCE_ID(input); UNITY_TRANSFER_INSTANCE_ID(input,o); UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                float3 world=PaintedWorldPosition(input.positionOS.xyz);
                o.positionWS=world; o.positionCS=TransformWorldToHClip(world);
                o.normalWS=TransformObjectToWorldNormal(input.normalOS); o.uv=input.uv; o.fog=ComputeFogFactor(o.positionCS.z);
                // Alpha carries a per-window household value: when its lamp is lit and how warmly.
                o.paint=float4(lerp(float3(1,1,1),input.color.rgb,_VertexPaint),input.color.a);
                return o;
            }
            half4 Frag(Varyings input,FRONT_FACE_TYPE face:FRONT_FACE_SEMANTIC):SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                float2 mirrored=1-abs(frac(input.uv*_PaintScale*.5)*2-1);
                float2 uv=_AtlasRect.xy+mirrored*_AtlasRect.zw;
                half3 paint=SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,uv).rgb;
                half3 albedo=lerp(_Color.rgb,paint,_TextureWeight)*input.paint.rgb;
                Light sun=GetMainLight(TransformWorldToShadowCoord(input.positionWS));
                // Roof planes are drawn from both sides: an eave seen from the street shades as an underside.
                float3 normal=normalize(lerp(input.normalWS*IS_FRONT_VFACE(face,1,-1),float3(0,1,0),_NormalFlatten));
                float light=dot(normal,sun.direction);
                // Painted bands, as a background painter lays them: sunlit, half-tone and shadow,
                // each a flat wash. The brush marks of the atlas push the band edge about so the
                // boundary wobbles like a stroke instead of following the geometry exactly.
                float stroke=dot(paint,half3(.2126,.7152,.0722))-.5;
                float l=light+stroke*.26;
                float w=max(fwidth(l)*1.5,.02+_Softness*.6);
                float sunlit=smoothstep(.32-w,.32+w,l);
                float halfTone=smoothstep(-.14-w,-.14+w,l);
                float shade=halfTone*.42+sunlit*.58;
                // Cast shadows are hard-edged painted shapes.
                float cast=smoothstep(.42,.58,sun.shadowAttenuation);
                shade*=lerp(1,cast,_ShadowStrength);
                // Shadowed paint takes the sky's blue; ground and roofs in shadow are the coolest.
                half3 shadowTint=_ShadowTint.rgb*lerp(half3(1,1,1),half3(.95,1.0,1.09),saturate(normal.y))+saturate(normal.y)*.025;
                half3 coloredShade=lerp(shadowTint,_LightTint.rgb,shade);
                half3 lightColor=lerp(half3(.30,.41,.63),half3(1,1,1),_KorikoDaylight);
                half3 color=albedo*coloredShade*lightColor;
                float night=1-smoothstep(.15,.70,_KorikoDaylight);
                // Households light their lamps at slightly different moments; a few stay dark.
                float household=input.paint.a;
                float evening=frac(household*7.13)*.45;
                float lamp=smoothstep(evening,evening+.12,night)*smoothstep(.12,.3,household);
                half3 warm=lerp(half3(.64,.37,.19),half3(.88,.58,.26),household);
                color=lerp(color,warm,saturate(_Emission*lamp));
                return half4(MixFog(color,input.fog),1);
            }
            ENDHLSL
        }
        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode"="ShadowCaster" }
            ZWrite On ZTest LEqual ColorMask 0 Cull [_Cull]
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
            Name "DepthNormals"
            Tags { "LightMode"="DepthNormals" }
            ZWrite On Cull [_Cull]
            HLSLPROGRAM
            #pragma vertex NormalsVert
            #pragma fragment NormalsFrag
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            struct NormalsAttributes { float4 positionOS:POSITION; float3 normalOS:NORMAL; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct NormalsVaryings { float4 positionCS:SV_POSITION; float3 normalWS:TEXCOORD0; UNITY_VERTEX_INPUT_INSTANCE_ID };
            NormalsVaryings NormalsVert(NormalsAttributes input)
            {
                NormalsVaryings o; UNITY_SETUP_INSTANCE_ID(input); UNITY_TRANSFER_INSTANCE_ID(input,o);
                o.positionCS=TransformWorldToHClip(PaintedWorldPosition(input.positionOS.xyz));
                o.normalWS=TransformObjectToWorldNormal(input.normalOS); return o;
            }
            half4 NormalsFrag(NormalsVaryings input,FRONT_FACE_TYPE face:FRONT_FACE_SEMANTIC):SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                return half4(normalize(input.normalWS)*IS_FRONT_VFACE(face,1,-1),0);
            }
            ENDHLSL
        }
        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode"="DepthOnly" }
            ZWrite On ColorMask R Cull [_Cull]
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
