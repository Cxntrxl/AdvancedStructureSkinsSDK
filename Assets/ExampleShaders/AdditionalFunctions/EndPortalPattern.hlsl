float RandomFromVector(float2 pos)
{
    uint ux = asuint(pos.x);
    uint uy = asuint(pos.y);
    
    uint h = ux;
    h += 0x9e3779b9u + (uy << 6) + (uy >> 2);
    h ^= h >> 16;
    h *= 0x85ebca6bu;
    h ^= h >> 13;
    h *= 0xc2b2ae35u;
    h ^= h >> 16;
    
    return (float)(h & 0x00ffffffu) / 16777216.0;
}

float2x2 rotate2d(float angle)
{
    return float2x2(
        sin(angle), -cos(angle),
        cos(angle), sin(angle)
        );
}

void EndPortalPattern_float(float time, float2 pos, float size, float frequency, float angle, float4 color, out float4 pattern)
{
    pos = mul(rotate2d(angle), pos);
    float one = floor(RandomFromVector(floor(pos * size + float2(time, 0.0))) + frequency);
    float two = .4 * floor(RandomFromVector(floor(pos * size + float2(-1 + time, 0.0))) + frequency);
    float three = .2 * floor(RandomFromVector(floor(pos * size + float2(-2 + time, 0.0))) + frequency);
    float four = .1 * floor(RandomFromVector(floor(pos * size + float2(time, -1.0))) + frequency);
    float five = .1 * floor(RandomFromVector(floor(pos * size + float2(time, 1.0))) + frequency);
    float six = .1 * floor(RandomFromVector(floor(pos * size + float2(1 + time, 0.0))) + frequency);

    float final = one + two + three + four + five + six;
    pattern = float4(final, final, final, 1) * color * (1.0 - 0.5 * RandomFromVector(floor(pos * size + float2(-1 + time, 0.0))));
}

