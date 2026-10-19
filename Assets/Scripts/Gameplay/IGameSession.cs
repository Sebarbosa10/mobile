using System;
using UnityEngine;

public enum GameMode
{
    Endless,
    Challenge
}

public readonly struct ScoreEvent
{
    public ScoreEvent(int score, Vector3 hoopPosition)
    {
        Score = score;
        HoopPosition = hoopPosition;
    }

    public int Score { get; }

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

    public string Summary { get; }
}

public interface IGameSession
{
    GameMode Mode { get; }

    ChallengeDefinition Challenge { get; }

    int Record { get; }

    Vector3 BallRestPosition { get; }

    bool IsPaused { get; }

    event Action RunStarted;

    event Action<ScoreEvent> Scored;

    event Action<float, float> TimerChanged;

    event Action ShotLaunched;

    event Action<bool> PausedChanged;

    event Action<GameOverInfo> GameOver;

    event Action<int, int> ChallengeProgressChanged;

    event Action<ChallengeEndInfo> ChallengeEnded;

    void Pause();

    void Resume();

    void Restart();

    void ExitToMenu();
}
