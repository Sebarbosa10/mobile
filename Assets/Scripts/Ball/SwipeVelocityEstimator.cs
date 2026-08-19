using UnityEngine;

/// <summary>
/// Calcula la dirección y velocidad de un swipe.
///
/// La dirección se obtiene principalmente del desplazamiento
/// total del dedo, evitando que pequeños movimientos laterales
/// al agarrar o soltar la pelota alteren demasiado el lanzamiento.
/// </summary>
public sealed class SwipeVelocityEstimator
{
    private const float MinimumAimDistancePixels = 35f;

    private Vector2 startPosition;
    private Vector2 lastPosition;

    private float startTime;
    private float travelledDistance;

    private bool isTracking;

    public void Begin(
        Vector2 screenPosition,
        float timestamp)
    {
        startPosition = screenPosition;
        lastPosition = screenPosition;

        startTime = timestamp;
        travelledDistance = 0f;

        isTracking = true;
    }

    public void Move(Vector2 screenPosition)
    {
        if (!isTracking)
            return;

        Vector2 movement =
            screenPosition - lastPosition;

        travelledDistance += movement.magnitude;

        lastPosition = screenPosition;
    }

    public SwipeReleaseGesture Release(
        Vector2 screenPosition,
        float timestamp)
    {
        if (!isTracking)
            return default;

        Move(screenPosition);

        isTracking = false;

        Vector2 totalMovement =
            screenPosition - startPosition;

        float distance =
            totalMovement.magnitude;

        float elapsedTime =
            Mathf.Max(
                timestamp - startTime,
                0.001f);

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
             * La dirección ahora sale del recorrido TOTAL
             * del dedo y no del último movimiento.
             */
            aimDirection =
                totalMovement.normalized;
        }

        float speed =
            travelledDistance / elapsedTime;

        return new SwipeReleaseGesture(
            aimDirection,
            speed);
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