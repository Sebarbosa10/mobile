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
                return new Vector3(
                    Mathf.Sin(phase) * amplitude,
                    0f,
                    (Mathf.Cos(phase) - 1f) * amplitude);

            default:
                return Vector3.zero;
        }
    }
}
