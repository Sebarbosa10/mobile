using System;
using UnityEngine;

public enum UnlockType
{
    Default,

    Record,

    TotalBaskets,

    CompleteChallenge,

    CompleteAllChallenges
}

[Serializable]
public sealed class UnlockCondition
{
    public UnlockType type;

    [Min(0)]
    public int value;

    [Tooltip("Texto corto para las tarjetas bloqueadas.")]
    public string shortText;

    public static UnlockCondition Default() =>
        new() { type = UnlockType.Default };

    public static UnlockCondition Create(UnlockType type, int value, string shortText) =>
        new() { type = type, value = value, shortText = shortText };

    public bool IsMet()
    {
        switch (type)
        {
            case UnlockType.Default:
                return true;

            case UnlockType.Record:
                return BallShotClock.LoadRecord() >= value;

            case UnlockType.TotalBaskets:
                return ChallengeProgress.TotalBaskets >= value;

            case UnlockType.CompleteChallenge:
                return ChallengeProgress.IsCompleted(value);

            case UnlockType.CompleteAllChallenges:
                for (int number = 1; number <= ChallengeCatalog.Count; number++)
                {
                    if (!ChallengeProgress.IsCompleted(number))
                        return false;
                }

                return true;

            default:
                return false;
        }
    }
}
