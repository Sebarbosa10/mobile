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
        Vector3 position,
        Quaternion rotation)
    {
        basePosition = position;
        baseRotation = rotation;
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

        if (!movementEnabled)
        {
            transform.SetPositionAndRotation(
                basePosition + stage.positionOffset,
                baseRotation);
        }
    }

    private void Update()
    {
        if (!movementEnabled)
            return;

        elapsedTime += Time.deltaTime;

        Vector3 direction =
            GetMovementDirection(
                currentStage.positionMode);

        float offset =
            Mathf.Sin(
                elapsedTime * currentStage.movementSpeed)
            * currentStage.movementAmplitude;

        Vector3 position =
            basePosition
            + currentStage.positionOffset
            + direction * offset;

        transform.SetPositionAndRotation(
            position,
            baseRotation);
    }

    private static Vector3 GetMovementDirection(
        HoopPositionMode mode)
    {
        return mode switch
        {
            HoopPositionMode.Depth =>
                Vector3.forward,

            HoopPositionMode.Horizontal =>
                Vector3.right,

            HoopPositionMode.Diagonal =>
                (Vector3.right + Vector3.forward).normalized,

            _ => Vector3.zero
        };
    }
}