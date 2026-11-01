using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public static class RenderQualityEnforcer
{
    private const float MaxRenderHeight = 1600f;
    private const int TargetMsaaSamples = 2;

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

        // El arte es plano y sin bloom: HDR solo agregaría costo en cada pasada de pantalla completa
        urpAsset.supportsHDR = false;

        urpAsset.shadowDistance = 0f;
    }
}
