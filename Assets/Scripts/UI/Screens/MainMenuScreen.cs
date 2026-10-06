using System;
using TMPro;
using UnityEngine;

/// <summary>
/// 1 · Menú principal: bajada "BÁSQUET DE PLAZA", logo "ÚLTIMA LUZ"
/// (Anton 196, rotado −4°, "LUZ" en Sol, sombra Noche), chip de récord,
/// botones JUGAR y DESAFÍOS y la pista de "sacudí el celu".
/// </summary>
public sealed class MainMenuScreen : UIScreen
{
    private TextMeshProUGUI recordValue;

    public event Action PlayRequested;
    public event Action ChallengesRequested;

    public static MainMenuScreen Create(RectTransform parent)
    {
        UIPalette palette = UIKit.Palette;
        UISkin skin = UIKit.Skin;

        RectTransform root = UIKit.CreateRect(parent, "MainMenuScreen");
        UIKit.Stretch(root);

        MainMenuScreen screen = root.gameObject.AddComponent<MainMenuScreen>();

        UIKit.CreateBackground(root, sky: true);

        RectTransform safe = UIKit.CreateSafeArea(root);

        // Posiciones "top" medidas desde el borde superior de la safe area
        // (el mockup reserva 96 px arriba).
        TextMeshProUGUI tagline = UIKit.CreateText(
            safe, "Tagline", "BÁSQUET DE PLAZA", UIFont.Label, 36f, palette.text, 10f);
        UIKit.PlaceTop(tagline.rectTransform, 54f, new Vector2(984f, 44f));

        string sunHex = ColorUtility.ToHtmlStringRGB(palette.sun);

        RectTransform logo = UIKit.CreateShadowedText(
            safe, "Logo",
            $"ÚLTIMA <color=#{sunHex}>LUZ</color>",
            "ÚLTIMA LUZ",
            UIFont.Display, 196f, palette.text, 10f, 2f,
            out _, out _);
        UIKit.PlaceTop(logo, 105f, new Vector2(1000f, 226f));
        logo.localRotation = Quaternion.Euler(0f, 0f, 4f);

        screen.recordValue = UIKit.CreateRecordChip(
            safe, "RecordChip", 76f, palette.night,
            40f, 34f, 3f, 48f, 32f, 16f,
            out RectTransform recordChip);
        recordChip.anchorMin = recordChip.anchorMax = new Vector2(0.5f, 1f);
        recordChip.anchoredPosition = new Vector2(0f, -358f);

        UIButton play = UIButton.Create(safe, "PlayButton", "JUGAR", ButtonVariant.Primary, large: true);
        UIKit.PlaceCenter(play.RectTransform, new Vector2(0f, -190f), play.RectTransform.sizeDelta);
        play.OnClick.AddListener(() => screen.PlayRequested?.Invoke());

        UIButton challenges = UIButton.Create(safe, "ChallengesButton", "DESAFÍOS", ButtonVariant.Primary);
        UIKit.PlaceCenter(challenges.RectTransform, new Vector2(0f, -389f), challenges.RectTransform.sizeDelta);
        challenges.OnClick.AddListener(() => screen.ChallengesRequested?.Invoke());

        RectTransform hint = UIKit.CreateChip(safe, "ShakeHint", 64f, palette.night, 28f, 16f);
        hint.anchorMin = hint.anchorMax = new Vector2(0.5f, 0f);
        hint.pivot = new Vector2(0.5f, 0f);
        hint.anchoredPosition = new Vector2(0f, 8f);
        UIKit.AddChipIcon(hint, skin.iconShake, 36f, palette.text);
        UIKit.AddChipText(hint, "Sacudí el celu y la pelota cambia de color", UIFont.Body, 30f, palette.text);

        screen.Refresh();

        return screen;
    }

    public void Refresh()
    {
        recordValue.text = BallShotClock.LoadRecord().ToString();
    }

    public override void Show()
    {
        base.Show();
        Refresh();
    }
}
