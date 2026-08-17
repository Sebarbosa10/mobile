using UnityEngine;

/// <summary>
/// Mantiene la pelota fuera de colliders sólidos mientras es arrastrada.
/// Resuelve penetraciones y evita que la pelota atraviese el suelo.
/// </summary>
[RequireComponent(typeof(Collider))]
public sealed class BallHoldPositionConstraint : MonoBehaviour
{
    [Header("Resolución")]
    [SerializeField, Min(0f)]
    private float separationPadding = 0.001f;

    [SerializeField, Range(1, 8)]
    private int maximumResolutionIterations = 4;

    [Header("Suelo")]
    [SerializeField, Min(0.1f)]
    private float groundProbeHeight = 20f;

    [SerializeField]
    private LayerMask blockingLayers = Physics.DefaultRaycastLayers;

    private Collider ownCollider;

    private float cachedResolutionRadius;
    private bool hasCachedResolutionRadius;

    private readonly Collider[] overlapBuffer =
        new Collider[32];

    private readonly RaycastHit[] groundHitBuffer =
        new RaycastHit[32];

    private void Awake()
    {
        ownCollider = GetComponent<Collider>();
    }

    /// <summary>
    /// Devuelve la posición más cercana al objetivo
    /// que no penetra colliders bloqueantes.
    /// </summary>
    public Vector3 Constrain(Vector3 desiredPosition)
    {
        if (ownCollider == null)
            return desiredPosition;

        Vector3 resolvedPosition =
            ClampAboveGround(desiredPosition);

        CacheResolutionRadius();

        for (int iteration = 0;
             iteration < maximumResolutionIterations;
             iteration++)
        {
            int overlapCount = Physics.OverlapSphereNonAlloc(
                resolvedPosition,
                cachedResolutionRadius,
                overlapBuffer,
                blockingLayers,
                QueryTriggerInteraction.Ignore);

            bool resolvedAnyOverlap = false;

            for (int index = 0;
                 index < overlapCount;
                 index++)
            {
                Collider otherCollider =
                    overlapBuffer[index];

                if (otherCollider == null)
                    continue;

                if (otherCollider == ownCollider)
                    continue;

                bool hasPenetration =
                    Physics.ComputePenetration(
                        ownCollider,
                        resolvedPosition,
                        transform.rotation,
                        otherCollider,
                        otherCollider.transform.position,
                        otherCollider.transform.rotation,
                        out Vector3 direction,
                        out float distance);

                if (!hasPenetration)
                    continue;

                resolvedPosition +=
                    direction *
                    (distance + separationPadding);

                resolvedAnyOverlap = true;
            }

            if (!resolvedAnyOverlap)
                break;
        }

        return resolvedPosition;
    }

    private void CacheResolutionRadius()
    {
        if (hasCachedResolutionRadius)
            return;

        cachedResolutionRadius =
            ownCollider.bounds.extents.magnitude;

        hasCachedResolutionRadius = true;
    }

    private Vector3 ClampAboveGround(
        Vector3 desiredPosition)
    {
        float originY =
            Mathf.Max(
                desiredPosition.y,
                transform.position.y)
            + groundProbeHeight;

        Vector3 origin = new Vector3(
            desiredPosition.x,
            originY,
            desiredPosition.z);

        float distance =
            originY
            - desiredPosition.y
            + groundProbeHeight;

        int hitCount = Physics.RaycastNonAlloc(
            origin,
            Vector3.down,
            groundHitBuffer,
            distance,
            blockingLayers,
            QueryTriggerInteraction.Ignore);

        RaycastHit? nearestGround = null;

        for (int index = 0;
             index < hitCount;
             index++)
        {
            RaycastHit hit = groundHitBuffer[index];

            if (hit.collider == ownCollider)
                continue;

            if (hit.normal.y <= 0.5f)
                continue;

            if (!nearestGround.HasValue ||
                hit.distance < nearestGround.Value.distance)
            {
                nearestGround = hit;
            }
        }

        if (!nearestGround.HasValue)
            return desiredPosition;

        float minimumCenterY =
            nearestGround.Value.point.y
            + ownCollider.bounds.extents.y
            + separationPadding;

        desiredPosition.y =
            Mathf.Max(
                desiredPosition.y,
                minimumCenterY);

        return desiredPosition;
    }
}