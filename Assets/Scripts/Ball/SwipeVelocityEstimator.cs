using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Calcula la dirección y velocidad de un swipe.
///
/// La dirección se obtiene principalmente del desplazamiento
/// total del dedo, evitando que pequeños movimientos laterales
/// al agarrar o soltar la pelota alteren demasiado el lanzamiento.
///
/// La velocidad, en cambio, se toma solo de una ventana reciente
/// del gesto. Promediarla sobre todo el gesto (desde que se agarra
/// la pelota) hace que un agarre lento seguido de un flick rápido
/// se sienta débil, porque el tiempo sostenido diluye el promedio.
/// Con la ventana reciente, el lanzamiento responde al golpe final
/// de la muñeca, que es lo que el jugador realmente siente.
/// </summary>
public sealed class SwipeVelocityEstimator
{
    private const float MinimumAimDistancePixels = 35f;
    private const float RecentVelocityWindowSeconds = 0.1f;

    private readonly Queue<Sample> recentSamples = new();

    private Vector2 startPosition;

    private bool isTracking;

    public void Begin(
        Vector2 screenPosition,
        float timestamp)
    {
        startPosition = screenPosition;

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

        Vector2 totalMovement =
            screenPosition - startPosition;

        float distance =
            totalMovement.magnitude;

        Vector2 aimDirection;

        /*
         * Si el dedo prácticamente no se movió,
         * no intentamos inventar una dirección.
         */
        if (distance < MinimumAimDistancePixels)
        {
            aimDirection = Vector2.up;
        }
        else
        {
            /*
             * La dirección sale del recorrido TOTAL
             * del dedo y no del último movimiento.
             */
            aimDirection =
                totalMovement.normalized;
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
               now - recentSamples.Peek().Timestamp > RecentVelocityWindowSeconds)
        {
            recentSamples.Dequeue();
        }
    }

    private float ComputeRecentSpeed(float now)
    {
        TrimOldSamples(now);

        if (recentSamples.Count < 2)
            return 0f;

        float elapsedTime =
            Mathf.Max(
                now - recentSamples.Peek().Timestamp,
                0.001f);

        float pathLength = 0f;
        Vector2 previousPosition = default;
        bool hasPrevious = false;

        foreach (Sample sample in recentSamples)
        {
            if (hasPrevious)
            {
                pathLength +=
                    (sample.Position - previousPosition).magnitude;
            }

            previousPosition = sample.Position;
            hasPrevious = true;
        }

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
