using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Desafío 1: varios aros girando en círculo. Hay que encestar en cada
/// uno (una sola vez cada uno) para ganar. No hay límite de tiempo ni
/// puntaje de por medio: es todo o nada.
/// </summary>
public sealed class RingChallengeController : MonoBehaviour
{
    [SerializeField]
    private string menuSceneName = "MainMenu";

    [SerializeField]
    private BallLauncher ballLauncher;

    [SerializeField]
    private HoopScoreTrigger[] hoops = System.Array.Empty<HoopScoreTrigger>();

    private static readonly Color ScoredColor = new(0.25f, 0.85f, 0.35f);

    private readonly HashSet<HoopScoreTrigger> scoredHoops = new();
    private readonly Dictionary<HoopScoreTrigger, Color> originalColors = new();

    private ChallengeHud hud;
    private PauseMenu pauseMenu;
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

        foreach (HoopScoreTrigger hoop in hoops)
        {
            if (hoop == null)
                continue;

            Renderer hoopRenderer = hoop.GetComponent<Renderer>();

            if (hoopRenderer != null)
                originalColors[hoop] = hoopRenderer.material.color;
        }
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

        if (!scoredHoops.Add(hoop))
            return;

        TintHoop(hoop, ScoredColor);

        hud.SetProgress("Aros", scoredHoops.Count, hoops.Length);

        if (scoredHoops.Count >= hoops.Length)
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
        hud.ShowResult(true, "¡Encestaste los 5 aros!");
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
        scoredHoops.Clear();

        foreach (HoopScoreTrigger hoop in hoops)
        {
            if (hoop != null && originalColors.TryGetValue(hoop, out Color original))
                TintHoop(hoop, original);
        }

        hud.SetProgress("Aros", 0, hoops.Length);
        hud.HideResult();
        pauseMenu.SetAvailable(true);

        if (ballLauncher != null)
        {
            ballLauncher.ForceReset();
            ballLauncher.enabled = true;
        }
    }

    private static void TintHoop(HoopScoreTrigger hoop, Color color)
    {
        Renderer hoopRenderer = hoop.GetComponent<Renderer>();

        if (hoopRenderer != null)
            hoopRenderer.material.color = color;
    }
}
