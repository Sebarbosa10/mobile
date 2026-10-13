using UnityEngine;

/// <summary>
/// Mueve al aro en un patrón combinado (círculo horizontal + rebote
/// vertical, cada uno a su propia velocidad) que nunca se repite de
/// forma predecible. Es el aro "esquivo" del Desafío 2.
/// </summary>
public sealed class EvasiveHoopMover : MonoBehaviour
{
    [SerializeField, Min(0f)]
    private float horizontalRadius = 1.2f;

    [SerializeField, Min(0f)]
    private float verticalAmplitude = 0.6f;

    [SerializeField, Min(0f)]
    private float horizontalSpeed = 2.5f;

    [SerializeField, Min(0f)]
    private float verticalSpeed = 1.7f;

    private Vector3 basePosition;
    private float elapsedTime;

    private void Awake()
    {
        basePosition = transform.position;
    }

    private void Update()
    {
        elapsedTime += Time.deltaTime;

        Vector3 offset = new(
            Mathf.Sin(elapsedTime * horizontalSpeed) * horizontalRadius,
            Mathf.Sin(elapsedTime * verticalSpeed) * verticalAmplitude,
            (Mathf.Cos(elapsedTime * horizontalSpeed) - 1f) * horizontalRadius);

        transform.position = basePosition + offset;
    }
}
