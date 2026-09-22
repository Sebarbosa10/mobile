using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Interfaz mínima del modo por récord: puntaje, récord y timer arriba
/// de la pantalla, más una pantalla de Game Over con botón de reinicio.
///
/// Se arma enteramente por código (ver UguiFactory), así BallShotClock
/// puede agregarla como componente propio sin necesitar wiring manual
/// en el Inspector.
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
        UguiFactory.EnsureEventSystem();
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

    private void BuildHud()
    {
        GameObject canvasObject = UguiFactory.CreateCanvas(transform, "SurvivalHud");

        scoreText = UguiFactory.CreateText(
            canvasObject.transform, "ScoreText",
            new Vector2(0, -60), 64, FontStyle.Normal);

        recordText = UguiFactory.CreateText(
            canvasObject.transform, "RecordText",
            new Vector2(0, -130), 40, FontStyle.Normal);

        timerText = UguiFactory.CreateText(
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

        Text title = UguiFactory.CreateText(
            gameOverPanel.transform, "Title",
            new Vector2(0, 180), 90, FontStyle.Bold);
        title.text = "PERDISTE";

        gameOverSummaryText = UguiFactory.CreateText(
            gameOverPanel.transform, "Summary",
            new Vector2(0, 30), 52, FontStyle.Normal);

        Button restartButton = UguiFactory.CreateButton(
            gameOverPanel.transform, "RestartButton",
            new Vector2(0, -180), new Vector2(480, 140),
            "REINTENTAR", new Color(0.89f, 0.44f, 0.13f, 1f));

        restartButton.onClick.AddListener(() => RestartRequested?.Invoke());

        gameOverPanel.SetActive(false);
    }
}
