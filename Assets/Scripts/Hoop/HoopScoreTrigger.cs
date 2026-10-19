using System.Collections.Generic;
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

    [Header("Aro")]
    [Tooltip("Centro del aro: su plano XZ local es el que tiene que cruzar la pelota. Vacío = se usa el 'RimColliders' hermano de este trigger.")]
    [SerializeField]
    private Transform rimCenter;

    [Tooltip("Radio interior del aro en el espacio de rimCenter. 0 = se calcula de los colliders del aro.")]
    [SerializeField, Min(0f)]
    private float rimInnerRadius;

    [Header("Validation")]
    [SerializeField, Min(0f)]
    private float minimumDownwardVelocity = 0.1f;

    [SerializeField, Min(0.05f)]
    private float scoreCooldown = 0.5f;

    private Collider triggerCollider;

    private HoopVisual visual;

    private float planeY;
    private float innerRadius;

    private Rigidbody trackedBall;
    private Vector3 lastLocalPosition;
    private bool enteredFromBelow;

    private float lastScoreTime =
        -Mathf.Infinity;

    private static readonly List<HoopScoreTrigger> active = new();

    public static IReadOnlyList<HoopScoreTrigger> Active =>
        active;

    public Vector3 RimCenterWorld =>
        rimCenter.TransformPoint(new Vector3(0f, planeY, 0f));

    public float RimRadiusWorld =>
        innerRadius * rimCenter.lossyScale.x;

    public HoopVisual Visual =>
        visual;

    private void OnEnable()
    {
        active.Add(this);
    }

    private void OnDisable()
    {
        active.Remove(this);
    }

    public event System.Action<HoopScoreTrigger> Scored;

    private void Awake()
    {
        triggerCollider =
            GetComponent<Collider>();

        triggerCollider.isTrigger = true;

        visual =
            GetComponentInParent<HoopVisual>();

        ResolveRim();
    }

    private void ResolveRim()
    {
        if (rimCenter == null && transform.parent != null)
            rimCenter = transform.parent.Find("RimColliders");

        if (rimCenter != null)
        {
            planeY = 0f;

            if (rimInnerRadius <= 0f)
            {
                CapsuleCollider segment = rimCenter.GetComponentInChildren<CapsuleCollider>();

                if (segment != null)
                {
                    Vector3 p = segment.transform.localPosition;
                    rimInnerRadius = new Vector2(p.x, p.z).magnitude - segment.radius;
                }
            }

            if (rimInnerRadius > 0f)
            {
                innerRadius = rimInnerRadius;
                return;
            }
        }

        rimCenter = transform;

        if (triggerCollider is BoxCollider box)
        {
            planeY = box.center.y + box.size.y * 0.5f;
            innerRadius = rimInnerRadius > 0f
                ? rimInnerRadius
                : Mathf.Min(box.size.x, box.size.z) * 0.5f;
        }
        else
        {
            planeY = transform.InverseTransformPoint(triggerCollider.bounds.max).y;
            innerRadius = rimInnerRadius > 0f ? rimInnerRadius : 0.2f;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!IsValidBall(other))
            return;

        Rigidbody ball = other.attachedRigidbody;

        if (ball == null)
            return;

        trackedBall = ball;
        lastLocalPosition = ToLocal(ball.position - ball.linearVelocity * Time.fixedDeltaTime);
        enteredFromBelow = lastLocalPosition.y < planeY;

        CheckCrossing();
    }

    private void OnTriggerStay(Collider other)
    {
        if (trackedBall != null && other.attachedRigidbody == trackedBall)
            CheckCrossing();
    }

    private void OnTriggerExit(Collider other)
    {
        if (trackedBall == null || other.attachedRigidbody != trackedBall)
            return;

        CheckCrossing();
        trackedBall = null;
    }

    private bool IsValidBall(Collider other)
    {
        return other.CompareTag(ballTag);
    }

    private Vector3 ToLocal(Vector3 world) =>
        rimCenter.InverseTransformPoint(world);

    private void CheckCrossing()
    {
        Vector3 previous = lastLocalPosition;
        Vector3 current = ToLocal(trackedBall.position);
        lastLocalPosition = current;

        if (enteredFromBelow)
            return;

        if (previous.y < planeY || current.y >= planeY)
            return;

        if (trackedBall.linearVelocity.y > -minimumDownwardVelocity)
            return;

        float t = (previous.y - planeY) / (previous.y - current.y);
        Vector3 crossing = Vector3.Lerp(previous, current, t);

        if (new Vector2(crossing.x, crossing.z).magnitude > innerRadius)
            return;

        if (IsOnCooldown())
            return;

        RegisterScore();
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

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        Debug.Log("[HoopScoreTrigger] Encestada detectada.", this);
#endif

        hoopController?.AddPoint();

        if (visual != null)
            visual.PlayScoreFlash();

        Scored?.Invoke(this);
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Transform center = rimCenter;
        float y = planeY;
        float radius = innerRadius;

        if (!Application.isPlaying)
        {
            center = rimCenter != null ? rimCenter : transform.parent != null ? transform.parent.Find("RimColliders") : null;
            y = 0f;
            radius = rimInnerRadius;

            if (center != null && radius <= 0f && center.GetComponentInChildren<CapsuleCollider>() is { } segment)
            {
                Vector3 p = segment.transform.localPosition;
                radius = new Vector2(p.x, p.z).magnitude - segment.radius;
            }
        }

        if (center == null || radius <= 0f)
            return;

        Gizmos.color = Color.green;
        Gizmos.matrix = center.localToWorldMatrix;
        Vector3 last = new(radius, y, 0f);

        for (int i = 1; i <= 32; i++)
        {
            float a = i / 32f * Mathf.PI * 2f;
            Vector3 next = new(Mathf.Cos(a) * radius, y, Mathf.Sin(a) * radius);
            Gizmos.DrawLine(last, next);
            last = next;
        }
    }
#endif
}
