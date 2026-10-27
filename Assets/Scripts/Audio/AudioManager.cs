using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

// Punto único de entrada al audio. El resto del juego llama a estos métodos estáticos con nombres
// del juego (PlayThrow, PlayScore, SetTheme...); adentro se postean los eventos de Wwise de AudioConfig.
// Se crea solo antes de cargar la primera escena y sobrevive a los cambios de escena.
public sealed class AudioManager : MonoBehaviour
{
    // Desactivados mientras se revisa la mezcla: con false no se inicia la música (incluye los
    // stingers) ni el ambiente. Volver a true para reactivarlos.
    private const bool MusicEnabled = false;
    private const bool AmbienceEnabled = false;
    private const bool HoopMoveLoopEnabled = true;

    public const string MusicVolumeKey = "Audio.MusicVolume";
    public const string SfxVolumeKey = "Audio.SfxVolume";

    private const float MinImpactSpeed = 0.6f;
    private const float ImpactSpeedForMaxVolume = 12f;
    private const float ImpactCooldown = 0.06f;
    private const float StartRetrySeconds = 10f;
    private const float AmbienceBankUnloadDelay = 1.5f;

    private static AudioManager instance;

    private AudioConfig config;

    private readonly HashSet<Object> movingHoops = new();
    private readonly Dictionary<BallSurface, float> lastImpactTime = new();

    private int scoreStreak;
    private bool ballAirPlaying;
    private bool carouselPlaying;
    private bool ambienceStarted;
    private string currentThemeId;
    private AK.Wwise.Bank currentAmbienceBank;

    public static float MusicVolume
    {
        get => PlayerPrefs.GetFloat(MusicVolumeKey, 1f);
        set
        {
            PlayerPrefs.SetFloat(MusicVolumeKey, Mathf.Clamp01(value));
            instance?.ApplyVolumes();
        }
    }

    public static float SfxVolume
    {
        get => PlayerPrefs.GetFloat(SfxVolumeKey, 1f);
        set
        {
            PlayerPrefs.SetFloat(SfxVolumeKey, Mathf.Clamp01(value));
            instance?.ApplyVolumes();
        }
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap()
    {
        AudioConfig audioConfig = AudioConfig.Instance;

        if (audioConfig == null)
        {
            Debug.LogWarning("[AudioManager] Falta Resources/AudioConfig. Usá Tools > Audio > Vincular objetos de Wwise.");
            return;
        }

        EnsureSoundEngine(audioConfig);

        GameObject root = new("AudioManager");
        DontDestroyOnLoad(root);

        // Todos los sonidos son 2D: el listener está en el mismo objeto que los emite
        root.AddComponent<AkAudioListener>();

        instance = root.AddComponent<AudioManager>();
        instance.config = audioConfig;
    }

    // Si la escena no tiene WwiseGlobal (por ejemplo al dar Play desde Challenges en el editor),
    // crea un AkInitializer con la configuración del proyecto. Si después aparece el de la escena,
    // ese se descarta solo porque ya existe una instancia.
    private static void EnsureSoundEngine(AudioConfig audioConfig)
    {
        if (FindAnyObjectByType<AkInitializer>() != null || audioConfig.wwiseInitializationSettings == null)
            return;

        GameObject wwise = new("WwiseGlobal (AudioManager)");
        wwise.SetActive(false);

        AkInitializer initializer = wwise.AddComponent<AkInitializer>();
        typeof(AkInitializer).GetField(nameof(AkInitializer.InitializationSettings))
            ?.SetValue(initializer, audioConfig.wwiseInitializationSettings);

        wwise.SetActive(true);
    }

    private void Awake()
    {
        SceneManager.sceneLoaded += HandleSceneLoaded;
    }

    private void Start()
    {
        config.Main.Load();
        config.Music.Load();

        ApplyVolumes();
        SetTheme(SkinProgress.EquippedThemeId);

        if (MusicEnabled)
            StartCoroutine(StartWhenLoaded(config.Play_Music, () => { }));
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;

        if (instance == this)
            instance = null;
    }

    private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Los loops de la escena anterior no tienen que seguir sonando
        StopBallAir();
        movingHoops.Clear();
        config.Stop_hoop_move_loop.Post(gameObject);
        SetCarouselRunning(false);
    }

