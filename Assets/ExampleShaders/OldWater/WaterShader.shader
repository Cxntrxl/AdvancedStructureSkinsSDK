Shader "ExampleShaders/Water"
{
    Properties
    {
        _TopColor("Top Sky Blue", Color) = (0.5,0.8,1,1)
        _BottomColor("Bottom Dark Blue", Color) = (0.02,0.06,0.4,1)
        _RippleColor("Ripple Color", Color) = (1,1,1,1)
        _LineColor("Side Line Color", Color) = (1,1,1,1)
        _VoronoiColor("Voronoi Edge Color", Color) = (0.7,0.9,1,1)

        _FloorHeight("Floor Height", Float) = 0.0
        [ToggleUI]_Stable("Stable (0..1)", Float) = 1.0
        _HeightScale("Height Scale", Float) = 1.0

        _RippleSpeed("Ripple Speed", Float) = 0.8
        _RippleFreq("Ripple Frequency", Float) = 6.0
        _RippleThreshold("Ripple Threshold", Range(0,1)) = 0.5

        _SideWaveFreq("Side Line Frequency", Float) = 3.0
        _SideWaveSpeed("Side Line Speed", Float) = 1.0
        _SideWaveThreshold("Side Line Threshold", Range(0,1)) = 0.6

        _VoronoiScale("Voronoi Scale", Float) = 3.0
        _VoronoiSpeed("Voronoi Scroll Speed", Float) = 0.4
        _VoronoiContrast("Voronoi Contrast", Float) = 3.0
        _VoronoiThreshold("Voronoi Threshold", Range(0,1)) = 0.5

        _OverallIntensity("Overall Intensity", Float) = 1.0
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" "RenderPipeline"="UniversalPipeline" }
        Pass
        {
            Name "Forward"
            Tags { "LightMode"="UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #pragma multi_compile _ UNITY_SINGLE_PASS_STEREO STEREO_INSTANCING_ON STEREO_MULTIVIEW_ON
            #pragma target 4.5

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float3 worldPos : TEXCOORD0;
                float3 worldNormal : TEXCOORD1;
                float3 objectPos : TEXCOORD2;
                float3 worldObjectOrigin : TEXCOORD3;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _TopColor, _BottomColor, _RippleColor, _LineColor, _VoronoiColor;
                float _FloorHeight, _Stable, _HeightScale;
                float _RippleSpeed, _RippleFreq, _RippleThreshold;
                float _SideWaveFreq, _SideWaveSpeed, _SideWaveThreshold;
                float _VoronoiScale, _VoronoiSpeed, _VoronoiContrast, _VoronoiThreshold;
                float _OverallIntensity;
            CBUFFER_END

            float2 hash22(float2 p)
            {
                float3 p3 = frac(float3(p.xyx) * 0.1031);
                p3 += dot(p3, p3.yzx + 33.33);
                return frac((p3.xx + p3.yz) * p3.zy);
            }

            float voronoiEdge(float2 uv)
            {
                float2 iuv = floor(uv);
                float2 fuv = frac(uv);
                float minDist = 1e9, second = 1e9;
                for (int y = -1; y <= 1; y++)
                for (int x = -1; x <= 1; x++)
                {
                    float2 b = float2(x, y);
                    float2 r = hash22(iuv + b);
                    float2 diff = b + r - fuv;
                    float d = dot(diff, diff);
                    if (d < minDist) { second = minDist; minDist = d; }
                    else if (d < second) second = d;
                }
                return saturate((sqrt(second) - sqrt(minDist)) * 2.0);
            }

            Varyings vert(Attributes IN)
            {
                Varyings OUT;

    UNITY_SETUP_INSTANCE_ID(IN);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);

    float3 worldPos = TransformObjectToWorld(IN.positionOS.xyz);
    float3 worldNormal = TransformObjectToWorldNormal(IN.normalOS);

    OUT.worldPos = worldPos;
    OUT.worldNormal = normalize(worldNormal);
    OUT.objectPos = IN.positionOS.xyz;

    // new: also store the object’s world origin
    OUT.worldObjectOrigin = TransformObjectToWorld(float3(0, 0, 0));

    OUT.positionHCS = TransformWorldToHClip(worldPos);
    return OUT;
            }

float TopRipples(float3 worldPos, float3 worldObjectOrigin, float time)
{
    // Compute distance from the object's origin, projected on the world XZ plane
    float2 pos = worldPos.xz - worldObjectOrigin.xz;
    float dist = length(pos);
    float ripple = sin(dist * _RippleFreq - time * _RippleSpeed);
    return step(_RippleThreshold, ripple);
}

            float SideCascades(float3 worldPos, float3 normal, float time)
            {
                float upDot = abs(dot(normal, float3(0,1,0)));
                float sideMask = smoothstep(0.0, 0.4, 1.0 - upDot);
                float2 uv = worldPos.zy; // vertical alignment
                float lines = frac(uv.y * _SideWaveFreq + time * _SideWaveSpeed);
                float lineMask = step(_SideWaveThreshold, abs(lines - 0.5));
                return lineMask * sideMask;
            }

            float CrashWaves(float3 worldPos, float time)
            {
                float heightFromFloor = worldPos.y - _FloorHeight;
                float wave = sin(heightFromFloor * 30.0 - time * 6.0);
                float fade = smoothstep(0.0, 0.25, heightFromFloor);
                return (1.0 - fade) * saturate(wave) * _Stable;
            }

            float3 GradientColor(float height)
            {
                float t = saturate(height / max(_HeightScale, 1e-3));
                return lerp(_BottomColor.rgb, _TopColor.rgb, t);
            }

            float3 VoronoiOverlay(float3 worldPos, float time)
            {
                float2 uv = worldPos.xz * _VoronoiScale + time * _VoronoiSpeed;
                float edge = voronoiEdge(uv);
                edge = 1.0 - pow(edge, _VoronoiContrast); // inverted
                float mask = step(_VoronoiThreshold, edge);
                return _VoronoiColor.rgb * mask;
            }

float3 TriplanarVoronoi(float3 worldPos, float3 worldNormal, float time)
{
    float3 blending = pow(abs(worldNormal), 4.0);
    blending /= (blending.x + blending.y + blending.z + 1e-5);

    float edgeX = voronoiEdge(worldPos.zy * _VoronoiScale + time * _VoronoiSpeed);
    float edgeY = voronoiEdge(worldPos.xz * _VoronoiScale + time * _VoronoiSpeed);
    float edgeZ = voronoiEdge(worldPos.xy * _VoronoiScale + time * _VoronoiSpeed);

    float edge = edgeX * blending.x + edgeY * blending.y + edgeZ * blending.z;
    edge = 1.0 - pow(edge, _VoronoiContrast);
    float mask = step(_VoronoiThreshold, edge);
    return _VoronoiColor.rgb * mask;
}
            
            half4 frag(Varyings IN) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(IN);
                float time = _Time.y;

// Ripple fade near edges
float topMask = saturate(dot(normalize(IN.worldNormal), float3(0,1,0)));
float rippleFade = smoothstep(0.0, 0.3, topMask);

float3 grad = GradientColor(IN.worldPos.y);
float ripples = TopRipples(IN.worldPos, IN.worldObjectOrigin, time) * rippleFade;
float cascades = SideCascades(IN.worldPos, IN.worldNormal, time);
float crash = CrashWaves(IN.worldPos, time);
float3 voronoi = TriplanarVoronoi(IN.worldPos, IN.worldNormal, time);

float3 color = grad
    + _RippleColor.rgb * ripples
    + _LineColor.rgb * cascades
    + _RippleColor.rgb * crash
    + voronoi;

return float4(saturate(color * _OverallIntensity), 1.0);

            }
            ENDHLSL
        }
    }
}
