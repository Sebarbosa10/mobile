using System;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Lógica de pausa compartida por todos los modos (endless y desafíos).
/// No tiene UI: la UI escucha PausedChanged a través de IGameSession.
///
/// En pausa el tiempo se congela (Time.timeScale = 0) y se desactiva el
/// BallLauncher para que tocar la pantalla no agarre la pelota. También
/// pausan el botón "atrás" de Android (llega como Escape) y que la app
/// pase a segundo plano.
/// </summary>
public sealed class GamePause : MonoBehaviour
{
    private BallLauncher ballLauncher;
    private bool launcherWasEnabled;
    private bool isAvailable = true;

    public event Action<bool> PausedChanged;

    public bool IsPaused { get; private set; }

    private void OnDestroy()
    {
        // Time.timeScale es global: si la escena se descarga estando en
        // pausa, no queremos que el resto del juego arranque congelado.
        Time.timeScale = 1f;
    }

    public void SetBallLauncher(BallLauncher launcher)
    {
        ballLauncher = launcher;
    }

    /// <summary>
    /// Habilita o no la pausa. Los modos la deshabilitan mientras se
    /// muestra la pantalla de resultado o de Game Over.
    /// </summary>
    public void SetAvailable(bool available)
    {
        isAvailable = available;

        if (!available && IsPaused)
            ClearPause();
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

        PausedChanged?.Invoke(true);
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
        if (!IsPaused)
            return;

        IsPaused = false;
        Time.timeScale = 1f;

        PausedChanged?.Invoke(false);
    }
}
