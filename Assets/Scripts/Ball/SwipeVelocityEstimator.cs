using UnityEngine;

/// <summary>
/// Calcula la velocidad y dirección de un gesto de swipe.
/// No depende de GameObjects, Rigidbody ni cámaras.
/// </summary>
public sealed class SwipeVelocityEstimator
{
    private const float MinimumAimDistancePixels = 20f;

    private Vector2 startPosition;
    private Vector2 lastPosition;
    private Vector2 lastMovementDirection;

    private float startTime;
    private float travelledDistance;

    private bool isTracking;

    public void Begin(
        Vector2 screenPosition,
        float timestamp)
    {
        startPosition = screenPosition;
        lastPosition = screenPosition;

        lastMovementDirection = Vector2.zero;

        startTime = timestamp;
        travelledDistance = 0f;

        isTracking = true;
    }

    public void Move(Vector2 screenPosition)
    {
        if (!isTracking)
            return;

        RegisterMovement(screenPosition);
    }

    public SwipeReleaseGesture Release(
        Vector2 screenPosition,
        float timestamp)
    {
        if (!isTracking)
            return default;

        isTracking = false;

        RegisterMovement(screenPosition);

        float elapsedTime =
            Mathf.Max(
                timestamp - startTime,
                Mathf.Epsilon);

        Vector2 directAim =
            screenPosition - startPosition;

        Vector2 aimDirection =
            directAim.magnitude >=
            MinimumAimDistancePixels
                ? directAim
                : lastMovementDirection;

        float speed =
            travelledDistance /
            elapsedTime;

        return new SwipeReleaseGesture(
            aimDirection,
            speed);
    }

    private void RegisterMovement(
        Vector2 screenPosition)
    {
        Vector2 movement =
            screenPosition - lastPosition;

        if (movement.sqrMagnitude >
            Mathf.Epsilon)
        {
            lastMovementDirection = movement;
        }

        travelledDistance += movement.magnitude;

        lastPosition = screenPosition;
    }
}

/// <summary>
/// Datos inmutables producidos al finalizar
/// un gesto de lanzamiento.
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