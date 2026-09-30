#ifndef KORIKO_CHARACTER_DEFORM
#define KORIKO_CHARACTER_DEFORM
// Shared by paint, ink, depth and shadow. Only the rider's shaders use this
// frame, so the town, camera and interface are never blurred or distorted.
float4x4 _KorikoRiderWorldToPose;
float4x4 _KorikoRiderPoseToWorld;
float4 _KorikoRiderSmear;
float4 _KorikoRiderLeftHip, _KorikoRiderLeftKnee, _KorikoRiderRightHip, _KorikoRiderRightKnee;
float4 _KorikoRiderLeftAnkle, _KorikoRiderRightAnkle;
float3 ClothOutsideLeg(float3 world,float4 hip,float4 knee)
{
    float3 axis=knee.xyz-hip.xyz;
    float3 closest=hip.xyz+axis*saturate(dot(world-hip.xyz,axis)/max(dot(axis,axis),.0001));
    float3 away=world-closest;float distance=length(away);
    return world+away/max(distance,.0001)*max(0,knee.w-distance)*hip.w;
}
float3 RiderWorldPosition(float3 positionOS,float cloth)
{
    float3 world=TransformObjectToWorld(positionOS);
    // Two analytic contacts keep the drawn hem outside the moving knees.
    // The same correction reaches its ink, fold lines, depth and shadow.
    if(cloth>.5)
    {
        world=ClothOutsideLeg(world,_KorikoRiderLeftHip,_KorikoRiderLeftKnee);
        world=ClothOutsideLeg(world,_KorikoRiderRightHip,_KorikoRiderRightKnee);
        // The film's smock reaches below the knee, so the shins keep it clear too.
        world=ClothOutsideLeg(world,float4(_KorikoRiderLeftKnee.xyz,_KorikoRiderLeftHip.w),_KorikoRiderLeftAnkle);
        world=ClothOutsideLeg(world,float4(_KorikoRiderRightKnee.xyz,_KorikoRiderRightHip.w),_KorikoRiderRightAnkle);
    }
    if(dot(abs(_KorikoRiderSmear),float4(1,1,1,1))<.0001)return world;
    float3 p=mul(_KorikoRiderWorldToPose,float4(world,1)).xyz;
    float tips=saturate(smoothstep(2.18,2.5,p.y)*.9+
        smoothstep(.24,.65,abs(p.x))*.55+smoothstep(.6,1.7,abs(p.z))*.8+
        (1-smoothstep(.35,1.1,p.y))*.55);
    // Keep the facial drawing registered even in an extreme turnaround.
    float face=smoothstep(1.68,1.80,p.y)*(1-smoothstep(2.12,2.24,p.y))*smoothstep(-.04,.15,p.z);
    float mask=(.12+tips*.88)*(1-face*.96);
    float3 sweep=float3((p.z+.15)*_KorikoRiderSmear.w,0,-p.x*_KorikoRiderSmear.w);
    p+=(_KorikoRiderSmear.xyz+sweep)*mask;
    return mul(_KorikoRiderPoseToWorld,float4(p,1)).xyz;
}
#ifdef KORIKO_FILM_COMMON
// Crows, gulls and other cel props share the character paint but not her pose.
// Without this, a turn smear sweeps distant birds by metres for two drawings.
float3 CelWorldPosition(float3 positionOS)
{
    return _Detached>.5?TransformObjectToWorld(positionOS):RiderWorldPosition(positionOS,_Cloth);
}
#endif
#endif
