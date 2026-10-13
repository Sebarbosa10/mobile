using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Interfaz compartida por los tres desafíos: una línea de progreso
/// arriba (con una etiqueta configurable, por ejemplo "Aros: X/Y" o
/// "Puntos: X/Y"), un timer opcional debajo, y una pantalla de
/// resultado (victoria o derrota) con botones para reintentar o
/// volver al menú. Armada por código, igual que SurvivalHud (ver
/// UguiFactory).
/// </summary>
public sealed class ChallengeHud : MonoBehaviour
{
    private Text progressText;
    private Text timerText;

    private GameObject resultPanel;
    private Text resultTitleText;
    private Text resultSummaryText;

    public event Action RestartRequested;
    public event Action ExitRequested;

    private void Awake()
    {
        UguiFactory.EnsureEventSystem();
        BuildHud();
    }

    public void SetProgress(string label, int current, int total)
    {
        progressText.text = $"{label}: {current}/{total}";
    }

    public void ShowTimer(float remaining)
    {
        timerText.gameObject.SetActive(true);
        timerText.text = $"Tiempo: {remaining:0.0}";
    }

    public void HideTimer()
    {
        timerText.gameObject.SetActive(false);
    }

    public void ShowResult(bool won, string summary)
    {
        resultTitleText.text = won ? "¡DESAFÍO COMPLETO!" : "PERDISTE";
        resultSummaryText.text = summary;
        resultPanel.SetActive(true);
    }

    public void HideResult()
    {
        resultPanel.SetActive(false);
    }

    private void BuildHud()
    {
        GameObject canvasObject = UguiFactory.CreateCanvas(transform, "ChallengeHud");

        progressText = UguiFactory.CreateText(
            canvasObject.transform, "ProgressText",
            new Vector2(0, -60), 64, FontStyle.Bold);

        timerText = UguiFactory.CreateText(
            canvasObject.transform, "TimerText",
            new Vector2(0, -130), 44, FontStyle.Normal);

        timerText.gameObject.SetActive(false);

        BuildResultPanel(canvasObject.transform);
    }

    private void BuildResultPanel(Transform parent)
    {
        resultPanel = new GameObject("ResultPanel", typeof(Image));
        resultPanel.transform.SetParent(parent, false);

        RectTransform panelRect = resultPanel.GetComponent<RectTransform>();
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.one;
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;

        resultPanel.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.75f);

        resultTitleText = UguiFactory.CreateText(
            resultPanel.transform, "Title",
            new Vector2(0, 180), 80, FontStyle.Bold);

        resultSummaryText = UguiFactory.CreateText(
            resultPanel.transform, "Summary",
            new Vector2(0, 60), 44, FontStyle.Normal);

        Button restartButton = UguiFactory.CreateButton(
            resultPanel.transform, "RestartButton",
            new Vector2(0, -100), new Vector2(480, 140),
            "REINTENTAR", new Color(0.89f, 0.44f, 0.13f, 1f));

        restartButton.onClick.AddListener(() => RestartRequested?.Invoke());

        Button exitButton = UguiFactory.CreateButton(
            resultPanel.transform, "ExitButton",
            new Vector2(0, -280), new Vector2(480, 140),
            "MENÚ", new Color(0.3f, 0.3f, 0.3f, 1f));

        exitButton.onClick.AddListener(() => ExitRequested?.Invoke());

        resultPanel.SetActive(false);
    }
}
