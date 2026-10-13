using System;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Motor del modo "por récord": el jugador debe encestar antes de que se
/// agote un timer fijo. Cada encestada reinicia el timer; un tiro errado
/// no lo hace, así que la presión se acumula hasta la próxima encestada.
///
/// Si el timer llega a cero mientras la pelota ya está en el aire, el
/// tiro se deja resolver: si encesta, vale igual y el timer se reinicia
/// como cualquier otra encestada. Recién si esa pelota cae sin encestar
/// (o si el timer llega a cero sin que haya ningún tiro en curso)
/// termina la partida: la pelota se congela y el puntaje se compara
/// contra el récord guardado (PlayerPrefs).
///
/// No conoce la UI: implementa IGameSession y la UI escucha sus eventos.
/// La partida se puede pausar (ver GamePause). Reiniciar o volver al
/// menú desde la pausa también cuenta el puntaje para el récord.
/// </summary>
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

    // El endless no tiene progreso de desafío: estos eventos nunca se disparan.
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

    public float RemainingTime =>
        remainingTime;

    public float TimeLimit =>
        timeLimit;

    private int CurrentScore =>
        hoopController != null ? hoopController.CurrentScore : 0;

    /// <summary>Récord guardado, para pantallas que no tienen partida (menú).</summary>
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

    /// <summary>
    /// El timer llegó a cero. Si hay un tiro en el aire, lo dejamos
    /// resolver (puede seguir encestando); recién si no hay ningún
    /// tiro en curso la partida termina en el acto.
    /// </summary>
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
        // En mobile la app puede morir estando en background sin volver
        // a pasar por EndRun: guardamos el progreso de desbloqueo acá.
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

    /// <summary>
    /// Compara el puntaje actual con el récord, lo guarda si lo supera y
    /// persiste también el progreso de desbloqueo. Devuelve true si hubo
    /// récord nuevo.
    /// </summary>
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
