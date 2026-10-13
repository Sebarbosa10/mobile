using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

/// <summary>
/// Helpers para armar UI (uGUI) enteramente por código, sin depender de
/// un Canvas ya armado a mano en la escena. Los usa cualquier HUD que
/// necesite generarse solo (SurvivalHud, ChallengeHud, el panel de
/// desafíos del menú principal), para no duplicar el mismo plumbing de
/// Canvas/Text/Button en cada uno.
/// </summary>
public static class UguiFactory
{
    public static void EnsureEventSystem()
    {
        if (Object.FindFirstObjectByType<EventSystem>() != null)
            return;

        _ = new GameObject(
            "EventSystem",
            typeof(EventSystem),
            typeof(InputSystemUIInputModule));
    }

    public static GameObject CreateCanvas(Transform parent, string name)
    {
        GameObject canvasObject = new(
            name,
            typeof(Canvas),
            typeof(CanvasScaler),
            typeof(GraphicRaycaster));

        if (parent != null)
            canvasObject.transform.SetParent(parent, false);

        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080, 1920);
        scaler.matchWidthOrHeight = 0.5f;

        return canvasObject;
    }

    public static Text CreateText(
        Transform parent,
        string name,
        Vector2 anchoredPosition,
        int fontSize,
        FontStyle fontStyle)
    {
        GameObject textObject = new(name, typeof(Text));
        textObject.transform.SetParent(parent, false);

        RectTransform rect = textObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 1f);
        rect.anchorMax = new Vector2(0.5f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = new Vector2(1000, 140);

        Text text = textObject.GetComponent<Text>();
        text.font = GetDefaultFont();
        text.fontSize = fontSize;
        text.fontStyle = fontStyle;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = Color.white;
        text.horizontalOverflow = HorizontalWrapMode.Overflow;
        text.verticalOverflow = VerticalWrapMode.Overflow;

        /*
         * El HUD no debe robar los toques del swipe de lanzamiento:
         * solo los botones necesitan responder a input.
         */
        text.raycastTarget = false;

        return text;
    }

    public static Button CreateButton(
        Transform parent,
        string name,
        Vector2 anchoredPosition,
        Vector2 size,
        string label,
        Color color)
    {
        GameObject buttonObject = new(name, typeof(Image), typeof(Button));
        buttonObject.transform.SetParent(parent, false);

        RectTransform rect = buttonObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = size;

        Image buttonImage = buttonObject.GetComponent<Image>();
        buttonImage.sprite = GetDefaultButtonSprite();
        buttonImage.type = Image.Type.Sliced;
        buttonImage.color = color;

        Text buttonText = CreateText(
            buttonObject.transform, "Label",
            Vector2.zero, 48, FontStyle.Bold);

        RectTransform textRect = buttonText.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.pivot = new Vector2(0.5f, 0.5f);
        textRect.anchoredPosition = Vector2.zero;
        textRect.sizeDelta = Vector2.zero;
        buttonText.text = label;

        return buttonObject.GetComponent<Button>();
    }

    public static Font GetDefaultFont()
    {
        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        if (font == null)
            font = Resources.GetBuiltinResource<Font>("Arial.ttf");

        return font;
    }

    public static Sprite GetDefaultButtonSprite()
    {
        return Resources.GetBuiltinResource<Sprite>("UI/Skin/UISprite.psd");
    }
}
