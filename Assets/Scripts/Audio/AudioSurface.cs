using UnityEngine;

// Fuerza el sonido de impacto de la pelota para este collider (y sus hijos).
public sealed class AudioSurface : MonoBehaviour
{
    [SerializeField]
    private BallSurface surface = BallSurface.Soft;

    public BallSurface Surface =>
        surface;
}
