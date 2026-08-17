using UnityEngine;

public enum HoopPositionMode
{
    Fixed,
    Depth,
    Horizontal,
    Diagonal
}

public enum HoopMovementMode
{
    Static,
    Moving
}

[CreateAssetMenu(
    fileName = "HoopDifficultyProfile",
    menuName = "Basketball/Hoop Difficulty Profile")]

public sealed class HoopDifficultyProfile : ScriptableObject
{
    [SerializeField]
    private HoopStage[] stages;

    public HoopStage GetStageForScore(int score)
    {
        if (stages == null || stages.Length == 0)
            return default;

        HoopStage selectedStage = stages[0];

        for (int i = 0; i < stages.Length; i++)
        {
            if (score < stages[i].requiredScore)
                break;

            selectedStage = stages[i];
        }

        return selectedStage;
    }
}

[System.Serializable]
public struct HoopStage
{
    [Min(0)]
    public int requiredScore;

    public HoopPositionMode positionMode;
    public HoopMovementMode movementMode;

    [Min(0f)]
    public float scale;

    [Min(0f)]
    public float movementAmplitude;

    [Min(0f)]
    public float movementSpeed;

    public Vector3 positionOffset;
}