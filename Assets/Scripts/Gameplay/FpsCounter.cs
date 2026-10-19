using TMPro;
using UnityEngine;

public sealed class FpsCounter : MonoBehaviour
{
    private static readonly bool ShowInReleaseBuilds = true;

    private const float RefreshInterval = 0.25f;
    private const float WorstWindow = 1f;

    private static readonly Color Good = new(0.251f, 0.851f, 0.349f);
    private static readonly Color Warning = new(1f, 0.816f, 0.443f);
    private static readonly Color Bad = new(0.902f, 0.2f, 0.2f);

    private TextMeshProUGUI label;

    private int frames;
    private float elapsed;
    private float worstFrame;
    private float worstElapsed;
    private float shownWorst;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Spawn()
    {
        if (!ShowInReleaseBuilds && !Debug.isDebugBuild)
            return;

        if (UISkin.Instance == null)
            return;

        Canvas canvas = UIKit.CreateCanvas("FpsCounter", 1000);
        DontDestroyOnLoad(canvas.gameObject);

        RectTransform safe = UIKit.CreateSafeArea(canvas.transform);

        RectTransform chip = UIKit.CreateChip(safe, "Chip", 52f, new Color(0.118f, 0.086f, 0.22f, 0.8f), 20f, 0f);
        chip.anchorMin = chip.anchorMax = Vector2.zero;
        chip.pivot = Vector2.zero;
        chip.anchoredPosition = new Vector2(16f, 16f);

        FpsCounter counter = canvas.gameObject.AddComponent<FpsCounter>();
        counter.label = UIKit.AddChipText(chip, "-- FPS", UIFont.Label, 28f, Color.white);
    }

    private void Update()
    {
        float delta = Time.unscaledDeltaTime;

        frames++;
        elapsed += delta;
        worstFrame = Mathf.Max(worstFrame, delta);
        worstElapsed += delta;

        if (worstElapsed >= WorstWindow)
        {
            shownWorst = worstFrame;
            worstFrame = 0f;
            worstElapsed = 0f;
        }

        if (elapsed < RefreshInterval)
            return;

        float fps = frames / elapsed;
        float ms = elapsed * 1000f / frames;
        float minFps = shownWorst > 0f ? 1f / shownWorst : fps;

        int target = PerformanceGovernor.TargetFrameRate;
        int thermal = PerformanceGovernor.ThermalStatus;

        if (thermal >= 0)
            label.SetText("{0:0} FPS  {1:0.0} ms  mín {2:0}  tope {3}  T{4}", fps, ms, minFps, target, thermal);
        else
            label.SetText("{0:0} FPS  {1:0.0} ms  mín {2:0}", fps, ms, minFps);

        label.color = fps >= 55f ? Good : fps >= 30f ? Warning : Bad;

        frames = 0;
        elapsed = 0f;
    }
}
