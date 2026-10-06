using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 4 · HUD de desafíos: chip centrado arriba con "DESAFÍO N · NOMBRE",
/// el contador ("Aros: 2/5", "Encestadas: 1/3", "Puntos: 5/8") y los
/// puntos de progreso. Solo en Contrarreloj aparece además un anillo de
/// 200 arriba a la izquierda con los segundos.
/// </summary>
public sealed class ChallengeHudScreen : UIScreen
{
    private const float ChipWidth = 500f;
    private const float ChipHeight = 262f;

    private ChallengeDefinition definition;

    private TextMeshProUGUI headerText;
    private TextMeshProUGUI counterLabel;
    private TextMeshProUGUI counterValue;
    private PipRow pips;
    private TimerRing timerRing;

    public static ChallengeHudScreen Create(RectTransform safeArea)
    {
        UIPalette palette = UIKit.Palette;
        UISkin skin = UIKit.Skin;

        RectTransform root = UIKit.CreateRect(safeArea, "ChallengeHud");
        UIKit.Stretch(root);

        ChallengeHudScreen screen = root.gameObject.AddComponent<ChallengeHudScreen>();

        Image chip = UIKit.CreateImage(root, "ProgressChip", skin.chip, palette.night);
        UIKit.SetChipRadius(chip, 36f);
        UIKit.PlaceTop(chip.rectTransform, 8f, new Vector2(ChipWidth, ChipHeight));
        RectTransform chipRect = chip.rectTransform;

        screen.headerText = UIKit.CreateText(
            chipRect, "Header", "", UIFont.Label, 28f, palette.sun, 6f);
        UIKit.PlaceTop(screen.headerText.rectTransform, 24f, new Vector2(ChipWidth - 40f, 34f));

        // Etiqueta y número comparten línea de base.
        RectTransform counter = UIKit.CreateRect(chipRect, "Counter");
        UIKit.PlaceTop(counter, 68f, new Vector2(ChipWidth - 64f, 114f));

        HorizontalLayoutGroup row = counter.gameObject.AddComponent<HorizontalLayoutGroup>();
        row.spacing = 20f;
        row.childAlignment = TextAnchor.MiddleCenter;
        row.childControlWidth = true;
        row.childControlHeight = false;
        row.childForceExpandWidth = false;
        row.childForceExpandHeight = false;

        screen.counterLabel = UIKit.CreateText(
            counter, "Label", "", UIFont.Label, 44f, palette.text, 2f, TextAlignmentOptions.Baseline);
        screen.counterLabel.rectTransform.sizeDelta = new Vector2(0f, 114f);

        screen.counterValue = UIKit.CreateText(
            counter, "Value", "0/0", UIFont.Display, 104f, palette.text, 0f, TextAlignmentOptions.Baseline);
        screen.counterValue.rectTransform.sizeDelta = new Vector2(0f, 114f);

        screen.pips = PipRow.Create(chipRect, "Pips", 40f);
        UIKit.PlaceTop(screen.pips.RectTransform, 192f, new Vector2(0f, 40f));

        screen.timerRing = TimerRing.Create(root, "TimerRing", 200f, 92f);
        UIKit.PlaceTopLeft(screen.timerRing.RectTransform, UIKit.SideMargin, 8f, new Vector2(200f, 200f));
        screen.timerRing.gameObject.SetActive(false);

        return screen;
    }

    public void SetChallenge(ChallengeDefinition challenge)
    {
        definition = challenge;

        string header = $"DESAFÍO {challenge.Number} · {challenge.Name.ToUpperInvariant()}";

        headerText.text = header;

        // Los nombres largos usan tracking +4 para entrar en el chip.
        headerText.characterSpacing = (header.Length > 20 ? 4f : 6f) / headerText.fontSize * 100f;

        counterLabel.text = $"{challenge.ProgressLabel}:";
    }

    public void SetProgress(int current, int total)
    {
        counterValue.text = $"{current}/{total}";

        if (definition == null)
            return;

        UIPalette palette = UIKit.Palette;

        // En Carrusel cada aro acertado se pinta de verde; en el resto, Sol.
        Color filled = definition.Number == 1 ? palette.success : palette.sun;

        bool fewPips = total <= 5;
        pips.Set(total, current, filled, fewPips ? 40f : 34f, fewPips ? 18f : 14f);
    }

    public void SetTimer(float remaining, float total)
    {
        timerRing.gameObject.SetActive(true);
        timerRing.SetText(Mathf.CeilToInt(remaining).ToString());
        timerRing.SetFraction(total > 0f ? remaining / total : 0f);
    }
}
