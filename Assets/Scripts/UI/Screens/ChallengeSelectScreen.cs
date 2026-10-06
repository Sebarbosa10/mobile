using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// 2 · Selector de desafíos: título, panel "Encestadas en el endless: N"
/// con la barra hacia el próximo desbloqueo, las tres tarjetas y VOLVER.
/// </summary>
public sealed class ChallengeSelectScreen : UIScreen
{
    private readonly List<ChallengeCard> cards = new();

    private TextMeshProUGUI countText;
    private ProgressBar bar;
    private TextMeshProUGUI fromLabel;
    private TextMeshProUGUI toLabel;
    private TextMeshProUGUI remainingLabel;

    public event Action<ChallengeDefinition> ChallengeSelected;
    public event Action BackRequested;

    public static ChallengeSelectScreen Create(RectTransform parent)
    {
        UIPalette palette = UIKit.Palette;
        UISkin skin = UIKit.Skin;

        RectTransform root = UIKit.CreateRect(parent, "ChallengeSelectScreen");
        UIKit.Stretch(root);

        ChallengeSelectScreen screen = root.gameObject.AddComponent<ChallengeSelectScreen>();

        UIKit.CreateBackground(root, sky: false);

        RectTransform safe = UIKit.CreateSafeArea(root);

        TextMeshProUGUI title = UIKit.CreateText(
            safe, "Title", "DESAFÍOS", UIFont.Display, 120f, palette.text, 3f);
        UIKit.PlaceTop(title.rectTransform, 16f, new Vector2(984f, 138f));

        // Panel de progreso hacia el próximo desbloqueo.
        Image panel = UIKit.CreateImage(safe, "ProgressPanel", skin.chip, palette.night);
        UIKit.SetChipRadius(panel, 36f);
        UIKit.PlaceTop(panel.rectTransform, 86f, new Vector2(984f, 258f));
        RectTransform panelRect = panel.rectTransform;

        Image hoopIcon = UIKit.CreateImage(panelRect, "HoopIcon", skin.iconHoop, palette.sun);
        UIKit.PlaceTopLeft(hoopIcon.rectTransform, 40f, 44f, new Vector2(48f, 48f));

        TextMeshProUGUI countLabel = UIKit.CreateText(
            panelRect, "CountLabel", "Encestadas en el endless:",
            UIFont.Body, 38f, palette.text, 0f, TextAlignmentOptions.MidlineLeft);
        UIKit.PlaceTopLeft(countLabel.rectTransform, 106f, 36f, new Vector2(640f, 64f));

        screen.countText = UIKit.CreateText(
            panelRect, "Count", "0", UIFont.Display, 64f, palette.sun, 0f, TextAlignmentOptions.MidlineRight);
        UIKit.PlaceTopRight(screen.countText.rectTransform, 40f, 36f, new Vector2(200f, 64f));

        screen.bar = ProgressBar.Create(panelRect, "Bar", new Vector2(904f, 36f));
        UIKit.PlaceTop(screen.bar.RectTransform, 122f, new Vector2(904f, 36f));

        screen.fromLabel = UIKit.CreateText(
            panelRect, "From", "0", UIFont.Body, 30f, palette.text, 0f, TextAlignmentOptions.MidlineLeft);
        UIKit.PlaceTopLeft(screen.fromLabel.rectTransform, 40f, 180f, new Vector2(100f, 42f));

        screen.remainingLabel = UIKit.CreateText(
            panelRect, "Remaining", "", UIFont.Body, 30f, palette.text);
        UIKit.PlaceTop(screen.remainingLabel.rectTransform, 180f, new Vector2(704f, 42f));

        screen.toLabel = UIKit.CreateText(
            panelRect, "To", "10", UIFont.Body, 30f, palette.text, 0f, TextAlignmentOptions.MidlineRight);
        UIKit.PlaceTopRight(screen.toLabel.rectTransform, 40f, 180f, new Vector2(100f, 42f));

        // Tarjetas.
        Vector2 cardSize = new(ChallengeCard.Size.x, ChallengeCard.Size.y + UIKit.ButtonLip);

        for (int number = 1; number <= ChallengeCatalog.Count; number++)
        {
            ChallengeCard card = ChallengeCard.Create(safe, ChallengeCatalog.Get(number));
            UIKit.PlaceTop(card.RectTransform, 384f + (number - 1) * 320f, cardSize);
            card.Selected += definition => screen.ChallengeSelected?.Invoke(definition);
            screen.cards.Add(card);
        }

        UIButton back = UIButton.Create(safe, "BackButton", "VOLVER", ButtonVariant.Neutral);
        UIKit.PlaceBottom(back.RectTransform, 20f, back.RectTransform.sizeDelta);
        back.OnClick.AddListener(() => screen.BackRequested?.Invoke());

        return screen;
    }

    public override void Show()
    {
        base.Show();
        Refresh();
    }

    private void Update()
    {
        // Botón "atrás" de Android (Escape en el Input System).
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            BackRequested?.Invoke();
    }

    public void Refresh()
    {
        int total = ChallengeProgress.TotalBaskets;

        countText.text = total.ToString();

        int nextLocked = 0;

        for (int number = 1; number <= ChallengeCatalog.Count; number++)
        {
            if (!ChallengeProgress.IsUnlocked(number))
            {
                nextLocked = number;
                break;
            }
        }

        if (nextLocked == 0)
        {
            bar.SetFraction(1f);
            fromLabel.text = "";
            toLabel.text = "";
            remainingLabel.text = "¡Desbloqueaste todos los desafíos!";
        }
        else
        {
            int from = ChallengeProgress.RequiredBaskets(nextLocked - 1);
            int to = ChallengeProgress.RequiredBaskets(nextLocked);

            bar.SetFraction((float)(total - from) / (to - from));
            fromLabel.text = from.ToString();
            toLabel.text = to.ToString();

            string name = ChallengeCatalog.Get(nextLocked).Name;
            remainingLabel.text = $"Faltan {to - total} para desbloquear «{name}»";
        }

        for (int i = 0; i < cards.Count; i++)
        {
            int number = i + 1;

            ChallengeCardState state =
                !ChallengeProgress.IsUnlocked(number) ? ChallengeCardState.Locked :
                ChallengeProgress.IsCompleted(number) ? ChallengeCardState.Completed :
                ChallengeCardState.Unlocked;

            cards[i].SetState(state);
        }
    }
}
