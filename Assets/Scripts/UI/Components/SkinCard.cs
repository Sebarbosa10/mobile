using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class SkinCard : MonoBehaviour
{
    private const float Radius = 32f;
    private const float BorderWidth = 6f;
    private const float MedalSize = 48f;
    private const float LockedAlpha = 0.35f;

    private Graphic picture;
    private TextMeshProUGUI nameText;
    private TextMeshProUGUI statusText;
    private Image border;
    private Image medal;
    private Image medalCheck;
    private Image medalLock;
    private bool feminine;

    public event Action Clicked;

    public RectTransform RectTransform =>
        (RectTransform)transform;

    public static SkinCard CreateBall(Transform parent, BallSkin skin, Vector2 size)
    {
        UIPalette palette = UIKit.Palette;
        SkinCard card = CreateBase(parent, $"Ball_{skin.id}", size, out RectTransform body);
        card.feminine = true;

        Image icon = UIKit.CreateImage(body, "Ball", skin.icon, Color.white);
        icon.preserveAspect = true;
        UIKit.PlaceTop(icon.rectTransform, 21f, new Vector2(140f, 140f));
        card.picture = icon;

        card.nameText = UIKit.CreateText(body, "Name", skin.displayName.ToUpperInvariant(), UIFont.Display, 40f, palette.text, 1f);
        UIKit.PlaceTop(card.nameText.rectTransform, 167f, new Vector2(size.x - 24f, 44f));

        card.statusText = UIKit.CreateText(body, "Status", "", UIFont.Body, 26f, palette.labelMuted);
        UIKit.PlaceTop(card.statusText.rectTransform, 217f, new Vector2(size.x - 24f, 32f));

        card.AddOverlays();
        return card;
    }

    public static SkinCard CreateTheme(Transform parent, BackgroundTheme theme, Vector2 size)
    {
        UIPalette palette = UIKit.Palette;
        SkinCard card = CreateBase(parent, $"Theme_{theme.id}", size, out RectTransform body);
        card.feminine = false;

        body.gameObject.AddComponent<Mask>().showMaskGraphic = true;

        RawImage thumbnail = UIKit.CreateRect(body, "Thumbnail").gameObject.AddComponent<RawImage>();
        thumbnail.texture = theme.thumbnail;
        thumbnail.raycastTarget = false;
        UIKit.PlaceTop(thumbnail.rectTransform, 0f, new Vector2(size.x, 190f));
        card.picture = thumbnail;

        card.nameText = UIKit.CreateText(body, "Name", theme.displayName.ToUpperInvariant(), UIFont.Display, 42f, palette.text, 1f,
            TextAlignmentOptions.MidlineLeft);
        UIKit.Place(card.nameText.rectTransform, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(28f, 0f), new Vector2(260f, 90f));

        card.statusText = UIKit.CreateText(body, "Status", "", UIFont.Body, 26f, palette.labelMuted, 0f,
            TextAlignmentOptions.MidlineRight);
        UIKit.Place(card.statusText.rectTransform, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-28f, 0f), new Vector2(200f, 90f));

        card.AddOverlays();
        return card;
    }

    private static SkinCard CreateBase(Transform parent, string name, Vector2 size, out RectTransform body)
    {
        RectTransform root = UIKit.CreateRect(parent, name);
        root.sizeDelta = size;

        SkinCard card = root.gameObject.AddComponent<SkinCard>();

        Image background = UIKit.CreateImage(root, "Body", UIKit.Skin.chip, UIKit.Palette.surface, raycast: true);
        UIKit.SetChipRadius(background, Radius);
        UIKit.Stretch(background.rectTransform);
        body = background.rectTransform;

        Button button = root.gameObject.AddComponent<Button>();
        button.transition = Selectable.Transition.None;
        button.targetGraphic = background;
        button.onClick.AddListener(() => card.Clicked?.Invoke());

        return card;
    }

    private void AddOverlays()
    {
        UIPalette palette = UIKit.Palette;
        UISkin skin = UIKit.Skin;

        border = UIKit.CreateImage(transform, "Border", UIShapes.Ring(Radius, BorderWidth), palette.success);
        border.type = Image.Type.Sliced;
        UIKit.Stretch(border.rectTransform);

        medal = UIKit.CreateImage(transform, "Medal", skin.disc, palette.success);
        UIKit.PlaceTopRight(medal.rectTransform, 16f, 16f, new Vector2(MedalSize, MedalSize));

        medalCheck = UIKit.CreateImage(medal.rectTransform, "Check", skin.iconCheck, palette.onSuccess);
        UIKit.PlaceCenter(medalCheck.rectTransform, Vector2.zero, new Vector2(30f, 30f));

        medalLock = UIKit.CreateImage(medal.rectTransform, "Lock", skin.iconLock, palette.lockedSubtext);
        UIKit.PlaceCenter(medalLock.rectTransform, Vector2.zero, new Vector2(28f, 28f));
    }

    public void SetState(bool unlocked, bool equipped, bool selected, string lockedText)
    {
        UIPalette palette = UIKit.Palette;

        Color pictureColor = Color.white;
        pictureColor.a = unlocked ? 1f : LockedAlpha;
        picture.color = pictureColor;

        nameText.color = unlocked ? palette.text : palette.lockedText;

        if (!unlocked)
        {
            SetStatus(lockedText, UIFont.Body, palette.lockedSubtext);
        }
        else if (equipped)
        {
            SetStatus("EN USO", UIFont.Label, palette.success);
        }
        else
        {
            SetStatus(feminine ? "Desbloqueada" : "Desbloqueado", UIFont.Body, palette.labelMuted);
        }

        border.gameObject.SetActive(equipped || selected);
        border.color = equipped ? palette.success : palette.sun;

        medal.gameObject.SetActive(equipped || !unlocked);
        medal.color = equipped ? palette.success : palette.locked;
        medalCheck.gameObject.SetActive(equipped);
        medalLock.gameObject.SetActive(!unlocked);
    }

    private void SetStatus(string text, UIFont font, Color color)
    {
        statusText.text = text;
        statusText.font = UIKit.Skin.GetFont(font);
        statusText.color = color;
    }
}
