using UnityEngine;

/// <summary>
/// Mantiene el campo de visión HORIZONTAL constante sin importar el
/// aspect ratio real de la pantalla, ajustando en runtime el FOV
/// vertical de la cámara (que es el que Unity expone).
///
/// Sin esto, una cámara con FOV vertical fijo muestra menos ancho en
/// celulares angostos/altos que en uno más "cuadrado" — lo cual puede
/// recortar contenido que se separa horizontalmente (los aros del
/// carrusel del Desafío 1, el movimiento lateral/diagonal del aro en
/// el modo endless). Fijando el FOV horizontal en cambio, el encuadre
/// horizontal es siempre el mismo que el usado para diseñar y probar
/// la escena; lo único que varía entre dispositivos es cuánto se ve
/// de más o de menos verticalmente (piso/cielo, no contenido de
/// juego).
/// </summary>
[RequireComponent(typeof(Camera))]
public sealed class CameraAspectFit : MonoBehaviour
{
    [Tooltip("Aspect ratio (ancho / alto) con el que se diseñó y probó la escena.")]
    [SerializeField, Min(0.01f)]
    private float referenceAspect = 1080f / 1920f;

    [Tooltip("FOV vertical con el que se diseñó la escena a ese aspect ratio.")]
    [SerializeField, Range(1f, 179f)]
    private float referenceVerticalFov = 60f;

    [Tooltip("Límites de seguridad para el FOV vertical resultante, por si algún dispositivo tiene un aspect ratio extremo.")]
    [SerializeField]
    private Vector2 verticalFovClamp = new(20f, 100f);

    private Camera targetCamera;
    private float lastAspect;

    private void Awake()
    {
        targetCamera = GetComponent<Camera>();
    }

    private void OnEnable()
    {
        Apply();
    }

    private void Update()
    {
        // El aspect ratio puede cambiar en runtime (rotación de pantalla,
        // ventana redimensionada en el editor); recalculamos solo si cambió.
        if (!Mathf.Approximately(targetCamera.aspect, lastAspect))
        {
            Apply();
        }
    }

    private void Apply()
    {
        float targetHorizontalFov =
            VerticalToHorizontalFov(referenceVerticalFov, referenceAspect);

        float verticalFov =
            HorizontalToVerticalFov(targetHorizontalFov, targetCamera.aspect);

        targetCamera.fieldOfView = Mathf.Clamp(
            verticalFov,
            verticalFovClamp.x,
            verticalFovClamp.y);

        lastAspect = targetCamera.aspect;
    }

    private static float VerticalToHorizontalFov(float verticalFov, float aspect)
    {
        float verticalRadians = verticalFov * Mathf.Deg2Rad;

        float horizontalRadians =
            2f * Mathf.Atan(Mathf.Tan(verticalRadians * 0.5f) * aspect);

        return horizontalRadians * Mathf.Rad2Deg;
    }

    private static float HorizontalToVerticalFov(float horizontalFov, float aspect)
    {
        float horizontalRadians = horizontalFov * Mathf.Deg2Rad;

        float verticalRadians =
            2f * Mathf.Atan(Mathf.Tan(horizontalRadians * 0.5f) / aspect);

        return verticalRadians * Mathf.Rad2Deg;
    }
}
