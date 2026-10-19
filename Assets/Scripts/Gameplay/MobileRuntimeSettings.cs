using UnityEngine;

public static class MobileRuntimeSettings
{
    private const bool Uncapped = true;

    private const int CappedFrameRate = 60;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Apply()
    {
        QualitySettings.vSyncCount = 0;

        int frameRate = Uncapped ? -1 : CappedFrameRate;

#if UNITY_ANDROID && !UNITY_EDITOR
        frameRate = Uncapped ? SelectHighestRefreshRate() : SelectRefreshRateFor(CappedFrameRate);
#elif UNITY_IOS && !UNITY_EDITOR
        if (Uncapped)
            frameRate = Mathf.RoundToInt((float)Screen.currentResolution.refreshRateRatio.value);
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

    private static bool Fits(double refreshRate, int fps)
    {
        if (refreshRate < fps - 1)
            return false;

        double ratio = refreshRate / fps;
        return System.Math.Abs(ratio - System.Math.Round(ratio)) < 0.03;
    }
}
