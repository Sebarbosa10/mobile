using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Desafío 3: contrarreloj. Encestar una cantidad fija de veces antes
/// de que se acabe el tiempo gana; que el tiempo llegue a cero sin
/// llegar a la meta pierde. A diferencia del modo endless (que es por
/// récord y no tiene final), acá hay una meta concreta y un límite de
/// tiempo fijo.
/// </summary>
public sealed class TimedTargetChallengeController : MonoBehaviour
{
    [SerializeField]
    private string menuSceneName = "MainMenu";

    [SerializeField]
    private BallLauncher ballLauncher;

    [SerializeField]
    private HoopScoreTrigger[] hoops = System.Array.Empty<HoopScoreTrigger>();

    [SerializeField, Min(1)]
    private int targetScore = 8;

    [SerializeField, Min(1f)]
    private float timeLimit = 20f;

    private ChallengeHud hud;
    private PauseMenu pauseMenu;
    private int score;
    private float remainingTime;
    private bool isFinished;

    private void Awake()
    {
        hud = GetComponent<ChallengeHud>();

        if (hud == null)
            hud = gameObject.AddComponent<ChallengeHud>();

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

        foreach (HoopScoreTrigger hoop in hoops)
            if (hoop != null)
                hoop.Scored += HandleHoopScored;
    }

    private void OnDisable()
    {
        hud.RestartRequested -= HandleRestartRequested;
        hud.ExitRequested -= HandleExitRequested;

        pauseMenu.RestartRequested -= HandleRestartRequested;
        pauseMenu.ExitRequested -= HandleExitRequested;

        foreach (HoopScoreTrigger hoop in hoops)
            if (hoop != null)
                hoop.Scored -= HandleHoopScored;
    }

    private void Start()
    {
        StartChallenge();
    }

    private void Update()
    {
        if (isFinished)
            return;

        remainingTime -= Time.deltaTime;

        hud.ShowTimer(Mathf.Max(remainingTime, 0f));

        if (remainingTime <= 0f)
        {
            LoseChallenge();
        }
    }

    private void HandleHoopScored(HoopScoreTrigger hoop)
    {
        if (isFinished)
            return;

        score++;

        hud.SetProgress("Puntos", score, targetScore);

        if (score >= targetScore)
        {
            WinChallenge();
        }
    }

    private void WinChallenge()
    {
        isFinished = true;

        FreezeBall();

        hud.ShowResult(true, $"Encestaste {score} de {targetScore} a tiempo");
    }

    private void LoseChallenge()
    {
        isFinished = true;
        remainingTime = 0f;

        FreezeBall();

        hud.ShowResult(false, $"Llegaste a {score} de {targetScore}");
    }

    private void FreezeBall()
    {
        pauseMenu.SetAvailable(false);

        if (ballLauncher == null)
            return;

        ballLauncher.ForceReset();
        ballLauncher.enabled = false;
    }

    private void HandleRestartRequested()
    {
        pauseMenu.ClearPause();

        StartChallenge();
    }

    private void HandleExitRequested()
    {
        pauseMenu.ClearPause();

        SceneManager.LoadScene(menuSceneName);
    }

    private void StartChallenge()
    {
        isFinished = false;
        score = 0;
        remainingTime = timeLimit;

        hud.SetProgress("Puntos", 0, targetScore);
        hud.ShowTimer(timeLimit);
        hud.HideResult();
        pauseMenu.SetAvailable(true);

        if (ballLauncher != null)
        {
            ballLauncher.ForceReset();
            ballLauncher.enabled = true;
        }
    }
}
