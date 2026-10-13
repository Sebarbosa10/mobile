using UnityEngine;

public sealed class HoopMovement : MonoBehaviour
{
    private Vector3 basePosition;
    private Quaternion baseRotation;

    private HoopStage currentStage;

    private float elapsedTime;
    private bool initialized;
    private bool movementEnabled;

    public void Initialize(
        Vector3 initialPosition,
        Quaternion initialRotation)
    {
        basePosition = initialPosition;
        baseRotation = initialRotation;

        initialized = true;
    }

    public void ApplyStage(HoopStage stage)
    {
        if (!initialized)
            return;

        currentStage = stage;
        elapsedTime = 0f;

        movementEnabled =
            stage.movementMode == HoopMovementMode.Moving;

        // En etapas estáticas, Unity directamente deja de invocar Update en
        // este componente en vez de llamarlo cada frame solo para hacer un
        // early-return. Costo cero cuando el aro no se mueve.
        enabled = movementEnabled;

        ApplyCurrentPosition();
    }

    private void Update()
    {
        if (!movementEnabled)
            return;

        elapsedTime += Time.deltaTime;

        ApplyCurrentPosition();
    }

    private void ApplyCurrentPosition()
    {
        Vector3 position =
            basePosition +
            currentStage.positionOffset;

        if (movementEnabled)
        {
            position += GetMovementOffset(
                currentStage.positionMode,
                elapsedTime * currentStage.movementSpeed,
                currentStage.movementAmplitude);
        }

        transform.SetPositionAndRotation(
            position,
            baseRotation);
    }

    private static Vector3 GetMovementOffset(
        HoopPositionMode mode,
        float phase,
        float amplitude)
    {
        switch (mode)
        {
            case HoopPositionMode.Depth:
                return Vector3.forward * (Mathf.Sin(phase) * amplitude);

            case HoopPositionMode.Horizontal:
                return Vector3.right * (Mathf.Sin(phase) * amplitude);

            case HoopPositionMode.Diagonal:
                return (Vector3.right + Vector3.forward).normalized *
                    (Mathf.Sin(phase) * amplitude);

            case HoopPositionMode.Vertical:
                return Vector3.up * (Mathf.Sin(phase) * amplitude);

            case HoopPositionMode.Circular:
                /*
                 * Órbita real en el plano horizontal (no un vaivén de ida
                 * y vuelta): arranca en el offset cero (fase 0 -> seno 0,
                 * coseno-1 0) y traza un círculo completo de radio
                 * "amplitude" alrededor de la posición base.
                 */
                return new Vector3(
                    Mathf.Sin(phase) * amplitude,
                    0f,
                    (Mathf.Cos(phase) - 1f) * amplitude);

            default:
                return Vector3.zero;
        }
    }
}
