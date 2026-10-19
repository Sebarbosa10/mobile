using System.Collections.Generic;
using UnityEngine;

public sealed class SwipeVelocityEstimator
{
    private const float MinimumAimDistancePixels = 35f;
    private const float DirectionWindowSeconds = 0.25f;
    private const float SpeedWindowSeconds = 0.1f;

    private readonly Queue<Sample> recentSamples = new();

    private bool isTracking;

    public void Begin(
        Vector2 screenPosition,
        float timestamp)
    {
        recentSamples.Clear();
        recentSamples.Enqueue(new Sample(screenPosition, timestamp));

        isTracking = true;
    }

    public void Move(
        Vector2 screenPosition,
        float timestamp)
    {
        if (!isTracking)
            return;

        recentSamples.Enqueue(new Sample(screenPosition, timestamp));

        TrimOldSamples(timestamp);
    }

    public SwipeReleaseGesture Release(
        Vector2 screenPosition,
        float timestamp)
    {
        if (!isTracking)
            return default;

        Move(screenPosition, timestamp);

        isTracking = false;

        Vector2 recentDisplacement =
            screenPosition - recentSamples.Peek().Position;

        Vector2 aimDirection;

        if (recentDisplacement.magnitude < MinimumAimDistancePixels)
        {
            aimDirection = Vector2.up;
        }
        else
        {
            aimDirection =
                recentDisplacement.normalized;
        }

        float speed =
            ComputeRecentSpeed(timestamp);

        return new SwipeReleaseGesture(
            aimDirection,
            speed);
    }

    private void TrimOldSamples(float now)
    {
        while (recentSamples.Count > 1 &&
               now - recentSamples.Peek().Timestamp > DirectionWindowSeconds)
        {
            recentSamples.Dequeue();
        }
    }

    private float ComputeRecentSpeed(float now)
    {
        float pathLength = 0f;
        Vector2 previousPosition = default;
        bool hasPrevious = false;
        float? oldestTimestampInWindow = null;

        foreach (Sample sample in recentSamples)
        {
            if (now - sample.Timestamp > SpeedWindowSeconds)
                continue;

            oldestTimestampInWindow ??= sample.Timestamp;

            if (hasPrevious)
            {
                pathLength +=
                    (sample.Position - previousPosition).magnitude;
            }

            previousPosition = sample.Position;
            hasPrevious = true;
        }

        if (oldestTimestampInWindow == null)
            return 0f;

        float elapsedTime =
            Mathf.Max(
                now - oldestTimestampInWindow.Value,
                0.001f);

        return pathLength / elapsedTime;
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
    public SwipeReleaseGesture(
        Vector2 aimDirection,
        float speed)
    {
        AimDirection = aimDirection;
        Speed = speed;
    }

    public Vector2 AimDirection { get; }

    public float Speed { get; }
}
