using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

/// <summary>Traduce el touch primario del dispositivo a eventos de gesto.</summary>
public sealed class BallTouchInput : MonoBehaviour, IBallGestureSource
{
    public event Action<Vector2> GestureStarted;
    public event Action<Vector2> GestureMoved;
    public event Action<Vector2> GestureReleased;

    private void Update()
    {
        if (Touchscreen.current == null)
            return;

        TouchControl touch = Touchscreen.current.primaryTouch;
        Vector2 position = touch.position.ReadValue();

        if (touch.press.wasPressedThisFrame)
            GestureStarted?.Invoke(position);

        if (touch.press.isPressed)
            GestureMoved?.Invoke(position);

        if (touch.press.wasReleasedThisFrame)
            GestureReleased?.Invoke(position);
    }
}
