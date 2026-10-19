Shader "Court/FlatToon"
{
    Properties
    {
        _BaseColor ("Color base", Color) = (1, 1, 1, 1)
        [Toggle(_VERTEX_COLOR)] _UseVertexColor ("Multiplicar por vertex color", Float) = 0

        [Header(Luz)]
        _LightThreshold ("Umbral luz/sombra (N·L)", Range(-1, 1)) = 0
        _LightSoftness ("Suavidad del corte", Range(0.001, 0.5)) = 0.06
        _LightInfluence ("Tinte del color de la luz", Range(0, 1)) = 0.25

        [Header(Sombra)]
        _ShadowTint ("Tinte de sombra", Color) = (0.1647, 0.1294, 0.2510, 1)
        _ShadowStrength ("Fuerza del tinte", Range(0, 1)) = 0.3
        _AmbientColor ("Ambiente plano", Color) = (0.4157, 0.3529, 0.6196, 1)
        _AmbientInfluence ("Influencia del ambiente en la sombra", Range(0, 1)) = 0.15

        [Header(Filo de luz)]
        [Toggle(_RIM)] _UseRim ("Filo de luz", Float) = 0
        _RimColor ("Color del filo", Color) = (1, 0.851, 0.627, 1)
        _RimWidth ("Ancho del filo", Range(0, 1)) = 0.3
        _RimSoftness ("Suavidad del filo", Range(0.001, 0.5)) = 0.04
        _RimLightAlign ("Solo del lado de la luz", Range(0, 1)) = 0.8

        [Header(Patron por textura)]
        [Toggle(_PATTERN_MAP)] _UsePatternMap ("Patrón por máscara", Float) = 0
        [NoScaleOffset] _PatternMap ("Máscara del patrón (R)", 2D) = "black" {}
        _PatternColor ("Color del patrón", Color) = (1, 1, 1, 1)

        [Header(Render)]
        [Enum(UnityEngine.Rendering.CullMode)] _Cull ("Cull", Float) = 2
        _OffsetFactor ("Depth offset factor", Float) = 0
        _OffsetUnits ("Depth offset units", Float) = 0
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque"
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Geometry"
        }

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

        CBUFFER_START(UnityPerMaterial)
            half4 _BaseColor;
            half _LightThreshold;
            half _LightSoftness;
            half _LightInfluence;
            half4 _ShadowTint;
            half _ShadowStrength;
            half4 _AmbientColor;
            half _AmbientInfluence;
            half4 _RimColor;
            half _RimWidth;
            half _RimSoftness;
            half _RimLightAlign;
            half4 _PatternColor;
        CBUFFER_END
        ENDHLSL

        Pass
        {
            Name "FlatToon"
            Tags { "LightMode" = "UniversalForward" }

            Cull [_Cull]
            Offset [_OffsetFactor], [_OffsetUnits]

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma shader_feature_local _VERTEX_COLOR
            #pragma shader_feature_local _RIM
            #pragma shader_feature_local _PATTERN_MAP
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            TEXTURE2D(_PatternMap);
            SAMPLER(sampler_PatternMap);

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                half4 color : COLOR;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 normalWS : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
                float2 uv : TEXCOORD2;
                half4 color : COLOR;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);

                VertexPositionInputs position = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionCS = position.positionCS;
                output.positionWS = position.positionWS;
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.color = input.color;
                output.uv = input.uv;
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);

                half3 baseColor = _BaseColor.rgb;
            #if defined(_PATTERN_MAP)
                half mask = SAMPLE_TEXTURE2D(_PatternMap, sampler_PatternMap, input.uv).r;
                baseColor = lerp(baseColor, _PatternColor.rgb, mask);
            #endif
            #if defined(_VERTEX_COLOR)
                baseColor *= input.color.rgb;
            #endif

                Light mainLight = GetMainLight();
                half3 normal = normalize(input.normalWS);
                half nDotL = dot(normal, mainLight.direction);

                half lit = smoothstep(
                    _LightThreshold - _LightSoftness,
                    _LightThreshold + _LightSoftness,
                    nDotL);

                half3 litColor = baseColor * lerp(1.0h, mainLight.color, _LightInfluence);

                half3 shadowColor = lerp(baseColor, _ShadowTint.rgb, _ShadowStrength);
                half ambientLuma = max(dot(_AmbientColor.rgb, half3(0.2126h, 0.7152h, 0.0722h)), 0.0001h);
                shadowColor = lerp(shadowColor, shadowColor * (_AmbientColor.rgb / ambientLuma), _AmbientInfluence);

                half3 color = lerp(shadowColor, litColor, lit);

            #if defined(_RIM)
                half3 viewDir = GetWorldSpaceNormalizeViewDir(input.positionWS);
                half fresnel = 1.0h - saturate(dot(normal, viewDir));
                half lightSide = lerp(1.0h, smoothstep(-0.2h, 0.35h, nDotL), _RimLightAlign);
                half rim = smoothstep(
                    1.0h - _RimWidth - _RimSoftness,
                    1.0h - _RimWidth + _RimSoftness,
                    fresnel) * lightSide;
                color = lerp(color, _RimColor.rgb, rim);
            #endif

                return half4(color, 1.0h);
            }
            ENDHLSL
        }

        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }

            ZWrite On
            ColorMask R
            Cull [_Cull]
            Offset [_OffsetFactor], [_OffsetUnits]

            HLSLPROGRAM
            #pragma vertex DepthVert
            #pragma fragment DepthFrag
            #pragma multi_compile_instancing

            struct DepthAttributes
            {
                float4 positionOS : POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct DepthVaryings
            {
                float4 positionCS : SV_POSITION;
            };

            DepthVaryings DepthVert(DepthAttributes input)
            {
                DepthVaryings output;
                UNITY_SETUP_INSTANCE_ID(input);
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                return output;
            }

            half DepthFrag(DepthVaryings input) : SV_Target
            {
                return 0;
            }
            ENDHLSL
        }
    }

    FallBack Off
}
