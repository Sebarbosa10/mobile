using UnityEngine;

// Referencias a los objetos de Wwise que usa el juego. Cada campo se llama igual que su objeto
// en Wwise (los States como Grupo_State): Tools > Audio > Vincular objetos de Wwise los completa
// leyendo Audio/Tools/wwise_objects.json, que exporta Audio/Tools/wwise_build.py.
[CreateAssetMenu(fileName = "AudioConfig", menuName = "Última Luz/Audio Config")]
public sealed class AudioConfig : ScriptableObject
{
    public const string ResourcePath = "AudioConfig";

    private static AudioConfig instance;

    public static AudioConfig Instance =>
        instance != null ? instance : instance = Resources.Load<AudioConfig>(ResourcePath);

    [Tooltip("AkWwiseInitializationSettings: se usa para crear el AkInitializer si la escena no tiene WwiseGlobal.")]
    public ScriptableObject wwiseInitializationSettings;

    [Header("Bancos")]
    public AK.Wwise.Bank Main;
    public AK.Wwise.Bank Music;
    public AK.Wwise.Bank Amb_Plaza;
    public AK.Wwise.Bank Amb_Azotea;
    public AK.Wwise.Bank Amb_Playa;
    public AK.Wwise.Bank Amb_Nevado;
    public AK.Wwise.Bank Amb_Desierto;
    public AK.Wwise.Bank Amb_Espacio;

    [Header("States")]
    public AK.Wwise.State GameMode_Menu;
    public AK.Wwise.State GameMode_Endless;
    public AK.Wwise.State GameMode_Challenge;
    public AK.Wwise.State GameMode_TimeAttack;
    public AK.Wwise.State GameState_Playing;
    public AK.Wwise.State GameState_Paused;
    public AK.Wwise.State GameState_Results;
    public AK.Wwise.State Theme_Plaza;
    public AK.Wwise.State Theme_Azotea;
    public AK.Wwise.State Theme_Playa;
    public AK.Wwise.State Theme_Nevado;
    public AK.Wwise.State Theme_Desierto;
    public AK.Wwise.State Theme_Espacio;

    [Header("Game Parameters")]
    public AK.Wwise.RTPC ThrowForce;
    public AK.Wwise.RTPC ImpactVelocity;
    public AK.Wwise.RTPC ScoreStreak;
    public AK.Wwise.RTPC TimerRemaining;
    public AK.Wwise.RTPC Score;
    public AK.Wwise.RTPC CarouselHoopIndex;
    public AK.Wwise.RTPC MusicVolume;
    public AK.Wwise.RTPC SFXVolume;

    [Header("Música y ambientes")]
    public AK.Wwise.Event Play_Music;
    public AK.Wwise.Event Stop_Music;
    public AK.Wwise.Event Play_Ambience;
    public AK.Wwise.Event Stop_Ambience;
    public AK.Wwise.Event Play_stg_nuevo_record;
    public AK.Wwise.Event Play_stg_victoria;
    public AK.Wwise.Event Play_stg_derrota;
    public AK.Wwise.Event Play_stg_game_over;

    [Header("Pelota")]
    public AK.Wwise.Event Play_ball_grab;
    public AK.Wwise.Event Play_ball_throw;
    public AK.Wwise.Event Play_ball_air;
    public AK.Wwise.Event Stop_ball_air;
    public AK.Wwise.Event Play_ball_bounce_floor;
    public AK.Wwise.Event Play_ball_bounce_soft;
    public AK.Wwise.Event Play_ball_hit_rim;
    public AK.Wwise.Event Play_ball_hit_backboard;
    public AK.Wwise.Event Play_ball_hit_pole;
    public AK.Wwise.Event Play_ball_respawn;
    public AK.Wwise.Event Play_ball_shake_color;

    [Header("Encestada")]
    public AK.Wwise.Event Play_Score_Clean;
    public AK.Wwise.Event Play_Score_Rim;
    public AK.Wwise.Event Play_Score_Backboard;
    public AK.Wwise.Event Play_miss_out;

