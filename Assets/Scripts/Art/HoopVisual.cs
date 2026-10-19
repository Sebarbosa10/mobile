using UnityEngine;

/// <summary>
/// Parte visual de un aro (prefabs Hoop y HoopSmall). No sabe nada del
/// puntaje: el gameplay le pide cosas y ella las muestra.
///
///  - SetScale: achica o agranda el aro, el tablero, la red y el
///    trigger (todo lo que cuelga de "Scaled"). El poste no se escala:
///    se estira para seguir apoyado en el piso.
///  - SetColor / ResetColor: pinta el aro (por ejemplo de verde al
///    acertarlo en el Carrusel) y muestra un anillo de brillo plano.
///  - SetCheckVisible: el check que flota sobre el aro acertado.
///  - PlayScoreFlash: el efecto 3D de una encestada (la red se sacude y
///    el anillo destella). Los efectos 2D (fx_burst, fx_ring, "+1") los
///    sigue disparando la UI desde IGameSession.Scored.
///
/// Si el aro se mueve (HoopMovement, EvasiveHoopMover), el poste y su
/// sombra se recalculan cada frame para quedar anclados al piso.
/// </summary>
public sealed class HoopVisual : MonoBehaviour
{
    /// <summary>Verde de "acertado" (#40D959), igual que en la UI.</summary>
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

    /// <summary>Pinta el aro de un color y muestra el anillo de brillo detrás.</summary>
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

    /// <summary>Vuelve al color del material y apaga el brillo.</summary>
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

    /// <summary>Efecto 3D de encestada: sacude la red y hace destellar el aro.</summary>
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

        // Pop rápido del aro (sube y vuelve) y un anillo que se abre y apaga.
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

    /// <summary>
    /// Apoya el poste en el piso ya mismo (también en el Editor, donde
    /// no corre Awake/LateUpdate). Lo usa el builder al ubicar el aro.
    /// </summary>
    public void SnapPoleToGround()
    {
        lastAttachPosition = new Vector3(float.NaN, 0f, 0f);
        UpdatePole();
    }

    /// <summary>
    /// Estira el poste desde el piso hasta el punto de enganche del
    /// tablero y alarga su sombra en proporción a la altura. Solo hace
    /// algo cuando el enganche se movió.
    /// </summary>
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
