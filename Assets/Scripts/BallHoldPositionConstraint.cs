using UnityEngine;

/// <summary>
/// Mantiene un collider agarrado fuera de los colliders sólidos.
/// Es independiente del input y puede reutilizarse para otros objetos arrastrables.
/// </summary>
[RequireComponent(typeof(Collider))]
public sealed class BallHoldPositionConstraint : MonoBehaviour
{
    [SerializeField, Min(0f)] private float separationPadding = 0.001f;
    [SerializeField, Range(1, 8)] private int maximumResolutionIterations = 4;
    [SerializeField, Min(0.1f)] private float groundProbeHeight = 20f;
    [SerializeField] private LayerMask blockingLayers = Physics.DefaultRaycastLayers;

    private Collider ownCollider;
    private float cachedResolutionRadius;
    private bool hasCachedResolutionRadius;
    private readonly Collider[] overlapBuffer = new Collider[32];
    private readonly RaycastHit[] groundHitBuffer = new RaycastHit[32];

    /// <summary>
    /// Devuelve la posición más cercana al objetivo que no penetra colliders bloqueantes.
    /// </summary>
    public Vector3 Constrain(Vector3 desiredPosition)
    {
        // AddComponent puede ejecutar este método antes de Awake en el primer frame.
        // Resolvemos la referencia aquí para que el primer arrastre también quede protegido.
        ownCollider ??= GetComponent<Collider>();
        if (ownCollider == null)
            return desiredPosition;

        Vector3 resolvedPosition = ClampAboveGround(desiredPosition);

        // El radio depende del tamaño del collider, que en la práctica no cambia en
        // runtime para una pelota. Se calcula una sola vez para evitar recalcular
        // bounds.extents en cada frame de arrastre.
        if (!hasCachedResolutionRadius)
        {
            cachedResolutionRadius = ownCollider.bounds.extents.magnitude;
            hasCachedResolutionRadius = true;
        }

        for (int iteration = 0; iteration < maximumResolutionIterations; iteration++)
        {
            int overlapCount = Physics.OverlapSphereNonAlloc(
                resolvedPosition,
                cachedResolutionRadius,
                overlapBuffer,
                blockingLayers,
                QueryTriggerInteraction.Ignore);

            bool resolvedAnyOverlap = false;
            for (int index = 0; index < overlapCount; index++)
            {
                Collider otherCollider = overlapBuffer[index];
                if (otherCollider == null || otherCollider == ownCollider)
                    continue;

                if (!Physics.ComputePenetration(
                        ownCollider,
                        resolvedPosition,
                        transform.rotation,
                        otherCollider,
                        otherCollider.transform.position,
                        otherCollider.transform.rotation,
                        out Vector3 direction,
                        out float distance))
                    continue;

                resolvedPosition += direction * (distance + separationPadding);
                resolvedAnyOverlap = true;
            }

            if (!resolvedAnyOverlap)
                break;
        }

        return resolvedPosition;
    }

    private Vector3 ClampAboveGround(Vector3 desiredPosition)
    {
        float originY = Mathf.Max(desiredPosition.y, transform.position.y) + groundProbeHeight;
        Vector3 origin = new(desiredPosition.x, originY, desiredPosition.z);
        float distance = originY - desiredPosition.y + groundProbeHeight;
        int hitCount = Physics.RaycastNonAlloc(
            origin,
            Vector3.down,
            groundHitBuffer,
            distance,
            blockingLayers,
            QueryTriggerInteraction.Ignore);

        RaycastHit? nearestGround = null;
        for (int index = 0; index < hitCount; index++)
        {
            RaycastHit hit = groundHitBuffer[index];
            if (hit.collider == ownCollider || hit.normal.y <= 0.5f)
                continue;

            if (!nearestGround.HasValue || hit.distance < nearestGround.Value.distance)
                nearestGround = hit;
        }

        if (!nearestGround.HasValue)
            return desiredPosition;

        float minimumCenterY = nearestGround.Value.point.y
            + ownCollider.bounds.extents.y
            + separationPadding;
        desiredPosition.y = Mathf.Max(desiredPosition.y, minimumCenterY);
        return desiredPosition;
    }
}