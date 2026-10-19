using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Al sacudir el celular, la pelota rota al siguiente color de la
/// paleta (naranja, celeste, rosa, amarillo) con un "pop" de escala
/// corto. El contorno y las costuras no cambian.
///
/// La sacudida se detecta con el acelerómetro: |aceleración| − 1 g
/// (lo que sobra de la gravedad) por encima de un umbral, con cooldown
/// para que una sola sacudida no cambie varios colores seguidos.
/// En el Editor, la tecla C hace lo mismo para probar sin celular.
/// </summary>
public sealed class BallColor : MonoBehaviour
{
    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

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
        new(0.949f, 0.541f, 0.118f, 1f), // #F28A1E naranja
        new(0.180f, 0.769f, 0.902f, 1f), // #2EC4E6 celeste
        new(0.941f, 0.306f, 0.541f, 1f), // #F04E8A rosa
        new(0.949f, 0.824f, 0.118f, 1f)  // #F2D21E amarillo
    };

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
    private float transitionTime = -1f;
    private float lastShakeTime = float.NegativeInfinity;
    private Vector3 baseScale = Vector3.one;

    public Color CurrentColor =>
        colors.Length > 0 ? colors[index] : Color.white;

    private void Awake()
    {
        block = new MaterialPropertyBlock();

        if (popTarget == null)
            popTarget = transform;

        baseScale = popTarget.localScale;

        if (colors.Length > 0)
            ApplyColor(colors[0]);
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

    /// <summary>Pasa al siguiente color de la paleta con la transición.</summary>
    public void NextColor()
    {
        if (colors.Length == 0)
            return;

        fromColor = transitionTime >= 0f ? Color.Lerp(fromColor, toColor, transitionTime / transitionDuration) : colors[index];
        index = (index + 1) % colors.Length;
        toColor = colors[index];
        transitionTime = 0f;
    }

    private void UpdateTransition()
    {
        if (transitionTime < 0f)
            return;

        // unscaled: si se sacude en pausa, la transición igual termina.
        transitionTime += Time.unscaledDeltaTime;
        float t = Mathf.Clamp01(transitionTime / transitionDuration);

        ApplyColor(Color.Lerp(fromColor, toColor, t));
        popTarget.localScale = baseScale * Mathf.Lerp(1f, popScale, Mathf.Sin(t * Mathf.PI));

        if (t >= 1f)
        {
            transitionTime = -1f;
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
}