    // Los bancos se cargan de forma asíncrona (Addressables): reintenta hasta que el evento exista
    private IEnumerator StartWhenLoaded(AK.Wwise.Event evt, System.Action started)
    {
        float giveUp = Time.realtimeSinceStartup + StartRetrySeconds;

        while (Time.realtimeSinceStartup < giveUp)
        {
            if (evt.Post(gameObject) != AkSoundEngine.AK_INVALID_PLAYING_ID)
            {
                started();
                yield break;
            }

            yield return new WaitForSecondsRealtime(0.25f);
        }

        Debug.LogWarning($"[AudioManager] No se pudo iniciar {evt.Name}: ¿se generaron y cargaron los bancos?");
    }

    private void ApplyVolumes()
    {
        config.MusicVolume.SetGlobalValue(MusicVolume * 100f);
        config.SFXVolume.SetGlobalValue(SfxVolume * 100f);
    }

    private void Post(AK.Wwise.Event evt)
    {
        evt?.Post(gameObject);
    }

    // ------------------------------------------------------------------ pelota

    public static void PlayGrab() =>
        instance?.Post(instance.config.Play_ball_grab);

    public static void PlayThrow(float force01)
    {
        if (instance == null)
            return;

        instance.config.ThrowForce.SetGlobalValue(Mathf.Clamp01(force01));
        instance.Post(instance.config.Play_ball_throw);
        instance.StartBallAir();
    }

    private void StartBallAir()
    {
        if (ballAirPlaying)
            return;

        ballAirPlaying = true;
        Post(config.Play_ball_air);
    }

    public static void StopBallAir()
    {
        if (instance == null || !instance.ballAirPlaying)
            return;

        instance.ballAirPlaying = false;
        instance.Post(instance.config.Stop_ball_air);
    }

    public static void PlayImpact(BallSurface surface, float speed)
    {
        if (instance == null || speed < MinImpactSpeed)
            return;

        // Evita ráfagas cuando la pelota roza varios colliders del mismo objeto en el mismo frame
        float now = Time.time;
        if (instance.lastImpactTime.TryGetValue(surface, out float last) && now - last < ImpactCooldown)
            return;

        instance.lastImpactTime[surface] = now;
        instance.config.ImpactVelocity.SetGlobalValue(Mathf.Clamp01(speed / ImpactSpeedForMaxVolume));
        instance.Post(instance.config.Impact(surface));
    }

    public static void PlayRespawn()
    {
        StopBallAir();
        instance?.Post(instance.config.Play_ball_respawn);
    }

    public static void PlayShakeColor() =>
        instance?.Post(instance?.config.Play_ball_shake_color);

    // ------------------------------------------------------------------ encestada

    public static void PlayScore(ScoreType type)
    {
        if (instance == null)
            return;

        // El pitch de score_plus_one sube un semitono por cada encestada seguida
        instance.config.ScoreStreak.SetGlobalValue(Mathf.Min(instance.scoreStreak, 10));
        instance.scoreStreak++;
        instance.Post(instance.config.Scored(type));
    }

    public static void PlayMiss()
    {
        if (instance == null)
            return;

        instance.scoreStreak = 0;
        instance.Post(instance.config.Play_miss_out);
    }

    public static void ResetStreak()
    {
        if (instance != null)
            instance.scoreStreak = 0;
    }

    public static void SetScore(int score) =>
        instance?.config.Score.SetGlobalValue(score);

    // ------------------------------------------------------------------ mecánicas

    public static void PlayTimerTick(bool urgent) =>
        instance?.Post(urgent ? instance.config.Play_timer_urgent : instance.config.Play_timer_tick);

