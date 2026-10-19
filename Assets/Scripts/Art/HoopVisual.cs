using UnityEngine;

public sealed class HoopVisual : MonoBehaviour
{
    public static readonly Color SuccessColor = new(0.251f, 0.851f, 0.349f, 1f);

    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

    [Header("Partes")]
    [SerializeField]
    private Transform scaledRoot;

    [SerializeField]
    private Renderer[] rimRenderers = System.Array.Empty<Renderer>();

    [SerializeField]
    private Renderer glowRenderer;

    [SerializeField]
    private ChainNet net;

    [SerializeField]
    private GameObject checkMark;

    [Header("Poste anclado al piso")]
    [SerializeField]
    private Transform poleAttach;

    [SerializeField]
    private Transform pole;

    [SerializeField]
    private Transform poleShadow;

    [SerializeField]
    private float groundY;

    [SerializeField, Min(0f)]
    private float shadowLengthPerMeter = 3.73f;

    [Header("Brillo y destello")]
    [SerializeField, Range(0f, 1f)]
    private float glowAlpha = 0.35f;

    [SerializeField]
    private Color flashColor = new(1f, 0.851f, 0.627f, 1f);

    [SerializeField, Min(0.01f)]
    private float flashDuration = 0.35f;

    [SerializeField, Min(1f)]
    private float flashGlowScale = 1.35f;

    [SerializeField, Min(1f)]
    private float flashRimScale = 1.08f;

    private MaterialPropertyBlock block;
    private Color highlightColor;
    private bool highlighted;
    private float flashTime = -1f;
    private Vector3 lastAttachPosition = new(float.NaN, 0f, 0f);
    private Vector3[] rimBaseScales;
    private Vector3 glowBaseScale = Vector3.one;

    public float Scale =>
        scaledRoot != null ? scaledRoot.localScale.x : 1f;

    private void Awake()
    {
        block = new MaterialPropertyBlock();

        rimBaseScales = new Vector3[rimRenderers.Length];
        for (int i = 0; i < rimRenderers.Length; i++)
            if (rimRenderers[i] != null)
                rimBaseScales[i] = rimRenderers[i].transform.localScale;

        if (glowRenderer != null)
        {
            glowBaseScale = glowRenderer.transform.localScale;
            glowRenderer.enabled = false;
        }

        if (checkMark != null)
            checkMark.SetActive(false);

        UpdatePole();
    }

    private void LateUpdate()
    {
        UpdatePole();
        UpdateFlash();
    }

    public void SetScale(float scale)
    {
        if (scaledRoot == null)
            return;

        scaledRoot.localScale = Vector3.one * Mathf.Max(0.01f, scale);
        UpdatePole();
    }

    public void SetColor(Color color)
    {
        highlighted = true;
        highlightColor = color;

        foreach (Renderer rim in rimRenderers)
        {
            if (rim == null)
                continue;

            rim.GetPropertyBlock(block);
            block.SetColor(BaseColorId, color);
            rim.SetPropertyBlock(block);
        }

        ApplyGlow(color, glowAlpha, 1f);
    }

    public void ResetColor()
    {
        highlighted = false;

        foreach (Renderer rim in rimRenderers)
            if (rim != null)
                rim.SetPropertyBlock(null);

        if (flashTime < 0f && glowRenderer != null)
            glowRenderer.enabled = false;
    }

    public void SetCheckVisible(bool visible)
    {
        if (checkMark != null)
            checkMark.SetActive(visible);
    }

    public void PlayScoreFlash()
    {
        if (net != null)
            net.Shake();

        flashTime = 0f;
    }

    private void UpdateFlash()
    {
        if (flashTime < 0f)
            return;

        flashTime += Time.deltaTime;
        float t = Mathf.Clamp01(flashTime / flashDuration);

        float pop = Mathf.Sin(t * Mathf.PI);
        for (int i = 0; i < rimRenderers.Length; i++)
            if (rimRenderers[i] != null)
                rimRenderers[i].transform.localScale = rimBaseScales[i] * Mathf.Lerp(1f, flashRimScale, pop);

        Color color = highlighted ? highlightColor : flashColor;
        float alpha = Mathf.Lerp(0.8f, highlighted ? glowAlpha : 0f, t);
        ApplyGlow(color, alpha, Mathf.Lerp(1f, flashGlowScale, 1f - (1f - t) * (1f - t)));

        if (t < 1f)
            return;

        flashTime = -1f;

        for (int i = 0; i < rimRenderers.Length; i++)
            if (rimRenderers[i] != null)
                rimRenderers[i].transform.localScale = rimBaseScales[i];

        if (highlighted)
            ApplyGlow(highlightColor, glowAlpha, 1f);
        else if (glowRenderer != null)
            glowRenderer.enabled = false;
    }

    private void ApplyGlow(Color color, float alpha, float scale)
    {
        if (glowRenderer == null)
            return;

        glowRenderer.enabled = alpha > 0.001f;
        glowRenderer.transform.localScale = glowBaseScale * scale;
        glowRenderer.GetPropertyBlock(block);
        block.SetColor(BaseColorId, new Color(color.r, color.g, color.b, alpha));
        glowRenderer.SetPropertyBlock(block);
    }

    public void SnapPoleToGround()
    {
        lastAttachPosition = new Vector3(float.NaN, 0f, 0f);
        UpdatePole();
    }

    private void UpdatePole()
    {
        if (pole == null || poleAttach == null)
            return;

        Vector3 attach = poleAttach.position;

        if ((attach - lastAttachPosition).sqrMagnitude < 1e-8f)
            return;

        lastAttachPosition = attach;

        float height = Mathf.Max(0.01f, attach.y - groundY);
        pole.position = new Vector3(attach.x, groundY, attach.z);
        pole.localScale = new Vector3(1f, height, 1f);

        if (poleShadow != null)
        {
            poleShadow.position = new Vector3(attach.x, groundY, attach.z);
            poleShadow.localScale = new Vector3(1f, 1f, height * shadowLengthPerMeter);
        }
    }
}
