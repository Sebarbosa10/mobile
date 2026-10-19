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
