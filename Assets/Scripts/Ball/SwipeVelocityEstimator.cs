using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Calcula la dirección y velocidad de un swipe.
///
/// Ambas se toman de una ventana reciente del gesto, no de todo
/// el recorrido desde que se agarró la pelota. Promediar sobre
/// todo el gesto hace que el lanzamiento se sienta desconectado
/// del recorrido real del dedo justo antes de soltar: un agarre
/// lento seguido de un flick rápido sale débil, y una curva en el
/// camino termina lanzando en línea recta desde el punto de agarre
/// en vez de seguir hacia donde el dedo se estaba moviendo al
/// soltar.
///
/// La ventana de dirección es más larga que la de velocidad para
/// no perder la intención de un apuntado lento y deliberado (poco
/// desplazamiento por cuadro, pero sostenido), mientras que la de
/// velocidad es corta para capturar el golpe final de la muñeca.
/// </summary>
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

        /*
         * Si el dedo prácticamente no se movió en la ventana
         * reciente, no intentamos inventar una dirección.
         */
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

/// <summary>
/// Información generada cuando termina un swipe.
/// </summary>
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
