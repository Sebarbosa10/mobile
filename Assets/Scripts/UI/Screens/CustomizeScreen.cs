using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public sealed class CustomizeScreen : UIScreen
{
    private const float ContentWidth = 984f;
    private const float Gap = 28f;
    private const float TabsTop = 16f;
    private const float TabsHeight = 120f;
    private const float PreviewTop = TabsTop + TabsHeight + Gap;

    private const float BallPreviewHeight = 380f;
    private const float BallCardHeight = 270f;
    private const int BallColumns = 3;

    private const float ThemePreviewHeight = 400f;
    private static readonly Vector2 ThemeCardSize = new(478f, 280f);
    private const float ThemeRowGap = 24f;

    private enum Tab
    {
        Balls,
        Themes
    }

    private SkinDatabase db;
    private Tab tab;

    private readonly TabButton[] tabButtons = new TabButton[2];
    private RectTransform ballsPage;
    private RectTransform themesPage;

    private readonly List<SkinCard> ballCards = new();
    private readonly List<SkinCard> themeCards = new();

    private BallSkin selectedBall;
    private BackgroundTheme selectedTheme;

    private SkinPreviewStage stage;
    private RawImage ballPreviewImage;
    private TextMeshProUGUI ballName;
    private StatusRow ballStatus;
    private readonly List<VariantDot> variantDots = new();

    private RawImage themeThumbnail;
    private TextMeshProUGUI themeName;
    private StatusRow themeStatus;
    private TextMeshProUGUI themeDescription;
    private readonly List<Image> paletteSwatches = new();
    private readonly List<Image> paletteRings = new();

    private UIButton equipButton;

    public event Action BackRequested;

    public static CustomizeScreen Create(RectTransform parent)
    {
        UIPalette palette = UIKit.Palette;

        RectTransform root = UIKit.CreateRect(parent, "CustomizeScreen");
        UIKit.Stretch(root);

        CustomizeScreen screen = root.gameObject.AddComponent<CustomizeScreen>();
        screen.db = SkinDatabase.Instance;

        Image background = UIKit.CreateImage(root, "Background", null, palette.background);
        UIKit.Stretch(background.rectTransform);

        RectTransform safe = UIKit.CreateSafeArea(root);

        Image tabs = UIKit.CreateImage(safe, "Tabs", UIKit.Skin.chip, palette.night);
        UIKit.SetChipRadius(tabs, 36f);
        UIKit.PlaceTop(tabs.rectTransform, TabsTop, new Vector2(ContentWidth, TabsHeight));

        screen.tabButtons[0] = TabButton.Create(tabs.rectTransform, "PELOTAS", left: true);
        screen.tabButtons[1] = TabButton.Create(tabs.rectTransform, "FONDOS", left: false);
        screen.tabButtons[0].Clicked += () => screen.SelectTab(Tab.Balls);
        screen.tabButtons[1].Clicked += () => screen.SelectTab(Tab.Themes);

        screen.ballsPage = UIKit.CreateRect(safe, "BallsPage");
        UIKit.Stretch(screen.ballsPage);
        screen.themesPage = UIKit.CreateRect(safe, "ThemesPage");
        UIKit.Stretch(screen.themesPage);

        if (screen.db != null)
        {
            screen.BuildBallsPage();
            screen.BuildThemesPage();
        }
        else
        {
            Debug.LogError("[CustomizeScreen] Falta la SkinDatabase: corré Tools > Skins > Build All.");
        }

        Vector2 buttonSize = new(470f, UIButton.DefaultSize.y + UIKit.ButtonLip);
        const float buttonOffset = (470f + 44f) * 0.5f;

        UIButton back = UIButton.Create(safe, "BackButton", "VOLVER", ButtonVariant.Neutral);
        back.RectTransform.sizeDelta = buttonSize;
        UIKit.PlaceBottom(back.RectTransform, 0f, buttonSize, -buttonOffset);
        back.OnClick.AddListener(() => screen.BackRequested?.Invoke());

        screen.equipButton = UIButton.Create(safe, "EquipButton", "EQUIPAR", ButtonVariant.Primary);
        screen.equipButton.RectTransform.sizeDelta = buttonSize;
        UIKit.PlaceBottom(screen.equipButton.RectTransform, 0f, buttonSize, buttonOffset);
        screen.equipButton.OnClick.AddListener(screen.Equip);

        screen.tab = Tab.Balls;
        return screen;
    }

    private void BuildBallsPage()
    {
        UIPalette palette = UIKit.Palette;

        Image panel = UIKit.CreateImage(ballsPage, "Preview", UIKit.Skin.chip, palette.night);
        UIKit.SetChipRadius(panel, 36f);
        UIKit.PlaceTop(panel.rectTransform, PreviewTop, new Vector2(ContentWidth, BallPreviewHeight));
        RectTransform panelRect = panel.rectTransform;

        ballPreviewImage = UIKit.CreateRect(panelRect, "Ball3D").gameObject.AddComponent<RawImage>();
        ballPreviewImage.raycastTarget = false;
        UIKit.Place(ballPreviewImage.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
            new Vector2(48f, 0f), new Vector2(SkinPreviewStage.Size, SkinPreviewStage.Size));

        RectTransform text = TextColumn(panelRect, 48f + SkinPreviewStage.Size + 48f, 48f, 14f);

        ballName = Layout(UIKit.CreateText(text, "Name", "", UIFont.Display, 88f, palette.text, 2f, TextAlignmentOptions.Left), 97f);
        ballStatus = StatusRow.Create(text, 32f);

        TextMeshProUGUI hint = UIKit.CreateText(text, "Hint", "Sacudí el celu para ver sus variantes:", UIFont.Body, 30f,
            palette.labelMuted, 0f, TextAlignmentOptions.Left);
        hint.textWrappingMode = TextWrappingModes.Normal;

        RectTransform dots = UIKit.CreateRect(text, "Variants");
        HorizontalLayoutGroup dotsLayout = dots.gameObject.AddComponent<HorizontalLayoutGroup>();
        dotsLayout.spacing = 14f;
        dotsLayout.childAlignment = TextAnchor.MiddleLeft;
        dotsLayout.childControlWidth = false;
        dotsLayout.childControlHeight = false;
        dotsLayout.childForceExpandWidth = false;
        dotsLayout.childForceExpandHeight = false;
        dots.gameObject.AddComponent<LayoutElement>().preferredHeight = VariantDot.Size;

        for (int i = 0; i < 4; i++)
            variantDots.Add(VariantDot.Create(dots));

        float gridTop = PreviewTop + BallPreviewHeight + Gap;
        float cardWidth = (ContentWidth - Gap * (BallColumns - 1)) / BallColumns;

        for (int i = 0; i < db.balls.Length; i++)
        {
            BallSkin skin = db.balls[i];

            if (skin == null)
                continue;

            int column = i % BallColumns;
            int row = i / BallColumns;

            SkinCard card = SkinCard.CreateBall(ballsPage, skin, new Vector2(cardWidth, BallCardHeight));
            UIKit.PlaceTop(card.RectTransform, gridTop + row * (BallCardHeight + Gap), card.RectTransform.sizeDelta,
                -ContentWidth * 0.5f + cardWidth * 0.5f + column * (cardWidth + Gap));
            card.Clicked += () => SelectBall(skin);
            ballCards.Add(card);
        }
    }

    private void BuildThemesPage()
    {
        UIPalette palette = UIKit.Palette;

        Image panel = UIKit.CreateImage(themesPage, "Preview", UIKit.Skin.chip, palette.night);
        UIKit.SetChipRadius(panel, 36f);
        UIKit.PlaceTop(panel.rectTransform, PreviewTop, new Vector2(ContentWidth, ThemePreviewHeight));
        RectTransform panelRect = panel.rectTransform;

        Image frame = UIKit.CreateImage(panelRect, "ThumbnailFrame", UIKit.Skin.chip, Color.white);
        UIKit.SetChipRadius(frame, 24f);
        UIKit.Place(frame.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(30f, 0f), new Vector2(440f, 340f));
        frame.gameObject.AddComponent<Mask>().showMaskGraphic = false;

        themeThumbnail = UIKit.CreateRect(frame.rectTransform, "Thumbnail").gameObject.AddComponent<RawImage>();
        themeThumbnail.raycastTarget = false;
        UIKit.Stretch(themeThumbnail.rectTransform);

        float visibleWidth = 440f / (340f / SkinCaptureSize.ThumbnailHeight) / SkinCaptureSize.ThumbnailWidth;
        themeThumbnail.uvRect = new Rect(0.5f - visibleWidth * 0.5f, 0f, visibleWidth, 1f);

        RectTransform text = TextColumn(panelRect, 30f + 440f + 40f, 30f, 14f);

        themeName = Layout(UIKit.CreateText(text, "Name", "", UIFont.Display, 80f, palette.text, 2f, TextAlignmentOptions.Left), 88f);
        themeStatus = StatusRow.Create(text, 30f);

        themeDescription = UIKit.CreateText(text, "Description", "", UIFont.Body, 29f, palette.labelMuted, 0f, TextAlignmentOptions.TopLeft);
        themeDescription.textWrappingMode = TextWrappingModes.Normal;

        RectTransform swatches = UIKit.CreateRect(text, "Palette");
        HorizontalLayoutGroup swatchLayout = swatches.gameObject.AddComponent<HorizontalLayoutGroup>();
        swatchLayout.spacing = 10f;
        swatchLayout.childAlignment = TextAnchor.MiddleLeft;
        swatchLayout.childControlWidth = false;
        swatchLayout.childControlHeight = false;
        swatchLayout.childForceExpandWidth = false;
        swatchLayout.childForceExpandHeight = false;
        swatches.gameObject.AddComponent<LayoutElement>().preferredHeight = 40f;

        for (int i = 0; i < 5; i++)
        {
            Image swatch = UIKit.CreateImage(swatches, $"Swatch{i}", UIKit.Skin.chip, Color.white);
            UIKit.SetChipRadius(swatch, 10f);
            swatch.rectTransform.sizeDelta = new Vector2(40f, 40f);
            paletteSwatches.Add(swatch);

            Image ring = UIKit.CreateImage(swatch.rectTransform, "Ring", UIShapes.Ring(10f, 2f), new Color(1f, 1f, 1f, 0.3f));
            ring.type = Image.Type.Sliced;
            UIKit.Stretch(ring.rectTransform);
            paletteRings.Add(ring);
        }

        float gridTop = PreviewTop + ThemePreviewHeight + Gap;

        for (int i = 0; i < db.themes.Length; i++)
        {
            BackgroundTheme theme = db.themes[i];

            if (theme == null)
                continue;

            int column = i % 2;
            int row = i / 2;

            SkinCard card = SkinCard.CreateTheme(themesPage, theme, ThemeCardSize);
            UIKit.PlaceTop(card.RectTransform, gridTop + row * (ThemeCardSize.y + ThemeRowGap), ThemeCardSize,
                -ContentWidth * 0.5f + ThemeCardSize.x * 0.5f + column * (ThemeCardSize.x + Gap));
            card.Clicked += () => SelectTheme(theme);
            themeCards.Add(card);
        }
    }

    public override void Show()
    {
        base.Show();

        if (db == null)
            return;

        SkinProgress.EvaluateUnlocks();

        selectedBall = SkinProgress.EquippedBall;
        selectedTheme = SkinProgress.EquippedTheme;

        if (stage == null)
        {
            stage = SkinPreviewStage.Create(UIKit.Palette.night);
            ballPreviewImage.texture = stage.Texture;
        }

        stage.gameObject.SetActive(true);
        SelectTab(tab);
    }

    public override void Hide()
    {
        if (stage != null)
            stage.gameObject.SetActive(false);

        base.Hide();
    }

    private void OnDestroy()
    {
        if (stage != null)
            Destroy(stage.gameObject);
    }

    private void Update()
    {
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            BackRequested?.Invoke();
    }

    private void SelectTab(Tab newTab)
    {
        tab = newTab;
        tabButtons[0].SetActive(tab == Tab.Balls);
        tabButtons[1].SetActive(tab == Tab.Themes);
        ballsPage.gameObject.SetActive(tab == Tab.Balls);
        themesPage.gameObject.SetActive(tab == Tab.Themes);
        Refresh();
    }

    private void SelectBall(BallSkin skin)
    {
        selectedBall = skin;
        Refresh();
    }

    private void SelectTheme(BackgroundTheme theme)
    {
        selectedTheme = theme;
        Refresh();
    }

    private void Equip()
    {
        if (tab == Tab.Balls)
            SkinProgress.Equip(selectedBall);
        else
            SkinProgress.Equip(selectedTheme);

        Refresh();
    }

    private void Refresh()
    {
        if (db == null)
            return;

        if (tab == Tab.Balls)
            RefreshBalls();
        else
            RefreshThemes();
    }

    private void RefreshBalls()
    {
        int card = 0;

        foreach (BallSkin skin in db.balls)
        {
            if (skin == null)
                continue;

            ballCards[card++].SetState(SkinProgress.IsUnlocked(skin), SkinProgress.IsEquipped(skin), skin == selectedBall,
                skin.unlock.shortText);
        }

        if (selectedBall == null)
            return;

        bool unlocked = SkinProgress.IsUnlocked(selectedBall);
        bool equipped = SkinProgress.IsEquipped(selectedBall);

        stage.Show(selectedBall);
        ballName.text = selectedBall.displayName.ToUpperInvariant();
        ballStatus.Set(unlocked, equipped ? "En uso" : unlocked ? "Desbloqueada" : selectedBall.unlock.shortText);

        for (int i = 0; i < variantDots.Count; i++)
        {
            bool has = selectedBall.variants != null && i < selectedBall.variants.Length;
            variantDots[i].gameObject.SetActive(has);

            if (has)
                variantDots[i].Set(selectedBall.variants[i]);
        }

        equipButton.SetInteractable(unlocked && !equipped);
    }

    private void RefreshThemes()
    {
        int card = 0;

        foreach (BackgroundTheme theme in db.themes)
        {
            if (theme == null)
                continue;

            themeCards[card++].SetState(SkinProgress.IsUnlocked(theme), SkinProgress.IsEquipped(theme), theme == selectedTheme,
                theme.unlock.shortText);
        }

        if (selectedTheme == null)
            return;

        bool unlocked = SkinProgress.IsUnlocked(selectedTheme);
        bool equipped = SkinProgress.IsEquipped(selectedTheme);

        themeThumbnail.texture = selectedTheme.thumbnail;
        themeName.text = selectedTheme.displayName.ToUpperInvariant();
        themeStatus.Set(unlocked, equipped ? "En uso" : unlocked ? "Desbloqueado" : selectedTheme.unlock.shortText);
        themeDescription.text = selectedTheme.description;

        for (int i = 0; i < paletteSwatches.Count; i++)
        {
            bool has = selectedTheme.palette != null && i < selectedTheme.palette.Length;
            paletteSwatches[i].gameObject.SetActive(has);

            if (!has)
                continue;

            Color color = selectedTheme.palette[i];
            paletteSwatches[i].color = color;
            paletteRings[i].gameObject.SetActive(Luminance(color) < 0.06f);
        }

        equipButton.SetInteractable(unlocked && !equipped);
    }

    private static RectTransform TextColumn(RectTransform panel, float left, float right, float spacing)
    {
        RectTransform column = UIKit.CreateRect(panel, "Text");
        column.anchorMin = new Vector2(0f, 0.5f);
        column.anchorMax = new Vector2(1f, 0.5f);
        column.pivot = new Vector2(0.5f, 0.5f);
        column.offsetMin = new Vector2(left, 0f);
        column.offsetMax = new Vector2(-right, 0f);

        VerticalLayoutGroup layout = column.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.spacing = spacing;
        layout.childAlignment = TextAnchor.MiddleLeft;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        ContentSizeFitter fitter = column.gameObject.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        return column;
    }

    private static TextMeshProUGUI Layout(TextMeshProUGUI label, float height)
    {
        LayoutElement element = label.gameObject.AddComponent<LayoutElement>();
        element.preferredHeight = height;
        element.minHeight = height;
        return label;
    }

    private static float Luminance(Color c) =>
        0.2126f * c.r + 0.7152f * c.g + 0.0722f * c.b;

    private static class SkinCaptureSize
    {
        public const float ThumbnailWidth = 956f;
        public const float ThumbnailHeight = 380f;
    }

    private sealed class TabButton
    {
        private Image lip;
        private Image face;
        private TextMeshProUGUI label;

        public event Action Clicked;

        public static TabButton Create(RectTransform tabs, string text, bool left)
        {
            const float padding = 10f;
            Vector2 size = new((ContentWidth - padding * 3f) * 0.5f, TabsHeight - padding * 2f);

            RectTransform root = UIKit.CreateRect(tabs, text);
            UIKit.Place(root, new Vector2(left ? 0f : 1f, 1f), new Vector2(left ? 0f : 1f, 1f),
                new Vector2(left ? padding : -padding, -padding), size);

            TabButton tab = new();

            tab.lip = UIKit.CreateImage(root, "Lip", UIKit.Skin.chip, UIKit.Palette.primaryLip);
            UIKit.SetChipRadius(tab.lip, 28f);
            UIKit.Stretch(tab.lip.rectTransform);
            tab.lip.rectTransform.anchoredPosition = new Vector2(0f, -6f);

            tab.face = UIKit.CreateImage(root, "Face", UIKit.Skin.chip, UIKit.Palette.primary, raycast: true);
            UIKit.SetChipRadius(tab.face, 28f);
            UIKit.Stretch(tab.face.rectTransform);

            tab.label = UIKit.CreateText(root, "Label", text, UIFont.Display, 56f, UIKit.Palette.text, 3f);
            UIKit.Stretch(tab.label.rectTransform);

            Button button = root.gameObject.AddComponent<Button>();
            button.transition = Selectable.Transition.None;
            button.targetGraphic = tab.face;
            button.onClick.AddListener(() => tab.Clicked?.Invoke());

            return tab;
        }

        public void SetActive(bool active)
        {
            lip.enabled = active;

            face.color = active ? UIKit.Palette.primary : Color.clear;
            label.color = active ? UIKit.Palette.text : UIKit.Palette.lockedText;
        }
    }

    private sealed class StatusRow
    {
        private Image dot;
        private Image lockIcon;
        private TextMeshProUGUI label;

        public static StatusRow Create(RectTransform parent, float fontSize)
        {
            RectTransform row = UIKit.CreateRect(parent, "Status");
            HorizontalLayoutGroup layout = row.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 12f;
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childControlWidth = true;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
            row.gameObject.AddComponent<LayoutElement>().preferredHeight = fontSize * 1.25f;

            StatusRow status = new()
            {
                dot = Fixed(UIKit.CreateImage(row, "Dot", UIKit.Skin.disc, UIKit.Palette.success), 22f),
                lockIcon = Fixed(UIKit.CreateImage(row, "Lock", UIKit.Skin.iconLock, UIKit.Palette.lockedSubtext), 28f),
                label = UIKit.CreateText(row, "Label", "", UIFont.Label, fontSize, UIKit.Palette.text, 0f, TextAlignmentOptions.MidlineLeft)
            };

            status.label.rectTransform.sizeDelta = new Vector2(0f, fontSize * 1.25f);
            return status;
        }

        private static Image Fixed(Image image, float size)
        {
            image.rectTransform.sizeDelta = new Vector2(size, size);
            LayoutElement element = image.gameObject.AddComponent<LayoutElement>();
            element.preferredWidth = size;
            element.minWidth = size;
            return image;
        }

        public void Set(bool unlocked, string text)
        {
            dot.gameObject.SetActive(unlocked);
            lockIcon.gameObject.SetActive(!unlocked);
            label.text = text;
            label.color = unlocked ? UIKit.Palette.text : UIKit.Palette.lockedSubtext;
        }
    }

    private sealed class VariantDot
    {
        public const float Size = 44f;

        private Image main;
        private Image second;

        public GameObject gameObject => main.gameObject;

        public static VariantDot Create(RectTransform parent)
        {
            Image disc = UIKit.CreateImage(parent, "Variant", UIKit.Skin.disc, Color.white);
            disc.rectTransform.sizeDelta = new Vector2(Size, Size);
            disc.gameObject.AddComponent<Mask>().showMaskGraphic = true;

            VariantDot dot = new() { main = disc };

            dot.second = UIKit.CreateImage(disc.rectTransform, "Second", null, Color.white);
            RectTransform rect = dot.second.rectTransform;
            rect.anchorMin = new Vector2(1f, 0f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(1f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(14f, 0f);

            return dot;
        }

        public void Set(BallVariant variant)
        {
            main.color = variant.Primary;
            second.gameObject.SetActive(variant.HasSecondary);
            second.color = variant.Secondary;
        }
    }
}
