using UnityEngine;

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
/// </summary>
public sealed class BallShotClock : MonoBehaviour
{
    private const string HighScoreKey = "HighScore";

    [Header("References")]
    [SerializeField]
    private BallLauncher ballLauncher;

    [SerializeField]
    private HoopController hoopController;

    [Header("Timing")]
    [Tooltip("Segundos disponibles para encestar antes de perder. Se reinicia con cada encestada.")]
    [SerializeField, Min(0.1f)]
    private float timeLimit = 7f;

    private SurvivalHud hud;

    private float remainingTime;
    private bool isGameOver;
    private bool pendingGameOver;
    private int bestScore;

    public float RemainingTime =>
        remainingTime;

    public float TimeLimit =>
        timeLimit;

    private void Awake()
    {
        hud = GetComponent<SurvivalHud>();

        if (hud == null)
            hud = gameObject.AddComponent<SurvivalHud>();

        bestScore = PlayerPrefs.GetInt(HighScoreKey, 0);
    }

    private void OnEnable()
    {
        hud.RestartRequested += HandleRestartRequested;

        if (hoopController != null)
            hoopController.ShotScored += HandleShotScored;

        if (ballLauncher != null)
            ballLauncher.BallLanded += HandleBallLanded;
    }

    private void OnDisable()
    {
        hud.RestartRequested -= HandleRestartRequested;

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
        hud.SetScore(
            hoopController != null ? hoopController.CurrentScore : 0);

        hud.SetRecord(bestScore);

        if (isGameOver)
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
    }

    private void HandleBallLanded()
    {
        if (!pendingGameOver)
            return;

        EndRun();
    }

    private void HandleRestartRequested()
    {
        if (!isGameOver)
            return;

        hoopController?.SetScore(0);

        StartRun();
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

        int finalScore =
            hoopController != null ? hoopController.CurrentScore : 0;

        bool isNewRecord = finalScore > bestScore;

        if (isNewRecord)
        {
            bestScore = finalScore;

            PlayerPrefs.SetInt(HighScoreKey, bestScore);
            PlayerPrefs.Save();
        }

        hud.ShowGameOver(finalScore, bestScore, isNewRecord);
    }
}
