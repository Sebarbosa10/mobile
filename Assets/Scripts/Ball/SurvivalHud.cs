using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

/// <summary>
/// Interfaz mínima del modo por récord: puntaje, récord y timer arriba
/// de la pantalla, más una pantalla de Game Over con botón de reinicio.
///
/// Se arma enteramente por código (no depende de un Canvas ya armado en
/// la escena), así BallShotClock puede agregarla como componente propio
/// sin necesitar wiring manual en el Inspector.
/// </summary>
public sealed class SurvivalHud : MonoBehaviour
{
    private Text scoreText;
    private Text recordText;
    private Text timerText;

    private GameObject gameOverPanel;
    private Text gameOverSummaryText;

    private readonly Color urgentColor = new(0.9f, 0.2f, 0.2f);
    private readonly Color calmColor = Color.white;

    public event Action RestartRequested;

    private void Awake()
    {
        EnsureEventSystem();
        BuildHud();
    }

    public void SetScore(int score)
    {
        scoreText.text = $"Puntaje: {score}";
    }

    public void SetRecord(int record)
    {
        recordText.text = $"Récord: {record}";
    }

    public void SetRemainingTime(float remaining, float total)
    {
        timerText.text = remaining.ToString("0.0");

        float urgency = total > 0f ? 1f - Mathf.Clamp01(remaining / total) : 1f;
        timerText.color = Color.Lerp(calmColor, urgentColor, urgency);
    }

    public void ShowGameOver(int finalScore, int record, bool isNewRecord)
    {
        gameOverSummaryText.text = isNewRecord
            ? $"¡Nuevo récord!\nPuntaje: {finalScore}"
            : $"Puntaje: {finalScore}\nRécord: {record}";

        gameOverPanel.SetActive(true);
    }

    public void HideGameOver()
    {
        gameOverPanel.SetActive(false);
    }

    private static void EnsureEventSystem()
    {
        if (FindFirstObjectByType<EventSystem>() != null)
            return;

        _ = new GameObject(
            "EventSystem",
            typeof(EventSystem),
            typeof(InputSystemUIInputModule));
    }

    private void BuildHud()
    {
        GameObject canvasObject = new(
            "SurvivalHud",
            typeof(Canvas),
            typeof(CanvasScaler),
            typeof(GraphicRaycaster));

        canvasObject.transform.SetParent(transform, false);

        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080, 1920);
        scaler.matchWidthOrHeight = 0.5f;

        scoreText = CreateText(
            canvasObject.transform, "ScoreText",
            new Vector2(0, -60), 64, FontStyle.Normal);

        recordText = CreateText(
            canvasObject.transform, "RecordText",
            new Vector2(0, -130), 40, FontStyle.Normal);

        timerText = CreateText(
            canvasObject.transform, "TimerText",
            new Vector2(0, -230), 100, FontStyle.Bold);

        BuildGameOverPanel(canvasObject.transform);
    }

    private void BuildGameOverPanel(Transform parent)
    {
        gameOverPanel = new GameObject("GameOverPanel", typeof(Image));
        gameOverPanel.transform.SetParent(parent, false);

        RectTransform panelRect = gameOverPanel.GetComponent<RectTransform>();
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.one;
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;

        gameOverPanel.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.75f);

        Text title = CreateText(
            gameOverPanel.transform, "Title",
            new Vector2(0, 180), 90, FontStyle.Bold);
        title.text = "PERDISTE";

        gameOverSummaryText = CreateText(
            gameOverPanel.transform, "Summary",
            new Vector2(0, 30), 52, FontStyle.Normal);

        Button restartButton = CreateButton(
            gameOverPanel.transform, new Vector2(0, -180), "REINTENTAR");

        restartButton.onClick.AddListener(() => RestartRequested?.Invoke());

        gameOverPanel.SetActive(false);
    }

    private static Text CreateText(
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
         * solo el botón de reinicio necesita responder a input.
         */
        text.raycastTarget = false;

        return text;
    }

    private static Button CreateButton(
        Transform parent,
        Vector2 anchoredPosition,
        string label)
    {
        GameObject buttonObject = new("RestartButton", typeof(Image), typeof(Button));
        buttonObject.transform.SetParent(parent, false);

        RectTransform rect = buttonObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = new Vector2(480, 140);

        buttonObject.GetComponent<Image>().color = new Color(0.89f, 0.44f, 0.13f, 1f);

        Text buttonText = CreateText(
            buttonObject.transform, "Label",
            Vector2.zero, 48, FontStyle.Bold);

        buttonText.GetComponent<RectTransform>().anchorMin = Vector2.zero;
        buttonText.GetComponent<RectTransform>().anchorMax = Vector2.one;
        buttonText.GetComponent<RectTransform>().pivot = new Vector2(0.5f, 0.5f);
        buttonText.GetComponent<RectTransform>().sizeDelta = Vector2.zero;
        buttonText.text = label;

        return buttonObject.GetComponent<Button>();
    }

    private static Font GetDefaultFont()
    {
        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        if (font == null)
            font = Resources.GetBuiltinResource<Font>("Arial.ttf");

        return font;
    }
}
