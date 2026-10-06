using UnityEngine;

/// <summary>
/// Progreso de desbloqueo de los desafíos. Cada encestada en el modo
/// endless suma al total acumulado (de todas las partidas, guardado en
/// PlayerPrefs), y cada BasketsPerUnlock encestadas se desbloquea el
/// siguiente desafío: 10 → Desafío 1, 20 → Desafío 2, etc. También
/// recuerda qué desafíos ya se completaron.
/// </summary>
public static class ChallengeProgress
{
    public const int BasketsPerUnlock = 10;

    private const string TotalBasketsKey = "EndlessTotalBaskets";
    private const string CompletedKeyPrefix = "ChallengeCompleted";

    public static int TotalBaskets =>
        PlayerPrefs.GetInt(TotalBasketsKey, 0);

    public static void RegisterBasket()
    {
        PlayerPrefs.SetInt(TotalBasketsKey, TotalBaskets + 1);
    }

    public static void Save()
    {
        PlayerPrefs.Save();
    }

    public static int RequiredBaskets(int challengeNumber) =>
        challengeNumber * BasketsPerUnlock;

    public static bool IsUnlocked(int challengeNumber) =>
        TotalBaskets >= RequiredBaskets(challengeNumber);

    public static bool IsCompleted(int challengeNumber) =>
        PlayerPrefs.GetInt(CompletedKeyPrefix + challengeNumber, 0) == 1;

    public static void MarkCompleted(int challengeNumber)
    {
        PlayerPrefs.SetInt(CompletedKeyPrefix + challengeNumber, 1);
        PlayerPrefs.Save();
    }
}
