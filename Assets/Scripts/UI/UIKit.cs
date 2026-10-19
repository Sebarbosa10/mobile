using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

public static class UIKit
{
    public const float ReferenceWidth = 1080f;
    public const float ReferenceHeight = 1920f;

    public const float TopReserve = 96f;
    public const float BottomReserve = 64f;

    public const float SideMargin = 48f;

    public const float ButtonLip = 12f;

    public const float PanelLip = 14f;

    private const float ChipSpriteRadius = 28f;

    public static UISkin Skin =>
        UISkin.Instance;

    public static UIPalette Palette =>
        UISkin.Palette;

    public static void EnsureEventSystem()
    {
        if (Object.FindFirstObjectByType<EventSystem>() != null)
            return;

        _ = new GameObject(
            "EventSystem",
            typeof(EventSystem),
            typeof(InputSystemUIInputModule));
    }

    public static Canvas CreateCanvas(string name, int sortingOrder)
    {
        GameObject canvasObject = new(
            name,
            typeof(Canvas),
            typeof(CanvasScaler),
            typeof(GraphicRaycaster));

        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = sortingOrder;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(ReferenceWidth, ReferenceHeight);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0f;
        scaler.referencePixelsPerUnit = 100f;

        return canvas;
    }

    public static RectTransform CreateSafeArea(Transform parent)
    {
        RectTransform safe = CreateRect(parent, "SafeArea");
        Stretch(safe);

        safe.gameObject.AddComponent<SafeAreaFitter>()
            .SetMinimumInsets(TopReserve, BottomReserve);

        return safe;
    }

    public static RectTransform CreateRect(Transform parent, string name)
    {
        GameObject go = new(name, typeof(RectTransform));
        go.layer = 5;

        RectTransform rect = go.GetComponent<RectTransform>();
        rect.SetParent(parent, false);

        return rect;
    }

    public static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    public static void Place(
        RectTransform rect,
        Vector2 anchor,
        Vector2 pivot,
        Vector2 position,
        Vector2 size)
    {
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.pivot = pivot;
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
    }

    public static void PlaceTop(RectTransform rect, float top, Vector2 size, float x = 0f) =>
        Place(rect, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(x, -top), size);

    public static void PlaceTopLeft(RectTransform rect, float left, float top, Vector2 size) =>
        Place(rect, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(left, -top), size);

    public static void PlaceTopRight(RectTransform rect, float right, float top, Vector2 size) =>
        Place(rect, Vector2.one, Vector2.one, new Vector2(-right, -top), size);

    public static void PlaceBottom(RectTransform rect, float bottom, Vector2 size, float x = 0f) =>
        Place(rect, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(x, bottom), size);

