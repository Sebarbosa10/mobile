using TMPro;
using UnityEngine;

/// <summary>
/// 3 · HUD del endless: anillo de 340 centrado arriba con el puntaje
/// adentro (Anton 150) y el timer de 7 s alrededor, y el chip de récord
/// debajo. Todo queda por encima de y = 560 para no tapar el swipe.
/// </summary>
public sealed class EndlessHudScreen : UIScreen
{
    private TimerRing ring;
    private TextMeshProUGUI recordValue;

    public static EndlessHudScreen Create(RectTransform safeArea)
    {
        UIPalette palette = UIKit.Palette;

        RectTransform root = UIKit.CreateRect(safeArea, "EndlessHud");
        UIKit.Stretch(root);

        EndlessHudScreen screen = root.gameObject.AddComponent<EndlessHudScreen>();

        screen.ring = TimerRing.Create(root, "ScoreRing", 340f, 150f);
        UIKit.PlaceTop(screen.ring.RectTransform, 8f, new Vector2(340f, 340f));

        screen.recordValue = UIKit.CreateRecordChip(
            root, "RecordChip", 64f, palette.night,
            34f, 30f, 2f, 40f, 28f, 12f,
            out RectTransform chip);
        chip.anchorMin = chip.anchorMax = new Vector2(0.5f, 1f);
        chip.anchoredPosition = new Vector2(0f, -(8f + 340f + 16f));

        return screen;
    }

    public void SetScore(int score, bool pop)
    {
        ring.SetText(score.ToString());

        if (pop)
            ring.Pop();
    }

    public void SetRecord(int record)
    {
        recordValue.text = record.ToString();
    }

    public void SetTimer(float remaining, float total)
    {
        ring.SetFraction(total > 0f ? remaining / total : 0f);
    }
}
