Shader "ExampleShaders/Crystal" {
    Properties {
        Texture2D_3812B1EC("Albedo", 2D) = "white" {}
        Texture2D_2058E65A("Normal", 2D) = "bump" {}
        Texture2D_8F187FEF("Material Mask", 2D) = "black" {}
        _GroundedTint("Grounded Tint", Color) = (1,1,1,1)
        _GroundedContrast("Grounded Contrast", Range(0,2)) = 1
        _Smoothness("Smoothness", Range(0,1)) = 0.4
        _EmissionStrength("Emission Strength", Range(0,5)) = 1.0
        _ParallaxDepth("Parallax Depth", Range(0,0.1)) = 0.02
        _InnerLayerStrength("Inner Layer Strength", Range(0,1)) = 0.5
        _InnerLayerFresnel("Inner Layer Fresnel Strength", Range(0,1)) = 0.5
        _FloorHeight("Floor Height", Float) = 0.0
        _FloorOffset("Floor Offset", Range(-1, 1)) = 0
        [ToggleUI]_Stable("Stable", Range(0,1)) = 0.0
    }

    SubShader {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" }
        LOD 200

        Pass {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Assets/ExampleShaders/Shaders/AdditionalFunctions/GroundMask.hlsl"

            struct Attributes {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float4 tangentOS  : TANGENT;
                float2 uv         : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS   : TEXCOORD1;
                float4 tangentWS  : TEXCOORD2;
                float2 uv         : TEXCOORD3;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            sampler2D Texture2D_3812B1EC;
            sampler2D Texture2D_2058E65A;
            sampler2D Texture2D_8F187FEF;

            CBUFFER_START(UnityPerMaterial)
                float4 Texture2D_3812B1EC_ST;
                float4 _GroundedTint;
                float _GroundedContrast;
                float _Smoothness;
                float _EmissionStrength;
                float _ParallaxDepth;
                float _InnerLayerStrength;
                float _InnerLayerFresnel;
                float _FloorHeight;
                float _FloorOffset;
                float _Stable;
            CBUFFER_END

            Varyings vert (Attributes IN) {
                Varyings OUT;

                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);
                
                OUT.positionWS = TransformObjectToWorld(IN.positionOS.xyz);
                OUT.positionCS = TransformWorldToHClip(OUT.positionWS);
                OUT.normalWS = TransformObjectToWorldNormal(IN.normalOS);
                OUT.tangentWS = float4(TransformObjectToWorldDir(IN.tangentOS.xyz), IN.tangentOS.w);
                OUT.uv = TRANSFORM_TEX(IN.uv, Texture2D_3812B1EC);
                return OUT;
            }

            float3 UnpackNormalMap(sampler2D normalMap, float2 uv) {
                float3 normal = UnpackNormal(tex2D(normalMap, uv));
                return normalize(normal);
            }

            float3x3 CreateTBN(float3 normalWS, float4 tangentWS) {
                float3 tangent = normalize(tangentWS.xyz);
                float3 bitangent = cross(normalWS, tangent) * tangentWS.w;
                return float3x3(tangent, bitangent, normalWS);
            }

            float4 frag(Varyings IN) : SV_Target {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(IN);
                
                float2 uv = IN.uv;
                float3 normalWS = normalize(IN.normalWS);
                float3 viewDirWS = normalize(GetCameraPositionWS() - IN.positionWS);
                float4 mask = tex2D(Texture2D_8F187FEF, uv);
                
                float3x3 TBN = CreateTBN(normalWS, IN.tangentWS);
                float3 viewDirTS = mul(transpose(TBN), viewDirWS);
                
                float parallax = (1 - saturate(viewDirTS.z)) * _ParallaxDepth * mask.g;
                float2 parallaxUV = uv - viewDirTS.xy * parallax;

                float4 baseCol = tex2D(Texture2D_3812B1EC, parallaxUV);
                
                float groundMask = 1.0;
                GroundMaskGradient_float(IN.positionWS, _FloorHeight, _Stable, _FloorOffset, groundMask);
                float4 groundedColor = lerp(_GroundedTint, baseCol, pow(groundMask, _GroundedContrast));
                
                mask = tex2D(Texture2D_8F187FEF, parallaxUV);
                normalWS = normalize(mul(UnpackNormalMap(Texture2D_2058E65A, parallaxUV), TBN));
                
                Light mainLight = GetMainLight();
                float3 lightDir = normalize(mainLight.direction);
                float NdotL = saturate(dot(normalWS, lightDir));
                float3 diffuse = groundedColor.rgb * mainLight.color * NdotL;
                
                float3 halfDir = normalize(lightDir + viewDirWS);
                float spec = pow(saturate(dot(normalWS, halfDir)), lerp(8, 128, _Smoothness));
                float3 specular = spec * mainLight.color * 0.5;
                
                float3 emission = groundedColor.rgb * mask.g * _EmissionStrength;
                
                float innerParallax = _ParallaxDepth * 2.0; 
                float2 innerUV = uv - viewDirTS.xy * innerParallax;
                float3 innerCol = tex2D(Texture2D_3812B1EC, innerUV).rgb;

                float3 innerGrounded = lerp(_GroundedTint * 0.6, innerCol, pow(groundMask, _GroundedContrast));
                
                float fresnel = pow(1.0 - saturate(dot(normalWS, viewDirWS)), 2.0);
                float innerBlend = lerp(_InnerLayerStrength, _InnerLayerFresnel, fresnel);
                
                float3 finalColor = diffuse + specular + emission + innerGrounded * innerBlend;

                return float4(finalColor, 1.0);
            }
            ENDHLSL
        }
    }
}