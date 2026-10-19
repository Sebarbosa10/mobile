Shader "Court/FlatToonAtlas"
{
    Properties
    {
        [NoScaleOffset] _AtlasMap ("Atlas de paleta", 2D) = "white" {}

        [Header(Luz)]
        _LightSoftness ("Suavidad del corte", Range(0.001, 0.5)) = 0.06
        _LightInfluence ("Tinte del color de la luz (toon)", Range(0, 1)) = 0.25
        _GraphicLightInfluence ("Tinte del color de la luz (gráfico)", Range(0, 1)) = 0.1

        [Header(Sombra)]
        _ShadowTint ("Tinte de sombra", Color) = (0.1647, 0.1294, 0.2510, 1)
        _AmbientColor ("Ambiente plano", Color) = (0.4157, 0.3529, 0.6196, 1)
        _AmbientInfluence ("Influencia del ambiente en la sombra", Range(0, 1)) = 0.15

        [Header(Render)]
        [Enum(UnityEngine.Rendering.CullMode)] _Cull ("Cull", Float) = 2
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
            half _LightSoftness;
            half _LightInfluence;
            half _GraphicLightInfluence;
            half4 _ShadowTint;
            half4 _AmbientColor;
            half _AmbientInfluence;
        CBUFFER_END
        ENDHLSL

        Pass
        {
            Name "FlatToonAtlas"
            Tags { "LightMode" = "UniversalForward" }

            Cull [_Cull]

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            TEXTURE2D(_AtlasMap);
            SAMPLER(sampler_AtlasMap);

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 normalWS : TEXCOORD0;
                float2 uv : TEXCOORD1;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);

                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.uv = input.uv;
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);

                half4 cell = SAMPLE_TEXTURE2D(_AtlasMap, sampler_AtlasMap, input.uv);
                half3 baseColor = cell.rgb;
                half graphic = step(0.99h, cell.a);
                half shadowStrength = cell.a;

                Light mainLight = GetMainLight();
                half nDotL = dot(normalize(input.normalWS), mainLight.direction);

                half lit = smoothstep(-_LightSoftness, _LightSoftness, nDotL);
                lit = max(lit, graphic);

                half influence = lerp(_LightInfluence, _GraphicLightInfluence, graphic);
                half3 litColor = baseColor * lerp(1.0h, mainLight.color, influence);

                half3 shadowColor = lerp(baseColor, _ShadowTint.rgb, shadowStrength);
                half ambientLuma = max(dot(_AmbientColor.rgb, half3(0.2126h, 0.7152h, 0.0722h)), 0.0001h);
                shadowColor = lerp(shadowColor, shadowColor * (_AmbientColor.rgb / ambientLuma), _AmbientInfluence);

                return half4(lerp(shadowColor, litColor, lit), 1.0h);
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
