Shader "Hidden/Stylized/StylizedPost"
{
    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" }
        ZWrite Off ZTest Always Cull Off Blend Off

        Pass
        {
            Name "StylizedPost"

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_local _ _OUTLINE

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

            half4 _OutlineColor;
            float _OutlineThickness;
            float _OutlineThreshold;
            float _OutlineFadeDistance;

            half _Saturation;
            half _Contrast;
            half4 _ShadowTint;
            half4 _HighlightTint;
            half _SplitToning;

            half4 _VignetteColor;
            half _VignetteIntensity;
            half _VignetteRadius;
            half _VignetteSoftness;

            float InverseDepth(float2 uv)
            {
                return 1.0 / max(LinearEyeDepth(SampleSceneDepth(uv), _ZBufferParams), 1e-4);
            }

            half3 Tint(half3 tint)
            {
                return tint / max(Luminance(tint), 1e-3);
            }

            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                float2 uv = input.texcoord;
                half4 source = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv);
                half3 color = source.rgb;

                #if defined(_OUTLINE)
                {
                    float2 texel = _OutlineThickness / _ScreenParams.xy;
                    float center = InverseDepth(uv);

                    float edge =
                        max(0.0, center - InverseDepth(uv + float2(texel.x, 0.0))) +
                        max(0.0, center - InverseDepth(uv - float2(texel.x, 0.0))) +
                        max(0.0, center - InverseDepth(uv + float2(0.0, texel.y))) +
                        max(0.0, center - InverseDepth(uv - float2(0.0, texel.y)));

                    edge = smoothstep(_OutlineThreshold, _OutlineThreshold * 2.0, edge / center);
                    edge *= 1.0 - saturate((1.0 / center) / _OutlineFadeDistance);

                    color = lerp(color, _OutlineColor.rgb, edge * _OutlineColor.a);
                }
                #endif

                half luma = Luminance(color);
                color = lerp(luma.xxx, color, _Saturation);
                color = max(0.0, (color - 0.18) * _Contrast + 0.18);

                half3 tint = lerp(Tint(_ShadowTint.rgb), Tint(_HighlightTint.rgb), smoothstep(0.05, 0.6, saturate(luma)));
                color *= lerp(1.0, tint, _SplitToning);

                float2 d = uv - 0.5;
                half vignette = smoothstep(_VignetteRadius, _VignetteRadius - _VignetteSoftness, length(d) * 1.41421356);
                color = lerp(color, _VignetteColor.rgb, (1.0 - vignette) * _VignetteIntensity);

                return half4(color, source.a);
            }
            ENDHLSL
        }
    }
}
