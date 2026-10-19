using UnityEngine;

[ExecuteAlways]
[DisallowMultipleComponent]
public sealed class StylizedLook : MonoBehaviour
{
    private static readonly int PosterizeStepsId = Shader.PropertyToID("_PosterizeSteps");
    private static readonly int PosterizeAmountId = Shader.PropertyToID("_PosterizeAmount");
    private static readonly int PosterizeDitherId = Shader.PropertyToID("_PosterizeDither");

    private static readonly int OutlineColorId = Shader.PropertyToID("_OutlineColor");
    private static readonly int OutlineThicknessId = Shader.PropertyToID("_OutlineThickness");
    private static readonly int OutlineThresholdId = Shader.PropertyToID("_OutlineThreshold");
    private static readonly int OutlineFadeDistanceId = Shader.PropertyToID("_OutlineFadeDistance");
    private static readonly int SaturationId = Shader.PropertyToID("_Saturation");
    private static readonly int ContrastId = Shader.PropertyToID("_Contrast");
    private static readonly int ShadowTintId = Shader.PropertyToID("_ShadowTint");
    private static readonly int HighlightTintId = Shader.PropertyToID("_HighlightTint");
    private static readonly int SplitToningId = Shader.PropertyToID("_SplitToning");
    private static readonly int VignetteColorId = Shader.PropertyToID("_VignetteColor");
    private static readonly int VignetteIntensityId = Shader.PropertyToID("_VignetteIntensity");
    private static readonly int VignetteRadiusId = Shader.PropertyToID("_VignetteRadius");
    private static readonly int VignetteSoftnessId = Shader.PropertyToID("_VignetteSoftness");

    public static StylizedLook Active { get; private set; }

    [Header("Posterizado")]
    [SerializeField]
    private bool posterize = true;

    [Tooltip("Escalones por canal. Menos = más \"póster\".")]
    [SerializeField, Range(2, 32)]
    private int posterizeSteps = 8;

    [Tooltip("Mezcla con la imagen original (1 = posterizado completo).")]
    [SerializeField, Range(0f, 1f)]
    private float posterizeAmount = 1f;

    [Tooltip("Dither para suavizar las bandas (0 = bandas duras).")]
    [SerializeField, Range(0f, 1f)]
    private float posterizeDither = 0.35f;

    [Header("Post estilizado")]
    [SerializeField]
    private bool stylized = true;

    [Header("Contorno")]
    [SerializeField]
    private bool outline = true;

    [SerializeField]
    private Color outlineColor = new(0.165f, 0.129f, 0.251f, 1f);

    [Tooltip("Grosor en píxeles.")]
    [SerializeField, Range(0.5f, 4f)]
    private float outlineThickness = 1.5f;

    [Tooltip("Diferencia de profundidad (relativa) a partir de la cual hay borde. Más alto = menos líneas.")]
    [SerializeField, Range(0.01f, 1f)]
    private float outlineThreshold = 0.15f;

    [Tooltip("Metros a partir de los cuales el contorno ya no se dibuja.")]
    [SerializeField, Min(1f)]
    private float outlineFadeDistance = 60f;

    [Header("Color")]
    [SerializeField, Range(0f, 2f)]
    private float saturation = 1.15f;

    [SerializeField, Range(0.5f, 1.5f)]
    private float contrast = 1.08f;

    [SerializeField]
    private Color shadowTint = new(0.45f, 0.35f, 0.85f, 1f);

    [SerializeField]
    private Color highlightTint = new(1f, 0.78f, 0.55f, 1f);

    [SerializeField, Range(0f, 1f)]
    private float splitToning = 0.25f;

    [Header("Viñeta")]
    [SerializeField]
    private Color vignetteColor = new(0.165f, 0.129f, 0.251f, 1f);

    [SerializeField, Range(0f, 1f)]
    private float vignetteIntensity = 0.35f;

    [SerializeField, Range(0.1f, 1.5f)]
    private float vignetteRadius = 1.05f;

    [SerializeField, Range(0.01f, 1f)]
    private float vignetteSoftness = 0.6f;

    public bool PosterizeEnabled => posterize;
    public bool StylizedEnabled => stylized;
    public bool OutlineEnabled => stylized && outline;

    private void OnEnable()
    {
        Active = this;
    }

    private void OnDisable()
    {
        if (Active == this)
            Active = null;
    }

    public void ApplyPosterize(Material material)
    {
        material.SetFloat(PosterizeStepsId, posterizeSteps);
        material.SetFloat(PosterizeAmountId, posterizeAmount);
        material.SetFloat(PosterizeDitherId, posterizeDither);
    }

    public void ApplyStylized(Material material)
    {
        if (outline)
            material.EnableKeyword("_OUTLINE");
        else
            material.DisableKeyword("_OUTLINE");

        material.SetColor(OutlineColorId, outlineColor);
        material.SetFloat(OutlineThicknessId, outlineThickness);
        material.SetFloat(OutlineThresholdId, outlineThreshold);
        material.SetFloat(OutlineFadeDistanceId, outlineFadeDistance);

        material.SetFloat(SaturationId, saturation);
        material.SetFloat(ContrastId, contrast);
        material.SetColor(ShadowTintId, shadowTint);
        material.SetColor(HighlightTintId, highlightTint);
        material.SetFloat(SplitToningId, splitToning);

        material.SetColor(VignetteColorId, vignetteColor);
        material.SetFloat(VignetteIntensityId, vignetteIntensity);
        material.SetFloat(VignetteRadiusId, vignetteRadius);
        material.SetFloat(VignetteSoftnessId, vignetteSoftness);
    }
}
