using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 6 · Game Over del endless. Dos variantes:
///  - Normal: "FIN DEL JUEGO", puntaje (Anton 300), chip de récord.
///  - Nuevo récord: borde y textos en Sol, trofeo, confeti y
///    "Récord anterior: N".
/// Ambas con REINICIAR y MENÚ.
/// </summary>
public sealed class GameOverScreen : UIScreen
{
    private OverlayPanel normalPanel;
    private OverlayPanel recordPanel;

    private TextMeshProUGUI normalScore;
    private TextMeshProUGUI normalRecord;
    private TextMeshProUGUI recordScore;
    private TextMeshProUGUI previousRecord;

    public event Action RestartRequested;
    public event Action ExitRequested;

    public static GameOverScreen Create(RectTransform parent)
    {
        RectTransform root = UIKit.CreateRect(parent, "GameOverScreen");
        UIKit.Stretch(root);

        GameOverScreen screen = root.gameObject.AddComponent<GameOverScreen>();

        screen.BuildNormal(root);
        screen.BuildRecord(root);

        root.gameObject.SetActive(false);

        return screen;
    }

    public void Show(GameOverInfo info)
    {
        gameObject.SetActive(true);
        transform.SetAsLastSibling();

        if (info.IsNewRecord)
        {
            recordScore.text = info.Score.ToString();
            previousRecord.text = $"Récord anterior: {info.PreviousRecord}";

            normalPanel.Hide();
            recordPanel.Show();
        }
        else
        {
            normalScore.text = info.Score.ToString();
            normalRecord.text = info.Record.ToString();

            recordPanel.Hide();
            normalPanel.Show();
        }
    }

    public override void Hide()
    {
        normalPanel.Hide();
        recordPanel.Hide();
        gameObject.SetActive(false);
    }

    private void BuildNormal(RectTransform root)
    {
        UIPalette palette = UIKit.Palette;

        normalPanel = OverlayPanel.Create(root, "Normal", 64f);
        RectTransform content = normalPanel.Content;

        TextMeshProUGUI title = UIKit.CreateText(
            content, "Title", "FIN DEL JUEGO", UIFont.Display, 104f, palette.text, 4f);
        title.rectTransform.sizeDelta = new Vector2(760f, 120f);
        normalPanel.Add(title, 120f);

        TextMeshProUGUI label = UIKit.CreateText(
            content, "ScoreLabel", "PUNTAJE", UIFont.Label, 36f, palette.labelMuted, 8f);
        label.rectTransform.sizeDelta = new Vector2(760f, 50f);
        normalPanel.Add(label, 50f, 20f + 24f);

        normalScore = UIKit.CreateText(content, "Score", "0", UIFont.Display, 300f, palette.text);
        normalScore.rectTransform.sizeDelta = new Vector2(760f, 315f);
        normalPanel.Add(normalScore, 315f, 20f);

        normalRecord = UIKit.CreateRecordChip(
            content, "RecordChip", 80f, palette.background,
            40f, 34f, 3f, 48f, 36f, 14f,
            out RectTransform chip);
        normalPanel.Add(chip, 80f, 20f);

        AddButtons(normalPanel, content, 20f);
    }

    private void BuildRecord(RectTransform root)
    {
        UIPalette palette = UIKit.Palette;
        UISkin skin = UIKit.Skin;

        recordPanel = OverlayPanel.Create(root, "NewRecord", 60f);
        recordPanel.SetBorder(palette.sun);
        Confetti.Create(recordPanel.Backdrop);

        RectTransform content = recordPanel.Content;

        Image trophy = UIKit.CreateImage(content, "Trophy", skin.iconTrophy, palette.sun);
        trophy.rectTransform.sizeDelta = new Vector2(120f, 120f);
        recordPanel.Add(trophy, 120f);

        TextMeshProUGUI title = UIKit.CreateText(
            content, "Title", "¡NUEVO RÉCORD!", UIFont.Display, 100f, palette.sun, 3f);
        title.rectTransform.sizeDelta = new Vector2(760f, 120f);
        recordPanel.Add(title, 120f, 16f);
        title.rectTransform.localRotation = Quaternion.Euler(0f, 0f, 3f);

        recordScore = UIKit.CreateText(content, "Score", "0", UIFont.Display, 300f, palette.sun);
        recordScore.rectTransform.sizeDelta = new Vector2(760f, 315f);
        recordPanel.Add(recordScore, 315f, 16f);

        RectTransform chip = UIKit.CreateChip(content, "PreviousRecord", 80f, palette.background, 36f, 0f);
        previousRecord = UIKit.AddChipText(chip, "Récord anterior: 0", UIFont.Body, 36f, palette.text);
        recordPanel.Add(chip, 80f, 16f);

        AddButtons(recordPanel, content, 16f);
    }

    private void AddButtons(OverlayPanel panel, RectTransform content, float gap)
    {
        UIButton restart = UIButton.Create(content, "RestartButton", "REINICIAR", ButtonVariant.Secondary);
        panel.Add(restart, restart.LayoutHeight, gap + 52f);
        restart.OnClick.AddListener(() => RestartRequested?.Invoke());

        UIButton menu = UIButton.Create(content, "MenuButton", "MENÚ", ButtonVariant.Neutral);
        panel.Add(menu, menu.LayoutHeight, gap + 28f);
        menu.OnClick.AddListener(() => ExitRequested?.Invoke());

        panel.Finish(92f);
    }
}
