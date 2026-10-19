using System.Collections.Generic;
using UnityEngine;

public static class SkinProgress
{
    private const string UnlockedKey = "Skins.Unlocked";
    private const string BallKey = "Skins.Ball";
    private const string ThemeKey = "Skins.Theme";

    private static HashSet<string> unlocked;

    private static HashSet<string> Unlocked
    {
        get
        {
            if (unlocked != null)
                return unlocked;

            unlocked = new HashSet<string>();

            foreach (string id in PlayerPrefs.GetString(UnlockedKey, "").Split(','))
            {
                if (!string.IsNullOrEmpty(id))
                    unlocked.Add(id);
            }

            return unlocked;
        }
    }

    public static bool IsUnlocked(BallSkin skin) =>
        skin != null && (skin.unlock.type == UnlockType.Default || Unlocked.Contains(Key("ball", skin.id)));

    public static bool IsUnlocked(BackgroundTheme theme) =>
        theme != null && (theme.unlock.type == UnlockType.Default || Unlocked.Contains(Key("theme", theme.id)));

    public static string EquippedBallId
    {
        get
        {
            SkinDatabase db = SkinDatabase.Instance;
            string fallback = db != null && db.DefaultBall != null ? db.DefaultBall.id : "";
            return PlayerPrefs.GetString(BallKey, fallback);
        }
    }

    public static string EquippedThemeId
    {
        get
        {
            SkinDatabase db = SkinDatabase.Instance;
            string fallback = db != null && db.DefaultTheme != null ? db.DefaultTheme.id : "";
            return PlayerPrefs.GetString(ThemeKey, fallback);
        }
    }

    public static BallSkin EquippedBall =>
        SkinDatabase.Instance != null ? SkinDatabase.Instance.GetBall(EquippedBallId) : null;

    public static BackgroundTheme EquippedTheme =>
        SkinDatabase.Instance != null ? SkinDatabase.Instance.GetTheme(EquippedThemeId) : null;

    public static bool IsEquipped(BallSkin skin) =>
        skin != null && skin == EquippedBall;

    public static bool IsEquipped(BackgroundTheme theme) =>
        theme != null && theme == EquippedTheme;

    public static void Equip(BallSkin skin)
    {
        if (!IsUnlocked(skin))
            return;

        PlayerPrefs.SetString(BallKey, skin.id);
        PlayerPrefs.Save();
    }

    public static void Equip(BackgroundTheme theme)
    {
        if (!IsUnlocked(theme))
            return;

        PlayerPrefs.SetString(ThemeKey, theme.id);
        PlayerPrefs.Save();
    }

    public static int EvaluateUnlocks()
    {
        SkinDatabase db = SkinDatabase.Instance;

        if (db == null)
            return 0;

        int count = 0;

        foreach (BallSkin ball in db.balls)
        {
            if (ball != null && !IsUnlocked(ball) && ball.unlock.IsMet())
            {
                Unlocked.Add(Key("ball", ball.id));
                count++;
            }
        }

        foreach (BackgroundTheme theme in db.themes)
        {
            if (theme != null && !IsUnlocked(theme) && theme.unlock.IsMet())
            {
                Unlocked.Add(Key("theme", theme.id));
                count++;
            }
        }

        if (count > 0)
        {
            PlayerPrefs.SetString(UnlockedKey, string.Join(",", Unlocked));
            PlayerPrefs.Save();
        }

        return count;
    }

    private static string Key(string kind, string id) =>
        $"{kind}:{id}";
}
