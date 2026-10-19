using UnityEngine;

public sealed class PerformanceGovernor : MonoBehaviour
{
    public const int ThermalUnknown = -1;
    private const int ThermalLight = 1;
    private const int ThermalModerate = 2;
    private const int ThermalSevere = 3;

    private const float PollInterval = 2f;
    private const float SlowWindow = 5f;
    private const float SlowRatio = 0.85f;
    private const float CoolDownTime = 45f;
    private const int MinFrameRate = 60;

    private static PerformanceGovernor instance;

    private int maxFrameRate;
    private float pollTimer;
    private float slowTime;
    private float coolTime;
    private bool renderScaleReduced;
    private float baseRenderScale;

#if UNITY_ANDROID && !UNITY_EDITOR
    private AndroidJavaObject powerManager;
#endif

    public static int ThermalStatus { get; private set; } = ThermalUnknown;

    public static int TargetFrameRate =>
        Application.targetFrameRate;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Spawn()
    {
        if (instance != null)
            return;

        GameObject go = new("PerformanceGovernor");
        DontDestroyOnLoad(go);
        instance = go.AddComponent<PerformanceGovernor>();
    }

    private void Start()
    {
        maxFrameRate = Application.targetFrameRate;

        if (UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline is
            UnityEngine.Rendering.Universal.UniversalRenderPipelineAsset urp)
            baseRenderScale = urp.renderScale;

#if UNITY_ANDROID && !UNITY_EDITOR
        try
        {
            using AndroidJavaClass version = new("android.os.Build$VERSION");

            if (version.GetStatic<int>("SDK_INT") >= 29)
            {
                using AndroidJavaClass player = new("com.unity3d.player.UnityPlayer");
                using AndroidJavaObject activity = player.GetStatic<AndroidJavaObject>("currentActivity");
                powerManager = activity.Call<AndroidJavaObject>("getSystemService", "power");
            }
        }
        catch (System.Exception exception)
        {
            Debug.LogWarning($"[PerformanceGovernor] Sin estado térmico: {exception.Message}");
        }
#endif
    }

    private void Update()
    {
        if (maxFrameRate <= 0)
            return;

        float delta = Time.unscaledDeltaTime;
        int target = Application.targetFrameRate;

        if (delta > 1f / (target * SlowRatio))
            slowTime += delta;
        else
            slowTime = Mathf.Max(0f, slowTime - delta * 0.5f);

        pollTimer += delta;

        if (pollTimer < PollInterval)
            return;

        pollTimer = 0f;
        ThermalStatus = ReadThermalStatus();

        bool hot = ThermalStatus >= ThermalModerate;
        bool warm = ThermalStatus >= ThermalLight || ThermalStatus == ThermalUnknown;
        bool warmAndSlow = warm && slowTime >= SlowWindow;

        if ((hot || warmAndSlow) && target > MinFrameRate)
        {
            SetFrameRate(Mathf.Max(MinFrameRate, target / 2));
            coolTime = 0f;
            slowTime = 0f;
        }

        if (ThermalStatus >= ThermalSevere)
            ReduceRenderScale();

        if (ThermalStatus == 0)
        {
            coolTime += PollInterval;

            if (coolTime >= CoolDownTime)
            {
                if (target < maxFrameRate)
                    SetFrameRate(maxFrameRate);

                RestoreRenderScale();
                coolTime = 0f;
            }
        }
        else
        {
            coolTime = 0f;
        }
    }

    private static void SetFrameRate(int frameRate)
    {
        Application.targetFrameRate = frameRate;
        Debug.Log($"[PerformanceGovernor] Tope de FPS → {frameRate} (térmico {ThermalStatus}).");
    }

    private void ReduceRenderScale()
    {
        if (renderScaleReduced ||
            UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline is not
                UnityEngine.Rendering.Universal.UniversalRenderPipelineAsset urp)
            return;

        urp.renderScale = Mathf.Max(0.5f, baseRenderScale * 0.85f);
        renderScaleReduced = true;
    }

    private void RestoreRenderScale()
    {
        if (!renderScaleReduced ||
            UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline is not
                UnityEngine.Rendering.Universal.UniversalRenderPipelineAsset urp)
            return;

        urp.renderScale = baseRenderScale;
        renderScaleReduced = false;
    }

    private int ReadThermalStatus()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        if (powerManager == null)
            return ThermalUnknown;

        try
        {
            return powerManager.Call<int>("getCurrentThermalStatus");
        }
        catch
        {
            return ThermalUnknown;
        }
#else
        return ThermalUnknown;
#endif
    }

    private void OnDestroy()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        powerManager?.Dispose();
#endif
    }
}
