using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public static class RenderQualityEnforcer
{
    private const float MaxRenderHeight = 1920f;
    private const int TargetMsaaSamples = 4;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Apply()
    {
        if (GraphicsSettings.currentRenderPipeline is not UniversalRenderPipelineAsset urpAsset)
        {
            Debug.LogWarning(
                "[RenderQualityEnforcer] El pipeline activo no es URP; no se ajustó la calidad.");

            return;
        }

        int screenHeight = Mathf.Max(Screen.height, Screen.width);
        urpAsset.renderScale = Mathf.Clamp(MaxRenderHeight / Mathf.Max(1, screenHeight), 0.5f, 1f);
        urpAsset.msaaSampleCount = TargetMsaaSamples;

        urpAsset.shadowDistance = 0f;
    }
}
