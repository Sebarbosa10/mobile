using UnityEngine;

/// <summary>
/// Convierte un gesto de swipe en una velocidad de lanzamiento.
///
/// La dirección se basa en la intención general del swipe,
/// limitando pequeñas desviaciones laterales involuntarias.
/// </summary>
public sealed class BallThrowTrajectoryCalculator
{
    private readonly float maxThrowSpeed;
    private readonly float forceMultiplier;
    private readonly float swipeSpeedForMaxThrow;
    private readonly float upwardArc;

    /*
     * Ángulo máximo que puede desviarse el lanzamiento
     * horizontalmente respecto de la dirección frontal.
     */
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

        float launchSpeed =
            normalizedSpeed *
            maxThrowSpeed *
            forceMultiplier;

        Vector2 aim =
            gesture.AimDirection;

        /*
         * Evitamos que pequeños movimientos laterales
         * generen una desviación exagerada.
         */
        aim = ClampHorizontalDeviation(aim);

        /*
         * Convertimos el input de pantalla
         * a una dirección 3D.
         */
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

        /*
         * Agregamos la parábola vertical.
         */
        Vector3 launchDirection =
            (
                planarDirection +
                Vector3.up * upwardArc
            ).normalized;

        return launchDirection * launchSpeed;
    }

    /// <summary>
    /// Limita la desviación horizontal del lanzamiento.
    ///
    /// Un tiro perfectamente frontal permanece frontal.
    /// Una desviación deliberada se respeta, pero nunca
    /// puede superar el ángulo máximo configurado.
    /// </summary>
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