Shader "Hidden/Stylized/Posterize"
{
    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" }
        ZWrite Off ZTest Always Cull Off Blend Off

        Pass
        {
            Name "Posterize"

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            float _PosterizeSteps;
            float _PosterizeAmount;
            float _PosterizeDither;

            static const float Bayer4x4[16] =
            {
                0.0,  8.0,  2.0, 10.0,
                12.0, 4.0, 14.0,  6.0,
                3.0, 11.0,  1.0,  9.0,
                15.0, 7.0, 13.0,  5.0
            };

            float Bayer(float2 pixel)
            {
                uint2 p = uint2(pixel) & 3u;
                return (Bayer4x4[p.y * 4u + p.x] + 0.5) / 16.0;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                half4 color = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_PointClamp, input.texcoord);

                #if UNITY_COLORSPACE_GAMMA
                half3 c = color.rgb;
                #else
                half3 c = LinearToSRGB(color.rgb);
                #endif

                float levels = max(_PosterizeSteps - 1.0, 1.0);
                float dither = (Bayer(input.positionCS.xy) - 0.5) * _PosterizeDither;
                half3 quantized = saturate(floor(c * levels + 0.5 + dither) / levels);

                c = lerp(c, quantized, _PosterizeAmount);

                #if UNITY_COLORSPACE_GAMMA
                color.rgb = c;
                #else
                color.rgb = SRGBToLinear(c);
                #endif

                return color;
            }
            ENDHLSL
        }
    }
}