    [Header("Mecánicas")]
    public AK.Wwise.Event Play_timer_tick;
    public AK.Wwise.Event Play_timer_urgent;
    public AK.Wwise.Event Play_timer_reset;
    public AK.Wwise.Event Play_timer_out;
    public AK.Wwise.Event Play_hoop_shrink;
    public AK.Wwise.Event Play_hoop_move_loop;
    public AK.Wwise.Event Stop_hoop_move_loop;
    public AK.Wwise.Event Play_carousel_loop;
    public AK.Wwise.Event Stop_carousel_loop;
    public AK.Wwise.Event Play_carousel_hoop_done;
    public AK.Wwise.Event Play_evasive_dash;
    public AK.Wwise.Event Play_challenge_start;

    [Header("UI")]
    public AK.Wwise.Event Play_ui_tap;
    public AK.Wwise.Event Play_ui_tap_primary;
    public AK.Wwise.Event Play_ui_back;
    public AK.Wwise.Event Play_ui_disabled;
    public AK.Wwise.Event Play_ui_tab;
    public AK.Wwise.Event Play_ui_select;
    public AK.Wwise.Event Play_ui_equip;
    public AK.Wwise.Event Play_ui_pause_in;
    public AK.Wwise.Event Play_ui_pause_out;
    public AK.Wwise.Event Play_ui_panel_in;
    public AK.Wwise.Event Play_ui_count_up;
    public AK.Wwise.Event Play_ui_progress_fill;
    public AK.Wwise.Event Play_ui_unlock;
    public AK.Wwise.Event Play_ui_confetti;
    public AK.Wwise.Event Play_ui_onboarding;

    public AK.Wwise.Event Ui(UISound sound) => sound switch
    {
        UISound.TapPrimary => Play_ui_tap_primary,
        UISound.Back => Play_ui_back,
        UISound.Disabled => Play_ui_disabled,
        UISound.Tab => Play_ui_tab,
        UISound.Select => Play_ui_select,
        UISound.Equip => Play_ui_equip,
        UISound.PauseIn => Play_ui_pause_in,
        UISound.PauseOut => Play_ui_pause_out,
        UISound.PanelIn => Play_ui_panel_in,
        UISound.CountUp => Play_ui_count_up,
        UISound.ProgressFill => Play_ui_progress_fill,
        UISound.Unlock => Play_ui_unlock,
        UISound.Confetti => Play_ui_confetti,
        UISound.Onboarding => Play_ui_onboarding,
        _ => Play_ui_tap
    };

    public AK.Wwise.Event Impact(BallSurface surface) => surface switch
    {
        BallSurface.Soft => Play_ball_bounce_soft,
        BallSurface.Rim => Play_ball_hit_rim,
        BallSurface.Backboard => Play_ball_hit_backboard,
        BallSurface.Pole => Play_ball_hit_pole,
        _ => Play_ball_bounce_floor
    };

    public AK.Wwise.Event Scored(ScoreType type) => type switch
    {
        ScoreType.Rim => Play_Score_Rim,
        ScoreType.Backboard => Play_Score_Backboard,
        _ => Play_Score_Clean
    };

    public AK.Wwise.Event StingerEvent(Stinger stinger) => stinger switch
    {
        Stinger.NewRecord => Play_stg_nuevo_record,
        Stinger.Victory => Play_stg_victoria,
        Stinger.Defeat => Play_stg_derrota,
        _ => Play_stg_game_over
    };

    public AK.Wwise.State Mode(MusicMode mode) => mode switch
    {
        MusicMode.Endless => GameMode_Endless,
        MusicMode.Challenge => GameMode_Challenge,
        MusicMode.TimeAttack => GameMode_TimeAttack,
        _ => GameMode_Menu
    };

    public AK.Wwise.State State(PlayState state) => state switch
    {
        PlayState.Paused => GameState_Paused,
        PlayState.Results => GameState_Results,
        _ => GameState_Playing
    };

    // Id de BackgroundTheme ("plaza") -> State y banco de su ambiente
    public bool TryGetTheme(string themeId, out AK.Wwise.State state, out AK.Wwise.Bank bank)
    {
        (state, bank) = themeId switch
        {
            "plaza" => (Theme_Plaza, Amb_Plaza),
            "azotea" => (Theme_Azotea, Amb_Azotea),
            "playa" => (Theme_Playa, Amb_Playa),
            "nevado" => (Theme_Nevado, Amb_Nevado),
            "desierto" => (Theme_Desierto, Amb_Desierto),
            "espacio" => (Theme_Espacio, Amb_Espacio),
            _ => (null, null)
        };

        return state != null;
    }
}
