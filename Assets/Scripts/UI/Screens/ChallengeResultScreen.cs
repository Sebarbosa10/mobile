using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 7 · Resultado de desafío. Victoria: "¡DESAFÍO COMPLETO!" en verde
/// con un check; derrota: "PERDISTE" con un reloj sobre rojo. Ambas con
/// el nombre del desafío, el resumen ("Llegaste a X de Y"), los puntos
/// de progreso y REINTENTAR / MENÚ.
/// </summary>
public sealed class ChallengeResultScreen : UIScreen
{
    private Panel victory;
    private Panel defeat;

    public event Action RestartRequested;
    public event Action ExitRequested;

    public static ChallengeResultScreen Create(RectTransform parent)
    {
        UIPalette palette = UIKit.Palette;
        UISkin skin = UIKit.Skin;

        RectTransform root = UIKit.CreateRect(parent, "ChallengeResultScreen");
        UIKit.Stretch(root);

        ChallengeResultScreen screen = root.gameObject.AddComponent<ChallengeResultScreen>();

        screen.victory = screen.BuildPanel(
            root, "Victory",
            palette.success, skin.iconCheck, palette.onSuccess,
            "¡DESAFÍO\nCOMPLETO!", 128f, 3f, palette.success, 290f);

        screen.defeat = screen.BuildPanel(
            root, "Defeat",
            palette.urgent, skin.iconClock, palette.text,
            "PERDISTE", 160f, 4f, palette.text, 180f);

        root.gameObject.SetActive(false);

        return screen;
    }

    public void Show(ChallengeDefinition challenge, ChallengeEndInfo info)
    {
        gameObject.SetActive(true);
        transform.SetAsLastSibling();

        Panel shown = info.Won ? victory : defeat;
        Panel hidden = info.Won ? defeat : victory;

        UIPalette palette = UIKit.Palette;

        shown.Header.text = $"DESAFÍO {challenge.Number} · {challenge.Name.ToUpperInvariant()}";
        shown.Summary.text = info.Summary;

        Color filled = challenge.Number == 1 ? palette.success : palette.sun;
        bool fewPips = info.Total <= 5;
        shown.Pips.Set(info.Total, info.Current, filled, 44f, fewPips ? 18f : 14f);

        hidden.Overlay.Hide();
        shown.Overlay.Show();
    }

    public override void Hide()
    {
        victory.Overlay.Hide();
        defeat.Overlay.Hide();
        gameObject.SetActive(false);
    }

    private Panel BuildPanel(
        RectTransform root,
        string name,
        Color accent,
        Sprite medalIcon,
        Color medalIconColor,
        string title,
        float titleSize,
        float titleTracking,
        Color titleColor,
        float titleHeight)
    {
        UIPalette palette = UIKit.Palette;
        UISkin skin = UIKit.Skin;

        OverlayPanel overlay = OverlayPanel.Create(root, name, 64f);
        overlay.SetBorder(accent);

        RectTransform content = overlay.Content;

        Image medal = UIKit.CreateImage(content, "Medal", skin.disc, accent);
        medal.rectTransform.sizeDelta = new Vector2(180f, 180f);
        overlay.Add(medal, 180f);

        Image icon = UIKit.CreateImage(medal.rectTransform, "Icon", medalIcon, medalIconColor);
        UIKit.PlaceCenter(icon.rectTransform, Vector2.zero, new Vector2(110f, 110f));

        TextMeshProUGUI titleText = UIKit.CreateText(
            content, "Title", title, UIFont.Display, titleSize, titleColor, titleTracking);
        titleText.rectTransform.sizeDelta = new Vector2(760f, titleHeight);
        titleText.lineSpacing = -12f;
        overlay.Add(titleText, titleHeight, 20f + 12f);

        Panel panel = new()
        {
            Overlay = overlay
        };

        panel.Header = UIKit.CreateText(
            content, "Header", "", UIFont.Label, 30f, palette.sun, 6f);
        panel.Header.rectTransform.sizeDelta = new Vector2(760f, 42f);
        overlay.Add(panel.Header, 42f, 20f + 12f);

        panel.Summary = UIKit.CreateText(content, "Summary", "", UIFont.Body, 44f, palette.text);
        panel.Summary.rectTransform.sizeDelta = new Vector2(760f, 56f);
        overlay.Add(panel.Summary, 56f, 20f);

        panel.Pips = PipRow.Create(content, "Pips", 44f);
        overlay.Add(panel.Pips, 44f, 20f);

        UIButton retry = UIButton.Create(content, "RetryButton", "REINTENTAR", ButtonVariant.Secondary);
        overlay.Add(retry, retry.LayoutHeight, 20f + 52f);
        retry.OnClick.AddListener(() => RestartRequested?.Invoke());

        UIButton menu = UIButton.Create(content, "MenuButton", "MENÚ", ButtonVariant.Neutral);
        overlay.Add(menu, menu.LayoutHeight, 20f + 28f);
        menu.OnClick.AddListener(() => ExitRequested?.Invoke());

        overlay.Finish(92f);

        return panel;
    }

    private sealed class Panel
    {
        public OverlayPanel Overlay;
        public TextMeshProUGUI Header;
        public TextMeshProUGUI Summary;
        public PipRow Pips;
    }
}
