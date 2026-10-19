using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

/// <summary>
/// IGameSession falso para probar la UI sin jugar. Se pone en un
/// GameObject de una escena vacía (con cámara): UIBootstrap lo detecta y
/// arma la UI de gameplay. Los eventos se disparan desde el menú
/// contextual del componente (clic derecho en el Inspector) o con el
/// teclado en Play:
///
///   E = encestada · T = reiniciar timer · G = Game Over · R = nuevo récord
///   V = victoria · D = derrota · L = lanzamiento (cierra el onboarding)
/// </summary>
public sealed class GameplayEventsStub : MonoBehaviour, IGameSession
{
    [SerializeField]
    private GameMode mode = GameMode.Endless;

    [SerializeField, Range(1, 3)]
    private int challengeNumber = 1;

    [SerializeField, Min(1)]
    private int challengeTarget = 5;

    [SerializeField, Min(0.1f)]
    private float timeLimit = 7f;

    [SerializeField]
    private Vector3 hoopPosition = new(0f, 3f, 8f);

    [SerializeField]
    private Vector3 ballPosition = new(0f, 1f, 0f);

    private int score;
    private int record = 24;
    private float remaining;
    private bool running;
    private bool paused;

    public event Action RunStarted;
    public event Action<ScoreEvent> Scored;
    public event Action<float, float> TimerChanged;
    public event Action ShotLaunched;
    public event Action<bool> PausedChanged;
    public event Action<GameOverInfo> GameOver;
    public event Action<int, int> ChallengeProgressChanged;
    public event Action<ChallengeEndInfo> ChallengeEnded;

    public GameMode Mode =>
        mode;

    public ChallengeDefinition Challenge =>
        mode == GameMode.Challenge ? ChallengeCatalog.Get(challengeNumber) : null;

    public int Record =>
        record;

    public Vector3 BallRestPosition =>
        ballPosition;

    public bool IsPaused =>
        paused;

    private void Start()
    {
        Restart();
    }

    private void Update()
    {
        Keyboard keyboard = Keyboard.current;

        if (keyboard != null)
        {
            if (keyboard.eKey.wasPressedThisFrame) SimulateScore();
            if (keyboard.tKey.wasPressedThisFrame) remaining = timeLimit;
            if (keyboard.gKey.wasPressedThisFrame) SimulateGameOver();
            if (keyboard.rKey.wasPressedThisFrame) SimulateNewRecord();
            if (keyboard.vKey.wasPressedThisFrame) SimulateVictory();
            if (keyboard.dKey.wasPressedThisFrame) SimulateDefeat();
            if (keyboard.lKey.wasPressedThisFrame) ShotLaunched?.Invoke();
        }

        if (!running || paused)
            return;

        remaining = Mathf.Max(0f, remaining - Time.deltaTime);
        TimerChanged?.Invoke(remaining, timeLimit);

        // El stub no pierde solo: el timer vuelve a empezar al llegar a cero.
        if (remaining <= 0f)
            remaining = timeLimit;
    }

    [ContextMenu("Simular encestada")]
    public void SimulateScore()
    {
        score++;
        remaining = timeLimit;

        Scored?.Invoke(new ScoreEvent(score, hoopPosition));

        if (mode == GameMode.Challenge)
            ChallengeProgressChanged?.Invoke(Mathf.Min(score, challengeTarget), challengeTarget);
    }

    [ContextMenu("Simular Game Over")]
    public void SimulateGameOver()
    {
        running = false;
        GameOver?.Invoke(new GameOverInfo(score, record, record, false));
    }

    [ContextMenu("Simular nuevo récord")]
    public void SimulateNewRecord()
    {
        running = false;
        GameOver?.Invoke(new GameOverInfo(record + 3, record + 3, record, true));
    }

    [ContextMenu("Simular victoria")]
    public void SimulateVictory()
    {
        running = false;
        ChallengeEnded?.Invoke(new ChallengeEndInfo(true, challengeTarget, challengeTarget, "Encestaste en los 5 aros"));
    }

    [ContextMenu("Simular derrota")]
    public void SimulateDefeat()
    {
        running = false;
        int reached = Mathf.Min(score, challengeTarget - 1);
        ChallengeEnded?.Invoke(new ChallengeEndInfo(false, reached, challengeTarget, $"Llegaste a {reached} de {challengeTarget}"));
    }

    public void Pause()
    {
        if (paused || !running)
            return;

        paused = true;
        Time.timeScale = 0f;
        PausedChanged?.Invoke(true);
    }

    public void Resume()
    {
        if (!paused)
            return;

        paused = false;
        Time.timeScale = 1f;
        PausedChanged?.Invoke(false);
    }

    public void Restart()
    {
        Resume();

        score = 0;
        remaining = timeLimit;
        running = true;

        RunStarted?.Invoke();
        TimerChanged?.Invoke(remaining, timeLimit);

        if (mode == GameMode.Challenge)
            ChallengeProgressChanged?.Invoke(0, challengeTarget);
    }

    public void ExitToMenu()
    {
        Resume();
        Debug.Log("[GameplayEventsStub] Volver al menú.");
        SceneManager.LoadScene("MainMenu");
    }

    private void OnDestroy()
    {
        Time.timeScale = 1f;
    }
}
