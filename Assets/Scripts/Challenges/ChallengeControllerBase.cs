using System;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Base de los tres desafíos. Resuelve lo que comparten: pausa,
/// reinicio, salida al menú, congelar la pelota al terminar, marcar el
/// desafío como completado y exponer todo como IGameSession para la UI.
///
/// Cada desafío concreto solo define su número, cómo se reinicia su
/// estado y qué pasa en cada encestada (llamando a ReportProgress y, al
/// final, a Finish).
/// </summary>
public abstract class ChallengeControllerBase : MonoBehaviour, IGameSession
{
    [SerializeField]
    private string menuSceneName = "MainMenu";

    [SerializeField]
    protected BallLauncher ballLauncher;

    [SerializeField]
    protected HoopScoreTrigger[] hoops = Array.Empty<HoopScoreTrigger>();

    private GamePause pause;

    public event Action RunStarted;
    public event Action<ScoreEvent> Scored;
    public event Action<float, float> TimerChanged;
    public event Action ShotLaunched;
    public event Action<bool> PausedChanged;
    public event Action<int, int> ChallengeProgressChanged;
    public event Action<ChallengeEndInfo> ChallengeEnded;

    // Los desafíos no tienen Game Over de récord: nunca se dispara.
    public event Action<GameOverInfo> GameOver { add { } remove { } }

    /// <summary>1 = Carrusel, 2 = Aro esquivo, 3 = Contrarreloj.</summary>
    protected abstract int ChallengeNumber { get; }

    /// <summary>Progreso actual y meta, para el HUD.</summary>
    protected abstract int CurrentProgress { get; }
    protected abstract int TargetProgress { get; }

    protected bool IsFinished { get; private set; }

    public GameMode Mode =>
        GameMode.Challenge;

    public ChallengeDefinition Challenge =>
        ChallengeCatalog.Get(ChallengeNumber);

    public int Record =>
        BallShotClock.LoadRecord();

    public Vector3 BallRestPosition =>
        ballLauncher != null ? ballLauncher.RestPosition : Vector3.zero;

    public bool IsPaused =>
        pause.IsPaused;

    protected virtual void Awake()
    {
        pause = GetComponent<GamePause>();

        if (pause == null)
            pause = gameObject.AddComponent<GamePause>();

        pause.SetBallLauncher(ballLauncher);
    }

    protected virtual void OnEnable()
    {
        pause.PausedChanged += HandlePausedChanged;

        foreach (HoopScoreTrigger hoop in hoops)
            if (hoop != null)
                hoop.Scored += HandleHoopScored;

        if (ballLauncher != null)
            ballLauncher.Launched += HandleLaunched;
    }

    protected virtual void OnDisable()
    {
        pause.PausedChanged -= HandlePausedChanged;

        foreach (HoopScoreTrigger hoop in hoops)
            if (hoop != null)
                hoop.Scored -= HandleHoopScored;

        if (ballLauncher != null)
            ballLauncher.Launched -= HandleLaunched;
    }

    private void Start()
    {
        StartChallenge();
    }

    /// <summary>Vuelve el estado propio del desafío a cero.</summary>
    protected abstract void ResetChallengeState();

    /// <summary>Se encestó en uno de los aros del desafío.</summary>
    protected abstract void OnHoopScored(HoopScoreTrigger hoop);

    private void HandleHoopScored(HoopScoreTrigger hoop)
    {
        if (IsFinished)
            return;

        OnHoopScored(hoop);
    }

    private void HandleLaunched()
    {
        ShotLaunched?.Invoke();
    }

    private void HandlePausedChanged(bool paused)
    {
        PausedChanged?.Invoke(paused);
    }

    /// <summary>Avisa a la UI de una encestada que sumó progreso.</summary>
    protected void ReportProgress(HoopScoreTrigger hoop)
    {
        Vector3 hoopPosition =
            hoop != null ? hoop.transform.position : Vector3.zero;

        Scored?.Invoke(new ScoreEvent(CurrentProgress, hoopPosition));
        ChallengeProgressChanged?.Invoke(CurrentProgress, TargetProgress);
    }

    protected void ReportTimer(float remaining, float total)
    {
        TimerChanged?.Invoke(remaining, total);
    }

    /// <summary>Termina el desafío: congela la pelota y avisa el resultado.</summary>
    protected void Finish(bool won, string summary)
    {
        if (IsFinished)
            return;

        IsFinished = true;

        if (ballLauncher != null)
        {
            ballLauncher.ForceReset();
            ballLauncher.enabled = false;
        }

        pause.SetAvailable(false);

        if (won)
            ChallengeProgress.MarkCompleted(ChallengeNumber);

        ChallengeEnded?.Invoke(
            new ChallengeEndInfo(won, CurrentProgress, TargetProgress, summary));
    }

    private void StartChallenge()
    {
        IsFinished = false;

        ResetChallengeState();

        if (ballLauncher != null)
        {
            ballLauncher.ForceReset();
            ballLauncher.enabled = true;
        }

        pause.SetAvailable(true);

        RunStarted?.Invoke();
        ChallengeProgressChanged?.Invoke(CurrentProgress, TargetProgress);
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
        pause.ClearPause();

        StartChallenge();
    }

    public void ExitToMenu()
    {
        pause.ClearPause();

        SceneManager.LoadScene(menuSceneName);
    }
}
