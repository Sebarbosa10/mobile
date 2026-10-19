using System;
using UnityEngine;
using UnityEngine.InputSystem;

public sealed class GamePause : MonoBehaviour
{
    private BallLauncher ballLauncher;
    private bool launcherWasEnabled;
    private bool isAvailable = true;

    public event Action<bool> PausedChanged;

    public bool IsPaused { get; private set; }

    private void OnDestroy()
    {
        Time.timeScale = 1f;
    }

    public void SetBallLauncher(BallLauncher launcher)
    {
        ballLauncher = launcher;
    }

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

    public void ClearPause()
    {
        if (!IsPaused)
            return;

        IsPaused = false;
        Time.timeScale = 1f;

        PausedChanged?.Invoke(false);
    }
}
