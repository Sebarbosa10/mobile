using UnityEngine;

public sealed class TimedTargetChallengeController : ChallengeControllerBase
{
    [SerializeField, Min(1)]
    private int targetScore = 8;

    [SerializeField, Min(1f)]
    private float timeLimit = 20f;

    private int score;
    private float remainingTime;
    private bool waitingForLanding;

    protected override int ChallengeNumber => 3;

    protected override int CurrentProgress =>
        score;

    protected override int TargetProgress =>
        targetScore;

    protected override void OnEnable()
    {
        base.OnEnable();

        if (ballLauncher != null)
            ballLauncher.BallLanded += HandleBallLanded;
    }

    protected override void OnDisable()
    {
        base.OnDisable();

        if (ballLauncher != null)
            ballLauncher.BallLanded -= HandleBallLanded;
    }

    private void Update()
    {
        if (IsFinished || IsPaused || waitingForLanding)
            return;

        remainingTime -= Time.deltaTime;

        ReportTimer(Mathf.Max(remainingTime, 0f), timeLimit);

        if (remainingTime > 0f)
            return;

        if (ballLauncher != null && ballLauncher.IsLaunched)
        {
            waitingForLanding = true;
            return;
        }

        FinishOutOfTime();
    }

    private void HandleBallLanded()
    {
        if (waitingForLanding && !IsFinished)
            FinishOutOfTime();
    }

    private void FinishOutOfTime()
    {
        waitingForLanding = false;
        Finish(false, $"Llegaste a {score} de {targetScore}");
    }

    protected override void ResetChallengeState()
    {
        score = 0;
        remainingTime = timeLimit;
        waitingForLanding = false;

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
