using UnityEngine;

[RequireComponent(typeof(BallLauncher))]
public sealed class BallCloseUpScale : MonoBehaviour
{
    [Tooltip("Parte visual de la pelota (BallArt). Vacío = el hijo con BallColor.")]
    [SerializeField]
    private Transform art;

    [Tooltip("Sombra de la pelota. Vacío = se busca en la escena.")]
    [SerializeField]
    private BallBlobShadow shadow;

    [Tooltip("Cuánto más grande se ve la pelota en reposo.")]
    [SerializeField, Range(1f, 2.5f)]
    private float closeUpScale = 1.5f;

    [Tooltip("Metros hacia el aro en los que la pelota vuelve a su tamaño real.")]
    [SerializeField, Min(0.1f)]
    private float fullSizeDistance = 5f;

    private BallLauncher launcher;
    private BallColor ballColor;
    private Vector3 artBaseScale = Vector3.one;
    private Vector3 forward = Vector3.forward;

    private void Awake()
    {
        launcher = GetComponent<BallLauncher>();

        if (art == null)
        {
            BallColor found = GetComponentInChildren<BallColor>(true);
            art = found != null ? found.transform : null;
        }

        if (art != null)
        {
            artBaseScale = art.localScale;
            ballColor = art.GetComponent<BallColor>();
        }

        if (shadow == null)
            shadow = FindFirstObjectByType<BallBlobShadow>();
    }

    public void SetArt(Transform newArt)
    {
        art = newArt;
        ballColor = null;

        if (art != null)
        {
            artBaseScale = art.localScale;
            ballColor = art.GetComponent<BallColor>();
        }
    }

    private void Start()
    {
        Camera camera = Camera.main;

        if (camera != null)
        {
            Vector3 flat = Vector3.ProjectOnPlane(camera.transform.forward, Vector3.up);

            if (flat.sqrMagnitude > 1e-6f)
                forward = flat.normalized;
        }
    }

    private void OnDisable()
    {
        if (art != null)
            art.localScale = artBaseScale;

        if (shadow != null)
            shadow.SizeMultiplier = 1f;
    }

    private void LateUpdate()
    {
        if (art == null)
            return;

        float advanced = Vector3.Dot(transform.position - launcher.RestPosition, forward);
        float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(advanced / fullSizeDistance));
        float scale = Mathf.Lerp(closeUpScale, 1f, t);

        float pop = ballColor != null ? ballColor.PopFactor : 1f;
        art.localScale = artBaseScale * (scale * pop);

        if (shadow != null)
            shadow.SizeMultiplier = scale;
    }
}
