using UnityEngine;

public sealed class BallThrowTrajectoryCalculator
{
    private const float StraightSoftnessDegrees = 9f;

    private readonly float maxThrowSpeed;
    private readonly float forceMultiplier;
    private readonly float swipeSpeedForMaxThrow;
    private readonly float upwardArc;

    private readonly float maximumHorizontalAngle;

    public BallThrowTrajectoryCalculator(
        float maxThrowSpeed,
        float forceMultiplier,
        float swipeSpeedForMaxThrow,
        float upwardArc,
        float maximumHorizontalAngle = 25f)
    {
        this.maxThrowSpeed = maxThrowSpeed;
        this.forceMultiplier = forceMultiplier;
        this.swipeSpeedForMaxThrow = swipeSpeedForMaxThrow;
        this.upwardArc = upwardArc;
        this.maximumHorizontalAngle =
            maximumHorizontalAngle;
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

        float easedSpeed =
            1f -
            (1f - normalizedSpeed) *
            (1f - normalizedSpeed);

        float launchSpeed =
            easedSpeed *
            maxThrowSpeed *
            forceMultiplier;

        Vector2 aim =
            gesture.AimDirection;

        aim = ClampHorizontalDeviation(aim);

        Vector3 forward =
            Vector3.ProjectOnPlane(
                camera.transform.forward,
                Vector3.up);

        if (forward.sqrMagnitude <= Mathf.Epsilon)
            forward = Vector3.forward;

        forward.Normalize();

        Vector3 right =
            Vector3.Cross(
                Vector3.up,
                forward).normalized;

        Vector3 planarDirection =
            forward * aim.y +
            right * aim.x;

        if (planarDirection.sqrMagnitude <=
            Mathf.Epsilon)
        {
            planarDirection = forward;
        }

        planarDirection.Normalize();

        Vector3 launchDirection =
            (
                planarDirection +
                Vector3.up * upwardArc
            ).normalized;

        return launchDirection * launchSpeed;
    }

    private Vector2 ClampHorizontalDeviation(
        Vector2 aim)
    {
        if (aim.sqrMagnitude <= Mathf.Epsilon)
            return Vector2.up;

        aim.Normalize();

        float angle =
            Mathf.Atan2(
                aim.x,
                aim.y)
            * Mathf.Rad2Deg;

        float squared = angle * angle;
        float softness = StraightSoftnessDegrees * StraightSoftnessDegrees;

        angle *= squared / (squared + softness);

        angle =
            Mathf.Clamp(
                angle,
                -maximumHorizontalAngle,
                maximumHorizontalAngle);

        float radians =
            angle * Mathf.Deg2Rad;

        return new Vector2(
            Mathf.Sin(radians),
            Mathf.Cos(radians));
    }
}
