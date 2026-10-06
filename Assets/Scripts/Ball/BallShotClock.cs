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
/// termina la partida: la pelota se congela, el puntaje se compara
/// contra el récord guardado (PlayerPrefs), y el jugador reinicia desde
/// cero con el botón de la pantalla de Game Over.
///
/// La partida se puede pausar (ver PauseMenu). Reiniciar o volver al
/// menú desde la pausa también cuenta el puntaje para el récord.
/// </summary>
public sealed class BallShotClock : MonoBehaviour
{
    private const string HighScoreKey = "HighScore";

    [Header("Navegación")]
    [SerializeField]
    private string menuSceneName = "MainMenu";

    [Header("References")]
    [SerializeField]
    private BallLauncher ballLauncher;

    [SerializeField]
    private HoopController hoopController;

    [SerializeField]
    private SurvivalHud hud;

    [Header("Timing")]
    [Tooltip("Segundos disponibles para encestar antes de perder. Se reinicia con cada encestada.")]
    [SerializeField, Min(0.1f)]
    private float timeLimit = 7f;

    private PauseMenu pauseMenu;

    private float remainingTime;
    private bool isGameOver;
    private bool pendingGameOver;
    private int bestScore;

    public float RemainingTime =>
        remainingTime;

    public float TimeLimit =>
        timeLimit;

    private int CurrentScore =>
        hoopController != null ? hoopController.CurrentScore : 0;

    private void Awake()
    {
        bestScore = PlayerPrefs.GetInt(HighScoreKey, 0);

        pauseMenu = GetComponent<PauseMenu>();

        if (pauseMenu == null)
            pauseMenu = gameObject.AddComponent<PauseMenu>();

        pauseMenu.SetBallLauncher(ballLauncher);
    }

    private void OnEnable()
    {
        hud.RestartRequested += HandleRestartRequested;
        hud.ExitRequested += HandleExitRequested;

        pauseMenu.RestartRequested += HandleRestartRequested;
        pauseMenu.ExitRequested += HandleExitRequested;

        if (hoopController != null)
            hoopController.ShotScored += HandleShotScored;

        if (ballLauncher != null)
            ballLauncher.BallLanded += HandleBallLanded;
    }

    private void OnDisable()
    {
        hud.RestartRequested -= HandleRestartRequested;
        hud.ExitRequested -= HandleExitRequested;

        pauseMenu.RestartRequested -= HandleRestartRequested;
        pauseMenu.ExitRequested -= HandleExitRequested;

        if (hoopController != null)
            hoopController.ShotScored -= HandleShotScored;

        if (ballLauncher != null)
            ballLauncher.BallLanded -= HandleBallLanded;
    }

    private void Start()
    {
        StartRun();
    }

    private void Update()
    {
        hud.SetScore(CurrentScore);

        hud.SetRecord(bestScore);

        if (isGameOver || pauseMenu.IsPaused)
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

        hud.SetRemainingTime(
            Mathf.Max(remainingTime, 0f),
            timeLimit);
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
        pendingGameOver = false;
        remainingTime = timeLimit;

        ChallengeProgress.RegisterBasket();
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

    private void HandleRestartRequested()
    {
        if (!isGameOver && !pauseMenu.IsPaused)
            return;

        if (pauseMenu.IsPaused)
        {
            CommitScore();
            pauseMenu.ClearPause();
        }

        hoopController?.SetScore(0);

        StartRun();
    }

    private void HandleExitRequested()
    {
        if (!isGameOver && !pauseMenu.IsPaused)
            return;

        if (pauseMenu.IsPaused)
        {
            CommitScore();
            pauseMenu.ClearPause();
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

        hud.HideGameOver();
        pauseMenu.SetAvailable(true);
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

        int finalScore = CurrentScore;

        bool isNewRecord = CommitScore();

        pauseMenu.SetAvailable(false);
        hud.ShowGameOver(finalScore, bestScore, isNewRecord);
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