    public static void PlaceCenter(RectTransform rect, Vector2 offset, Vector2 size) =>
        Place(rect, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), offset, size);

    public static Image CreateImage(
        Transform parent,
        string name,
        Sprite sprite,
        Color color,
        bool raycast = false)
    {
        RectTransform rect = CreateRect(parent, name);

        Image image = rect.gameObject.AddComponent<Image>();
        image.sprite = sprite;
        image.color = color;
        image.raycastTarget = raycast;

        if (sprite != null && sprite.border != Vector4.zero)
            image.type = Image.Type.Sliced;

        return image;
    }

    public static void SetChipRadius(Image image, float radius)
    {
        image.pixelsPerUnitMultiplier = ChipSpriteRadius / Mathf.Max(1f, radius);
    }

    public static TextMeshProUGUI CreateText(
        Transform parent,
        string name,
        string text,
        UIFont font,
        float size,
        Color color,
        float trackingPx = 0f,
        TextAlignmentOptions alignment = TextAlignmentOptions.Center)
    {
        RectTransform rect = CreateRect(parent, name);

        TextMeshProUGUI label = rect.gameObject.AddComponent<TextMeshProUGUI>();
        label.font = Skin.GetFont(font);
        label.fontSize = size;
        label.color = color;
        label.alignment = alignment;
        label.textWrappingMode = TextWrappingModes.NoWrap;
        label.overflowMode = TextOverflowModes.Overflow;

        label.characterSpacing = size > 0f ? trackingPx / size * 100f : 0f;

        label.text = text;

        label.raycastTarget = false;

        return label;
    }

    public static RectTransform CreateShadowedText(
        Transform parent,
        string name,
        string text,
        string shadowText,
        UIFont font,
        float size,
        Color color,
        float shadowOffset,
        float trackingPx,
        out TextMeshProUGUI label,
        out TextMeshProUGUI shadow)
    {
        RectTransform container = CreateRect(parent, name);

        shadow = CreateText(container, "Shadow", shadowText, font, size, Palette.night, trackingPx);
        Stretch(shadow.rectTransform);
        shadow.rectTransform.anchoredPosition = new Vector2(0f, -shadowOffset);

        label = CreateText(container, "Text", text, font, size, color, trackingPx);
        Stretch(label.rectTransform);

        return container;
    }

    public static RectTransform CreateChip(
        Transform parent,
        string name,
        float height,
        Color background,
        float paddingX,
        float spacing)
    {
        Image image = CreateImage(parent, name, Skin.pill, background);
        RectTransform rect = image.rectTransform;
        rect.pivot = new Vector2(0.5f, 1f);
        rect.sizeDelta = new Vector2(0f, height);

        HorizontalLayoutGroup layout = rect.gameObject.AddComponent<HorizontalLayoutGroup>();
        layout.padding = new RectOffset((int)paddingX, (int)paddingX, 0, 0);
        layout.spacing = spacing;
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;

        ContentSizeFitter fitter = rect.gameObject.AddComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        fitter.verticalFit = ContentSizeFitter.FitMode.Unconstrained;

        return rect;
    }

    public static Image AddChipIcon(RectTransform chip, Sprite sprite, float size, Color color)
    {
        Image icon = CreateImage(chip, "Icon", sprite, color);
        icon.rectTransform.sizeDelta = new Vector2(size, size);
        icon.preserveAspect = true;

        LayoutElement element = icon.gameObject.AddComponent<LayoutElement>();
        element.preferredWidth = size;
        element.minWidth = size;

        return icon;
    }

    public static TextMeshProUGUI AddChipText(
        RectTransform chip,
        string text,
        UIFont font,
        float size,
        Color color,
        float trackingPx = 0f)
    {
        TextMeshProUGUI label = CreateText(chip, "Text", text, font, size, color, trackingPx);
        label.rectTransform.sizeDelta = new Vector2(0f, chip.sizeDelta.y);

        return label;
    }

    public static TextMeshProUGUI CreateRecordChip(
        Transform parent,
        string name,
        float height,
        Color background,
        float iconSize,
        float labelSize,
        float labelTracking,
        float valueSize,
        float paddingX,
        float spacing,
        out RectTransform chip)
    {
        chip = CreateChip(parent, name, height, background, paddingX, spacing);

        AddChipIcon(chip, Skin.iconTrophy, iconSize, Palette.sun);
        AddChipText(chip, "RÉCORD", UIFont.Label, labelSize, Palette.text, labelTracking);

        return AddChipText(chip, "0", UIFont.Display, valueSize, Palette.sun);
    }

    public static RectTransform CreateBackground(Transform parent, bool sky)
    {
        Image background = sky
            ? CreateImage(parent, "Background", Skin.backgroundSky, Color.white)
            : CreateImage(parent, "Background", null, Palette.background);

        Stretch(background.rectTransform);

        Image skyline = CreateImage(background.rectTransform, "Skyline", Skin.backgroundSkyline, Color.white);
        RectTransform skylineRect = skyline.rectTransform;
        skylineRect.anchorMin = new Vector2(0f, 0f);
        skylineRect.anchorMax = new Vector2(1f, 0f);
        skylineRect.pivot = new Vector2(0.5f, 0f);
        skylineRect.anchoredPosition = Vector2.zero;
        skylineRect.sizeDelta = new Vector2(0f, 420f);

        return background.rectTransform;
    }
}
