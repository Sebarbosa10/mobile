using System;
using UnityEngine;

/// <summary>Contrato de entrada para la interacción de una pelota.</summary>
public interface IBallGestureSource
{
    event Action<Vector2> GestureStarted;
    event Action<Vector2> GestureMoved;
    event Action<Vector2> GestureReleased;
}
