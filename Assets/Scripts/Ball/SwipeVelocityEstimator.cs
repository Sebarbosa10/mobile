using System.Collections.Generic;
using UnityEngine;

public sealed class SwipeVelocityEstimator
{
    private const float MinimumAimDistancePixels = 35f;
    private const float HistorySeconds = 0.25f;
    private const float FitWindowSeconds = 0.15f;
    private const float RecencyTimeConstant = 0.06f;
    private const float MinimumFitSpan = 0.012f;
    private const float LiftOffSlowdown = 0.55f;
    private const float LiftOffIgnoreSeconds = 0.04f;
    private const float FingerEndInfluence = 0.3f;

    private readonly List<Sample> samples = new();

    private bool isTracking;

    public void Begin(Vector2 screenPosition, float timestamp)
    {
        samples.Clear();
        samples.Add(new Sample(screenPosition, timestamp));
        isTracking = true;
    }

    public void Move(Vector2 screenPosition, float timestamp)
    {
        if (!isTracking)
            return;

        Sample last = samples[samples.Count - 1];

        if (timestamp <= last.Timestamp)
        {
            samples[samples.Count - 1] = new Sample(screenPosition, last.Timestamp);
            return;
        }

        samples.Add(new Sample(screenPosition, timestamp));

        while (samples.Count > 2 && timestamp - samples[0].Timestamp > HistorySeconds)
            samples.RemoveAt(0);
    }

    public SwipeReleaseGesture Release(Vector2 screenPosition, float timestamp)
    {
        if (!isTracking)
            return default;

        Move(screenPosition, timestamp);
        isTracking = false;

        Vector2 velocity = FitVelocity(timestamp);
        Vector2 stroke = StrokeDirection(timestamp);

        Vector2 aimDirection = Vector2.up;

        if (stroke.magnitude >= MinimumAimDistancePixels)
        {
            aimDirection = stroke.normalized;

            if (velocity.sqrMagnitude > 1f)
                aimDirection = Vector2.Lerp(aimDirection, velocity.normalized, FingerEndInfluence).normalized;
        }

        return new SwipeReleaseGesture(aimDirection, velocity.magnitude);
    }

    private Vector2 StrokeDirection(float now)
    {
        Vector2 start = samples[0].Position;
        Vector2 end = samples[samples.Count - 1].Position;

        for (int i = samples.Count - 1; i > 0; i--)
        {
            if (now - samples[i].Timestamp >= LiftOffIgnoreSeconds)
            {
                end = samples[i].Position;
                break;
            }
        }

        Vector2 stroke = end - start;
        return stroke.magnitude >= MinimumAimDistancePixels ? stroke : samples[samples.Count - 1].Position - start;
    }

    private Vector2 FitVelocity(float now)
    {
        int first = samples.Count - 1;

        while (first > 0 && now - samples[first - 1].Timestamp <= FitWindowSeconds)
            first--;

        if (samples.Count - first < 2 || samples[samples.Count - 1].Timestamp - samples[first].Timestamp < MinimumFitSpan)
            return FallbackVelocity();

        Vector2 fitted = WeightedSlope(first);
        Vector2 peak = PeakVelocity(first);

        if (peak.magnitude * LiftOffSlowdown > fitted.magnitude)
            fitted = fitted.normalized * (peak.magnitude * LiftOffSlowdown);

        return fitted;
    }

    private Vector2 WeightedSlope(int first)
    {
        float now = samples[samples.Count - 1].Timestamp;

        float sumW = 0f, sumT = 0f, sumTT = 0f;
        Vector2 sumP = Vector2.zero, sumTP = Vector2.zero;

        for (int i = first; i < samples.Count; i++)
        {
            float t = samples[i].Timestamp - now;
            float w = Mathf.Exp(t / RecencyTimeConstant);
            Vector2 p = samples[i].Position;

            sumW += w;
            sumT += w * t;
            sumTT += w * t * t;
            sumP += w * p;
            sumTP += w * t * p;
        }

        float denominator = sumW * sumTT - sumT * sumT;

        if (Mathf.Abs(denominator) < 1e-9f)
            return FallbackVelocity();

        return (sumW * sumTP - sumT * sumP) / denominator;
    }

    private Vector2 PeakVelocity(int first)
    {
        Vector2 peak = Vector2.zero;

        for (int i = Mathf.Max(first, 1); i < samples.Count; i++)
        {
            float dt = samples[i].Timestamp - samples[i - 1].Timestamp;

            if (dt <= 0f)
                continue;

            Vector2 v = (samples[i].Position - samples[i - 1].Position) / dt;

            if (v.sqrMagnitude > peak.sqrMagnitude)
                peak = v;
        }

        return peak;
    }

    private Vector2 FallbackVelocity()
    {
        Sample a = samples[0];
        Sample b = samples[samples.Count - 1];
        float dt = Mathf.Max(b.Timestamp - a.Timestamp, 0.001f);
        return (b.Position - a.Position) / dt;
    }

    private readonly struct Sample
    {
        public Sample(Vector2 position, float timestamp)
        {
            Position = position;
            Timestamp = timestamp;
        }

        public Vector2 Position { get; }

        public float Timestamp { get; }
    }
}

public readonly struct SwipeReleaseGesture
{
    public SwipeReleaseGesture(Vector2 aimDirection, float speed)
    {
        AimDirection = aimDirection;
        Speed = speed;
    }

    public Vector2 AimDirection { get; }

    public float Speed { get; }
}
