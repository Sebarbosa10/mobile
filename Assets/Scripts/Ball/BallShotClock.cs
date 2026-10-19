using System;
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class BallShotClock : MonoBehaviour, IGameSession
{
    public const string HighScoreKey = "HighScore";

    [Header("Navegación")]
    [SerializeField]
    private string menuSceneName = "MainMenu";

    [Header("References")]
    [SerializeField]
    private BallLauncher ballLauncher;

    [SerializeField]
    private HoopController hoopController;

    [Header("Timing")]
    [Tooltip("Segundos disponibles para encestar antes de perder. Se reinicia con cada encestada.")]
    [SerializeField, Min(0.1f)]
    private float timeLimit = 7f;

    private GamePause pause;

    private float remainingTime;
    private bool isGameOver;
    private bool pendingGameOver;
    private int bestScore;

    public event Action RunStarted;
    public event Action<ScoreEvent> Scored;
    public event Action<float, float> TimerChanged;
    public event Action ShotLaunched;
    public event Action<bool> PausedChanged;
    public event Action<GameOverInfo> GameOver;

    public event Action<int, int> ChallengeProgressChanged { add { } remove { } }
    public event Action<ChallengeEndInfo> ChallengeEnded { add { } remove { } }

    public GameMode Mode =>
        GameMode.Endless;

    public ChallengeDefinition Challenge =>
        null;

    public int Record =>
        bestScore;

    public Vector3 BallRestPosition =>
        ballLauncher != null ? ballLauncher.RestPosition : Vector3.zero;

    public bool IsPaused =>
        pause.IsPaused;

    private int CurrentScore =>
        hoopController != null ? hoopController.CurrentScore : 0;

    public static int LoadRecord() =>
        PlayerPrefs.GetInt(HighScoreKey, 0);

    private void Awake()
    {
        bestScore = LoadRecord();

        pause = GetComponent<GamePause>();

        if (pause == null)
            pause = gameObject.AddComponent<GamePause>();

        pause.SetBallLauncher(ballLauncher);
    }

    private void OnEnable()
    {
        pause.PausedChanged += HandlePausedChanged;

        if (hoopController != null)
            hoopController.ShotScored += HandleShotScored;

        if (ballLauncher != null)
        {
            ballLauncher.BallLanded += HandleBallLanded;
            ballLauncher.Launched += HandleLaunched;
        }
    }

    private void OnDisable()
    {
        pause.PausedChanged -= HandlePausedChanged;

        if (hoopController != null)
            hoopController.ShotScored -= HandleShotScored;

        if (ballLauncher != null)
        {
            ballLauncher.BallLanded -= HandleBallLanded;
            ballLauncher.Launched -= HandleLaunched;
        }
    }

    private void Start()
    {
        StartRun();
    }

    private void Update()
    {
        if (isGameOver || pause.IsPaused)
            return;

        if (!pendingGameOver)
        {
            remainingTime -= Time.deltaTime;

            if (remainingTime <= 0f)
            {
                remainingTime = 0f;
                HandleTimeExpired();
            }
        }

        TimerChanged?.Invoke(Mathf.Max(remainingTime, 0f), timeLimit);
    }

    private void HandleTimeExpired()
    {
        bool shotInFlight =
            ballLauncher != null && ballLauncher.IsLaunched;

        if (shotInFlight)
        {
            pendingGameOver = true;
            return;
        }

        EndRun();
    }

    private void HandleShotScored()
    {
        if (isGameOver)
            return;

        pendingGameOver = false;
        remainingTime = timeLimit;

        ChallengeProgress.RegisterBasket();

        Vector3 hoopPosition =
            hoopController != null ? hoopController.RimPosition : Vector3.zero;

        Scored?.Invoke(new ScoreEvent(CurrentScore, hoopPosition));
    }

    private void HandleLaunched()
    {
        ShotLaunched?.Invoke();
    }

    private void HandlePausedChanged(bool paused)
    {
        PausedChanged?.Invoke(paused);
    }

    private void OnApplicationPause(bool paused)
    {
        if (paused)
            ChallengeProgress.Save();
    }

    private void HandleBallLanded()
    {
        if (!pendingGameOver)
            return;

        EndRun();
    }

    public void Pause()
    {
        pause.Pause();
    }

    public void Resume()
    {
        pause.Resume();
    }

    public void Restart()
    {
        if (!isGameOver && !pause.IsPaused)
            return;

        if (pause.IsPaused)
        {
            CommitScore();
            pause.ClearPause();
        }

        hoopController?.SetScore(0);

        StartRun();
    }

    public void ExitToMenu()
    {
        if (!isGameOver && !pause.IsPaused)
            return;

        if (pause.IsPaused)
        {
            CommitScore();
            pause.ClearPause();
        }

        SceneManager.LoadScene(menuSceneName);
    }

    private void StartRun()
    {
        isGameOver = false;
        pendingGameOver = false;
        remainingTime = timeLimit;

        if (ballLauncher != null)
        {
            ballLauncher.ForceReset();
            ballLauncher.enabled = true;
        }

        pause.SetAvailable(true);

        RunStarted?.Invoke();
        TimerChanged?.Invoke(remainingTime, timeLimit);
    }

    private void EndRun()
    {
        isGameOver = true;
        pendingGameOver = false;
        remainingTime = 0f;

        if (ballLauncher != null)
        {
            ballLauncher.ForceReset();
            ballLauncher.enabled = false;
        }

        int previousRecord = bestScore;
        bool isNewRecord = CommitScore();

        pause.SetAvailable(false);

        TimerChanged?.Invoke(0f, timeLimit);
        GameOver?.Invoke(new GameOverInfo(CurrentScore, bestScore, previousRecord, isNewRecord));
    }

    private bool CommitScore()
    {
        int finalScore = CurrentScore;

        bool isNewRecord = finalScore > bestScore;

        if (isNewRecord)
        {
            bestScore = finalScore;

            PlayerPrefs.SetInt(HighScoreKey, bestScore);
        }

        ChallengeProgress.Save();

        return isNewRecord;
    }
}
