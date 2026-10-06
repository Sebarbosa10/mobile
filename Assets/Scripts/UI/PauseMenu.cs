using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// Pausa compartida por todos los modos de juego (endless y desafíos):
/// un botón de pausa arriba a la derecha y un panel con Reanudar,
/// Reiniciar y Menú. Se arma por código (ver UguiFactory) en un Canvas
/// propio que se dibuja por encima del HUD del modo.
///
/// En pausa el tiempo se congela (Time.timeScale = 0) y se desactiva el
/// BallLauncher para que tocar la pantalla no agarre la pelota. También
/// pausan el botón "atrás" de Android (llega como Escape) y que la app
/// pase a segundo plano.
///
/// Reanudar lo resuelve este componente; Reiniciar y Menú dependen de
/// cada modo, así que solo se avisan por evento. El modo debe llamar a
/// ClearPause antes de reiniciar o salir.
/// </summary>
public sealed class PauseMenu : MonoBehaviour
{
    private const int CanvasSortingOrder = 10;

    private BallLauncher ballLauncher;
    private bool launcherWasEnabled;

    private bool isAvailable = true;

    private GameObject pauseButtonObject;
    private GameObject pausePanel;

    public event Action RestartRequested;
    public event Action ExitRequested;

    public bool IsPaused { get; private set; }

    private void Awake()
    {
        UguiFactory.EnsureEventSystem();
        BuildUi();
    }

    private void OnDestroy()
    {
        // Time.timeScale es global: si la escena se descarga estando en
        // pausa, no queremos que el resto del juego arranque congelado.
        Time.timeScale = 1f;
    }

    /// <summary>
    /// Lanzador a desactivar mientras dure la pausa.
    /// </summary>
    public void SetBallLauncher(BallLauncher launcher)
    {
        ballLauncher = launcher;
    }

    /// <summary>
    /// Habilita o no la pausa (y muestra u oculta su botón). Los modos la
    /// deshabilitan mientras se muestra la pantalla de resultado o de
    /// Game Over.
    /// </summary>
    public void SetAvailable(bool available)
    {
        isAvailable = available;

        if (!available && IsPaused)
            ClearPause();

        pauseButtonObject.SetActive(available && !IsPaused);
    }

    private void Update()
    {
        if (Keyboard.current == null ||
            !Keyboard.current.escapeKey.wasPressedThisFrame)
            return;

        if (IsPaused)
            Resume();
        else
            Pause();
    }

    private void OnApplicationPause(bool paused)
    {
        // Al volver a la app, el jugador encuentra la partida en pausa
        // en vez de con el tiempo corriendo.
        if (paused)
            Pause();
    }

    public void Pause()
    {
        if (!isAvailable || IsPaused)
            return;

        IsPaused = true;
        Time.timeScale = 0f;

        if (ballLauncher != null)
        {
            launcherWasEnabled = ballLauncher.enabled;
            ballLauncher.enabled = false;
        }

        pauseButtonObject.SetActive(false);
        pausePanel.SetActive(true);
    }

    public void Resume()
    {
        if (!IsPaused)
            return;

        ClearPause();

        if (ballLauncher != null)
            ballLauncher.enabled = launcherWasEnabled;
    }

    /// <summary>
    /// Sale de la pausa sin restaurar el lanzador: lo usan los modos
    /// justo antes de reiniciar o volver al menú, que ya se encargan
    /// ellos mismos del estado de la pelota.
    /// </summary>
    public void ClearPause()
    {
        IsPaused = false;
        Time.timeScale = 1f;

        pausePanel.SetActive(false);
        pauseButtonObject.SetActive(isAvailable);
    }

    private void BuildUi()
    {
        // En la raíz de la escena: si quedara bajo otro Canvas (como el
        // del HUD), sería un Canvas anidado e ignoraría su propio
        // sortingOrder y CanvasScaler.
        GameObject safeArea = UguiFactory.CreateCanvas(null, "PauseCanvas");

        safeArea.GetComponentInParent<Canvas>().sortingOrder =
            CanvasSortingOrder;

        Button pauseButton = UguiFactory.CreateButton(
            safeArea.transform, "PauseButton",
            Vector2.zero, new Vector2(140, 140),
            "II", new Color(0f, 0f, 0f, 0.45f));

        RectTransform pauseRect = pauseButton.GetComponent<RectTransform>();
        pauseRect.anchorMin = Vector2.one;
        pauseRect.anchorMax = Vector2.one;
        pauseRect.pivot = Vector2.one;
        pauseRect.anchoredPosition = new Vector2(-40, -40);

        pauseButton.onClick.AddListener(Pause);
        pauseButtonObject = pauseButton.gameObject;

        pausePanel = new GameObject("PausePanel", typeof(Image));
        pausePanel.transform.SetParent(safeArea.transform, false);

        RectTransform panelRect = pausePanel.GetComponent<RectTransform>();
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.one;
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;

        pausePanel.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.75f);

        // CreateText ancla al borde superior: la Y se mide desde arriba.
        Text title = UguiFactory.CreateText(
            pausePanel.transform, "Title",
            new Vector2(0, -520), 90, FontStyle.Bold);
        title.text = "PAUSA";

        Button resumeButton = UguiFactory.CreateButton(
            pausePanel.transform, "ResumeButton",
            new Vector2(0, 80), new Vector2(520, 150),
            "REANUDAR", new Color(0.19607843f, 0.4117647f, 0.7607843f, 1f));
        resumeButton.onClick.AddListener(Resume);

        Button restartButton = UguiFactory.CreateButton(
            pausePanel.transform, "RestartButton",
            new Vector2(0, -100), new Vector2(520, 150),
            "REINICIAR", new Color(0.89f, 0.44f, 0.13f, 1f));
        restartButton.onClick.AddListener(() => RestartRequested?.Invoke());

        Button menuButton = UguiFactory.CreateButton(
            pausePanel.transform, "MenuButton",
            new Vector2(0, -280), new Vector2(520, 150),
            "MENÚ", new Color(0.3f, 0.3f, 0.3f, 1f));
        menuButton.onClick.AddListener(() => ExitRequested?.Invoke());

        pausePanel.SetActive(false);
    }
}
