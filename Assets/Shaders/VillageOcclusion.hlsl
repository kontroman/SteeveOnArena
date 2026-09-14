#ifndef MINEARENA_VILLAGE_OCCLUSION
#define MINEARENA_VILLAGE_OCCLUSION
// Set per runtime material, immediately before each camera renders.
float4 _VillageTarget;
float4 _VillageCutout; // radius, feather, depth margin, minimum world height

void VillageOcclusionClip(float4 positionCS)
{
    if (_VillageTarget.w < 0.5) return;
    float4 targetCS = TransformWorldToHClip(_VillageTarget.xyz);
    float targetDepth = -TransformWorldToView(_VillageTarget.xyz).z;
    if (targetDepth <= 0.0 || targetCS.w <= 0.0) return;

    float2 uv = positionCS.xy / _ScaledScreenParams.xy;
    float depth = positionCS.z;
#if !UNITY_REVERSED_Z
    depth = lerp(UNITY_NEAR_CLIP_VALUE, 1.0, depth);
#endif
    float3 worldPos = ComputeWorldSpacePosition(uv, depth, UNITY_MATRIX_I_VP);
    float fragmentDepth = -TransformWorldToView(worldPos).z;
    if (fragmentDepth >= targetDepth - _VillageCutout.z || worldPos.y <= _VillageCutout.w) return;

    float4 targetScreen = ComputeScreenPos(targetCS);
    float2 delta = uv - targetScreen.xy / targetScreen.w;
    delta.x *= _ScaledScreenParams.x / _ScaledScreenParams.y;
    float worldDistance = length(delta) * 2.0 * targetCS.w / abs(UNITY_MATRIX_P._m11);
    float visibility = smoothstep(max(0.0, _VillageCutout.x - _VillageCutout.y), _VillageCutout.x, worldDistance);
    // Stable screen-space 4x4 Bayer pattern: opaque depth writes, no alpha sorting.
    uint2 cell = (uint2)positionCS.xy & 3u;
    uint threshold = ((cell.x & 1u) ^ (cell.y & 1u)) * 8u + (cell.y & 1u) * 4u
        + (((cell.x >> 1u) & 1u) ^ ((cell.y >> 1u) & 1u)) * 2u + ((cell.y >> 1u) & 1u);
    clip(visibility - (threshold + 0.5) / 16.0);
}
#endif
