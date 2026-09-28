using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Interfaz del modo por récord: puntaje, récord y timer arriba de la
/// pantalla, más una pantalla de Game Over con botones de reinicio y
/// menú.
///
/// Toda la UI ya existe armada en la escena (Canvas, textos, botones):
/// este componente no instancia nada, solo lee/escribe sobre esas
/// referencias y las activa/desactiva. Así no hay costo de generar UI
/// en tiempo de ejecución cada vez que arranca la partida.
/// </summary>
public sealed class SurvivalHud : MonoBehaviour
{
    [Header("HUD")]
    [SerializeField]
    private Text scoreText;

    [SerializeField]
    private Text recordText;

    [SerializeField]
    private Text timerText;

    [Header("Game Over")]
    [SerializeField]
    private GameObject gameOverPanel;

    [SerializeField]
    private Text gameOverSummaryText;

    [SerializeField]
    private Button restartButton;

    [SerializeField]
    private Button exitButton;

    private readonly Color urgentColor = new(0.9f, 0.2f, 0.2f);
    private readonly Color calmColor = Color.white;

    public event Action RestartRequested;
    public event Action ExitRequested;

    private void Awake()
    {
        restartButton.onClick.AddListener(() => RestartRequested?.Invoke());
        exitButton.onClick.AddListener(() => ExitRequested?.Invoke());
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
}
