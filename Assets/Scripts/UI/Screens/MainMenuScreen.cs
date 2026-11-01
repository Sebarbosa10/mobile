using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class MainMenuScreen : UIScreen
{
    private const float HintBottom = 8f;
    private const float HintBottomWithBanner = 136f;

    private TextMeshProUGUI recordValue;
    private RectTransform shakeHint;
    private RectTransform removeAdsButton;
    private TextMeshProUGUI removeAdsLabel;
    private RectTransform privacyButton;

    public event Action PlayRequested;
    public event Action ChallengesRequested;
    public event Action CustomizeRequested;

    public static MainMenuScreen Create(RectTransform parent)
    {
        UIPalette palette = UIKit.Palette;
        UISkin skin = UIKit.Skin;

        RectTransform root = UIKit.CreateRect(parent, "MainMenuScreen");
        UIKit.Stretch(root);

        MainMenuScreen screen = root.gameObject.AddComponent<MainMenuScreen>();

        UIKit.CreateBackground(root, sky: true);

        RectTransform safe = UIKit.CreateSafeArea(root);

        TextMeshProUGUI tagline = UIKit.CreateText(
            safe, "Tagline", "STREET BASKETBALL", UIFont.Label, 36f, palette.text, 10f);
        UIKit.PlaceTop(tagline.rectTransform, 54f, new Vector2(984f, 44f));

        string sunHex = ColorUtility.ToHtmlStringRGB(palette.sun);

        RectTransform logo = UIKit.CreateShadowedText(
            safe, "Logo",
            $"HOOP <color=#{sunHex}>RUSH</color>",
            "HOOP RUSH",
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

        UIButton play = UIButton.Create(safe, "PlayButton", "PLAY", ButtonVariant.Primary, large: true);
        UIKit.PlaceCenter(play.RectTransform, new Vector2(0f, -190f), play.RectTransform.sizeDelta);
        play.OnClick.AddListener(() => screen.PlayRequested?.Invoke());

        UIButton challenges = UIButton.Create(safe, "ChallengesButton", "CHALLENGES", ButtonVariant.Primary);
        UIKit.PlaceCenter(challenges.RectTransform, new Vector2(0f, -389f), challenges.RectTransform.sizeDelta);
        challenges.OnClick.AddListener(() => screen.ChallengesRequested?.Invoke());

        UIButton customize = UIButton.Create(safe, "CustomizeButton", "CUSTOMIZE", ButtonVariant.Primary);
        float customizeY = -389f - challenges.RectTransform.sizeDelta.y - 36f;
        UIKit.PlaceCenter(customize.RectTransform, new Vector2(0f, customizeY), customize.RectTransform.sizeDelta);
        customize.OnClick.AddListener(() => screen.CustomizeRequested?.Invoke());

        RectTransform hint = UIKit.CreateChip(safe, "ShakeHint", 64f, palette.night, 28f, 16f);
        hint.anchorMin = hint.anchorMax = new Vector2(0.5f, 0f);
        hint.pivot = new Vector2(0.5f, 0f);
        hint.anchoredPosition = new Vector2(0f, HintBottom);
        UIKit.AddChipIcon(hint, skin.iconShake, 36f, palette.text);
        UIKit.AddChipText(hint, "Shake your phone to change the ball color", UIFont.Body, 30f, palette.text);
        screen.shakeHint = hint;

        screen.removeAdsButton = CreateChipButton(
            safe, "RemoveAdsButton", Vector2.one, new Vector2(-24f, 0f), "", Monetization.BuyRemoveAds,
            out screen.removeAdsLabel);

        screen.privacyButton = CreateChipButton(
            safe, "PrivacyButton", Vector2.up, new Vector2(24f, 0f), "PRIVACY", Monetization.ShowPrivacyOptions,
            out _);

        screen.Refresh();

        return screen;
    }

    private static RectTransform CreateChipButton(
        RectTransform parent,
        string name,
        Vector2 corner,
        Vector2 position,
        string text,
        Action clicked,
        out TextMeshProUGUI label)
    {
        UIPalette palette = UIKit.Palette;

        RectTransform chip = UIKit.CreateChip(parent, name, 72f, palette.night, 28f, 0f);
        chip.anchorMin = chip.anchorMax = corner;
        chip.pivot = corner;
        chip.anchoredPosition = position;
        label = UIKit.AddChipText(chip, text, UIFont.Label, 26f, palette.text);

        Image face = chip.GetComponent<Image>();
        face.raycastTarget = true;

        Button button = chip.gameObject.AddComponent<Button>();
        button.transition = Selectable.Transition.None;
        button.targetGraphic = face;
        button.onClick.AddListener(() =>
        {
            AudioManager.PlayUI(UISound.Tap);
            clicked();
        });

        return chip;
    }

    private void OnEnable()
    {
        Monetization.Changed += Refresh;
        Monetization.ShowBanner();
    }

    private void OnDisable()
    {
        Monetization.Changed -= Refresh;
        Monetization.HideBanner();
    }

    public void Refresh()
    {
        recordValue.text = BallShotClock.LoadRecord().ToString();

        bool adsRemoved = Monetization.AdsRemoved;

        removeAdsButton.gameObject.SetActive(!adsRemoved);
        removeAdsLabel.text = $"NO ADS {Monetization.RemoveAdsPrice}";
        privacyButton.gameObject.SetActive(!adsRemoved && Monetization.PrivacyOptionsRequired);
        shakeHint.anchoredPosition = new Vector2(0f, adsRemoved ? HintBottom : HintBottomWithBanner);
    }

    public override void Show()
    {
        base.Show();
        Refresh();
    }
}
