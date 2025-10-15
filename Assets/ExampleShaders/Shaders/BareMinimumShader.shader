Shader "ExampleShaders/BareMinimum"
{
    Properties
    {
        _ColorAbove("Color Above Floor", Color) = (.85,.7,.55,1)
        _ColorBelow("Color Below Floor", Color) = (.33,.3,.27,1)
        _FloorHeight("Floor Height", Float) = 0.0
        _Stable("Grounded", Range(0,1)) = 1.0
        _ToonSteps("Toon Steps", Range(1,8)) = 3
        _ShadowTint("Shadow Tint", Color) = (0.7,0.7,0.7,1)
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" }
        Pass
        {
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS
            #pragma multi_compile_instancing
            #pragma multi_compile_fog
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT

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
                UNITY_VERTEX_OUTPUT_STEREO
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _ColorAbove;
                float4 _ColorBelow;
                float _FloorHeight;
                float _Stable;
                float _ToonSteps;
                float4 _ShadowTint;
            CBUFFER_END

            Varyings vert(Attributes IN)
            {
                Varyings OUT;

                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);

                float3 worldPos = TransformObjectToWorld(IN.positionOS.xyz);
                float3 worldNormal = TransformObjectToWorldNormal(IN.normalOS);

                OUT.worldPos = worldPos;
                OUT.worldNormal = normalize(worldNormal);

                // VR-correct projection
                OUT.positionHCS = TransformWorldToHClip(worldPos);
                return OUT;
            }

            // Floor mask helper
            float GetFloorMask(float3 worldPos, float offset)
            {
                float isAbove = step(_FloorHeight + offset, worldPos.y);
                return lerp(1.0 - _Stable, 1.0, isAbove);
            }

            float4 frag(Varyings IN) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(IN);
                // --- Lighting ---
                Light mainLight = GetMainLight();
                float NdotL = saturate(dot(IN.worldNormal, mainLight.direction));

                // Quantize light for toon shading
                float toonBrightness = floor(NdotL * _ToonSteps) / (_ToonSteps - 1);
                float3 litColor = lerp(_ShadowTint, 1.0, toonBrightness);

                // --- Apply color and mask ---
                float floorMask = GetFloorMask(IN.worldPos, 0.3);
                float4 groundedColor = lerp(_ColorBelow, _ColorAbove, floorMask);
                float3 finalColor = groundedColor * litColor * mainLight.color.rgb;

                return float4(finalColor, _ColorAbove.a);
            }
            ENDHLSL
        }
    }
}
