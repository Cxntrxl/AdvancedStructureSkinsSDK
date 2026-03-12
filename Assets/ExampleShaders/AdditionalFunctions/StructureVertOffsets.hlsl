#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderVariablesFunctions.hlsl"

float4 remap(float4 In, float2 InMinMax, float2 OutMinMax)
{
    return OutMinMax.x + (In - InMinMax.x) * (OutMinMax.y - OutMinMax.x) / (InMinMax.y - InMinMax.x);
}

float sinWave(float magnitude, float frequency, float time)
{
    return magnitude * sin(2 * 3.14159 * frequency * time);
}

float3 StructureVertOffsets(float3 worldPos, float floorHeight, float stable, float offset, float floorBlendAmount, float shake, float shakeAmount, float shakeFrequency)
{
    float3 sinPos = {
        sinWave(shakeAmount, shakeFrequency, _Time),
        sinWave(shakeAmount, shakeFrequency, _Time * 0.3),
        sinWave(shakeAmount, shakeFrequency, _Time * 0.7)
    };

    float3 shakePos = worldPos + (sinPos * shake * 0.5);
    
    float samplePoint =  clamp(shakePos.y - (floorHeight + offset), 0.0, 1.0);
    float mask = lerp(1.0 - stable, 1.0, samplePoint);
    float remapedMask = remap(mask, float2(0.01, 0.5), float2(floorBlendAmount, -0.16));
    float clampedMask = clamp(remapedMask, 0.0, 1.0);
    float newY = shakePos.y - clampedMask;
    float3 groundedPos = float3(shakePos.x, newY, shakePos.z);

    return TransformWorldToObject(groundedPos);
}

void StructureVertOffsets_float(float3 worldPos, float floorHeight, float stable, float offset, float floorBlendAmount, float shake, float shakeAmount, float shakeFrequency, out float3 vertOffsets)
{
    vertOffsets = StructureVertOffsets(worldPos, floorHeight, stable, offset, floorBlendAmount, shake, shakeAmount, shakeFrequency);
}

