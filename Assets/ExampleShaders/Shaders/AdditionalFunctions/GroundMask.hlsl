void GroundMask_float(float3 worldPos, float floorHeight, float stable, float offset, out float mask)
{
    float isAbove = step(floorHeight + offset, worldPos.y);
    mask = lerp(1.0 - stable, 1.0, isAbove);
}

void GroundMaskGradient_float(float3 worldPos, float floorHeight, float stable, float offset, out float mask)
{
    float samplePoint =  clamp(worldPos.y - (floorHeight + offset), 0.0, 1.0);
    mask = lerp(1.0 - stable, 1.0, samplePoint);
}

