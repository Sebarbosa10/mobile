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

    /// <summary>
    /// Se dispara en cada encestada detectada, tenga o no un
    /// HoopController asignado. La usan los desafíos (por ejemplo el
    /// carrusel de aros) para reaccionar a una encestada sin depender
    /// del sistema de puntaje/dificultad del modo endless.
    /// </summary>
    public event System.Action<HoopScoreTrigger> Scored;

    private void Awake()
    {
        triggerCollider =
            GetComponent<Collider>();

        triggerCollider.isTrigger = true;
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
        Scored?.Invoke(this);
    }
}