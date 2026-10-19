using UnityEngine;
using UnityEngine.InputSystem;

public sealed class BallColor : MonoBehaviour
{
    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

    [System.Serializable]
    public struct ExtraTint
    {
        public Renderer renderer;
        public int materialIndex;
        public Color[] colors;
    }

    [Header("Render")]
    [SerializeField]
    private Renderer bodyRenderer;

    [Tooltip("Índice del material del cuerpo (el 0; las costuras son el 1).")]
    [SerializeField, Min(0)]
    private int bodyMaterialIndex;

    [Tooltip("Lo que hace el pop de escala (por defecto, este transform).")]
    [SerializeField]
    private Transform popTarget;

    [Header("Colores")]
    [SerializeField]
    private Color[] colors =
    {
        new(0.949f, 0.541f, 0.118f, 1f),
        new(0.180f, 0.769f, 0.902f, 1f),
        new(0.941f, 0.306f, 0.541f, 1f),
        new(0.949f, 0.824f, 0.118f, 1f)
    };

    [Tooltip("Partes que cambian junto con el cuerpo (vacío en la Clásica).")]
    [SerializeField]
    private ExtraTint[] extraTints = System.Array.Empty<ExtraTint>();

    [Header("Sacudida")]
    [Tooltip("Aceleración extra (en g, descontando la gravedad) que cuenta como sacudida.")]
    [SerializeField, Min(0f)]
    private float shakeThreshold = 2.5f;

    [SerializeField, Min(0f)]
    private float cooldown = 0.5f;

    [Header("Transición")]
    [SerializeField, Min(0.01f)]
    private float transitionDuration = 0.18f;

    [SerializeField, Min(1f)]
    private float popScale = 1.15f;

    private MaterialPropertyBlock block;
    private int index;
    private Color fromColor;
    private Color toColor;
    private Color[] extraFrom = System.Array.Empty<Color>();
    private Color[] extraTo = System.Array.Empty<Color>();
    private float transitionTime = -1f;
    private float lastShakeTime = float.NegativeInfinity;
    private Vector3 baseScale = Vector3.one;

    public float PopFactor { get; private set; } = 1f;

    private void Awake()
    {
        block = new MaterialPropertyBlock();

        if (popTarget == null)
            popTarget = transform;

        baseScale = popTarget.localScale;

        extraFrom = new Color[extraTints.Length];
        extraTo = new Color[extraTints.Length];

        if (colors.Length > 0)
        {
            ApplyColor(colors[0]);

            for (int i = 0; i < extraTints.Length; i++)
                ApplyExtra(i, ExtraColor(i, 0));
        }
    }

    private void OnEnable()
    {
        if (Accelerometer.current != null)
            InputSystem.EnableDevice(Accelerometer.current);
    }

    private void OnDisable()
    {
        if (Accelerometer.current != null)
            InputSystem.DisableDevice(Accelerometer.current);

        if (popTarget != null)
            popTarget.localScale = baseScale;
    }

    private void Update()
    {
        if (DetectShake())
            NextColor();

        UpdateTransition();
    }

    private bool DetectShake()
    {
#if UNITY_EDITOR
        if (Keyboard.current != null && Keyboard.current.cKey.wasPressedThisFrame)
            return true;
#endif

        if (Accelerometer.current == null)
            return false;

        Vector3 acceleration = Accelerometer.current.acceleration.ReadValue();

        if (Mathf.Abs(acceleration.magnitude - 1f) < shakeThreshold)
            return false;

        if (Time.unscaledTime - lastShakeTime < cooldown)
            return false;

        lastShakeTime = Time.unscaledTime;
        return true;
    }

    public void NextColor()
    {
        if (colors.Length == 0)
            return;

        bool midTransition = transitionTime >= 0f;
        float progress = midTransition ? Mathf.Clamp01(transitionTime / transitionDuration) : 0f;

        fromColor = midTransition ? Color.Lerp(fromColor, toColor, progress) : colors[index];

        for (int i = 0; i < extraTints.Length; i++)
            extraFrom[i] = midTransition ? Color.Lerp(extraFrom[i], extraTo[i], progress) : ExtraColor(i, index);

        index = (index + 1) % colors.Length;
        toColor = colors[index];

        for (int i = 0; i < extraTints.Length; i++)
            extraTo[i] = ExtraColor(i, index);

        transitionTime = 0f;
    }

    private void UpdateTransition()
    {
        if (transitionTime < 0f)
            return;

        transitionTime += Time.unscaledDeltaTime;
        float t = Mathf.Clamp01(transitionTime / transitionDuration);

        ApplyColor(Color.Lerp(fromColor, toColor, t));

        for (int i = 0; i < extraTints.Length; i++)
            ApplyExtra(i, Color.Lerp(extraFrom[i], extraTo[i], t));

        PopFactor = Mathf.Lerp(1f, popScale, Mathf.Sin(t * Mathf.PI));
        popTarget.localScale = baseScale * PopFactor;

        if (t >= 1f)
        {
            transitionTime = -1f;
            PopFactor = 1f;
            popTarget.localScale = baseScale;
        }
    }

    private void ApplyColor(Color color)
    {
        if (bodyRenderer == null)
            return;

        bodyRenderer.GetPropertyBlock(block, bodyMaterialIndex);
        block.SetColor(BaseColorId, color);
        bodyRenderer.SetPropertyBlock(block, bodyMaterialIndex);
    }

    private Color ExtraColor(int extra, int variant)
    {
        Color[] extraColors = extraTints[extra].colors;

        if (extraColors == null || extraColors.Length == 0)
            return Color.white;

        return extraColors[Mathf.Min(variant, extraColors.Length - 1)];
    }

    private void ApplyExtra(int extra, Color color)
    {
        ExtraTint tint = extraTints[extra];

        if (tint.renderer == null)
            return;

        tint.renderer.GetPropertyBlock(block, tint.materialIndex);
        block.SetColor(BaseColorId, color);
        tint.renderer.SetPropertyBlock(block, tint.materialIndex);
    }
}
