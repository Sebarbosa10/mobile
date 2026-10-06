using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Fuerza, al arrancar el juego, la calidad de render que queremos en
/// todos los dispositivos: resolución completa (render scale 1), MSAA
/// 4x y sombras con buena definición. Sin esto, un URP Asset con render scale menor a 1 dibuja el juego
/// a menor resolución y lo estira, y sin MSAA los bordes se ven
/// serruchados: el resultado es una imagen borrosa/pixelada.
///
/// Corre solo (RuntimeInitializeOnLoadMethod), no hace falta ponerlo en
/// ninguna escena. Así da igual qué valores queden guardados en el
/// asset o qué nivel de calidad elija la plataforma.
/// </summary>
public static class RenderQualityEnforcer
{
    private const float TargetRenderScale = 1f;
    private const int TargetMsaaSamples = 4;
    private const float TargetShadowDistance = 25f;
    private const int TargetShadowmapResolution = 2048;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Apply()
    {
        if (GraphicsSettings.currentRenderPipeline is not UniversalRenderPipelineAsset urpAsset)
        {
            Debug.LogWarning(
                "[RenderQualityEnforcer] El pipeline activo no es URP; no se ajustó la calidad.");

            return;
        }

        urpAsset.renderScale = TargetRenderScale;
        urpAsset.msaaSampleCount = TargetMsaaSamples;

        /*
         * La escena entra en pocos metros: concentrar el shadowmap en
         * esa distancia (en vez de 50 m) y darle más resolución evita
         * que las sombras se vean en escalera/pixeladas.
         */
        urpAsset.shadowDistance = TargetShadowDistance;
        urpAsset.mainLightShadowmapResolution = TargetShadowmapResolution;
    }
}
