using UnityEngine;

[ExecuteAlways]
[RequireComponent(typeof(Camera))]
public sealed class CourtCameraRig : MonoBehaviour
{
    [Header("Puntos de la cancha")]
    [SerializeField]
    private Vector3 ballPoint = new(0f, 1f, 0f);

    [SerializeField]
    private Vector3 rimPoint = new(0f, 2.5f, 8f);

    [Header("Encuadre (en 9:16)")]
    [Tooltip("Altura de la cámara (m). Más alta = se ve más el interior del aro, pero la pelota queda más chica.")]
    [SerializeField, Range(1.5f, 8f)]
    private float height = 4.5f;

    [Tooltip("FOV vertical de diseño en 9:16. Más alto = cámara más cerca y pelota algo más grande, con más deformación en los bordes.")]
    [SerializeField, Range(25f, 75f)]
    private float verticalFov = 50f;

    [Tooltip("Dónde cae la pelota en reposo: 0 = borde de arriba, 1 = borde de abajo.")]
    [SerializeField, Range(0.5f, 0.95f)]
    private float ballScreenY = 0.8f;

    [Tooltip("Dónde cae el centro del aro: 0 = borde de arriba, 1 = borde de abajo.")]
    [SerializeField, Range(0.1f, 0.6f)]
    private float rimScreenY = 0.4f;

    [Tooltip("Aspect ratio (ancho / alto) para el que se diseña el encuadre.")]
    [SerializeField, Min(0.01f)]
    private float referenceAspect = 1080f / 1920f;

    private bool dirty = true;
    private float lastAspect;
    private Camera targetCamera;

    private void OnEnable()
    {
        targetCamera = GetComponent<Camera>();

        if (TryGetComponent(out CameraAspectFit fit))
            fit.enabled = false;

        ApplyNow();
    }

    private void OnValidate()
    {
        dirty = true;
    }

    private void Update()
    {
        if (dirty || !Mathf.Approximately(targetCamera.aspect, lastAspect))
            ApplyNow();
    }

    public void ApplyNow()
    {
        dirty = false;

        Vector3 toRim = rimPoint - ballPoint;
        toRim.y = 0f;
        float rimDistance = toRim.magnitude;
        Vector3 forward = rimDistance > 1e-4f ? toRim / rimDistance : Vector3.forward;

        float tanHalf = Mathf.Tan(verticalFov * 0.5f * Mathf.Deg2Rad);
        float distance = SolveDistance(rimDistance, tanHalf);
        float pitch = PitchFor(distance, tanHalf);

        Vector3 position = ballPoint - forward * distance;
        position.y = height;

        Quaternion rotation =
            Quaternion.LookRotation(forward, Vector3.up) *
            Quaternion.Euler(pitch * Mathf.Rad2Deg, 0f, 0f);

        transform.SetPositionAndRotation(position, rotation);

        if (targetCamera == null)
            targetCamera = GetComponent<Camera>();

        targetCamera.fieldOfView = FitFov(targetCamera.aspect);
        lastAspect = targetCamera.aspect;
    }

    private float FitFov(float aspect)
    {
        float halfTan = Mathf.Tan(verticalFov * 0.5f * Mathf.Deg2Rad);
        float keepWidth = 2f * Mathf.Atan(halfTan * referenceAspect / Mathf.Max(aspect, 0.01f)) * Mathf.Rad2Deg;
        return Mathf.Clamp(Mathf.Max(verticalFov, keepWidth), 1f, 120f);
    }

    private float PitchFor(float distance, float tanHalf)
    {
        float ballAngle = Mathf.Atan2(height - ballPoint.y, distance);
        return ballAngle - Mathf.Atan((2f * ballScreenY - 1f) * tanHalf);
    }

    private float RimRow(float distance, float rimDistance, float tanHalf)
    {
        float rimAngle = Mathf.Atan2(height - rimPoint.y, rimDistance + distance);
        return 0.5f + 0.5f * Mathf.Tan(rimAngle - PitchFor(distance, tanHalf)) / tanHalf;
    }

    private float SolveDistance(float rimDistance, float tanHalf)
    {
        float low = 0.5f;
        float high = 40f;

        for (int i = 0; i < 40; i++)
        {
            float mid = (low + high) * 0.5f;

            if (RimRow(mid, rimDistance, tanHalf) < rimScreenY)
                low = mid;
            else
                high = mid;
        }

        return (low + high) * 0.5f;
    }
}
