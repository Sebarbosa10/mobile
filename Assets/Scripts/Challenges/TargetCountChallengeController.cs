using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Desafío 2: encestar una cantidad fija de veces en un aro que se
/// mueve de forma esquiva (ver EvasiveHoopMover). No importa cuántos
/// tiros falles en el camino, solo cuenta cuántas veces encestaste.
/// </summary>
public sealed class TargetCountChallengeController : MonoBehaviour
{
    [SerializeField]
    private string menuSceneName = "MainMenu";

    [SerializeField]
    private BallLauncher ballLauncher;

    [SerializeField]
    private HoopScoreTrigger[] hoops = System.Array.Empty<HoopScoreTrigger>();

    [SerializeField, Min(1)]
    private int targetHits = 3;

    private ChallengeHud hud;
    private PauseMenu pauseMenu;
    private int hitCount;
    private bool isWon;

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

    private void HandleHoopScored(HoopScoreTrigger hoop)
    {
        if (isWon)
            return;

        hitCount++;

        hud.SetProgress("Encestadas", hitCount, targetHits);

        if (hitCount >= targetHits)
        {
            WinChallenge();
        }
    }

    private void WinChallenge()
    {
        isWon = true;

        if (ballLauncher != null)
        {
            ballLauncher.ForceReset();
            ballLauncher.enabled = false;
        }

        pauseMenu.SetAvailable(false);
        hud.ShowResult(true, $"Encestaste {targetHits} veces seguidas");
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
        isWon = false;
        hitCount = 0;

        hud.SetProgress("Encestadas", 0, targetHits);
        hud.HideResult();
        pauseMenu.SetAvailable(true);

        if (ballLauncher != null)
        {
            ballLauncher.ForceReset();
            ballLauncher.enabled = true;
        }
    }
}
