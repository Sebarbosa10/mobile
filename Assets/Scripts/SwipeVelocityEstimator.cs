using UnityEngine;

/// <summary>
/// Mantiene la distancia recorrida y el punto de suelta de un gesto.
/// No depende de Unity como componente ni conoce física o cámaras.
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

    public void Begin(Vector2 screenPosition, float timestamp)
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

    public SwipeReleaseGesture Release(Vector2 screenPosition, float timestamp)
    {
        if (!isTracking)
            return default;

        isTracking = false;
        RegisterMovement(screenPosition);
        float elapsedTime = Mathf.Max(timestamp - startTime, Mathf.Epsilon);
        Vector2 directAim = screenPosition - startPosition;
        Vector2 aimDirection = directAim.magnitude >= MinimumAimDistancePixels
            ? directAim
            : lastMovementDirection;
        return new SwipeReleaseGesture(aimDirection, travelledDistance / elapsedTime);
    }

    private void RegisterMovement(Vector2 screenPosition)
    {
        Vector2 movement = screenPosition - lastPosition;
        if (movement.sqrMagnitude > Mathf.Epsilon)
            lastMovementDirection = movement;

        travelledDistance += movement.magnitude;
        lastPosition = screenPosition;
    }
}

/// <summary>Datos inmutables de lanzamiento producidos por un gesto.</summary>
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
