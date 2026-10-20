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

    private readonly FrameTiming[] timings = new FrameTiming[1];

    private TextMeshProUGUI label;

    private int frames;
    private float elapsed;
    private float worstFrame;
    private float worstElapsed;
    private float shownWorst;

    private double cpuSum;
    private double gpuSum;
    private int timingSamples;
    private int gpuSamples;

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

        RectTransform chip = UIKit.CreateChip(safe, "Chip", 92f, new Color(0.118f, 0.086f, 0.22f, 0.8f), 20f, 0f);
        chip.anchorMin = chip.anchorMax = Vector2.zero;
        chip.pivot = Vector2.zero;
        chip.anchoredPosition = new Vector2(16f, 16f);

        FpsCounter counter = canvas.gameObject.AddComponent<FpsCounter>();
        counter.label = UIKit.AddChipText(chip, "-- FPS", UIFont.Label, 28f, Color.white);
        counter.label.alignment = TextAlignmentOptions.Left;
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

        SampleFrameTiming();

        if (elapsed < RefreshInterval)
            return;

        float fps = frames / elapsed;
        float ms = elapsed * 1000f / frames;
        float minFps = shownWorst > 0f ? 1f / shownWorst : fps;
        int thermal = PerformanceGovernor.ThermalStatus;

        if (timingSamples > 0)
        {
            float cpu = (float)(cpuSum / timingSamples);
            float gpu = gpuSamples > 0 ? (float)(gpuSum / gpuSamples) : 0f;
            float potential = 1000f / Mathf.Max(cpu, gpu, 0.1f);

            if (gpuSamples > 0)
                label.SetText("{0:0} FPS  {1:0.0} ms  mín {2:0}  T{3}\nposible {4:0} FPS  CPU {5:0.0}  GPU {6:0.0} ms",
                    fps, ms, minFps, thermal, potential, cpu, gpu);
            else
                label.SetText("{0:0} FPS  {1:0.0} ms  mín {2:0}  T{3}\nposible {4:0} FPS  CPU {5:0.0} ms",
                    fps, ms, minFps, thermal, potential, cpu);
        }
        else
        {
            label.SetText("{0:0} FPS  {1:0.0} ms  mín {2:0}  T{3}", fps, ms, minFps, thermal);
        }

        label.color = fps >= 55f ? Good : fps >= 30f ? Warning : Bad;

        frames = 0;
        elapsed = 0f;
        cpuSum = 0d;
        gpuSum = 0d;
        timingSamples = 0;
        gpuSamples = 0;
    }

    private void SampleFrameTiming()
    {
        FrameTimingManager.CaptureFrameTimings();

        if (FrameTimingManager.GetLatestTimings(1, timings) == 0)
            return;

        FrameTiming timing = timings[0];

        double mainThread = timing.cpuMainThreadFrameTime - timing.cpuMainThreadPresentWaitTime;
        double cpu = System.Math.Max(mainThread, timing.cpuRenderThreadFrameTime);

        if (cpu <= 0d)
            return;

        cpuSum += cpu;
        timingSamples++;

        if (timing.gpuFrameTime > 0d)
        {
            gpuSum += timing.gpuFrameTime;
            gpuSamples++;
        }
    }
}
