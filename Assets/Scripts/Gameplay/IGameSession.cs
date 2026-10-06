using System;
using UnityEngine;

public enum GameMode
{
    Endless,
    Challenge
}

/// <summary>Una encestada: el puntaje (o progreso) resultante y dónde ocurrió.</summary>
public readonly struct ScoreEvent
{
    public ScoreEvent(int score, Vector3 hoopPosition)
    {
        Score = score;
        HoopPosition = hoopPosition;
    }

    public int Score { get; }

    /// <summary>Posición en el mundo del aro donde se encestó (para los efectos).</summary>
    public Vector3 HoopPosition { get; }
}

public readonly struct GameOverInfo
{
    public GameOverInfo(int score, int record, int previousRecord, bool isNewRecord)
    {
        Score = score;
        Record = record;
        PreviousRecord = previousRecord;
        IsNewRecord = isNewRecord;
    }

    public int Score { get; }
    public int Record { get; }
    public int PreviousRecord { get; }
    public bool IsNewRecord { get; }
}

public readonly struct ChallengeEndInfo
{
    public ChallengeEndInfo(bool won, int current, int total, string summary)
    {
        Won = won;
        Current = current;
        Total = total;
        Summary = summary;
    }

    public bool Won { get; }
    public int Current { get; }
    public int Total { get; }

    /// <summary>Resumen para la pantalla de resultado, por ejemplo "Llegaste a 5 de 8".</summary>
    public string Summary { get; }
}

/// <summary>
/// Contrato entre el gameplay y la UI. El gameplay (BallShotClock en el
/// endless, ChallengeControllerBase en los desafíos) lo implementa y
/// dispara los eventos; la UI se suscribe y nunca al revés: el gameplay
/// no conoce ningún componente de UI.
///
/// Los comandos (Pause, Restart, etc.) son lo único que la UI le pide al
/// gameplay, en respuesta a botones.
/// </summary>
public interface IGameSession
{
    GameMode Mode { get; }

    /// <summary>Definición del desafío en curso (null en el endless).</summary>
    ChallengeDefinition Challenge { get; }

    /// <summary>Récord guardado del endless (al inicio de la partida).</summary>
    int Record { get; }

    /// <summary>Posición de reposo de la pelota (para el onboarding).</summary>
    Vector3 BallRestPosition { get; }

    bool IsPaused { get; }

    /// <summary>Arrancó (o se reinició) una partida o desafío.</summary>
    event Action RunStarted;

    /// <summary>OnScore: se encestó.</summary>
    event Action<ScoreEvent> Scored;

    /// <summary>OnTimerChanged: (restante, total) en segundos.</summary>
    event Action<float, float> TimerChanged;

    /// <summary>La pelota se lanzó (el jugador soltó el swipe).</summary>
    event Action ShotLaunched;

    event Action<bool> PausedChanged;

    /// <summary>OnGameOver: terminó una partida del endless.</summary>
    event Action<GameOverInfo> GameOver;

    /// <summary>OnChallengeProgress: (actual, total) del desafío.</summary>
    event Action<int, int> ChallengeProgressChanged;

    /// <summary>OnChallengeEnd: el desafío terminó (ganado o perdido).</summary>
    event Action<ChallengeEndInfo> ChallengeEnded;

    void Pause();

    void Resume();

    void Restart();

    void ExitToMenu();
}
