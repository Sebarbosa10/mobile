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
            Vector3 direction =
                GetMovementDirection(
                    currentStage.positionMode);

            float movementOffset =
                Mathf.Sin(
                    elapsedTime *
                    currentStage.movementSpeed)
                *
                currentStage.movementAmplitude;

            position +=
                direction *
                movementOffset;
        }

        transform.SetPositionAndRotation(
            position,
            baseRotation);
    }

    private static Vector3 GetMovementDirection(
        HoopPositionMode mode)
    {
        switch (mode)
        {
            case HoopPositionMode.Depth:
                return Vector3.forward;

            case HoopPositionMode.Horizontal:
                return Vector3.right;

            case HoopPositionMode.Diagonal:
                return (
                    Vector3.right +
                    Vector3.forward
                ).normalized;

            default:
                return Vector3.zero;
        }
    }
}