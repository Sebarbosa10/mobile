using UnityEngine;

public sealed class HoopController : MonoBehaviour
{
    [Header("Configuration")]
    [SerializeField]
    private HoopDifficultyProfile difficultyProfile;

    [Header("Visual")]
    [SerializeField]
    private Transform visualRoot;

    [Header("Movement")]
    [SerializeField]
    private HoopMovement movement;

    private Vector3 basePosition;
    private Quaternion baseRotation;

    private void Awake()
    {
        basePosition = transform.position;
        baseRotation = transform.rotation;

        if (movement != null)
            movement.Initialize(basePosition, baseRotation);
    }

    public void ApplyScore(int score)
    {
        if (difficultyProfile == null)
            return;

        HoopStage stage =
            difficultyProfile.GetStageForScore(score);

        ApplyStage(stage);
    }

    private void ApplyStage(HoopStage stage)
    {
        transform.SetPositionAndRotation(
            basePosition + stage.positionOffset,
            baseRotation);

        ApplyScale(stage.scale);

        if (movement != null)
            movement.ApplyStage(stage);
    }

    private void ApplyScale(float scale)
    {
        if (visualRoot == null)
            return;

        visualRoot.localScale = Vector3.one * Mathf.Max(0.01f, scale);
    }
}