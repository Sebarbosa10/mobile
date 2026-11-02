using UnityEngine;

public static class MobileRuntimeSettings
{
    private const bool Uncapped = false;

    private const int CappedFrameRate = 60;

    private const int UnlimitedFrameRate = 1000;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Apply()
    {
        QualitySettings.vSyncCount = 0;

        int frameRate = Uncapped ? UnlimitedFrameRate : CappedFrameRate;

#if UNITY_ANDROID && !UNITY_EDITOR
        if (Uncapped)
        {
            SelectHighestRefreshRate();
        }
        else
        {
            SelectRefreshRateFor(CappedFrameRate);
            RequestDisplayMode(CappedFrameRate);
        }
#endif

        Application.targetFrameRate = frameRate;

        Screen.autorotateToLandscapeLeft = false;
        Screen.autorotateToLandscapeRight = false;
        Screen.autorotateToPortraitUpsideDown = false;
        Screen.autorotateToPortrait = true;
        Screen.orientation = ScreenOrientation.Portrait;

        Screen.sleepTimeout = SleepTimeout.NeverSleep;
    }

    private static int SelectHighestRefreshRate()
    {
        double current = Screen.currentResolution.refreshRateRatio.value;
        Resolution best = default;
        double bestRate = current;

        foreach (Resolution mode in Screen.resolutions)
        {
            double rate = mode.refreshRateRatio.value;

            if (rate > bestRate + 0.5)
            {
                best = mode;
                bestRate = rate;
            }
        }

        if (bestRate > current + 0.5)
        {
            Screen.SetResolution(Screen.width, Screen.height, Screen.fullScreenMode, best.refreshRateRatio);
            Debug.Log($"[MobileRuntimeSettings] Pantalla de {current:0} Hz → {bestRate:0} Hz (FPS sin tope).");
        }

        return Mathf.Max(CappedFrameRate, Mathf.RoundToInt((float)bestRate));
    }

    private static int SelectRefreshRateFor(int fps)
    {
        double current = Screen.currentResolution.refreshRateRatio.value;

        if (Fits(current, fps))
            return fps;

        Resolution best = default;
        bool found = false;

        foreach (Resolution mode in Screen.resolutions)
        {
            double rate = mode.refreshRateRatio.value;

            if (Fits(rate, fps) && (!found || rate < best.refreshRateRatio.value))
            {
                best = mode;
                found = true;
            }
        }

        if (found)
        {
            Screen.SetResolution(Screen.width, Screen.height, Screen.fullScreenMode, best.refreshRateRatio);
            Debug.Log($"[MobileRuntimeSettings] Pantalla de {current:0} Hz → {best.refreshRateRatio.value:0} Hz para {fps} FPS parejos.");
        }

        return fps;
    }

#if UNITY_ANDROID && !UNITY_EDITOR
    // Pide el modo de pantalla directamente a Android, para que 60 FPS entren parejos en el ritmo
    // de la pantalla en paneles de 90 o 120 Hz.
    private static void RequestDisplayMode(int fps)
    {
        try
        {
            AndroidJavaClass player = new("com.unity3d.player.UnityPlayer");
            AndroidJavaObject activity = player.GetStatic<AndroidJavaObject>("currentActivity");

            activity.Call("runOnUiThread", new AndroidJavaRunnable(() => SetPreferredMode(activity, fps)));
        }
        catch (System.Exception exception)
        {
            Debug.LogWarning($"[MobileRuntimeSettings] No se pudo pedir el modo de pantalla: {exception.Message}");
        }
    }

    private static void SetPreferredMode(AndroidJavaObject activity, int fps)
    {
        try
        {
            using AndroidJavaObject window = activity.Call<AndroidJavaObject>("getWindow");
            using AndroidJavaObject manager = activity.Call<AndroidJavaObject>("getWindowManager");
            using AndroidJavaObject display = manager.Call<AndroidJavaObject>("getDefaultDisplay");
            using AndroidJavaObject current = display.Call<AndroidJavaObject>("getMode");

            int width = current.Call<int>("getPhysicalWidth");
            int height = current.Call<int>("getPhysicalHeight");

            foreach (AndroidJavaObject mode in display.Call<AndroidJavaObject[]>("getSupportedModes"))
            {
                bool matches = mode.Call<int>("getPhysicalWidth") == width &&
                               mode.Call<int>("getPhysicalHeight") == height &&
                               Mathf.Abs(mode.Call<float>("getRefreshRate") - fps) < 1f;

                if (!matches)
                    continue;

                using AndroidJavaObject attributes = window.Call<AndroidJavaObject>("getAttributes");
                attributes.Set("preferredDisplayModeId", mode.Call<int>("getModeId"));
                window.Call("setAttributes", attributes);

                Debug.Log($"[MobileRuntimeSettings] Modo de pantalla pedido: {fps} Hz.");
                return;
            }

            Debug.Log($"[MobileRuntimeSettings] La pantalla no tiene un modo de {fps} Hz.");
        }
        catch (System.Exception exception)
        {
            Debug.LogWarning($"[MobileRuntimeSettings] No se pudo pedir el modo de pantalla: {exception.Message}");
        }
    }
#endif

    private static bool Fits(double refreshRate, int fps)
    {
        if (refreshRate < fps - 1)
            return false;

        double ratio = refreshRate / fps;
        return System.Math.Abs(ratio - System.Math.Round(ratio)) < 0.03;
    }
}
