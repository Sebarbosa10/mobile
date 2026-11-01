using UnityEngine;

// Desafíos de un solo aro: las reglas salen de ChallengeCatalog, así que una misma raíz de la escena
// sirve para varios desafíos.
public abstract class RuleChallengeController : ChallengeControllerBase
{
    private int score;
    private float remainingTime;
    private bool waitingForLanding;
    private bool scoredThisShot;

    protected override int ChallengeNumber =>
        ChallengeSelection.SelectedChallenge;

    protected override int CurrentProgress =>
        score;

    protected override int TargetProgress =>
        Challenge.TargetBaskets;

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
        float timeLimit = Challenge.TimeLimit;

        if (timeLimit <= 0f || IsFinished || IsPaused || waitingForLanding)
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

        FinishLost();
    }

    private void HandleBallLanded()
    {
        if (IsFinished)
            return;

        if (waitingForLanding || (Challenge.NoMisses && !scoredThisShot))
            FinishLost();

        scoredThisShot = false;
    }

    private void FinishLost()
    {
        waitingForLanding = false;
        Finish(false, $"Llegaste a {score} de {TargetProgress}");
    }

    protected override void ResetChallengeState()
    {
        score = 0;
        remainingTime = Challenge.TimeLimit;
        waitingForLanding = false;
        scoredThisShot = false;

        if (Challenge.TimeLimit > 0f)
            ReportTimer(remainingTime, Challenge.TimeLimit);
    }

    protected override void OnHoopScored(HoopScoreTrigger hoop)
    {
        score++;
        scoredThisShot = true;

        ReportProgress(hoop);

        if (score >= TargetProgress)
            Finish(true, $"Encestaste {score} de {TargetProgress}");
    }
}
