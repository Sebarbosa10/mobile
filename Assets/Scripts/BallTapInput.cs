using System;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>Expone pulsaciones del Pointer del Input System como un evento de dominio.</summary>
public sealed class BallTapInput : MonoBehaviour
{
    public event Action Tapped;

    private void Update()
    {
        if (Pointer.current != null && Pointer.current.press.wasPressedThisFrame)
            Tapped?.Invoke();
    }
}
