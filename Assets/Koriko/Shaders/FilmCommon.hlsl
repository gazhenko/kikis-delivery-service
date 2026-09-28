#ifndef KORIKO_FILM_COMMON
#define KORIKO_FILM_COMMON
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
// One layout across forward, shadow and depth passes preserves SRP batching.
CBUFFER_START(UnityPerMaterial)
    float4 _BaseMap_ST, _Color, _AtlasRect, _ShadowTint, _LightTint;
    float _TextureWeight, _Softness, _Wind, _Emission;
    float _PaintScale, _ShadowStrength, _NormalFlatten, _VertexPaint;
    float _Face, _Rim, _Cloth, _Reserved1;
CBUFFER_END
float _KorikoDaylight;
float4 _KorikoHorizonColor;
float3 _KorikoCelLightDirection;
float3 PaintedWorldPosition(float3 positionOS)
{
    float3 world=TransformObjectToWorld(positionOS);
    float t=floor(_Time.y*12.0)/12.0;
    // Small, slow movement in canopy masses; no animated noise on the painting.
    world.x+=sin(world.z*.31+t*.85)*sin(world.y*.55+t*.6)*.10*_Wind;
    return world;
}
#endif
