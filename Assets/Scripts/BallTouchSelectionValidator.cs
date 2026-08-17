using UnityEngine;

/// <summary>Valida que un gesto comience sobre el collider de esta pelota.</summary>
[RequireComponent(typeof(Collider))]
public sealed class BallTouchSelectionValidator : MonoBehaviour
{
    [SerializeField, Min(0.1f)] private float raycastDistance = 100f;
    [SerializeField] private LayerMask selectableLayers = Physics.DefaultRaycastLayers;

    private Collider ballCollider;

    public bool IsSelected(Camera camera, Vector2 screenPosition)
    {
        if (camera == null)
            return false;

        ballCollider ??= GetComponent<Collider>();
        if (ballCollider == null)
            return false;

        Ray ray = camera.ScreenPointToRay(screenPosition);
        return Physics.Raycast(
            ray,
            out RaycastHit hit,
            raycastDistance,
            selectableLayers,
            QueryTriggerInteraction.Ignore)
            && hit.collider == ballCollider;
    }
}
