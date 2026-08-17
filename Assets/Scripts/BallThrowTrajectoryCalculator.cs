using UnityEngine;

/// <summary>
/// Convierte los datos de un gesto en una velocidad
/// física de lanzamiento.
/// </summary>
public sealed class BallThrowTrajectoryCalculator
{
    private readonly float maxThrowSpeed;
    private readonly float forceMultiplier;
    private readonly float swipeSpeedForMaxThrow;
    private readonly float upwardArc;

    public BallThrowTrajectoryCalculator(
        float maxThrowSpeed,
        float forceMultiplier,
        float swipeSpeedForMaxThrow,
        float upwardArc)
    {
        this.maxThrowSpeed = maxThrowSpeed;
        this.forceMultiplier = forceMultiplier;
        this.swipeSpeedForMaxThrow = swipeSpeedForMaxThrow;
        this.upwardArc = upwardArc;
    }

    public Vector3 Calculate(
        SwipeReleaseGesture gesture,
        Camera camera)
    {
        if (camera == null)
            return Vector3.zero;

        float normalizedSpeed =
            Mathf.Clamp01(
                gesture.Speed /
                swipeSpeedForMaxThrow);

        float launchSpeed =
            normalizedSpeed
            * maxThrowSpeed
            * forceMultiplier;

        Vector2 viewportDirection =
            NormalizeToViewport(
                gesture.AimDirection);

        Vector3 flatForward =
            Vector3.ProjectOnPlane(
                camera.transform.forward,
                Vector3.up).normalized;

        Vector3 planarDirection =
            camera.transform.right
            * viewportDirection.x
            +
            flatForward
            * viewportDirection.y;

        if (planarDirection.sqrMagnitude <=
            Mathf.Epsilon)
        {
            return Vector3.zero;
        }

        Vector3 launchDirection =
            (
                planarDirection.normalized
                + Vector3.up * upwardArc
            ).normalized;

        return launchDirection * launchSpeed;
    }

    private static Vector2 NormalizeToViewport(
        Vector2 screenDirection)
    {
        if (screenDirection.sqrMagnitude <=
            Mathf.Epsilon)
        {
            return Vector2.zero;
        }

        Vector2 viewportDirection =
            new Vector2(
                screenDirection.x / Screen.width,
                screenDirection.y / Screen.height);

        return viewportDirection.normalized;
    }
}