// Contorno por casco invertido: dibuja las caras traseras del mesh
// infladas a lo largo de la normal, en un color plano. Se usa en la
// pelota para que siempre se lea sobre el piso, la llave y el cielo.
//
// Necesita normales suaves (vértices compartidos): con normales por cara
// el casco se abre en las aristas. El builder genera un mesh aparte
// para el contorno.
Shader "Court/Outline"
{
    Properties
    {
        _BaseColor ("Color del contorno", Color) = (0.2314, 0.1020, 0.0392, 1)
        _OutlineWidth ("Ancho (unidades del objeto)", Range(0, 0.2)) = 0.025
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque"
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Geometry"
        }

        Pass
        {
            Name "Outline"
            Tags { "LightMode" = "UniversalForward" }

            Cull Front
            ZWrite On

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
                float _OutlineWidth;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                float3 inflated = input.positionOS.xyz + normalize(input.normalOS) * _OutlineWidth;
                output.positionCS = TransformObjectToHClip(inflated);
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                return _BaseColor;
            }
            ENDHLSL
        }
    }

    FallBack Off
}
