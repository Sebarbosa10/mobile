using System;
using UnityEngine;

[CreateAssetMenu(fileName = "SkinDatabase", menuName = "Última Luz/Skin Database")]
public sealed class SkinDatabase : ScriptableObject
{
    public BallSkin[] balls = Array.Empty<BallSkin>();
    public BackgroundTheme[] themes = Array.Empty<BackgroundTheme>();

    [Tooltip("Sombra de la vista previa 3D (el prefab BallShadow de la escena).")]
    public GameObject previewShadowPrefab;

    private static SkinDatabase instance;

    public static SkinDatabase Instance
    {
        get
        {
            if (instance == null)
                instance = Resources.Load<SkinDatabase>("SkinDatabase");

            return instance;
        }
    }

    // Nombres de la pelota y el fondo que da un desafío, para mostrar el premio
    public static string ChallengeRewards(int challengeNumber)
    {
        if (Instance == null)
            return "";

        var names = new System.Collections.Generic.List<string>();

        foreach (BallSkin ball in Instance.balls)
        {
            if (ball != null && GrantedBy(ball.unlock, challengeNumber))
                names.Add(ball.displayName);
        }

        foreach (BackgroundTheme theme in Instance.themes)
        {
            if (theme != null && GrantedBy(theme.unlock, challengeNumber))
                names.Add(theme.displayName);
        }

        return string.Join(" y ", names);
    }

    private static bool GrantedBy(UnlockCondition unlock, int challengeNumber) =>
        unlock.type == UnlockType.CompleteChallenge && unlock.value == challengeNumber;

    public BallSkin DefaultBall =>
        balls.Length > 0 ? balls[0] : null;

    public BackgroundTheme DefaultTheme =>
        themes.Length > 0 ? themes[0] : null;

    public BallSkin GetBall(string id)
    {
        foreach (BallSkin ball in balls)
        {
            if (ball != null && ball.id == id)
                return ball;
        }

        return DefaultBall;
    }

    public BackgroundTheme GetTheme(string id)
    {
        foreach (BackgroundTheme theme in themes)
        {
            if (theme != null && theme.id == id)
                return theme;
        }

        return DefaultTheme;
    }
}
