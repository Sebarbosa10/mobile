using UnityEngine;

public sealed class TimedTargetChallengeController : ChallengeControllerBase
{
    [SerializeField, Min(1)]
    private int targetScore = 8;

    [SerializeField, Min(1f)]
    private float timeLimit = 20f;

    private int score;
    private float remainingTime;

    protected override int ChallengeNumber => 3;

    protected override int CurrentProgress =>
        score;

    protected override int TargetProgress =>
        targetScore;

    private void Update()
    {
        if (IsFinished || IsPaused)
            return;

        remainingTime -= Time.deltaTime;

        ReportTimer(Mathf.Max(remainingTime, 0f), timeLimit);

        if (remainingTime <= 0f)
            Finish(false, $"Llegaste a {score} de {targetScore}");
    }

    protected override void ResetChallengeState()
    {
        score = 0;
        remainingTime = timeLimit;

        ReportTimer(remainingTime, timeLimit);
    }

    protected override void OnHoopScored(HoopScoreTrigger hoop)
    {
        score++;

        ReportProgress(hoop);

        if (score >= targetScore)
            Finish(true, $"Encestaste {score} de {targetScore} a tiempo");
    }
}
