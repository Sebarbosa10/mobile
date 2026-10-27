using UnityEngine;

public sealed class GameplayUI : MonoBehaviour
{
    private IGameSession session;
    private UIManager ui;

    private EndlessHudScreen endlessHud;
    private ChallengeHudScreen challengeHud;
    private PauseButton pauseButton;
    private PauseScreen pauseScreen;
    private GameOverScreen gameOverScreen;
    private ChallengeResultScreen resultScreen;
    private FeedbackLayer feedback;
    private OnboardingHint onboarding;

    private bool runEnded;

    private bool IsEndless =>
        session.Mode == GameMode.Endless;

    public static GameplayUI Create(IGameSession session)
    {
        UIManager ui = UIManager.Create("GameplayUI");

        GameplayUI gameplayUI = ui.gameObject.AddComponent<GameplayUI>();
        gameplayUI.ui = ui;
        gameplayUI.session = session;
        gameplayUI.Build();
        gameplayUI.Subscribe();

        return gameplayUI;
    }

    private void Build()
    {
        RectTransform root = ui.Root;
        RectTransform safe = UIKit.CreateSafeArea(root);

        if (IsEndless)
            endlessHud = ui.Register(EndlessHudScreen.Create(safe));
        else
            challengeHud = ui.Register(ChallengeHudScreen.Create(safe));

        pauseButton = PauseButton.Create(safe);
        UIKit.PlaceTopRight((RectTransform)pauseButton.transform, 40f, 8f, new Vector2(140f, 140f));
        pauseButton.OnClick.AddListener(session.Pause);

        feedback = FeedbackLayer.Create(root);
        onboarding = OnboardingHint.Create(root, feedback);

        pauseScreen = ui.Register(PauseScreen.Create(root));
        pauseScreen.ResumeRequested += session.Resume;
        pauseScreen.RestartRequested += session.Restart;
        pauseScreen.ExitRequested += session.ExitToMenu;

        if (IsEndless)
        {
            gameOverScreen = ui.Register(GameOverScreen.Create(root));
            gameOverScreen.RestartRequested += session.Restart;
            gameOverScreen.ExitRequested += session.ExitToMenu;
        }
        else
        {
            resultScreen = ui.Register(ChallengeResultScreen.Create(root));
            resultScreen.RestartRequested += session.Restart;
            resultScreen.ExitRequested += session.ExitToMenu;
        }
    }

    private void Subscribe()
    {
        session.RunStarted += HandleRunStarted;
        session.Scored += HandleScored;
        session.TimerChanged += HandleTimerChanged;
        session.ShotLaunched += HandleShotLaunched;
        session.PausedChanged += HandlePausedChanged;
        session.GameOver += HandleGameOver;
        session.ChallengeProgressChanged += HandleChallengeProgress;
        session.ChallengeEnded += HandleChallengeEnded;
    }

    private void OnDestroy()
    {
        if (session is Object sessionObject && sessionObject == null)
            return;

        session.RunStarted -= HandleRunStarted;
        session.Scored -= HandleScored;
        session.TimerChanged -= HandleTimerChanged;
        session.ShotLaunched -= HandleShotLaunched;
        session.PausedChanged -= HandlePausedChanged;
        session.GameOver -= HandleGameOver;
        session.ChallengeProgressChanged -= HandleChallengeProgress;
        session.ChallengeEnded -= HandleChallengeEnded;
    }

    private void HandleRunStarted()
    {
        runEnded = false;

        pauseScreen.Hide();
        gameOverScreen?.Hide();
        resultScreen?.Hide();
        pauseButton.gameObject.SetActive(true);

        if (IsEndless)
        {
            endlessHud.SetScore(0, pop: false);
            endlessHud.SetRecord(session.Record);
        }
        else
        {
            challengeHud.SetChallenge(session.Challenge);
        }

        onboarding.Show(session.BallRestPosition);
    }

    private void HandleScored(ScoreEvent score)
    {
        if (IsEndless)
            endlessHud.SetScore(score.Score, pop: true);

        UIPalette palette = UIKit.Palette;

        bool carousel = !IsEndless && session.Challenge.Number == 1;

        feedback.PlayScore(score.HoopPosition, carousel ? palette.success : palette.text);
    }

    private void HandleTimerChanged(float remaining, float total)
    {
        if (IsEndless)
            endlessHud.SetTimer(remaining, total);
        else
            challengeHud.SetTimer(remaining, total);
    }

    private void HandleShotLaunched()
    {
        onboarding.Complete();
    }

    private void HandlePausedChanged(bool paused)
    {
        if (paused)
            pauseScreen.Show();
        else
            pauseScreen.Hide();

        pauseButton.gameObject.SetActive(!paused && !runEnded);
    }

    private void HandleGameOver(GameOverInfo info)
    {
        runEnded = true;

        PlayUnlockSoundIf(SkinProgress.EvaluateUnlocks() > 0);

        pauseButton.gameObject.SetActive(false);
        endlessHud.SetRecord(info.Record);
        gameOverScreen.Show(info);
    }

    // El desbloqueo suena después del stinger de fin de partida
    private void PlayUnlockSoundIf(bool unlocked)
    {
        if (unlocked)
            StartCoroutine(PlayUnlockLater());
    }

    private static System.Collections.IEnumerator PlayUnlockLater()
    {
        yield return new WaitForSecondsRealtime(2.2f);
        AudioManager.PlayUI(UISound.Unlock);
    }

    private void HandleChallengeProgress(int current, int total)
    {
        challengeHud.SetProgress(current, total);
    }

    private void HandleChallengeEnded(ChallengeEndInfo info)
    {
        runEnded = true;

        PlayUnlockSoundIf(SkinProgress.EvaluateUnlocks() > 0);

        pauseButton.gameObject.SetActive(false);
        resultScreen.Show(session.Challenge, info);
    }
}
