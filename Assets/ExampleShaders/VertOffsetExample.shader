Shader "ExampleShaders/VertOffsets"
{
    Properties
    {
        _FloorHeight("Floor Height", Float) = 0.0
        [ToggleUI]_Stable("Stable", Float) = 0.0
        _FloorOffset("Floor Offset", Float) = 0.0
        _FloorBlendAmount("Floor Blend Amount", Float) = 0.1
        [ToggleUI]_shake("Shake", Float) = 0.0
        _shakeAmount("Shake Amount", Float) = 0.0075
        _shakeFrequency("Shake Frequency", Float) = 75.0
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" }

        Pass
        {
            HLSLPROGRAM

            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Assets/ExampleShaders/Shaders/AdditionalFunctions/StructureVertOffsets.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD1;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
            };

            CBUFFER_START(UnityPerMaterial)
                float _FloorHeight;
                float _FloorOffset;
                float _FloorBlendAmount;
                float _Stable;
                float _shake;
                float _shakeAmount;
                float _shakeFrequency;
            CBUFFER_END

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                float3 positionWS = TransformObjectToWorld(IN.positionOS);
                
                float3 offsetPositionOS = StructureVertOffsets(
                    positionWS,
                    _FloorHeight,
                    _Stable,
                    _FloorOffset,
                    _FloorBlendAmount,
                    _shake,
                    _shakeAmount,
                    _shakeFrequency
                    );
                
                OUT.positionHCS = TransformObjectToHClip(offsetPositionOS);
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                half4 color = half4(1, 1, 1, 1);
                return color;
            }
            ENDHLSL
        }
    }
}
