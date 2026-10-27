using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

// Conecta el audio a la sesión de juego de cada escena (endless o desafío): modo de música,
// estados de juego, timer, stingers y el carrusel. En el menú solo pone la música del menú.
public sealed class SessionAudio : MonoBehaviour
{
    private const float UrgentSeconds = 3f;
    private const float StingerDelay = 0.6f;

    private IGameSession session;
    private bool isTimeAttack;
    private bool hasCarousel;

    private int lastWholeSecond = -1;
    private float lastRemaining = -1f;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Register()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
        SceneManager.sceneLoaded += HandleSceneLoaded;
    }

    private static void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            foreach (MonoBehaviour behaviour in root.GetComponentsInChildren<MonoBehaviour>())
            {
                if (behaviour is IGameSession found && behaviour.isActiveAndEnabled)
                {
                    behaviour.gameObject.AddComponent<SessionAudio>().Bind(found, scene);
                    return;
                }
            }

            if (root.GetComponentInChildren<MainMenuController>() != null)
            {
                AudioManager.SetMode(MusicMode.Menu);
                AudioManager.SetPlayState(PlayState.Playing);
                return;
            }
        }
    }

    private void Bind(IGameSession gameSession, Scene scene)
    {
        session = gameSession;
        isTimeAttack = session is TimedTargetChallengeController;
        hasCarousel = FindInScene<ChallengeCarousel>(scene) != null;

        AudioManager.SetMode(session.Mode == GameMode.Endless ? MusicMode.Endless
            : isTimeAttack ? MusicMode.TimeAttack : MusicMode.Challenge);

        session.RunStarted += HandleRunStarted;
        session.Scored += HandleScored;
        session.TimerChanged += HandleTimerChanged;
        session.PausedChanged += HandlePausedChanged;
        session.GameOver += HandleGameOver;
        session.ChallengeProgressChanged += HandleChallengeProgress;
        session.ChallengeEnded += HandleChallengeEnded;

        // La sesión arranca en su Start, que corre después de este bind
    }

    private void OnDestroy()
    {
        if (session == null)
            return;

        session.RunStarted -= HandleRunStarted;
        session.Scored -= HandleScored;
        session.TimerChanged -= HandleTimerChanged;
        session.PausedChanged -= HandlePausedChanged;
        session.GameOver -= HandleGameOver;
        session.ChallengeProgressChanged -= HandleChallengeProgress;
        session.ChallengeEnded -= HandleChallengeEnded;

        AudioManager.SetCarouselRunning(false);
    }

    private void HandleRunStarted()
    {
        lastWholeSecond = -1;
        lastRemaining = -1f;

        AudioManager.ResetStreak();
        AudioManager.SetScore(0);
        AudioManager.SetPlayState(PlayState.Playing);

        if (session.Mode == GameMode.Challenge)
            AudioManager.PlayChallengeStart();

        AudioManager.SetCarouselRunning(hasCarousel);
    }

    private void HandleScored(ScoreEvent score)
    {
        if (session.Mode == GameMode.Endless)
            AudioManager.SetScore(score.Score);
    }

    private void HandleTimerChanged(float remaining, float total)
    {
        if (session.IsPaused)
            return;

        AudioManager.SetTimerRemaining(total > 0f ? remaining / total : 0f);

        // El timer volvió a llenarse (en endless, después de encestar)
        if (lastRemaining >= 0f && remaining > lastRemaining + 0.5f)
        {
            AudioManager.PlayTimerReset();
            lastWholeSecond = Mathf.CeilToInt(remaining);
        }

        lastRemaining = remaining;

        // Un tic por segundo entero; los últimos segundos con el sonido urgente
        int wholeSecond = Mathf.CeilToInt(remaining);

        if (lastWholeSecond < 0)
        {
            lastWholeSecond = wholeSecond;
            return;
        }

        if (wholeSecond < lastWholeSecond && remaining > 0f)
            AudioManager.PlayTimerTick(wholeSecond <= UrgentSeconds);

        lastWholeSecond = wholeSecond;
    }

    private void HandlePausedChanged(bool paused)
    {
        AudioManager.SetPlayState(paused ? PlayState.Paused : PlayState.Playing);
        AudioManager.PlayUI(paused ? UISound.PauseIn : UISound.PauseOut);
    }

    private void HandleGameOver(GameOverInfo info)
    {
        AudioManager.SetPlayState(PlayState.Results);
        AudioManager.StopBallAir();
        AudioManager.PlayTimerOut();

        StartCoroutine(StingerLater(info.IsNewRecord ? Stinger.NewRecord : Stinger.GameOver));
    }

    private void HandleChallengeProgress(int current, int total)
    {
        if (hasCarousel && current > 0)
            AudioManager.PlayCarouselHoopDone(current);
    }

    private void HandleChallengeEnded(ChallengeEndInfo info)
    {
        AudioManager.SetPlayState(PlayState.Results);
        AudioManager.StopBallAir();
        AudioManager.SetCarouselRunning(false);

        if (isTimeAttack && !info.Won)
            AudioManager.PlayTimerOut();

        StartCoroutine(StingerLater(info.Won ? Stinger.Victory : Stinger.Defeat));
    }

    // El stinger entra un poco después del buzzer o del último sonido de juego para no pisarlos
    private static IEnumerator StingerLater(Stinger stinger)
    {
        yield return new WaitForSecondsRealtime(StingerDelay);
        AudioManager.PlayStinger(stinger);
    }

    private static T FindInScene<T>(Scene scene) where T : Component
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            T found = root.GetComponentInChildren<T>(true);

            if (found != null)
                return found;
        }

        return null;
    }
}
