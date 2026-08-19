using UnityEngine;

[RequireComponent(typeof(Collider))]
public sealed class HoopScoreTrigger : MonoBehaviour
{
    [Header("References")]
    [SerializeField]
    private HoopController hoopController;

    [Header("Ball")]
    [SerializeField]
    private string ballTag = "Ball";

    [Header("Validation")]
    [SerializeField, Min(0f)]
    private float minimumDownwardVelocity = 0.1f;

    [SerializeField, Min(0.05f)]
    private float scoreCooldown = 0.5f;

    private Collider triggerCollider;

    private float lastScoreTime =
        -Mathf.Infinity;

    private void Awake()
    {
        triggerCollider =
            GetComponent<Collider>();

        triggerCollider.isTrigger = true;

        if (hoopController == null)
        {
            // Sin esta referencia el trigger detecta la pelota pero nunca suma
            // puntos, y eso es silencioso y difícil de diagnosticar en un
            // dispositivo. Mejor avisar apenas arranca la escena.
            Debug.LogWarning(
                "HoopScoreTrigger: no tiene un HoopController asignado, no va a sumar puntos.",
                this);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!IsValidBall(other))
            return;

        if (!IsMovingDownward(other))
            return;

        if (IsOnCooldown())
            return;

        RegisterScore();
    }

    private bool IsValidBall(Collider other)
    {
        return other.CompareTag(ballTag);
    }

    private bool IsMovingDownward(Collider other)
    {
        Rigidbody rigidbody =
            other.attachedRigidbody;

        if (rigidbody == null)
            return false;

        return rigidbody.linearVelocity.y <
               -minimumDownwardVelocity;
    }

    private bool IsOnCooldown()
    {
        return Time.time <
               lastScoreTime +
               scoreCooldown;
    }

    private void RegisterScore()
    {
        lastScoreTime =
            Time.time;

        // Confirma que la detección física funcionó, independientemente de si
        // el HoopController después logra sumar el punto o no. Separar estos
        // dos logs ayuda a distinguir un problema de físicas/trigger de uno
        // de wiring/referencias.
        Debug.Log("[HoopScoreTrigger] Encestada detectada.", this);

        hoopController?.AddPoint();
    }
}