    public static void SetTimerRemaining(float fraction01) =>
        instance?.config.TimerRemaining.SetGlobalValue(Mathf.Clamp01(fraction01));

    public static void PlayTimerReset() =>
        instance?.Post(instance?.config.Play_timer_reset);

    public static void PlayTimerOut() =>
        instance?.Post(instance?.config.Play_timer_out);

    public static void PlayHoopShrink() =>
        instance?.Post(instance?.config.Play_hoop_shrink);

    // Puede haber varios aros moviéndose: el loop suena mientras al menos uno se mueva
    public static void SetHoopMoving(Object hoop, bool moving)
    {
        if (!HoopMoveLoopEnabled || instance == null)
            return;

        bool wasMoving = instance.movingHoops.Count > 0;

        if (moving)
            instance.movingHoops.Add(hoop);
        else
            instance.movingHoops.Remove(hoop);

        bool isMoving = instance.movingHoops.Count > 0;

        if (isMoving && !wasMoving)
            instance.Post(instance.config.Play_hoop_move_loop);
        else if (!isMoving && wasMoving)
            instance.Post(instance.config.Stop_hoop_move_loop);
    }

    public static void SetCarouselRunning(bool running)
    {
        if (instance == null || instance.carouselPlaying == running)
            return;

        instance.carouselPlaying = running;
        instance.Post(running ? instance.config.Play_carousel_loop : instance.config.Stop_carousel_loop);
    }

    public static void PlayCarouselHoopDone(int hoopIndex)
    {
        if (instance == null)
            return;

        instance.config.CarouselHoopIndex.SetGlobalValue(Mathf.Clamp(hoopIndex, 1, 5));
        instance.Post(instance.config.Play_carousel_hoop_done);
    }

    public static void PlayEvasiveDash() =>
        instance?.Post(instance?.config.Play_evasive_dash);

    public static void PlayChallengeStart() =>
        instance?.Post(instance?.config.Play_challenge_start);

    // ------------------------------------------------------------------ UI

    public static void PlayUI(UISound sound) =>
        instance?.Post(instance?.config.Ui(sound));

    // ------------------------------------------------------------------ música, estados y fondos

    public static void SetMode(MusicMode mode) =>
        instance?.config.Mode(mode).SetValue();

    public static void SetPlayState(PlayState state) =>
        instance?.config.State(state).SetValue();

    public static void PlayStinger(Stinger stinger)
    {
        if (MusicEnabled)
            instance?.Post(instance?.config.StingerEvent(stinger));
    }

    // Cambia el ambiente al fondo equipado: carga su banco, cambia el State Theme (el switch hace
    // crossfade de 1 s) y descarga el banco anterior cuando terminó el fade.
    public static void SetTheme(string themeId)
    {
        if (!AmbienceEnabled || instance == null || themeId == instance.currentThemeId)
            return;

        if (!instance.config.TryGetTheme(themeId, out AK.Wwise.State state, out AK.Wwise.Bank bank))
        {
            Debug.LogWarning($"[AudioManager] No hay ambiente para el fondo '{themeId}'.");
            return;
        }

        AK.Wwise.Bank previous = instance.currentAmbienceBank;

        bank.Load();
        state.SetValue();

        instance.currentThemeId = themeId;
        instance.currentAmbienceBank = bank;

        // Con el ambiente ya sonando, el switch (modo continuo) cambia solo al State nuevo
        if (!instance.ambienceStarted)
            instance.StartCoroutine(instance.StartWhenLoaded(instance.config.Play_Ambience, () => instance.ambienceStarted = true));

        if (previous != null && previous != bank)
            instance.StartCoroutine(instance.UnloadLater(previous));
    }

    private IEnumerator UnloadLater(AK.Wwise.Bank bank)
    {
        yield return new WaitForSecondsRealtime(AmbienceBankUnloadDelay);

        if (bank != currentAmbienceBank)
            bank.Unload();
    }
}
