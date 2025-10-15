#include <UnityShaderVariables.cginc>

float3 RGBtoHSV(float3 c)
{
    float4 K = float4(0., -1./3., 2./3., -1.);
    float4 p = lerp(float4(c.bg, K.wz), float4(c.gb, K.xy), step(c.b, c.g));
    float4 q = lerp(float4(p.xyw, c.r), float4(c.r, p.yzx), step(p.x, c.r));

    float d = q.x - min(q.w, q.y);
    float e = 1e-10;
    return float3(abs(q.z + (q.w - q.y) / (6. * d + e)), d / (q.x + e), q.x);
}

float3 HSVtoRGB(float3 c)
{
    float3 rgb = clamp(abs(frac(c.x + float3(0, 1./3., 2./3.)) * 6. - 3.) - 1., 0., 1.);
    return c.z * lerp(float3(1,1,1), rgb, c.y);
}

void ProjectAndBlendHSV_float(
    float4 baseColor, 
    float4 overlayColor, 
    float3 worldDir, 
    float distance,
    out float4 output)
{
    float3 parallaxedDir = normalize(worldDir - (_WorldSpaceCameraPos / distance));
    
    float3 hsvBase = RGBtoHSV(baseColor.rgb);
    float3 hsvOverlay = RGBtoHSV(overlayColor.rgb);
    
    float blend = saturate(hsvOverlay.z);
    float3 hsvResult = lerp(hsvBase, hsvOverlay, blend);
    
    float3 rgbResult = HSVtoRGB(hsvResult);
    
    float alpha = lerp(baseColor.a, overlayColor.a, blend);

    output = float4(rgbResult, alpha);
}
