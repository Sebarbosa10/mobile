using UnityEngine;

public sealed class BallBlobShadow : MonoBehaviour
{
    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

    [SerializeField]
    private Transform target;

    [SerializeField]
    private float groundY;

    [Tooltip("Separación del piso para que nunca haga z-fighting.")]
    [SerializeField, Min(0f)]
    private float lift = 0.012f;

    [Tooltip("Diámetro con la pelota apoyada en el piso.")]
    [SerializeField, Min(0.01f)]
    private float size = 0.5f;

    [Tooltip("Altura a partir de la cual la sombra queda en su mínimo.")]
    [SerializeField, Min(0.1f)]
    private float fadeHeight = 4f;

    [SerializeField, Range(0f, 1f)]
    private float minScale = 0.5f;

    [SerializeField]
    private Color color = new(0.165f, 0.129f, 0.251f, 0.35f);

    [SerializeField, Range(0f, 1f)]
    private float minAlphaFactor = 0.35f;

    private Renderer shadowRenderer;
    private MaterialPropertyBlock block;

    public float SizeMultiplier { get; set; } = 1f;

    private void Awake()
    {
        shadowRenderer = GetComponentInChildren<Renderer>();
        block = new MaterialPropertyBlock();
    }

    private void LateUpdate()
    {
        if (target == null || shadowRenderer == null)
            return;

        Vector3 position = target.position;
        float height = position.y - groundY;

        if (height < 0f)
        {
            shadowRenderer.enabled = false;
            return;
        }

        shadowRenderer.enabled = true;

        float k = Mathf.Clamp01(height / fadeHeight);
        transform.SetPositionAndRotation(new Vector3(position.x, groundY + lift, position.z), Quaternion.identity);
        transform.localScale = Vector3.one * (size * SizeMultiplier * Mathf.Lerp(1f, minScale, k));

        block.SetColor(BaseColorId, new Color(color.r, color.g, color.b, color.a * Mathf.Lerp(1f, minAlphaFactor, k)));
        shadowRenderer.SetPropertyBlock(block);
    }
}
