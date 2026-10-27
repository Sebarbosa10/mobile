using UnityEngine;

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

    [Tooltip("Mínimo de segundos entre dos sonidos de esquive.")]
    [SerializeField, Min(0f)]
    private float dashSoundCooldown = 0.9f;

    private Vector3 basePosition;
    private float elapsedTime;
    private float lastDashSoundTime = float.NegativeInfinity;

    private void Awake()
    {
        basePosition = transform.position;
    }

    private void Update()
    {
        float previousDirection = Mathf.Sign(Mathf.Cos(elapsedTime * horizontalSpeed));

        elapsedTime += Time.deltaTime;

        // Cada vez que el aro cambia de dirección "esquiva": suena un whoosh
        if (Mathf.Sign(Mathf.Cos(elapsedTime * horizontalSpeed)) != previousDirection &&
            Time.time - lastDashSoundTime >= dashSoundCooldown)
        {
            lastDashSoundTime = Time.time;
            AudioManager.PlayEvasiveDash();
        }

        Vector3 offset = new(
            Mathf.Sin(elapsedTime * horizontalSpeed) * horizontalRadius,
            Mathf.Sin(elapsedTime * verticalSpeed) * verticalAmplitude,
            (Mathf.Cos(elapsedTime * horizontalSpeed) - 1f) * horizontalRadius);

        transform.position = basePosition + offset;
    }
}
