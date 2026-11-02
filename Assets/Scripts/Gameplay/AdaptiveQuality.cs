using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

// Baja la calidad por etapas si el dispositivo no sostiene los FPS, y recuerda el nivel para la
// próxima vez. Sirve para cualquier teléfono, sin mantener una lista de modelos.
//   1: sin MSAA · 2: menos resolución · 3: sin posterizado · 4: sin contorno
public sealed class AdaptiveQuality : MonoBehaviour
{
    private const string LevelKey = "Quality.Level.2";
    private const int MaxLevel = 4;

    private const float WindowSeconds = 4f;
    private const float MinFps = 50f;
    private const float MaxFrameSeconds = 0.25f;
    private const float RenderScaleStep = 0.8f;

    private static int level;

    private float baseRenderScale;
    private float windowTime;
    private int windowFrames;

    public static bool PosterizeAllowed =>
        level < 3;

    public static bool OutlineAllowed =>
        level < 4;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Spawn()
    {
        if (Application.isEditor)
            return;

        GameObject go = new("AdaptiveQuality");
        DontDestroyOnLoad(go);
        go.AddComponent<AdaptiveQuality>();
    }

    private void Awake()
    {
        if (GraphicsSettings.currentRenderPipeline is UniversalRenderPipelineAsset urp)
            baseRenderScale = urp.renderScale;

        level = Mathf.Clamp(PlayerPrefs.GetInt(LevelKey, 0), 0, MaxLevel);
        Apply();
    }

    private void Update()
    {
        float delta = Time.unscaledDeltaTime;

        // Cargas de escena, pausa y anuncios no cuentan como rendimiento del juego
        if (Time.timeScale == 0f || delta > MaxFrameSeconds)
        {
            ResetWindow();
            return;
        }

        windowTime += delta;
        windowFrames++;

        if (windowTime < WindowSeconds)
            return;

        float fps = windowFrames / windowTime;
        ResetWindow();

        if (fps >= MinFps || level >= MaxLevel)
            return;

        level++;
        PlayerPrefs.SetInt(LevelKey, level);
        PlayerPrefs.Save();
        Apply();

        Debug.Log($"[AdaptiveQuality] {fps:0} FPS: calidad baja al nivel {level}.");
    }

    private void ResetWindow()
    {
        windowTime = 0f;
        windowFrames = 0;
    }

    private void Apply()
    {
        if (GraphicsSettings.currentRenderPipeline is not UniversalRenderPipelineAsset urp)
            return;

        if (level >= 1)
            urp.msaaSampleCount = 1;

        if (level >= 2)
            urp.renderScale = Mathf.Max(0.5f, baseRenderScale * RenderScaleStep);
    }
}
