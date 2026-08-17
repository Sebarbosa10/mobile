using System;
using UnityEngine;

public sealed class HoopProgression : MonoBehaviour
{
    [SerializeField]
    private HoopController hoopController;

    [SerializeField]
    private int currentScore;

    public int CurrentScore => currentScore;

    public event Action<int> ScoreChanged;

    public void AddPoint()
    {
        SetScore(currentScore + 1);
    }

    public void SetScore(int score)
    {
        int sanitizedScore = Mathf.Max(0, score);

        if (sanitizedScore == currentScore)
            return;

        currentScore = sanitizedScore;

        hoopController?.ApplyScore(currentScore);
        ScoreChanged?.Invoke(currentScore);
    }
}